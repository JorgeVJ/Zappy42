using Godot;
using System;

public partial class Egg : Node3D
{
    private static PackedScene scene = ResourceLoader.Load("res://egg.tscn") as PackedScene;

    private MeshInstance3D mesh;

    public int Id { get; private set; }

    public static Egg Create(Vector3 pos, int id)
    {
        Egg instance = scene.Instantiate<Egg>();
        instance.Position = pos;
        instance.Id = id;
        instance.Name = $"Egg_{id}";
        return instance;
    }

    public override void _Ready()
    {
        mesh = GetNode<MeshInstance3D>("Mesh");
    }

    public override void _Process(double delta)
    {
    }
}
