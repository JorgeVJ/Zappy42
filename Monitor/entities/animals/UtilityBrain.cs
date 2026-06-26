// Cerebro de Utility AI: elige, entre varios comportamientos candidatos, el de mayor
// Score y lo ejecuta; reevalúa periódicamente y conmuta cuando otro pasa a ser más
// deseable. Es a su vez un IAnimalBehavior (patrón compuesto), de modo que Animal
// sigue corriendo un único Behavior sin cambios en su bucle.
//
// Espejo del "decider" del proyecto de referencia (UtilityDecider.Decide: puntuar
// todo y quedarse con el máximo), adaptado a comportamientos con estado (Enter/Tick).
public class UtilityBrain : IAnimalBehavior
{
	// Cada cuánto se reevalúan los scores (s). Entre evaluaciones corre el activo.
	public float EvalInterval = 0.5f;

	// Histéresis: el candidato debe superar al activo por este margen para conmutar,
	// evitando parpadeo cuando dos scores quedan casi empatados en el umbral.
	public float SwitchMargin = 0.15f;

	private readonly IAnimalBehavior[] _behaviors;
	private IAnimalBehavior _active;
	private float _evalTimer;

	public IAnimalBehavior Active => _active;

	public UtilityBrain(IAnimalBehavior[] behaviors)
	{
		_behaviors = behaviors;
	}

	public void Enter(Animal animal)
	{
		_evalTimer = 0f;
		_active = BestBehavior(animal, out _);
		_active?.Enter(animal);
	}

	public void Tick(Animal animal, double delta)
	{
		_evalTimer -= (float)delta;
		if (_evalTimer <= 0f)
		{
			_evalTimer = EvalInterval;
			Reevaluate(animal);
		}

		_active?.Tick(animal, delta);
	}

	// El cerebro vale lo que su mejor opción (permite anidar cerebros sin romper nada).
	public float Score(Animal animal)
	{
		BestBehavior(animal, out float best);
		return best;
	}

	private void Reevaluate(Animal animal)
	{
		IAnimalBehavior best = BestBehavior(animal, out float bestScore);
		if (best == null || best == _active)
			return;

		float activeScore = _active?.Score(animal) ?? float.NegativeInfinity;
		if (bestScore > activeScore + SwitchMargin)
		{
			_active = best;
			_active.Enter(animal);
		}
	}

	private IAnimalBehavior BestBehavior(Animal animal, out float bestScore)
	{
		IAnimalBehavior best = null;
		bestScore = float.NegativeInfinity;

		foreach (IAnimalBehavior b in _behaviors)
		{
			float s = b.Score(animal);
			if (s > bestScore)
			{
				bestScore = s;
				best = b;
			}
		}

		return best;
	}
}
