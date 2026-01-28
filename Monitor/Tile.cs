using Godot;
using System;
using System.Collections.Generic;

public partial class Tile : Node3D
{
	private static PackedScene scene = ResourceLoader.Load("res://tile.tscn") as PackedScene;

	private Node3D resourceContainer;

    private MeshInstance3D mesh;

    [Signal]
    public delegate void TileClickedEventHandler(Tile tile);

    private Dictionary<Resource.ResourceType, int> inventory = new();

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

		foreach (Resource.ResourceType t in Enum.GetValues(typeof(Resource.ResourceType)))
			inventory[t] = 0;
	}

	public void SetResourceCount(Resource.ResourceType type, int count)
	{
		inventory[type] = count;
		RefreshResourcesVisual();
	}

	public void PickResource(Resource.ResourceType type)
	{
		if (inventory[type] > 0)
			SetResourceCount(type, inventory[type] - 1);
	}

	public int GetResource(Resource.ResourceType type)
	{
		return inventory[type];
	}

	private void RefreshResourcesVisual()
	{
		foreach (Node child in resourceContainer.GetChildren())
			child.QueueFree();

		int index = 0;
		foreach (var kv in inventory)
		{
			var type = kv.Key;
			int amount = kv.Value;

			for (int i = 0; i < amount; i++)
			{
				var res = Resource.Create(Vector3.Zero);
				res.SetResourceType(type);

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
