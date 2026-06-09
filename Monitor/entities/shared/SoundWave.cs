using Godot;

// Expanding ground ring used to visualize a broadcast (pbc) as sound propagating
// outward from the emitter's tile. Built entirely from code (no .tscn) and
// self-destructs once the animation finishes.
public partial class SoundWave : MeshInstance3D
{
    // World units the ring's outer edge grows to (base radius is 1.0, so this is
    // also the scale factor applied on X/Z). With TILE_SIZE = 2 a value of 6
    // covers roughly a 3-tile radius around the emitter.
    [Export] public float MaxRadius = 6.0f;
    [Export] public float Duration = 1.2f;
    [Export] public Color WaveColor = new Color(0.4f, 0.8f, 1.0f, 0.8f);

    public static SoundWave Create(Vector3 pos, Color? color = null)
    {
        var wave = new SoundWave();
        wave.Position = pos;
        if (color.HasValue)
            wave.WaveColor = color.Value;
        return wave;
    }

    public override void _Ready()
    {
        // Torus lies flat on the XZ plane (its axis is Y), so it reads as a ring
        // on the ground. Kept thin so it barely rises above the terrain.
        Mesh = new TorusMesh
        {
            InnerRadius = 0.85f,
            OuterRadius = 1.0f,
        };

        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = WaveColor,
            EmissionEnabled = true,
            Emission = WaveColor,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        MaterialOverride = mat;

        // Start as a small ring; expand only on X/Z so the tube stays flat.
        Scale = new Vector3(0.3f, 1.0f, 0.3f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", new Vector3(MaxRadius, 1.0f, MaxRadius), Duration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(mat, "albedo_color:a", 0.0f, Duration)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
