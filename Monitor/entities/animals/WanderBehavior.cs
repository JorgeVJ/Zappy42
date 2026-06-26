using Godot;

// Comportamiento de paseo: elige destinos cercanos dentro del dominio navegable y
// deja que la locomoción lleve al animal hasta ellos, con pausas ocasionales para
// que el movimiento sea orgánico. Único comportamiento por ahora; en el futuro será
// uno más entre los que elija el UtilityBrain.
public class WanderBehavior : IAnimalBehavior
{
	public float WanderRadius = 6f;
	public float PauseChance = 0.35f;   // probabilidad de pausar al llegar a un destino
	public float PauseMin = 1.0f;
	public float PauseMax = 3.5f;

	// Puntuación base: es el "estado por defecto". Cualquier otro comportamiento
	// (huir, etc.) gana cuando su Score supera este baseline.
	public float WanderWeight = 1.0f;

	private float _pauseTimer;

	// Pasear siempre es viable: utilidad constante baja que sirve de suelo.
	public float Score(Animal animal) => WanderWeight;

	public void Enter(Animal animal)
	{
		_pauseTimer = 0f;
		PickNewTarget(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		// Velocidad de crucero normal mientras pasea.
		animal.Locomotion.SpeedScale = 1f;

		if (_pauseTimer > 0f)
		{
			_pauseTimer -= (float)delta;
			return;
		}

		// Al llegar (o si por algún motivo no hay objetivo), decidir: pausar o seguir.
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
