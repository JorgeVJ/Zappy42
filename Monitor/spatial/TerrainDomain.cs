using Godot;

/// <summary>
/// Región de tierra firme del mapa: tiles cuya altura queda por encima del nivel del
/// mar (más un margen de orilla opcional). Usado por <see cref="DecorationSystem"/>
/// para que la vegetación no se coloque sobre agua.
/// </summary>
/// <remarks>
/// El muestreo de altura por tile lo aporta <see cref="HeightField"/>, compartido con
/// los dominios navegables.
/// </remarks>
public class TerrainDomain : HeightField, ISpatialDomain
{
	private readonly float _seaY;
	private readonly float _shoreMargin;

	public TerrainDomain(float[,] heightMap, HeightMapGrid grid, float seaY, float shoreMargin)
		: base(heightMap, grid)
	{
		_seaY = seaY;
		_shoreMargin = shoreMargin;
	}

	public bool Contains(Vector3 worldPos)
	{
		return TryTileHeight(worldPos.X, worldPos.Z, out float h) && h >= _seaY + _shoreMargin;
	}
}
