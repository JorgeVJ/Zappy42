using Godot;
using System;
using zappy;

public partial class Player : Node3D, ISelectable, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://player.tscn") as PackedScene;

    private MeshInstance3D mesh;

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
        }
    }

    public void SetTilePos(int x, int y)
    {
        TilePos = new Vector2I(x, y);
    }

    public override void _Ready()
    {
        mesh = GetNode<MeshInstance3D>("Mesh");
        inventory = GetNode<Inventory>("Inventory");
    }

    public void Init(int id, string teamName)
    {
        Id = id;
        TeamName = teamName;
        Name = $"Player_{id}";
    }

    public void SetLevel(int level)
    {
        Level = level;
    }

    public void SetOrientation(int o)
    {
        Orientation = o;

        // Zappy: 1=N, 2=E, 3=S, 4=W (normalmente)
        float yaw = o switch
        {
            1 => 0f,
            2 => Mathf.Pi / 2f,
            3 => Mathf.Pi,
            4 => -Mathf.Pi / 2f,
            _ => 0f
        };

        Rotation = new Vector3(0, yaw, 0);
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
    }

    public void UnHightlight()
    {
        mesh.MaterialOverlay = null;
    }
}
