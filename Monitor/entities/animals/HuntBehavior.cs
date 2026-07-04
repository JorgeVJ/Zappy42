using Godot;

/// <summary>
/// Comportamiento de caza genérico, como pequeña máquina de estados (Acecho → Ataque →
/// Recuperación). Cuando el blackboard detecta una presa cerca, su Score supera al paseo: el
/// depredador va hacia ella con el estado <see cref="HuntState"/> y, a corta distancia, lanza
/// <see cref="AttackAction"/> (one-shot). Al terminar el golpe **captura** la presa (la elimina) y
/// se queda un instante quieto antes de ceder el control de vuelta al paseo.
/// </summary>
/// <remarks>
/// Agnóstico de especie: la detección de presas la hace <see cref="AnimalContext"/> (por grupo de
/// Godot, <see cref="PreyGroup"/>) y la animación la resuelve <see cref="IAnimated"/>; no castea a
/// ningún tipo concreto, así que sirve para cualquier depredador (aquí el zorro).
/// </remarks>
public class HuntBehavior : IUtilityBehavior<Animal>
{
	/// <summary>Grupo de Godot al que pertenecen las presas cazables (lo puebla AnimalSystem).</summary>
	public const string PreyGroup = "Prey";

	/// <summary>Distancia (horizontal) a la que se lanza el ataque.</summary>
	public float AttackRange = 1.2f;

	/// <summary>Peso de la caza: debe superar al paseo (≈1) para ganar el cerebro.</summary>
	public float HuntWeight = 4f;

	/// <summary>Multiplicador de velocidad mientras acecha.</summary>
	public float HuntSpeedScale = 1.4f;

	/// <summary>Segundos que permanece quieto tras capturar, antes de retomar el paseo.</summary>
	public float RecoverTime = 0.8f;

	/// <summary>Duración máxima del ataque (s): red de seguridad por si el clip no notifica su fin o no existe.</summary>
	public float AttackMaxDuration = 1.5f;

	/// <summary>Estado de animación (IAnimated) mientras acecha.</summary>
	public string HuntState = "hunt";

	/// <summary>Acción one-shot (IAnimated) del golpe.</summary>
	public string AttackAction = "attack";

	/// <summary>Estado de animación al recuperarse tras capturar.</summary>
	public string IdleState = "idle";

	private enum Phase
	{
		Chase,
		Attack,
		Recover,
	}

	private Phase _phase;
	private Node3D _target;
	private float _recoverTimer;
	private float _attackTimer;

	public float Score(Animal animal)
	{
		if (_phase != Phase.Chase)
			return HuntWeight;

		Node3D prey = animal.Context.NearestPrey;
		if (prey == null)
			return 0f;

		float dist = AnimalContext.HorizontalDistance(animal.GlobalPosition, prey.GlobalPosition);
		return HuntWeight * ScoringUtils.Falloff(dist, 0f, animal.Context.PreyDetectRange);
	}

	public void Enter(Animal animal)
	{
		_phase = Phase.Chase;
		_recoverTimer = 0f;
		_target = animal.Context.NearestPrey;
		(animal as IAnimated)?.PlayState(HuntState);
	}

	public void Tick(Animal animal, double delta)
	{
		switch (_phase)
		{
			case Phase.Chase:
				TickChase(animal);
				break;
			case Phase.Attack:
				TickAttack(animal, delta);
				break;
			case Phase.Recover:
				TickRecover(animal, delta);
				break;
		}
	}

	/// <summary>Acecho: camina hacia la presa; a corta distancia inicia el ataque.</summary>
	private void TickChase(Animal animal)
	{
		animal.Locomotion.SpeedScale = HuntSpeedScale;

		if (!GodotObject.IsInstanceValid(_target))
			_target = animal.Context.NearestPrey;
		if (_target == null)
			return;

		Vector3 preyPos = _target.GlobalPosition;
		animal.Locomotion.SetTarget(preyPos);

		if (AnimalContext.HorizontalDistance(animal.GlobalPosition, preyPos) <= AttackRange)
			EnterAttack(animal);
	}

	/// <summary>Lanza el ataque one-shot hacia la presa.</summary>
	private void EnterAttack(Animal animal)
	{
		_phase = Phase.Attack;
		_attackTimer = 0f;
		animal.Locomotion.SpeedScale = 1f;
		(animal as IAnimated)?.PlayAction(AttackAction);
	}

	/// <summary>Ataque: espera a que termine el clip (o venza el timeout); captura y se recupera.</summary>
	private void TickAttack(Animal animal, double delta)
	{
		_attackTimer += (float)delta;
		IAnimated anim = animal as IAnimated;
		bool finished = anim == null || anim.ActionFinished;
		if (!finished && _attackTimer < AttackMaxDuration)
			return;

		Capture();
		_phase = Phase.Recover;
		_recoverTimer = RecoverTime;
		(animal as IAnimated)?.PlayState(IdleState);
		animal.Locomotion.Stop();
	}

	/// <summary>Recuperación: se queda quieto un instante y luego libera el control.</summary>
	private void TickRecover(Animal animal, double delta)
	{
		animal.Locomotion.SpeedScale = 1f;
		_recoverTimer -= (float)delta;

		if (_recoverTimer <= 0f)
		{
			_phase = Phase.Chase;
			_target = null;
		}
	}

	/// <summary>Elimina la presa capturada (si sigue viva) del árbol de escena.</summary>
	private void Capture()
	{
		if (GodotObject.IsInstanceValid(_target))
			_target.QueueFree();
		_target = null;
	}
}
