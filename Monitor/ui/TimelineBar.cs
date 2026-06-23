using Godot;

// UI de la barra de tiempo: HSlider + label de franja + botón de "Live" +
// botón de Play/Pause. Consume la API de TimelineController (CursorBandIndex,
// Log.Bands.Count, IsLive, IsPlaying, JumpTo, GoLive, Play, Pause, Tick) — ver
// network/TimelineController.cs. Cableado por Connection en _Ready() vía
// Setup().
public partial class TimelineBar : Control
{
    [Export] private HSlider _slider;
    [Export] private Label _statusLabel;
    [Export] private Button _liveButton;
    [Export] private Button _playPauseButton;

    private TimelineController _timeline;

    // Mientras el usuario arrastra el slider, _Process no debe pisar su valor.
    private bool _dragging = false;

    public void Setup(TimelineController timeline)
    {
        _timeline = timeline;
    }

    public override void _Ready()
    {
        _slider.DragStarted += OnDragStarted;
        _slider.DragEnded += OnDragEnded;
        _liveButton.Pressed += () => _timeline?.GoLive();
        _playPauseButton.Pressed += OnPlayPausePressed;
    }

    public override void _Process(double delta)
    {
        if (_timeline == null)
            return;

        _timeline.Tick(delta);

        int maxBand = _timeline.Log.Bands.Count - 1;
        if (!Mathf.IsEqualApprox(_slider.MaxValue, maxBand))
            _slider.MaxValue = maxBand;

        if (!_dragging)
            _slider.Value = _timeline.IsLive ? maxBand : _timeline.CursorBandIndex;

        _statusLabel.Text = maxBand < 0 ? "Sin datos" : $"Franja {(int)_slider.Value} / {maxBand}";
        _liveButton.Text = "● LIVE";
        _liveButton.Disabled = _timeline.IsLive;

        _playPauseButton.Text = _timeline.IsPlaying ? "⏸" : "▶";
        _playPauseButton.Disabled = _timeline.IsLive || _timeline.CursorBandIndex >= maxBand;
    }

    // Al empezar a arrastrar el slider, pausar el modo Play (igual que ya
    // ocurre con Live al saltar de franja vía JumpTo).
    private void OnDragStarted()
    {
        _dragging = true;
        _timeline?.Pause();
    }

    // Al soltar el slider, saltar a la franja elegida (reset + replay
    // instantáneo vía TimelineController.JumpTo).
    private void OnDragEnded(bool valueChanged)
    {
        _dragging = false;
        if (valueChanged && _timeline != null)
            _timeline.JumpTo((int)_slider.Value);
    }

    // Alterna entre reproducir (avanzar franjas con el tiempo) y pausar.
    private void OnPlayPausePressed()
    {
        if (_timeline == null)
            return;

        if (_timeline.IsPlaying)
            _timeline.Pause();
        else
            _timeline.Play();
    }
}
