using Godot;

/// <summary>
/// Generic, reusable MeshInstance3D that renders itself as a small glowing
/// sphere: a translucent unshaded material with a rim highlight, topped with
/// an emissive glow applied via <see cref="GlowEffect"/>.
/// </summary>
public partial class GlowOrb : MeshInstance3D
{
    public Color OrbColor = Colors.White;
    public GlowEffect Glow = new(Colors.Red);

    public override void _Ready()
    {
        Mesh = new SphereMesh { Radius = 1.5f, Height = 1.5f, RadialSegments = 16, Rings = 8 };

        Glow.ApplyTo(this);
    }
}
