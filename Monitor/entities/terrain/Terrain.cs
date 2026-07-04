using Godot;
using System.Collections.Generic;

public partial class Terrain : Node3D
{
	[Export]
	public int Width = 5;

	[Export]
	public int Height = 5;

	[Export]
	public float HeightScale = 6f;

	[Export]
	public float NoiseScale = 0.08f;

	/// <summary>
	/// Anillos de casillas extra generados alrededor del grid jugable.
	/// </summary>
	/// <remarks>
	/// No son seleccionables (coordenadas fuera de rango → GetTile devuelve null) y descienden bajo el nivel del mar
	/// (falloff de isla) para ocultar el borde flotante de la malla sobre el agua.
	/// </remarks>
	[Export]
	public int BorderMargin = 4;

	/// <summary>
	/// Cuánto baja el borde exterior del margen por debajo del mínimo del terreno jugable.
	/// </summary>
	/// <remarks>
	/// Como el nivel del mar = lerp(min, max, 0.35) ≥ min, restar SkirtDepth garantiza que el
	/// borde exterior quede sumergido.
	/// </remarks>
	[Export]
	public float SkirtDepth = 5f;

	private float _lineWidth = 0.01f;

	[Export]
	public float LineWidth
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

	/// <summary>
	/// Ruido reutilizado por GenerateHeightMap y CornerHeight (esquinas del margen).
	/// </summary>
	private FastNoiseLite _noise;

	/// <summary>
	/// Mínimo del heightMap jugable (mismo min que usa WaterSystem para el nivel del mar).
	/// </summary>
	private float _minHeight;

	private Tile[,] tiles;

	private MeshInstance3D terrainMesh;
	private GrassSystem _grassSystem;
	private DecorationSystem _decorationSystem;
	private WaterSystem _waterSystem;
	private AnimalSystem _animalSystem;

	private static readonly PackedScene resourceScene = ResourceLoader.Load<PackedScene>("res://entities/resources/resource.tscn");
	private readonly Dictionary<(int, int), List<Resource>> tileResources = new();

	/// <summary>
	/// Half-extent of the area within a tile where resources can be placed (TILE_SIZE / 2 minus a margin).
	/// </summary>
	private const float ResourcePlacementRange = 0.7f;

	/// <summary>
	/// Vertical offset above the tile surface so resources don't z-fight with the ground mesh.
	/// </summary>
	public const float ResourceGroundOffset = 0.05f;

	/// <summary>
	/// Vertical offset above the tile surface used by entities (players, eggs) and effects
	/// (sound waves, incantation pulses) positioned via TerrainSnap.TileCenter.
	/// </summary>
	public const float EntityGroundOffset = 0.15f;

	/// <summary>
	/// Radio aproximado (en unidades de mundo) ocupado por un recurso, usado para evitar
	/// que su offset dentro del tile lo deje embebido en una decoración cercana (C12).
	/// </summary>
	private const float ResourceRadius = 0.25f;

	/// <summary>
	/// Distancia (en unidades de mundo) hasta la que una decoración se considera "cercana"
	/// a un tile y por tanto relevante como obstáculo al posicionar sus recursos.
	/// </summary>
	/// <remarks>
	/// Cubre el propio tile más un margen para decoraciones de tiles vecinos cuyo footprint invada
	/// el tile actual.
	/// </remarks>
	private const float DecorationProximityRange = TILE_SIZE * 1.5f;

	public override void _Ready()
	{
		terrainMesh = GetNode<MeshInstance3D>("MeshInstance3D");
		_grassSystem = GetNodeOrNull<GrassSystem>("GrassSystem");
		_decorationSystem = GetNodeOrNull<DecorationSystem>("DecorationSystem");
		_waterSystem = GetNodeOrNull<WaterSystem>("WaterSystem");
		_animalSystem = GetNodeOrNull<AnimalSystem>("AnimalSystem");
	}

