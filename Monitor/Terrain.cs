using Godot;
using System.Collections.Generic;

public partial class Terrain : Node3D
{
	[Export] public int Width = 10;
	[Export] public int Height = 10;
	
	[Export] public float HeightScale = 3f;
	[Export] public float NoiseScale = 0.08f;
    [Export] public float TileSize = 3.0f;

    private float[,] heightMap;

	private Tile[,] tiles;

	private MeshInstance3D terrainMesh;

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
			}
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
				Vector3 v0 = new Vector3(x * TileSize, heightMap[x, y], y * TileSize);
				Vector3 v1 = new Vector3((x + 1) * TileSize, heightMap[x + 1, y], y * TileSize);
				Vector3 v2 = new Vector3(x * TileSize, heightMap[x, y + 1], (y + 1) * TileSize);
				Vector3 v3 = new Vector3((x + 1) * TileSize, heightMap[x + 1, y + 1], (y + 1) * TileSize);

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
        // Create colision
        var shape = mesh.CreateTrimeshShape();

        var collisionShape = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
        collisionShape.Shape = shape;
    }

	public Tile GetTileFromPosition(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.X);
		int y = Mathf.FloorToInt(pos.Z);
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
