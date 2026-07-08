using Godot;

/// <summary>
/// Blackboard por animal: percepción cacheada una sola vez por frame + memoria compartida
/// entre comportamientos. Evita que cada behavior vuelva a percibir por su cuenta (cámara,
/// presas) en cada Score, y ofrece un lugar común para el objetivo actual y otros datos que
/// varios comportamientos comparten.
/// </summary>
/// <remarks>
/// Autocontenido: solo usa tipos de Godot y Animal. La percepción de presas es opcional (solo
/// si se configura <see cref="PreyGroup"/>), de modo que los animales que no cazan no pagan el escaneo.
/// </remarks>
public class AnimalContext
{
	/// <summary>Distancia a la cámara activa; PositiveInfinity si no hay cámara.</summary>
	public float CameraDistance { get; private set; } = float.PositiveInfinity;

	/// <summary>Posición mundial de la cámara activa en el último refresco (válida si <see cref="HasCamera"/>).</summary>
	public Vector3 CameraPosition { get; private set; }

	/// <summary>¿Había cámara activa en el último refresco?</summary>
	public bool HasCamera { get; private set; }

	/// <summary>Presa válida más cercana dentro del rango (o null); solo se rellena si <see cref="PreyGroup"/> está fijado.</summary>
	public Node3D NearestPrey { get; private set; }

	/// <summary>Objetivo actual compartido entre comportamientos (p. ej. la presa durante la caza).</summary>
	public Node3D Target { get; set; }

	/// <summary>Grupo de Godot cuyas instancias son presas; si es null/vacío, no se escanea.</summary>
	public string PreyGroup;

	/// <summary>Radio (horizontal) de detección de presas.</summary>
	public float PreyDetectRange = 6f;

	/// <summary>Altura máxima (respecto al animal) a la que una presa se considera alcanzable.</summary>
	public float MaxPreyAltitude = 1.5f;

	/// <summary>Recalcula la percepción cacheada para este frame (cámara y presa más cercana).</summary>
	public void Refresh(Animal animal)
	{
		Camera3D cam = animal.GetViewport()?.GetCamera3D();
		HasCamera = cam != null;
		CameraPosition = HasCamera ? cam.GlobalPosition : Vector3.Zero;
		CameraDistance = HasCamera ? animal.GlobalPosition.DistanceTo(CameraPosition) : float.PositiveInfinity;

		NearestPrey = string.IsNullOrEmpty(PreyGroup) ? null : FindNearestPrey(animal);
	}

	/// <summary>Presa válida más cercana en <see cref="PreyGroup"/> dentro del rango y a altura alcanzable.</summary>
	private Node3D FindNearestPrey(Animal animal)
	{
		SceneTree tree = animal.GetTree();
		if (tree == null)
			return null;

		Vector3 from = animal.GlobalPosition;
		Node3D best = null;
		float bestDist = PreyDetectRange;

		foreach (Node node in tree.GetNodesInGroup(PreyGroup))
		{
			if (node is not Node3D prey || !GodotObject.IsInstanceValid(prey))
				continue;
			if (prey.GlobalPosition.Y - from.Y > MaxPreyAltitude)
				continue;

			float dist = HorizontalDistance(from, prey.GlobalPosition);
			if (dist < bestDist)
			{
				bestDist = dist;
				best = prey;
			}
		}

		return best;
	}

	/// <summary>Distancia en el plano horizontal (ignora la altura), como usa la caza terrestre.</summary>
	public static float HorizontalDistance(Vector3 a, Vector3 b)
	{
		return new Vector2(a.X, a.Z).DistanceTo(new Vector2(b.X, b.Z));
	}
}
