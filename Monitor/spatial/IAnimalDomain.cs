using Godot;

/// <summary>
/// Abstracción de "dónde puede moverse un animal": el espacio navegable. Cada tipo
/// de animal (acuático, terrestre, aéreo) tiene un dominio concreto que responde si
/// un punto es válido, lo proyecta de vuelta a la región y propone destinos cercanos
/// alcanzables.
/// </summary>
/// <remarks>
/// Solo usa tipos de Godot, sin referenciar ningún tipo específico de este proyecto,
/// para que este módulo se pueda copiar entero a otro proyecto Godot.
/// </remarks>
public interface IAnimalDomain : ISpatialDomain
{
	/// <summary>
	/// Proyecta un punto (posiblemente fuera) de vuelta al interior de la región.
	/// Usado cada frame por la locomoción para que el animal no salga del dominio.
	/// </summary>
	Vector3 ClampToValid(Vector3 worldPos);

	/// <summary>
	/// Propone un destino cercano y alcanzable para pasear, dentro de <paramref name="radius"/>
	/// desde <paramref name="from"/>. Devuelve <paramref name="from"/> si no encuentra
	/// ninguno (el animal se queda quieto).
	/// </summary>
	Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng);
}
