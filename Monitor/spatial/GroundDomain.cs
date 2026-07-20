using Godot;

/// <summary>
/// Dominio navegable de un animal terrestre: cubre las columnas de tierra del mapa (por encima
/// del nivel del mar más un margen de orilla) y mantiene al animal a ras de la superficie del
/// terreno. Es un dominio de suelo puro (sin volumen aéreo): <see cref="ClampToValid"/> fija la
/// altura a la del terreno cada paso, de modo que el animal queda pegado al suelo en pendientes
/// sin lógica de snapping adicional.
/// </summary>
/// <remarks>
/// Construido solo desde primitivas (heightMap, dimensiones, alturas), sin referenciar Terrain.
/// El muestreo de altura y de destinos en anillo lo aporta <see cref="HeightField"/>.
/// </remarks>
public class GroundDomain : HeightField, IAnimalDomain
{
	private readonly float _seaY;
	private readonly float _shoreMargin;

	public GroundDomain(float[,] heightMap, HeightMapGrid grid, float seaY, float shoreMargin)
		: base(heightMap, grid)
	{
		_seaY = seaY;
		_shoreMargin = shoreMargin;
	}

	/// <summary>Altura de la superficie del terreno bajo una columna X/Z.</summary>
	public float FloorHeight(float worldX, float worldZ)
	{
		return SampleHeight(worldX, worldZ);
	}

	/// <summary>¿La columna X/Z cae sobre un tile de tierra (por encima del nivel del mar + orilla)?</summary>
	public bool IsLandColumn(float worldX, float worldZ)
	{
		return TryTileHeight(worldX, worldZ, out float h) && h >= _seaY + _shoreMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		return IsLandColumn(worldPos.X, worldPos.Z);
	}

	public Vector3 ClampToValid(Vector3 worldPos)
	{
		worldPos = ClampXZ(worldPos);
		worldPos.Y = FloorHeight(worldPos.X, worldPos.Z);
		return worldPos;
	}

	/// <summary>
	/// Destino de paseo terrestre: un punto sobre una columna de tierra, a ras de la superficie
	/// del terreno. Reintenta hasta caer en tierra; si no encuentra, se queda donde estaba.
	/// </summary>
	public Vector3 SampleWanderTarget(Vector3 from, float radius, RandomNumberGenerator rng)
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

	/// <summary>En un dominio de suelo puro, la superficie es el propio espacio de paseo.</summary>
	public Vector3 SampleSurfaceTarget(Vector3 from, float radius, RandomNumberGenerator rng)
	{
		return SampleWanderTarget(from, radius, rng);
	}

	/// <summary>El animal terrestre va pegado al suelo: está en superficie si la columna es tierra.</summary>
	public bool IsAtSurface(Vector3 worldPos, float threshold)
	{
		return IsLandColumn(worldPos.X, worldPos.Z);
	}
}
