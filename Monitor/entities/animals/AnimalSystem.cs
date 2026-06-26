using Godot;
using System.Collections.Generic;

// Coloca peces decorativos sobre las zonas de agua del mapa. Autocontenido: no
// referencia Terrain ni ningún otro tipo del proyecto, solo recibe primitivas
// (heightMap, width, height) para poder copiarse/pegarse a otro proyecto con
// un sistema de heightmap similar añadiendo una única llamada a Generate().
public partial class AnimalSystem : Node3D
{
	// Mismo cálculo de nivel del mar que WaterSystem (fracción entre min y max
	// del heightMap). Se duplica en vez de referenciar WaterSystem para que
	// AnimalSystem no dependa de ningún otro nodo del árbol de escena.
	[Export(PropertyHint.Range, "0,1,0.01")] public float SeaLevelFraction = 0.35f;
	[Export] public float SeaLevelOffset = 0f;

	[Export(PropertyHint.Range, "0,20,1")] public int FishCount = 6;
	[Export] public float SpawnYOffset = 0.3f;
	[Export] public float TileSize = 2.0f;

	// Especies disponibles: cada spawn elige una ruta al azar de este array. Para
	// añadir más especies basta con incluir un .glb con huesos "Body"/"Tail" y
	// añadir su ruta aquí (configurable desde el inspector).
	[Export] public string[] FishModels =
	{
		"res://entities/animals/ClownFish.glb",
		"res://entities/animals/SurgeonFish.glb",
	};

	private Node3D _container;

	public override void _Ready()
	{
		_container = new Node3D { Name = "Fishes" };
		AddChild(_container);
	}

	public void Generate(float[,] heightMap, int width, int height)
	{
		if (_container == null || heightMap == null || FishModels.Length == 0)
			return;

		foreach (Node child in _container.GetChildren())
			child.QueueFree();

		float seaY = ComputeSeaLevel(heightMap, SeaLevelFraction) + SeaLevelOffset;

		var waterTiles = new List<Vector2I>();
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if (GetTileHeight(heightMap, x, y) < seaY)
					waterTiles.Add(new Vector2I(x, y));
			}
		}

		if (waterTiles.Count == 0)
			return;

		int count = Mathf.Min(FishCount, waterTiles.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2I tile = waterTiles[GD.RandRange(0, waterTiles.Count - 1)];
			Vector3 pos = new Vector3(
				tile.X * TileSize + TileSize / 2f,
				seaY - SpawnYOffset,
				tile.Y * TileSize + TileSize / 2f
			);

			string modelPath = FishModels[GD.RandRange(0, FishModels.Length - 1)];
			_container.AddChild(Fish.Create(pos, modelPath));
		}
	}

	private static float GetTileHeight(float[,] heightMap, int x, int y)
	{
		return (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
	}

	private static float ComputeSeaLevel(float[,] heightMap, float fraction)
	{
		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (float h in heightMap)
		{
			if (h < min) min = h;
			if (h > max) max = h;
		}

		return Mathf.Lerp(min, max, fraction);
	}
}
