using Godot;

public partial class Egg : Node3D
{
    private static PackedScene scene = ResourceLoader.Load("res://entities/egg/egg.tscn") as PackedScene;

    private MeshInstance3D mesh;
    private bool _hatched;

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

    // Transición visual de eclosión: tinte cálido + pequeño "pop" de escala.
    // No elimina el huevo (eso lo hace ebo cuando el jugador se conecta).
    public void Hatch()
    {
        if (_hatched)
            return;
        _hatched = true;

        if (mesh != null)
        {
            var mat = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.85f, 0.3f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.6f, 0.1f),
            };
            mesh.MaterialOverlay = mat;
        }

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector3.One * 1.3f, 0.15f);
        tween.TweenProperty(this, "scale", Vector3.One, 0.2f);
    }

    public override void _Process(double delta)
    {
    }
}
