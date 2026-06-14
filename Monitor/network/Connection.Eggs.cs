using Godot;

// Handlers de huevos: puesta (enw), eclosión (eht), consumo al conectar un
// jugador (ebo) y muerte por hambre (edi).
public partial class Connection
{
    private void RegisterEggHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("enw", enw); // enw #e #n X Y\n - The egg is laid on the tile by a player
        dispatcher.Register("eht", eht); // eht #e\n - The egg hatches
        dispatcher.Register("ebo", ebo); // ebo #e\n - A player connects for an egg
        dispatcher.Register("edi", edi); // edi #e\n - The hatched egg dies of hunger
    }

    private void edi(string[] parts)
    {
        // edi #e
        if (!RequireLength(parts, 2, "edi"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "edi", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out var egg))
        {
            Log.Error($"[edi] Egg #{eggId} no existe.");
            return;
        }

        eggManager.Remove(eggId);

        Log.Debug($"[edi] Egg #{eggId} murio de hambre.");
    }

    private void ebo(string[] parts)
    {
        // ebo #e — un jugador se conecta desde el huevo: aquí SÍ se elimina.
        if (!RequireLength(parts, 2, "ebo"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "ebo", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out var egg))
        {
            // Tolerante: el huevo puede haberse eliminado ya (p. ej. por edi); no es un error.
            Log.Debug($"[ebo] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        eggManager.Remove(eggId);

        Log.Debug($"[ebo] Egg #{eggId} consumido: un jugador se conectó.");
    }

    private void eht(string[] parts)
    {
        // eht #e — el huevo eclosiona: señal visual, NO se elimina (eso lo hace ebo).
        if (!RequireLength(parts, 2, "eht"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "eht", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out var egg))
        {
            Log.Debug($"[eht] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        egg.Hatch();

        Log.Debug($"[eht] Egg #{eggId} ha eclosionado.");
    }

    private void enw(string[] parts)
    {
        // enw #e #n X Y
        if (!RequireLength(parts, 5, "enw"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "enw", "#e", out int eggId))
            return;
        if (!TryParseField(parts[2].TrimStart('#'), "enw", "#n", out int playerId))
            return;
        if (!TryParseField(parts[3], "enw", "X", out int x))
            return;
        if (!TryParseField(parts[4], "enw", "Y", out int y))
            return;

        Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, Terrain.EntityGroundOffset);

        var egg = eggManager.CreateEgg(eggId, worldPos);

        Log.Debug($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
    }
}
