using Godot;

// Curvas de respuesta para el Utility AI de los animales. Espejo de las utilidades
// de scoring del proyecto de referencia (SpringChallenge2026, TrollFarmBot/AI):
// convierten magnitudes crudas (distancias, conteos) en features normalizadas que
// los comportamientos combinan con pesos para producir su Score. Solo math de Godot
// → autocontenido y portable.
public static class ScoringUtils
{
	// Mapea linealmente [min, max] → [0, 1], con clamp.
	public static float Normalize(float value, float min, float max)
	{
		if (max <= min)
			return 0f;
		return Mathf.Clamp((value - min) / (max - min), 0f, 1f);
	}

	// Decae con la distancia: 1/(1 + dist·k). dist=0 → 1; crece la distancia → 0.
	public static float Proximity(float distance, float k = 0.5f)
	{
		return 1f / (1f + Mathf.Max(0f, distance) * k);
	}

	// 1 si dist ≤ inner, 0 si dist ≥ outer, lineal en medio. Útil para "cerca de X".
	public static float Falloff(float distance, float inner, float outer)
	{
		return 1f - Normalize(distance, inner, outer);
	}
}
