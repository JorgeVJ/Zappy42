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

    /// <summary>Orientación del jugador: 1=N, 2=E, 3=S, 4=W (convención Zappy).</summary>
    public int Orientation { get; private set; } = 1;
    public Vector2I TilePos { get; private set; } = new Vector2I(0, 0);

    /// <summary>Factor de velocidad derivado del time unit del servidor. 1 = normal.</summary>
    public float SpeedFactor { get; private set; } = 1f;

    /// <summary>Velocidad de steering, gestionada por CrowdSystem.</summary>
    public Vector3 Velocity;

    /// <summary>A partir de este factor de velocidad se corre en vez de andar.</summary>
    private const float RunThreshold = 3.0f;
    private const float MinSpeedFactor = 0.25f;
    private const float MaxSpeedFactor = 12.0f;

    /// <summary>Por debajo de esta velocidad horizontal, el jugador pasa a idle.</summary>
    private const float IdleSpeedThreshold = 0.15f;

    [Signal]
    public delegate void PlayerClickedEventHandler(Player player);

    public static Player Create(Vector3 pos)
    {
        Player instance = scene.Instantiate<Player>();
        instance.Position = pos;
        Log.Debug($"Player.Create: created instance at {pos}");
        return instance;
    }

    /// <remarks>
    /// El destino lógico es el tile; el desplazamiento real (steering hacia el centro
    /// del tile + separación de vecinos) lo conduce CrowdSystem. Durante el replay
    /// instantáneo de la barra de tiempo no hay frames entre mensajes para que
    /// CrowdSystem haga el steering, así que se clava la posición real al centro del
    /// tile para evitar un "viaje" visible al volver a Live.
    /// </remarks>
    public void SetTilePos(int x, int y)
    {
        TilePos = new Vector2I(x, y);

        if (Connection.ReplayInstant && _terrain != null)
        {
            GlobalPosition = TerrainSnap.TileCenter(_terrain, x, y, 0f);
            Velocity = Vector3.Zero;
        }
        Log.Debug($"SetTilePos: player {Id} new tile ({x},{y})");
    }

    /// <summary>
    /// Llamado por CrowdSystem cada frame con la velocidad horizontal actual: elige
    /// idle / andar / correr (correr cuando el time unit del servidor es alto).
    /// </summary>
    public void UpdateLocomotion(float speed)
    {
        if (speed < IdleSpeedThreshold)
            _shamanAnim?.PlayIdle();
        else if (SpeedFactor >= RunThreshold)
            _shamanAnim?.PlayRun();
        else
            _shamanAnim?.PlayWalk();
    }

    /// <summary>Animación de incantación (hechizo), delegada en el controlador del Shaman.</summary>
    public void PlaySpell()
    {
        _shamanAnim?.PlaySpell();
    }

    public void StopSpell()
    {
        _shamanAnim?.PlayIdle();
    }

    /// <remarks>
    /// Animación "one-shot" disparada al recoger un recurso: se reproduce una vez y,
    /// transcurrida su duración real, vuelve a Idle sola (a diferencia de
    /// PlaySpell/StopSpell, el servidor no envía un mensaje de "fin" para este gesto).
    /// </remarks>
    public void PlayCollect()
    {
        if (_shamanAnim == null)
            return;

        PlayOneShot(() => _shamanAnim.PlayCollect(), _shamanAnim.CollectDuration,
            () => _shamanAnim.IsPlayingCollect);
    }

    public void PlayPickUp()
    {
        if (_shamanAnim == null)
            return;

        PlayOneShot(() => _shamanAnim.PlayPickUp(), _shamanAnim.PickUpDuration,
            () => _shamanAnim.IsPlayingPickUp);
    }

    /// <remarks>
    /// Al terminar la espera, solo vuelve a Idle si el jugador sigue vivo y la animación
    /// one-shot sigue siendo la actual (si mientras tanto empezó otra, p. ej. una
    /// incantación o un movimiento, no se interrumpe).
    /// </remarks>
    private async void PlayOneShot(Action play, float duration, Func<bool> stillPlaying)
    {
        if (play == null)
            return;

        play();

        if (duration <= 0f)
            return;

        float speedScale = Mathf.Max(0.01f, SpeedFactor);
        float waitSeconds = duration / speedScale;

        await ToSignal(GetTree().CreateTimer(waitSeconds), "timeout");

        if (IsInstanceValid(this) && (stillPlaying?.Invoke() ?? false))
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
            AnimationPlayer ap = FindAnimationPlayer(modelNode);
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
            AnimationPlayer found = FindAnimationPlayer(child);
            if (found != null)
                return found;
        }

        return null;
    }

    public void SetTerrain(Terrain terrain)
    {
        _terrain = terrain;
    }

    /// <summary>Ajusta la velocidad de movimiento y de animación según el time unit del servidor.</summary>
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

    /// <summary>Applies the level's equipment loadout plus the orbiting gem group above the head.</summary>
    private void ApplyEquipment()
    {
        if (modelNode == null) 
            return;

        equipmentManager.ApplyLoadout(modelNode, 
            ShamanEquipmentConfig.GetLoadout(Level));

        OrbitingSlot orbitingSlot = new(
            "Head",
            ShamanEquipmentConfig.OrbitPivotOffsets,
            ShamanEquipmentConfig.OrbitRotationSpeedDeg,
            ShamanEquipmentConfig.GetOrbitingGems(Level));

        equipmentManager.AttachOrbitingGroup(modelNode, orbitingSlot);
    }

    /// <summary>Orientación Zappy: 1=N, 2=E, 3=S, 4=W.</summary>
    public void SetOrientation(int o)
    {
        Orientation = o;

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
