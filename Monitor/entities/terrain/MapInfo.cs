using Godot;

public partial class GrassSystem
{
	/// <summary>
	/// Agrupa las dimensiones del mapa y el heightmap compartidos por los métodos
	/// invocados desde <see cref="Generate"/>, de forma que queden dentro del
	/// límite de 4 parámetros.
	/// </summary>
	private readonly record struct MapInfo(float[,] HeightMap, int Width, int Height, float TileSize);
}
