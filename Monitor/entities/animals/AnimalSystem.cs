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

	[Export]
	public float TileSize = 2.0f;

	/// <summary>Margen que el animal deja respecto al fondo del volumen navegable acuático.</summary>
	[Export]
	public float FloorMargin = 0.4f;

	/// <summary>Margen que el animal deja respecto a la superficie del volumen navegable acuático.</summary>
	[Export]
	public float SurfaceMargin = 0.4f;

	/// <summary>Margen de orilla: sólo tiles cuya altura supera nivel del mar + este margen cuentan como tierra.</summary>
	[Export]
	public float ShoreMargin = 0.2f;

	/// <summary>Perfil de los peces (spawn + huida). Si queda sin asignar, se usa un perfil por defecto.</summary>
	[Export]
	public AnimalProfile FishProfile;

	/// <summary>Perfil de las aves (spawn + vuelo). Si queda sin asignar, se usa un perfil por defecto.</summary>
	[Export]
	public AnimalProfile BirdProfile;

	/// <summary>Perfil de los zorros (spawn + caza). Si queda sin asignar, se usa un perfil por defecto.</summary>
	[Export]
	public AnimalProfile FoxProfile;

	private Node3D _container;
	private Node3D _birdContainer;
	private Node3D _foxContainer;

	public override void _Ready()
	{
		FishProfile ??= DefaultFishProfile();
		BirdProfile ??= DefaultBirdProfile();
		FoxProfile ??= DefaultFoxProfile();

		_container = new Node3D { Name = "Fishes" };
		AddChild(_container);
		_birdContainer = new Node3D { Name = "Birds" };
		AddChild(_birdContainer);
		_foxContainer = new Node3D { Name = "Foxes" };
		AddChild(_foxContainer);
	}

	/// <summary>Perfil por defecto de los peces (equivale a la configuración previa hardcodeada).</summary>
	private static AnimalProfile DefaultFishProfile()
	{
		return new AnimalProfile
		{
			Models = new string[] { "res://entities/animals/ClownFish.glb", "res://entities/animals/SurgeonFish.glb" },
			Count = 6,
			MaxSpeed = 1.6f,
			WanderRadius = 6f,
		};
	}

	/// <summary>Perfil por defecto de las aves (equivale a la configuración previa hardcodeada).</summary>
	private static AnimalProfile DefaultBirdProfile()
	{
		return new AnimalProfile
		{
			Models = new string[] { "res://entities/animals/Bird.glb" },
			Count = 3,
			MaxSpeed = 1.2f,
			WanderRadius = 6f,
		};
	}

	/// <summary>Perfil por defecto de los zorros (equivale a la configuración previa hardcodeada).</summary>
	private static AnimalProfile DefaultFoxProfile()
	{
		return new AnimalProfile
		{
			Models = new string[] { "res://entities/animals/Fox.glb" },
			Count = 3,
			MaxSpeed = 0.5f,
			WanderRadius = 6f,
		};
	}

	public void Generate(float[,] heightMap, int width, int height)
	{
		if (heightMap == null)
			return;

		ComputeHeightRange(heightMap, out float min, out float max);
		float seaY = Mathf.Lerp(min, max, SeaLevelFraction) + SeaLevelOffset;
		HeightMapGrid grid = new HeightMapGrid(width, height, TileSize);

		GenerateFish(heightMap, width, height, seaY, grid);
		GenerateBirds(heightMap, width, height, seaY, max, grid);
		GenerateFoxes(heightMap, width, height, seaY, grid);
	}

	/// <summary>Recolecta tiles de agua y reparte los peces en su volumen navegable.</summary>
	private void GenerateFish(float[,] heightMap, int width, int height, float seaY, HeightMapGrid grid)
	{
		if (_container == null || FishProfile.Models.Length == 0)
			return;

		foreach (Node child in _container.GetChildren())
			child.QueueFree();

		List<Vector2I> waterTiles = CollectWaterTiles(heightMap, width, height, seaY);
		if (waterTiles.Count == 0)
			return;

		NavigableMargins margins = new NavigableMargins(FloorMargin, SurfaceMargin);
		AquaticDomain domain = new AquaticDomain(heightMap, grid, seaY, margins);
		SpawnFish(heightMap, seaY, waterTiles, domain);
	}

	/// <summary>Recolecta tiles de tierra y reparte las aves, que caminan por tierra y vuelan sobre todo el mapa.</summary>
	private void GenerateBirds(float[,] heightMap, int width, int height, float seaY, float maxHeight, HeightMapGrid grid)
	{
		if (_birdContainer == null || BirdProfile.Models.Length == 0)
			return;

		foreach (Node child in _birdContainer.GetChildren())
			child.QueueFree();

		List<Vector2I> landTiles = CollectLandTiles(heightMap, width, height, seaY);
		if (landTiles.Count == 0)
			return;

		AerialBounds bounds = new AerialBounds(ShoreMargin, BirdProfile.MinFlyAltitude, maxHeight + BirdProfile.CeilingAltitude);
		AerialDomain domain = new AerialDomain(heightMap, grid, seaY, bounds);
		SpawnBirds(heightMap, landTiles, domain);
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
	/// Elige FishProfile.Count tiles de agua al azar y coloca un pez en cada uno, inyectando
	/// el dominio navegable compartido y el cerebro de Utility AI (pasear/huir).
	/// </summary>
	private void SpawnFish(float[,] heightMap, float seaY, List<Vector2I> waterTiles, AquaticDomain domain)
	{
		int count = Mathf.Min(FishProfile.Count, waterTiles.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2I tile = waterTiles[GD.RandRange(0, waterTiles.Count - 1)];
			Vector3 pos = domain.ClampToValid(new Vector3(
				tile.X * TileSize + TileSize / 2f,
				(seaY + GetTileHeight(heightMap, tile.X, tile.Y)) / 2f,
				tile.Y * TileSize + TileSize / 2f
			));

			string modelPath = FishProfile.Models[GD.RandRange(0, FishProfile.Models.Length - 1)];
			Fish fish = Fish.Create(pos, modelPath);
			fish.Domain = domain;
			fish.Locomotion.MaxSpeed = FishProfile.MaxSpeed;
			fish.Behavior = new UtilityBrain<Animal>(new IUtilityBehavior<Animal>[]
			{
				new AmbientLocomotionBehavior(new AmbientLocomotionBehavior.Gait[]
				{
					new AmbientLocomotionBehavior.Gait { State = "swim", SpeedScale = 1f, Moves = true, DwellMin = 3f, DwellMax = 7f },
					new AmbientLocomotionBehavior.Gait { State = "rest", SpeedScale = 1f, Moves = false, DwellMin = 1f, DwellMax = 3f },
				}) { WanderRadius = FishProfile.WanderRadius },
				new FleeBehavior { FleeInner = FishProfile.FleeInner, FleeOuter = FishProfile.FleeOuter, FleeSpeedScale = FishProfile.FleeSpeedScale },
			});
			_container.AddChild(fish);
		}
	}

	/// <summary>
	/// Recorre todos los tiles del mapa y devuelve los que quedan por encima del nivel
	/// del mar más el margen de orilla (candidatos válidos para colocar aves en tierra).
	/// </summary>
	private List<Vector2I> CollectLandTiles(float[,] heightMap, int width, int height, float seaY)
	{
		List<Vector2I> landTiles = new List<Vector2I>();
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if (GetTileHeight(heightMap, x, y) >= seaY + ShoreMargin)
					landTiles.Add(new Vector2I(x, y));
			}
		}

		return landTiles;
	}

	/// <summary>
	/// Elige BirdProfile.Count tiles de tierra al azar y coloca un ave en cada uno, inyectando el
	/// dominio aéreo compartido y el cerebro de Utility AI (caminar/volar).
	/// </summary>
	private void SpawnBirds(float[,] heightMap, List<Vector2I> landTiles, AerialDomain domain)
	{
		int count = Mathf.Min(BirdProfile.Count, landTiles.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2I tile = landTiles[GD.RandRange(0, landTiles.Count - 1)];
			Vector3 pos = new Vector3(
				tile.X * TileSize + TileSize / 2f,
				GetTileHeight(heightMap, tile.X, tile.Y),
				tile.Y * TileSize + TileSize / 2f
			);

			string modelPath = BirdProfile.Models[GD.RandRange(0, BirdProfile.Models.Length - 1)];
			Bird bird = Bird.Create(pos, modelPath);
			bird.AddToGroup(HuntBehavior.PreyGroup);
			bird.Aerial = domain;
			bird.Domain = domain;
			bird.Locomotion.MaxSpeed = BirdProfile.MaxSpeed;
			bird.Behavior = new UtilityBrain<Animal>(new IUtilityBehavior<Animal>[]
			{
				new AmbientLocomotionBehavior(new AmbientLocomotionBehavior.Gait[]
				{
					new AmbientLocomotionBehavior.Gait { State = "walk", SpeedScale = 1f, Moves = true, DwellMin = 2f, DwellMax = 4f },
					new AmbientLocomotionBehavior.Gait { State = "walk", SpeedScale = 1f, Moves = false, DwellMin = 1f, DwellMax = 2.5f },
				}) { WanderRadius = BirdProfile.WanderRadius, UseSurface = true },
				new FlyBehavior
				{
					FlyInner = BirdProfile.FlyInner,
					FlyOuter = BirdProfile.FlyOuter,
					FlySpeedScale = BirdProfile.FlySpeedScale,
					LandingSpeedScale = BirdProfile.LandingSpeedScale,
					FlyDwellMin = BirdProfile.FlyDwellMin,
					FlyDwellMax = BirdProfile.FlyDwellMax,
					WanderRadius = BirdProfile.WanderRadius,
				},
			});
			_birdContainer.AddChild(bird);
		}
	}

	/// <summary>Recolecta tiles de tierra y reparte los zorros, que se pasean por tierra alternando sus animaciones.</summary>
	private void GenerateFoxes(float[,] heightMap, int width, int height, float seaY, HeightMapGrid grid)
	{
		if (_foxContainer == null || FoxProfile.Models.Length == 0)
			return;

		foreach (Node child in _foxContainer.GetChildren())
			child.QueueFree();

		List<Vector2I> landTiles = CollectLandTiles(heightMap, width, height, seaY);
		if (landTiles.Count == 0)
			return;

		GroundDomain domain = new GroundDomain(heightMap, grid, seaY, ShoreMargin);
		SpawnFoxes(heightMap, landTiles, domain);
	}

	/// <summary>
	/// Elige FoxProfile.Count tiles de tierra al azar y coloca un zorro en cada uno, inyectando el dominio
	/// terrestre compartido y el cerebro de Utility AI que alterna quieto/caminar/correr.
	/// </summary>
	private void SpawnFoxes(float[,] heightMap, List<Vector2I> landTiles, GroundDomain domain)
	{
		int count = Mathf.Min(FoxProfile.Count, landTiles.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2I tile = landTiles[GD.RandRange(0, landTiles.Count - 1)];
			Vector3 pos = new Vector3(
				tile.X * TileSize + TileSize / 2f,
				GetTileHeight(heightMap, tile.X, tile.Y),
				tile.Y * TileSize + TileSize / 2f
			);

			string modelPath = FoxProfile.Models[GD.RandRange(0, FoxProfile.Models.Length - 1)];
			Fox fox = Fox.Create(pos, modelPath);
			fox.Domain = domain;
			fox.Locomotion.MaxSpeed = FoxProfile.MaxSpeed;
			fox.Context.PreyGroup = HuntBehavior.PreyGroup;
			fox.Context.PreyDetectRange = FoxProfile.HuntDetectRange;
			fox.Context.MaxPreyAltitude = FoxProfile.MaxPreyAltitude;
			fox.Behavior = BuildFoxBrain();
			_foxContainer.AddChild(fox);
		}
	}

	/// <summary>
	/// Construye el cerebro del zorro: un paseo ambiental que alterna quieto/caminar/correr con
	/// dwell aleatorio, más la caza (acecha y ataca aves cercanas), que gana cuando hay presa. La
	/// detección de presas (grupo/rango/altura) se configura en el blackboard del zorro.
	/// </summary>
	private UtilityBrain<Animal> BuildFoxBrain()
	{
		return new UtilityBrain<Animal>(new IUtilityBehavior<Animal>[]
		{
			new AmbientLocomotionBehavior(new AmbientLocomotionBehavior.Gait[]
			{
				new AmbientLocomotionBehavior.Gait { State = "idle", SpeedScale = 1f, Moves = false, DwellMin = 2f, DwellMax = 5f },
				new AmbientLocomotionBehavior.Gait { State = "walk", SpeedScale = 1f, Moves = true, DwellMin = 3f, DwellMax = 7f },
				new AmbientLocomotionBehavior.Gait { State = "run", SpeedScale = FoxProfile.RunSpeedScale, Moves = true, DwellMin = 2f, DwellMax = 5f },
			}) { WanderRadius = FoxProfile.WanderRadius },
			new HuntBehavior
			{
				AttackRange = FoxProfile.HuntAttackRange,
				HuntWeight = FoxProfile.HuntWeight,
				HuntSpeedScale = FoxProfile.HuntSpeedScale,
				RecoverTime = FoxProfile.HuntRecoverTime,
			},
		});
	}

	private static float GetTileHeight(float[,] heightMap, int x, int y)
	{
		return (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
	}

	private static void ComputeHeightRange(float[,] heightMap, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;
		foreach (float h in heightMap)
		{
			if (h < min) min = h;
			if (h > max) max = h;
		}
	}
}
