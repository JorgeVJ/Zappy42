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
    /// Returns the created BoneAttachment3D or null on error.
    /// </summary>
    public BoneAttachment3D AttachToBone(Node owner, string boneName, string sceneKey, Offsets? offsets = null)
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
}