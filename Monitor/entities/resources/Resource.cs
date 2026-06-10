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

	private static PackedScene scene = ResourceLoader.Load("res://entities/resources/resource.tscn") as PackedScene;

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
		PlaySpawnAnimation();
	}

	// Animación genérica de aparición: el recurso cae un poco y crece con un "pop".
	// Framework reutilizable; los efectos temáticos por tipo (meteorito, volcán, rayo)
	// se añadirán encima en una fase posterior (requieren assets/VFX).
	private void PlaySpawnAnimation()
	{
		Vector3 finalPos   = Position;
		Vector3 finalScale = Scale;
		const float dropHeight = 0.4f;

		// Estado inicial: encogido y un poco por encima de su posición final.
		Position = finalPos + new Vector3(0f, dropHeight, 0f);
		Scale    = finalScale * 0.01f;

		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "position", finalPos, 1.5f)
			 .SetTrans(Tween.TransitionType.Bounce)
			 .SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "scale", finalScale, 1f)
			 .SetTrans(Tween.TransitionType.Back)
			 .SetEase(Tween.EaseType.Out);
	}

	public void SetResourceType(ResourceType type)
	{
		if (mesh == null)
			mesh = GetNode<MeshInstance3D>("Mesh");

		string modelPath = $"res://entities/resources/models/{type}.glb";
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

		var color = type switch
		{
			ResourceType.Nourriture => new Color(0.2f, 1.0f, 0.2f, 0.65f),
			ResourceType.Linemate   => new Color(0.8f, 0.8f, 0.8f, 0.65f),
			ResourceType.Deraumere  => new Color(0.2f, 0.6f, 1.0f, 0.65f),
			ResourceType.Sibur      => new Color(1.0f, 0.6f, 0.2f, 0.65f),
			ResourceType.Mendiane   => new Color(1.0f, 0.2f, 1.0f, 0.65f),
			ResourceType.Phiras     => new Color(1.0f, 1.0f, 0.2f, 0.65f),
			ResourceType.Thystame   => new Color(1.0f, 0.2f, 0.2f, 0.65f),
			_ => Colors.White
		};

		var mat = new StandardMaterial3D();
		mat.AlbedoColor  = color;
		mat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
		mat.Roughness    = 0.05f;
		mat.Metallic     = 0.0f;
		mat.RimEnabled   = true;
		mat.Rim          = 0.6f;
		mesh.SetSurfaceOverrideMaterial(0, mat);
	}
}
