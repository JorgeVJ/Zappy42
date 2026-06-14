using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class DecorationSystem : Node3D
{
	[Export] public float TreeDensity = 0.04f;
	[Export] public float RockDensity = 0.08f;
	[Export] public float BushDensity = 0.08f;
	[Export] public float GrassPropDensity = 0.16f;

	private const string ModelsPath = "res://entities/terrain/models/";

	private static readonly Regex ModelNamePattern =
		new(@"^(?<type>[A-Za-z]+)_(?<letter>[A-Z])_(?<w>\d+)x(?<l>\d+)\.glb$", RegexOptions.Compiled);

	private readonly record struct DecorationModel(PackedScene Scene, int FootprintW, int FootprintL);

	public void Generate(float[,] heightMap, int width, int height)
	{
		foreach (Node child in GetChildren())
			child.QueueFree();

		var modelsByType = DiscoverModels();
		var occupied = new bool[width, height];

		// Seed differs from GrassSystem (42) so prop placement doesn't correlate with grass blades.
		var rng = new RandomNumberGenerator();
		rng.Seed = 1337;

		PlaceDecorations(modelsByType, "Tree", TreeDensity, width, height, heightMap, occupied, rng);
		PlaceDecorations(modelsByType, "Rock", RockDensity, width, height, heightMap, occupied, rng);
		PlaceDecorations(modelsByType, "Bush", BushDensity, width, height, heightMap, occupied, rng);
		PlaceDecorations(modelsByType, "Grass", GrassPropDensity, width, height, heightMap, occupied, rng);
	}

	private void PlaceDecorations(Dictionary<string, List<DecorationModel>> modelsByType, string type,
		float density, int width, int height, float[,] heightMap, bool[,] occupied, RandomNumberGenerator rng)
	{
		if (!modelsByType.TryGetValue(type, out var models) || models.Count == 0)
			return;

		int count = Mathf.RoundToInt(density * width * height);
		int maxAttempts = count * 20;

		for (int placed = 0, attempts = 0; placed < count && attempts < maxAttempts; attempts++)
		{
			var model = models[rng.RandiRange(0, models.Count - 1)];
			int w = model.FootprintW;
			int l = model.FootprintL;

			if (w > width || l > height)
				continue;

			int tx = rng.RandiRange(0, width - w);
			int ty = rng.RandiRange(0, height - l);

			if (!IsFree(occupied, tx, ty, w, l))
				continue;

			MarkOccupied(occupied, tx, ty, w, l);
			PlaceInstance(model, tx, ty, w, l, heightMap, width, height, rng);
			placed++;
		}
	}

	private static bool IsFree(bool[,] occupied, int tx, int ty, int w, int l)
	{
		for (int x = tx; x < tx + w; x++)
			for (int y = ty; y < ty + l; y++)
				if (occupied[x, y])
					return false;
		return true;
	}

	private static void MarkOccupied(bool[,] occupied, int tx, int ty, int w, int l)
	{
		for (int x = tx; x < tx + w; x++)
			for (int y = ty; y < ty + l; y++)
				occupied[x, y] = true;
	}

	private void PlaceInstance(DecorationModel model, int tx, int ty, int w, int l,
		float[,] heightMap, int width, int height, RandomNumberGenerator rng)
	{
		float worldX = tx * Terrain.TILE_SIZE + w * Terrain.TILE_SIZE / 2f;
		float worldZ = ty * Terrain.TILE_SIZE + l * Terrain.TILE_SIZE / 2f;
		float worldY = TerrainSnap.SampleHeight(heightMap, worldX, worldZ, Terrain.TILE_SIZE, width, height);

		var instance = model.Scene.Instantiate<Node3D>();
		instance.Position = new Vector3(worldX, worldY, worldZ);
		instance.RotateY(rng.RandfRange(0f, Mathf.Tau));
		AddChild(instance);
	}

	private static Dictionary<string, List<DecorationModel>> DiscoverModels()
	{
		var result = new Dictionary<string, List<DecorationModel>>();

		using var dir = DirAccess.Open(ModelsPath);
		if (dir == null)
			return result;

		dir.ListDirBegin();
		for (string fileName = dir.GetNext(); fileName != ""; fileName = dir.GetNext())
		{
			var match = ModelNamePattern.Match(fileName);
			if (!match.Success)
				continue;

			var scene = ResourceLoader.Load<PackedScene>(ModelsPath + fileName);
			if (scene == null)
				continue;

			string type = match.Groups["type"].Value;
			int w = int.Parse(match.Groups["w"].Value);
			int l = int.Parse(match.Groups["l"].Value);

			if (!result.TryGetValue(type, out var list))
				result[type] = list = new List<DecorationModel>();

			list.Add(new DecorationModel(scene, w, l));
		}
		dir.ListDirEnd();

		return result;
	}
}
