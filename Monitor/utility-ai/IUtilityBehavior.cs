/// <summary>
/// Un comportamiento de Utility AI: decide a qué dedicar al agente a lo largo del
/// tiempo. Es la "costura" del sistema de decisiones: cada agente corre un único
/// comportamiento activo, y un UtilityBrain elige entre varios candidatos según su
/// Score. No depende de ningún tipo concreto de agente (animal, humano, objeto
/// animado...): el genérico TAgent es el que decide sobre qué actúa.
/// </summary>
public interface IUtilityBehavior<TAgent>
{
	/// <summary>Se llama una vez al activar el comportamiento (reinicia su estado interno).</summary>
	void Enter(TAgent agent);

	/// <summary>Se llama cada frame mientras el comportamiento está activo.</summary>
	void Tick(TAgent agent, double delta);

	/// <summary>
	/// Utilidad actual de este comportamiento: el UtilityBrain elige el de mayor
	/// Score. Mayor = más deseable; cercano a 0 = irrelevante ahora mismo.
	/// </summary>
	float Score(TAgent agent);
}
