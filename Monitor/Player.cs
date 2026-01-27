using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Node3D
{
    private static PackedScene scene = ResourceLoader.Load("res://player.tscn") as PackedScene;

    private MeshInstance3D mesh;

    public int Id { get; private set; }
    public string TeamName { get; private set; } = "";
    public int Level { get; private set; } = 1;
    public int Orientation { get; private set; } = 1; // 1..4 en Zappy
    public Dictionary<Resource.ResourceType, int> Inventory { get; private set; } = new();
    public Vector2I TilePos { get; private set; } = new Vector2I(0, 0);

    public static Player Create(Vector3 pos)
    {
        Player instance = scene.Instantiate<Player>();
        instance.Position = pos;
        return instance;
    }

    public void SetTilePos(int x, int y)
    {
        TilePos = new Vector2I(x, y);
    }

    public void SetInventory(int[] counts)
    {
        // counts.Length debe ser 7
        for (int i = 0; i < 7; i++)
            Inventory[(Resource.ResourceType)i] = counts[i];
    }

    public void AddToInventory(Resource.ResourceType type, int amount = 1)
    {
        if (!Inventory.ContainsKey(type))
            Inventory[type] = 0;

        Inventory[type] += amount;
    }

    public void RemoveFromInventory(Resource.ResourceType type, int amount = 1)
    {
        if (!Inventory.ContainsKey(type))
            Inventory[type] = 0;

        Inventory[type] = Math.Max(0, Inventory[type] - amount);
    }

    public override void _Ready()
    {
        mesh = GetNode<MeshInstance3D>("Mesh");
    }

    public void Init(int id, string teamName)
    {
        Id = id;
        TeamName = teamName;
        Name = $"Player_{id}";
    }

    public void SetLevel(int level)
    {
        Level = level;
    }

    public void SetOrientation(int o)
    {
        Orientation = o;

        // Zappy: 1=N, 2=E, 3=S, 4=W (normalmente)
        float yaw = o switch
        {
            1 => 0f,
            2 => Mathf.Pi / 2f,
            3 => Mathf.Pi,
            4 => -Mathf.Pi / 2f,
            _ => 0f
        };

        Rotation = new Vector3(0, yaw, 0);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
