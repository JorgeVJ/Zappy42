using Godot;

/// <summary>
/// Zorro decorativo móvil. Hereda de ClipAnimal (dominio + locomoción + comportamiento +
/// reproducción de clips) y usa el AnimationPlayer del modelo para reproducir sus clips (estar
/// quieto, caminar, correr, acechar y atacar). Es un animal terrestre puro (no vuela): su cerebro
/// pasea alternando animaciones y, si tiene un ave cerca, la acecha (Hunt) y la ataca (Attack,
/// one-shot). El pegado al suelo lo garantiza el dominio terrestre (GroundDomain). Las transiciones
/// entre clips se funden (cross-fade) para que los huesos no salten a la nueva pose.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia tipos del proyecto (solo el dominio terrestre portable).
/// El enum <see cref="Animations"/> se define en el archivo parcial Fox.Animations.cs.
/// </remarks>
public partial class Fox : ClipAnimal, IAnimated
{
	/// <summary>Tiempo de fundido (s) entre clips en bucle, para suavizar el cambio de pose.</summary>
	[Export]
	public float BlendTime = 0.15f;

	/// <summary>Tiempo de fundido (s) más corto al entrar en el ataque, para que el golpe sea nítido.</summary>
	[Export]
	public float AttackBlendTime = 0.06f;

	private bool _attackFinished;

	/// <summary>True cuando el clip de ataque (one-shot) ha terminado desde el último PlayOnce(Attack).</summary>
	public bool AttackFinished => _attackFinished;

	public static Fox Create(Vector3 pos, string modelPath)
	{
		Fox fox = new Fox { Position = pos, ModelPath = modelPath };
		return fox;
	}

	public override void _Ready()
	{
		LoadModelAndPlayer();
		if (Player != null)
		{
			Player.PlaybackDefaultBlendTime = BlendTime;
			Player.AnimationFinished += OnAnimationFinished;
		}

		base._Ready();
	}

	/// <summary>Reproduce en bucle la animación indicada (con fundido), resolviendo su clip como "Fox_&lt;valor&gt;".</summary>
	public void Play(Animations anim)
	{
		PlayClip($"Fox_{anim}", true, BlendTime);
	}

	/// <summary>Reproduce la animación una sola vez (sin bucle, con fundido corto) y arma la detección de fin.</summary>
	public void PlayOnce(Animations anim)
	{
		_attackFinished = false;
		PlayClip($"Fox_{anim}", false, AttackBlendTime);
	}

	/// <summary>IAnimated: resuelve el estado ("idle"/"walk"/"run"/"hunt") al valor del enum y lo reproduce en bucle.</summary>
	public void PlayState(string state)
	{
		if (System.Enum.TryParse(state, true, out Animations anim))
			Play(anim);
	}

	/// <summary>IAnimated: resuelve la acción ("attack") al valor del enum y la reproduce one-shot.</summary>
	public void PlayAction(string action)
	{
		if (System.Enum.TryParse(action, true, out Animations anim))
			PlayOnce(anim);
	}

	/// <summary>IAnimated: expone el fin del clip one-shot (ataque).</summary>
	public bool ActionFinished => AttackFinished;

	/// <summary>Marca el fin del ataque cuando el AnimationPlayer termina el clip one-shot Fox_Attack.</summary>
	private void OnAnimationFinished(StringName name)
	{
		if (name == $"Fox_{Animations.Attack}")
			_attackFinished = true;
	}
}
