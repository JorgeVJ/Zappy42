using Godot;
using System.Collections.Generic;

// Posicionamiento dinámico de jugadores con steering tipo "boids" (Reynolds):
// cada jugador se dirige al centro de su tile (arrival) y se separa de los
// vecinos cercanos (separation), de modo que varios jugadores comparten tile
// agrupándose de forma orgánica pero sin solaparse. La velocidad máxima escala
// con el time unit del servidor (Player.SpeedFactor), reutilizando D1.
public partial class CrowdSystem : Node
{
    [Export] public float BaseSpeed       = 2.5f; // u/seg a factor 1
    [Export] public float ArrivalRadius   = 0.6f; // frenado al acercarse al centro del tile
    [Export] public float SeparationDist  = 0.7f; // separación deseada entre jugadores
    [Export] public float SeparationWeight = 1.6f;
    [Export] public float ArrivalWeight   = 1.0f;
    [Export] public float Damping         = 8.0f; // suavizado de la velocidad

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

        var list = new List<Player>(_players.All);
        if (list.Count == 0)
            return;

        // El alcance máximo de afección se mantiene por debajo de la amplitud de la
        // celda (Terrain.TILE_SIZE) para no empujar entre celdas vecinas.
        float sepDist = Mathf.Min(SeparationDist, Terrain.TILE_SIZE * 0.9f);

        foreach (var p in list)
        {
            Vector3 pos = p.GlobalPosition;

            // Arrival: hacia el centro de su tile (solo XZ), frenando al acercarse.
            Vector3 target = TerrainSnap.TileCenter(_terrain, p.TilePos.X, p.TilePos.Y, 0f);
            Vector3 toTarget = new Vector3(target.X - pos.X, 0f, target.Z - pos.Z);
            float distT = toTarget.Length();
            Vector3 arrive = Vector3.Zero;
            if (distT > 0.001f)
            {
                float slow = distT < ArrivalRadius ? distT / ArrivalRadius : 1f;
                arrive = (toTarget / distT) * slow;
            }

            // Separation: empuje desde los vecinos cercanos (solo XZ).
            Vector3 sep = Vector3.Zero;
            foreach (var q in list)
            {
                if (q == p)
                    continue;

                // El alcance de afección no cruza a celdas vecinas: solo separamos de
                // los jugadores que comparten el mismo tile lógico.
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
                    // Solape exacto: dirección determinista por Id (golden angle) para romper la simetría.
                    float ang = p.Id * 2.39996323f;
                    sep += new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 0.5f;
                }
            }

            float maxSpeed = BaseSpeed * Mathf.Max(0.1f, p.SpeedFactor);

            Vector3 desiredVel = (arrive * ArrivalWeight + sep * SeparationWeight) * maxSpeed;
            if (desiredVel.Length() > maxSpeed)
                desiredVel = desiredVel.Normalized() * maxSpeed;

            Vector3 vel = p.Velocity.Lerp(desiredVel, Mathf.Clamp(Damping * dt, 0f, 1f));
            Vector3 newPos = pos + vel * dt;

            // Altura: seguir el terreno bajo la posición real.
            int tx = Mathf.FloorToInt(newPos.X / Terrain.TILE_SIZE);
            int ty = Mathf.FloorToInt(newPos.Z / Terrain.TILE_SIZE);
            newPos.Y = _terrain.GetTileHeight(tx, ty);

            p.Velocity = vel;
            p.GlobalPosition = newPos;

            float horizSpeed = new Vector2(vel.X, vel.Z).Length();
            p.UpdateLocomotion(horizSpeed);
        }
    }
}
