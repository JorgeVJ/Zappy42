/// <summary>
/// Orquesta la "barra de tiempo": acumula los mensajes recibidos del servidor en
/// un EventLog (agrupados en TimeBand) y permite saltar a una franja concreta
/// reseteando el mundo (Connection.ResetWorldState) y reproduciendo de forma
/// instantánea (Connection.ReplayInstant = true) los mensajes [0..franja].
/// </summary>
/// <remarks>
/// Mientras IsLive es true, los mensajes que llegan se despachan normalmente
/// (animados) y el cursor sigue siempre la última franja. Al saltar a una
/// franja anterior (JumpTo), IsLive pasa a false: los mensajes que sigan
/// llegando del servidor se acumulan en el log pero no se aplican hasta que se
/// vuelva a Live (GoLive).
/// </remarks>
public class TimelineController
{
    /// <summary>
    /// Intervalo base (segundos reales) entre el avance de una franja y la
    /// siguiente en modo Play. 0.6s da una cadencia "normal" perceptible sin
    /// resultar lenta; se escala por el factor de velocidad del servidor
    /// (Connection.CurrentSpeedFactor) para que Play vaya más rápido si la
    /// partida está acelerada (sgt &gt; 1).
    /// </summary>
    private const double BaseStepIntervalSeconds = 0.6;

    private readonly Connection _connection;
    private readonly MessageDispatcher _dispatcher;

    public readonly EventLog Log = new();

    /// <summary>
    /// -1 = mundo vacío (antes de la primera franja).
    /// </summary>
    public int CursorBandIndex { get; private set; } = -1;
    public bool IsLive { get; private set; } = true;

    /// <summary>
    /// True mientras el modo "Play" está avanzando franja a franja con el tiempo.
    /// </summary>
    public bool IsPlaying { get; private set; } = false;

    private double _playElapsedSeconds = 0.0;

    public TimelineController(Connection connection, MessageDispatcher dispatcher)
    {
        _connection = connection;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Llamado por Connection por cada línea recibida del transporte (real o mock).
    /// </summary>
    public void OnLineReceived(string line)
    {
        Log.Append(line);

        if (IsLive)
        {
            _dispatcher.Dispatch(line);
            CursorBandIndex = Log.Bands.Count - 1;
        }
    }

    /// <summary>
    /// Resetea el mundo y reproduce instantáneamente el log hasta el final de
    /// la franja indicada (-1 = mundo vacío). Deja IsLive activo solo si la
    /// franja destino es la última conocida.
    /// </summary>
    public void JumpTo(int bandIndex)
    {
        if (bandIndex < -1 || bandIndex >= Log.Bands.Count)
            return;

        Connection.ReplayInstant = true;
        try
        {
            _connection.ResetWorldState();

            if (bandIndex >= 0)
            {
                int endMessage = Log.Bands[bandIndex].EndIndex;
                for (int i = 0; i <= endMessage; i++)
                    _dispatcher.Dispatch(Log.Messages[i].Raw);
            }
        }
        finally
        {
            Connection.ReplayInstant = false;
        }

        CursorBandIndex = bandIndex;
        IsLive = bandIndex == Log.Bands.Count - 1;
    }

    /// <summary>
    /// Vuelve a la última franja conocida y reanuda el seguimiento en vivo.
    /// </summary>
    public void GoLive()
    {
        JumpTo(Log.Bands.Count - 1);
        IsLive = true;
        IsPlaying = false;
    }

    /// <summary>
    /// Inicia el modo "Play": avanza franja a franja con el tiempo real
    /// (ver Tick). No tiene efecto si ya estamos en Live (no hay nada que
    /// reproducir hacia delante) o si no quedan franjas por delante del cursor.
    /// </summary>
    public void Play()
    {
        if (IsLive || CursorBandIndex >= Log.Bands.Count - 1)
            return;

        IsPlaying = true;
        _playElapsedSeconds = 0.0;
    }

    /// <summary>
    /// Detiene el modo Play, dejando el cursor donde esté.
    /// </summary>
    public void Pause()
    {
        IsPlaying = false;
        _playElapsedSeconds = 0.0;
    }

    /// <summary>
    /// Llamado desde TimelineBar._Process con el delta de frame. Si IsPlaying,
    /// acumula tiempo real y, al superar el intervalo por franja (escalado por
    /// el factor de velocidad del servidor), avanza una franja con JumpTo.
    /// Al alcanzar la última franja conocida, pasa a Live y detiene Play.
    /// </summary>
    public void Tick(double delta)
    {
        if (!IsPlaying)
            return;

        float speedFactor = _connection?.CurrentSpeedFactor ?? 1f;
        if (speedFactor <= 0f)
            speedFactor = 1f;

        double interval = BaseStepIntervalSeconds / speedFactor;

        _playElapsedSeconds += delta;
        if (_playElapsedSeconds < interval)
            return;

        _playElapsedSeconds = 0.0;

        if (CursorBandIndex >= Log.Bands.Count - 1)
        {
            GoLive();
            return;
        }

        JumpTo(CursorBandIndex + 1);

        if (IsLive)
            IsPlaying = false;
    }
}
