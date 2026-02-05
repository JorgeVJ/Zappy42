using Godot;
using System;
using zappy;

public partial class Player : Node3D, ISelectable, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://player.tscn") as PackedScene;

    private MeshInstance3D mesh;

    private Tween moveTween;

    private AnimationPlayer creatureAnim;

    private EquipmentManager equipmentManager;

    public int Id { get; private set; }
    public string TeamName { get; private set; } = "";
    public int Level { get; private set; } = 1;
    public int Orientation { get; private set; } = 1; // 1..4 en Zappy
    public Vector2I TilePos { get; private set; } = new Vector2I(0, 0);

    private Inventory inventory;
    public Inventory Inventory => inventory ??= GetNode<Inventory>("Inventory");

    [Signal]
    public delegate void PlayerClickedEventHandler(Player player);

    public static Player Create(Vector3 pos)
    {
        Player instance = scene.Instantiate<Player>();
        instance.Position = pos;
        GD.Print($"Player.Create: created instance at {pos}");
        return instance;
    }

    private void _on_area_3d_input_event(
    Node camera,
    InputEvent @event,
    Vector3 position,
    Vector3 normal,
    int shapeIdx)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(nameof(PlayerClicked), this);
            GD.Print($"Player._on_area_3d_input_event: player {Id} clicked");
        }
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

        // Compute world target position from tile coords
        Vector3 target = new Vector3(x * 2, 0.3f, y * 2);

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

        // Start creature animation if available
        if (creatureAnim != null)
        {
            if (creatureAnim.HasAnimation("ArmatureAction"))
            {
                creatureAnim.Play("ArmatureAction");
                GD.Print($"SetTilePos: playing animation 'ArmatureAction' for player {Id}");
            }
            else
            {
                GD.Print($"SetTilePos: animation 'ArmatureAction' not found for player {Id}");
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

        if (creatureAnim == null)
        {
            GD.Print($"OnMoveCompleted: no AnimationPlayer to update for player {Id}");
            return;
        }

        // Prefer to switch to "Idle" if it exists, otherwise stop the animation
        if (creatureAnim.HasAnimation("Idle"))
        {
            creatureAnim.Play("Idle");
            GD.Print($"OnMoveCompleted: playing 'Idle' for player {Id}");
        }
        else
        {
            creatureAnim.Stop();
            GD.Print($"OnMoveCompleted: stopped animation for player {Id} (no 'Idle')");
        }
    }

    public override void _Ready()
    {
        mesh = GetNode<MeshInstance3D>("Mesh");
        inventory = GetNode<Inventory>("Inventory");

        GD.Print($"_Ready: player node ready, Id placeholder = {Id}");

        equipmentManager = new EquipmentManager();
        equipmentManager.RegisterScene("armor", "res://ArmorLvl1.glb");

        var creatureNode = GetNodeOrNull<Node3D>("Creature");
        if (creatureNode != null)
        {
            GD.Print("Searching for AnimationPlayer in Creature node...");
            creatureAnim = FindAnimationPlayer(creatureNode);
            if (creatureAnim != null)
            {
                GD.Print("AnimationPlayer found!");
            }
            else
            {
                GD.Print("No AnimationPlayer found in Creature node.");
            }

            equipmentManager.AttachToBone(this, "BackArm2.R", "armor");
            equipmentManager.AttachToBone(this, "BackArm2.L", "armor", Offsets.Rotation(0, -90, 0));
            equipmentManager.AttachToBone(this, "FrontArm2.R", "armor");
            equipmentManager.AttachToBone(this, "FrontArm2.L", "armor", Offsets.Rotation(0, -90, 0));
        }
        else
        {
            GD.Print("No Creature node found as child of Player.");
        }
    }

    private AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer ap)
        {
            return ap;
        }

        foreach (Node child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null)
            {
                return found;
            }
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
    }

    public void SetOrientation(int o)
    {
        Orientation = o;

        // Zappy: 1=N, 2=E, 3=S, 4=W (normal)
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

    public void Highlight()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.DarkCyan;
        mesh.MaterialOverlay = mat;
        GD.Print($"Highlight: player {Id} highlighted");
    }

    public void UnHightlight()
    {
        mesh.MaterialOverlay = null;
        GD.Print($"UnHightlight: player {Id} unhighlighted");
    }
}
