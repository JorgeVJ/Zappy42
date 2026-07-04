using Godot;

/// <summary>
/// Base común de las regiones/dominios construidos sobre un heightmap: posee el mapa de
/// alturas y su rejilla, y aporta el muestreo de altura (por tile y bilineal), las
/// comprobaciones de límites y el muestreo de destinos en anillo que compartían las
/// implementaciones concretas (acuática, aérea, terrestre y de vegetación).
/// </summary>
/// <remarks>
/// Solo usa tipos de Godot y primitivas, sin referenciar ningún tipo específico de este
/// proyecto, para que este módulo se pueda copiar entero a otro proyecto Godot.
/// </remarks>
public abstract class HeightField
{
	/// <summary>Selecciona un candidato de anillo: valida la columna (x,z) y, si sirve, produce el punto final.</summary>
	protected delegate bool TrySelect(float x, float z, out Vector3 result);

	protected readonly float[,] _heightMap;
	protected readonly HeightMapGrid _grid;

	/// <summary>Nº de intentos al muestrear un destino en anillo antes de rendirse.</summary>
	protected const int SampleAttempts = 12;

	protected HeightField(float[,] heightMap, HeightMapGrid grid)
	{
		_heightMap = heightMap;
		_grid = grid;
	}

	/// <summary>Ancho total del mapa en unidades de mundo.</summary>
	protected float WorldWidth => _grid.Width * _grid.TileSize;

	/// <summary>Alto total del mapa en unidades de mundo.</summary>
	protected float WorldHeight => _grid.Height * _grid.TileSize;

	/// <summary>
	/// Altura del centro del tile que contiene la columna X/Z (promedio de sus 2 esquinas
	/// diagonales, igual que Terrain.GetTileHeight). Devuelve false si la columna cae fuera del mapa.
	/// </summary>
	protected bool TryTileHeight(float worldX, float worldZ, out float height)
	{
		height = 0f;
		int tx = Mathf.FloorToInt(worldX / _grid.TileSize);
		int ty = Mathf.FloorToInt(worldZ / _grid.TileSize);
		if (tx < 0 || tx >= _grid.Width || ty < 0 || ty >= _grid.Height)
			return false;

		height = (_heightMap[tx + 1, ty] + _heightMap[tx, ty + 1]) / 2f;
		return true;
	}

	/// <summary>¿La columna X/Z está dentro de los límites horizontales del mapa?</summary>
	protected bool InBounds(float worldX, float worldZ)
	{
		return worldX >= 0f && worldX <= WorldWidth
			&& worldZ >= 0f && worldZ <= WorldHeight;
	}

	/// <summary>Recorta la columna X/Z de un punto a los límites horizontales del mapa.</summary>
	protected Vector3 ClampXZ(Vector3 worldPos)
	{
		worldPos.X = Mathf.Clamp(worldPos.X, 0f, WorldWidth);
		worldPos.Z = Mathf.Clamp(worldPos.Z, 0f, WorldHeight);
		return worldPos;
	}

	/// <summary>
	/// Altura del terreno en una posición de mundo arbitraria, por interpolación bilineal de
	/// las 4 esquinas del heightMap que rodean la celda.
	/// </summary>
	protected float SampleHeight(float worldX, float worldZ)
	{
		float gx = worldX / _grid.TileSize;
		float gz = worldZ / _grid.TileSize;

		int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, _grid.Width - 1);
		int z0 = Mathf.Clamp(Mathf.FloorToInt(gz), 0, _grid.Height - 1);
		int x1 = Mathf.Min(x0 + 1, _grid.Width);
		int z1 = Mathf.Min(z0 + 1, _grid.Height);

		float fx = Mathf.Clamp(gx - x0, 0f, 1f);
		float fz = Mathf.Clamp(gz - z0, 0f, 1f);

		float top = Mathf.Lerp(_heightMap[x0, z0], _heightMap[x1, z0], fx);
		float bottom = Mathf.Lerp(_heightMap[x0, z1], _heightMap[x1, z1], fx);
		return Mathf.Lerp(top, bottom, fz);
	}

	/// <summary>
	/// Muestrea hasta <see cref="SampleAttempts"/> candidatos en un anillo alrededor de
	/// <paramref name="from"/> y devuelve el primero que <paramref name="select"/> acepta;
	/// si ninguno vale, devuelve <paramref name="from"/> (el animal se queda quieto).
	/// </summary>
	protected Vector3 SampleRing(Vector3 from, float radius, RandomNumberGenerator rng, TrySelect select)
	{
		for (int i = 0; i < SampleAttempts; i++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float dist = rng.RandfRange(0.2f * radius, radius);
			float x = from.X + Mathf.Cos(angle) * dist;
			float z = from.Z + Mathf.Sin(angle) * dist;
			if (select(x, z, out Vector3 result))
				return result;
		}

		return from;
	}
}
