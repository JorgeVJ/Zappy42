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
    public GlowEffect Glow = new(Colors.White);

    public override void _Ready()
    {
        Mesh = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 16, Rings = 8 };

        var mat = new StandardMaterial3D
        {
            AlbedoColor = OrbColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            RimEnabled = true,
            Rim = 1.0f,
        };
        SetSurfaceOverrideMaterial(0, mat);

        Glow.ApplyTo(this);
    }
}
