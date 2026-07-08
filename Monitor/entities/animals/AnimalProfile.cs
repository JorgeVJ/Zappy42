using Godot;

/// <summary>
/// Perfil de una especie de animal: agrupa los parámetros de spawn y de tuning de comportamiento
/// que antes vivían sueltos en AnimalSystem, para poder añadir o afinar una especie desde el
/// inspector (o en código) sin tocar la lógica de generación. Data-driven: los valores viven aquí;
/// la composición del cerebro (qué behaviors combina) sigue en AnimalSystem.
/// </summary>
/// <remarks>
/// Es un único tipo compartido por las tres especies; cada una usa el subconjunto de campos que le
/// aplica (los peces la huida, las aves el vuelo, el zorro la caza) e ignora el resto.
/// </remarks>
public partial class AnimalProfile : Resource
{
	/// <summary>Rutas .glb entre las que cada spawn elige al azar. Vacío = no se genera la especie.</summary>
	[Export]
	public string[] Models = System.Array.Empty<string>();

	[Export(PropertyHint.Range, "0,20,1")]
	public int Count = 3;

	/// <summary>Velocidad de crucero (se inyecta en la locomoción).</summary>
	[Export]
	public float MaxSpeed = 1.2f;

	/// <summary>Radio de los saltos de paseo.</summary>
	[Export]
	public float WanderRadius = 6f;

	/// <summary>Huida (peces): distancia de cámara para huida máxima.</summary>
	[Export]
	public float FleeInner = 2f;

	/// <summary>Huida (peces): distancia a la que deja de huir.</summary>
	[Export]
	public float FleeOuter = 6f;

	/// <summary>Huida (peces): cuánto acelera el nado al huir.</summary>
	[Export]
	public float FleeSpeedScale = 4.2f;

	/// <summary>Vuelo (aves): altura mínima de vuelo sobre el suelo.</summary>
	[Export]
	public float MinFlyAltitude = 3f;

	/// <summary>Vuelo (aves): techo por encima del punto más alto del terreno.</summary>
	[Export]
	public float CeilingAltitude = 12f;

	/// <summary>Vuelo (aves): distancia de cámara a la que despega seguro.</summary>
	[Export]
	public float FlyInner = 4f;

	/// <summary>Vuelo (aves): distancia a la que deja de volar y aterriza.</summary>
	[Export]
	public float FlyOuter = 10f;

	/// <summary>Vuelo (aves): cuánto acelera al volar en crucero respecto a caminar.</summary>
	[Export]
	public float FlySpeedScale = 2.5f;

	/// <summary>Vuelo (aves): tiempo mínimo de vuelo tras despegar aunque la cámara se aleje.</summary>
	[Export]
	public float FlyDwellMin = 3f;

	[Export]
	public float FlyDwellMax = 7f;

	/// <summary>Vuelo (aves): velocidad del descenso al aterrizar (más calmada que el crucero).</summary>
	[Export]
	public float LandingSpeedScale = 1.5f;

	/// <summary>Correr (zorro): cuánto acelera en el estado Run respecto a caminar.</summary>
	[Export]
	public float RunSpeedScale = 2.5f;

	/// <summary>Caza (zorro): distancia horizontal a la que detecta y acecha una presa.</summary>
	[Export]
	public float HuntDetectRange = 6f;

	/// <summary>Caza (zorro): distancia horizontal a la que lanza el ataque.</summary>
	[Export]
	public float HuntAttackRange = 1.2f;

	/// <summary>Caza (zorro): peso del Score de caza (debe superar el paseo ≈1 para ganar el cerebro).</summary>
	[Export]
	public float HuntWeight = 4f;

	/// <summary>Caza (zorro): cuánto acelera al acechar respecto a caminar.</summary>
	[Export]
	public float HuntSpeedScale = 1.4f;

	/// <summary>Caza (zorro): segundos quieto tras capturar antes de retomar el paseo.</summary>
	[Export]
	public float HuntRecoverTime = 0.8f;

	/// <summary>Caza (zorro): altura máxima (respecto al zorro) a la que una presa es cazable.</summary>
	[Export]
	public float MaxPreyAltitude = 1.5f;
}
