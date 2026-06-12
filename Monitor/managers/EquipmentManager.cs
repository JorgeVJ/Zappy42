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
            GD.PrintErr("EquipmentManager.RegisterScene: key or scenePath is null/empty");
            return;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            GD.PrintErr($"EquipmentManager.RegisterScene: failed to load scene at '{scenePath}'");
            return;
        }

        sceneCache[key] = scene;
        GD.Print($"EquipmentManager: scene '{scenePath}' registered as '{key}'");
    }

    /// <summary>
    /// Attempts to obtain the PackedScene registered by key. If the key is a valid path,
    /// it will also try to load it automatically.
    /// </summary>
    private PackedScene ResolveScene(string keyOrPath)
    {
        if (string.IsNullOrEmpty(keyOrPath))
            return null;

        if (sceneCache.TryGetValue(keyOrPath, out var scene))
            return scene;

        scene = ResourceLoader.Load<PackedScene>(keyOrPath);
        if (scene != null)
        {   
            sceneCache[keyOrPath] = scene;
            GD.Print($"EquipmentManager: implicitly loaded scene from path '{keyOrPath}'");
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
    /// sceneKey can be the registered key or a path to the scene.
    /// Optionally instantiates a list of child scenes as children of the attached
    /// instance itself (e.g. a gem socketed into a staff), each with its own offsets
    /// relative to the parent instance's local space.
    /// Returns the created BoneAttachment3D or null on error.
    /// </summary>
    public BoneAttachment3D AttachToBone(Node owner, string boneName, string sceneKey, Offsets? offsets = null, IReadOnlyList<EquipmentChild> children = null)
    {
        Skeleton3D skeleton = FindSkeleton3D(owner);
        if (skeleton == null)
        {
            GD.PrintErr("EquipmentManager.AttachToBone: skeleton is null");
            return null;
        }

        if (string.IsNullOrEmpty(boneName) || string.IsNullOrEmpty(sceneKey))
        {
            GD.PrintErr("EquipmentManager.AttachToBone: boneName or sceneKey is null/empty");
            return null;
        }

        if (skeleton.FindBone(boneName) == -1)
        {
            GD.Print($"EquipmentManager: bone not found: {boneName}");
            return null;
        }

        PackedScene scene = ResolveScene(sceneKey);
        if (scene == null)
        {
            GD.PrintErr($"EquipmentManager: scene not found for key/path '{sceneKey}'");
            return null;
        }

        BoneAttachment3D boneAttach = new();
        boneAttach.BoneName = boneName;
        skeleton.AddChild(boneAttach);

        // Instantiate the scene and add it to the BoneAttachment
        Node3D inst;
        try
        {
            inst = scene.Instantiate<Node3D>();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EquipmentManager: failed to instantiate scene '{sceneKey}': {ex.Message}");
            boneAttach.QueueFree();
            return null;
        }

        boneAttach.AddChild(inst);

        // Reset transform to exactly inherit from the bone
        inst.Transform = Transform3D.Identity;

        // Apply offsets if provided
        if (offsets is not null)
        {
            inst.Position = offsets.Value.Position;
            inst.Rotation = new Vector3(
                Mathf.DegToRad(offsets.Value.RotationDeg.X),
                Mathf.DegToRad(offsets.Value.RotationDeg.Y),
                Mathf.DegToRad(offsets.Value.RotationDeg.Z));
            inst.Scale = offsets.Value.Scale;
        }

        // Instantiate optional child models attached to this equipment instance
        // (e.g. a gem socketed into a staff). Offsets are relative to inst's local space.
        if (children is not null)
        {
            foreach (var child in children)
            {
                AttachChild(inst, child);
            }
        }

        // Store reference
        if (!attachments.TryGetValue(boneName, out var list))
        {
            list = new List<BoneAttachment3D>();
            attachments[boneName] = list;
        }
        list.Add(boneAttach);

        GD.Print($"EquipmentManager: attached scene '{sceneKey}' to bone '{boneName}'");
        return boneAttach;
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
            GD.PrintErr($"EquipmentManager: child scene not found for path '{child.ScenePath}'");
            return;
        }

        Node3D childInst;
        try
        {
            childInst = scene.Instantiate<Node3D>();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EquipmentManager: failed to instantiate child scene '{child.ScenePath}': {ex.Message}");
            return;
        }

        parent.AddChild(childInst);
        childInst.Transform = Transform3D.Identity;

        if (child.Offsets is not null)
        {
            childInst.Position = child.Offsets.Value.Position;
            childInst.Rotation = new Vector3(
                Mathf.DegToRad(child.Offsets.Value.RotationDeg.X),
                Mathf.DegToRad(child.Offsets.Value.RotationDeg.Y),
                Mathf.DegToRad(child.Offsets.Value.RotationDeg.Z));
            childInst.Scale = child.Offsets.Value.Scale;
        }

        if (child.Glow is not null)
            child.Glow.Value.ApplyTo(childInst);

        GD.Print($"EquipmentManager: attached child scene '{child.ScenePath}' to '{parent.Name}'");
    }

    /// <summary>
    /// Instantiates a procedural <see cref="GlowOrb"/> as a regular child of <paramref name="parent"/>,
    /// applying its offsets in the parent's local space. Mirrors <see cref="AttachChild"/> but for
    /// procedural orbs instead of GLB scenes.
    /// </summary>
    private void AttachOrb(Node3D parent, OrbSpec spec)
    {
        var orb = new GlowOrb { OrbColor = spec.Color, Glow = spec.Glow };
        parent.AddChild(orb);
        orb.Transform = Transform3D.Identity;

        orb.Position = spec.Offsets.Position;
        orb.Rotation = new Vector3(
            Mathf.DegToRad(spec.Offsets.RotationDeg.X),
            Mathf.DegToRad(spec.Offsets.RotationDeg.Y),
            Mathf.DegToRad(spec.Offsets.RotationDeg.Z));
        orb.Scale = spec.Offsets.Scale;
    }

    /// <summary>
    /// Attaches a continuously-rotating <see cref="OrbitingPivot"/> to the specified bone,
    /// with a group of procedural glowing orbs arranged around it. The whole group spins
    /// together around the pivot's local Y axis. If orbs is null or empty, nothing is
    /// attached (e.g. for levels where the group is not yet unlocked).
    /// Tracked alongside regular attachments, so ClearAll()/ApplyLoadout() remove it too.
    /// Returns the created BoneAttachment3D or null if nothing was attached.
    /// </summary>
    public BoneAttachment3D AttachOrbitingGroup(Node owner, string boneName, Offsets pivotOffsets, float rotationSpeedDeg, IReadOnlyList<OrbSpec> orbs)
    {
        if (orbs == null || orbs.Count == 0)
            return null;

        Skeleton3D skeleton = FindSkeleton3D(owner);
        if (skeleton == null)
        {
            GD.PrintErr("EquipmentManager.AttachOrbitingGroup: skeleton is null");
            return null;
        }

        if (string.IsNullOrEmpty(boneName) || skeleton.FindBone(boneName) == -1)
        {
            GD.Print($"EquipmentManager: bone not found: {boneName}");
            return null;
        }

        BoneAttachment3D boneAttach = new();
        boneAttach.BoneName = boneName;
        skeleton.AddChild(boneAttach);

        var pivot = new OrbitingPivot { RotationSpeedDeg = rotationSpeedDeg };
        boneAttach.AddChild(pivot);

        pivot.Position = pivotOffsets.Position;
        pivot.Rotation = new Vector3(
            Mathf.DegToRad(pivotOffsets.RotationDeg.X),
            Mathf.DegToRad(pivotOffsets.RotationDeg.Y),
            Mathf.DegToRad(pivotOffsets.RotationDeg.Z));
        pivot.Scale = pivotOffsets.Scale;

        foreach (var orb in orbs)
            AttachOrb(pivot, orb);

        if (!attachments.TryGetValue(boneName, out var list))
        {
            list = new List<BoneAttachment3D>();
            attachments[boneName] = list;
        }
        list.Add(boneAttach);

        GD.Print($"EquipmentManager: attached orbiting group of {orbs.Count} orb(s) to bone '{boneName}'");
        return boneAttach;
    }

    /// <summary>
    /// Returns the list of BoneAttachment3D attached to a bone (if any).
    /// </summary>
    public List<BoneAttachment3D> GetAttachments(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return null;
        return attachments.TryGetValue(boneName, out var list) ? list : null;
    }

    /// <summary>
    /// Removes and frees all attachments associated with a bone.
    /// </summary>
    public void RemoveAttachments(string boneName)
    {
        if (string.IsNullOrEmpty(boneName)) return;

        if (!attachments.TryGetValue(boneName, out var list)) return;

        foreach (var attach in list)
        {
            if (attach.IsInsideTree())
                attach.QueueFree();
        }

        attachments.Remove(boneName);
        GD.Print($"EquipmentManager: removed attachments for bone '{boneName}'");
    }

    /// <summary>
    /// Removes and frees all managed attachments.
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in attachments)
        {
            foreach (var attach in kv.Value)
            {
                if (attach.IsInsideTree())
                    attach.QueueFree();
            }
        }
        attachments.Clear();
        GD.Print("EquipmentManager: cleared all attachments");
    }

    /// <summary>
    /// Clears all current attachments and applies a new set of equipment slots.
    /// Call this whenever the character's level (or equipment state) changes.
    /// </summary>
    public void ApplyLoadout(Node owner, IReadOnlyList<EquipmentSlot> slots)
    {
        ClearAll();
        foreach (var slot in slots)
            AttachToBone(owner, slot.BoneName, slot.ScenePath, slot.Offsets, slot.Children);
    }
}