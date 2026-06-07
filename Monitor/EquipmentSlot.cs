/// <summary>
/// Defines a single equipment piece to attach to a specific bone.
/// Generic and reusable across projects — no project-specific data here.
/// </summary>
public readonly struct EquipmentSlot
{
    public readonly string BoneName;
    public readonly string ScenePath;
    public readonly Offsets? Offsets;

    public EquipmentSlot(string boneName, string scenePath, Offsets? offsets = null)
    {
        BoneName  = boneName;
        ScenePath = scenePath;
        Offsets   = offsets;
    }
}
