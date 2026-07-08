using Godot;

/// <summary>
/// Comportamiento de vuelo de un ave, como pequeña máquina de estados: al despegar
/// (cámara cerca) pasea por el aire durante un tiempo mínimo (dwell) aunque la cámara
/// se aleje, y solo entonces inicia el aterrizaje —desciende planeando hacia un punto
/// de tierra— tocando suelo antes de ceder el paso a caminar. Si la cámara vuelve a
/// acercarse durante el descenso, reanuda el vuelo. No huye activamente: solo alza el
/// vuelo, pasea y planea de vuelta.
/// </summary>
/// <remarks>
/// Agnóstico de especie: opera sobre <see cref="Animal"/>, su <see cref="IAnimalDomain"/> (aire con
/// SampleWanderTarget, suelo con SampleSurfaceTarget/IsAtSurface) y la capacidad <see cref="IAnimated"/>
/// (estados "fly"/"walk"); no castea a Bird. La cámara la lee cacheada del blackboard.
/// </remarks>
public class FlyBehavior : IUtilityBehavior<Animal>
{
	/// <summary>A esta distancia (o menos) de la cámara, vuelo asegurado.</summary>
	public float FlyInner = 4f;

	/// <summary>A esta distancia (o más) de la cámara, ya no despega.</summary>
	public float FlyOuter = 10f;

	/// <summary>Peso del vuelo (mayor que WalkWeight para ganar al estar cerca la cámara).</summary>
	public float FlyWeight = 3f;

	/// <summary>Cuánto acelera el desplazamiento al volar en crucero respecto a caminar.</summary>
	public float FlySpeedScale = 2.5f;

	/// <summary>Velocidad del descenso al aterrizar (más calmada que el crucero).</summary>
	public float LandingSpeedScale = 1.5f;

	public float WanderRadius = 8f;

	/// <summary>Ventana de vuelo mínima tras despegar, aunque la cámara ya se haya alejado.</summary>
	public float FlyDwellMin = 3f;

	public float FlyDwellMax = 7f;

	/// <summary>Distancia a la superficie (sobre tierra) por debajo de la cual se considera "tocado suelo".</summary>
	public float LandThreshold = 0.6f;

	private float _elapsed;
	private float _dwell;
	private bool _landing;
	private bool _landed = true;

	public float Score(Animal animal)
	{
		float camScore = CameraScore(animal);
		if (_landed)
			return camScore;
		return Mathf.Max(camScore, FlyWeight);
	}

	public void Enter(Animal animal)
	{
		_elapsed = 0f;
		_landing = false;
		_landed = false;
		_dwell = animal.Rng.RandfRange(FlyDwellMin, FlyDwellMax);
		(animal as IAnimated)?.PlayState("fly");
		PickAirTarget(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		if (_landed)
			return;

		_elapsed += (float)delta;
		bool cameraClose = CameraScore(animal) > 0f;

		if (_landing)
			TickLanding(animal, cameraClose);
		else
			TickCruise(animal, cameraClose);
	}

	/// <summary>Crucero: pasea por el aire; pasado el dwell y con la cámara lejos, inicia el aterrizaje.</summary>
	private void TickCruise(Animal animal, bool cameraClose)
	{
		animal.Locomotion.SpeedScale = FlySpeedScale;

		if (!cameraClose && _elapsed >= _dwell)
		{
			_landing = true;
			PickGroundTarget(animal);
			return;
		}

		if (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget)
			PickAirTarget(animal);
	}

	/// <summary>Aterrizaje: desciende hacia un punto de tierra; aborta si vuelve la cámara, toca suelo si llega.</summary>
	private void TickLanding(Animal animal, bool cameraClose)
	{
		animal.Locomotion.SpeedScale = LandingSpeedScale;

		if (cameraClose)
		{
			_landing = false;
			PickAirTarget(animal);
			return;
		}

		if (animal.Domain != null && animal.Domain.IsAtSurface(animal.GlobalPosition, LandThreshold))
		{
			(animal as IAnimated)?.PlayState("walk");
			_landed = true;
			return;
		}

		if (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget)
			PickGroundTarget(animal);
	}

	private float CameraScore(Animal animal)
	{
		return FlyWeight * AnimalScoring.CameraFalloff(animal, FlyInner, FlyOuter);
	}

	private void PickAirTarget(Animal animal)
	{
		if (animal.Domain == null)
			return;

		Vector3 target = animal.Domain.SampleWanderTarget(animal.GlobalPosition, WanderRadius, animal.Rng);
		animal.Locomotion.SetTarget(target);
	}

	private void PickGroundTarget(Animal animal)
	{
		if (animal.Domain == null)
			return;

		Vector3 target = animal.Domain.SampleSurfaceTarget(animal.GlobalPosition, WanderRadius, animal.Rng);
		animal.Locomotion.SetTarget(target);
	}
}
