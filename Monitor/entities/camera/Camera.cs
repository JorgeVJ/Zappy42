using Godot;
using System;
using System.Linq;

public partial class Camera : Camera3D
{
	[Export] public float MoveSpeed = 10.0f;
	[Export] public float FastMultiplier = 4.0f;
	[Export] public float MouseSensitivity = 0.002f;

	private float _yaw = 0f;
	private float _pitch = 0f;
	private bool _mouseCaptured = false;
	private CameraFollowBehavior _followBehavior;

	[Signal]
	public delegate void OnLeftClickEventHandler(GodotObject collider, Vector3 position);

	public override void _Ready()
	{
		Vector3 rot = Rotation;
		_pitch = rot.X;
		_yaw = rot.Y;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
			{
				_mouseCaptured = !_mouseCaptured;
				Input.MouseMode = _mouseCaptured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
			}
			else if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
			{
				HandleClick();
			}
		}

		if (_mouseCaptured && e is InputEventMouseMotion mm)
		{
			_yaw -= mm.Relative.X * MouseSensitivity;
			_pitch -= mm.Relative.Y * MouseSensitivity;

			// Clamp to avoid turning
			_pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

			Rotation = new Vector3(_pitch, _yaw, 0);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// El comportamiento de seguimiento (si existe) se añade como hijo
		// dinámicamente desde Connection, así que se busca de forma perezosa.
		if (_followBehavior == null)
			_followBehavior = GetChildren().OfType<CameraFollowBehavior>().FirstOrDefault();

		// Mientras la cámara está "lockeada" sobre un objetivo, el movimiento
		// libre WASD/QE se desactiva: CameraFollowBehavior se encarga de
		// orbitar/zoomear sin que ambos comportamientos compitan por GlobalPosition.
		if (_followBehavior != null && _followBehavior.IsLocked)
			return;

        // Shift to move faster
        float speed = MoveSpeed;
		if (Input.IsKeyPressed(Key.Shift))
			speed *= FastMultiplier;

		Vector3 dir = Vector3.Zero;

		// WASD
		if (Input.IsKeyPressed(Key.W)) dir += -Transform.Basis.Z;
		if (Input.IsKeyPressed(Key.S)) dir += Transform.Basis.Z;
		if (Input.IsKeyPressed(Key.A)) dir += -Transform.Basis.X;
		if (Input.IsKeyPressed(Key.D)) dir += Transform.Basis.X;

        // Up and down
        if (Input.IsKeyPressed(Key.E)) dir += Transform.Basis.Y;
		if (Input.IsKeyPressed(Key.Q)) dir += -Transform.Basis.Y;

		if (dir != Vector3.Zero)
		{
			dir = dir.Normalized();
			GlobalPosition += dir * speed * dt;
		}
	}

	public void SyncEulerAngles()
	{
		_pitch = Rotation.X;
		_yaw   = Rotation.Y;
	}

	private void HandleClick()
	{
		var mousePos = GetViewport().GetMousePosition();

		Vector3 origin = ProjectRayOrigin(mousePos);
		Vector3 dir = ProjectRayNormal(mousePos);

		var space = GetWorld3D().DirectSpaceState;

		var query = PhysicsRayQueryParameters3D.Create(
			origin,
			origin + dir * 1000
		);

		var result = space.IntersectRay(query);

		if (result.Count == 0)
			return;

        Vector3 position = (Vector3)result["position"];
        GodotObject collider = result["collider"].AsGodotObject();

		EmitSignal(nameof(OnLeftClick), collider, position);
	}
}
