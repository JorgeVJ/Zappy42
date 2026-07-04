using Godot;
using System.Collections.Generic;

public static partial class PlacementFinder
{
    /// <summary>
    /// Agrupa los parámetros de búsqueda compartidos por <see cref="FindFreeOffset"/> y
    /// <see cref="FindFreePosition"/>, de forma que ambos métodos queden dentro del
    /// límite de 4 parámetros.
    /// </summary>
    /// <param name="Range">Medio-ancho de la región candidata alrededor del centro (cuadrado [-range, range]^2).</param>
    /// <param name="Obstacles">Lista de obstáculos a evitar (posición world XZ + radio).</param>
    /// <param name="PlacedRadius">Radio del objeto que se va a colocar (se suma al radio del obstáculo para la comprobación de colisión).</param>
    /// <param name="Rng">Generador ya sembrado por el llamador (mantiene el determinismo por (x,y,tipo)).</param>
    /// <param name="MaxAttempts">Número de intentos antes de hacer fallback.</param>
    public readonly record struct PlacementQuery(
        float Range,
        IReadOnlyList<Obstacle> Obstacles,
        float PlacedRadius,
        RandomNumberGenerator Rng,
        int MaxAttempts = 10);
}
