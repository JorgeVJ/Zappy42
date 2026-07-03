using Godot;
using System.Collections.Generic;

/// <summary>
/// Historial de mensajes crudos del servidor, agrupados en "franjas de tiempo"
/// (TimeBand) por proximidad de llegada.
/// </summary>
/// <remarks>
/// El servidor notifica los resultados de las acciones de los jugadores uno a
/// uno, pero los de un mismo tick llegan en una ráfaga muy próxima en tiempo
/// real; agruparlos da una granularidad de scrub con sentido ("qué pasó en
/// este momento") sin depender del protocolo de tiempo del servidor (sgt).
/// </remarks>
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
            TimeBand last = Bands[^1];
            last.EndIndex = idx;
            Bands[^1] = last;
        }
    }
}
