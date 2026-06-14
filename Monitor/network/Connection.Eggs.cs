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
        int eggId = int.Parse(parts[1].TrimStart('#'));

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
        if (parts.Length < 2)
            return;

        int eggId = int.Parse(parts[1].TrimStart('#'));

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
        if (parts.Length < 2)
            return;

        int eggId = int.Parse(parts[1].TrimStart('#'));

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
        int eggId = int.Parse(parts[1].TrimStart('#'));
        int playerId = int.Parse(parts[2].TrimStart('#'));
        int x = int.Parse(parts[3]);
        int y = int.Parse(parts[4]);

        Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, Terrain.EntityGroundOffset);

        var egg = eggManager.CreateEgg(eggId, worldPos);

        Log.Debug($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
    }
}
