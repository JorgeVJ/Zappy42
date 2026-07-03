using Godot;
using System.Collections.Generic;

/// <summary>
/// Coloca peces decorativos sobre las zonas de agua del mapa.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia Terrain ni ningún otro tipo del proyecto, solo
/// recibe primitivas (heightMap, width, height).
/// </remarks>
public partial class AnimalSystem : Node3D
{
	/// <summary>
	/// Mismo cálculo de nivel del mar que WaterSystem (fracción entre min y max
	/// del heightMap). Se duplica en vez de referenciar WaterSystem para que
	/// AnimalSystem no dependa de ningún otro nodo del árbol de escena.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float SeaLevelFraction = 0.35f;

	[Export]
	public float SeaLevelOffset = 0f;

	[Export(PropertyHint.Range, "0,20,1")]
	public int FishCount = 6;

	[Export]
	public float TileSize = 2.0f;

	/// <summary>Márgenes que el pez deja respecto al fondo del volumen navegable.</summary>
	[Export]
	public float FloorMargin = 0.4f;

	/// <summary>Márgenes que el pez deja respecto a la superficie del volumen navegable.</summary>
	[Export]
	public float SurfaceMargin = 0.4f;

	/// <summary>Tuning del paseo: se inyecta en la locomoción de cada pez.</summary>
	[Export]
	public float MaxSpeed = 1.6f;

	/// <summary>Tuning del paseo: se inyecta en el comportamiento de cada pez.</summary>
	[Export]
	public float WanderRadius = 6f;

	/// <summary>Tuning de la huida de la cámara (Utility AI): distancia de cámara para huida máxima.</summary>
	[Export]
	public float FleeInner = 2f;

	/// <summary>Tuning de la huida de la cámara (Utility AI): distancia a la que deja de huir.</summary>
	[Export]
	public float FleeOuter = 6f;

	/// <summary>Tuning de la huida de la cámara (Utility AI): cuánto acelera el nado al huir.</summary>
	[Export]
	public float FleeSpeedScale = 4.2f;

	/// <summary>
	/// Especies disponibles: cada spawn elige una ruta al azar de este array. Para
	/// añadir más especies basta con incluir un .glb con huesos "Body"/"Tail" y
	/// añadir su ruta aquí (configurable desde el inspector).
	/// </summary>
	[Export]
	public string[] FishModels =
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
		List<Vector2I> waterTiles = CollectWaterTiles(heightMap, width, height, seaY);
		if (waterTiles.Count == 0)
			return;

		HeightMapGrid grid = new HeightMapGrid(width, height, TileSize);
		NavigableMargins margins = new NavigableMargins(FloorMargin, SurfaceMargin);
		AquaticDomain domain = new AquaticDomain(heightMap, grid, seaY, margins);
		SpawnFish(heightMap, seaY, waterTiles, domain);
	}

	/// <summary>
	/// Recorre todos los tiles del mapa y devuelve los que quedan por debajo del
	/// nivel del mar (candidatos válidos para colocar peces).
	/// </summary>
	private static List<Vector2I> CollectWaterTiles(float[,] heightMap, int width, int height, float seaY)
	{
		List<Vector2I> waterTiles = new List<Vector2I>();
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if (GetTileHeight(heightMap, x, y) < seaY)
					waterTiles.Add(new Vector2I(x, y));
			}
		}

		return waterTiles;
	}

	/// <summary>
	/// Elige FishCount tiles de agua al azar y coloca un pez en cada uno, inyectando
	/// el dominio navegable compartido y el cerebro de Utility AI (pasear/huir).
	/// </summary>
	private void SpawnFish(float[,] heightMap, float seaY, List<Vector2I> waterTiles, AquaticDomain domain)
	{
		int count = Mathf.Min(FishCount, waterTiles.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2I tile = waterTiles[GD.RandRange(0, waterTiles.Count - 1)];
			Vector3 pos = domain.ClampToValid(new Vector3(
				tile.X * TileSize + TileSize / 2f,
				(seaY + GetTileHeight(heightMap, tile.X, tile.Y)) / 2f,
				tile.Y * TileSize + TileSize / 2f
			));

			string modelPath = FishModels[GD.RandRange(0, FishModels.Length - 1)];
			Fish fish = Fish.Create(pos, modelPath);
			fish.Domain = domain;
			fish.Locomotion.MaxSpeed = MaxSpeed;
			fish.Behavior = new UtilityBrain(new IAnimalBehavior[]
			{
				new WanderBehavior { WanderRadius = WanderRadius },
				new FleeBehavior { FleeInner = FleeInner, FleeOuter = FleeOuter, FleeSpeedScale = FleeSpeedScale },
			});
			_container.AddChild(fish);
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
