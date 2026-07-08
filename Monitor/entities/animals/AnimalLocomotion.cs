using Godot;

/// <summary>
/// Steering procedural genérico para mover un Node3D hacia un objetivo con
/// aceleración/frenado suaves y giro gradual hacia el rumbo. Imita el patrón de
/// velocidad deseada, Lerp con damping y aplicar a posición, pero es autocontenido
/// y agnóstico del proyecto. Una instancia por animal.
/// </summary>
public class AnimalLocomotion
{
	public float MaxSpeed = 1.6f;

	/// <summary>Mayor = acelera/frena más rápido hacia la velocidad deseada.</summary>
	public float Damping = 2.5f;

	/// <summary>Distancia a la que se considera "llegado".</summary>
	public float ArrivalRadius = 0.4f;

	/// <summary>Rapidez del giro hacia el rumbo (slerp de orientación).</summary>
	public float TurnSpeed = 4.0f;

	/// <summary>
	/// Multiplicador transitorio de velocidad que fija el comportamiento activo cada
	/// frame (1 = crucero normal; mayor que 1 al huir). Permite acelerar el nado sin
	/// mutar <see cref="MaxSpeed"/>.
	/// </summary>
	public float SpeedScale = 1f;

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

	/// <summary>Detiene al animal: descarta el objetivo y anula la velocidad (para el estado quieto).</summary>
	public void Stop()
	{
		HasTarget = false;
		Velocity = Vector3.Zero;
		Arrived = true;
	}

	public void Tick(Node3D body, IAnimalDomain domain, double delta)
	{
		Arrived = false;
		if (!HasTarget)
			return;

		float dt = (float)delta;
		Vector3 pos = body.GlobalPosition;
		Vector3 toTarget = Target - pos;

		UpdateVelocity(toTarget, dt);

		Vector3 newPos = pos + Velocity * dt;
		if (domain != null)
			newPos = domain.ClampToValid(newPos);
		body.GlobalPosition = newPos;

		FaceVelocity(body, dt);
	}

	/// <summary>
	/// Actualiza <see cref="Velocity"/> hacia el objetivo actual: frena suavemente y
	/// marca llegada si está dentro de <see cref="ArrivalRadius"/>, o acelera hacia la
	/// velocidad deseada (con frenado de llegada progresivo) en caso contrario.
	/// </summary>
	private void UpdateVelocity(Vector3 toTarget, float dt)
	{
		float dist = toTarget.Length();
		float dampFactor = Mathf.Clamp(Damping * dt, 0f, 1f);

		if (dist < ArrivalRadius)
		{
			Velocity = Velocity.Lerp(Vector3.Zero, dampFactor);
			Arrived = true;
			HasTarget = false;
		}
		else
		{
			float arrival = Mathf.Min(1f, dist / (ArrivalRadius * 4f));
			Vector3 desiredVel = toTarget.Normalized() * MaxSpeed * SpeedScale * arrival;
			Velocity = Velocity.Lerp(desiredVel, dampFactor);
		}
	}

	/// <summary>
	/// Gira el cuerpo suavemente para mirar hacia su dirección de movimiento (incluye
	/// pitch para subir/bajar en 3D). A diferencia de Player, no hace snapping a 90°.
	/// </summary>
	private void FaceVelocity(Node3D body, float dt)
	{
		Vector3 vel = Velocity;
		if (vel.LengthSquared() < 0.0001f)
			return;

		Vector3 dir = vel.Normalized();
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
