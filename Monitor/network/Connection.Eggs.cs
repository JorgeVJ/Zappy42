using Godot;

/// <summary>
/// Handlers de huevos: puesta (enw), eclosión (eht), consumo al conectar un
/// jugador (ebo) y muerte por hambre (edi).
/// </summary>
public partial class Connection
{
    private void RegisterEggHandlers(MessageDispatcher dispatcher)
    {
        dispatcher.Register("enw", enw);
        dispatcher.Register("eht", eht);
        dispatcher.Register("ebo", ebo);
        dispatcher.Register("edi", edi);
    }

    private void edi(string[] parts)
    {
        if (!RequireLength(parts, 2, "edi"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "edi", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out Egg egg))
        {
            Log.Error($"[edi] Egg #{eggId} no existe.");
            return;
        }

        eggManager.Remove(eggId);

        Log.Debug($"[edi] Egg #{eggId} murio de hambre.");
    }

    /// <summary>
    /// Un jugador se conecta desde el huevo: aquí SÍ se elimina.
    /// </summary>
    private void ebo(string[] parts)
    {
        if (!RequireLength(parts, 2, "ebo"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "ebo", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out Egg egg))
        {
            Log.Debug($"[ebo] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        eggManager.Remove(eggId);

        Log.Debug($"[ebo] Egg #{eggId} consumido: un jugador se conectó.");
    }

    /// <summary>
    /// El huevo eclosiona: señal visual, no se elimina (eso lo hace ebo).
    /// </summary>
    private void eht(string[] parts)
    {
        if (!RequireLength(parts, 2, "eht"))
            return;

        if (!TryParseField(parts[1].TrimStart('#'), "eht", "#e", out int eggId))
            return;

        if (!eggManager.TryGet(eggId, out Egg egg))
        {
            Log.Debug($"[eht] Egg #{eggId} ya no existe (ignorado).");
            return;
        }

        egg.Hatch();

        Log.Debug($"[eht] Egg #{eggId} ha eclosionado.");
    }

    private void enw(string[] parts)
    {
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

        Egg egg = eggManager.CreateEgg(eggId, worldPos);

        Log.Debug($"[enw] Egg #{eggId} puesto por Player #{playerId} en ({x},{y})");
    }
}
