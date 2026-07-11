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

    private ToggleSwitch _water;
    private ToggleSwitch _decorations;
    private ToggleSwitch _animals;
    private ToggleSwitch _sound;

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
        AddLowPowerButton();

        _water.Toggled += on => EmitSignal(SignalName.WaterToggled, on);
        _decorations.Toggled += on => EmitSignal(SignalName.DecorationsToggled, on);
        _animals.Toggled += on => EmitSignal(SignalName.AnimalsToggled, on);
        _sound.Toggled += on => EmitSignal(SignalName.SoundToggled, on);
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
}
