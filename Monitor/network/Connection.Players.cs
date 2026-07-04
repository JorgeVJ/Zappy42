using Godot;
using System.Collections.Generic;
using zappy;

/// <summary>
/// Handlers de jugadores: spawn/movimiento/nivel/inventario (pnw/ppo/plv/pin),
/// expulsión y muerte (pex/pdi), recursos (pgt/pdr), huevos/incantaciones
/// (pfk/pic/pie) y broadcasts (pbc), más los efectos visuales asociados
/// (mensajes flotantes, ondas de sonido, resaltado de incantación).
/// </summary>
public partial class Connection
{
    /// <summary>
    /// Jugadores incantando por tile (pic), para terminar su animación al recibir pie.
    /// </summary>
    private readonly Dictionary<(int, int), List<int>> _incantations = new();

    private void RegisterPlayerHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("pnw", pnw);
        dispatcher.Register("ppo", ppo);
        dispatcher.Register("plv", plv);
        dispatcher.Register("pin", pin);
        dispatcher.Register("pex", pex);
        dispatcher.Register("pbc", pbc);
        dispatcher.Register("pic", pic);
        dispatcher.Register("pie", pie);
        dispatcher.Register("pfk", pfk);
        dispatcher.Register("pdr", pdr);
        dispatcher.Register("pgt", pgt);
        dispatcher.Register("pdi", pdi);
    }

    private void pdi(string[] parts)
    {
        if (!RequireLength(parts, 2, "pdi"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pdi", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pdi] Player #{id} no existe.");
            return;
        }

        playerManager.Remove(id);
        _teamPanel?.RemovePlayer(id);

        Log.Debug($"[pdi] Player #{id} murio (eliminado).");
    }

    private void pgt(string[] parts)
    {
        if (!RequireLength(parts, 3, "pgt"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pgt", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pgt", "i", out int itemId))
            return;

        Resource.ResourceType type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pgt] Player #{id} no existe todavia.");
            return;
        }

        player.Inventory.Add(type, 1);
        Vector2I tilePos = player.TilePos;
        terrainManager[tilePos.X, tilePos.Y]?.Inventory.Remove(type, 1);
        _teamPanel?.SetLastAction(id, $"+ {type}");
        player.PlayCollect();
        Log.Debug($"[pgt] Player #{id} tomo {type}");
    }

    private void pdr(string[] parts)
    {
        if (!RequireLength(parts, 3, "pdr"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pdr", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pdr", "i", out int itemId))
            return;

        Resource.ResourceType type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pdr] Player #{id} no existe todavia.");
            return;
        }

        player.Inventory.Remove(type, 1);

        _teamPanel?.SetLastAction(id, $"- {type}");
        player.PlayPickUp();
        Log.Debug($"[pdr] Player #{id} dejo {type}");
    }

    private void pfk(string[] parts)
    {
        if (!RequireLength(parts, 2, "pfk"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pfk", "#n", out int playerId))
            return;

        if (!playerManager.TryGet(playerId, out Player player))
        {
            Log.Error($"[pfk] Player #{playerId} no encontrado");
            return;
        }

        Log.Debug($"[pfk] Player #{playerId} esta poniendo un huevo");
        _teamPanel?.SetLastAction(playerId, "🥚 pone huevo");
    }

    private void pie(string[] parts)
    {
        if (!RequireLength(parts, 4, "pie"))
            return;

        if (!TryParseField(parts[1], "pie", "X", out int x))
            return;
        if (!TryParseField(parts[2], "pie", "Y", out int y))
            return;
        if (!TryParseField(parts[3], "pie", "R", out int result))
            return;

        bool success = result == 1;

        Log.Debug($"[pie] Incantacion en tile ({x},{y}) {(success ? "EXITOSA" : "FALLIDA")}");

        StopIncantationSpells(x, y);
        ShowIncantationOutcome(x, y, success);
    }

    /// <summary>
    /// Termina la animación de hechizo de los jugadores que estaban incantando
    /// en el tile indicado, al recibir el resultado de la incantación (pie).
    /// </summary>
    private void StopIncantationSpells(int x, int y)
    {
        if (!_incantations.TryGetValue((x, y), out List<int> playerIds))
            return;

        foreach (int pid in playerIds)
        {
            if (playerManager.TryGet(pid, out Player player))
                player.StopSpell();
        }
        _incantations.Remove((x, y));
    }

    /// <summary>
    /// Quita el resaltado del tile y muestra un pulso de color según el resultado
    /// (verde = éxito, rojo = fallo) reutilizando el efecto SoundWave. El pulso es
    /// un efecto transitorio sin estado persistente: se omite durante el replay
    /// instantáneo de la barra de tiempo.
    /// </summary>
    private void ShowIncantationOutcome(int x, int y, bool success)
    {
        terrainManager?.DeselectTile();
        if (!ReplayInstant)
            ShowIncantationResult(x, y, success);
    }

    private void pic(string[] parts)
    {
        if (!RequireLength(parts, 4, "pic"))
            return;

        if (!TryParseField(parts[1], "pic", "X", out int x))
            return;
        if (!TryParseField(parts[2], "pic", "Y", out int y))
            return;
        if (!TryParseField(parts[3], "pic", "L", out int level))
            return;

        if (!TryParsePicPlayerIds(parts, out List<int> playerIds))
            return;

        Log.Debug($"[pic] Incantacion en tile ({x},{y}) nivel {level} con jugadores: {string.Join(",", playerIds)}");

        _incantations[(x, y)] = playerIds;
        StartIncantationSpells(playerIds, level);
    }

    /// <summary>
    /// Parsea la lista de jugadores involucrados en una incantación (pic).
    /// </summary>
    private bool TryParsePicPlayerIds(string[] parts, out List<int> playerIds)
    {
        playerIds = new List<int>();
        for (int i = 4; i < parts.Length; i++)
        {
            if (!TryParseField(parts[i].TrimStart('#'), "pic", $"#n[{i - 4}]", out int pid))
                return false;

            playerIds.Add(pid);
        }
        return true;
    }

    private void StartIncantationSpells(List<int> playerIds, int level)
    {
        foreach (int pid in playerIds)
        {
            _teamPanel?.SetLastAction(pid, $"✨ incant. Nv.{level}");
            if (playerManager.TryGet(pid, out Player player))
                player.PlaySpell();
        }
    }

    /// <summary>
    /// Pulso de color sobre el tile al terminar una incantación (verde éxito / rojo fallo).
    /// </summary>
    private void ShowIncantationResult(int x, int y, bool success)
    {
        if (terrainManager == null || terrainManager[x, y] == null)
            return;

        Color color = success ? new Color(0.3f, 1f, 0.4f, 0.85f) : new Color(1f, 0.3f, 0.3f, 0.85f);
        Vector3 center = TerrainSnap.TileCenter(terrainManager, x, y, Terrain.EntityGroundOffset);
        SoundWave wave = SoundWave.Create(center, color);
        terrainManager.AddChild(wave);
    }

    private async void ShowPlayerMessage(Player player, string msg)
    {
        Label3D label = new Label3D();
        label.Text = msg;
        label.Position = new Vector3(0, 1.5f, 0);
        player.AddChild(label);

        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        if (IsInstanceValid(label))
            label.QueueFree();
    }

    private void pbc(string[] parts)
    {
        if (!RequireLength(parts, 3, "pbc"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pbc", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pbc] Player #{id} no existe.");
            return;
        }

        string message = string.Join(" ", parts, 2, parts.Length - 2);

        Log.Debug($"[pbc] Player #{id} dice: {message}");
        _teamPanel?.SetLastAction(id, $"📢 {message}");

        ShowBroadcastEffects(player, message);
    }

    /// <summary>
    /// Globo de texto y onda de sonido son efectos transitorios sin estado
    /// persistente: se omiten durante el replay instantáneo de la barra de tiempo.
    /// </summary>
    private void ShowBroadcastEffects(Player player, string message)
    {
        if (ReplayInstant)
            return;

        ShowPlayerMessage(player, message);
        ShowSoundWave(player);
    }

    /// <summary>
    /// Expanding ground ring centered on the emitter's tile, visualizing the
    /// broadcast as sound propagating outward. Complements the floating text.
    /// </summary>
    private void ShowSoundWave(Player player)
    {
        if (terrainManager == null)
            return;

        Vector3 center = TerrainSnap.TileCenter(terrainManager, player.TilePos.X, player.TilePos.Y, Terrain.EntityGroundOffset);
        SoundWave wave = SoundWave.Create(center);
        terrainManager.AddChild(wave);
    }

    private void pex(string[] parts)
    {
        if (!RequireLength(parts, 2, "pex"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pex", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pex] Player #{id} no existe.");
            return;
        }

        Log.Debug($"[pex] Player #{id} expulso a otros jugadores.");
        _teamPanel?.SetLastAction(id, "💨 expulsó");
    }

    private void pin(string[] parts)
    {
        if (!RequireLength(parts, 11, "pin"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pin", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pin", "X", out int x))
            return;
        if (!TryParseField(parts[3], "pin", "Y", out int y))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[pin] Player #{id} no existe todavía.");
            return;
        }

        player.SetTilePos(x, y);

        if (!TryApplyInventory(parts, player))
            return;

        Log.Debug($"[pin] Player #{id} inventario actualizado");
    }

    /// <summary>
    /// Parsea y aplica los 7 campos de cantidad de recursos del mensaje pin
    /// al inventario del jugador.
    /// </summary>
    private bool TryApplyInventory(string[] parts, Player player)
    {
        if (!TryParseField(parts[4], "pin", "q0 (Nourriture)", out int nourriture)) return false;
        if (!TryParseField(parts[5], "pin", "q1 (Linemate)", out int linemate)) return false;
        if (!TryParseField(parts[6], "pin", "q2 (Deraumere)", out int deraumere)) return false;
        if (!TryParseField(parts[7], "pin", "q3 (Sibur)", out int sibur)) return false;
        if (!TryParseField(parts[8], "pin", "q4 (Mendiane)", out int mendiane)) return false;
        if (!TryParseField(parts[9], "pin", "q5 (Phiras)", out int phiras)) return false;
        if (!TryParseField(parts[10], "pin", "q6 (Thystame)", out int thystame)) return false;

        Inventory inv = player.Inventory;
        inv.Set(Resource.ResourceType.Nourriture, nourriture);
        inv.Set(Resource.ResourceType.Linemate, linemate);
        inv.Set(Resource.ResourceType.Deraumere, deraumere);
        inv.Set(Resource.ResourceType.Sibur, sibur);
        inv.Set(Resource.ResourceType.Mendiane, mendiane);
        inv.Set(Resource.ResourceType.Phiras, phiras);
        inv.Set(Resource.ResourceType.Thystame, thystame);
        return true;
    }

    private void plv(string[] parts)
    {
        if (!RequireLength(parts, 3, "plv"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "plv", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "plv", "L", out int level))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[plv] Player #{id} no existe todavia.");
            return;
        }

        player.SetLevel(level);
        _teamPanel?.SetLevel(id, level);

        Log.Debug($"[plv] Player #{id} -> level {level}");
    }

    private void ppo(string[] parts)
    {
        if (!RequireLength(parts, 5, "ppo"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "ppo", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "ppo", "X", out int x))
            return;
        if (!TryParseField(parts[3], "ppo", "Y", out int y))
            return;
        if (!TryParseField(parts[4], "ppo", "O", out int o))
            return;

        if (!playerManager.TryGet(id, out Player player))
        {
            Log.Error($"[ppo] Player #{id} no existe todavia.");
            return;
        }

        player.SetTilePos(x, y);
        player.SetOrientation(o);
        _teamPanel?.SetLastAction(id, $"→ ({x},{y})");
    }

    private void pnw(string[] parts)
    {
        if (!RequireLength(parts, 7, "pnw"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pnw", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pnw", "X", out int x))
            return;
        if (!TryParseField(parts[3], "pnw", "Y", out int y))
            return;
        if (!TryParseField(parts[4], "pnw", "O", out int o))
            return;
        if (!TryParseField(parts[5], "pnw", "L", out int level))
            return;
        string team = parts[6];

        Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, 0f);
        Player player = playerManager.GetOrCreate(id, worldPos, team);
        PlayerSpawnState spawnState = new PlayerSpawnState(worldPos, x, y, o, level);
        ApplySpawnState(player, spawnState);

        _teamPanel?.AddPlayer(id, team, level);
        Log.Debug($"[pnw] Player #{id} team={team} pos=({x},{y}) o={o} lvl={level}");
    }

    /// <summary>
    /// Aplica el estado de spawn/reconexión a un jugador nuevo o ya existente.
    /// Alinea el tile lógico con la posición de spawn explícitamente: sin esto
    /// TilePos queda en (0,0) por defecto y CrowdSystem arrastra a todos los
    /// jugadores al tile (0,0) hasta su primer ppo/pin.
    /// </summary>
    private void ApplySpawnState(Player player, PlayerSpawnState state)
    {
        player.Position = state.WorldPos;
        player.SetTerrain(terrainManager);
        player.SetTilePos(state.X, state.Y);
        player.SetOrientation(state.Orientation);
        player.SetLevel(state.Level);
        player.SetSpeedFactor(_currentSpeedFactor);
    }
}
