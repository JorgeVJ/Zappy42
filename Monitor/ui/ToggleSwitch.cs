using Godot;

/// <summary>
/// Interruptor deslizante animado con estética neumórfica (soft-3D), dibujado a
/// mano. Emite <see cref="ToggledEventHandler"/> al cambiar de estado.
/// </summary>
/// <remarks>
/// La perilla se desplaza interpolando <c>_knobT</c> hacia su destino en
/// _Process, que se auto-desactiva al terminar la animación para no consumir
/// frames en reposo. Los colores provienen de ui/Neumorphism.cs.
/// </remarks>
public partial class ToggleSwitch : Control
{
    [Signal]
    public delegate void ToggledEventHandler(bool on);

    private const float SwitchWidth = 52f;
    private const float SwitchHeight = 28f;
    private const float KnobPadding = 3f;
    private const float AnimSpeed = 8f;

    private bool _on;
    private float _knobT;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SwitchWidth, SwitchHeight);
        MouseDefaultCursorShape = CursorShape.PointingHand;
        _knobT = _on ? 1f : 0f;
    }

    /// <summary>Estado actual del interruptor.</summary>
    public bool IsOn => _on;

    /// <summary>
    /// Fija el estado. Con emit=false actualiza sin re-emitir la señal (para
    /// sincronizar con cambios externos, p. ej. la tecla M del mute).
    /// </summary>
    public void SetOn(bool on, bool emit = true)
    {
        _on = on;
        if (emit)
            EmitSignal(SignalName.Toggled, _on);
        SetProcess(true);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            SetOn(!_on);
            AcceptEvent();
        }
    }

    public override void _Process(double delta)
    {
        float target = _on ? 1f : 0f;
        _knobT = Mathf.MoveToward(_knobT, target, (float)delta * AnimSpeed);
        QueueRedraw();
        if (Mathf.IsEqualApprox(_knobT, target))
            SetProcess(false);
    }

    public override void _Draw()
    {
        float r = SwitchHeight / 2f;
        Color track = Neumorphism.TrackOff.Lerp(Neumorphism.TrackOn, _knobT);
        DrawCapsule(track, r);

        float knobX = Mathf.Lerp(r, SwitchWidth - r, _knobT);
        float knobR = r - KnobPadding;
        DrawCircle(new Vector2(knobX, r), knobR + 1f, Neumorphism.Shadow);
        DrawCircle(new Vector2(knobX, r), knobR, Neumorphism.Knob);
    }

    /// <summary>Dibuja la pista como cápsula (dos círculos + rectángulo central).</summary>
    private void DrawCapsule(Color color, float r)
    {
        DrawCircle(new Vector2(r, r), r, color);
        DrawCircle(new Vector2(SwitchWidth - r, r), r, color);
        DrawRect(new Rect2(r, 0f, SwitchWidth - SwitchHeight, SwitchHeight), color);
    }
}
