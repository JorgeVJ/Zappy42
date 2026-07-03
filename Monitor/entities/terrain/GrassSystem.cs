using Godot;

public partial class GrassSystem : Node3D
{
	[Export]
	public int Density = 420;

	[Export]
	public float BladeHeight = 0.08f;

	[Export]
	public float BladeWidth = 0.06f;

	[Export]
	public float ScaleVariance = 0.3f;

	private MultiMeshInstance3D _mmi;

	private static readonly Shader GrassShader =
		ResourceLoader.Load<Shader>("res://entities/terrain/grass.gdshader");

	/// <summary>
	/// Agrupa las dimensiones del mapa y el heightmap compartidos por los métodos
	/// invocados desde <see cref="Generate"/>, de forma que queden dentro del
	/// límite de 4 parámetros.
	/// </summary>
	private readonly record struct MapInfo(float[,] HeightMap, int Width, int Height, float TileSize);

	private readonly record struct BladeSpec(float Cx, float Tilt, float BaseW, float TipW, float HFrac);

	public override void _Ready()
	{
		_mmi = new MultiMeshInstance3D();
		AddChild(_mmi);
	}

	public void Generate(float[,] heightMap, int width, int height)
	{
		int count = Density * width * height;
		if (count == 0) return;

		MapInfo mapInfo = new MapInfo(heightMap, width, height, Terrain.TILE_SIZE);

		MultiMesh multiMesh = new MultiMesh();
		multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		multiMesh.Mesh = CreateGrassMesh();
		multiMesh.InstanceCount = count;

		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Seed = 42;

		PopulateInstanceTransforms(multiMesh, mapInfo, count, rng);

		_mmi.Multimesh = multiMesh;
		_mmi.MaterialOverride = BuildMaterial(mapInfo);
	}

	private void PopulateInstanceTransforms(MultiMesh multiMesh, MapInfo mapInfo, int count, RandomNumberGenerator rng)
	{
		float mapW = mapInfo.Width * mapInfo.TileSize;
		float mapD = mapInfo.Height * mapInfo.TileSize;

		for (int i = 0; i < count; i++)
		{
			float worldX = rng.RandfRange(0f, mapW);
			float worldZ = rng.RandfRange(0f, mapD);

			HeightMapGrid grid = new HeightMapGrid(mapInfo.Width, mapInfo.Height, mapInfo.TileSize);
			float worldY = TerrainSnap.SampleHeight(mapInfo.HeightMap, worldX, worldZ, grid);

			float scale = 1.0f + rng.RandfRange(-ScaleVariance, ScaleVariance);
			float rotY  = rng.RandfRange(0f, Mathf.Tau);

			Basis basis = new Basis(Vector3.Up, rotY).Scaled(new Vector3(scale, scale, scale));
			multiMesh.SetInstanceTransform(i, new Transform3D(basis, new Vector3(worldX, worldY, worldZ)));
		}
	}

	private ShaderMaterial BuildMaterial(MapInfo mapInfo)
	{
		float mapW = mapInfo.Width * mapInfo.TileSize;
		float mapD = mapInfo.Height * mapInfo.TileSize;

		ShaderMaterial mat = new ShaderMaterial();
		mat.Shader = GrassShader;
		mat.SetShaderParameter("map_width", mapW);
		mat.SetShaderParameter("map_depth", mapD);
		mat.SetShaderParameter("grass_texture", GenerateGrassTexture());
		return mat;
	}

	private Mesh CreateGrassMesh()
	{
		(Vector3[] vertices, Vector2[] uvs, Vector3[] normals, int[] indices) = BuildBladeArrays();

		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private (Vector3[] vertices, Vector2[] uvs, Vector3[] normals, int[] indices) BuildBladeArrays()
	{
		float h = BladeHeight;
		float hw = BladeWidth * 0.5f;

		Vector3[] vertices = new Vector3[]
		{
			new(-hw, 0, 0), new(hw, 0, 0), new(-hw, h, 0), new(hw, h, 0),
			new(0, 0, -hw), new(0, 0, hw), new(0, h, -hw), new(0, h, hw),
		};

		Vector2[] uvs = new Vector2[]
		{
			new(0, 0), new(1, 0), new(0, 1), new(1, 1),
			new(0, 0), new(1, 0), new(0, 1), new(1, 1),
		};

		Vector3[] normals = new Vector3[]
		{
			Vector3.Back, Vector3.Back, Vector3.Back, Vector3.Back,
			Vector3.Right, Vector3.Right, Vector3.Right, Vector3.Right,
		};

		int[] indices = new int[]
		{
			0, 1, 2,  1, 3, 2,
			4, 5, 6,  5, 7, 6,
		};

		return (vertices, uvs, normals, indices);
	}

	/// <summary>
	/// Genera una textura RGBA de 128x256 con 5 siluetas de briznas de hierba.
	/// </summary>
	/// <remarks>
	/// py=0 = base de la brizna (verde oscuro, ancha); py=255 = punta de la brizna (verde claro, estrecha).
	/// Si los colores aparecen invertidos en el motor, invertir UV.y en el fragment shader.
	/// </remarks>
	private static ImageTexture GenerateGrassTexture()
	{
		const int texW = 128;
		const int texH = 256;

		Image image = Image.Create(texW, texH, false, Image.Format.Rgba8);
		image.Fill(new Color(0, 0, 0, 0));

		BladeSpec[] blades = GetBladeSpecs();

		for (int py = 0; py < texH; py++)
		{
			DrawBladeRow(image, py, blades);
		}

		return ImageTexture.CreateFromImage(image);
	}

	private static BladeSpec[] GetBladeSpecs()
	{
		return new BladeSpec[]
		{
			new(0.10f, -0.05f, 0.060f, 0.010f, 0.70f),
			new(0.28f,  0.04f, 0.055f, 0.010f, 0.88f),
			new(0.50f, -0.03f, 0.065f, 0.012f, 1.00f),
			new(0.72f,  0.05f, 0.055f, 0.010f, 0.80f),
			new(0.90f, -0.04f, 0.050f, 0.010f, 0.72f),
		};
	}

	private static void DrawBladeRow(Image image, int py, BladeSpec[] blades)
	{
		int texW = image.GetWidth();
		int texH = image.GetHeight();
		Color transparent = new Color(0, 0, 0, 0);
		float bladeV = (float)py / (texH - 1);

		for (int px = 0; px < texW; px++)
		{
			float nx = (float)px / texW;

			if (TryGetBladeColor(nx, bladeV, blades, out Color color))
				image.SetPixel(px, py, color);
			else
				image.SetPixel(px, py, transparent);
		}
	}

	private static bool TryGetBladeColor(float nx, float bladeV, BladeSpec[] blades, out Color color)
	{
		Color darkGreen  = new Color(0.12f, 0.38f, 0.08f);
		Color lightGreen = new Color(0.42f, 0.80f, 0.24f);

		foreach (BladeSpec blade in blades)
		{
			if (bladeV > blade.HFrac) continue;

			float t        = bladeV / blade.HFrac;
			float w        = Mathf.Lerp(blade.BaseW, blade.TipW, t);
			float centerX  = blade.Cx + blade.Tilt * t;

			if (Mathf.Abs(nx - centerX) < w * 0.5f)
			{
				Color blended = darkGreen.Lerp(lightGreen, t);
				color = new Color(blended.R, blended.G, blended.B, 1f);
				return true;
			}
		}

		color = default;
		return false;
	}
}
