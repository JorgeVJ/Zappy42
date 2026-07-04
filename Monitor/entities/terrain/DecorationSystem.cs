using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class DecorationSystem : DecorationSystemBase
{
	[Export]
	public float TreeDensity = 0.04f;

	[Export]
	public float RockDensity = 0.08f;

	[Export]
	public float BushDensity = 0.08f;

	[Export]
	public float GrassPropDensity = 0.16f;

	private const string ModelsPath = "res://entities/terrain/models/";

	private static readonly Regex ModelNamePattern =
		new(@"^(?<type>[A-Za-z]+)_(?<letter>[A-Z])_(?<w>\d+)x(?<l>\d+)\.glb$", RegexOptions.Compiled);

	/// <summary>
	/// Obstáculos colocados durante <see cref="Generate"/>, expuestos para que Terrain pueda evitar
	/// embeber recursos (bct) dentro de árboles/rocas/arbustos cercanos (C12). Cada
	/// entrada es la posición world XZ del centro de la decoración y un radio de exclusión
	/// derivado de su footprint (en tiles).
	/// </summary>
	private readonly List<PlacementFinder.Obstacle> _obstacles = new();

	public IReadOnlyList<PlacementFinder.Obstacle> Obstacles => _obstacles;

	public void Generate(float[,] heightMap, int width, int height)
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		_obstacles.Clear();

		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Seed = 1337;

		MapContext ctx = new MapContext(DiscoverModels(), width, height, heightMap, new bool[width, height], rng);

		HeightMapGrid grid = new HeightMapGrid(width, height, Terrain.TILE_SIZE);
		TerrainDomain landDomain = BuildLandDomain(heightMap, grid);

		PlaceDecorations(ctx, "Tree", TreeDensity, landDomain);
		PlaceDecorations(ctx, "Rock", RockDensity, null);
		PlaceDecorations(ctx, "Bush", BushDensity, landDomain);
		PlaceDecorations(ctx, "Grass", GrassPropDensity, landDomain);
	}

	private void PlaceDecorations(MapContext ctx, string type, float density, ISpatialDomain domain)
	{
		if (!ctx.ModelsByType.TryGetValue(type, out List<DecorationModel> models) || models.Count == 0)
			return;

		int count = Mathf.RoundToInt(density * ctx.Width * ctx.Height);
		int maxAttempts = count * 20;

		for (int placed = 0, attempts = 0; placed < count && attempts < maxAttempts; attempts++)
		{
			if (TryPlaceOne(ctx, models, domain))
				placed++;
		}
	}

	/// <summary>
	/// Intenta colocar una única decoración de la lista de modelos candidatos.
	/// Extraído de PlaceDecorations para respetar el límite de longitud de método.
	/// </summary>
	private bool TryPlaceOne(MapContext ctx, List<DecorationModel> models, ISpatialDomain domain)
	{
		DecorationModel model = models[ctx.Rng.RandiRange(0, models.Count - 1)];
		int w = model.FootprintW;
		int l = model.FootprintL;

		if (w > ctx.Width || l > ctx.Height)
			return false;

		int tx = ctx.Rng.RandiRange(0, ctx.Width - w);
		int ty = ctx.Rng.RandiRange(0, ctx.Height - l);
		TileRect rect = new TileRect(tx, ty, w, l);

		if (!IsFree(ctx.Occupied, rect))
			return false;

		Vector2 centerXZ = TileCenterXZ(rect);
		if (domain != null && !domain.Contains(new Vector3(centerXZ.X, 0f, centerXZ.Y)))
			return false;

		MarkOccupied(ctx.Occupied, rect);
		PlaceInstance(model, rect, ctx);
		return true;
	}

	private static Vector2 TileCenterXZ(TileRect rect)
	{
		float worldX = rect.Tx * Terrain.TILE_SIZE + rect.W * Terrain.TILE_SIZE / 2f;
		float worldZ = rect.Ty * Terrain.TILE_SIZE + rect.L * Terrain.TILE_SIZE / 2f;
		return new Vector2(worldX, worldZ);
	}

	private static bool IsFree(bool[,] occupied, TileRect rect)
	{
		for (int x = rect.Tx; x < rect.Tx + rect.W; x++)
			for (int y = rect.Ty; y < rect.Ty + rect.L; y++)
				if (occupied[x, y])
					return false;
		return true;
	}

	private static void MarkOccupied(bool[,] occupied, TileRect rect)
	{
		for (int x = rect.Tx; x < rect.Tx + rect.W; x++)
			for (int y = rect.Ty; y < rect.Ty + rect.L; y++)
				occupied[x, y] = true;
	}

	/// <summary>
	/// Instancia la decoración en la posición de mundo calculada y registra su obstáculo.
	/// </summary>
	/// <remarks>
	/// Radio de exclusión aproximado: la mitad de la diagonal del footprint (en
	/// unidades de mundo), para que un recurso (C12) no aparezca embebido dentro
	/// de esta decoración independientemente de su orientación.
	/// </remarks>
	private void PlaceInstance(DecorationModel model, TileRect rect, MapContext ctx)
	{
		Vector2 centerXZ = TileCenterXZ(rect);
		HeightMapGrid grid = new HeightMapGrid(ctx.Width, ctx.Height, Terrain.TILE_SIZE);
		float worldY = TerrainSnap.SampleHeight(ctx.HeightMap, centerXZ.X, centerXZ.Y, grid);

		Node3D instance = model.Scene.Instantiate<Node3D>();
		instance.Position = new Vector3(centerXZ.X, worldY, centerXZ.Y);
		instance.RotateY(ctx.Rng.RandfRange(0f, Mathf.Tau));
		AddChild(instance);

		float footprintRadius = new Vector2(rect.W * Terrain.TILE_SIZE, rect.L * Terrain.TILE_SIZE).Length() / 2f;
		_obstacles.Add(new PlacementFinder.Obstacle(centerXZ, footprintRadius));
	}

	private static Dictionary<string, List<DecorationModel>> DiscoverModels()
	{
		Dictionary<string, List<DecorationModel>> result = new Dictionary<string, List<DecorationModel>>();

		using DirAccess dir = DirAccess.Open(ModelsPath);
		if (dir == null)
			return result;

		dir.ListDirBegin();
		for (string fileName = dir.GetNext(); fileName != ""; fileName = dir.GetNext())
			RegisterModelIfMatch(fileName, result);
		dir.ListDirEnd();

		return result;
	}

	/// <summary>
	/// Parsea un nombre de archivo de modelo y, si encaja con el patrón esperado, lo
	/// registra en <paramref name="result"/>. Extraído de DiscoverModels para respetar
	/// el límite de longitud de método.
	/// </summary>
	private static void RegisterModelIfMatch(string fileName, Dictionary<string, List<DecorationModel>> result)
	{
		Match match = ModelNamePattern.Match(fileName);
		if (!match.Success)
			return;

		PackedScene scene = ResourceLoader.Load<PackedScene>(ModelsPath + fileName);
		if (scene == null)
			return;

		string type = match.Groups["type"].Value;
		int w = int.Parse(match.Groups["w"].Value);
		int l = int.Parse(match.Groups["l"].Value);

		if (!result.TryGetValue(type, out List<DecorationModel> list))
			result[type] = list = new List<DecorationModel>();

		list.Add(new DecorationModel(scene, w, l));
	}
}
