/// <summary>
/// Defines a child model attached directly to an already-instantiated equipment scene
/// (e.g. a gem socketed into a staff), rather than to a skeleton bone.
/// Offsets are relative to the parent equipment instance's local space.
/// </summary>
public readonly struct EquipmentChild
{
    public readonly string ScenePath;
    public readonly Offsets? Offsets;
    public readonly GlowEffect? Glow;

    public EquipmentChild(string scenePath, Offsets? offsets = null, GlowEffect? glow = null)
    {
        ScenePath = scenePath;
        Offsets   = offsets;
        Glow      = glow;
    }
}
