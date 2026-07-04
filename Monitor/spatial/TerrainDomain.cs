using Godot;

/// <summary>
/// Región de tierra firme del mapa: tiles cuya altura queda por encima del nivel del
/// mar (más un margen de orilla opcional). Usado por <see cref="DecorationSystem"/>
/// para que la vegetación no se coloque sobre agua.
/// </summary>
public class TerrainDomain : ISpatialDomain
{
	private readonly float[,] _heightMap;
	private readonly HeightMapGrid _grid;
	private readonly float _seaY;
	private readonly float _shoreMargin;

	public TerrainDomain(float[,] heightMap, HeightMapGrid grid, float seaY, float shoreMargin)
	{
		_heightMap = heightMap;
		_grid = grid;
		_seaY = seaY;
		_shoreMargin = shoreMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		int tx = Mathf.FloorToInt(worldPos.X / _grid.TileSize);
		int ty = Mathf.FloorToInt(worldPos.Z / _grid.TileSize);

		if (tx < 0 || tx >= _grid.Width || ty < 0 || ty >= _grid.Height)
			return false;

		return TileHeight(tx, ty) >= _seaY + _shoreMargin;
	}

	private float TileHeight(int tx, int ty)
	{
		return (_heightMap[tx + 1, ty] + _heightMap[tx, ty + 1]) / 2f;
	}
}
