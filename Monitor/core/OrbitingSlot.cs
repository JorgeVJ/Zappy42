using System.Collections.Generic;

/// <summary>
/// Defines a group of procedural orbs that orbit around a pivot attached to a bone.
/// </summary>
public readonly struct OrbitingSlot
{
    public readonly string BoneName;
    public readonly Offsets PivotOffsets;
    public readonly float RotationSpeedDeg;
    public readonly IReadOnlyList<OrbSpec> Orbs;

    public OrbitingSlot(string boneName, Offsets pivotOffsets, float rotationSpeedDeg, IReadOnlyList<OrbSpec> orbs)
    {
        BoneName         = boneName;
        PivotOffsets     = pivotOffsets;
        RotationSpeedDeg = rotationSpeedDeg;
        Orbs             = orbs;
    }
}
