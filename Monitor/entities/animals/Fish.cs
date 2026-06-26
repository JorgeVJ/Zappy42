using Godot;

// Pez decorativo móvil. Hereda de Animal (dominio + locomoción + comportamiento) y
// añade la animación procedural de los huesos "Body" y "Tail" (los modelos no traen
// clips). Genérico: sirve para cualquier .glb de pez con ese rig; la malla concreta
// se pasa como ruta al factory. Autocontenido: no referencia tipos del proyecto.
public partial class Fish : Animal
{
	[Export] public float TailFrequency = 3.5f;
	[Export] public float TailAmplitudeDegrees = 25f;
	[Export] public float BodyFrequency = 3.5f;
	[Export] public float BodyAmplitudeDegrees = 8f;

	// Cuánto acelera el aleteo de la cola con la velocidad de nado (0 = constante).
	[Export] public float SpeedTailBoost = 1.2f;

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

		if (!string.IsNullOrEmpty(_modelPath))
		{
			var packed = ResourceLoader.Load<PackedScene>(_modelPath);
			if (packed != null)
			{
				var model = packed.Instantiate<Node3D>();
				AddChild(model);

				_skeleton = FindSkeleton(model);
				if (_skeleton != null)
				{
					_bodyBone = _skeleton.FindBone("Body");
					_tailBone = _skeleton.FindBone("Tail");

					if (_bodyBone >= 0)
						_bodyRest = _skeleton.GetBoneRest(_bodyBone);
					if (_tailBone >= 0)
						_tailRest = _skeleton.GetBoneRest(_tailBone);
				}
			}
		}

		base._Ready();
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

	// Animación de huesos, modulada por la velocidad de nado: la cola bate más rápido
	// y con más amplitud al crucero, y suave al estar casi parado (anticipa el objetivo
	// de "animaciones que cambian según el estado").
	protected override void OnLocomotionUpdate(float speed)
	{
		if (_skeleton == null)
			return;

		float maxSpeed = Mathf.Max(Locomotion?.MaxSpeed ?? 1f, 0.001f);
		float speedFactor = Mathf.Clamp(speed / maxSpeed, 0f, 1f);

		float freqMul = 1f + SpeedTailBoost * speedFactor;
		float ampMul = 0.5f + 0.5f * speedFactor; // 50% en reposo → 100% nadando

		_phase += (float)GetProcessDeltaTime() * freqMul;

		if (_tailBone >= 0)
		{
			float angle = Mathf.DegToRad(TailAmplitudeDegrees) * ampMul * Mathf.Sin(_phase * TailFrequency);
			Transform3D pose = _tailRest with { Basis = _tailRest.Basis * Basis.FromEuler(new Vector3(angle, 0f, 0f)) };
			_skeleton.SetBonePoseRotation(_tailBone, pose.Basis.GetRotationQuaternion());
		}

		if (_bodyBone >= 0)
		{
			float angle = Mathf.DegToRad(BodyAmplitudeDegrees) * ampMul * Mathf.Sin(_phase * BodyFrequency + Mathf.Pi);
			Transform3D pose = _bodyRest with { Basis = _bodyRest.Basis * Basis.FromEuler(new Vector3(angle, 0f, 0f)) };
			_skeleton.SetBonePoseRotation(_bodyBone, pose.Basis.GetRotationQuaternion());
		}
	}
}
