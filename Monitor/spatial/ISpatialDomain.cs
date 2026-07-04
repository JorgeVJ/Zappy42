using Godot;

/// <summary>
/// Región del mundo dentro de la cual un punto se considera válido. Es la parte mínima
/// y reutilizable de <see cref="IAnimalDomain"/>: cualquier sistema que necesite decidir
/// si una posición cae dentro de una zona (agua, tierra, etc.) puede consumir esta
/// interfaz sin arrastrar los conceptos de locomoción animal (clamping, paseo).
/// </summary>
/// <remarks>
/// Solo usa tipos de Godot, sin referenciar ningún tipo específico de este proyecto,
/// para que este módulo se pueda copiar entero a otro proyecto Godot.
/// </remarks>
public interface ISpatialDomain
{
	/// <summary>¿Es este punto del mundo un sitio válido dentro del dominio?</summary>
	bool Contains(Vector3 worldPos);
}
