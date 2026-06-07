using Godot;
using System.Collections.Generic;

public partial class Terrain : Node3D
{
	[Export] public int Width = 5;
	[Export] public int Height = 5;

	[Export] public float HeightScale = 3f;
	[Export] public float NoiseScale = 0.08f;
	private float _lineWidth = 0.01f;
	[Export] public float LineWidth
	{
		get => _lineWidth;
		set
		{
			_lineWidth = value;
			if (terrainMesh?.GetActiveMaterial(0) is ShaderMaterial mat)
				mat.SetShaderParameter("line_width", _lineWidth);
		}
	}

	public const float TILE_SIZE = 2.0f;

    private float[,] heightMap;

	private Tile[,] tiles;

	private MeshInstance3D terrainMesh;

	private static readonly PackedScene resourceScene = ResourceLoader.Load<PackedScene>("res://entities/resources/resource.tscn");
	private readonly Dictionary<(int, int), List<Resource>> tileResources = new();

	// Offsets within a tile for placing up to 7 resource nodes (one per type)
	private static readonly Vector2[] ResourceOffsets = {
		new( 0,      0),
		new(-0.6f,   0),
		new( 0.6f,   0),
		new( 0,     -0.6f),
		new( 0,      0.6f),
		new(-0.6f,  -0.6f),
		new( 0.6f,   0.6f),
	};

	public override void _Ready()
	{
		terrainMesh = GetNode<MeshInstance3D>("MeshInstance3D");
	}

	public void InitializeMap(int width, int height)
	{
		Width = width;
		Height = height;

		CreateTiles();
		GenerateHeightMap();
		GenerateTerrainMesh();
	}

	private void CreateTiles()
	{
		tiles = new Tile[Width, Height];

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				tiles[x, y] = new Tile(x, y);
				tileResources[(x, y)] = new List<Resource>();
				int cx = x, cy = y;
				tiles[x, y].Inventory.Changed += () => UpdateTileResources(cx, cy);
			}
		}
	}

	private void UpdateTileResources(int x, int y)
	{
		foreach (var r in tileResources[(x, y)])
			r.QueueFree();
		tileResources[(x, y)].Clear();

		// The tile center falls on Triangle 2 (v1, v3, v2) with barycentric weights 0.5/0/0.5
		// → correct surface height = (h[x+1,y] + h[x,y+1]) / 2
		float h = (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
		Vector3 center = new Vector3(x * TILE_SIZE + TILE_SIZE / 2f, h, y * TILE_SIZE + TILE_SIZE / 2f);

		int slot = 0;
		foreach (var kvp in tiles[x, y].Inventory.All)
		{
			if (kvp.Value <= 0) continue;

			var offset = ResourceOffsets[slot % ResourceOffsets.Length];
			var resource = resourceScene.Instantiate<Resource>();
			resource.Position = center + new Vector3(offset.X, 0.05f, offset.Y);
			AddChild(resource);
			resource.SetResourceType(kvp.Key);
			tileResources[(x, y)].Add(resource);
			slot++;
		}
	}

	void GenerateHeightMap()
	{
		heightMap = new float[Width + 1, Height + 1];

		var noise = new FastNoiseLite();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		noise.Frequency = NoiseScale;

		for (int x = 0; x <= Width; x++)
		{
			for (int y = 0; y <= Height; y++)
			{
				float n = noise.GetNoise2D(x, y);
				heightMap[x, y] = n * HeightScale;
			}
		}
	}

	private void GenerateTerrainMesh()
	{
		var vertices = new List<Vector3>();
		var indices = new List<int>();
		var normals = new List<Vector3>();

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				Vector3 v0 = new Vector3(x * TILE_SIZE, heightMap[x, y], y * TILE_SIZE);
				Vector3 v1 = new Vector3((x + 1) * TILE_SIZE, heightMap[x + 1, y], y * TILE_SIZE);
				Vector3 v2 = new Vector3(x * TILE_SIZE, heightMap[x, y + 1], (y + 1) * TILE_SIZE);
				Vector3 v3 = new Vector3((x + 1) * TILE_SIZE, heightMap[x + 1, y + 1], (y + 1) * TILE_SIZE);

				int baseIndex = vertices.Count;

				vertices.Add(v0);
				vertices.Add(v1);
				vertices.Add(v2);
				vertices.Add(v3);

				indices.Add(baseIndex);
				indices.Add(baseIndex + 1);
				indices.Add(baseIndex + 2);

				indices.Add(baseIndex + 1);
				indices.Add(baseIndex + 3);
				indices.Add(baseIndex + 2);

				normals.Add(Vector3.Up);
				normals.Add(Vector3.Up);
				normals.Add(Vector3.Up);
				normals.Add(Vector3.Up);
			}
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		terrainMesh.Mesh = mesh;

		// Sync shader parameters from C# exports (overrides any value stored in .tscn)
		if (terrainMesh.GetActiveMaterial(0) is ShaderMaterial mat)
		{
			mat.SetShaderParameter("tile_size", TILE_SIZE);
			mat.SetShaderParameter("line_width", LineWidth);
			mat.SetShaderParameter("has_selection", false);
			mat.SetShaderParameter("selected_tile", new Vector2(-1, -1));
		}

        // Create collision
        var shape = mesh.CreateTrimeshShape();

        var collisionShape = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
        collisionShape.Shape = shape;
    }

	public void SelectTile(int x, int y)
	{
		if (terrainMesh?.GetActiveMaterial(0) is ShaderMaterial mat)
		{
			mat.SetShaderParameter("has_selection", true);
			mat.SetShaderParameter("selected_tile", new Vector2(x, y));
		}
	}

	public void DeselectTile()
	{
		if (terrainMesh?.GetActiveMaterial(0) is ShaderMaterial mat)
		{
			mat.SetShaderParameter("has_selection", false);
			mat.SetShaderParameter("selected_tile", new Vector2(-1, -1));
		}
	}

	public Tile GetTileFromPosition(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.X / TILE_SIZE);
		int y = Mathf.FloorToInt(pos.Z / TILE_SIZE);
		return GetTile(x, y);
	}

	public Tile GetTile(int x, int y)
	{
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return null;
		return tiles[x, y];
	}

	public Tile this[int x, int y]
	{
		get => GetTile(x, y);
	}
}
