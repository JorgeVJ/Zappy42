using Godot;

// Base genérica de un animal decorativo móvil. Reúne las tres capas del sistema:
//   - Domain:     dónde puede moverse (espacio navegable).
//   - Locomotion: cómo se desplaza/gira hacia un objetivo (steering suave).
//   - Behavior:   qué hace a lo largo del tiempo (hoy: pasear).
// Cada frame ejecuta comportamiento → locomoción → hook de animación. Las
// subclases (Fish, y en el futuro aves/terrestres) añaden su animación concreta
// y reaccionan a la velocidad en OnLocomotionUpdate.
//
// Autocontenido: no referencia Terrain/Connection ni ningún tipo del proyecto.
public partial class Animal : Node3D
{
	public IAnimalDomain Domain { get; set; }
	public AnimalLocomotion Locomotion { get; protected set; } = new AnimalLocomotion();
	public IAnimalBehavior Behavior { get; set; }
	public RandomNumberGenerator Rng { get; } = new RandomNumberGenerator();

	public override void _Ready()
	{
		Rng.Randomize();

		// Comportamiento por defecto: pasear. En el futuro, un UtilityBrain
		// sustituirá este comportamiento único por una selección entre varios.
		Behavior ??= new WanderBehavior();
		Behavior.Enter(this);
	}

	public override void _Process(double delta)
	{
		// Sin dominio no hay paseo posible; las subclases pueden seguir animando.
		if (Domain != null)
		{
			Behavior?.Tick(this, delta);
			Locomotion?.Tick(this, Domain, delta);
		}

		OnLocomotionUpdate(Locomotion?.CurrentSpeed ?? 0f);
	}

	// Hook para que cada especie ajuste su animación según la velocidad actual.
	protected virtual void OnLocomotionUpdate(float speed) { }
}
