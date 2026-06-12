/// <summary>
/// Defines a child model attached directly to an already-instantiated equipment scene
/// (e.g. a gem socketed into a staff), rather than to a skeleton bone.
/// Offsets are relative to the parent equipment instance's local space.
/// Generic and reusable across projects — no project-specific data here.
/// </summary>
public readonly struct EquipmentChild
{
    public readonly string ScenePath;
    public readonly Offsets? Offsets;

    public EquipmentChild(string scenePath, Offsets? offsets = null)
    {
        ScenePath = scenePath;
        Offsets   = offsets;
    }
}
