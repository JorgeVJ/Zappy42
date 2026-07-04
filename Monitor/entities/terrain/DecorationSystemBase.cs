using Godot;

/// <summary>
/// Base común para los sistemas que pueblan el terreno (decoraciones estáticas y
/// hierba dinámica), evitando que la vegetación aparezca sobre zonas sumergidas.
/// </summary>
public abstract partial class DecorationSystemBase : Node3D
{
	/// <summary>
	/// Mismo cálculo de nivel del mar que WaterSystem (fracción entre min y max
	/// del heightMap). Se duplica en vez de referenciar WaterSystem para que este
	/// sistema no dependa de ningún otro nodo del árbol de escena.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float SeaLevelFraction = 0.35f;

	[Export]
	public float SeaLevelOffset = 0f;

	/// <summary>
	/// Distancia por encima del nivel del mar que debe tener un tile para contar
	/// como tierra firme, de forma que la vegetación no nazca pegada a la orilla.
	/// </summary>
	[Export]
	public float ShoreMargin = 0.3f;

	protected TerrainDomain BuildLandDomain(float[,] heightMap, HeightMapGrid grid)
	{
		return new TerrainDomain(heightMap, grid, ComputeSeaThreshold(heightMap), ShoreMargin);
	}

	protected SmoothTerrainDomain BuildSmoothLandDomain(float[,] heightMap, HeightMapGrid grid)
	{
		return new SmoothTerrainDomain(heightMap, grid, ComputeSeaThreshold(heightMap), ShoreMargin);
	}

	private float ComputeSeaThreshold(float[,] heightMap)
	{
		return ComputeSeaLevel(heightMap, SeaLevelFraction) + SeaLevelOffset;
	}

	private static float ComputeSeaLevel(float[,] heightMap, float fraction)
	{
		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (float h in heightMap)
		{
			if (h < min) min = h;
			if (h > max) max = h;
		}

		return Mathf.Lerp(min, max, fraction);
	}
}
