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
        int id = int.Parse(parts[1].TrimStart('#'));

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
        int id = int.Parse(parts[1].TrimStart('#'));
        int itemId = int.Parse(parts[2]);

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
        int id = int.Parse(parts[1].TrimStart('#'));
        int itemId = int.Parse(parts[2]);

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
        int playerId = int.Parse(parts[1].TrimStart('#'));

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
        if (parts.Length < 4)
            return;

        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        int result = int.Parse(parts[3]);
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
        if (parts.Length < 4)
            return;

        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        int level = int.Parse(parts[3]);

        // jugadores involucrados
        List<int> playerIds = new List<int>();
        for (int i = 4; i < parts.Length; i++)
        {
            playerIds.Add(int.Parse(parts[i].TrimStart('#')));
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
        int id = int.Parse(parts[1].TrimStart('#'));

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
        int id = int.Parse(parts[1].TrimStart('#'));

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
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);

        if (!playerManager.TryGet(id, out var player))
        {
            Log.Error($"[pin] Player #{id} no existe todavía.");
            return;
        }

        // Posición (pin SIEMPRE incluye posición)
        player.SetTilePos(x, y);

        // Inventario
        var inv = player.Inventory;

        inv.Set(Resource.ResourceType.Nourriture, int.Parse(parts[4]));
        inv.Set(Resource.ResourceType.Linemate, int.Parse(parts[5]));
        inv.Set(Resource.ResourceType.Deraumere, int.Parse(parts[6]));
        inv.Set(Resource.ResourceType.Sibur, int.Parse(parts[7]));
        inv.Set(Resource.ResourceType.Mendiane, int.Parse(parts[8]));
        inv.Set(Resource.ResourceType.Phiras, int.Parse(parts[9]));
        inv.Set(Resource.ResourceType.Thystame, int.Parse(parts[10]));

        Log.Debug($"[pin] Player #{id} inventario actualizado");
    }

    private void plv(string[] parts)
    {
        // plv #n L
        int id = int.Parse(parts[1].TrimStart('#'));
        int level = int.Parse(parts[2]);

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
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);
        int o = int.Parse(parts[4]);

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
        int id = int.Parse(parts[1].TrimStart('#'));
        int x = int.Parse(parts[2]);
        int y = int.Parse(parts[3]);
        int o = int.Parse(parts[4]);
        int level = int.Parse(parts[5]);
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
