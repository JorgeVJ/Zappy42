using Godot;
using System.Collections.Generic;

public partial class Terrain : Node3D
{
	[Export] public int Width = 50;
	[Export] public int Height = 50;
	[Export] public float HeightScale = 3f;
	[Export] public float NoiseScale = 0.08f;

	private float[,] heightMap;

	public override void _Ready()
	{
		GenerateHeightMap();
		GenerateMesh();
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

	void GenerateMesh()
	{
		var vertices = new List<Vector3>();
		var indices = new List<int>();
		var normals = new List<Vector3>();

		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				Vector3 v0 = new Vector3(x, heightMap[x, y], y);
				Vector3 v1 = new Vector3(x + 1, heightMap[x + 1, y], y);
				Vector3 v2 = new Vector3(x, heightMap[x, y + 1], y + 1);
				Vector3 v3 = new Vector3(x + 1, heightMap[x + 1, y + 1], y + 1);

				int baseIndex = vertices.Count;

				vertices.Add(v0);
				vertices.Add(v1);
				vertices.Add(v2);
				vertices.Add(v3);

				// Triangle 1
				indices.Add(baseIndex);
				indices.Add(baseIndex + 1);
				indices.Add(baseIndex + 2);

				// Triangle 2
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

		var meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		meshInstance.Mesh = mesh;

		// Create colision
		var shape = mesh.CreateTrimeshShape();

		var collisionShape = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
		collisionShape.Shape = shape;
	}

	public Vector2I GetTileFromPosition(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.X);
		int y = Mathf.FloorToInt(pos.Z);
		return new Vector2I(x, y);
	}
}
