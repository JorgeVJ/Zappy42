using Godot;

public class Leg
{
    private readonly QuadrupedController owner;
    private readonly Marker3D marker;
    private readonly RayCast3D ray;
    private readonly Marker3D stepTarget;

    private bool isStepping = false;

    public Leg(QuadrupedController owner, Marker3D marker, RayCast3D ray, Marker3D target)
    {
        this.owner = owner;
        this.marker = marker;
        this.ray = ray;
        stepTarget = target;
    }

    public void UpdateRaycast()
    {
        if (ray.IsColliding())
        {
            Vector3 hit = ray.GetCollisionPoint();
            stepTarget.GlobalPosition = hit;
        }
    }

    public bool NeedsStep()
    {
        if (isStepping)
            return false;

        float distance = marker.GlobalPosition.DistanceTo(stepTarget.GlobalPosition);
        return Mathf.Abs(distance) > owner.StepDistance;
    }

    public void Step()
    {
        isStepping = true;

        if (owner.StepOrder.Peek() == this)
            owner.StepOrder.Dequeue();

        owner.StepOrder.Enqueue(this);

        Vector3 start = marker.GlobalPosition;
        Vector3 end = stepTarget.GlobalPosition;
        Vector3 half = (start + end) * 0.5f;
        half += owner.GlobalTransform.Basis.Y * owner.StepHeight;

        var tween = owner.CreateTween();
        tween.TweenProperty(marker, "global_position", half, owner.StepDuration);
        tween.TweenProperty(marker, "global_position", end, owner.StepDuration);

        tween.TweenCallback(Callable.From(() =>
        {
            isStepping = false;
            owner.LegStepping = false;
        }));
    }
}
