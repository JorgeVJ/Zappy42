using Godot;
using System.Collections.Generic;

/// <summary>
/// Posicionamiento dinámico de jugadores con steering tipo "boids" (Reynolds):
/// cada jugador se dirige al centro de su tile (arrival) y se separa de los
/// vecinos cercanos (separation), de modo que varios jugadores comparten tile
/// agrupándose de forma orgánica pero sin solaparse. La velocidad máxima escala
/// con el time unit del servidor (Player.SpeedFactor).
/// </summary>
public partial class CrowdSystem : Node
{
    /// <summary>u/seg a factor 1.</summary>
    [Export]
    public float BaseSpeed = 2.5f;

    /// <summary>Frenado al acercarse al centro del tile.</summary>
    [Export]
    public float ArrivalRadius = 0.6f;

    /// <summary>Separación deseada entre jugadores.</summary>
    [Export]
    public float SeparationDist = 0.7f;

    [Export]
    public float SeparationWeight = 1.6f;

    [Export]
    public float ArrivalWeight = 1.0f;

    /// <summary>Suavizado de la velocidad.</summary>
    [Export]
    public float Damping = 8.0f;

    private PlayerManager _players;
    private Terrain _terrain;

    public void Setup(PlayerManager players, Terrain terrain)
    {
        _players = players;
        _terrain = terrain;
    }

    public override void _Process(double delta)
    {
        if (_players == null || _terrain == null)
            return;

        float dt = (float)delta;
        if (dt <= 0f)
            return;

        List<Player> list = new List<Player>(_players.All);
        if (list.Count == 0)
            return;

        float sepDist = Mathf.Min(SeparationDist, Terrain.TILE_SIZE * 0.9f);

        foreach (Player p in list)
            StepPlayer(p, list, sepDist, dt);
    }

    /// <remarks>
    /// El alcance máximo de afección (sepDist) se mantiene por debajo de la amplitud de
    /// la celda (Terrain.TILE_SIZE) para no empujar entre celdas vecinas.
    /// </remarks>
    private void StepPlayer(Player p, List<Player> list, float sepDist, float dt)
    {
        Vector3 pos = p.GlobalPosition;

        Vector3 arrive = ComputeArrival(p, pos);
        Vector3 sep = ComputeSeparation(p, pos, list, sepDist);

        float maxSpeed = BaseSpeed * Mathf.Max(0.1f, p.SpeedFactor);

        Vector3 desiredVel = (arrive * ArrivalWeight + sep * SeparationWeight) * maxSpeed;
        if (desiredVel.Length() > maxSpeed)
            desiredVel = desiredVel.Normalized() * maxSpeed;

        Vector3 vel = p.Velocity.Lerp(desiredVel, Mathf.Clamp(Damping * dt, 0f, 1f));
        Vector3 newPos = pos + vel * dt;

        int tx = Mathf.FloorToInt(newPos.X / Terrain.TILE_SIZE);
        int ty = Mathf.FloorToInt(newPos.Z / Terrain.TILE_SIZE);
        newPos.Y = _terrain.GetTileHeight(tx, ty);

        p.Velocity = vel;
        p.GlobalPosition = newPos;

        float horizSpeed = new Vector2(vel.X, vel.Z).Length();
        p.UpdateLocomotion(horizSpeed);
    }

    /// <summary>Arrival: hacia el centro de su tile (solo XZ), frenando al acercarse.</summary>
    private Vector3 ComputeArrival(Player p, Vector3 pos)
    {
        Vector3 target = TerrainSnap.TileCenter(_terrain, p.TilePos.X, p.TilePos.Y, 0f);
        Vector3 toTarget = new Vector3(target.X - pos.X, 0f, target.Z - pos.Z);
        float distT = toTarget.Length();
        if (distT <= 0.001f)
            return Vector3.Zero;

        float slow = distT < ArrivalRadius ? distT / ArrivalRadius : 1f;
        return (toTarget / distT) * slow;
    }

    /// <remarks>
    /// Separation: empuje desde los vecinos cercanos (solo XZ). El alcance de afección
    /// no cruza a celdas vecinas: solo separa de los jugadores que comparten el mismo
    /// tile lógico. En caso de solape exacto usa una dirección determinista por Id
    /// (golden angle) para romper la simetría.
    /// </remarks>
    private Vector3 ComputeSeparation(Player p, Vector3 pos, List<Player> list, float sepDist)
    {
        Vector3 sep = Vector3.Zero;

        foreach (Player q in list)
        {
            if (q == p)
                continue;
            if (q.TilePos != p.TilePos)
                continue;

            Vector3 oq = q.GlobalPosition;
            Vector3 d = new Vector3(pos.X - oq.X, 0f, pos.Z - oq.Z);
            float dd = d.Length();
            if (dd >= sepDist)
                continue;

            if (dd > 0.0001f)
                sep += (d / dd) * ((sepDist - dd) / sepDist);
            else
            {
                float ang = p.Id * 2.39996323f;
                sep += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 0.5f;
            }
        }

        return sep;
    }
}
