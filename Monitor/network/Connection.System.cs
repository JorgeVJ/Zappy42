using Godot;
using System.Collections.Generic;

// Handlers de sistema: handshake (WELCOME), mapa/equipos (msz/bct/tna),
// control de velocidad (sgt/OnSpeedChanged/ApplySpeedFactor) y mensajería
// genérica del servidor (smg/seg/suc/sbp).
public partial class Connection
{
    private readonly List<string> teams = new List<string>();

    // Time unit que corresponde a velocidad normal (factor 1). El slider va de 1 a 10.
    private const float SpeedReference = 1f;
    private float _currentSpeedFactor = 1f;

    private void RegisterSystemHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("WELCOME", _ => OnWelcome()); // handshake: el servidor saluda; respondemos GRAPHIC
        dispatcher.Register("msz", msz); // msz X Y\n msz\n Map size
        dispatcher.Register("bct", bct); // bct X Y q q q q q q q\n bct X Y\n Contents of a map tile
        dispatcher.Register("tna", tna); // tna N\n(× nbr teams) tna\n Team names
        dispatcher.Register("sgt", sgt); // sgt T\n sgt\n Request for current time unit
        dispatcher.Register("seg", seg); // seg N\n - End of game, team N wins
        dispatcher.Register("smg", smg); // smg M\n - Server message
        dispatcher.Register("suc", suc); // suc\n - Unknown command
        dispatcher.Register("sbp", sbp); // sbp\n - Bad parameters for the command
    }

    // Handshake: el servidor saluda con WELCOME; respondemos GRAPHIC y, de paso,
    // pedimos el time unit actual (sgt sin argumentos) para que el slider de
    // SpeedControlPanel arranque sincronizado con el valor real del servidor
    // (la respuesta llega como "sgt T" y la procesa el handler sgt() ya existente).
    private void OnWelcome()
    {
        SendMessage("GRAPHIC");
        SendMessage("sgt");
    }

    private void sbp(string[] parts)
    {
        GD.Print("[sbp] Parámetros inválidos en comando enviado al servidor");
    }

    private void suc(string[] parts)
    {
        GD.Print("[suc] Comando desconocido recibido del servidor");
    }

    private void smg(string[] parts)
    {
        // smg M — mensaje de texto informativo del servidor (puede contener espacios).
        // NO implica fin de partida ni debe pausar la escena (antes era copia de seg).
        if (parts.Length < 2)
            return;

        string message = string.Join(" ", parts, 1, parts.Length - 1);
        GD.Print($"[smg] {message}");
        // El mensaje ya queda visible en MessageLogPanel vía OnLineReceived.
    }

    private void seg(string[] parts)
    {
        string winner = parts[1];
        GD.Print($"[seg] ¡Juego terminado! Equipo ganador: {winner}");
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

    // Propaga el factor de velocidad (derivado del time unit) a todos los jugadores,
    // para escalar movimiento y animación. Se llama al cambiar el slider y al recibir sgt.
    private void ApplySpeedFactor(int t)
    {
        _currentSpeedFactor = Mathf.Max(1, t) / SpeedReference;
        foreach (var player in playerManager.All)
            player.SetSpeedFactor(_currentSpeedFactor);
    }

    private void sgt(string[] parts)
    {
        int tick = int.Parse(parts[1]);
        GD.Print($"[sgt] Tiempo actual del servidor: {tick}");
        _speedPanel?.SetDisplayValue(tick);
        ApplySpeedFactor(tick);
    }

    private void tna(string[] parts)
    {
        if (parts.Length < 2)
        {
            GD.PrintErr("[tna] Formato incorrecto.");
            return;
        }

        string teamName = parts[1];
        if (!teams.Contains(teamName))
        {
            teams.Add(teamName);
        }

        _teamPanel?.RegisterTeam(teamName);
        GD.Print($"[tna] Equipo registrado: {teamName}");
    }

    private void bct(string[] parts)
    {
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);

        // Recursos son lo que viene a partir del índice 3
        for (int i = 3; i < parts.Length; i++)
        {
            terrainManager[x, y].Inventory.Set((Resource.ResourceType)(i - 3), int.Parse(parts[i]));
        }
    }

    private void msz(string[] parts)
    {
        var mapW = int.Parse(parts[1]);
        var mapH = int.Parse(parts[2]);
        GD.Print($"Mapa de tamaño: {mapW} x {mapH}");
        //CreateMap();
        terrainManager.InitializeMap(mapW, mapH);
        SendMessage("mct");
    }
}
