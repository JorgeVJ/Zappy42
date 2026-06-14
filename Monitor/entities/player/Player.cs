using Godot;
using System;
using zappy;

public partial class Player : SelectableInventoryNode3D, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://entities/player/player.tscn") as PackedScene;

    private ShamanAnimationController _shamanAnim;

    private EquipmentManager equipmentManager;
    private Node3D modelNode;
    private Terrain _terrain;

    public int Id { get; private set; }
    public string TeamName { get; private set; } = "";
    public int Level { get; private set; } = 1;
    public int Orientation { get; private set; } = 1; // 1..4 en Zappy
    public Vector2I TilePos { get; private set; } = new Vector2I(0, 0);

    // Factor de velocidad derivado del time unit del servidor (D1). 1 = normal.
    public float SpeedFactor { get; private set; } = 1f;

    // Velocidad de steering, gestionada por CrowdSystem (D3).
    public Vector3 Velocity;

    private const float RunThreshold       = 3.0f;  // a partir de este factor se corre en vez de andar
    private const float MinSpeedFactor     = 0.25f;
    private const float MaxSpeedFactor     = 12.0f;
    private const float IdleSpeedThreshold = 0.15f; // por debajo de esta velocidad horizontal -> idle

    [Signal]
    public delegate void PlayerClickedEventHandler(Player player);

    public static Player Create(Vector3 pos)
    {
        Player instance = scene.Instantiate<Player>();
        instance.Position = pos;
        Log.Debug($"Player.Create: created instance at {pos}");
        return instance;
    }

    public void SetTilePos(int x, int y)
    {
        // El destino lógico es el tile; el desplazamiento real (steering hacia el
        // centro del tile + separación de vecinos) lo conduce CrowdSystem (D3).
        TilePos = new Vector2I(x, y);

        // Durante el replay instantáneo de la barra de tiempo no hay frames
        // entre mensajes para que CrowdSystem haga el steering: clavar la
        // posición real al centro del tile para evitar un "viaje" visible al
        // volver a Live.
        if (Connection.ReplayInstant && _terrain != null)
        {
            GlobalPosition = TerrainSnap.TileCenter(_terrain, x, y, 0f);
            Velocity = Vector3.Zero;
        }
        Log.Debug($"SetTilePos: player {Id} new tile ({x},{y})");
    }

    // Llamado por CrowdSystem cada frame con la velocidad horizontal actual: elige
    // idle / andar / correr (correr cuando el time unit del servidor es alto).
    public void UpdateLocomotion(float speed)
    {
        if (speed < IdleSpeedThreshold)
            _shamanAnim?.PlayIdle();
        else if (SpeedFactor >= RunThreshold)
            _shamanAnim?.PlayRun();
        else
            _shamanAnim?.PlayWalk();
    }

    // Animación de incantación (hechizo). Delegan en el controlador del Shaman,
    // igual que SetTilePos usa PlayWalk/PlayIdle.
    public void PlaySpell()
    {
        _shamanAnim?.PlaySpell();
    }

    public void StopSpell()
    {
        _shamanAnim?.PlayIdle();
    }

    public override void _Ready()
    {
        base._Ready();

        Log.Debug($"_Ready: player node ready, Id placeholder = {Id}");

        equipmentManager = new EquipmentManager();

        modelNode = GetNodeOrNull<Node3D>("Model");
        if (modelNode != null)
        {
            var ap = FindAnimationPlayer(modelNode);
            if (ap != null)
                _shamanAnim = new ShamanAnimationController(ap);

            ApplyEquipment();
        }
        else
        {
            Log.Debug("_Ready: no 'Model' node found as child of Player.");
        }
    }

    private AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer ap)
            return ap;

        foreach (Node child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null)
                return found;
        }

        return null;
    }

    public void SetTerrain(Terrain terrain)
    {
        _terrain = terrain;
    }

    // Ajusta la velocidad de movimiento y de animación según el time unit del servidor.
    public void SetSpeedFactor(float factor)
    {
        SpeedFactor = Mathf.Clamp(factor, MinSpeedFactor, MaxSpeedFactor);
        _shamanAnim?.SetSpeedScale(SpeedFactor);
    }

    public void Init(int id, string teamName)
    {
        Id = id;
        TeamName = teamName;
        Name = $"Player_{id}";
        Log.Debug($"Init: player initialized Id={Id}, team={TeamName}");
    }

    public void SetLevel(int level)
    {
        Level = level;
        Log.Debug($"SetLevel: player {Id} level set to {Level}");
        ApplyEquipment();
    }

    // Applies the level's equipment loadout plus the orbiting gem group above the head.
    private void ApplyEquipment()
    {
        if (modelNode == null) 
            return;

        equipmentManager.ApplyLoadout(modelNode, 
            ShamanEquipmentConfig.GetLoadout(Level));

        equipmentManager.AttachOrbitingGroup(modelNode, 
            "Head", 
            ShamanEquipmentConfig.OrbitPivotOffsets, 
            ShamanEquipmentConfig.OrbitRotationSpeedDeg, 
            ShamanEquipmentConfig.GetOrbitingGems(Level));
    }

    public void SetOrientation(int o)
    {
        Orientation = o;

        // Zappy: 1=N, 2=E, 3=S, 4=W
        float yaw = o switch
        {
            1 => 0f,
            2 => Mathf.Pi / 2f,
            3 => Mathf.Pi,
            4 => -Mathf.Pi / 2f,
            _ => 0f
        };

        Rotation = new Vector3(0, yaw, 0);
        Log.Debug($"SetOrientation: player {Id} orientation set to {o} (yaw {yaw})");
    }
}
