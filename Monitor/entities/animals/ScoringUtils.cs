using Godot;

/// <summary>
/// Curvas de respuesta para el Utility AI de los animales: convierten magnitudes
/// crudas (distancias, conteos) en features normalizadas que los comportamientos
/// combinan con pesos para producir su Score.
/// </summary>
/// <remarks>
/// Solo usa math de Godot, por lo que es autocontenido y portable.
/// </remarks>
public static class ScoringUtils
{
	/// <summary>Mapea linealmente [min, max] a [0, 1], con clamp.</summary>
	public static float Normalize(float value, float min, float max)
	{
		if (max <= min)
			return 0f;
		return Mathf.Clamp((value - min) / (max - min), 0f, 1f);
	}

	/// <summary>Decae con la distancia: 1/(1 + dist·k). dist=0 da 1; crece la distancia hacia 0.</summary>
	public static float Proximity(float distance, float k = 0.5f)
	{
		return 1f / (1f + Mathf.Max(0f, distance) * k);
	}

	/// <summary>1 si dist es menor o igual a inner, 0 si dist es mayor o igual a outer, lineal en medio.</summary>
	public static float Falloff(float distance, float inner, float outer)
	{
		return 1f - Normalize(distance, inner, outer);
	}
}
