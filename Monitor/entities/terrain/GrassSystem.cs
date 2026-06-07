using Godot;

public partial class GrassSystem : Node3D
{
	[Export] public int Density = 420;
	[Export] public float BladeHeight = 0.08f;
	[Export] public float BladeWidth = 0.06f;
	[Export] public float ScaleVariance = 0.3f;

	private MultiMeshInstance3D _mmi;

	private static readonly Shader GrassShader =
		ResourceLoader.Load<Shader>("res://entities/terrain/grass.gdshader");

	public override void _Ready()
	{
		_mmi = new MultiMeshInstance3D();
		AddChild(_mmi);
	}

	public void Generate(float[,] heightMap, int width, int height)
	{
		int count = Density * width * height;
		if (count == 0) return;

		var multiMesh = new MultiMesh();
		multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		multiMesh.Mesh = CreateGrassMesh();
		multiMesh.InstanceCount = count;

		var rng = new RandomNumberGenerator();
		rng.Seed = 42;

		float tileSize = Terrain.TILE_SIZE;
		float mapW = width  * tileSize;
		float mapD = height * tileSize;

		for (int i = 0; i < count; i++)
		{
			float worldX = rng.RandfRange(0f, mapW);
			float worldZ = rng.RandfRange(0f, mapD);

			float worldY = TerrainSnap.SampleHeight(heightMap, worldX, worldZ, tileSize, width, height);

			float scale = 1.0f + rng.RandfRange(-ScaleVariance, ScaleVariance);
			float rotY  = rng.RandfRange(0f, Mathf.Tau);

			var basis = new Basis(Vector3.Up, rotY).Scaled(new Vector3(scale, scale, scale));
			multiMesh.SetInstanceTransform(i, new Transform3D(basis, new Vector3(worldX, worldY, worldZ)));
		}

		_mmi.Multimesh = multiMesh;

		var mat = new ShaderMaterial();
		mat.Shader = GrassShader;
		mat.SetShaderParameter("map_width", mapW);
		mat.SetShaderParameter("map_depth", mapD);
		mat.SetShaderParameter("grass_texture", GenerateGrassTexture());
		_mmi.MaterialOverride = mat;
	}

	private Mesh CreateGrassMesh()
	{
		float h = BladeHeight;
		float hw = BladeWidth * 0.5f;

		var vertices = new Vector3[]
		{
			// Quad A (XY plane)
			new(-hw, 0, 0), new(hw, 0, 0), new(-hw, h, 0), new(hw, h, 0),
			// Quad B (ZY plane)
			new(0, 0, -hw), new(0, 0, hw), new(0, h, -hw), new(0, h, hw),
		};

		var uvs = new Vector2[]
		{
			new(0, 0), new(1, 0), new(0, 1), new(1, 1),
			new(0, 0), new(1, 0), new(0, 1), new(1, 1),
		};

		var normals = new Vector3[]
		{
			Vector3.Back, Vector3.Back, Vector3.Back, Vector3.Back,
			Vector3.Right, Vector3.Right, Vector3.Right, Vector3.Right,
		};

		var indices = new int[]
		{
			0, 1, 2,  1, 3, 2,
			4, 5, 6,  5, 7, 6,
		};

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	// Generates a 128x256 RGBA texture with 5 grass blade silhouettes.
	// py=0 = blade base (dark green, wide); py=255 = blade tip (light green, narrow).
	// If colors appear inverted in-engine, flip UV.y in the fragment shader.
	private static ImageTexture GenerateGrassTexture()
	{
		const int texW = 128;
		const int texH = 256;

		var image = Image.Create(texW, texH, false, Image.Format.Rgba8);
		image.Fill(new Color(0, 0, 0, 0));

		// (centerX normalized, tilt per bladeV unit, baseWidth normalized, tipWidth normalized, heightFrac)
		(float cx, float tilt, float baseW, float tipW, float hFrac)[] blades =
		{
			(0.10f, -0.05f, 0.060f, 0.010f, 0.70f),
			(0.28f,  0.04f, 0.055f, 0.010f, 0.88f),
			(0.50f, -0.03f, 0.065f, 0.012f, 1.00f),
			(0.72f,  0.05f, 0.055f, 0.010f, 0.80f),
			(0.90f, -0.04f, 0.050f, 0.010f, 0.72f),
		};

		var darkGreen  = new Color(0.12f, 0.38f, 0.08f);
		var lightGreen = new Color(0.42f, 0.80f, 0.24f);
		var transparent = new Color(0, 0, 0, 0);

		for (int py = 0; py < texH; py++)
		{
			// bladeV: 0 = base row (py=0), 1 = tip row (py=texH-1)
			float bladeV = (float)py / (texH - 1);

			for (int px = 0; px < texW; px++)
			{
				float nx = (float)px / texW;
				bool hit = false;

				foreach (var (cx, tilt, baseW, tipW, hFrac) in blades)
				{
					if (bladeV > hFrac) continue;

					float t        = bladeV / hFrac;               // 0=base, 1=tip of this blade
					float w        = Mathf.Lerp(baseW, tipW, t);
					float centerX  = cx + tilt * t;

					if (Mathf.Abs(nx - centerX) < w * 0.5f)
					{
						var color = darkGreen.Lerp(lightGreen, t);
						image.SetPixel(px, py, new Color(color.R, color.G, color.B, 1f));
						hit = true;
						break;
					}
				}

				if (!hit)
					image.SetPixel(px, py, transparent);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}
}
