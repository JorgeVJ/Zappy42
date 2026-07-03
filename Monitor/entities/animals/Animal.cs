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
	public IAnimalBehavior Behavior { get; set; }
	public RandomNumberGenerator Rng { get; } = new RandomNumberGenerator();

	public override void _Ready()
	{
		Rng.Randomize();

		Behavior ??= new WanderBehavior();
		Behavior.Enter(this);
	}

	public override void _Process(double delta)
	{
		if (Domain != null)
		{
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
