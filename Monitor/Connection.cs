using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

public partial class Connection : Node
{
    private TcpClient _client;
    private NetworkStream _stream;
    private byte[] _buffer = new byte[4096];
    private StringBuilder _incoming = new StringBuilder();

    private PlayerManager playerManager;
    private EggManager eggManager;

    private Node tileCollection;
    private Tile[,] tiles;

    private int mapW = 5;
    private int mapH = 5;

    private List<string> teams = new List<string>();
    
    private RandomNumberGenerator rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        tileCollection = GetNode("Tiles");
        playerManager = GetNode<PlayerManager>("PlayerManager");
        eggManager = GetNode<EggManager>("EggManager");
        try
        {
            _client = new TcpClient();
            _client.Connect("127.0.0.1", 12345);
            _stream = _client.GetStream();

            GD.Print("Conectado al servidor Zappy!");

            // AddRandomResources();
            SendMessage("msz");
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error al conectar: " + ex.Message);
        }
    }

    private void CreateMap()
    {
        tiles = new Tile[mapW, mapH];

        for (int x = 0; x < mapW; x++)
        {
            for (int y = 0; y < mapH; y++)
            {
                Tile tile = Tile.Create(new Vector3(x * 2, 0, y * 2));
                tileCollection.AddChild(tile);

                tiles[x, y] = tile;
            }
        }
    }

    private void AddRandomResources()
    {
        rng.Randomize();

        for (int x = 0; x < mapW; x++)
        {
            for (int y = 0; y < mapH; y++)
            {
                var tile = tiles[x, y];

                // ejemplo: 0-2 recursos por tipo (solo para test)
                tile.SetResourceCount(Resource.ResourceType.Nourriture, rng.RandiRange(0, 2));
                tile.SetResourceCount(Resource.ResourceType.Linemate, rng.RandiRange(0, 2));
                tile.SetResourceCount(Resource.ResourceType.Deraumere, rng.RandiRange(0, 2));
            }
        }
    }

    public void SendMessage(string msg)
    {
        if (_stream == null)
        {
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
        _stream.Write(data, 0, data.Length);
    }

    public override void _Process(double delta)
    {
        if (_stream == null || !_stream.DataAvailable)
        {
            return;
        }

        byte[] buffer = new byte[_client.Available];
        int bytesRead = _stream.Read(buffer, 0, buffer.Length);

        string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        string[] lines = msg.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            GD.Print("Processing line: " + line);
            HandleServerMessage(line);
        }
    }

    private void HandleServerMessage(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        switch (parts[0])
        {
            case "msz": msz(parts); break; // msz X Y\n msz\n Map size
            case "bct": bct(parts); break; // bct X Y q q q q q q q\n bct X Y\n Contents of a map tile
            case "tna": tna(parts); break; // tna N\n(× nbr teams) tna\n Team names
            case "pnw": pnw(parts); break; // pnw #n X Y O L N\n - New player connection
            case "ppo": ppo(parts); break; // ppo #n X Y O\n ppo #n\n Player position
            case "plv": plv(parts); break; // plv #n L\n plv #n\n Player level
            case "pin": pin(parts); break; // pin #n X Y q q q q q q q\n pin #n\n Player inventory
            case "pex": pex(parts); break; // pex #n\n - A player is expelled
            case "pbc": pbc(parts); break; // pbc #n M\n - A player broadcasts
            case "pic": pic(parts); break; // pic X Y L #n #n ...\n - Incantation started on the tile by a player
            case "pie": pie(parts); break; // pie X Y R\n - End of incantation with result R(0 or 1)
            case "pfk": pfk(parts); break; // pfk #n\n - The player lays an egg
            case "pdr": pdr(parts); break; // pdr #n i\n - The player drops a resource
            case "pgt": pgt(parts); break; // pgt #n i\n - The player takes a resource
            case "pdi": pdi(parts); break; // pdi #n\n - The player dies of hunger
            case "enw": enw(parts); break; // enw #e #n X Y\n - The egg is laid on the tile by a player
            case "eht": eht(parts); break; // eht #e\n - The egg hatches
            case "ebo": ebo(parts); break; // ebo #e\n - A player connects for an egg
            case "edi": edi(parts); break; // edi #e\n - The hatched egg dies of hunger
            case "sgt": sgt(parts); break; // sgt T\n sgt\n Request for current time unit
            case "seg": seg(parts); break; // seg N\n - End of game, team N wins
            case "smg": smg(parts); break; // smg M\n - Server message
            case "suc": suc(parts); break; // suc\n - Unknown command
            case "sbp": sbp(parts); break; // sbp\n - Bad parameters for the command

            default:
                GD.Print("Mensaje desconocido: " + line);
                break;
        }
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
        string winner = parts[1];
        GD.Print($"[seg] ¡Juego terminado! Equipo ganador: {winner}");
        // opcional: pausar la escena
        GetTree().Paused = true;
    }

    private void seg(string[] parts)
    {
        string winner = parts[1];
        GD.Print($"[seg] ¡Juego terminado! Equipo ganador: {winner}");
        // opcional: pausar la escena
        GetTree().Paused = true;
    }

    private void sgt(string[] parts)
    {
        int tick = int.Parse(parts[1]);
        GD.Print($"[sgt] Tiempo actual del servidor: {tick}");
    }

    private void edi(string[] parts)
    {
        // edi #e
        int eggId = int.Parse(parts[1].TrimStart('#'));

        if (!eggManager.TryGet(eggId, out var egg))
        {
            GD.PrintErr($"[edi] Egg #{eggId} no existe.");
            return;
        }

        eggManager.Remove(eggId);

        GD.Print($"[edi] Egg #{eggId} murio de hambre.");
    }

    private void ebo(string[] parts)
    {
        // ebo #e
        int eggId = int.Parse(parts[1].TrimStart('#'));

        if (!eggManager.TryGet(eggId, out var egg))
        {
            GD.PrintErr($"[ebo] Egg #{eggId} no existe.");
            return;
        }

        // eliminamos el huevo porque el jugador se conecta
        eggManager.Remove(eggId);

        GD.Print($"[ebo] Egg #{eggId} ahora es controlado por un jugador (se conecto).");
    }

    private void eht(string[] parts)
    {
        // eht #e
        int eggId = int.Parse(parts[1].TrimStart('#'));

        if (!eggManager.TryGet(eggId, out var egg))
        {
            GD.PrintErr($"[eht] Egg #{eggId} no existe.");
            return;
        }

        eggManager.Remove(eggId);

        GD.Print($"[eht] Egg #{eggId} ha eclosionado.");
    }

    private void enw(string[] parts)
    {
        // enw #e #n X Y
        int eggId = int.Parse(parts[1].TrimStart('#'));
        int playerId = int.Parse(parts[2].TrimStart('#'));
        int x = int.Parse(parts[3]);
        int y = int.Parse(parts[4]);

        Vector3 worldPos = new Vector3(x * 2, 0.3f, y * 2);

        var egg = eggManager.CreateEgg(eggId, worldPos);

        GD.Print($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
    }

    private void pdi(string[] parts)
    {
        // pdi #n
        int id = int.Parse(parts[1].TrimStart('#'));

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pdi] Player #{id} no existe.");
            return;
        }

        playerManager.Remove(id);

        GD.Print($"[pdi] Player #{id} murio (eliminado).");
    }

    private void pgt(string[] parts)
    {
        // pgt #n i
        int id = int.Parse(parts[1].TrimStart('#'));
        int itemId = int.Parse(parts[2]);

        var type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pgt] Player #{id} no existe todavia.");
            return;
        }

        // +1 al inventario local (si lo estás usando)
        player.AddToInventory(type, 1);
        var tilePos = player.TilePos;
        tiles[tilePos.X, tilePos.Y].PickResource(type);
        GD.Print($"[pgt] Player #{id} tomo {type}");
    }

    private void pdr(string[] parts)
    {
        // pdr #n i
        int id = int.Parse(parts[1].TrimStart('#'));
        int itemId = int.Parse(parts[2]);

        var type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pdr] Player #{id} no existe todavia.");
            return;
        }

        player.RemoveFromInventory(type, 1);

        GD.Print($"[pdr] Player #{id} dejo {type}");
    }

    private void pfk(string[] parts)
    {
        // pfk #n
        int playerId = int.Parse(parts[1].TrimStart('#'));

        if (!playerManager.TryGet(playerId, out var player))
        {
            GD.PrintErr($"[pfk] Player #{playerId} no encontrado");
            return;
        }

        GD.Print($"[pfk] Player #{playerId} esta poniendo un huevo");

        // Highlight del tile donde está el jugador
        Vector2I tilePos = player.TilePos;

        if (tilePos.X < 0 || tilePos.X >= mapW || tilePos.Y < 0 || tilePos.Y >= mapH)
            return;

        var tile = tiles[tilePos.X, tilePos.Y];

        // Color "pre-huevo" (verde amarillento)
        tile.Highlight(new Color(0.7f, 0.9f, 0.3f, 0.45f));

        // Opcional: feedback visual en el jugador
        // player.Flash(new Color(1f, 1f, 0.4f));
    }

    private async System.Threading.Tasks.Task FadeOutTile(Node3D mesh, float seconds)
    {
        await System.Threading.Tasks.Task.Delay((int)(seconds * 1000));
        if (mesh != null && mesh.IsInsideTree())
            mesh.QueueFree();
    }

    private void pie(string[] parts)
    {
        // pie X Y R
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        int result = int.Parse(parts[3]);

        GD.Print($"[pie] Incantacion en tile ({x},{y}) {(result == 1 ? "EXITOSA" : "FALLIDA")}");

        if (x < 0 || x >= mapW || y < 0 || y >= mapH)
            return;

        var tile = tiles[x, y];

        // elegir color según resultado
        Color color = result == 1 ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        tile.Highlight(color);

        // opcional: eliminar highlight después de 1 segundo
        _ = FadeOutTile(tile, 1.0f);
    }

    private void pic(string[] parts)
    {
        // pic X Y L #n #n ...
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        int level = int.Parse(parts[3]);

        // jugadores involucrados
        List<int> playerIds = new List<int>();
        for (int i = 4; i < parts.Length; i++)
            playerIds.Add(int.Parse(parts[i].TrimStart('#')));

        GD.Print($"[pic] Incantacion en tile ({x},{y}) nivel {level} con jugadores: {string.Join(",", playerIds)}");

        if (x < 0 || x >= mapW || y < 0 || y >= mapH)
            return;

        // GrayBox: resaltar tile
        tiles[x, y].Highlight(new Color(1f, 0.5f, 0f, 0.5f)); // naranja semi-transparente
    }

    private async void ShowPlayerMessage(Player player, string msg)
    {
        var label = new Label3D();
        label.Text = msg;
        label.Position = new Vector3(0, 1.5f, 0); // encima del jugador
        player.AddChild(label);

        // desaparecer después de 2 segundos
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        label.QueueFree();
    }

    private void pbc(string[] parts)
    {
        // pbc #n M
        int id = int.Parse(parts[1].TrimStart('#'));

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pbc] Player #{id} no existe.");
            return;
        }

        // reconstruimos el mensaje (puede contener espacios)
        string message = string.Join(" ", parts, 2, parts.Length - 2);

        GD.Print($"[pbc] Player #{id} dice: {message}");

        // ⭐ Opción visual rápida:
        // podrías añadir un Label3D encima del jugador para mostrar temporalmente
        ShowPlayerMessage(player, message);
    }

    private void pex(string[] parts)
    {
        // pex #n
        int id = int.Parse(parts[1].TrimStart('#'));

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pex] Player #{id} no existe.");
            return;
        }

        GD.Print($"[pex] Player #{id} expulso a otros jugadores.");
    }

    private void pin(string[] parts)
    {
        // pin #n X Y q0 q1 q2 q3 q4 q5 q6
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[pin] Player #{id} no existe todavia.");
            return;
        }

        // Actualizamos también su posición (viene incluida en pin)
        Vector3 worldPos = new Vector3(x * 2, 0.3f, y * 2);
        player.Position = worldPos;
        player.SetTilePos(x, y);
        int[] inv = new int[7];
        for (int i = 0; i < 7; i++)
            inv[i] = int.Parse(parts[4 + i]);

        player.SetInventory(inv);

        GD.Print($"[pin] Player #{id} inv=({string.Join(",", inv)})");
    }

    private void plv(string[] parts)
    {
        // plv #n L
        int id = int.Parse(parts[1].TrimStart('#'));
        int level = int.Parse(parts[2]);

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[plv] Player #{id} no existe todavia.");
            return;
        }

        player.SetLevel(level);

        GD.Print($"[plv] Player #{id} -> level {level}");
    }

    private void ppo(string[] parts)
    {
        // ppo #n X Y O
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);
        int o = int.Parse(parts[4]);

        if (!playerManager.TryGet(id, out var player))
        {
            GD.PrintErr($"[ppo] Player #{id} no existe todavia.");
            return;
        }

        Vector3 worldPos = new Vector3(x * 2, 0.3f, y * 2);
        player.Position = worldPos;
        player.SetTilePos(x, y);
        player.SetOrientation(o);

        // GD.Print($"[ppo] Player #{id} -> ({x},{y}) o={o}");
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
            teams.Add(teamName);

        GD.Print($"[tna] Equipo registrado: {teamName}");
    }

    private void pnw(string[] parts)
    {
        // pnw #n X Y O L N
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);
        int o = int.Parse(parts[4]);
        int level = int.Parse(parts[5]);
        string team = parts[6];

        // convertir coords tile -> mundo
        Vector3 worldPos = new Vector3(x * 2, 0.3f, y * 2);

        var player = playerManager.GetOrCreate(id, worldPos, team);

        // si ya existía, lo actualizamos también
        player.Position = worldPos;
        player.SetOrientation(o);
        player.SetLevel(level);

        GD.Print($"[pnw] Player #{id} team={team} pos=({x},{y}) o={o} lvl={level}");
    }

    private void bct(string[] parts)
    {
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);

        // Recursos son lo que viene a partir del índice 3
        for (int i = 3; i < parts.Length; i++)
        {
            tiles[x, y].SetResourceCount((Resource.ResourceType)(i - 3), int.Parse(parts[i]));
        }
    }

    private void msz(string[] parts)
    {
        mapW = int.Parse(parts[1]);
        mapH = int.Parse(parts[2]);
        GD.Print($"Mapa de tamaño: {mapW} x {mapH}");
        CreateMap();
        SendMessage("mct");
    }
}
