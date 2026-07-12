using Godot;

/// <summary>
/// Panel reutilizable con cabecera (título + botón ✕) y botón de restauración.
/// Uso: heredar y llamar Setup() en _Ready(), luego añadir nodos a Content.
/// </summary>
public partial class CollapsiblePanel : Control
{
    private Button        _minimizedBtn;

    /// <summary>Área de contenido expuesta a las subclases.</summary>
    protected VBoxContainer Content { get; private set; }

    /// <summary>
    /// Contenedor raíz del panel expandido, expuesto a las subclases para que
    /// puedan re-estilar su propio fondo (p. ej. estilo neumórfico) sin afectar
    /// al resto de paneles.
    /// </summary>
    protected PanelContainer PanelRoot { get; private set; }

    /// <summary>
    /// Construye la estructura del panel.
    /// </summary>
    /// <param name="title">Texto del título y del botón minimizado.</param>
    /// <param name="panelRect">Posición y tamaño del panel expandido.</param>
    /// <param name="minimizedIcon">Icono (ui/icons/&lt;name&gt;.svg) del botón colapsado; si es null usa el título como texto.
    /// El botón colapsado se coloca en la bandeja compartida (esquina superior izquierda), no en este panel.</param>
    protected void Setup(string title, Rect2 panelRect, string minimizedIcon = null)
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        BuildPanel(title, panelRect);
        BuildMinimizedButton(title, minimizedIcon);
    }

    /// <summary>
    /// Construye el panel expandido (contenedor, margen, cabecera y área de
    /// contenido). Incluye un ResizeBehavior hijo, equivalente a añadirlo como
    /// hijo en el editor de escenas pero sin necesidad de fichero .tscn.
    /// </summary>
    private void BuildPanel(string title, Rect2 panelRect)
    {
        PanelContainer panel = new PanelContainer();
        panel.Position = panelRect.Position;
        panel.Size     = panelRect.Size;
        AddChild(panel);
        PanelRoot = panel;

        panel.AddChild(new ResizeBehavior());

        MarginContainer margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   8);
        margin.AddThemeConstantOverride("margin_right",  8);
        margin.AddThemeConstantOverride("margin_top",    6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        VBoxContainer mainVBox = new VBoxContainer();
        margin.AddChild(mainVBox);

        BuildHeader(mainVBox, title);
        mainVBox.AddChild(new HSeparator());

        Content = new VBoxContainer();
        Content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVBox.AddChild(Content);
    }

    /// <summary>Cabecera del panel: título (izquierda) + botón ✕ (derecha).</summary>
    private void BuildHeader(VBoxContainer mainVBox, string title)
    {
        HBoxContainer header = new HBoxContainer();
        mainVBox.AddChild(header);

        Label titleLbl = new Label();
        titleLbl.Text                = title;
        titleLbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(titleLbl);

        Button closeBtn = new Button();
        IconButton.Apply(closeBtn, "close", "Cerrar");
        closeBtn.Pressed += Collapse;
        header.AddChild(closeBtn);
    }

    /// <summary>
    /// Botón minimizado: vive en la bandeja compartida (HBoxContainer). El
    /// contenedor ignora a los hijos ocultos, así que solo los paneles
    /// colapsados muestran su botón y todos quedan pegados a la izquierda,
    /// sin solaparse.
    /// </summary>
    private void BuildMinimizedButton(string title, string minimizedIcon)
    {
        _minimizedBtn = new Button();
        if (minimizedIcon != null)
            IconButton.Apply(_minimizedBtn, minimizedIcon, title);
        else
            _minimizedBtn.Text = title;
        _minimizedBtn.Pressed += Expand;
        _minimizedBtn.Hide();
        GetTray().AddChild(_minimizedBtn);
    }

    /// <summary>
    /// Bandeja compartida por todos los CollapsiblePanel para sus botones
    /// colapsados (esquina superior izquierda). Creada de forma perezosa la
    /// primera vez que un panel hace Setup().
    /// </summary>
    private static HBoxContainer _minimizedTray;

    /// <summary>
    /// Obtiene (creando de forma perezosa si hace falta) la bandeja compartida.
    /// Se cuelga del contenedor UI del panel (CanvasLayer/UI) para renderizar
    /// por encima del 3D (igual que los paneles) y quedar por debajo del overlay
    /// de ganador (CanvasLayer 10). El AddChild se difiere porque este método se
    /// llama desde Setup() en _Ready, momento en que el padre está "ocupado
    /// montando hijos" y un AddChild directo falla silenciosamente (la bandeja
    /// no entraría al árbol y los botones colapsados no se verían); CallDeferred
    /// lo añade ya libre.
    /// </summary>
    private HBoxContainer GetTray()
    {
        if (_minimizedTray == null || !IsInstanceValid(_minimizedTray))
        {
            _minimizedTray = new HBoxContainer();
            _minimizedTray.AddThemeConstantOverride("separation", 8);
            _minimizedTray.SetAnchorsAndOffsetsPreset(
                Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize, 10);
            GetParent().CallDeferred(Node.MethodName.AddChild, _minimizedTray);
        }
        return _minimizedTray;
    }

    /// <summary>Arranca el panel colapsado (solo su botón en la bandeja compartida).</summary>
    protected void StartCollapsed() => Collapse();

    /// <summary>Alterna entre expandido y colapsado.</summary>
    public void Toggle()
    {
        if (PanelRoot.Visible) Collapse();
        else Expand();
    }

    private void Collapse()
    {
        PanelRoot.Hide();
        _minimizedBtn.Show();
    }

    private void Expand()
    {
        _minimizedBtn.Hide();
        PanelRoot.Show();
    }
}
