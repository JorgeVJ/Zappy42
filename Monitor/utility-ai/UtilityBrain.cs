/// <summary>
/// Cerebro de Utility AI: elige, entre varios comportamientos candidatos, el de mayor
/// Score y lo ejecuta; reevalúa periódicamente y conmuta cuando otro pasa a ser más
/// deseable. Es a su vez un IUtilityBehavior (patrón compuesto), de modo que el agente
/// sigue corriendo un único Behavior sin cambios en su bucle.
/// </summary>
public class UtilityBrain<TAgent> : IUtilityBehavior<TAgent>
{
	/// <summary>Cada cuántos segundos se reevalúan los scores. Entre evaluaciones corre el activo.</summary>
	public float EvalInterval = 0.5f;

	/// <summary>
	/// Histéresis: el candidato debe superar al activo por este margen para conmutar,
	/// evitando parpadeo cuando dos scores quedan casi empatados en el umbral.
	/// </summary>
	public float SwitchMargin = 0.15f;

	private readonly IUtilityBehavior<TAgent>[] _behaviors;
	private IUtilityBehavior<TAgent> _active;
	private float _evalTimer;

	public IUtilityBehavior<TAgent> Active => _active;

	public UtilityBrain(IUtilityBehavior<TAgent>[] behaviors)
	{
		_behaviors = behaviors;
	}

	public void Enter(TAgent agent)
	{
		_evalTimer = 0f;
		_active = BestBehavior(agent, out _);
		_active?.Enter(agent);
	}

	public void Tick(TAgent agent, double delta)
	{
		_evalTimer -= (float)delta;
		if (_evalTimer <= 0f)
		{
			_evalTimer = EvalInterval;
			Reevaluate(agent);
		}

		_active?.Tick(agent, delta);
	}

	/// <summary>El cerebro vale lo que su mejor opción (permite anidar cerebros sin romper nada).</summary>
	public float Score(TAgent agent)
	{
		BestBehavior(agent, out float best);
		return best;
	}

	private void Reevaluate(TAgent agent)
	{
		IUtilityBehavior<TAgent> best = BestBehavior(agent, out float bestScore);
		if (best == null || best == _active)
			return;

		float activeScore = _active?.Score(agent) ?? float.NegativeInfinity;
		if (bestScore > activeScore + SwitchMargin)
		{
			_active = best;
			_active.Enter(agent);
		}
	}

	private IUtilityBehavior<TAgent> BestBehavior(TAgent agent, out float bestScore)
	{
		IUtilityBehavior<TAgent> best = null;
		bestScore = float.NegativeInfinity;

		foreach (IUtilityBehavior<TAgent> b in _behaviors)
		{
			float s = b.Score(agent);
			if (s > bestScore)
			{
				bestScore = s;
				best = b;
			}
		}

		return best;
	}
}
