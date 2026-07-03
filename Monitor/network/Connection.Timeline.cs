using Godot;

/// <summary>
/// Backend de la barra de tiempo: TimelineController agrupa los mensajes
/// recibidos en franjas (TimeBand) y, al saltar a una de ellas, resetea el
/// mundo (ResetWorldState) y reproduce el log de forma instantánea.
/// </summary>
public partial class Connection
{
    /// <summary>
    /// Activo mientras TimelineController reproduce el log (JumpTo). Los
    /// handlers con efectos visuales (movimiento, spawn de recursos,
    /// incantaciones/broadcasts) deben aplicar el resultado final sin animar.
    /// SendMessage() también se vuelve no-op para no reenviar comandos (mct,
    /// sgt, GRAPHIC...) al servidor real durante el replay.
    /// </summary>
    public static bool ReplayInstant = false;

    private TimelineController _timeline;

    /// <summary>
    /// Vuelve el mundo a su estado inicial (vacío) para que TimelineController
    /// pueda reproducir el log desde el principio.
    /// </summary>
    public void ResetWorldState()
    {
        playerManager.Clear();
        eggManager.Clear();
        terrainManager.Reset();

        _incantations.Clear();
        teams.Clear();
        _teamPanel?.Reset();

        _currentSpeedFactor = 1f;
        GetTree().Paused = false;
    }
}
