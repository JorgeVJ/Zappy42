using Godot;

/// <summary>
/// Comportamiento de paseo: elige destinos cercanos dentro del dominio navegable y
/// deja que la locomoción lleve al animal hasta ellos, con pausas ocasionales para
/// que el movimiento sea orgánico. Es el comportamiento por defecto entre los que
/// elige el UtilityBrain.
/// </summary>
public class WanderBehavior : IUtilityBehavior<Animal>
{
	public float WanderRadius = 6f;

	/// <summary>Probabilidad de pausar al llegar a un destino.</summary>
	public float PauseChance = 0.35f;

	public float PauseMin = 1.0f;
	public float PauseMax = 3.5f;

	/// <summary>
	/// Puntuación base: es el "estado por defecto". Cualquier otro comportamiento
	/// (huir, etc.) gana cuando su Score supera este baseline.
	/// </summary>
	public float WanderWeight = 1.0f;

	private float _pauseTimer;

	/// <summary>Pasear siempre es viable: utilidad constante baja que sirve de suelo.</summary>
	public float Score(Animal animal) => WanderWeight;

	public void Enter(Animal animal)
	{
		_pauseTimer = 0f;
		PickNewTarget(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		animal.Locomotion.SpeedScale = 1f;

		if (_pauseTimer > 0f)
		{
			_pauseTimer -= (float)delta;
			return;
		}

		if (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget)
		{
			if (animal.Rng.Randf() < PauseChance)
				_pauseTimer = animal.Rng.RandfRange(PauseMin, PauseMax);
			else
				PickNewTarget(animal);
		}
	}

	private void PickNewTarget(Animal animal)
	{
		Vector3 target = animal.Domain.SampleWanderTarget(
			animal.GlobalPosition, WanderRadius, animal.Rng);
		animal.Locomotion.SetTarget(target);
	}
}
