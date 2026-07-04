/// <summary>
/// Línea cruda recibida del servidor junto con el instante de recepción
/// (Time.GetTicksMsec()), usado por EventLog para agrupar mensajes en franjas.
/// </summary>
public struct LogEntry
{
    public string Raw;
    public double ReceivedAtMs;

    public LogEntry(string raw, double receivedAtMs)
    {
        Raw = raw;
        ReceivedAtMs = receivedAtMs;
    }
}
