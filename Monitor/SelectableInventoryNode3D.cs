using Godot;
using zappy;

public partial class SelectableInventoryNode3D : Node3D, ISelectable, IInventory
{
    protected MeshInstance3D mesh;

    private Inventory inventory;
    public Inventory Inventory => inventory ??= GetNode<Inventory>("Inventory");

    [Signal]
    public delegate void OnClickedEventHandler(Node3D sender);

    public override void _Ready()
    {
        mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
        inventory = GetNodeOrNull<Inventory>("Inventory");
    }

    private void _on_area_3d_input_event(
        Node camera,
        InputEvent @event,
        Vector3 position,
        Vector3 normal,
        int shapeIdx)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(nameof(OnClicked), this);
        }
    }

    public virtual void Highlight()
    {
        if (mesh == null)
        {
            return;
        }

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.DarkCyan;
        mesh.MaterialOverlay = mat;
    }

    public virtual void UnHightlight()
    {
        if (mesh == null)
        {
            return;
        }

        mesh.MaterialOverlay = null;
    }
}