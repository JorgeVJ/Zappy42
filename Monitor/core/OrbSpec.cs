using Godot;

/// <summary>
/// Defines a single glowing orb placed around an <see cref="OrbitingPivot"/>.
/// Unlike <see cref="EquipmentChild"/>, it has no GLB scene — the orb is a
/// procedural sphere built by <see cref="GlowOrb"/>.
/// </summary>
public readonly struct OrbSpec
{
    public readonly Offsets Offsets;
    public readonly Color Color;
    public readonly GlowEffect Glow;

    public OrbSpec(Offsets offsets, Color color, GlowEffect glow)
    {
        Offsets = offsets;
        Color   = color;
        Glow    = glow;
    }
}
