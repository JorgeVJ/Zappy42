using Godot;
using System.Collections.Generic;

/// <summary>
/// Handlers de sistema: handshake (WELCOME), mapa/equipos (msz/bct/tna),
/// control de velocidad (sgt/OnSpeedChanged/ApplySpeedFactor) y mensajería
/// genérica del servidor (smg/seg/suc/sbp).
/// </summary>
public partial class Connection
{
    private readonly List<string> teams = new List<string>();

    /// <summary>
    /// Time unit que corresponde a velocidad normal (factor 1). El slider va de 1 a 10.
    /// </summary>
    private const float SpeedReference = 1f;
    private float _currentSpeedFactor = 1f;

    /// <summary>
    /// Factor de velocidad actual del servidor (derivado de sgt), usado por
    /// TimelineController para escalar el ritmo de reproducción del modo Play.
    /// </summary>
    public float CurrentSpeedFactor => _currentSpeedFactor;

    private void RegisterSystemHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("WELCOME", _ => OnWelcome());
        dispatcher.Register("msz", msz);
        dispatcher.Register("bct", bct);
        dispatcher.Register("tna", tna);
        dispatcher.Register("sgt", sgt);
        dispatcher.Register("seg", seg);
        dispatcher.Register("smg", smg);
        dispatcher.Register("suc", suc);
        dispatcher.Register("sbp", sbp);
    }

    /// <summary>
    /// Handshake: el servidor saluda con WELCOME; respondemos GRAPHIC y, de paso,
    /// pedimos el time unit actual (sgt sin argumentos) para que el slider de
    /// SpeedControlPanel arranque sincronizado con el valor real del servidor
    /// (la respuesta llega como "sgt T" y la procesa el handler sgt() ya existente).
    /// </summary>
    private void OnWelcome()
    {
        SendMessage("GRAPHIC");
        SendMessage("sgt");
    }

    private void sbp(string[] parts)
    {
        Log.Debug("[sbp] Parámetros inválidos en comando enviado al servidor");
    }

    private void suc(string[] parts)
    {
        Log.Debug("[suc] Comando desconocido recibido del servidor");
    }

    /// <summary>
    /// Mensaje de texto informativo del servidor (puede contener espacios). No
    /// implica fin de partida ni debe pausar la escena.
    /// </summary>
    private void smg(string[] parts)
    {
        if (parts.Length < 2)
            return;

        string message = string.Join(" ", parts, 1, parts.Length - 1);
        Log.Debug($"[smg] {message}");
    }

    private void seg(string[] parts)
    {
        if (!RequireLength(parts, 2, "seg"))
            return;

        string winner = parts[1];
        Log.Info($"[seg] ¡Juego terminado! Equipo ganador: {winner}");
        _teamPanel?.ShowWinner(winner);
        GetTree().Paused = true;
    }

    private void OnSpeedChanged(int t)
    {
        if (UseMockServer)
            _transport.SetMockSpeed(t);
        else
            SendMessage($"sst {t}");

        ApplySpeedFactor(t);
    }

    /// <summary>
    /// Propaga el factor de velocidad (derivado del time unit) a todos los jugadores,
    /// para escalar movimiento y animación. Se llama al cambiar el slider y al recibir sgt.
    /// </summary>
    private void ApplySpeedFactor(int t)
    {
        _currentSpeedFactor = Mathf.Max(1, t) / SpeedReference;
        foreach (Player player in playerManager.All)
            player.SetSpeedFactor(_currentSpeedFactor);
    }

    private void sgt(string[] parts)
    {
        if (!RequireLength(parts, 2, "sgt"))
            return;

        if (!TryParseField(parts[1], "sgt", "T", out int tick))
            return;

        Log.Debug($"[sgt] Tiempo actual del servidor: {tick}");
        _speedPanel?.SetDisplayValue(tick);
        ApplySpeedFactor(tick);
    }

    private void tna(string[] parts)
    {
        if (parts.Length < 2)
        {
            Log.Error("[tna] Formato incorrecto.");
            return;
        }

        string teamName = parts[1];
        if (!teams.Contains(teamName))
        {
            teams.Add(teamName);
        }

        _teamPanel?.RegisterTeam(teamName);
        Log.Debug($"[tna] Equipo registrado: {teamName}");
    }

    /// <summary>
    /// El terreno aún no está inicializado (no llegó msz) o las coordenadas
    /// están fuera de rango: el indexador devuelve null, descartar el mensaje.
    /// </summary>
    private void bct(string[] parts)
    {
        if (!RequireLength(parts, 3, "bct"))
            return;

        if (!TryParseField(parts[1], "bct", "X", out int x))
            return;
        if (!TryParseField(parts[2], "bct", "Y", out int y))
            return;

        Tile tile = terrainManager?[x, y];
        if (tile == null)
        {
            Log.Warn($"[bct] Tile ({x},{y}) fuera de rango o terreno sin inicializar; mensaje descartado.");
            return;
        }

        for (int i = 3; i < parts.Length; i++)
        {
            if (!TryParseField(parts[i], "bct", $"q{i - 3}", out int amount))
                return;

            tile.Inventory.Set((Resource.ResourceType)(i - 3), amount);
        }
    }

    private void msz(string[] parts)
    {
        if (!RequireLength(parts, 3, "msz"))
            return;

        if (!TryParseField(parts[1], "msz", "X", out int mapW))
            return;
        if (!TryParseField(parts[2], "msz", "Y", out int mapH))
            return;

        Log.Debug($"Mapa de tamaño: {mapW} x {mapH}");
        terrainManager.InitializeMap(mapW, mapH);
        SendMessage("mct");
    }
}
