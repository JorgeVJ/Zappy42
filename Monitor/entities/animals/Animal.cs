using Godot;

/// <summary>
/// Base genérica de un animal decorativo móvil. Reúne las tres capas del sistema:
/// Domain (dónde puede moverse, espacio navegable), Locomotion (cómo se desplaza/gira
/// hacia un objetivo mediante steering suave) y Behavior (qué hace a lo largo del
/// tiempo). Cada frame ejecuta comportamiento, luego locomoción y por último el hook
/// de animación. Las subclases (p. ej. Fish) añaden su animación concreta y
/// reaccionan a la velocidad en <see cref="OnLocomotionUpdate"/>.
/// </summary>
/// <remarks>
/// Autocontenido: no referencia Terrain/Connection ni ningún tipo del proyecto.
/// </remarks>
public partial class Animal : Node3D
{
	public IAnimalDomain Domain { get; set; }
	public AnimalLocomotion Locomotion { get; protected set; } = new AnimalLocomotion();
	public IUtilityBehavior<Animal> Behavior { get; set; }
	public RandomNumberGenerator Rng { get; } = new RandomNumberGenerator();

	/// <summary>Blackboard: percepción cacheada 1×/frame y memoria compartida entre comportamientos.</summary>
	public AnimalContext Context { get; } = new AnimalContext();

	/// <summary>Ruta del modelo .glb que las subclases instancian; la fija el factory Create.</summary>
	protected string ModelPath;

	public override void _Ready()
	{
		Rng.Randomize();

		Behavior ??= new AmbientLocomotionBehavior();
		Behavior.Enter(this);
	}

	/// <summary>
	/// Instancia el modelo desde <see cref="ModelPath"/> (si hay uno), lo añade como hijo y lo
	/// devuelve para que la subclase resuelva su esqueleto o AnimationPlayer. Devuelve null si
	/// no hay ruta o el recurso no carga.
	/// </summary>
	protected Node3D LoadModel()
	{
		if (string.IsNullOrEmpty(ModelPath))
			return null;

		PackedScene packed = ResourceLoader.Load<PackedScene>(ModelPath);
		if (packed == null)
			return null;

		Node3D model = packed.Instantiate<Node3D>();
		AddChild(model);
		return model;
	}

	/// <summary>Busca en profundidad el primer descendiente del tipo pedido (incluido el propio nodo).</summary>
	protected static T FindInDescendants<T>(Node node) where T : Node
	{
		if (node is T match)
			return match;

		foreach (Node child in node.GetChildren())
		{
			T found = FindInDescendants<T>(child);
			if (found != null)
				return found;
		}

		return null;
	}

	public override void _Process(double delta)
	{
		if (Domain != null)
		{
			Context.Refresh(this);
			Behavior?.Tick(this, delta);
			Locomotion?.Tick(this, Domain, delta);
		}

		OnLocomotionUpdate(Locomotion?.CurrentSpeed ?? 0f);
	}

	/// <summary>
	/// Hook para que cada especie ajuste su animación según la velocidad actual.
	/// </summary>
	protected virtual void OnLocomotionUpdate(float speed) { }
}
