using Godot;
using zappy;

public partial class SelectableInventoryNode3D : Node3D, ISelectable, IInventory
{
    protected MeshInstance3D mesh;

    private Inventory inventory;
    public Inventory Inventory => inventory ??= new Inventory();

    public virtual string DisplayTitle => "Objeto";

    public override void _Ready()
    {
        mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
        inventory = new Inventory();
    }

    public virtual void Highlight()
    {
        if (mesh == null)
        {
            return;
        }

        StandardMaterial3D mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.DarkCyan;
        mesh.MaterialOverlay = mat;
    }

    public virtual void UnHighlight()
    {
        if (mesh == null)
        {
            return;
        }

        mesh.MaterialOverlay = null;
    }
}