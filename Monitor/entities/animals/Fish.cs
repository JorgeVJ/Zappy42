using Godot;

// Pez decorativo, no interactivo. Genérico: sirve para cualquier modelo de pez
// con huesos "Body" y "Tail" (la malla concreta se pasa como ruta al factory).
// Autocontenido: no referencia ningún tipo del resto del proyecto (Terrain,
// Connection, etc.) para poder copiarse/quitarse como una unidad junto a
// AnimalSystem.cs y los .glb. Anima de forma procedural los huesos "Body" y
// "Tail" del modelo (que no traen animaciones propias).
public partial class Fish : Node3D
{
	[Export] public float TailFrequency = 3.5f;
	[Export] public float TailAmplitudeDegrees = 25f;
	[Export] public float BodyFrequency = 3.5f;
	[Export] public float BodyAmplitudeDegrees = 8f;

	private string _modelPath;

	private Skeleton3D _skeleton;
	private int _bodyBone = -1;
	private int _tailBone = -1;
	private Transform3D _bodyRest;
	private Transform3D _tailRest;
	private float _phase;

	public static Fish Create(Vector3 pos, string modelPath)
	{
		var fish = new Fish { Position = pos, _modelPath = modelPath };
		return fish;
	}

	public override void _Ready()
	{
		_phase = GD.Randf() * Mathf.Tau;

		if (string.IsNullOrEmpty(_modelPath))
			return;

		var packed = ResourceLoader.Load<PackedScene>(_modelPath);
		if (packed == null)
			return;

		var model = packed.Instantiate<Node3D>();
		AddChild(model);

		_skeleton = FindSkeleton(model);
		if (_skeleton == null)
			return;

		_bodyBone = _skeleton.FindBone("Body");
		_tailBone = _skeleton.FindBone("Tail");

		if (_bodyBone >= 0)
			_bodyRest = _skeleton.GetBoneRest(_bodyBone);
		if (_tailBone >= 0)
			_tailRest = _skeleton.GetBoneRest(_tailBone);
	}

	private static Skeleton3D FindSkeleton(Node node)
	{
		if (node is Skeleton3D skeleton)
			return skeleton;

		foreach (Node child in node.GetChildren())
		{
			var found = FindSkeleton(child);
			if (found != null)
				return found;
		}

		return null;
	}

	public override void _Process(double delta)
	{
		if (_skeleton == null)
			return;

		_phase += (float)delta;

		if (_tailBone >= 0)
		{
			float angle = Mathf.DegToRad(TailAmplitudeDegrees) * Mathf.Sin(_phase * TailFrequency);
            Transform3D pose = _tailRest with { Basis = _tailRest.Basis * Basis.FromEuler(new Vector3(0f, 0f, angle)) };
			_skeleton.SetBonePoseRotation(_tailBone, pose.Basis.GetRotationQuaternion());
		}

		if (_bodyBone >= 0)
		{
			float angle = Mathf.DegToRad(BodyAmplitudeDegrees) * Mathf.Sin(_phase * BodyFrequency + Mathf.Pi);
			Transform3D pose = _bodyRest with { Basis = _bodyRest.Basis * Basis.FromEuler(new Vector3(0f, 0f, angle)) };
			_skeleton.SetBonePoseRotation(_bodyBone, pose.Basis.GetRotationQuaternion());
		}
	}
}
