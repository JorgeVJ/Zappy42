using Godot;

/// <summary>
/// Pez decorativo móvil. Hereda de Animal (dominio + locomoción + comportamiento) y
/// añade la animación procedural de los huesos "Body" y "Tail" (los modelos no traen
/// clips). Genérico: sirve para cualquier .glb de pez con ese rig; la malla concreta
/// se pasa como ruta al factory.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia tipos del proyecto.
/// </remarks>
public partial class Fish : Animal
{
	[Export]
	public float TailFrequency = 3.5f;

	[Export]
	public float TailAmplitudeDegrees = 25f;

	[Export]
	public float BodyFrequency = 3.5f;

	[Export]
	public float BodyAmplitudeDegrees = 8f;

	/// <summary>Cuánto acelera el aleteo de la cola con la velocidad de nado (0 = constante).</summary>
	[Export]
	public float SpeedTailBoost = 1.2f;

	private Skeleton3D _skeleton;
	private int _bodyBone = -1;
	private int _tailBone = -1;
	private Transform3D _bodyRest;
	private Transform3D _tailRest;
	private float _phase;

	public static Fish Create(Vector3 pos, string modelPath)
	{
		Fish fish = new Fish { Position = pos, ModelPath = modelPath };
		return fish;
	}

	public override void _Ready()
	{
		_phase = GD.Randf() * Mathf.Tau;
		LoadModelAndSkeleton();
		base._Ready();
	}

	/// <summary>
	/// Instancia el modelo del pez desde <see cref="Animal.ModelPath"/> (si hay uno) y
	/// resuelve el esqueleto y los huesos "Body"/"Tail" que anima OnLocomotionUpdate,
	/// guardando su pose de reposo.
	/// </summary>
	private void LoadModelAndSkeleton()
	{
		Node3D model = LoadModel();
		if (model == null)
			return;

		_skeleton = FindInDescendants<Skeleton3D>(model);
		if (_skeleton == null)
			return;

		_bodyBone = _skeleton.FindBone("Body");
		_tailBone = _skeleton.FindBone("Tail");

		if (_bodyBone >= 0)
			_bodyRest = _skeleton.GetBoneRest(_bodyBone);
		if (_tailBone >= 0)
			_tailRest = _skeleton.GetBoneRest(_tailBone);
	}

	/// <summary>
	/// Animación de huesos, modulada por la velocidad de nado: la cola bate más rápido
	/// y con más amplitud al crucero, y suave al estar casi parado.
	/// </summary>
	protected override void OnLocomotionUpdate(float speed)
	{
		if (_skeleton == null)
			return;

		float maxSpeed = Mathf.Max(Locomotion?.MaxSpeed ?? 1f, 0.001f);
		float speedFactor = Mathf.Clamp(speed / maxSpeed, 0f, 1f);

		float freqMul = 1f + SpeedTailBoost * speedFactor;
		float ampMul = 0.5f + 0.5f * speedFactor;

		_phase += (float)GetProcessDeltaTime() * freqMul;

		AnimateTail(ampMul);
		AnimateBody(ampMul);
	}

	/// <summary>Aplica la rotación sinusoidal del aleteo de cola sobre su pose de reposo.</summary>
	private void AnimateTail(float ampMul)
	{
		if (_tailBone < 0)
			return;

		float angle = Mathf.DegToRad(TailAmplitudeDegrees) * ampMul * Mathf.Sin(_phase * TailFrequency);
		Transform3D pose = _tailRest with { Basis = _tailRest.Basis * Basis.FromEuler(new Vector3(angle, 0f, 0f)) };
		_skeleton.SetBonePoseRotation(_tailBone, pose.Basis.GetRotationQuaternion());
	}

	/// <summary>Aplica la rotación sinusoidal (en contrafase) del balanceo de cuerpo sobre su pose de reposo.</summary>
	private void AnimateBody(float ampMul)
	{
		if (_bodyBone < 0)
			return;

		float angle = Mathf.DegToRad(BodyAmplitudeDegrees) * ampMul * Mathf.Sin(_phase * BodyFrequency + Mathf.Pi);
		Transform3D pose = _bodyRest with { Basis = _bodyRest.Basis * Basis.FromEuler(new Vector3(angle, 0f, 0f)) };
		_skeleton.SetBonePoseRotation(_bodyBone, pose.Basis.GetRotationQuaternion());
	}
}
