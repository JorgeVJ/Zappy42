using Godot;

/// <summary>
/// Ave decorativa móvil. Hereda de ClipAnimal (dominio + locomoción + comportamiento +
/// reproducción de clips) y usa el AnimationPlayer del modelo para reproducir en bucle sus
/// clips de caminar y volar. Camina pegada al suelo sobre tierra y, al volar, se inclina
/// (bank) hacia el interior de las curvas que describe. Genérico para cualquier .glb con esos clips.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia tipos del proyecto (solo el dominio aéreo portable).
/// </remarks>
public partial class Bird : ClipAnimal, IAnimated
{
	[Export]
	public string WalkAnimation = "Parrot_Walk";

	[Export]
	public string FlyAnimation = "Parrot_Fly";

	/// <summary>Distancia a la superficie por debajo de la cual el ave se pega al suelo al caminar.</summary>
	[Export]
	public float GroundSnapThreshold = 0.5f;

	/// <summary>Cuánta inclinación por unidad de velocidad angular de giro (rad/s) al volar.</summary>
	[Export]
	public float BankGain = 6f;

	[Export]
	public float MaxBankDegrees = 45f;

	/// <summary>Rapidez con la que la inclinación converge a su valor deseado (mayor = más ágil).</summary>
	[Export]
	public float BankResponse = 6f;

	/// <summary>Dominio aéreo tipado, compartido; también se asigna a Animal.Domain.</summary>
	public AerialDomain Aerial { get; set; }

	private bool _flying;
	private Vector3 _prevForward = Vector3.Forward;
	private float _bank;

	public static Bird Create(Vector3 pos, string modelPath)
	{
		Bird bird = new Bird { Position = pos, ModelPath = modelPath };
		return bird;
	}

	public override void _Ready()
	{
		LoadModelAndPlayer();
		base._Ready();
		PlayClip(WalkAnimation);
	}

	/// <summary>Cambia el modo del ave (volar/caminar) y reproduce el clip correspondiente.</summary>
	public void SetFlying(bool flying)
	{
		_flying = flying;
		PlayClip(flying ? FlyAnimation : WalkAnimation);
	}

	/// <summary>IAnimated: "fly"/"walk" mapean al modo de vuelo del ave; el resto se ignora.</summary>
	public void PlayState(string state)
	{
		if (state == "fly")
			SetFlying(true);
		else if (state == "walk")
			SetFlying(false);
	}

	/// <summary>IAnimated: el ave no tiene acciones one-shot.</summary>
	public void PlayAction(string action) { }

	/// <summary>IAnimated: sin acciones one-shot, siempre "terminada".</summary>
	public bool ActionFinished => true;

	/// <summary>
	/// Cada frame: si vuela, inclina el modelo hacia el interior de la curva según la
	/// velocidad de giro del rumbo; si camina, nivela las alas y pega el ave al suelo.
	/// </summary>
	protected override void OnLocomotionUpdate(float speed)
	{
		float dt = (float)GetProcessDeltaTime();
		if (_flying)
			UpdateBank(dt);
		else
			GroundAndLevel(dt);
	}

	/// <summary>
	/// Mide la velocidad angular del rumbo horizontal y aplica un roll proporcional al
	/// modelo (más cerrada la curva → más inclinación), suavizado hacia su valor deseado.
	/// </summary>
	private void UpdateBank(float dt)
	{
		Vector3 curForward = FlattenForward();
		float desired = 0f;
		if (dt > 0.0001f && curForward.LengthSquared() > 0.0001f && _prevForward.LengthSquared() > 0.0001f)
		{
			float yawRate = _prevForward.SignedAngleTo(curForward, Vector3.Up) / dt;
			float maxBank = Mathf.DegToRad(MaxBankDegrees);
			desired = Mathf.Clamp(-yawRate * BankGain, -maxBank, maxBank);
		}

		_prevForward = curForward;
		ApplyBank(desired, dt);
	}

	/// <summary>Nivela progresivamente las alas y, si el ave está sobre tierra y baja, la pega al suelo.</summary>
	private void GroundAndLevel(float dt)
	{
		_prevForward = FlattenForward();
		ApplyBank(0f, dt);

		if (Aerial == null)
			return;

		Vector3 pos = GlobalPosition;
		if (!Aerial.IsLandColumn(pos.X, pos.Z))
			return;

		float surface = Aerial.FloorHeight(pos.X, pos.Z);
		if (pos.Y - surface < GroundSnapThreshold)
			GlobalPosition = pos with { Y = surface };
	}

	/// <summary>Suaviza <see cref="_bank"/> hacia el objetivo y lo aplica como roll local del modelo.</summary>
	private void ApplyBank(float desired, float dt)
	{
		if (Model == null)
			return;

		_bank = Mathf.Lerp(_bank, desired, Mathf.Clamp(BankResponse * dt, 0f, 1f));
		Model.Rotation = Model.Rotation with { Z = _bank };
	}

	private Vector3 FlattenForward()
	{
		Vector3 forward = -GlobalBasis.Z;
		forward.Y = 0f;
		return forward.LengthSquared() > 0.0001f ? forward.Normalized() : _prevForward;
	}
}
