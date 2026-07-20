using Godot;

/// <summary>
/// Defines an emissive glow applied as a material overlay on a model's meshes.
/// Generic and reusable: can be applied to any Node3D (equipment pieces, resources,
/// eggs, etc.), not just equipment children.
/// </summary>
public readonly struct GlowEffect
{
    public readonly Color Color;
    public readonly float EnergyMultiplier;

    public GlowEffect(Color color, float energyMultiplier = 1.0f)
    {
        Color = color;
        EnergyMultiplier = energyMultiplier;
    }

    /// <summary>
    /// Recursively enables emission on every surface of every MeshInstance3D under
    /// <paramref name="root"/> (inclusive). Each surface's existing material is duplicated
    /// so its original albedo/texture is preserved, with emission added on top.
    /// </summary>
    public void ApplyTo(Node root)
    {
        if (root is MeshInstance3D mesh)
        {
            int surfaceCount = mesh.Mesh?.GetSurfaceCount() ?? 0;
            for (int i = 0; i < surfaceCount; i++)
            {
                StandardMaterial3D material = mesh.GetActiveMaterial(i) is StandardMaterial3D original
                    ? (StandardMaterial3D)original.Duplicate()
                    : new StandardMaterial3D();

                material.EmissionEnabled = true;
                material.Emission = Color;
                material.EmissionEnergyMultiplier = EnergyMultiplier;

                mesh.SetSurfaceOverrideMaterial(i, material);
            }
        }

        foreach (Node child in root.GetChildren())
            ApplyTo(child);
    }
}
