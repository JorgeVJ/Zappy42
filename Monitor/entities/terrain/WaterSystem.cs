using Godot;

/// <summary>
/// Mar procedural alrededor del terreno.
/// </summary>
/// <remarks>
/// Sigue el mismo patrón que GrassSystem /
/// DecorationSystem: Node3D hijo de Terrain con un Generate(...) invocado desde
/// Terrain.GenerateTerrainMesh(). Crea un único plano de agua muy grande que se
/// recentra sobre la cámara cada frame para dar sensación de mar infinito; toda la
/// animación (caústicas, normales, espuma de costa) vive en water.gdshader y es
/// world-space, así que mover el plano no desplaza visualmente el patrón.
/// </remarks>
public partial class WaterSystem : Node3D
{
	/// <summary>
	/// Tamaño del quad de agua. Grande para que llegue al horizonte (efecto infinito).
	/// </summary>
	[Export]
	public float PlaneSize = 600f;

	/// <summary>
	/// Nivel del mar como fracción entre el mínimo y el máximo del heightMap.
	/// </summary>
	/// <remarks>
	/// 0 = al punto más bajo (no inunda nada), 1 = al más alto (lo inunda todo).
	/// ~0.35 inunda los valles → archipiélago fiable en cualquier tamaño de mapa.
	/// </remarks>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float SeaLevelFraction = 0.35f;

	/// <summary>
	/// Ajuste fino absoluto (unidades de mundo) sumado al nivel calculado.
	/// </summary>
	[Export]
	public float SeaLevelOffset = 0f;

	private MeshInstance3D _mesh;
	private float _seaY;

	private static readonly Shader WaterShader =
		ResourceLoader.Load<Shader>("res://entities/terrain/water.gdshader");

	/// <remarks>
	/// Sin colisión (igual que GrassSystem): los clics atraviesan el agua y siguen
	/// impactando el terreno, por lo que la selección de tile no cambia.
	/// </remarks>
	public override void _Ready()
	{
		_mesh = new MeshInstance3D
		{
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		AddChild(_mesh);
	}

	public void Generate(float[,] heightMap, int width, int height)
	{
		if (_mesh == null)
			return;

		_seaY = ComputeSeaLevel(heightMap) + SeaLevelOffset;

		_mesh.Mesh = new PlaneMesh
		{
			Size = new Vector2(PlaneSize, PlaneSize)
		};

		_mesh.MaterialOverride = new ShaderMaterial { Shader = WaterShader };

		float cx = width * Terrain.TILE_SIZE / 2f;
		float cz = height * Terrain.TILE_SIZE / 2f;
		_mesh.Position = new Vector3(cx, _seaY, cz);
	}

	/// <remarks>
	/// Recentra el plano en la cámara cada frame para que el mar siempre llegue al
	/// horizonte. El shader es world-space, así que deslizar el plano no desplaza el
	/// patrón, por lo que se ve continuo y sin bordes.
	/// </remarks>
	public override void _Process(double delta)
	{
		if (_mesh?.Mesh == null)
			return;

		Camera3D cam = GetViewport()?.GetCamera3D();
		if (cam == null)
			return;

		Vector3 camPos = cam.GlobalPosition;
		_mesh.GlobalPosition = new Vector3(camPos.X, _seaY, camPos.Z);
	}

	private float ComputeSeaLevel(float[,] heightMap)
	{
		if (heightMap == null || heightMap.Length == 0)
			return 0f;

		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (float h in heightMap)
		{
			if (h < min) min = h;
			if (h > max) max = h;
		}

		return Mathf.Lerp(min, max, SeaLevelFraction);
	}
}
