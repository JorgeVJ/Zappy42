using Godot;

/// <summary>
/// Comportamiento de huida de la cámara: cuando la cámara se acerca, el pez acelera
/// el nado y se aleja en dirección opuesta. Su Score crece al estar cerca y cae a 0
/// al alejarse la cámara, momento en que el cerebro vuelve a pasear.
/// </summary>
/// <remarks>
/// Lee la cámara ya cacheada en el blackboard (<see cref="AnimalContext"/>), no la consulta
/// por su cuenta, para no re-percibir en cada frame y mantener la portabilidad del sistema.
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
		return FleeWeight * AnimalScoring.CameraFalloff(animal, FleeInner, FleeOuter);
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
		Vector3 pos = animal.GlobalPosition;

		if (!animal.Context.HasCamera)
		{
			animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
			return;
		}

		Vector3 away = ComputeFleeDirection(animal, pos, animal.Context.CameraPosition);
		if (TryPickDirectionalTarget(animal, pos, away))
			return;

		animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
	}

	/// <summary>
	/// Dirección de huida en el plano horizontal, opuesta a la cámara; si coincide con
	/// su posición, elige una dirección aleatoria en su lugar.
	/// </summary>
	private static Vector3 ComputeFleeDirection(Animal animal, Vector3 pos, Vector3 cameraPos)
	{
		Vector3 away = pos - cameraPos;
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
}
