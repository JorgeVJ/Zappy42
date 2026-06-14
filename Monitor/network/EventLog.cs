using Godot;
using System.Collections.Generic;

// Línea cruda recibida del servidor junto con el instante de recepción
// (Time.GetTicksMsec()), usado por EventLog para agrupar mensajes en franjas.
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

// Rango inclusivo [StartIndex, EndIndex] sobre EventLog.Messages que agrupa
// mensajes recibidos "juntos" (ver EventLog.BandGapMs).
public struct TimeBand
{
    public int StartIndex;
    public int EndIndex;

    public TimeBand(int startIndex, int endIndex)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }
}

// Historial de mensajes crudos del servidor, agrupados en "franjas de tiempo"
// (TimeBand) por proximidad de llegada. El servidor notifica los resultados de
// las acciones de los jugadores uno a uno, pero los de un mismo tick llegan en
// una ráfaga muy próxima en tiempo real; agruparlos da una granularidad de
// scrub con sentido ("qué pasó en este momento") sin depender del protocolo de
// tiempo del servidor (sgt).
public class EventLog
{
    private const double BandGapMs = 100.0;

    public List<LogEntry> Messages { get; } = new();
    public List<TimeBand> Bands { get; } = new();

    public void Append(string raw)
    {
        double now = Time.GetTicksMsec();
        Messages.Add(new LogEntry(raw, now));
        int idx = Messages.Count - 1;

        if (Bands.Count == 0 || now - Messages[Bands[^1].EndIndex].ReceivedAtMs > BandGapMs)
        {
            Bands.Add(new TimeBand(idx, idx));
        }
        else
        {
            var last = Bands[^1];
            last.EndIndex = idx;
            Bands[^1] = last;
        }
    }
}
