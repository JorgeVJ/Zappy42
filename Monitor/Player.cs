using Godot;
using System;
using zappy;

public partial class Player : SelectableInventoryNode3D, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://player.tscn") as PackedScene;

    private Tween moveTween;

    private AnimationPlayer modelAnim;
    private AnimationPlayer droneAnim;

    private EquipmentManager equipmentManager;

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
        // If position did not change, skip movement
        if (TilePos.X == x && TilePos.Y == y)
        {
            GD.Print($"SetTilePos: player {Id} already at tile ({x},{y}), no move required");
            return;
        }

        // Update logical tile coordinates
        TilePos = new Vector2I(x, y);
        GD.Print($"SetTilePos: player {Id} new tile ({x},{y})");

        // Compute world target position from tile coords (center of tile)
        Vector3 target = new Vector3(
            x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f,
            0.3f,
            y * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f);

        // Try to cancel any previous tween
        try
        {
            moveTween?.Kill();
            GD.Print($"SetTilePos: previous tween killed for player {Id} (if any)");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SetTilePos: error killing previous tween for player {Id}: {ex.Message}");
        }

        // Start walk animation if available
        if (modelAnim != null)
        {
            string animName = "walking_2_inplace";
            if (modelAnim.HasAnimation(animName))
            {
                modelAnim.Play(animName);
                GD.Print($"SetTilePos: playing animation '{animName}' for player {Id}");
            }
            else
            {
                GD.Print($"SetTilePos: animation '{animName}' not found for player {Id}");
            }
        }
        else
        {
            GD.Print($"SetTilePos: no AnimationPlayer found for player {Id}");
        }

        // Create a new tween to animate position
        moveTween = CreateTween();
        float duration = 2.0f;
        GD.Print($"SetTilePos: starting tween for player {Id} to {target} duration {duration}s");
        moveTween.TweenProperty(this, "position", target, duration);
        moveTween.TweenCallback(Callable.From(() => OnMoveCompleted()));
    }

    private void OnMoveCompleted()
    {
        GD.Print($"OnMoveCompleted: movement finished for player {Id}");

        if (modelAnim == null)
        {
            GD.Print($"OnMoveCompleted: no AnimationPlayer to update for player {Id}");
            return;
        }

        if (modelAnim.HasAnimation("Idle_9"))
        {
            modelAnim.Play("Idle_9");
            GD.Print($"OnMoveCompleted: playing 'Idle_9' for player {Id}");
        }
        else
        {
            modelAnim.Stop();
            GD.Print($"OnMoveCompleted: stopped animation for player {Id} (no 'Idle_9')");
        }
    }

    public override void _Ready()
    {
        // Inicializaciones comunes (mesh, Inventory) en la clase base
        base._Ready();

        GD.Print($"_Ready: player node ready, Id placeholder = {Id}");

        equipmentManager = new EquipmentManager();

        var modelNode = GetNodeOrNull<Node3D>("Model");
        if (modelNode != null)
        {
            GD.Print("_Ready: searching for AnimationPlayer in Model node...");
            modelAnim = FindAnimationPlayer(modelNode);
            GD.Print(modelAnim != null ? "_Ready: AnimationPlayer found." : "_Ready: no AnimationPlayer found in Model node.");

            equipmentManager.ApplyLoadout(this, ShamanEquipmentConfig.GetLoadout(Level));
        }
        else
        {
            GD.Print("_Ready: no 'Model' node found as child of Player.");
        }

        var droneNode = GetNodeOrNull<Node3D>("Drone");
        if (droneNode != null)
        {
            GD.Print("_Ready: searching for AnimationPlayer in Drone node...");
            droneAnim = FindAnimationPlayer(droneNode);
            if (droneAnim != null)
            {
                GD.Print("_Ready: Drone AnimationPlayer found.");

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
            else
            {
                GD.Print("_Ready: no AnimationPlayer found in Drone node.");
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
        equipmentManager.ApplyLoadout(this, ShamanEquipmentConfig.GetLoadout(Level));
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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
