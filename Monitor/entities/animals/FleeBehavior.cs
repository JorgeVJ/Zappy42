using Godot;

/// <summary>
/// Comportamiento de huida de la cámara: cuando la cámara se acerca, el pez acelera
/// el nado y se aleja en dirección opuesta. Su Score crece al estar cerca y cae a 0
/// al alejarse la cámara, momento en que el cerebro vuelve a pasear.
/// </summary>
/// <remarks>
/// Consulta la cámara con API de Godot (GetViewport().GetCamera3D()), no con tipos
/// del proyecto, para mantener la portabilidad del sistema de animales.
/// </remarks>
public class FleeBehavior : IUtilityBehavior<Animal>
{
	/// <summary>A esta distancia (o menos) de la cámara, huida máxima.</summary>
	public float FleeInner = 1f;

	/// <summary>A esta distancia (o más) de la cámara, ya no huye.</summary>
	public float FleeOuter = 6f;

	/// <summary>Peso de la huida (mayor que WanderWeight para ganar al estar cerca).</summary>
	public float FleeWeight = 3f;

	/// <summary>Cuánto acelera el nado al huir.</summary>
	public float FleeSpeedScale = 4.2f;

	/// <summary>Longitud del salto de huida.</summary>
	public float FleeStep = 5f;

	private const int DirectionAttempts = 6;

	public float Score(Animal animal)
	{
		Camera3D cam = GetCamera(animal);
		if (cam == null)
			return 0f;
		float dist = animal.GlobalPosition.DistanceTo(cam.GlobalPosition);
		float closeness = ScoringUtils.Falloff(dist, FleeInner, FleeOuter);
		return FleeWeight * closeness;
	}

	public void Enter(Animal animal)
	{
		PickFleeTarget(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		animal.Locomotion.SpeedScale = FleeSpeedScale;

		if (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget)
			PickFleeTarget(animal);
	}

	private void PickFleeTarget(Animal animal)
	{
		Camera3D cam = GetCamera(animal);
		Vector3 pos = animal.GlobalPosition;

		if (cam == null)
		{
			animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
			return;
		}

		Vector3 away = ComputeFleeDirection(animal, pos, cam);
		if (TryPickDirectionalTarget(animal, pos, away))
			return;

		animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
	}

	/// <summary>
	/// Dirección de huida en el plano horizontal, opuesta a la cámara; si coincide con
	/// su posición, elige una dirección aleatoria en su lugar.
	/// </summary>
	private static Vector3 ComputeFleeDirection(Animal animal, Vector3 pos, Camera3D cam)
	{
		Vector3 away = pos - cam.GlobalPosition;
		away.Y = 0f;
		if (away.LengthSquared() < 0.0001f)
			away = new Vector3(animal.Rng.Randf() - 0.5f, 0f, animal.Rng.Randf() - 0.5f);
		return away.Normalized();
	}

	/// <summary>
	/// Intenta varios candidatos en el hemisferio de huida, girando si el directo cae
	/// fuera del agua; se queda con el primero válido y fija el destino en la
	/// locomoción. Devuelve false si ningún candidato es válido.
	/// </summary>
	private bool TryPickDirectionalTarget(Animal animal, Vector3 pos, Vector3 away)
	{
		for (int i = 0; i < DirectionAttempts; i++)
		{
			float yaw = (i == 0) ? 0f : Mathf.DegToRad(25f * ((i + 1) / 2) * (i % 2 == 0 ? 1 : -1));
			Vector3 dir = away.Rotated(Vector3.Up, yaw);
			Vector3 candidate = animal.Domain.ClampToValid(pos + dir * FleeStep);
			if (animal.Domain.Contains(candidate))
			{
				animal.Locomotion.SetTarget(candidate);
				return true;
			}
		}

		return false;
	}

	private static Camera3D GetCamera(Animal animal)
	{
		return animal.GetViewport()?.GetCamera3D();
	}
}
