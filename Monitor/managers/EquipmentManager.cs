using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages instantiation and binding of scenes (armor, items, etc.) to bones of a Skeleton3D.
/// Keeps a cache of PackedScene by key and a dictionary of created BoneAttachment3D per bone.
/// </summary>
public class EquipmentManager
{
    private readonly Dictionary<string, PackedScene> sceneCache = new();
    private readonly Dictionary<string, List<BoneAttachment3D>> attachments = new();

    /// <summary>
    /// Registers and preloads a scene for later use.
    /// The key can be an alias (e.g. "armor") or the actual path ("res://...").
    /// </summary>
    public void RegisterScene(string key, string scenePath)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(scenePath))
        {
            Log.Error("EquipmentManager.RegisterScene: key or scenePath is null/empty");
            return;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            Log.Error($"EquipmentManager.RegisterScene: failed to load scene at '{scenePath}'");
            return;
        }

        sceneCache[key] = scene;
        Log.Debug($"EquipmentManager: scene '{scenePath}' registered as '{key}'");
    }

    /// <summary>
    /// Attempts to obtain the PackedScene registered by key. If the key is a valid path,
    /// it will also try to load it automatically.
    /// </summary>
    private PackedScene ResolveScene(string keyOrPath)
    {
        if (string.IsNullOrEmpty(keyOrPath))
            return null;

        if (sceneCache.TryGetValue(keyOrPath, out PackedScene scene))
            return scene;

        scene = ResourceLoader.Load<PackedScene>(keyOrPath);
        if (scene != null)
        {
            sceneCache[keyOrPath] = scene;
            Log.Debug($"EquipmentManager: implicitly loaded scene from path '{keyOrPath}'");
            return scene;
        }

        return null;
    }

    private Skeleton3D FindSkeleton3D(Node node)
    {
        if (node is Skeleton3D skeleton)
        {
            return skeleton;
        }

        foreach (Node child in node.GetChildren())
        {
            Skeleton3D found = FindSkeleton3D(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Attaches an instantiated scene to the specified bone of the skeleton.
    /// <paramref name="slot"/>'s ScenePath can be the registered key or a path to the scene.
    /// Optionally instantiates a list of child scenes as children of the attached
    /// instance itself (e.g. a gem socketed into a staff), each with its own offsets
    /// relative to the parent instance's local space.
    /// Returns the created BoneAttachment3D or null on error.
    /// </summary>
    public BoneAttachment3D AttachToBone(Node owner, EquipmentSlot slot)
    {
        if (!TryResolveAttachment(owner, slot, out Skeleton3D skeleton, out PackedScene scene))
            return null;

        return BuildBoneAttachment(skeleton, slot, scene);
    }

    /// <summary>
    /// Validates that <paramref name="owner"/> has a skeleton, <paramref name="slot"/>'s
    /// bone exists on it and its scene can be resolved. Extracted from AttachToBone to
    /// keep that method within the project's method-length convention.
    /// </summary>
    private bool TryResolveAttachment(Node owner, EquipmentSlot slot, out Skeleton3D skeleton, out PackedScene scene)
    {
        scene = null;
        if (!TryResolveSkeletonBone(owner, slot, out skeleton))
            return false;

        scene = ResolveScene(slot.ScenePath);
        if (scene == null)
        {
            Log.Error($"EquipmentManager: scene not found for key/path '{slot.ScenePath}'");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that <paramref name="owner"/> has a skeleton and that <paramref name="slot"/>'s
    /// bone exists on it. Extracted from TryResolveAttachment to keep that method within the
    /// project's method-length convention.
    /// </summary>
    private bool TryResolveSkeletonBone(Node owner, EquipmentSlot slot, out Skeleton3D skeleton)
    {
        skeleton = FindSkeleton3D(owner);
        if (skeleton == null)
        {
            Log.Error("EquipmentManager.AttachToBone: skeleton is null");
            return false;
        }

        if (string.IsNullOrEmpty(slot.BoneName) || string.IsNullOrEmpty(slot.ScenePath))
        {
            Log.Error("EquipmentManager.AttachToBone: boneName or sceneKey is null/empty");
            return false;
        }

        if (skeleton.FindBone(slot.BoneName) == -1)
        {
            Log.Debug($"EquipmentManager: bone not found: {slot.BoneName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Instantiates <paramref name="scene"/>, attaches it to a new BoneAttachment3D on
    /// <paramref name="slot"/>'s bone, applies offsets/children and registers the
    /// attachment. Extracted from AttachToBone to keep that method within the
    /// project's method-length convention.
    /// </summary>
    private BoneAttachment3D BuildBoneAttachment(Skeleton3D skeleton, EquipmentSlot slot, PackedScene scene)
    {
        BoneAttachment3D boneAttach = new();
        boneAttach.BoneName = slot.BoneName;
        skeleton.AddChild(boneAttach);

        if (!TryInstantiate(scene, slot.ScenePath, out Node3D inst))
        {
            boneAttach.QueueFree();
            return null;
        }

        boneAttach.AddChild(inst);
        inst.Transform = Transform3D.Identity;
        ApplyOffsetsAndChildren(inst, slot);
        RegisterAttachment(slot.BoneName, boneAttach);

        Log.Debug($"EquipmentManager: attached scene '{slot.ScenePath}' to bone '{slot.BoneName}'");
        return boneAttach;
    }

    /// <summary>
    /// Applies a slot's offsets and instantiates its child scenes. Extracted from
    /// BuildBoneAttachment to keep that method within the method-length convention.
    /// </summary>
    private void ApplyOffsetsAndChildren(Node3D inst, EquipmentSlot slot)
    {
        if (slot.Offsets is not null)
            ApplyOffsets(inst, slot.Offsets.Value);

        if (slot.Children is not null)
            foreach (EquipmentChild child in slot.Children)
                AttachChild(inst, child);
    }

    /// <summary>
    /// Instantiates a scene as a Node3D, logging and reporting failure instead of throwing.
    /// </summary>
    private static bool TryInstantiate(PackedScene scene, string scenePath, out Node3D instance)
    {
        try
        {
            instance = scene.Instantiate<Node3D>();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"EquipmentManager: failed to instantiate scene '{scenePath}': {ex.Message}");
            instance = null;
            return false;
        }
    }

    /// <summary>
    /// Applies position/rotation(deg)/scale offsets to a Node3D.
    /// </summary>
    private void ApplyOffsets(Node3D target, Offsets offsets)
    {
        target.Position = offsets.Position;
        target.Rotation = new Vector3(
            Mathf.DegToRad(offsets.RotationDeg.X),
            Mathf.DegToRad(offsets.RotationDeg.Y),
            Mathf.DegToRad(offsets.RotationDeg.Z));
        target.Scale = offsets.Scale;
    }

    /// <summary>
    /// Adds a BoneAttachment3D to the tracked list for a bone, creating the list if needed.
    /// </summary>
    private void RegisterAttachment(string boneName, BoneAttachment3D boneAttach)
    {
        if (!attachments.TryGetValue(boneName, out List<BoneAttachment3D> list))
        {
            list = new List<BoneAttachment3D>();
            attachments[boneName] = list;
        }
        list.Add(boneAttach);
    }

    /// <summary>
    /// Instantiates a child scene as a regular child Node3D of an already-attached
    /// equipment instance, applying its own offsets in the parent's local space.
    /// Errors are logged but non-fatal — a missing/invalid child model does not
    /// affect the parent equipment piece.
    /// </summary>
    private void AttachChild(Node3D parent, EquipmentChild child)
    {
        if (string.IsNullOrEmpty(child.ScenePath))
            return;

        PackedScene scene = ResolveScene(child.ScenePath);
        if (scene == null)
        {
            Log.Error($"EquipmentManager: child scene not found for path '{child.ScenePath}'");
            return;
        }

        if (!TryInstantiate(scene, child.ScenePath, out Node3D childInst))
            return;

        parent.AddChild(childInst);
        childInst.Transform = Transform3D.Identity;

        if (child.Offsets is not null)
            ApplyOffsets(childInst, child.Offsets.Value);

        if (child.Glow is not null)
            child.Glow.Value.ApplyTo(childInst);

        Log.Debug($"EquipmentManager: attached child scene '{child.ScenePath}' to '{parent.Name}'");
    }

    /// <summary>
    /// Instantiates a procedural <see cref="GlowOrb"/> as a regular child of <paramref name="parent"/>,
    /// applying its offsets in the parent's local space. Mirrors <see cref="AttachChild"/> but for
    /// procedural orbs instead of GLB scenes.
    /// </summary>
    private void AttachOrb(Node3D parent, OrbSpec spec)
    {
        GlowOrb orb = new() { OrbColor = spec.Color, Glow = spec.Glow };
        parent.AddChild(orb);
        orb.Transform = Transform3D.Identity;
        ApplyOffsets(orb, spec.Offsets);
    }

    /// <summary>
    /// Attaches a continuously-rotating <see cref="OrbitingPivot"/> to the specified bone,
    /// with a group of procedural glowing orbs arranged around it. The whole group spins
    /// together around the pivot's local Y axis. If orbs is null or empty, nothing is
    /// attached (e.g. for levels where the group is not yet unlocked).
    /// Tracked alongside regular attachments, so ClearAll()/ApplyLoadout() remove it too.
    /// Returns the created BoneAttachment3D or null if nothing was attached.
    /// </summary>
    public BoneAttachment3D AttachOrbitingGroup(Node owner, OrbitingSlot slot)
    {
        if (slot.Orbs == null || slot.Orbs.Count == 0)
            return null;

        Skeleton3D skeleton = FindSkeleton3D(owner);
        if (skeleton == null)
        {
            Log.Error("EquipmentManager.AttachOrbitingGroup: skeleton is null");
            return null;
        }

        if (string.IsNullOrEmpty(slot.BoneName) || skeleton.FindBone(slot.BoneName) == -1)
        {
            Log.Debug($"EquipmentManager: bone not found: {slot.BoneName}");
            return null;
        }

        BoneAttachment3D boneAttach = BuildOrbitingGroup(skeleton, slot);

        Log.Debug($"EquipmentManager: attached orbiting group of {slot.Orbs.Count} orb(s) to bone '{slot.BoneName}'");
        return boneAttach;
    }

    /// <summary>
    /// Creates the BoneAttachment3D/OrbitingPivot hierarchy for AttachOrbitingGroup and
    /// registers it. Extracted to keep AttachOrbitingGroup within the method-length
    /// convention.
    /// </summary>
    private BoneAttachment3D BuildOrbitingGroup(Skeleton3D skeleton, OrbitingSlot slot)
    {
        BoneAttachment3D boneAttach = new();
        boneAttach.BoneName = slot.BoneName;
        skeleton.AddChild(boneAttach);

        OrbitingPivot pivot = new() { RotationSpeedDeg = slot.RotationSpeedDeg };
        boneAttach.AddChild(pivot);
        ApplyOffsets(pivot, slot.PivotOffsets);

        foreach (OrbSpec orb in slot.Orbs)
            AttachOrb(pivot, orb);

        RegisterAttachment(slot.BoneName, boneAttach);
        return boneAttach;
    }

    /// <summary>
    /// Returns the list of BoneAttachment3D attached to a bone (if any).
    /// </summary>
    public List<BoneAttachment3D> GetAttachments(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return null;
        return attachments.TryGetValue(boneName, out List<BoneAttachment3D> list) ? list : null;
    }

    /// <summary>
    /// Removes and frees all attachments associated with a bone.
    /// </summary>
    public void RemoveAttachments(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return;

        if (!attachments.TryGetValue(boneName, out List<BoneAttachment3D> list)) return;

        foreach (BoneAttachment3D attach in list)
        {
            if (attach.IsInsideTree())
                attach.QueueFree();
        }

        attachments.Remove(boneName);
        Log.Debug($"EquipmentManager: removed attachments for bone '{boneName}'");
    }

    /// <summary>
    /// Removes and frees all managed attachments.
    /// </summary>
    public void ClearAll()
    {
        foreach (KeyValuePair<string, List<BoneAttachment3D>> kv in attachments)
        {
            foreach (BoneAttachment3D attach in kv.Value)
            {
                if (attach.IsInsideTree())
                    attach.QueueFree();
            }
        }
        attachments.Clear();
        Log.Debug("EquipmentManager: cleared all attachments");
    }

    /// <summary>
    /// Clears all current attachments and applies a new set of equipment slots.
    /// Call this whenever the character's level (or equipment state) changes.
    /// </summary>
    public void ApplyLoadout(Node owner, IReadOnlyList<EquipmentSlot> slots)
    {
        ClearAll();
        foreach (EquipmentSlot slot in slots)
            AttachToBone(owner, slot);
    }
}
