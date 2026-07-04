using Godot;

/// <summary>
/// Comportamiento de paseo ambiental: cicla entre "gaits" (quieto/caminar/correr, o nadar/pausar)
/// con temporizadores de permanencia (dwell) aleatorios — un FSM diminuto y explícito, no un
/// oscilador disfrazado de utilidad. En cada gait fija la velocidad, pide a la entidad la animación
/// correspondiente (<see cref="IAnimated"/>) y, si el gait se mueve, elige destinos del dominio.
/// Su Score es un baseline constante: el estado por defecto que el cerebro usa cuando ningún impulso
/// reactivo (huir, volar, cazar) gana. Sustituye a los antiguos Wander/Walk/FoxState.
/// </summary>
public class AmbientLocomotionBehavior : IUtilityBehavior<Animal>
{
	/// <summary>Un modo de locomoción ambiental: su animación, velocidad, si se mueve y cuánto dura (dwell).</summary>
	public struct Gait
	{
		public string State;
		public float SpeedScale;
		public bool Moves;
		public float DwellMin;
		public float DwellMax;
	}

	/// <summary>Peso base (estado por defecto). Otros comportamientos ganan cuando su Score lo supera.</summary>
	public float Weight = 1f;

	/// <summary>Radio de los saltos de paseo.</summary>
	public float WanderRadius = 6f;

	/// <summary>Destinos a ras de superficie (aves que caminan) en vez del volumen del dominio (peces).</summary>
	public bool UseSurface;

	private readonly Gait[] _gaits;
	private int _index = -1;
	private float _dwellTimer;

	public AmbientLocomotionBehavior(Gait[] gaits)
	{
		_gaits = gaits ?? System.Array.Empty<Gait>();
	}

	/// <summary>Constructor por defecto: un único gait de paseo, para servir de comportamiento base.</summary>
	public AmbientLocomotionBehavior()
		: this(new Gait[] { new Gait { State = "walk", SpeedScale = 1f, Moves = true, DwellMin = 2f, DwellMax = 5f } })
	{
	}

	public float Score(Animal animal) => Weight;

	public void Enter(Animal animal)
	{
		_index = -1;
		NextGait(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		if (_gaits.Length == 0)
			return;

		_dwellTimer -= (float)delta;
		Gait gait = _gaits[_index];
		animal.Locomotion.SpeedScale = gait.SpeedScale;

		if (_dwellTimer <= 0f)
			NextGait(animal);
		else if (gait.Moves && (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget))
			PickTarget(animal);
	}

	/// <summary>Elige un gait (evitando repetir si hay varios), lo activa, anima y reinicia su dwell.</summary>
	private void NextGait(Animal animal)
	{
		if (_gaits.Length == 0)
			return;

		_index = PickGaitIndex(animal);
		Gait gait = _gaits[_index];
		_dwellTimer = animal.Rng.RandfRange(gait.DwellMin, gait.DwellMax);

		(animal as IAnimated)?.PlayState(gait.State);

		if (gait.Moves)
			PickTarget(animal);
		else
			animal.Locomotion.Stop();
	}

	private int PickGaitIndex(Animal animal)
	{
		if (_gaits.Length == 1)
			return 0;

		int next = animal.Rng.RandiRange(0, _gaits.Length - 1);
		if (next == _index)
			next = (next + 1) % _gaits.Length;
		return next;
	}

	private void PickTarget(Animal animal)
	{
		if (animal.Domain == null)
			return;

		Vector3 from = animal.GlobalPosition;
		Vector3 target = UseSurface
			? animal.Domain.SampleSurfaceTarget(from, WanderRadius, animal.Rng)
			: animal.Domain.SampleWanderTarget(from, WanderRadius, animal.Rng);
		animal.Locomotion.SetTarget(target);
	}
}
