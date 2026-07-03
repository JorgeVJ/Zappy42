/// <summary>
/// Un comportamiento de animal: decide a dónde ir / qué hacer a lo largo del tiempo,
/// actuando sobre la locomoción y la animación del animal. Es la "costura" del
/// sistema de decisiones de Utility: cada animal corre un único comportamiento activo
/// (por ejemplo WanderBehavior), y un UtilityBrain elige entre varios candidatos según
/// su Score.
/// </summary>
public interface IAnimalBehavior
{
	/// <summary>Se llama una vez al activar el comportamiento (reinicia su estado interno).</summary>
	void Enter(Animal animal);

	/// <summary>Se llama cada frame mientras el comportamiento está activo.</summary>
	void Tick(Animal animal, double delta);

	/// <summary>
	/// Utilidad actual de este comportamiento: el UtilityBrain elige el de mayor
	/// Score. Mayor = más deseable; cercano a 0 = irrelevante ahora mismo.
	/// </summary>
	float Score(Animal animal);
}
