using Godot;

/// <summary>
/// Variante de <see cref="TerrainDomain"/> que usa la altura interpolada del terreno
/// (<see cref="TerrainSnap.SampleHeight"/>) en vez de la media por tile, para que el
/// corte agua/tierra siga el contorno real de la orilla en vez de la rejilla de tiles.
/// Usado por <see cref="GrassSystem"/>, cuyas posiciones no están ancladas a tiles.
/// </summary>
public class SmoothTerrainDomain : ISpatialDomain
{
	private readonly float[,] _heightMap;
	private readonly HeightMapGrid _grid;
	private readonly float _seaY;
	private readonly float _shoreMargin;

	public SmoothTerrainDomain(float[,] heightMap, HeightMapGrid grid, float seaY, float shoreMargin)
	{
		_heightMap = heightMap;
		_grid = grid;
		_seaY = seaY;
		_shoreMargin = shoreMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		float height = TerrainSnap.SampleHeight(_heightMap, worldPos.X, worldPos.Z, _grid);
		return height >= _seaY + _shoreMargin;
	}
}
