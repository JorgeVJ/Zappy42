using Godot;

/// <summary>
/// Música de fondo del monitor.
/// </summary>
/// <remarks>
/// Vive como hijo directo de "Game" (sibling de "Connection"/"Terrain"), NO dentro
/// de Connection: así Connection.ResetWorldState() (replay/reset de la barra de
/// tiempo) no la toca ni la reinicia.
/// ProcessMode = Always para que siga sonando aunque GetTree().Paused = true
/// (fin de partida, handler "seg" en Connection.System.cs).
/// Loop: el asset audio/music.mp3 tiene loop=false en su .import (generado, no
/// se toca). Se reproduce en bucle manualmente reconectando Play() al terminar
/// (señal Finished), lo que funciona sin depender del tipo de AudioStream.
/// Mute: botón de icono (altavoz / altavoz tachado) en la esquina superior
/// derecha (Button en modo toggle, estilo unificado de ui/IconButton.cs) y
/// tecla M (_UnhandledInput), ambos alternan entre VolumeDb normal y silencio.
/// </remarks>
public partial class MusicPlayer : Control
{
    [Export]
    public string MusicPath = "res://audio/music.mp3";

    [Export]
    public float NormalVolumeDb = -10f;

    [Export]
    public float MutedVolumeDb = -80f;

    [Export]
    private AudioStreamPlayer _player;

    [Export]
    private Button _muteButton;

    private bool _muted;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        AudioStream stream = GD.Load<AudioStream>(MusicPath);
        if (stream != null)
        {
            _player.Stream = stream;
            _player.VolumeDb = NormalVolumeDb;
            _player.Finished += OnFinished;
            _player.Play();
        }
        else
        {
            Log.Warn($"[MusicPlayer] No se pudo cargar {MusicPath}");
        }

        if (_muteButton != null)
        {
            _muteButton.ToggleMode = true;
            IconButton.Style(_muteButton);
            _muteButton.Toggled += OnMuteToggled;
            _muteButton.ButtonPressed = _muted;
            UpdateMuteButtonIcon();
        }
    }

    /// <summary>Reinicia la pista al terminar -&gt; bucle manual (el .import tiene loop=false).</summary>
    private void OnFinished()
    {
        _player.Play();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.M)
        {
            ToggleMute();
        }
    }

    private void OnMuteToggled(bool pressed)
    {
        SetMuted(pressed);
    }

    public void ToggleMute() => SetMuted(!_muted);

    public void SetMuted(bool muted)
    {
        _muted = muted;
        _player.VolumeDb = _muted ? MutedVolumeDb : NormalVolumeDb;

        if (_muteButton != null)
        {
            _muteButton.SetPressedNoSignal(_muted);
            UpdateMuteButtonIcon();
        }
    }

    private void UpdateMuteButtonIcon()
    {
        _muteButton.Icon = IconButton.Load(_muted ? "mute" : "sound");
        _muteButton.TooltipText = _muted ? "Unmute" : "Mute";
    }
}
