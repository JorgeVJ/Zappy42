using Godot;
using System.Collections.Generic;

public partial class Terrain : Node3D
{
	/// <summary>
	/// Agrupa los parámetros de contexto de tile compartidos por todas las llamadas a
	/// <see cref="GetResourceOffset"/> para un mismo tile, de forma que el método
	/// quede dentro del límite de 4 parámetros.
	/// </summary>
	private readonly record struct ResourceOffsetContext(int X, int Y, Vector3 Center, List<PlacementFinder.Obstacle> Obstacles);
}
