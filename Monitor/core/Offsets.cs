using Godot;
using System;

/// <summary>
/// Helper struct to carry position/rotation (in degrees)/scale offsets for attachments.
/// </summary>
public struct Offsets
{
    public Vector3 Position { get; set; }
    public Vector3 RotationDeg { get; set; }
    public Vector3 Scale { get; set; }

    public Offsets(Vector3 position, Vector3 rotationDeg, Vector3 scale)
    {
        Position = position;
        RotationDeg = rotationDeg;
        Scale = scale;
    }

    public static Offsets Rotation(float x, float y, float z)
    {
        return new Offsets(new Vector3(0, 0, 0), new Vector3(x, y, z), new Vector3(1, 1, 1));
    }
}