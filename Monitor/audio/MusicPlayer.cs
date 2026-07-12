using Godot;

/// <summary>
/// Música de fondo del monitor.
/// </summary>
/// <remarks>
/// Vive como hijo directo de "Game" (sibling de "Connection"/"Terrain"), fuera de
/// la capa de UI y NO dentro de Connection: así Connection.ResetWorldState()
/// (replay/reset de la barra de tiempo) no la toca ni la reinicia.
/// ProcessMode = Always para que siga sonando aunque GetTree().Paused = true
/// (fin de partida, handler "seg" en Connection.System.cs).
/// Loop: el asset audio/music.mp3 tiene loop=false en su .import (generado, no
/// se toca). Se reproduce en bucle manualmente reconectando Play() al terminar
/// (señal Finished), lo que funciona sin depender del tipo de AudioStream.
/// Mute: no tiene control propio en pantalla; lo gobierna el interruptor "Sonido"
/// del SettingsPanel (vía SetMuted()/señal MutedChanged) y el atajo de tecla M
/// (_UnhandledInput), que alternan entre VolumeDb normal y silencio.
/// </remarks>
public partial class MusicPlayer : Node
{
    [Signal]
    public delegate void MutedChangedEventHandler(bool muted);

    [Export]
    public string MusicPath = "res://audio/music.mp3";

    [Export]
    public float NormalVolumeDb = -10f;

    [Export]
    public float MutedVolumeDb = -80f;

    [Export]
    private AudioStreamPlayer _player;

    private bool _muted;

    /// <summary>Estado actual de silencio.</summary>
    public bool IsMuted => _muted;

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

    public void ToggleMute() => SetMuted(!_muted);

    public void SetMuted(bool muted)
    {
        _muted = muted;
        _player.VolumeDb = _muted ? MutedVolumeDb : NormalVolumeDb;
        EmitSignal(SignalName.MutedChanged, _muted);
    }
}
