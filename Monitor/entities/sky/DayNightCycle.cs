using Godot;

/// <summary>
/// Ciclo día/noche: convierte la antigua DirectionalLight3D estática en un Sol dinámico,
/// añade una Luna para la noche y dirige el cielo/ambiente según la hora del día
/// (TimeOfDay).
/// </summary>
/// <remarks>
/// El ciclo avanza en su propio reloj de pared (DayDurationSeconds), independiente del
/// tiempo del servidor/timeline. Se pausa junto con el juego (process mode por defecto).
/// Solo afecta a iluminación y cielo; no hay discos de sol/luna visibles.
/// </remarks>
public partial class DayNightCycle : Node3D
{
	/// <summary>Se emite al cambiar AutoRun (p. ej. tecla L) para sincronizar la UI.</summary>
	[Signal]
	public delegate void AutoRunChangedEventHandler(bool on);

	[Export]
	public bool AutoRun = true;

	[Export]
	public float DayDurationSeconds = 120f;

	/// <summary>Fracción del día: 0 = medianoche, 0.25 = amanecer, 0.5 = mediodía, 0.75 = atardecer.</summary>
	[Export(PropertyHint.Range, "0,1")]
	public float TimeOfDay = 0.5f;

	/// <summary>Blanco cálido.</summary>
	[Export]
	public Color SunDayColor = new Color(1.0f, 0.96f, 0.84f);

	/// <summary>Naranja/rojo.</summary>
	[Export]
	public Color SunHorizonColor = new Color(1.0f, 0.45f, 0.18f);

	[Export]
	public float MaxSunEnergy = 1.4f;

	/// <summary>Azul suave.</summary>
	[Export]
	public Color MoonColor = new Color(0.5f, 0.62f, 0.9f);

	[Export]
	public float MaxMoonEnergy = 0.45f;

	/// <summary>Azul cielo diurno.</summary>
	[Export]
	public Color SkyTopDay = new Color(0.39f, 0.58f, 0.93f);

	/// <summary>Azul oscuro (no negro).</summary>
	[Export]
	public Color SkyTopNight = new Color(0.03f, 0.04f, 0.10f);

	[Export]
	public Color SkyHorizonDay = new Color(0.65f, 0.66f, 0.67f);

	[Export]
	public Color SkyHorizonNight = new Color(0.05f, 0.06f, 0.13f);

	/// <summary>Tinte cálido amanecer/atardecer.</summary>
	[Export]
	public Color HorizonDuskColor = new Color(1.0f, 0.5f, 0.22f);

	/// <summary>Referencia al WorldEnvironment hermano (cableada vía NodePath en el .tscn).</summary>
	[Export]
	public WorldEnvironment WorldEnv;

	private DirectionalLight3D _sun;
	private DirectionalLight3D _moon;
	private ProceduralSkyMaterial _sky;

	public override void _Ready()
	{
		_sun = GetNode<DirectionalLight3D>("Sun");
		_moon = GetNode<DirectionalLight3D>("Moon");

		if (WorldEnv != null && WorldEnv.Environment != null && WorldEnv.Environment.Sky != null)
			_sky = WorldEnv.Environment.Sky.SkyMaterial as ProceduralSkyMaterial;

		Apply(TimeOfDay);
	}

	/// <remarks>Siempre aplica para que las ediciones del inspector se reflejen en vivo.</remarks>
	public override void _Process(double delta)
	{
		if (AutoRun)
			TimeOfDay = Mathf.PosMod(TimeOfDay + (float)delta / DayDurationSeconds, 1f);

		Apply(TimeOfDay);
	}

	public void Apply(float t)
	{
		float el = ComputeSunElevation(t);
		ApplySunAndMoonOrientation(t, el);

		float dayFactor = Mathf.SmoothStep(-0.1f, 0.25f, el);

		ApplySun(el, dayFactor);
		ApplyMoon(dayFactor);
		ApplySky(el, dayFactor);
	}

	/// <summary>Elevación del sol: -1 medianoche, 0 amanecer/atardecer, +1 mediodía.</summary>
	private float ComputeSunElevation(float t) => -Mathf.Cos(t * Mathf.Tau);

	/// <summary>Orientación del sol y luna (opuestos).</summary>
	private void ApplySunAndMoonOrientation(float t, float el)
	{
		if (_sun != null)
			_sun.RotationDegrees = new Vector3(-el * 90f, t * 360f, 0f);
		if (_moon != null)
			_moon.RotationDegrees = new Vector3(el * 90f, t * 360f + 180f, 0f);
	}

	/// <summary>Energía se desvanece de noche; color va de horizonte (naranja) a día (blanco cálido).</summary>
	private void ApplySun(float el, float dayFactor)
	{
		if (_sun == null)
			return;

		_sun.LightEnergy = MaxSunEnergy * dayFactor;
		_sun.LightColor = SunHorizonColor.Lerp(SunDayColor, Mathf.SmoothStep(0f, 0.4f, el));
	}

	/// <summary>Energía inversa al sol.</summary>
	private void ApplyMoon(float dayFactor)
	{
		if (_moon == null)
			return;

		_moon.LightEnergy = MaxMoonEnergy * (1f - dayFactor);
		_moon.LightColor = MoonColor;
	}

	/// <summary>
	/// Interpolación noche-día + tinte cálido cerca del horizonte (amanecer/atardecer,
	/// dusk ~1 cuando el sol está cerca del horizonte, |el| pequeño, 0 lejos de él).
	/// </summary>
	private void ApplySky(float el, float dayFactor)
	{
		if (_sky == null)
			return;

		Color skyTop = SkyTopNight.Lerp(SkyTopDay, dayFactor);
		Color skyHorizon = SkyHorizonNight.Lerp(SkyHorizonDay, dayFactor);

		float dusk = 1f - Mathf.Min(1f, Mathf.Abs(el) / 0.25f);
		skyHorizon = skyHorizon.Lerp(HorizonDuskColor, dusk * 0.6f);

		_sky.SkyTopColor = skyTop;
		_sky.SkyHorizonColor = skyHorizon;
		_sky.GroundHorizonColor = skyHorizon;
	}

	/// <summary>Asigna AutoRun y notifica el cambio (para reflejarlo en la UI).</summary>
	public void SetAutoRun(bool on)
	{
		AutoRun = on;
		EmitSignal(SignalName.AutoRunChanged, on);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey k && k.Pressed && !k.Echo)
			HandleKey(k.Keycode);
	}

	/// <summary>
	/// L alterna el ciclo automático (pausa/reanuda). ']' avanza la hora del día,
	/// '[' la retrocede.
	/// </summary>
	private void HandleKey(Key keycode)
	{
		switch (keycode)
		{
			case Key.L:
				SetAutoRun(!AutoRun);
				break;
			case Key.Bracketright:
				TimeOfDay = Mathf.PosMod(TimeOfDay + 0.02f, 1f);
				Apply(TimeOfDay);
				break;
			case Key.Bracketleft:
				TimeOfDay = Mathf.PosMod(TimeOfDay - 0.02f, 1f);
				Apply(TimeOfDay);
				break;
		}
	}
}
