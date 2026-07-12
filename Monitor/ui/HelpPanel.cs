using Godot;

/// <summary>
/// Panel de ayuda: leyenda de todos los controles de teclado y ratón del monitor.
/// </summary>
/// <remarks>
/// Hereda de CollapsiblePanel (mismo patrón que SettingsPanel/MessageLogPanel).
/// Se alterna con la tecla F1 (cableada en Connection._UnhandledInput) y arranca
/// colapsado, mostrando solo su botón de icono en la bandeja superior izquierda.
/// Es la fuente visible de los atajos repartidos por Camera/CameraFollowBehavior/
/// Connection/MusicPlayer/DayNightCycle/ScreenshotService.
/// </remarks>
public partial class HelpPanel : CollapsiblePanel
{
    /// <summary>Color de acento para los títulos de sección (verde neumórfico).</summary>
    private static readonly Color SectionColor = new Color(0.27f, 1f, 0.53f);

    public override void _Ready()
    {
        Setup("Controles", new Rect2(20, 60, 300, 360), minimizedIcon: "help");
        Neumorphism.StylePanel(PanelRoot);
        Content.AddThemeConstantOverride("separation", 4);
        BuildRows();
        StartCollapsed();
    }

    private void BuildRows()
    {
        AddSection("Cámara libre");
        AddShortcut("Mover", "W A S D");
        AddShortcut("Bajar / Subir", "Q / E");
        AddShortcut("Rápido", "Shift");
        AddShortcut("Mirar (capturar ratón)", "Clic der.");
        AddShortcut("Seleccionar", "Clic izq.");

        AddSection("Cámara siguiendo");
        AddShortcut("Orbitar", "W A S D");
        AddShortcut("Zoom", "Rueda");
        AddShortcut("Soltar", "Clic der.");

        AddSection("Paneles");
        AddShortcut("Ayuda", "F1");
        AddShortcut("Mensajes", "F2");
        AddShortcut("Equipos", "F3");

        AddSection("Entorno y audio");
        AddShortcut("Día/noche automático", "L");
        AddShortcut("Hora del día −/+", "[ / ]");
        AddShortcut("Silenciar música", "M");

        AddSection("Herramientas");
        AddShortcut("Captura de pantalla", "F12");
    }

    /// <summary>Título de sección en negrita/acento seguido de un separador.</summary>
    private void AddSection(string title)
    {
        Label header = new Label();
        header.Text = title;
        header.AddThemeColorOverride("font_color", SectionColor);
        Content.AddChild(header);
        Content.AddChild(new HSeparator());
    }

    /// <summary>Fila "acción — tecla": etiqueta a la izquierda que expande, tecla a la derecha.</summary>
    private void AddShortcut(string action, string key)
    {
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        Label actionLbl = new Label();
        actionLbl.Text = action;
        actionLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(actionLbl);

        Label keyLbl = new Label();
        keyLbl.Text = key;
        keyLbl.HorizontalAlignment = HorizontalAlignment.Right;
        keyLbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.82f));
        row.AddChild(keyLbl);

        Content.AddChild(row);
    }
}
