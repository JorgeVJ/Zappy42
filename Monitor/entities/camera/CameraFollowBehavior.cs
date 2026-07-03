using Godot;

public partial class CameraFollowBehavior : Node
{
    [Export]
    public float FollowDistance = 6.0f;

    [Export]
    public float FollowHeight = 4.0f;

    [Export]
    public float FollowLerpSpeed = 4.0f;

    [Export]
    public float MinOrbitDistance = 1.5f;

    [Export]
    public float MaxOrbitDistance = 20.0f;

    [Export]
    public float ZoomSpeed = 0.75f;

    /// <summary>Velocidad de orbitado, en radianes por segundo.</summary>
    [Export]
    public float OrbitSpeed = 1.5f;

    /// <summary>Límite de pitch, ~80 grados.</summary>
    private const float MaxPitch = Mathf.Pi * 80f / 180f;

    public bool IsLocked => _target != null;

    private Camera _camera;
    private Node3D _target;

    private float _orbitDistance;
    private float _orbitYaw;
    private float _orbitPitch;

    public override void _Ready()
    {
        _camera = GetParent<Camera>();
    }

    /// <remarks>
    /// Deriva la distancia/ángulos orbitales iniciales a partir del offset fijo actual
    /// (FollowHeight, FollowDistance), de forma que el primer frame no produzca un salto
    /// brusco de cámara.
    /// </remarks>
    public void StartFollowing(Node3D target)
    {
        _target = target;

        Vector3 offset = new Vector3(0, FollowHeight, FollowDistance);
        _orbitDistance = Mathf.Clamp(offset.Length(), MinOrbitDistance, MaxOrbitDistance);

        _orbitYaw = Mathf.Atan2(offset.X, offset.Z);

        float horizontalDist = new Vector2(offset.X, offset.Z).Length();
        _orbitPitch = Mathf.Atan2(offset.Y, horizontalDist);
        _orbitPitch = Mathf.Clamp(_orbitPitch, -MaxPitch, MaxPitch);
    }

    public void StopFollowing() => _target = null;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                _target = null;
                return;
            }

            if (_target == null) return;

            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                _orbitDistance = Mathf.Clamp(_orbitDistance - ZoomSpeed, MinOrbitDistance, MaxOrbitDistance);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                _orbitDistance = Mathf.Clamp(_orbitDistance + ZoomSpeed, MinOrbitDistance, MaxOrbitDistance);
            }
        }
    }

    /// <remarks>
    /// WASD orbita la cámara alrededor del objetivo en vez de romper el lock. La
    /// conversión de coordenadas esféricas a cartesianas usa yaw medido desde +Z
    /// hacia +X, y pitch como elevación respecto al plano horizontal.
    /// </remarks>
    public override void _Process(double delta)
    {
        if (_target == null) return;

        float dt = (float)delta;

        if (Input.IsKeyPressed(Key.A)) _orbitYaw += OrbitSpeed * dt;
        if (Input.IsKeyPressed(Key.D)) _orbitYaw -= OrbitSpeed * dt;
        if (Input.IsKeyPressed(Key.W)) _orbitPitch += OrbitSpeed * dt;
        if (Input.IsKeyPressed(Key.S)) _orbitPitch -= OrbitSpeed * dt;

        _orbitPitch = Mathf.Clamp(_orbitPitch, -MaxPitch, MaxPitch);

        float cosPitch = Mathf.Cos(_orbitPitch);
        Vector3 offset = new Vector3(
            _orbitDistance * cosPitch * Mathf.Sin(_orbitYaw),
            _orbitDistance * Mathf.Sin(_orbitPitch),
            _orbitDistance * cosPitch * Mathf.Cos(_orbitYaw)
        );

        Vector3 desired = _target.GlobalPosition + offset;
        _camera.GlobalPosition = _camera.GlobalPosition.Lerp(desired, FollowLerpSpeed * dt);

        _camera.LookAt(_target.GlobalPosition + Vector3.Up * 0.5f, Vector3.Up);
        _camera.SyncEulerAngles();
    }
}
