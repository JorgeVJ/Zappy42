using Godot;
using System.Collections.Generic;

// Helper genérico y reutilizable para encontrar una posición libre dentro de una
// región candidata (p. ej. el interior de un tile) evitando colisionar con una
// lista de obstáculos circulares (posición world XZ + radio).
//
// Desacoplado de Terrain/DecorationSystem: solo depende de tipos básicos de Godot
// (Vector2/Vector3/RandomNumberGenerator), por lo que es testeable de forma aislada.
public static class PlacementFinder
{
    // Un obstáculo circular en el plano XZ: posición world y radio de exclusión.
    public readonly record struct Obstacle(Vector2 PositionXZ, float Radius);

    // Busca un offset (relativo a `center`) dentro de [-range, range] en X y Z que,
    // sumado a `center`, no caiga dentro del radio de ningún obstáculo (más el radio
    // propio del objeto a colocar, `placedRadius`).
    //
    // - `center`: centro de la región candidata (en coordenadas world XZ).
    // - `range`: medio-ancho de la región candidata alrededor de `center` (cuadrado [-range, range]^2).
    // - `obstacles`: lista de obstáculos a evitar (posición world XZ + radio).
    // - `placedRadius`: radio del objeto que se va a colocar (se suma al radio del obstáculo
    //   para la comprobación de colisión). Por defecto 0.
    // - `rng`: generador ya sembrado por el llamador (mantiene el determinismo por (x,y,tipo)).
    // - `maxAttempts`: número de intentos antes de hacer fallback.
    //
    // Devuelve un offset relativo a `center`. Si no se encuentra hueco libre tras
    // `maxAttempts` intentos, hace fallback a `fallbackOffset` si se especifica, o si no
    // al último offset candidato generado (comportamiento equivalente al de antes de
    // introducir la comprobación de colisiones: la posición pseudoaleatoria original).
    public static Vector2 FindFreeOffset(
        Vector2 center,
        float range,
        IReadOnlyList<Obstacle> obstacles,
        float placedRadius,
        RandomNumberGenerator rng,
        int maxAttempts = 10,
        Vector2? fallbackOffset = null)
    {
        var lastCandidate = Vector2.Zero;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            lastCandidate = new Vector2(
                rng.RandfRange(-range, range),
                rng.RandfRange(-range, range)
            );

            if (!Collides(center + lastCandidate, placedRadius, obstacles))
                return lastCandidate;
        }

        return fallbackOffset ?? lastCandidate;
    }

    // Variante 3D de conveniencia: opera sobre Vector3 ignorando la coordenada Y
    // (altura) tanto en `center` como en los obstáculos. Útil cuando se trabaja
    // directamente con posiciones world en 3D.
    public static Vector3 FindFreePosition(
        Vector3 center,
        float range,
        IReadOnlyList<Obstacle> obstacles,
        float placedRadius,
        RandomNumberGenerator rng,
        int maxAttempts = 10,
        Vector3? fallbackPosition = null)
    {
        var centerXZ = new Vector2(center.X, center.Z);
        var fallbackOffset = fallbackPosition.HasValue
            ? new Vector2(fallbackPosition.Value.X, fallbackPosition.Value.Z) - centerXZ
            : (Vector2?)null;

        var offset = FindFreeOffset(centerXZ, range, obstacles, placedRadius, rng, maxAttempts, fallbackOffset);

        return new Vector3(center.X + offset.X, center.Y, center.Z + offset.Y);
    }

    // True si `positionXZ` (con radio `radius`) colisiona con algún obstáculo de la lista,
    // es decir, si la distancia entre centros es menor que la suma de radios.
    private static bool Collides(Vector2 positionXZ, float radius, IReadOnlyList<Obstacle> obstacles)
    {
        if (obstacles == null)
            return false;

        for (int i = 0; i < obstacles.Count; i++)
        {
            var obstacle = obstacles[i];
            float minDist = radius + obstacle.Radius;
            if (positionXZ.DistanceSquaredTo(obstacle.PositionXZ) < minDist * minDist)
                return true;
        }

        return false;
    }
}
