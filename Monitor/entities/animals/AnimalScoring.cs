using Godot;

/// <summary>
/// Utilidades de scoring específicas de animales para el Utility AI. Complementa a
/// ScoringUtils (curvas genéricas) con ayudas que necesitan el contexto del animal,
/// como la proximidad de la cámara, sin acoplar el framework portable a Animal.
/// </summary>
/// <remarks>
/// Consulta la cámara con API de Godot (GetViewport().GetCamera3D()), no con tipos del
/// proyecto, para mantener la portabilidad del sistema de animales.
/// </remarks>
public static class AnimalScoring
{
	/// <summary>
	/// Cercanía de la cámara al animal en [0,1]: 1 a <paramref name="inner"/> o menos, 0 a
	/// <paramref name="outer"/> o más. Devuelve 0 si no hay cámara activa. Base de los Score
	/// de huida (peces) y despegue (aves). Lee la distancia ya cacheada en el blackboard.
	/// </summary>
	public static float CameraFalloff(Animal animal, float inner, float outer)
	{
		return ScoringUtils.Falloff(animal.Context.CameraDistance, inner, outer);
	}
}
