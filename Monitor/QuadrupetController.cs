using Godot;
using System;
using System.Collections.Generic;

public partial class QuadrupedController : Node3D
{
    [Export] public float StepDistance = 0.3f;
    [Export] public float StepHeight = 0.15f;
    [Export] public float StepRayLength = 2.0f;
    [Export] public float StepDuration = 0.2f;

    private Skeleton3D _skeleton;
    private Node3D _ikRoot;

    private List<Leg> _legs = new();

    private struct LegDefinition
    {
        public string Name;
        public string RootBone;
        public string TipBone;
        public Vector3 Offset;

        public LegDefinition(string name, string root, string tip, Vector3 offset)
        {
            Name = name;
            RootBone = root;
            TipBone = tip;
            Offset = offset;
        }
    }

    private List<LegDefinition> _definitions = new()
    {
        new LegDefinition("FrontLeft",  "FrontArm.L",  "FrontArm2.L.001",  new Vector3(-0.7f, 0,  0.8f)),
        new LegDefinition("FrontRight", "FrontArm.R",  "FrontArm2.R.001",  new Vector3( 0.7f, 0,  0.8f)),
        new LegDefinition("BackLeft",   "BackLeg.L",   "BackLeg2.L.001",   new Vector3(-0.7f, 0, -0.8f)),
        new LegDefinition("BackRight",  "BackLeg.R",   "BackLeg2.R.001",   new Vector3( 0.7f, 0, -0.8f)),
    };

    public override void _Ready()
    {
        _skeleton = FindChild("Skeleton3D", true, false) as Skeleton3D;
        if (_skeleton == null)
        {
            GD.PrintErr("Skeleton3D not found.");
            return;
        }

        _ikRoot = new Node3D();
        _ikRoot.Name = "IK_ROOT";
        AddChild(_ikRoot);

        foreach (var def in _definitions)
        {
            var leg = CreateLeg(def);
            _legs.Add(leg);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var leg in _legs)
            leg.UpdateRaycast();
    }

    public override void _Process(double delta)
    {
        foreach (var leg in _legs)
            leg.UpdateStepLogic();
    }

    private Leg CreateLeg(LegDefinition def)
    {
        Node3D legRoot = new Node3D();
        legRoot.Name = def.Name;
        legRoot.Position = def.Offset;
        _ikRoot.AddChild(legRoot);

        Marker3D marker = new Marker3D();
        marker.TopLevel = true;
        legRoot.AddChild(marker);

        RayCast3D ray = new RayCast3D();
        ray.TargetPosition = new Vector3(0, -StepRayLength, 0);
        ray.Enabled = true;
        ray.CollideWithAreas = true;
        legRoot.AddChild(ray);

        Marker3D target = new Marker3D();
        ray.AddChild(target);

        SkeletonIK3D ik = new SkeletonIK3D();
        ik.RootBone = def.RootBone;
        ik.TipBone = def.TipBone;
        ik.TargetNode = marker.GetPath();
        ik.UseMagnet = true;
        ik.Magnet = new Vector3(0, 0.5f, 0);
        _skeleton.AddChild(ik);
        ik.Start();

        return new Leg(this, marker, ray, target);
    }

    // =========================
    // CLASE INTERNA LEG
    // =========================

    private class Leg
    {
        private QuadrupedController _owner;
        private Marker3D _marker;
        private RayCast3D _ray;
        private Marker3D _target;

        private bool _isStepping = false;

        public Leg(QuadrupedController owner, Marker3D marker, RayCast3D ray, Marker3D target)
        {
            _owner = owner;
            _marker = marker;
            _ray = ray;
            _target = target;
        }

        public void UpdateRaycast()
        {
            if (_ray.IsColliding())
            {
                Vector3 hit = _ray.GetCollisionPoint();
                _target.GlobalPosition = hit;
            }
        }

        public void UpdateStepLogic()
        {
            if (_isStepping)
                return;

            float distance = _marker.GlobalPosition.DistanceTo(_target.GlobalPosition);

            if (Mathf.Abs(distance) > _owner.StepDistance)
                Step();
        }

        private void Step()
        {
            _isStepping = true;

            Vector3 start = _marker.GlobalPosition;
            Vector3 end = _target.GlobalPosition;
            Vector3 half = (start + end) * 0.5f;
            half += _owner.GlobalTransform.Basis.Y * _owner.StepHeight;

            var tween = _owner.CreateTween();
            tween.TweenProperty(_marker, "global_position", half, _owner.StepDuration);
            tween.TweenProperty(_marker, "global_position", end, _owner.StepDuration);
            tween.TweenCallback(Callable.From(() => _isStepping = false));
        }
    }
}