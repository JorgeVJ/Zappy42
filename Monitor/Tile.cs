using Godot;
using System;
using zappy;

public partial class Tile : Node3D, ISelectable, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://tile.tscn") as PackedScene;

    private Node3D resourceContainer;

    private MeshInstance3D mesh;

    [Signal]
    public delegate void TileClickedEventHandler(Tile tile);

    private Inventory inventory;
    public Inventory Inventory => inventory ??= GetNode<Inventory>("Inventory");

    public static Tile Create(Vector3 pos)
    {
        Tile tile = scene.Instantiate<Tile>();
        tile.Position = pos;
        return tile;
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
            EmitSignal(nameof(TileClicked), this);
        }
    }

    public override void _Ready()
    {
        resourceContainer = GetNodeOrNull<Node3D>("Resources");
        mesh = GetNode<MeshInstance3D>("Mesh");
        if (resourceContainer == null)
        {
            resourceContainer = new Node3D();
            resourceContainer.Name = "Resources";
            AddChild(resourceContainer);
        }

        Inventory.Changed += RefreshResourcesVisual;
    }

    private void RefreshResourcesVisual()
    {
        foreach (Node child in resourceContainer.GetChildren())
            child.QueueFree();

        int index = 0;
        foreach (var kv in Inventory.All)
        {
            for (int i = 0; i < kv.Value; i++)
            {
                var res = Resource.Create(Vector3.Zero);
                res.SetResourceType(kv.Key);

                float offsetX = (index % 3) * 0.4f - 0.4f;
                float offsetZ = (index / 3) * 0.4f - 0.4f;
                res.Position = new Vector3(offsetX, 0.5f, offsetZ);

                resourceContainer.AddChild(res);
                index++;
            }
        }
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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
