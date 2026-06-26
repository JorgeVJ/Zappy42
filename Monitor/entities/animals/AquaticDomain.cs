using Godot;

// Dominio navegable acuático: el volumen de agua del mapa. Construido a partir del
// heightMap (primitivas, sin depender de Terrain): un punto es válido si su columna
// X/Z es un tile de agua (altura de tile por debajo del nivel del mar) y su Y está
// entre el fondo del terreno (+margen) y la superficie del mar (−margen).
//
// Replica internamente el muestreo bilineal de altura para no acoplar a TerrainSnap
// y mantener el sistema de animales copiable/pegable a otro proyecto.
public class AquaticDomain : IAnimalDomain
{
	private readonly float[,] _heightMap;
	private readonly int _width;
	private readonly int _height;
	private readonly float _tileSize;
	private readonly float _seaY;
	private readonly float _floorMargin;
	private readonly float _surfaceMargin;

	// Nº de intentos al muestrear un destino de paseo antes de rendirse.
	private const int SampleAttempts = 12;

	public AquaticDomain(float[,] heightMap, int width, int height, float tileSize,
		float seaY, float floorMargin, float surfaceMargin)
	{
		_heightMap = heightMap;
		_width = width;
		_height = height;
		_tileSize = tileSize;
		_seaY = seaY;
		_floorMargin = floorMargin;
		_surfaceMargin = surfaceMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		if (!IsWaterColumn(worldPos.X, worldPos.Z))
			return false;

		float floor = SampleFloor(worldPos.X, worldPos.Z);
		return worldPos.Y >= floor + _floorMargin && worldPos.Y <= _seaY - _surfaceMargin;
	}

	public Vector3 ClampToValid(Vector3 worldPos)
	{
		float floor = SampleFloor(worldPos.X, worldPos.Z);
		float min = floor + _floorMargin;
		float max = _seaY - _surfaceMargin;
		if (max < min)
			max = min;

		worldPos.Y = Mathf.Clamp(worldPos.Y, min, max);
		return worldPos;
	}

	public Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		for (int i = 0; i < SampleAttempts; i++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float dist = rng.RandfRange(0.2f * radius, radius);
			float x = from.X + Mathf.Cos(angle) * dist;
			float z = from.Z + Mathf.Sin(angle) * dist;

			if (!IsWaterColumn(x, z))
				continue;

			float floor = SampleFloor(x, z);
			float min = floor + _floorMargin;
			float max = _seaY - _surfaceMargin;
			if (max <= min)
				continue;

			float y = rng.RandfRange(min, max);
			return new Vector3(x, y, z);
		}

		return from;
	}

	// ¿La columna X/Z cae sobre un tile de agua (altura de tile bajo el nivel del mar)?
	private bool IsWaterColumn(float worldX, float worldZ)
	{
		int tx = Mathf.FloorToInt(worldX / _tileSize);
		int ty = Mathf.FloorToInt(worldZ / _tileSize);

		if (tx < 0 || tx >= _width || ty < 0 || ty >= _height)
			return false;

		return TileHeight(tx, ty) < _seaY;
	}

	// Altura del centro del tile (promedio de las 2 esquinas diagonales), igual que
	// AnimalSystem.GetTileHeight / Terrain.GetTileHeight.
	private float TileHeight(int tx, int ty)
	{
		return (_heightMap[tx + 1, ty] + _heightMap[tx, ty + 1]) / 2f;
	}

	// Altura del fondo en una posición de mundo arbitraria, por interpolación bilineal
	// de las 4 esquinas del heightMap que rodean la celda.
	private float SampleFloor(float worldX, float worldZ)
	{
		float gx = worldX / _tileSize;
		float gz = worldZ / _tileSize;

		int x0 = Mathf.Clamp(Mathf.FloorToInt(gx), 0, _width - 1);
		int z0 = Mathf.Clamp(Mathf.FloorToInt(gz), 0, _height - 1);
		int x1 = Mathf.Min(x0 + 1, _width);
		int z1 = Mathf.Min(z0 + 1, _height);

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
