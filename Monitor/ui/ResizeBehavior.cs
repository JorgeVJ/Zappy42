using Godot;

/// <summary>
/// Comportamiento reutilizable que añade redimensión interactiva y clamp-al-viewport
/// a cualquier Control libre (hijo directo de un Control plano, no de un Container).
///
/// Uso:
///     miControl.AddChild(new ResizeBehavior());
///
/// Equivale al patrón Behavior de WPF: la lógica queda separada del control al que
/// se adjunta y puede reutilizarse sin herencia.
///
/// Requisito: GetParent().GetParent() debe ser un Control no-Container, ya que el
/// handle de resize se añade ahí como sibling del target para evitar que un Container
/// gestione su posición y tamaño.
/// </summary>
public partial class ResizeBehavior : Node
{
    [Export] public float MinWidth   { get; set; } = 150f;
    [Export] public float MinHeight  { get; set; } = 80f;
    [Export] public float HandleSize { get; set; } = 14f;

    private Control _target;
    private Control _freeParent;   // padre del target — aquí vive el handle
    private Control _handle;

    private bool    _isResizing;
    private Vector2 _resizeStartMouse;
    private Vector2 _resizeStartSize;

    public override void _Ready()
    {
        _target = GetParent() as Control;
        if (_target == null)
        {
            GD.PushError("ResizeBehavior: debe ser hijo de un Control.");
            return;
        }

        _freeParent = _target.GetParent() as Control;
        if (_freeParent == null)
        {
            GD.PushError("ResizeBehavior: el padre del target debe ser un Control libre (no Container).");
            return;
        }

        _target.CustomMinimumSize = new Vector2(MinWidth, MinHeight);

        // El handle se añade como sibling del target (hijo del "abuelo" libre),
        // no como hijo del target: si el target fuera un Container lo gestionaría.
        var handle = new ColorRect();
        handle.Color                   = new Color(0.7f, 0.7f, 0.7f, 0.4f);
        handle.Size                    = new Vector2(HandleSize, HandleSize);
        handle.MouseDefaultCursorShape = Control.CursorShape.Fdiagsize;
        handle.GuiInput               += OnHandleInput;
        _freeParent.AddChild(handle);
        _handle = handle;

        _target.Resized           += SyncHandle;
        _target.VisibilityChanged += OnTargetVisibilityChanged; // oculta el handle al colapsar
        GetViewport().SizeChanged += ClampToViewport;
        SetProcessInput(true);

        SyncHandle();
    }

    public override void _ExitTree()
    {
        // El handle es hijo del abuelo, no del target, así que hay que liberarlo manualmente.
        if (IsInstanceValid(_handle))
            _handle.QueueFree();

        var vp = GetViewport();
        if (IsInstanceValid(vp))
            vp.SizeChanged -= ClampToViewport;
    }

    // ── Resize ────────────────────────────────────────────────────────────

    private void OnHandleInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            _isResizing       = true;
            _resizeStartMouse = GetViewport().GetMousePosition();
            _resizeStartSize  = _target.Size;
            GetViewport().SetInputAsHandled();
        }
    }

    // _Input captura el movimiento aunque el ratón salga del handle durante el arrastre.
    public override void _Input(InputEvent e)
    {
        if (!_isResizing) return;

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            _isResizing = false;
            GetViewport().SetInputAsHandled();
        }
        else if (e is InputEventMouseMotion)
        {
            var delta  = GetViewport().GetMousePosition() - _resizeStartMouse;
            var vpSize = GetViewport().GetVisibleRect().Size;
            _target.Size = new Vector2(
                Mathf.Clamp(_resizeStartSize.X + delta.X, MinWidth,  vpSize.X - _target.Position.X),
                Mathf.Clamp(_resizeStartSize.Y + delta.Y, MinHeight, vpSize.Y - _target.Position.Y)
            );
            GetViewport().SetInputAsHandled();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SyncHandle()
    {
        if (!IsInstanceValid(_handle)) return;
        _handle.Position = _target.Position + _target.Size - new Vector2(HandleSize, HandleSize);
    }

    private void OnTargetVisibilityChanged()
    {
        if (IsInstanceValid(_handle))
            _handle.Visible = _target.Visible;
    }

    private void ClampToViewport()
    {
        if (!IsInstanceValid(_target)) return;
        var vpSize = GetViewport().GetVisibleRect().Size;
        var pos    = _target.Position;
        pos.X = Mathf.Clamp(pos.X, 0f, Mathf.Max(0f, vpSize.X - _target.Size.X));
        pos.Y = Mathf.Clamp(pos.Y, 0f, Mathf.Max(0f, vpSize.Y - _target.Size.Y));
        _target.Position = pos;
        SyncHandle();
    }
}
