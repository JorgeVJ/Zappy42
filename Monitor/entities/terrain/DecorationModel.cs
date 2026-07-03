using Godot;

public partial class DecorationSystem : Node3D
{
	private readonly record struct DecorationModel(PackedScene Scene, int FootprintW, int FootprintL);
}
