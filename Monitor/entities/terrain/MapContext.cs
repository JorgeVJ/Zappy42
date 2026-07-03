using Godot;
using System.Collections.Generic;

public partial class DecorationSystem : Node3D
{
	/// <summary>
	/// Agrupa los datos compartidos por toda una llamada a <see cref="Generate"/>
	/// (mapa de modelos, dimensiones, heightmap, ocupación y RNG) para que los métodos
	/// que colocan decoraciones queden dentro del límite de 4 parámetros.
	/// </summary>
	private readonly record struct MapContext(
		Dictionary<string, List<DecorationModel>> ModelsByType,
		int Width,
		int Height,
		float[,] HeightMap,
		bool[,] Occupied,
		RandomNumberGenerator Rng);
}
