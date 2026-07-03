using Godot;

/// <summary>
/// UI de la barra de tiempo: HSlider + label de franja + botón de "Live" +
/// botón de Play/Pause.
/// </summary>
/// <remarks>
/// Consume la API de TimelineController (CursorBandIndex, Log.Bands.Count,
/// IsLive, IsPlaying, JumpTo, GoLive, Play, Pause, Tick) — ver
/// network/TimelineController.cs. Cableado por Connection en _Ready() vía
/// Setup().
/// </remarks>
public partial class TimelineBar : Control
{
    [Export]
    private HSlider _slider;
    [Export]
    private Label _statusLabel;
    [Export]
    private Button _liveButton;
    [Export]
    private Button _playPauseButton;

    private TimelineController _timeline;

    /// <summary>Mientras el usuario arrastra el slider, _Process no debe pisar su valor.</summary>
    private bool _dragging = false;

    /// <summary>Iconos del botón Play/Pause, cacheados para no recargarlos por frame.</summary>
    private Texture2D _playIcon;
    private Texture2D _pauseIcon;

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

        IconButton.Apply(_liveButton, "live", "Live");
        IconButton.Style(_playPauseButton);
        _playPauseButton.TooltipText = "Play/Pause";
        _playIcon  = IconButton.Load("play");
        _pauseIcon = IconButton.Load("pause");
        _playPauseButton.Icon = _playIcon;
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
        _liveButton.Disabled = _timeline.IsLive;

        _playPauseButton.Icon = _timeline.IsPlaying ? _pauseIcon : _playIcon;
        _playPauseButton.Disabled = _timeline.IsLive || _timeline.CursorBandIndex >= maxBand;
    }

    /// <summary>
    /// Al empezar a arrastrar el slider, pausar el modo Play (igual que ya
    /// ocurre con Live al saltar de franja vía JumpTo).
    /// </summary>
    private void OnDragStarted()
    {
        _dragging = true;
        _timeline?.Pause();
    }

    /// <summary>
    /// Al soltar el slider, saltar a la franja elegida (reset + replay
    /// instantáneo vía TimelineController.JumpTo).
    /// </summary>
    private void OnDragEnded(bool valueChanged)
    {
        _dragging = false;
        if (valueChanged && _timeline != null)
            _timeline.JumpTo((int)_slider.Value);
    }

    /// <summary>Alterna entre reproducir (avanzar franjas con el tiempo) y pausar.</summary>
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
