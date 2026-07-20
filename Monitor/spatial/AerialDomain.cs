using Godot;

/// <summary>
/// Dominio navegable de un ave: cubre todo el mapa horizontalmente y verticalmente
/// desde la superficie (terreno sobre tierra, nivel del mar sobre agua) hasta un techo.
/// Es un dominio único suelo↔techo: caminar y volar comparten el mismo volumen y solo
/// difieren en la altura a la que se piden los destinos, de modo que despegue y
/// aterrizaje son planeos suaves de la locomoción (sin teletransporte al cambiar de modo).
/// </summary>
/// <remarks>
/// Construido solo desde primitivas (heightMap, dimensiones, alturas), sin referenciar
/// Terrain. El muestreo de altura y de destinos en anillo lo aporta <see cref="HeightField"/>.
/// </remarks>
public class AerialDomain : HeightField, IAnimalDomain
{
	private readonly float _seaY;
	private readonly float _shoreMargin;
	private readonly float _minFlyAltitude;
	private readonly float _ceiling;

	public AerialDomain(float[,] heightMap, HeightMapGrid grid, float seaY, AerialBounds bounds)
		: base(heightMap, grid)
	{
		_seaY = seaY;
		_shoreMargin = bounds.ShoreMargin;
		_minFlyAltitude = bounds.MinFlyAltitude;
		_ceiling = bounds.Ceiling;
	}

	/// <summary>
	/// Altura del suelo bajo una columna: superficie del terreno sobre tierra, o nivel
	/// del mar sobre agua. El ave nunca baja por debajo de este valor.
	/// </summary>
	public float FloorHeight(float worldX, float worldZ)
	{
		return Mathf.Max(SampleHeight(worldX, worldZ), _seaY);
	}

	/// <summary>¿La columna X/Z cae sobre un tile de tierra (por encima del nivel del mar + orilla)?</summary>
	public bool IsLandColumn(float worldX, float worldZ)
	{
		return TryTileHeight(worldX, worldZ, out float h) && h >= _seaY + _shoreMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		if (!InBounds(worldPos.X, worldPos.Z))
			return false;

		float floor = FloorHeight(worldPos.X, worldPos.Z);
		return worldPos.Y >= floor && worldPos.Y <= _ceiling;
	}

	public Vector3 ClampToValid(Vector3 worldPos)
	{
		worldPos = ClampXZ(worldPos);
		float floor = FloorHeight(worldPos.X, worldPos.Z);
		float max = Mathf.Max(floor, _ceiling);
		worldPos.Y = Mathf.Clamp(worldPos.Y, floor, max);
		return worldPos;
	}

	/// <summary>
	/// Destino de vuelo: un punto al azar en el aire sobre cualquier parte del mapa
	/// (tierra o agua), a una altura entre el suelo + margen mínimo y el techo.
	/// </summary>
	public Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		return SampleRing(from, radius, rng, TrySelectAir);

		bool TrySelectAir(float x, float z, out Vector3 result)
		{
			result = Vector3.Zero;
			if (!InBounds(x, z))
				return false;

			float min = FloorHeight(x, z) + _minFlyAltitude;
			float max = Mathf.Max(min, _ceiling);
			result = new Vector3(x, rng.RandfRange(min, max), z);
			return true;
		}
	}

	/// <summary>
	/// Destino a ras de suelo (aterrizaje): un punto sobre una columna de tierra, a ras de la
	/// superficie del terreno. Reintenta hasta caer en tierra; si no encuentra, se queda.
	/// </summary>
	public Vector3 SampleSurfaceTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		return SampleRing(from, radius, rng, TrySelectLand);

		bool TrySelectLand(float x, float z, out Vector3 result)
		{
			result = Vector3.Zero;
			if (!IsLandColumn(x, z))
				return false;

			result = new Vector3(x, FloorHeight(x, z), z);
			return true;
		}
	}

	/// <summary>¿Está el punto sobre tierra y a menos de <paramref name="threshold"/> de la superficie (posado)?</summary>
	public bool IsAtSurface(Vector3 worldPos, float threshold)
	{
		if (!IsLandColumn(worldPos.X, worldPos.Z))
			return false;

		return worldPos.Y - FloorHeight(worldPos.X, worldPos.Z) < threshold;
	}
}
