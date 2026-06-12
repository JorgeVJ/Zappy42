using System.Collections.Generic;

/// <summary>
/// Project-specific equipment configuration for the Shaman character.
/// Maps each player level (1-7) to a list of equipment slots (bone + GLB asset).
///
/// To reuse the equipment system in another project:
///   - Copy EquipmentManager.cs, EquipmentSlot.cs, EquipmentChild.cs, OrbitingPivot.cs and Offsets.cs unchanged.
///   - Create a new XxxEquipmentConfig.cs with the correct bone names and asset paths.
///
/// Shaman bone hierarchy (relevant slots):
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
    // Shared offsets — reused across levels
    // -------------------------------------------------------------------------

    // Staff.glb on RightHand. Adapted from the old staff_basic.glb offsets;
    // re-tune in-editor once Staff.glb is visible on the character.
    private static readonly Offsets StaffOffsets =
        new(new Godot.Vector3(-10.455f, 15.318f, -2.096f), new Godot.Vector3(-41.7f, 104.2f, -104.6f), new Godot.Vector3(50, 80, 50));

    // skull_mask.glb on Head, as used from Level 4 onward.
    private static readonly Offsets SkullMaskOffsets =
        new(new Godot.Vector3(0, 28f, 16f), new Godot.Vector3(-15F, 0.7f, -0.4f), new Godot.Vector3(16, 16, 16));

    // Placeholder identity offsets for the staff gem children — needs visual
    // tuning in-editor once the gem is visible attached to Staff.glb.
    private static readonly Offsets GemOffsets =
        new(new Godot.Vector3(0, 0, 0), new Godot.Vector3(0, 0, 0), new Godot.Vector3(1, 1, 1));

    // Gem children — each level's "active" gem replaces the previous one (not cumulative).
    private static readonly List<EquipmentChild> GemLvl1 = new()
    {
        new(Eq + "Staff_Gem_Lvl1.glb", GemOffsets),
    };

    private static readonly List<EquipmentChild> GemLvl2 = new()
    {
        new(Eq + "Staff_Gem_Lvl2.glb", GemOffsets),
    };

    private static readonly List<EquipmentChild> GemLvl3 = new()
    {
        new(Eq + "Staff_Gem_Lvl3.glb", GemOffsets),
    };

    // -------------------------------------------------------------------------
    // Orbiting gems above the head — replaces the old neck collar (see C10/D6).
    // A small group of gems spins continuously above the Shaman's head, growing
    // from 0 (levels 1-3) to 2 (levels 4-5) to 3 (levels 6-7).
    // -------------------------------------------------------------------------

    // Pivot anchored above the Head bone. Placeholder — needs visual tuning in-editor.
    public static readonly Offsets OrbitPivotOffsets =
        new(new Godot.Vector3(0, 45f, 0), new Godot.Vector3(0, 0, 0), new Godot.Vector3(1, 1, 1));

    // One full rotation every 6 seconds.
    public const float OrbitRotationSpeedDeg = 60f;

    // Reuses the smallest staff gem model. Position (orbit radius/angle) and scale are
    // placeholders — needs visual tuning in-editor once visible above the head.
    private static readonly List<EquipmentChild> OrbitGems2 = new()
    {
        new(Eq + "Staff_Gem_Lvl1.glb", new Offsets(new Godot.Vector3(18, 0, 0), new Godot.Vector3(0, 0, 0), new Godot.Vector3(30, 30, 30))),
        new(Eq + "Staff_Gem_Lvl1.glb", new Offsets(new Godot.Vector3(-18, 0, 0), new Godot.Vector3(0, 0, 0), new Godot.Vector3(30, 30, 30))),
    };

    private static readonly List<EquipmentChild> OrbitGems3 = new()
    {
        new(Eq + "Staff_Gem_Lvl1.glb", new Offsets(new Godot.Vector3(18, 0, 0), new Godot.Vector3(0, 0, 0), new Godot.Vector3(30, 30, 30))),
        new(Eq + "Staff_Gem_Lvl1.glb", new Offsets(new Godot.Vector3(-9, 0, 15.6f), new Godot.Vector3(0, 0, 0), new Godot.Vector3(30, 30, 30))),
        new(Eq + "Staff_Gem_Lvl1.glb", new Offsets(new Godot.Vector3(-9, 0, -15.6f), new Godot.Vector3(0, 0, 0), new Godot.Vector3(30, 30, 30))),
    };

    /// <summary>
    /// Returns the orbiting gem group for the given level, or null if no group
    /// should be shown (levels 1-3).
    /// </summary>
    public static IReadOnlyList<EquipmentChild> GetOrbitingGems(int level) =>
        level switch
        {
            4 or 5 => OrbitGems2,
            6 or 7 => OrbitGems3,
            _ => null
        };

    // -------------------------------------------------------------------------
    // Level definitions — edit here to change what each level wears
    // -------------------------------------------------------------------------

    // Level 1 — no accessories
    private static readonly List<EquipmentSlot> Level1 = new();

    // Level 2 — Staff
    private static readonly List<EquipmentSlot> Level2 = new()
    {
        new("RightHand", Eq + "Staff.glb", StaffOffsets),
    };

    // Level 3 — skull mask + Staff with Gem Lvl1
    private static readonly List<EquipmentSlot> Level3 = new()
    {
        new("Head", Eq + "skull_mask.glb"),
        new("RightHand", Eq + "Staff.glb", StaffOffsets, GemLvl1),
    };

    // Level 4 — skull mask (with offsets) + Staff with Gem Lvl1
    private static readonly List<EquipmentSlot> Level4 = new()
    {
        new("Head", Eq + "skull_mask.glb", SkullMaskOffsets),
        new("RightHand", Eq + "Staff.glb", StaffOffsets, GemLvl1),
    };

    // Level 5 — skull mask + Staff with Gem Lvl2 (replaces Lvl1)
    private static readonly List<EquipmentSlot> Level5 = new()
    {
        new("Head", Eq + "skull_mask.glb"),
        new("RightHand", Eq + "Staff.glb", StaffOffsets, GemLvl2),
    };

    // Level 6 — skull mask + Staff with Gem Lvl2 + shoulder bones
    private static readonly List<EquipmentSlot> Level6 = new()
    {
        new("Head",     Eq + "skull_mask.glb"),
        new("RightHand",     Eq + "Staff.glb", StaffOffsets, GemLvl2),
        new("LeftShoulder",  Eq + "shoulder_bone.glb"),
        new("RightShoulder", Eq + "shoulder_bone.glb"),
    };

    // Level 7 — full set + horns + Staff with Gem Lvl3 (replaces Lvl2)
    // (glow applied via shader in Player.cs, not here)
    private static readonly List<EquipmentSlot> Level7 = new()
    {
        new("Head",     Eq + "skull_mask.glb"),
        new("RightHand",     Eq + "Staff.glb", StaffOffsets, GemLvl3),
        new("LeftShoulder",  Eq + "shoulder_bone.glb"),
        new("RightShoulder", Eq + "shoulder_bone.glb"),
        new("Head",          Eq + "horns.glb"),
    };
}
