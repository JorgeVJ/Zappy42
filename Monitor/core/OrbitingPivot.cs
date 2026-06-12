using Godot;

/// <summary>
/// Generic, reusable Node3D that continuously rotates itself around its local Y axis.
/// Any children added to it (e.g. a group of orbiting gems) revolve together as a group.
/// Generic and reusable across projects — no project-specific data here.
/// </summary>
public partial class OrbitingPivot : Node3D
{
    /// <summary>Rotation speed in degrees per second.</summary>
    public float RotationSpeedDeg = 60f;

    public override void _Process(double delta)
    {
        RotateY(Mathf.DegToRad(RotationSpeedDeg) * (float)delta);
    }
}
