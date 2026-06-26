using Godot;

// Steering procedural genérico para mover un Node3D hacia un objetivo con
// aceleración/frenado suaves y giro gradual hacia el rumbo. Imita el patrón de
// CrowdSystem (velocidad deseada → Lerp con damping → aplicar a posición) pero
// es autocontenido y agnóstico del proyecto. Una instancia por animal.
public class AnimalLocomotion
{
	public float MaxSpeed = 1.6f;
	public float Damping = 2.5f;      // mayor = acelera/frena más rápido hacia la velocidad deseada
	public float ArrivalRadius = 0.4f; // distancia a la que se considera "llegado"
	public float TurnSpeed = 4.0f;     // rapidez del giro hacia el rumbo (slerp de orientación)

	public Vector3 Velocity { get; private set; }
	public Vector3 Target { get; private set; }
	public bool HasTarget { get; private set; }
	public bool Arrived { get; private set; }
	public float CurrentSpeed => Velocity.Length();

	public void SetTarget(Vector3 target)
	{
		Target = target;
		HasTarget = true;
		Arrived = false;
	}

	public void Tick(Node3D body, IAnimalDomain domain, double delta)
	{
		Arrived = false;
		if (!HasTarget)
			return;

		float dt = (float)delta;
		Vector3 pos = body.GlobalPosition;
		Vector3 toTarget = Target - pos;
		float dist = toTarget.Length();

		if (dist < ArrivalRadius)
		{
			// Llegada: frenar suavemente y avisar para que el comportamiento re-encadene.
			Velocity = Velocity.Lerp(Vector3.Zero, Mathf.Clamp(Damping * dt, 0f, 1f));
			Arrived = true;
			HasTarget = false;
		}
		else
		{
			// Velocidad deseada hacia el objetivo, con frenado de llegada cuando se acerca.
			float arrival = Mathf.Min(1f, dist / (ArrivalRadius * 4f));
			Vector3 desiredVel = toTarget.Normalized() * MaxSpeed * arrival;
			Velocity = Velocity.Lerp(desiredVel, Mathf.Clamp(Damping * dt, 0f, 1f));
		}

		Vector3 newPos = pos + Velocity * dt;
		if (domain != null)
			newPos = domain.ClampToValid(newPos);
		body.GlobalPosition = newPos;

		FaceVelocity(body, dt);
	}

	// Gira el cuerpo suavemente para mirar hacia su dirección de movimiento (incluye
	// pitch para subir/bajar en 3D). A diferencia de Player, no hace snapping a 90°.
	private void FaceVelocity(Node3D body, float dt)
	{
		Vector3 vel = Velocity;
		if (vel.LengthSquared() < 0.0001f)
			return;

		Vector3 dir = vel.Normalized();
		// Evita el caso degenerado cuando el rumbo es casi vertical.
		Vector3 up = Mathf.Abs(dir.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;

		Basis targetBasis = Basis.LookingAt(dir, up);
		Quaternion current = body.GlobalBasis.GetRotationQuaternion();
		Quaternion target = targetBasis.GetRotationQuaternion();
		Quaternion next = current.Slerp(target, Mathf.Clamp(TurnSpeed * dt, 0f, 1f));

		Transform3D xform = body.GlobalTransform;
		xform.Basis = new Basis(next);
		body.GlobalTransform = xform;
	}
}
