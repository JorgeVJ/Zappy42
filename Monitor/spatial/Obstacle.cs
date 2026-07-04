using Godot;

public static partial class PlacementFinder
{
    /// <summary>Un obstáculo circular en el plano XZ: posición world y radio de exclusión.</summary>
    public readonly record struct Obstacle(Vector2 PositionXZ, float Radius);
}
