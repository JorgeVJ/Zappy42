using Godot;

/// <summary>
/// Dominio navegable acuático: el volumen de agua del mapa. Construido a partir del
/// heightMap (primitivas, sin depender de Terrain): un punto es válido si su columna
/// X/Z es un tile de agua (altura de tile por debajo del nivel del mar) y su Y está
/// entre el fondo del terreno (+margen) y la superficie del mar (−margen).
/// </summary>
/// <remarks>
/// El muestreo de altura (bilineal, por tile) y de destinos en anillo lo aporta
/// <see cref="HeightField"/>, para no acoplar a TerrainSnap ni duplicarlo entre dominios.
/// </remarks>
public class AquaticDomain : HeightField, IAnimalDomain
{
	private readonly float _seaY;
	private readonly NavigableMargins _margins;

	public AquaticDomain(float[,] heightMap, HeightMapGrid grid, float seaY, NavigableMargins margins)
		: base(heightMap, grid)
	{
		_seaY = seaY;
		_margins = margins;
	}

	public bool Contains(Vector3 worldPos)
	{
		if (!IsWaterColumn(worldPos.X, worldPos.Z))
			return false;

		float floor = SampleHeight(worldPos.X, worldPos.Z);
		return worldPos.Y >= floor + _margins.Floor && worldPos.Y <= _seaY - _margins.Surface;
	}

	public Vector3 ClampToValid(Vector3 worldPos)
	{
		float floor = SampleHeight(worldPos.X, worldPos.Z);
		float min = floor + _margins.Floor;
		float max = _seaY - _margins.Surface;
		if (max < min)
			max = min;

		worldPos.Y = Mathf.Clamp(worldPos.Y, min, max);
		return worldPos;
	}

	public Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		return SampleRing(from, radius, rng, TrySelectWater);

		bool TrySelectWater(float x, float z, out Vector3 result)
		{
			result = Vector3.Zero;
			if (!IsWaterColumn(x, z))
				return false;

			float min = SampleHeight(x, z) + _margins.Floor;
			float max = _seaY - _margins.Surface;
			if (max <= min)
				return false;

			result = new Vector3(x, rng.RandfRange(min, max), z);
			return true;
		}
	}

	/// <summary>El agua no tiene "superficie sólida" a la que pasear: no aplica.</summary>
	public Vector3 SampleSurfaceTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		return from;
	}

	/// <summary>El volumen acuático no tiene concepto de "tocar suelo": nunca.</summary>
	public bool IsAtSurface(Vector3 worldPos, float threshold)
	{
		return false;
	}

	/// <summary>¿La columna X/Z cae sobre un tile de agua (altura de tile bajo el nivel del mar)?</summary>
	private bool IsWaterColumn(float worldX, float worldZ)
	{
		return TryTileHeight(worldX, worldZ, out float h) && h < _seaY;
	}
}
