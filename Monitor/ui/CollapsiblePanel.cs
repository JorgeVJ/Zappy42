using Godot;

/// <summary>
/// Panel reutilizable con cabecera (título + botón ✕) y botón de restauración.
/// Uso: heredar y llamar Setup() en _Ready(), luego añadir nodos a Content.
/// </summary>
public partial class CollapsiblePanel : Control
{
    private Control       _panelRoot;
    private Button        _minimizedBtn;

    /// <summary>Área de contenido expuesta a las subclases.</summary>
    protected VBoxContainer Content { get; private set; }

    /// <summary>
    /// Construye la estructura del panel.
    /// </summary>
    /// <param name="title">Texto del título y del botón minimizado.</param>
    /// <param name="panelRect">Posición y tamaño del panel expandido.</param>
    /// <param name="minimizedIcon">Icono (ui/icons/&lt;name&gt;.svg) del botón colapsado; si es null usa el título como texto.
    /// El botón colapsado se coloca en la bandeja compartida (esquina superior izquierda), no en este panel.</param>
    protected void Setup(string title, Rect2 panelRect, string minimizedIcon = null)
    {
        // Este nodo llena toda la pantalla pero es transparente al ratón
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // ── Panel expandido ───────────────────────────────────────────────
        var panel = new PanelContainer();
        panel.Position = panelRect.Position;
        panel.Size     = panelRect.Size;
        AddChild(panel);
        _panelRoot = panel;

        // Equivale a añadir ResizeBehavior como hijo en el editor de escenas —
        // misma jerarquía en runtime, sin necesidad de fichero .tscn.
        panel.AddChild(new ResizeBehavior());

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   8);
        margin.AddThemeConstantOverride("margin_right",  8);
        margin.AddThemeConstantOverride("margin_top",    6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var mainVBox = new VBoxContainer();
        margin.AddChild(mainVBox);

        // Cabecera: título (izquierda) + botón ✕ (derecha)
        var header = new HBoxContainer();
        mainVBox.AddChild(header);

        var titleLbl = new Label();
        titleLbl.Text                = title;
        titleLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(titleLbl);

        var closeBtn = new Button();
        IconButton.Apply(closeBtn, "close", "Cerrar");
        closeBtn.Pressed += Collapse;
        header.AddChild(closeBtn);

        mainVBox.AddChild(new HSeparator());

        // Área de contenido
        Content = new VBoxContainer();
        Content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVBox.AddChild(Content);

        // ── Botón minimizado ──────────────────────────────────────────────
        // Vive en la bandeja compartida (HBoxContainer): el contenedor ignora a
        // los hijos ocultos, así que solo los paneles colapsados muestran su
        // botón y todos quedan pegados a la izquierda, sin solaparse.
        _minimizedBtn = new Button();
        if (minimizedIcon != null)
            IconButton.Apply(_minimizedBtn, minimizedIcon, title);
        else
            _minimizedBtn.Text = title;
        _minimizedBtn.Pressed += Expand;
        _minimizedBtn.Hide();
        GetTray().AddChild(_minimizedBtn);
    }

    // Bandeja compartida por todos los CollapsiblePanel para sus botones
    // colapsados (esquina superior izquierda). Creada de forma perezosa la
    // primera vez que un panel hace Setup().
    private static HBoxContainer _minimizedTray;

    private HBoxContainer GetTray()
    {
        if (_minimizedTray == null || !IsInstanceValid(_minimizedTray))
        {
            _minimizedTray = new HBoxContainer();
            _minimizedTray.AddThemeConstantOverride("separation", 8);
            // Root es un padre siempre válido en _Ready y renderiza por encima
            // del 3D (igual que los paneles); queda por debajo del overlay de
            // ganador, que usa un CanvasLayer en capa 10.
            GetTree().Root.AddChild(_minimizedTray);
            _minimizedTray.SetAnchorsAndOffsetsPreset(
                Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize, 10);
        }
        return _minimizedTray;
    }

    /// <summary>Alterna entre expandido y colapsado.</summary>
    public void Toggle()
    {
        if (_panelRoot.Visible) Collapse();
        else Expand();
    }

    private void Collapse()
    {
        _panelRoot.Hide();
        _minimizedBtn.Show();
    }

    private void Expand()
    {
        _minimizedBtn.Hide();
        _panelRoot.Show();
    }
}
