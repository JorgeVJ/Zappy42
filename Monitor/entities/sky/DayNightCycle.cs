using Godot;

// Ciclo día/noche: convierte la antigua DirectionalLight3D estática en un Sol
// dinámico, añade una Luna para la noche y dirige el cielo/ambiente según la
// hora del día (TimeOfDay). El ciclo avanza en su propio reloj de pared
// (DayDurationSeconds), independiente del tiempo del servidor/timeline. Se pausa
// junto con el juego (process mode por defecto). Solo afecta a iluminación y
// cielo; no hay discos de sol/luna visibles.
public partial class DayNightCycle : Node3D
{
	// --- Ciclo ---
	[Export] public bool AutoRun = true;
	[Export] public float DayDurationSeconds = 120f;

	// Fracción del día: 0 = medianoche, 0.25 = amanecer, 0.5 = mediodía, 0.75 = atardecer.
	[Export(PropertyHint.Range, "0,1")] public float TimeOfDay = 0.5f;

	// --- Sol ---
	[Export] public Color SunDayColor = new Color(1.0f, 0.96f, 0.84f);     // blanco cálido
	[Export] public Color SunHorizonColor = new Color(1.0f, 0.45f, 0.18f); // naranja/rojo
	[Export] public float MaxSunEnergy = 3.0f;

	// --- Luna ---
	[Export] public Color MoonColor = new Color(0.5f, 0.62f, 0.9f);        // azul suave
	[Export] public float MaxMoonEnergy = 0.45f;

	// --- Paletas de cielo ---
	[Export] public Color SkyTopDay = new Color(0.39f, 0.58f, 0.93f);      // azul cielo diurno
	[Export] public Color SkyTopNight = new Color(0.03f, 0.04f, 0.10f);    // azul oscuro (no negro)
	[Export] public Color SkyHorizonDay = new Color(0.65f, 0.66f, 0.67f);
	[Export] public Color SkyHorizonNight = new Color(0.05f, 0.06f, 0.13f);
	[Export] public Color HorizonDuskColor = new Color(1.0f, 0.5f, 0.22f); // tinte cálido amanecer/atardecer

	// Referencia al WorldEnvironment hermano (cableada vía NodePath en el .tscn).
	[Export] public WorldEnvironment WorldEnv;

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

	public override void _Process(double delta)
	{
		if (AutoRun)
			TimeOfDay = Mathf.PosMod(TimeOfDay + (float)delta / DayDurationSeconds, 1f);

		// Siempre aplicar para que las ediciones del inspector se reflejen en vivo.
		Apply(TimeOfDay);
	}

	public void Apply(float t)
	{
		// Elevación del sol: -1 medianoche, 0 amanecer/atardecer, +1 mediodía.
		float el = -Mathf.Cos(t * Mathf.Tau);

		// Orientación del sol y luna (opuestos).
		if (_sun != null)
			_sun.RotationDegrees = new Vector3(-el * 90f, t * 360f, 0f);
		if (_moon != null)
			_moon.RotationDegrees = new Vector3(el * 90f, t * 360f + 180f, 0f);

		// 0 = noche, 1 = sol bien alto.
		float dayFactor = Mathf.SmoothStep(-0.1f, 0.25f, el);

		// Sol: energía se desvanece de noche; color va de horizonte (naranja) a día (blanco cálido).
		if (_sun != null)
		{
			_sun.LightEnergy = MaxSunEnergy * dayFactor;
			_sun.LightColor = SunHorizonColor.Lerp(SunDayColor, Mathf.SmoothStep(0f, 0.4f, el));
		}

		// Luna: energía inversa al sol.
		if (_moon != null)
		{
			_moon.LightEnergy = MaxMoonEnergy * (1f - dayFactor);
			_moon.LightColor = MoonColor;
		}

		// Cielo: interpolación noche↔día + tinte cálido cerca del horizonte (amanecer/atardecer).
		if (_sky != null)
		{
			Color skyTop = SkyTopNight.Lerp(SkyTopDay, dayFactor);
			Color skyHorizon = SkyHorizonNight.Lerp(SkyHorizonDay, dayFactor);

			// dusk ~1 cuando el sol está cerca del horizonte (|el| pequeño), 0 lejos de él.
			float dusk = 1f - Mathf.Min(1f, Mathf.Abs(el) / 0.25f);
			skyHorizon = skyHorizon.Lerp(HorizonDuskColor, dusk * 0.6f);

			_sky.SkyTopColor = skyTop;
			_sky.SkyHorizonColor = skyHorizon;
			_sky.GroundHorizonColor = skyHorizon;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey k && k.Pressed && !k.Echo)
		{
			switch (k.Keycode)
			{
				case Key.L: // alternar ciclo automático (pausa/reanuda)
					AutoRun = !AutoRun;
					break;
				case Key.Bracketright: // ']' avanzar la hora del día
					TimeOfDay = Mathf.PosMod(TimeOfDay + 0.02f, 1f);
					Apply(TimeOfDay);
					break;
				case Key.Bracketleft: // '[' retroceder la hora del día
					TimeOfDay = Mathf.PosMod(TimeOfDay - 0.02f, 1f);
					Apply(TimeOfDay);
					break;
			}
		}
	}
}
