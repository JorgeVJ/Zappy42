using System.Collections.Generic;

/// <summary>
/// Project-specific equipment configuration for the Shaman character.
/// Maps each player level (1-7) to a list of equipment slots (bone + GLB asset).
///
/// To reuse the equipment system in another project:
///   - Copy EquipmentManager.cs, EquipmentSlot.cs and Offsets.cs unchanged.
///   - Create a new XxxEquipmentConfig.cs with the correct bone names and asset paths.
///
/// Shaman bone hierarchy (relevant slots):
///   neck          → necklace / pendant
///   Headfront     → mask / face piece
///   Head          → horns / hat
///   RightHand     → staff / weapon
///   LeftShoulder / RightShoulder → shoulder pieces
///   LeftForeArm  / RightForeArm  → bracers
/// </summary>
public static class ShamanEquipmentConfig
{
    private const string Eq = "res://entities/player/models/equipments/";

    /// <summary>
    /// Returns the list of equipment slots for the given level.
    /// Missing GLB files are silently ignored by EquipmentManager (logs a warning).
    /// </summary>
    public static IReadOnlyList<EquipmentSlot> GetLoadout(int level) =>
        level switch
        {
            1 => Level1,
            2 => Level2,
            3 => Level3,
            4 => Level4,
            5 => Level5,
            6 => Level6,
            7 => Level7,
            _ => Level1
        };

    // -------------------------------------------------------------------------
    // Level definitions — edit here to change what each level wears
    // -------------------------------------------------------------------------

    // Level 1 — no accessories
    private static readonly List<EquipmentSlot> Level1 = new();

    // Level 2 — bone necklace
    private static readonly List<EquipmentSlot> Level2 = new()
    {
        new("neck", Eq + "collar_bone.glb"),
    };

    // Level 3 — bone necklace + skull mask
    private static readonly List<EquipmentSlot> Level3 = new()
    {
        new("neck",      Eq + "collar_bone.glb"),
        new("Head", Eq + "skull_mask.glb"),
    };

    // Level 4 — bone necklace + skull mask + basic staff
    private static readonly List<EquipmentSlot> Level4 = new()
    {
        new("neck",      Eq + "collar_bone.glb"),
        new("Head", Eq + "skull_mask.glb", new Offsets(new Godot.Vector3(0, 28f, 16f), new Godot.Vector3(-15F, 0.7f, -0.4f), new Godot.Vector3(16, 16, 16))),
        new("RightHand", Eq + "staff_basic.glb", new Offsets(new Godot.Vector3(-10.455f, 15.318f, -2.096f), new Godot.Vector3(-41.7f, 104.2f, -104.6f), new Godot.Vector3(50, 80, 50))),
    };

    // Level 5 — gem necklace (replaces bone) + skull mask + basic staff
    private static readonly List<EquipmentSlot> Level5 = new()
    {
        new("neck",      Eq + "collar_gem.glb"),
        new("Head", Eq + "skull_mask.glb"),
        new("RightHand", Eq + "staff_basic.glb"),
    };

    // Level 6 — gem necklace + skull mask + orb staff + shoulder bones
    private static readonly List<EquipmentSlot> Level6 = new()
    {
        new("neck",          Eq + "collar_gem.glb"),
        new("Head",     Eq + "skull_mask.glb"),
        new("RightHand",     Eq + "staff_orb.glb"),
        new("LeftShoulder",  Eq + "shoulder_bone.glb"),
        new("RightShoulder", Eq + "shoulder_bone.glb"),
    };

    // Level 7 — full set + horns (glow applied via shader in Player.cs, not here)
    private static readonly List<EquipmentSlot> Level7 = new()
    {
        new("neck",          Eq + "collar_gem.glb"),
        new("Head",     Eq + "skull_mask.glb"),
        new("RightHand",     Eq + "staff_orb.glb"),
        new("LeftShoulder",  Eq + "shoulder_bone.glb"),
        new("RightShoulder", Eq + "shoulder_bone.glb"),
        new("Head",          Eq + "horns.glb"),
    };
}
