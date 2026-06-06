using Godot;
using System;

public partial class Resource : Node3D
{
	public enum ResourceType
	{
		Nourriture,
		Linemate,
		Deraumere,
		Sibur,
		Mendiane,
		Phiras,
		Thystame,
	}

	private static PackedScene scene = ResourceLoader.Load("res://resource.tscn") as PackedScene;

	private MeshInstance3D mesh;
	private Node3D customModel;

	public static Resource Create(Vector3 pos)
	{
		Resource tile = scene.Instantiate<Resource>();
		tile.Position = pos;
		return tile;
	}

	public override void _Ready()
	{
		mesh = GetNode<MeshInstance3D>("Mesh");
	}

	public void SetResourceType(ResourceType type)
	{
		if (mesh == null)
			mesh = GetNode<MeshInstance3D>("Mesh");

		string modelPath = $"res://models/{type.ToString()}.glb";
		if (ResourceLoader.Exists(modelPath))
		{
			mesh.Visible = false;
			if (customModel == null)
			{
				customModel = ResourceLoader.Load<PackedScene>(modelPath).Instantiate<Node3D>();
				customModel.Scale = Vector3.One * 0.15f;
				AddChild(customModel);
			}
			return;
		}

		mesh.Visible = true;
		if (customModel != null)
		{
			customModel.QueueFree();
			customModel = null;
		}

		var mat = new StandardMaterial3D();
		mat.AlbedoColor = type switch
		{
			ResourceType.Nourriture => new Color(0.2f, 1.0f, 0.2f),
			ResourceType.Linemate   => new Color(0.8f, 0.8f, 0.8f),
			ResourceType.Deraumere  => new Color(0.2f, 0.6f, 1.0f),
			ResourceType.Sibur      => new Color(1.0f, 0.6f, 0.2f),
			ResourceType.Mendiane   => new Color(1.0f, 0.2f, 1.0f),
			ResourceType.Phiras     => new Color(1.0f, 1.0f, 0.2f),
			ResourceType.Thystame   => new Color(1.0f, 0.2f, 0.2f),
			_ => Colors.White
		};
		mesh.SetSurfaceOverrideMaterial(0, mat);
	}
}
