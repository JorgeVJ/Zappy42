/// <summary>
/// Capacidad de animación de un animal: los comportamientos expresan QUÉ estado o acción
/// quieren ("walk", "fly", "hunt", "attack"…) y la entidad concreta lo traduce a sus clips o
/// huesos, sin que el comportamiento conozca la especie. Así un mismo behavior sirve para
/// cualquier animal que sepa animarse; los que no (p. ej. peces por huesos) simplemente no la implementan.
/// </summary>
public interface IAnimated
{
	/// <summary>Fija el estado de locomoción en bucle (idle/walk/run/fly…). La entidad ignora los que no soporte.</summary>
	void PlayState(string state);

	/// <summary>Lanza una acción puntual one-shot (p. ej. "attack"). No-op si la entidad no la soporta.</summary>
	void PlayAction(string action);

	/// <summary>True cuando la última acción one-shot ha terminado (para encadenar tras un ataque, etc.).</summary>
	bool ActionFinished { get; }
}
