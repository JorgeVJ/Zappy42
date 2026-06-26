using Godot;

// Abstracción de "dónde puede moverse un animal": el espacio navegable. Cada tipo
// de animal (acuático, terrestre, aéreo) tiene un dominio concreto que responde si
// un punto es válido, lo proyecta de vuelta a la región y propone destinos cercanos
// alcanzables. Mantener este interfaz agnóstico del proyecto (solo tipos de Godot)
// para conservar la portabilidad del sistema de animales.
public interface IAnimalDomain
{
	// ¿Es este punto del mundo un sitio válido donde el animal puede estar?
	bool Contains(Vector3 worldPos);

	// Proyecta un punto (posiblemente fuera) de vuelta al interior de la región.
	// Usado cada frame por la locomoción para que el animal no salga del dominio.
	Vector3 ClampToValid(Vector3 worldPos);

	// Propone un destino cercano y alcanzable para pasear, dentro de `radius` desde
	// `from`. Devuelve `from` si no encuentra ninguno (el animal se queda quieto).
	Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng);
}
