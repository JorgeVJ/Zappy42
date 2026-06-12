using Godot;

/// <summary>
/// Generic, reusable MeshInstance3D that renders itself as a small glowing
/// sphere: a translucent unshaded material with a rim highlight, topped with
/// an emissive glow applied via <see cref="GlowEffect"/>.
/// Generic and reusable across projects — no project-specific data here.
/// </summary>
public partial class GlowOrb : MeshInstance3D
{
    public Color OrbColor = Colors.White;
    public GlowEffect Glow = new(Colors.Red);

    public override void _Ready()
    {
        Mesh = new SphereMesh { Radius = 1.5f, Height = 1.5f, RadialSegments = 16, Rings = 8 };

        //var shader = GD.Load<Shader>("res://entities/player/models/equipments/orb.gdshader");

        //var material = new ShaderMaterial
        //{
        //    Shader = shader
        //};

        // SetSurfaceOverrideMaterial(0, material);

        Glow.ApplyTo(this);
    }
}
