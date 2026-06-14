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
            GD.PrintErr($"[edi] Egg #{eggId} no existe.");
            return;
        }

        eggManager.Remove(eggId);

        GD.Print($"[edi] Egg #{eggId} murio de hambre.");
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
            GD.Print($"[ebo] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        eggManager.Remove(eggId);

        GD.Print($"[ebo] Egg #{eggId} consumido: un jugador se conectó.");
    }

    private void eht(string[] parts)
    {
        // eht #e — el huevo eclosiona: señal visual, NO se elimina (eso lo hace ebo).
        if (parts.Length < 2)
            return;

        int eggId = int.Parse(parts[1].TrimStart('#'));

        if (!eggManager.TryGet(eggId, out var egg))
        {
            GD.Print($"[eht] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        egg.Hatch();

        GD.Print($"[eht] Egg #{eggId} ha eclosionado.");
    }

    private void enw(string[] parts)
    {
        // enw #e #n X Y
        int eggId = int.Parse(parts[1].TrimStart('#'));
        int playerId = int.Parse(parts[2].TrimStart('#'));
        int x = int.Parse(parts[3]);
        int y = int.Parse(parts[4]);

        Vector3 worldPos = TerrainSnap.TileCenter(terrainManager, x, y, 0.15f);

        var egg = eggManager.CreateEgg(eggId, worldPos);

        GD.Print($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
    }
}
