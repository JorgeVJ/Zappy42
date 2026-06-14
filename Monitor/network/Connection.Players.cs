using Godot;
using System.Collections.Generic;

// Handlers de jugadores: spawn/movimiento/nivel/inventario (pnw/ppo/plv/pin),
// expulsión y muerte (pex/pdi), recursos (pgt/pdr), huevos/incantaciones
// (pfk/pic/pie) y broadcasts (pbc), más los efectos visuales asociados
// (mensajes flotantes, ondas de sonido, resaltado de incantación).
public partial class Connection
{
    // Jugadores incantando por tile (pic), para terminar su animación al recibir pie.
    private readonly Dictionary<(int, int), List<int>> _incantations = new();

    private void RegisterPlayerHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("pnw", pnw); // pnw #n X Y O L N\n - New player connection
        dispatcher.Register("ppo", ppo); // ppo #n X Y O\n ppo #n\n Player position
        dispatcher.Register("plv", plv); // plv #n L\n plv #n\n Player level
        dispatcher.Register("pin", pin); // pin #n X Y q q q q q q q\n pin #n\n Player inventory
        dispatcher.Register("pex", pex); // pex #n\n - A player is expelled
        dispatcher.Register("pbc", pbc); // pbc #n M\n - A player broadcasts
        dispatcher.Register("pic", pic); // pic X Y L #n #n ...\n - Incantation started on the tile by a player
        dispatcher.Register("pie", pie); // pie X Y R\n - End of incantation with result R(0 or 1)
        dispatcher.Register("pfk", pfk); // pfk #n\n - The player lays an egg
        dispatcher.Register("pdr", pdr); // pdr #n i\n - The player drops a resource
        dispatcher.Register("pgt", pgt); // pgt #n i\n - The player takes a resource
        dispatcher.Register("pdi", pdi); // pdi #n\n - The player dies of hunger
    }

    private void pdi(string[] parts)
    {
        // pdi #n
        if (!RequireLength(parts, 2, "pdi"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pdi", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out var player))
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
        // pgt #n i
        if (!RequireLength(parts, 3, "pgt"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pgt", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pgt", "i", out int itemId))
            return;

        var type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out var player))
        {
            Log.Error($"[pgt] Player #{id} no existe todavia.");
            return;
        }

        // +1 al inventario local (si lo estás usando)
        player.Inventory.Add(type, 1);
        var tilePos = player.TilePos;
        terrainManager[tilePos.X, tilePos.Y]?.Inventory.Remove(type, 1);
        _teamPanel?.SetLastAction(id, $"+ {type}");
        player.PlayCollect();
        Log.Debug($"[pgt] Player #{id} tomo {type}");
    }

    private void pdr(string[] parts)
    {
        // pdr #n i
        if (!RequireLength(parts, 3, "pdr"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pdr", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pdr", "i", out int itemId))
            return;

        var type = (Resource.ResourceType)itemId;

        if (!playerManager.TryGet(id, out var player))
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
        // pfk #n
        if (!RequireLength(parts, 2, "pfk"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pfk", "#n", out int playerId))
            return;

        if (!playerManager.TryGet(playerId, out var player))
        {
            Log.Error($"[pfk] Player #{playerId} no encontrado");
            return;
        }

        Log.Debug($"[pfk] Player #{playerId} esta poniendo un huevo");
        _teamPanel?.SetLastAction(playerId, "🥚 pone huevo");
    }

    private void pie(string[] parts)
    {
        // pie X Y R
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

        // Terminar la animación de hechizo de los jugadores que estaban incantando aquí.
        if (_incantations.TryGetValue((x, y), out var playerIds))
        {
            foreach (int pid in playerIds)
            {
                if (playerManager.TryGet(pid, out var player))
                    player.StopSpell();
            }
            _incantations.Remove((x, y));
        }

        // Quitar el resaltado del tile y mostrar un pulso de color según el resultado
        // (verde = éxito, rojo = fallo) reutilizando el efecto SoundWave. El pulso es
        // un efecto transitorio sin estado persistente: se omite durante el replay
        // instantáneo de la barra de tiempo.
        terrainManager?.DeselectTile();
        if (!ReplayInstant)
            ShowIncantationResult(x, y, success);
    }

    private void pic(string[] parts)
    {
        // pic X Y L #n #n ...
        if (!RequireLength(parts, 4, "pic"))
            return;

        if (!TryParseField(parts[1], "pic", "X", out int x))
            return;
        if (!TryParseField(parts[2], "pic", "Y", out int y))
            return;
        if (!TryParseField(parts[3], "pic", "L", out int level))
            return;

        // jugadores involucrados
        List<int> playerIds = new List<int>();
        for (int i = 4; i < parts.Length; i++)
        {
            if (!TryParseField(parts[i].TrimStart('#'), "pic", $"#n[{i - 4}]", out int pid))
                return;

            playerIds.Add(pid);
        }

        Log.Debug($"[pic] Incantacion en tile ({x},{y}) nivel {level} con jugadores: {string.Join(",", playerIds)}");

        _incantations[(x, y)] = playerIds;

        foreach (int pid in playerIds)
        {
            _teamPanel?.SetLastAction(pid, $"✨ incant. Nv.{level}");
            if (playerManager.TryGet(pid, out var player))
                player.PlaySpell();
        }

        // Resaltar el tile de la incantación (reutiliza el shader de selección del terreno).
        if (terrainManager != null && terrainManager[x, y] != null)
            terrainManager.SelectTile(x, y);
    }

    // Pulso de color sobre el tile al terminar una incantación (verde éxito / rojo fallo).
    private void ShowIncantationResult(int x, int y, bool success)
    {
        if (terrainManager == null || terrainManager[x, y] == null)
            return;

        Color color = success ? new Color(0.3f, 1f, 0.4f, 0.85f) : new Color(1f, 0.3f, 0.3f, 0.85f);
        Vector3 center = TerrainSnap.TileCenter(terrainManager, x, y, Terrain.EntityGroundOffset);
        var wave = SoundWave.Create(center, color);
        terrainManager.AddChild(wave);
    }

    private async void ShowPlayerMessage(Player player, string msg)
    {
        var label = new Label3D();
        label.Text = msg;
        label.Position = new Vector3(0, 1.5f, 0); // encima del jugador
        player.AddChild(label);

        // desaparecer después de 2 segundos (salvo que ResetWorldState ya haya
        // liberado al jugador y, con él, esta etiqueta - p.ej. al saltar en la
        // barra de tiempo mientras el mensaje seguía visible).
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        if (IsInstanceValid(label))
            label.QueueFree();
    }

    private void pbc(string[] parts)
    {
        // pbc #n M
        if (!RequireLength(parts, 3, "pbc"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pbc", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out var player))
        {
            Log.Error($"[pbc] Player #{id} no existe.");
            return;
        }

        // reconstruimos el mensaje (puede contener espacios)
        string message = string.Join(" ", parts, 2, parts.Length - 2);

        Log.Debug($"[pbc] Player #{id} dice: {message}");
        _teamPanel?.SetLastAction(id, $"📢 {message}");

        // Globo de texto y onda de sonido son efectos transitorios sin estado
        // persistente: se omiten durante el replay instantáneo de la barra de tiempo.
        if (!ReplayInstant)
        {
            ShowPlayerMessage(player, message);
            ShowSoundWave(player);
        }
    }

    // Expanding ground ring centered on the emitter's tile, visualizing the
    // broadcast as sound propagating outward. Complements the floating text.
    private void ShowSoundWave(Player player)
    {
        if (terrainManager == null)
            return;

        Vector3 center = TerrainSnap.TileCenter(terrainManager, player.TilePos.X, player.TilePos.Y, Terrain.EntityGroundOffset);
        var wave = SoundWave.Create(center);
        terrainManager.AddChild(wave);
    }

    private void pex(string[] parts)
    {
        // pex #n
        if (!RequireLength(parts, 2, "pex"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pex", "#n", out int id))
            return;

        if (!playerManager.TryGet(id, out var player))
        {
            Log.Error($"[pex] Player #{id} no existe.");
            return;
        }

        Log.Debug($"[pex] Player #{id} expulso a otros jugadores.");
        _teamPanel?.SetLastAction(id, "💨 expulsó");
    }

    private void pin(string[] parts)
    {
        // pin #n X Y q0 q1 q2 q3 q4 q5 q6
        if (!RequireLength(parts, 11, "pin"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "pin", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "pin", "X", out int x))
            return;
        if (!TryParseField(parts[3], "pin", "Y", out int y))
            return;

        if (!playerManager.TryGet(id, out var player))
        {
            Log.Error($"[pin] Player #{id} no existe todavía.");
            return;
        }

        // Posición (pin SIEMPRE incluye posición)
        player.SetTilePos(x, y);

        // Inventario
        var inv = player.Inventory;

        if (!TryParseField(parts[4], "pin", "q0 (Nourriture)", out int nourriture)) return;
        if (!TryParseField(parts[5], "pin", "q1 (Linemate)", out int linemate)) return;
        if (!TryParseField(parts[6], "pin", "q2 (Deraumere)", out int deraumere)) return;
        if (!TryParseField(parts[7], "pin", "q3 (Sibur)", out int sibur)) return;
        if (!TryParseField(parts[8], "pin", "q4 (Mendiane)", out int mendiane)) return;
        if (!TryParseField(parts[9], "pin", "q5 (Phiras)", out int phiras)) return;
        if (!TryParseField(parts[10], "pin", "q6 (Thystame)", out int thystame)) return;

        inv.Set(Resource.ResourceType.Nourriture, nourriture);
        inv.Set(Resource.ResourceType.Linemate, linemate);
        inv.Set(Resource.ResourceType.Deraumere, deraumere);
        inv.Set(Resource.ResourceType.Sibur, sibur);
        inv.Set(Resource.ResourceType.Mendiane, mendiane);
        inv.Set(Resource.ResourceType.Phiras, phiras);
        inv.Set(Resource.ResourceType.Thystame, thystame);

        Log.Debug($"[pin] Player #{id} inventario actualizado");
    }

    private void plv(string[] parts)
    {
        // plv #n L
        if (!RequireLength(parts, 3, "plv"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "plv", "#n", out int id))
            return;
        if (!TryParseField(parts[2], "plv", "L", out int level))
            return;

        if (!playerManager.TryGet(id, out var player))
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
        // ppo #n X Y O
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

        if (!playerManager.TryGet(id, out var player))
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
        // pnw #n X Y O L N
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

        // convertir coords tile -> mundo (centro del tile)
        Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, 0f);

        var player = playerManager.GetOrCreate(id, worldPos, team);

        // si ya existía, lo actualizamos también
        player.Position = worldPos;
        player.SetTerrain(terrainManager);
        // Alinear el tile lógico con la posición de spawn: sin esto TilePos queda
        // en (0,0) por defecto y CrowdSystem arrastra a todos los jugadores al
        // tile (0,0) hasta su primer ppo/pin.
        player.SetTilePos(x, y);
        player.SetOrientation(o);
        player.SetLevel(level);
        player.SetSpeedFactor(_currentSpeedFactor);

        _teamPanel?.AddPlayer(id, team, level);
        Log.Debug($"[pnw] Player #{id} team={team} pos=({x},{y}) o={o} lvl={level}");
    }
}
