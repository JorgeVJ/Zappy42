using Godot;
using System.Collections.Generic;

public partial class Terrain
{
	/// <summary>
	/// Agrupa las listas de vértices/índices/normales en construcción para que los
	/// métodos que las rellenan queden dentro del límite de 4 parámetros.
	/// </summary>
	private readonly record struct MeshBuffers(List<Vector3> Vertices, List<int> Indices, List<Vector3> Normals);
}
