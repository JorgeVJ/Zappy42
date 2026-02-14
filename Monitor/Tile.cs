using Godot;
using System;
using zappy;

public partial class Tile : SelectableInventoryNode3D, IInventory
{
    private static PackedScene scene = ResourceLoader.Load("res://tile.tscn") as PackedScene;

    private Node3D resourceContainer;

    [Signal]
    public delegate void TileClickedEventHandler(Tile tile);

    public static Tile Create(Vector3 pos)
    {
        Tile tile = scene.Instantiate<Tile>();
        tile.Position = pos;
        return tile;
    }

    public override void _Ready()
    {
        // Inicializaciones comunes (mesh, Inventory) en la clase base
        base._Ready();

        resourceContainer = GetNodeOrNull<Node3D>("Resources");
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

    // El resaltado/unresaltado provienen de la clase base

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
