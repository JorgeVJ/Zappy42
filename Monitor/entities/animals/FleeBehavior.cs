using Godot;

// Comportamiento de huida de la cámara: cuando la cámara se acerca, el pez acelera
// el nado y se aleja en dirección opuesta. Su Score crece al estar cerca y cae a 0
// al alejarse la cámara, momento en que el cerebro vuelve a pasear.
//
// Consulta la cámara con API de Godot (GetViewport().GetCamera3D()), no con tipos
// del proyecto → mantiene la portabilidad del sistema de animales.
public class FleeBehavior : IAnimalBehavior
{
	public float FleeInner = 1f;        // a esta distancia (o menos) de la cámara, huida máxima
	public float FleeOuter = 6f;       // a esta distancia (o más), ya no huye
	public float FleeWeight = 3f;       // peso de la huida (> WanderWeight para ganar al estar cerca)
	public float FleeSpeedScale = 4.2f; // cuánto acelera el nado al huir
	public float FleeStep = 5f;         // longitud del salto de huida

	private const int DirectionAttempts = 6;

	public float Score(Animal animal)
	{
		Camera3D cam = GetCamera(animal);
		if (cam == null)
			return 0f;
		float dist = animal.GlobalPosition.DistanceTo(cam.GlobalPosition);
		GD.Print("ScoreIn");
        GD.Print(dist);
        GD.Print(FleeInner);
        GD.Print(FleeOuter);
		GD.Print("ScoreOut");
        float closeness = ScoringUtils.Falloff(dist, FleeInner, FleeOuter);
		return FleeWeight * closeness;
	}

	public void Enter(Animal animal)
	{
		GD.Print("FleeBehavior Enter");
		PickFleeTarget(animal);
    }

	public void Tick(Animal animal, double delta)
	{
		// Nado acelerado mientras huye (la cola se acelera sola vía OnLocomotionUpdate).
		animal.Locomotion.SpeedScale = FleeSpeedScale;

		// Re-encadena destinos sin pausas: huir es continuo y persigue alejarse de la
		// cámara, que además se mueve.
		if (animal.Locomotion.Arrived || !animal.Locomotion.HasTarget)
			PickFleeTarget(animal);
	}

	private void PickFleeTarget(Animal animal)
	{
		Camera3D cam = GetCamera(animal);
		Vector3 pos = animal.GlobalPosition;

		if (cam == null)
		{
			// Sin cámara no hay de qué huir: un salto cualquiera dentro del dominio.
			animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
			return;
		}

		Vector3 away = pos - cam.GlobalPosition;
		away.Y = 0f; // dirección de huida en el plano; el dominio ajusta la profundidad
		if (away.LengthSquared() < 0.0001f)
			away = new Vector3(animal.Rng.Randf() - 0.5f, 0f, animal.Rng.Randf() - 0.5f);
		away = away.Normalized();

		// Intenta varios candidatos en el hemisferio de huida, girando si el directo
		// cae fuera del agua; se queda con el primero válido.
		for (int i = 0; i < DirectionAttempts; i++)
		{
			float yaw = (i == 0) ? 0f : Mathf.DegToRad(25f * ((i + 1) / 2) * (i % 2 == 0 ? 1 : -1));
			Vector3 dir = away.Rotated(Vector3.Up, yaw);
			Vector3 candidate = animal.Domain.ClampToValid(pos + dir * FleeStep);
			if (animal.Domain.Contains(candidate))
			{
				animal.Locomotion.SetTarget(candidate);
				return;
			}
		}

		// Fallback: cualquier destino válido cercano (sigue moviéndose).
		animal.Locomotion.SetTarget(animal.Domain.SampleWanderTarget(pos, FleeStep, animal.Rng));
	}

	private static Camera3D GetCamera(Animal animal)
	{
		return animal.GetViewport()?.GetCamera3D();
	}
}
