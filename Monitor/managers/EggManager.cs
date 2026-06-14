using Godot;

public partial class EggManager : EntityManager<Egg>
{
    protected override string ContainerName => "Eggs";

    public Egg CreateEgg(int id, Vector3 pos)
    {
        if (entities.TryGetValue(id, out var existing))
            return existing;

        var egg = Egg.Create(pos, id);
        return Register(id, egg);
    }
}
