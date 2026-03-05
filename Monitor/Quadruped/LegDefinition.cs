using Godot;

public struct LegDefinition
{
    public string Name;
    public string RootBone;
    public string TipBone;
    public Vector3 Offset;

    public LegDefinition(string name, string root, string tip, Vector3 offset)
    {
        Name = name;
        RootBone = root;
        TipBone = tip;
        Offset = offset;
    }
}
