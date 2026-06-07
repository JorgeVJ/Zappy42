using Godot;

public partial class CameraFollowBehavior : Node
{
    [Export] public float FollowDistance  = 6.0f;
    [Export] public float FollowHeight    = 4.0f;
    [Export] public float FollowLerpSpeed = 4.0f;

    private Camera _camera;
    private Node3D _target;

    public override void _Ready()
    {
        _camera = GetParent<Camera>();
    }

    public void StartFollowing(Node3D target) => _target = target;
    public void StopFollowing()               => _target = null;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb &&
            mb.ButtonIndex == MouseButton.Right && mb.Pressed)
        {
            _target = null;
        }
    }

    public override void _Process(double delta)
    {
        if (_target == null) return;

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.S) ||
            Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.D) ||
            Input.IsKeyPressed(Key.E) || Input.IsKeyPressed(Key.Q))
        {
            _target = null;
            return;
        }

        float dt = (float)delta;

        Vector3 desired = _target.GlobalPosition + new Vector3(0, FollowHeight, FollowDistance);
        _camera.GlobalPosition = _camera.GlobalPosition.Lerp(desired, FollowLerpSpeed * dt);

        _camera.LookAt(_target.GlobalPosition + Vector3.Up * 0.5f, Vector3.Up);
        _camera.SyncEulerAngles();
    }
}
