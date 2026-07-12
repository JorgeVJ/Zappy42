using Godot;

/// <summary>
/// Panel de configuración gráfica: interruptores para aligerar el render en
/// equipos de poca potencia (agua, decoración/césped, fauna) y control de sonido.
/// </summary>
/// <remarks>
/// No referencia Terrain ni MusicPlayer: emite señales que Connection conecta a
/// esos nodos tras el AddChild (mismo patrón que TeamProgressPanel.PlayerSelected),
/// evitando dependencias de orden de inicialización. Estilo soft-3D vía
/// ui/Neumorphism.cs, aplicado solo a su propio fondo.
/// </remarks>
public partial class SettingsPanel : CollapsiblePanel
{
    [Signal]
    public delegate void WaterToggledEventHandler(bool on);

    [Signal]
    public delegate void DecorationsToggledEventHandler(bool on);

    [Signal]
    public delegate void AnimalsToggledEventHandler(bool on);

    [Signal]
    public delegate void SoundToggledEventHandler(bool on);

    [Signal]
    public delegate void DayNightAutoToggledEventHandler(bool on);

    [Signal]
    public delegate void TimeOfDayChangedEventHandler(float t);

    private ToggleSwitch _water;
    private ToggleSwitch _decorations;
    private ToggleSwitch _animals;
    private ToggleSwitch _sound;
    private ToggleSwitch _dayNightAuto;
    private HSlider _timeOfDay;

    public override void _Ready()
    {
        Setup("Configuración", new Rect2(20, 60, 280, 260), minimizedIcon: "settings");
        Neumorphism.StylePanel(PanelRoot);
        Content.AddThemeConstantOverride("separation", 10);
        BuildRows();
        StartCollapsed();
    }

    /// <summary>Monta las filas de interruptores y conecta su reemisión de señales.</summary>
    private void BuildRows()
    {
        _water = AddRow("Agua", false);
        _decorations = AddRow("Decoración y césped", false);
        _animals = AddRow("Fauna", false);
        _sound = AddRow("Sonido", true);
        _dayNightAuto = AddRow("Ciclo día/noche", true);
        _timeOfDay = AddSliderRow("Hora del día", 0f, 1f, 0.5f);
        AddLowPowerButton();

        _water.Toggled += on => EmitSignal(SignalName.WaterToggled, on);
        _decorations.Toggled += on => EmitSignal(SignalName.DecorationsToggled, on);
        _animals.Toggled += on => EmitSignal(SignalName.AnimalsToggled, on);
        _sound.Toggled += on => EmitSignal(SignalName.SoundToggled, on);
        _dayNightAuto.Toggled += on => EmitSignal(SignalName.DayNightAutoToggled, on);
        _timeOfDay.ValueChanged += value => EmitSignal(SignalName.TimeOfDayChanged, (float)value);
    }

    /// <summary>Fila con etiqueta a la izquierda e interruptor a la derecha, con estado inicial dado.</summary>
    private ToggleSwitch AddRow(string label, bool defaultOn)
    {
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        Label lbl = new Label();
        lbl.Text = label;
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(lbl);

        ToggleSwitch sw = new ToggleSwitch();
        sw.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(sw);
        Content.AddChild(row);

        sw.SetOn(defaultOn, emit: false);
        return sw;
    }

    /// <summary>Fila con etiqueta a la izquierda y deslizador (seek) a la derecha.</summary>
    private HSlider AddSliderRow(string label, float min, float max, float value)
    {
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        Label lbl = new Label();
        lbl.Text = label;
        lbl.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(lbl);

        HSlider slider = new HSlider();
        slider.MinValue = min;
        slider.MaxValue = max;
        slider.Step = 0.01;
        slider.Value = value;
        slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        slider.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(slider);
        Content.AddChild(row);

        return slider;
    }

    /// <summary>Botón que apaga de golpe agua, decoración y fauna.</summary>
    private void AddLowPowerButton()
    {
        Content.AddChild(new HSeparator());

        Button btn = new Button();
        btn.Text = "Modo bajo consumo";
        btn.Pressed += OnLowPower;
        Content.AddChild(btn);
    }

    private void OnLowPower()
    {
        _water.SetOn(false);
        _decorations.SetOn(false);
        _animals.SetOn(false);
    }

    /// <summary>
    /// Refleja el estado de sonido desde fuera (tecla M) sin re-emitir la señal.
    /// </summary>
    public void SetSoundOn(bool on, bool emit = false)
    {
        _sound?.SetOn(on, emit);
    }

    /// <summary>
    /// Refleja el ciclo día/noche desde fuera (tecla L) sin re-emitir la señal.
    /// </summary>
    public void SetDayNightAuto(bool on, bool emit = false)
    {
        _dayNightAuto?.SetOn(on, emit);
    }

    /// <summary>Fija el valor inicial del deslizador de hora sin re-emitir.</summary>
    public void SetTimeOfDayValue(float t)
    {
        _timeOfDay?.SetValueNoSignal(t);
    }
}
