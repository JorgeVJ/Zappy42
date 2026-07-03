using Godot;

/// <summary>
/// Dimensiones de la rejilla del heightmap: ancho/alto en tiles y el tamaño de
/// cada tile en unidades de mundo.
/// </summary>
public readonly struct HeightMapGrid
{
	public readonly int Width;
	public readonly int Height;
	public readonly float TileSize;

	public HeightMapGrid(int width, int height, float tileSize)
	{
		Width = width;
		Height = height;
		TileSize = tileSize;
	}
}

/// <summary>
/// Márgenes que un dominio navegable deja respecto al fondo y a la superficie
/// del volumen en el que se puede mover un animal.
/// </summary>
public readonly struct NavigableMargins
{
	public readonly float Floor;
	public readonly float Surface;

	public NavigableMargins(float floor, float surface)
	{
		Floor = floor;
		Surface = surface;
	}
}

/// <summary>
/// Dominio navegable acuático: el volumen de agua del mapa. Construido a partir del
/// heightMap (primitivas, sin depender de Terrain): un punto es válido si su columna
/// X/Z es un tile de agua (altura de tile por debajo del nivel del mar) y su Y está
/// entre el fondo del terreno (+margen) y la superficie del mar (−margen).
/// </summary>
/// <remarks>
/// Replica internamente el muestreo bilineal de altura para no acoplar a TerrainSnap.
/// </remarks>
public class AquaticDomain : IAnimalDomain
{
	private readonly float[,] _heightMap;
	private readonly HeightMapGrid _grid;
	private readonly float _seaY;
	private readonly NavigableMargins _margins;

	/// <summary>Nº de intentos al muestrear un destino de paseo antes de rendirse.</summary>
	private const int SampleAttempts = 12;

	public AquaticDomain(float[,] heightMap, HeightMapGrid grid, float seaY, NavigableMargins margins)
	{
		_heightMap = heightMap;
		_grid = grid;
		_seaY = seaY;
		_margins = margins;
	}

	public bool Contains(Vector3 worldPos)
	{
		if (!IsWaterColumn(worldPos.X, worldPos.Z))
			return false;

		float floor = SampleFloor(worldPos.X, worldPos.Z);
		return worldPos.Y >= floor + _margins.Floor && worldPos.Y <= _seaY - _margins.Surface;
	}

	public Vector3 ClampToValid(Vector3 worldPos)
	{
		float floor = SampleFloor(worldPos.X, worldPos.Z);
		float min = floor + _margins.Floor;
		float max = _seaY - _margins.Surface;
		if (max < min)
			max = min;

		worldPos.Y = Mathf.Clamp(worldPos.Y, min, max);
		return worldPos;
	}

	public Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		for (int i = 0; i < SampleAttempts; i++)
		{
			if (TrySampleColumn(from, radius, rng, out Vector3 candidate))
				return candidate;
		}

		return from;
	}

	/// <summary>
	/// Intenta un único candidato de paseo: un punto al azar en un anillo alrededor de
	/// <paramref name="from"/>. Devuelve false si el candidato no cae en una columna de
	/// agua navegable, para que el llamador pueda reintentar.
	/// </summary>
	private bool TrySampleColumn(Vector3 from, float radius, RandomNumberGenerator rng, out Vector3 candidate)
	{
		candidate = from;

		float angle = rng.RandfRange(0f, Mathf.Tau);
		float dist = rng.RandfRange(0.2f * radius, radius);
		float x = from.X + Mathf.Cos(angle) * dist;
		float z = from.Z + Mathf.Sin(angle) * dist;

		if (!IsWaterColumn(x, z))
			return false;

		float floor = SampleFloor(x, z);
		float min = floor + _margins.Floor;
		float max = _seaY - _margins.Surface;
		if (max <= min)
			return false;

		float y = rng.RandfRange(min, max);
		candidate = new Vector3(x, y, z);
		return true;
	}

	/// <summary>¿La columna X/Z cae sobre un tile de agua (altura de tile bajo el nivel del mar)?</summary>
	private bool IsWaterColumn(float worldX, float worldZ)
	{
		int tx = Mathf.FloorToInt(worldX / _grid.TileSize);
		int ty = Mathf.FloorToInt(worldZ / _grid.TileSize);

		if (tx < 0 || tx >= _grid.Width || ty < 0 || ty >= _grid.Height)
			return false;

		return TileHeight(tx, ty) < _seaY;
	}

	/// <summary>
	/// Altura del centro del tile (promedio de las 2 esquinas diagonales), igual que
	/// AnimalSystem.GetTileHeight / Terrain.GetTileHeight.
	/// </summary>
	private float TileHeight(int tx, int ty)
	{
		return (_heightMap[tx + 1, ty] + _heightMap[tx, ty + 1]) / 2f;
	}

	/// <summary>
	/// Altura del fondo en una posición de mundo arbitraria, por interpolación bilineal
	/// de las 4 esquinas del heightMap que rodean la celda.
	/// </summary>
	private float SampleFloor(float worldX, float worldZ)
	{
		float gx = worldX / _grid.TileSize;
		float gz = worldZ / _grid.TileSize;

		int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, _grid.Width - 1);
		int z0 = Mathf.Clamp(Mathf.FloorToInt(gz), 0, _grid.Height - 1);
		int x1 = Mathf.Min(x0 + 1, _grid.Width);
		int z1 = Mathf.Min(z0 + 1, _grid.Height);

		float fx = Mathf.Clamp(gx - x0, 0f, 1f);
		float fz = Mathf.Clamp(gz - z0, 0f, 1f);

		float h00 = _heightMap[x0, z0];
		float h10 = _heightMap[x1, z0];
		float h01 = _heightMap[x0, z1];
		float h11 = _heightMap[x1, z1];

		float top = Mathf.Lerp(h00, h10, fx);
		float bottom = Mathf.Lerp(h01, h11, fx);
		return Mathf.Lerp(top, bottom, fz);
	}
}