	/// <summary>Muestra u oculta el mar procedural (WaterSystem).</summary>
	public void SetWaterEnabled(bool enabled)
	{
		if (_waterSystem != null)
			_waterSystem.Visible = enabled;
	}

	/// <summary>Muestra u oculta la decoración estática (árboles, rocas, arbustos) y el césped procedural.</summary>
	public void SetDecorationsEnabled(bool enabled)
	{
		if (_decorationSystem != null)
			_decorationSystem.Visible = enabled;
		if (_grassSystem != null)
			_grassSystem.Visible = enabled;
	}

	/// <summary>Muestra u oculta la fauna (peces y aves) y pausa su IA al ocultarla.</summary>
	public void SetAnimalsEnabled(bool enabled)
	{
		if (_animalSystem == null)
			return;
		_animalSystem.Visible = enabled;
		_animalSystem.ProcessMode = enabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}

	public void InitializeMap(int width, int height)
	{
		Reset();

		Width = width;
		Height = height;

		CreateTiles();
		GenerateHeightMap();
		GenerateTerrainMesh();
	}

	/// <summary>
	/// Libera los recursos sobre el terreno y limpia tileResources para que
	/// InitializeMap() (msz) pueda volver a llamarse de forma segura.
	/// </summary>
	/// <remarks>
	/// Lo usa TimelineController al resetear el mundo para reproducir el log desde 0.
	/// </remarks>
	public void Reset()
	{
		foreach (List<Resource> list in tileResources.Values)
			foreach (Resource r in list)
				r.QueueFree();
		tileResources.Clear();
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
		foreach (Resource r in tileResources[(x, y)])
			r.QueueFree();
		tileResources[(x, y)].Clear();

		float h = (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
		Vector3 center = new Vector3(x * TILE_SIZE + TILE_SIZE / 2f, h, y * TILE_SIZE + TILE_SIZE / 2f);

		List<PlacementFinder.Obstacle> obstacles = GetNearbyDecorationObstacles(x, y);
		ResourceOffsetContext offsetContext = new ResourceOffsetContext(x, y, center, obstacles);

		foreach (KeyValuePair<Resource.ResourceType, int> kvp in tiles[x, y].Inventory.AllOrdered)
		{
			if (kvp.Value <= 0) continue;

			Vector2 offset = GetResourceOffset(offsetContext, kvp.Key);
			Resource resource = resourceScene.Instantiate<Resource>();
			resource.Position = center + new Vector3(offset.X, ResourceGroundOffset, offset.Y);
			AddChild(resource);
			resource.SetResourceType(kvp.Key);
			tileResources[(x, y)].Add(resource);
		}
	}

	/// <summary>
	/// Filtra, de entre todos los obstáculos expuestos por DecorationSystem, los que caen
	/// en o cerca del tile (x, y) — es decir, relevantes para el offset de sus recursos.
	/// </summary>
	private List<PlacementFinder.Obstacle> GetNearbyDecorationObstacles(int x, int y)
	{
		List<PlacementFinder.Obstacle> result = new List<PlacementFinder.Obstacle>();

		IReadOnlyList<PlacementFinder.Obstacle> allObstacles = _decorationSystem?.Obstacles;
		if (allObstacles == null || allObstacles.Count == 0)
			return result;

		Vector2 tileCenter = new Vector2(x * TILE_SIZE + TILE_SIZE / 2f, y * TILE_SIZE + TILE_SIZE / 2f);

		foreach (PlacementFinder.Obstacle obstacle in allObstacles)
		{
			float maxDist = DecorationProximityRange + obstacle.Radius;
			if (tileCenter.DistanceSquaredTo(obstacle.PositionXZ) <= maxDist * maxDist)
				result.Add(obstacle);
		}

		return result;
	}

	/// <summary>
	/// Posición pseudoaleatoria dentro del tile, sembrada por (x, y, tipo) para que sea
	/// determinista: no cambia entre actualizaciones de inventario (sin parpadeos), pero
	/// varía entre tiles y tipos en lugar de repetir siempre el mismo patrón (C9).
	/// </summary>
	/// <remarks>
	/// Evita además colisionar con decoraciones (árboles/rocas/arbustos) cercanas (C12),
	/// usando PlacementFinder con el mismo RNG sembrado para mantener el determinismo.
	/// </remarks>
	private static Vector2 GetResourceOffset(ResourceOffsetContext context, Resource.ResourceType type)
	{
		uint seed = (uint)(context.X * 73856093) ^ (uint)(context.Y * 19349663) ^ (uint)((int)type * 83492791);
		RandomNumberGenerator rng = new RandomNumberGenerator();
		rng.Seed = seed;

		Vector2 centerXZ = new Vector2(context.Center.X, context.Center.Z);
		PlacementFinder.PlacementQuery query = new(ResourcePlacementRange, context.Obstacles, ResourceRadius, rng);
		return PlacementFinder.FindFreeOffset(centerXZ, query);
	}

	private void GenerateHeightMap()
	{
		heightMap = new float[Width + 1, Height + 1];

		_noise = new FastNoiseLite();
		_noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_noise.Frequency = NoiseScale;

		_minHeight = float.MaxValue;

		for (int x = 0; x <= Width; x++)
		{
			for (int y = 0; y <= Height; y++)
			{
				float n = _noise.GetNoise2D(x, y);
				float h = n * HeightScale;
				heightMap[x, y] = h;
				if (h < _minHeight) _minHeight = h;
			}
		}
	}

	/// <summary>
	/// Altura de la esquina (cx, cy) del grid extendido.
	/// </summary>
	/// <remarks>
	/// Para esquinas dentro de la región jugable [0..Width, 0..Height] devuelve el valor exacto de heightMap (costura sin grietas
	/// y alturas de juego intactas). Para esquinas del margen mezcla la altura natural de ruido
	/// hacia outerY (bajo el nivel del mar) según un smoothstep de cuán fuera están del grid
	/// jugable → falloff de isla que esconde el borde flotante bajo el agua.
	/// </remarks>
	private float CornerHeight(int cx, int cy)
	{
		if (cx >= 0 && cx <= Width && cy >= 0 && cy <= Height)
			return heightMap[cx, cy];

		int outX = Mathf.Max(Mathf.Max(-cx, cx - Width), 0);
		int outY = Mathf.Max(Mathf.Max(-cy, cy - Height), 0);
		float t = Mathf.Clamp((float)Mathf.Max(outX, outY) / BorderMargin, 0f, 1f);

		float natural = _noise.GetNoise2D(cx, cy) * HeightScale;
		float outerY = _minHeight - SkirtDepth;
		return Mathf.Lerp(natural, outerY, Mathf.SmoothStep(0f, 1f, t));
	}

	/// <summary>
	/// Construye la malla del terreno (incluyendo el margen de borde con falloff de isla),
	/// sincroniza el shader, regenera la colisión y dispara la generación de los
	/// sistemas dependientes (hierba, decoración, agua, animales).
	/// </summary>
	private void GenerateTerrainMesh()
	{
		ArrayMesh mesh = BuildTerrainMesh();
		terrainMesh.Mesh = mesh;

		SyncShaderParameters();
		RebuildCollision(mesh);

		_grassSystem?.Generate(heightMap, Width, Height);
		_decorationSystem?.Generate(heightMap, Width, Height);
		_waterSystem?.Generate(heightMap, Width, Height);
		_animalSystem?.Generate(heightMap, Width, Height);
	}

	/// <summary>
	/// Genera los vértices/índices/normales del grid extendido (±BorderMargin alrededor de
	/// la región jugable) y los empaqueta en un ArrayMesh.
	/// </summary>
	/// <remarks>
	/// Las casillas del margen no son seleccionables (coordenadas fuera de rango) y
	/// CornerHeight las hace descender bajo el agua para ocultar el borde flotante de la malla.
	/// </remarks>
	private ArrayMesh BuildTerrainMesh()
	{
		MeshBuffers buffers = new MeshBuffers(new List<Vector3>(), new List<int>(), new List<Vector3>());

		for (int x = -BorderMargin; x < Width + BorderMargin; x++)
		{
			for (int y = -BorderMargin; y < Height + BorderMargin; y++)
			{
				AddTileQuad(x, y, buffers);
			}
		}

		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);

		arrays[(int)Mesh.ArrayType.Vertex] = buffers.Vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = buffers.Indices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = buffers.Normals.ToArray();

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	/// <summary>
	/// Añade los 4 vértices y 2 triángulos correspondientes a la casilla (x, y) del
	/// grid extendido a las listas de la malla en construcción.
	/// </summary>
	private void AddTileQuad(int x, int y, MeshBuffers buffers)
	{
		Vector3 v0 = new Vector3(x * TILE_SIZE, CornerHeight(x, y), y * TILE_SIZE);
		Vector3 v1 = new Vector3((x + 1) * TILE_SIZE, CornerHeight(x + 1, y), y * TILE_SIZE);
		Vector3 v2 = new Vector3(x * TILE_SIZE, CornerHeight(x, y + 1), (y + 1) * TILE_SIZE);
		Vector3 v3 = new Vector3((x + 1) * TILE_SIZE, CornerHeight(x + 1, y + 1), (y + 1) * TILE_SIZE);

		int baseIndex = buffers.Vertices.Count;

		buffers.Vertices.Add(v0);
		buffers.Vertices.Add(v1);
		buffers.Vertices.Add(v2);
		buffers.Vertices.Add(v3);

		buffers.Indices.Add(baseIndex);
		buffers.Indices.Add(baseIndex + 1);
		buffers.Indices.Add(baseIndex + 2);

		buffers.Indices.Add(baseIndex + 1);
		buffers.Indices.Add(baseIndex + 3);
		buffers.Indices.Add(baseIndex + 2);

		buffers.Normals.Add(Vector3.Up);
		buffers.Normals.Add(Vector3.Up);
		buffers.Normals.Add(Vector3.Up);
		buffers.Normals.Add(Vector3.Up);
	}

	/// <summary>
	/// Sincroniza los parámetros del shader de terreno con los valores actuales de los
	/// exports de C# (sobrescribe cualquier valor guardado en el .tscn).
	/// </summary>
	private void SyncShaderParameters()
	{
		if (terrainMesh.GetActiveMaterial(0) is ShaderMaterial mat)
		{
			mat.SetShaderParameter("tile_size", TILE_SIZE);
			mat.SetShaderParameter("line_width", LineWidth);
			mat.SetShaderParameter("has_selection", false);
			mat.SetShaderParameter("selected_tile", new Vector2(-1, -1));
		}
	}

	private void RebuildCollision(ArrayMesh mesh)
	{
		Shape3D shape = mesh.CreateTrimeshShape();

		CollisionShape3D collisionShape = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
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

	/// <summary>
	/// Devuelve la casilla (x, y), o null si está fuera de rango o si el mapa aún no se ha inicializado.
	/// </summary>
	/// <remarks>
	/// tiles es null hasta que InitializeMap() (msz) lo crea: si llega algún
	/// mensaje del protocolo antes de msz, devuelve null en vez de lanzar
	/// NullReferenceException.
	/// </remarks>
	public Tile GetTile(int x, int y)
	{
		if (tiles == null)
			return null;
		if (x < 0 || x >= Width || y < 0 || y >= Height)
			return null;
		return tiles[x, y];
	}

	public Tile this[int x, int y]
	{
		get => GetTile(x, y);
	}

	public float GetTileHeight(int tileX, int tileY)
	{
		if (heightMap == null) return 0f;
		tileX = Mathf.Clamp(tileX, 0, Width - 1);
		tileY = Mathf.Clamp(tileY, 0, Height - 1);
		return (heightMap[tileX + 1, tileY] + heightMap[tileX, tileY + 1]) / 2f;
	}
}
