/// <summary>
/// Rango inclusivo [StartIndex, EndIndex] sobre EventLog.Messages que agrupa
/// mensajes recibidos "juntos" (ver EventLog.BandGapMs).
/// </summary>
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
