using Godot;
using System;

public partial class Resource : Node3D
{
	private static PackedScene scene = ResourceLoader.Load("res://entities/resources/resource.tscn") as PackedScene;

	private MeshInstance3D mesh;
	private Node3D customModel;

	public static Resource Create(Vector3 pos)
	{
		Resource tile = scene.Instantiate<Resource>();
		tile.Position = pos;
		return tile;
	}

	/// <remarks>
	/// Durante el replay instantáneo de la barra de tiempo el recurso debe aparecer
	/// directamente en su posición/escala final, sin animación.
	/// </remarks>
	public override void _Ready()
	{
		mesh = GetNode<MeshInstance3D>("Mesh");

		if (Connection.ReplayInstant)
			return;

		PlaySpawnAnimation();
	}

	/// <summary>Animación genérica de aparición: el recurso cae un poco y crece con un "pop".</summary>
	private void PlaySpawnAnimation()
	{
		Vector3 finalPos   = Position;
		Vector3 finalScale = Scale;
		const float dropHeight = 0.4f;

		Position = finalPos + new Vector3(0f, dropHeight, 0f);
		Scale    = finalScale * 0.01f;

		Tween tween = CreateTween();
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
			UseCustomModel(modelPath);
			return;
		}

		ApplyFallbackMaterial(type);
	}

	private void UseCustomModel(string modelPath)
	{
		mesh.Visible = false;
		if (customModel == null)
		{
			customModel = ResourceLoader.Load<PackedScene>(modelPath).Instantiate<Node3D>();
			customModel.Scale = Vector3.One * 0.15f;
			AddChild(customModel);
		}
	}

	private void ApplyFallbackMaterial(ResourceType type)
	{
		mesh.Visible = true;
		if (customModel != null)
		{
			customModel.QueueFree();
			customModel = null;
		}

		StandardMaterial3D mat = BuildResourceMaterial(ResourceColor(type));
		mesh.SetSurfaceOverrideMaterial(0, mat);
	}

	private static Color ResourceColor(ResourceType type) => type switch
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

	private static StandardMaterial3D BuildResourceMaterial(Color color)
	{
		StandardMaterial3D mat = new StandardMaterial3D();
		mat.AlbedoColor  = color;
		mat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
		mat.Roughness    = 0.05f;
		mat.Metallic     = 0.0f;
		mat.RimEnabled   = true;
		mat.Rim          = 0.6f;
		return mat;
	}
}
