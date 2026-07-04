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
