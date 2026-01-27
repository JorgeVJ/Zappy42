using Godot;
using System;

public partial class Camera3d : Camera3D
{
	[Export] public float MoveSpeed = 10.0f;
	[Export] public float FastMultiplier = 4.0f;
	[Export] public float MouseSensitivity = 0.002f;

	private float _yaw = 0f;
	private float _pitch = 0f;
	private bool _mouseCaptured = false;

	public override void _Ready()
	{
		// Inicializar rotación desde la cámara actual
		Vector3 rot = Rotation;
		_pitch = rot.X;
		_yaw = rot.Y;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		// Click derecho para capturar/liberar ratón
		if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right && mb.Pressed)
		{
			_mouseCaptured = !_mouseCaptured;
			Input.MouseMode = _mouseCaptured ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
		}

		// Rotación con el ratón
		if (_mouseCaptured && e is InputEventMouseMotion mm)
		{
			_yaw -= mm.Relative.X * MouseSensitivity;
			_pitch -= mm.Relative.Y * MouseSensitivity;

			// Clamp del pitch para que no se dé la vuelta
			_pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

			Rotation = new Vector3(_pitch, _yaw, 0);
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// Shift para ir rápido
		float speed = MoveSpeed;
		if (Input.IsKeyPressed(Key.Shift))
			speed *= FastMultiplier;

		Vector3 dir = Vector3.Zero;

		// WASD
		if (Input.IsKeyPressed(Key.W)) dir += -Transform.Basis.Z;
		if (Input.IsKeyPressed(Key.S)) dir += Transform.Basis.Z;
		if (Input.IsKeyPressed(Key.A)) dir += -Transform.Basis.X;
		if (Input.IsKeyPressed(Key.D)) dir += Transform.Basis.X;

		// Subir/Bajar (Space / Ctrl)
		if (Input.IsKeyPressed(Key.Space)) dir += Transform.Basis.Y;
		if (Input.IsKeyPressed(Key.Ctrl)) dir += -Transform.Basis.Y;

		if (dir != Vector3.Zero)
		{
			dir = dir.Normalized();
			GlobalPosition += dir * speed * dt;
		}
	}
}
