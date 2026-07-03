using Godot;
using System.Collections.Generic;

/// <summary>
/// Helper genérico y reutilizable para encontrar una posición libre dentro de una
/// región candidata (p. ej. el interior de un tile) evitando colisionar con una
/// lista de obstáculos circulares (posición world XZ + radio).
/// </summary>
/// <remarks>
/// Desacoplado de Terrain/DecorationSystem: solo depende de tipos básicos de Godot
/// (Vector2/Vector3/RandomNumberGenerator), por lo que es testeable de forma aislada.
/// </remarks>
public static partial class PlacementFinder
{
    /// <summary>
    /// Busca un offset (relativo a <paramref name="center"/>) dentro de [-range, range] en
    /// X y Z que, sumado a <paramref name="center"/>, no caiga dentro del radio de ningún
    /// obstáculo (más el radio propio del objeto a colocar).
    /// </summary>
    /// <param name="center">Centro de la región candidata (en coordenadas world XZ).</param>
    /// <param name="query">Parámetros de búsqueda (rango, obstáculos, radio, rng, intentos).</param>
    /// <param name="fallbackOffset">
    /// Offset de fallback si no se encuentra hueco libre tras los intentos configurados en
    /// <paramref name="query"/>. Si no se especifica, se usa el último offset candidato generado.
    /// </param>
    /// <returns>Un offset relativo a <paramref name="center"/>.</returns>
    public static Vector2 FindFreeOffset(Vector2 center, PlacementQuery query, Vector2? fallbackOffset = null)
    {
        Vector2 lastCandidate = Vector2.Zero;

        for (int attempt = 0; attempt < query.MaxAttempts; attempt++)
        {
            lastCandidate = new Vector2(
                query.Rng.RandfRange(-query.Range, query.Range),
                query.Rng.RandfRange(-query.Range, query.Range)
            );

            if (!Collides(center + lastCandidate, query.PlacedRadius, query.Obstacles))
                return lastCandidate;
        }

        return fallbackOffset ?? lastCandidate;
    }

    /// <summary>
    /// Variante 3D de conveniencia: opera sobre Vector3 ignorando la coordenada Y (altura)
    /// tanto en <paramref name="center"/> como en los obstáculos. Útil cuando se trabaja
    /// directamente con posiciones world en 3D.
    /// </summary>
    public static Vector3 FindFreePosition(Vector3 center, PlacementQuery query, Vector3? fallbackPosition = null)
    {
        Vector2 centerXZ = new Vector2(center.X, center.Z);
        Vector2? fallbackOffset = fallbackPosition.HasValue
            ? new Vector2(fallbackPosition.Value.X, fallbackPosition.Value.Z) - centerXZ
            : (Vector2?)null;

        Vector2 offset = FindFreeOffset(centerXZ, query, fallbackOffset);

        return new Vector3(center.X + offset.X, center.Y, center.Z + offset.Y);
    }

    /// <summary>
    /// True si <paramref name="positionXZ"/> (con el radio dado) colisiona con algún
    /// obstáculo de la lista, es decir, si la distancia entre centros es menor que la
    /// suma de radios.
    /// </summary>
    private static bool Collides(Vector2 positionXZ, float radius, IReadOnlyList<Obstacle> obstacles)
    {
        if (obstacles == null)
            return false;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle obstacle = obstacles[i];
            float minDist = radius + obstacle.Radius;
            if (positionXZ.DistanceSquaredTo(obstacle.PositionXZ) < minDist * minDist)
                return true;
        }

        return false;
    }
}
