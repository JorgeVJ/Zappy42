// Orquesta la "barra de tiempo": acumula los mensajes recibidos del servidor en
// un EventLog (agrupados en TimeBand) y permite saltar a una franja concreta
// reseteando el mundo (Connection.ResetWorldState) y reproduciendo de forma
// instantánea (Connection.ReplayInstant = true) los mensajes [0..franja].
//
// Mientras IsLive es true, los mensajes que llegan se despachan normalmente
// (animados) y el cursor sigue siempre la última franja. Al saltar a una
// franja anterior (JumpTo), IsLive pasa a false: los mensajes que sigan
// llegando del servidor se acumulan en el log pero no se aplican hasta que se
// vuelva a Live (GoLive).
public class TimelineController
{
    private readonly Connection _connection;
    private readonly MessageDispatcher _dispatcher;

    public readonly EventLog Log = new();

    // -1 = mundo vacío (antes de la primera franja).
    public int CursorBandIndex { get; private set; } = -1;
    public bool IsLive { get; private set; } = true;

    public TimelineController(Connection connection, MessageDispatcher dispatcher)
    {
        _connection = connection;
        _dispatcher = dispatcher;
    }

    // Llamado por Connection por cada línea recibida del transporte (real o mock).
    public void OnLineReceived(string line)
    {
        Log.Append(line);

        if (IsLive)
        {
            _dispatcher.Dispatch(line);
            CursorBandIndex = Log.Bands.Count - 1;
        }
    }

    // Resetea el mundo y reproduce instantáneamente el log hasta el final de
    // la franja indicada (-1 = mundo vacío). Deja IsLive activo solo si la
    // franja destino es la última conocida.
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

    // Vuelve a la última franja conocida y reanuda el seguimiento en vivo.
    public void GoLive()
    {
        JumpTo(Log.Bands.Count - 1);
        IsLive = true;
    }
}
