using Godot;
using System.Collections.Generic;

public partial class PlayerManager : EntityManager<Player>
{
    protected override string ContainerName => "Players";

    [Signal]
    public delegate void PlayerCreatedEventHandler(Player player);

    public Player GetOrCreate(int id, Vector3 pos, string teamName)
    {
        if (entities.TryGetValue(id, out Player existing))
            return existing;

        Player p = Player.Create(pos);
        p.Init(id, teamName);
        EmitSignal(nameof(PlayerCreated), p);

        return Register(id, p);
    }

    /// <summary>
    /// Vista de solo lectura de todos los jugadores activos (para propagar velocidad,
    /// posicionamiento, etc.).
    /// </summary>
    public IReadOnlyCollection<Player> All => entities.Values;
}
