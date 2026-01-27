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

	public static Resource Create(Vector3 pos)
	{
		Resource tile = scene.Instantiate<Resource>();
		tile.Position = pos;
		return tile;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mesh = GetNode<MeshInstance3D>("Mesh");
	}

	public void SetResourceType(ResourceType type)
	{
		if (mesh == null)
			mesh = GetNode<MeshInstance3D>("Mesh");

		var mat = new StandardMaterial3D();
		mat.AlbedoColor = type switch
		{
			ResourceType.Nourriture => new Color(0.2f, 1.0f, 0.2f),   // verde 
			ResourceType.Linemate => new Color(0.8f, 0.8f, 0.8f),   // gris claro
			ResourceType.Deraumere => new Color(0.2f, 0.6f, 1.0f),   // azul
			ResourceType.Sibur => new Color(1.0f, 0.6f, 0.2f),   // naranja
			ResourceType.Mendiane => new Color(1.0f, 0.2f, 1.0f),   // magenta
			ResourceType.Phiras => new Color(1.0f, 1.0f, 0.2f),   // amarillo
			ResourceType.Thystame => new Color(1.0f, 0.2f, 0.2f),   // rojo
			_ => Colors.White
		};

		// Aplica el material al surface 0 del mesh
		mesh.SetSurfaceOverrideMaterial(0, mat);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
