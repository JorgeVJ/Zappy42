// Un comportamiento de animal: decide a dónde ir / qué hacer a lo largo del tiempo,
// actuando sobre la locomoción y la animación del animal. Es la "costura" para el
// futuro sistema de decisiones de Utility: hoy cada animal corre un único
// comportamiento (WanderBehavior); mañana un UtilityBrain elegirá entre varios
// según un Score (volar/posarse/cazar, pasear/saltar/huir/comer, etc.).
public interface IAnimalBehavior
{
	// Se llama una vez al activar el comportamiento (reinicia su estado interno).
	void Enter(Animal animal);

	// Se llama cada frame mientras el comportamiento está activo.
	void Tick(Animal animal, double delta);

	// Futuro Utility AI: el cerebro elegirá el comportamiento de mayor Score.
	// float Score(Animal animal);
}
