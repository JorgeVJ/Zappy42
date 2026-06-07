using Godot;
using System.Collections.Generic;

public partial class EggManager : Node
{
    private Dictionary<int, Egg> eggs = new();

    private Node3D eggContainer;

    public override void _Ready()
    {
        eggContainer = GetNodeOrNull<Node3D>("Eggs");
        if (eggContainer == null)
        {
            eggContainer = new Node3D();
            eggContainer.Name = "Eggs";
            AddChild(eggContainer);
        }
    }

    public Egg CreateEgg(int id, Vector3 pos)
    {
        if (eggs.ContainsKey(id))
            return eggs[id];

        var egg = Egg.Create(pos, id);
        eggContainer.AddChild(egg);
        eggs[id] = egg;
        return egg;
    }

    public bool TryGet(int id, out Egg egg) => eggs.TryGetValue(id, out egg);

    public void Remove(int id)
    {
        if (!eggs.TryGetValue(id, out var egg))
            return;

        egg.QueueFree();
        eggs.Remove(id);
    }
}
