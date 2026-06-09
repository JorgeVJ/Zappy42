using Godot;
using System;
using zappy;

public partial class Player : SelectableInventoryNode3D, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://entities/player/player.tscn") as PackedScene;

    private Tween moveTween;

    private ShamanAnimationController _shamanAnim;
    private AnimationPlayer droneAnim;

    private EquipmentManager equipmentManager;
    private Node3D modelNode;
    private Terrain _terrain;

    public int Id { get; private set; }
    public string TeamName { get; private set; } = "";
    public int Level { get; private set; } = 1;
    public int Orientation { get; private set; } = 1; // 1..4 en Zappy
    public Vector2I TilePos { get; private set; } = new Vector2I(0, 0);

    [Signal]
    public delegate void PlayerClickedEventHandler(Player player);

    public static Player Create(Vector3 pos)
    {
        Player instance = scene.Instantiate<Player>();
        instance.Position = pos;
        GD.Print($"Player.Create: created instance at {pos}");
        return instance;
    }

    public void SetTilePos(int x, int y)
    {
        if (TilePos.X == x && TilePos.Y == y)
        {
            GD.Print($"SetTilePos: player {Id} already at tile ({x},{y}), no move required");
            return;
        }

        TilePos = new Vector2I(x, y);
        GD.Print($"SetTilePos: player {Id} new tile ({x},{y})");

        Vector3 target = TerrainSnap.TileCenter(_terrain, x, y, 0f);

        try
        {
            moveTween?.Kill();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SetTilePos: error killing previous tween for player {Id}: {ex.Message}");
        }

        _shamanAnim?.PlayWalk();

        moveTween = CreateTween();
        float duration = 2.0f;
        GD.Print($"SetTilePos: starting tween for player {Id} to {target} duration {duration}s");
        moveTween.TweenProperty(this, "position", target, duration);
        moveTween.TweenCallback(Callable.From(() => OnMoveCompleted()));
    }

    private void OnMoveCompleted()
    {
        GD.Print($"OnMoveCompleted: movement finished for player {Id}");
        _shamanAnim?.PlayIdle();
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

        GD.Print($"_Ready: player node ready, Id placeholder = {Id}");

        equipmentManager = new EquipmentManager();

        modelNode = GetNodeOrNull<Node3D>("Model");
        if (modelNode != null)
        {
            var ap = FindAnimationPlayer(modelNode);
            if (ap != null)
                _shamanAnim = new ShamanAnimationController(ap);

            equipmentManager.ApplyLoadout(modelNode, ShamanEquipmentConfig.GetLoadout(Level));
        }
        else
        {
            GD.Print("_Ready: no 'Model' node found as child of Player.");
        }

        var droneNode = GetNodeOrNull<Node3D>("Drone");
        if (droneNode != null)
        {
            droneAnim = FindAnimationPlayer(droneNode);
            if (droneAnim != null)
            {
                const string droneIdle = "ArmatureDrone|Dron_Idle_Bake2";
                var anim = droneAnim.GetAnimation(droneIdle);
                if (anim != null)
                {
                    anim.LoopMode = Animation.LoopModeEnum.Linear;
                    droneAnim.Play(droneIdle);
                    GD.Print($"_Ready: Drone playing '{droneIdle}' in loop.");
                }
                else
                {
                    GD.Print($"_Ready: Drone animation '{droneIdle}' not found.");
                }
            }
        }
        else
        {
            GD.Print("_Ready: no 'Drone' node found as child of Player.");
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

    public void Init(int id, string teamName)
    {
        Id = id;
        TeamName = teamName;
        Name = $"Player_{id}";
        GD.Print($"Init: player initialized Id={Id}, team={TeamName}");
    }

    public void SetLevel(int level)
    {
        Level = level;
        GD.Print($"SetLevel: player {Id} level set to {Level}");
        if (modelNode != null)
            equipmentManager.ApplyLoadout(modelNode, ShamanEquipmentConfig.GetLoadout(Level));
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
        GD.Print($"SetOrientation: player {Id} orientation set to {o} (yaw {yaw})");
    }

    public override void _Process(double delta)
    {
    }
}
