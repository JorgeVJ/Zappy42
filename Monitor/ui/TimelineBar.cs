using Godot;

// UI de la barra de tiempo: HSlider + label de franja + botón de "Live".
// Consume la API de TimelineController (CursorBandIndex, Log.Bands.Count,
// IsLive, JumpTo, GoLive) — ver network/TimelineController.cs. Cableado por
// Connection en _Ready() vía Setup().
public partial class TimelineBar : Control
{
    [Export] private HSlider _slider;
    [Export] private Label _statusLabel;
    [Export] private Button _liveButton;

    private TimelineController _timeline;

    // Mientras el usuario arrastra el slider, _Process no debe pisar su valor.
    private bool _dragging = false;

    public void Setup(TimelineController timeline)
    {
        _timeline = timeline;
    }

    public override void _Ready()
    {
        _slider.DragStarted += () => _dragging = true;
        _slider.DragEnded += OnDragEnded;
        _liveButton.Pressed += () => _timeline?.GoLive();
    }

    public override void _Process(double delta)
    {
        if (_timeline == null)
            return;

        int maxBand = _timeline.Log.Bands.Count - 1;
        if (!Mathf.IsEqualApprox(_slider.MaxValue, maxBand))
            _slider.MaxValue = maxBand;

        if (!_dragging)
            _slider.Value = _timeline.IsLive ? maxBand : _timeline.CursorBandIndex;

        _statusLabel.Text = maxBand < 0 ? "Sin datos" : $"Franja {(int)_slider.Value} / {maxBand}";
        _liveButton.Text = "● LIVE";
        _liveButton.Disabled = _timeline.IsLive;
    }

    // Al soltar el slider, saltar a la franja elegida (reset + replay
    // instantáneo vía TimelineController.JumpTo).
    private void OnDragEnded(bool valueChanged)
    {
        _dragging = false;
        if (valueChanged && _timeline != null)
            _timeline.JumpTo((int)_slider.Value);
    }
}
