using Godot;
using System.Collections.Generic;

public partial class QuadrupedController : Node3D
{
	[Export] public float StepDistance = 0.1f;
	[Export] public float StepHeight = 0.15f;
	[Export] public float StepRayLength = 2.0f;
	[Export] public float StepDuration = 0.05f;

	private Skeleton3D skeleton;
	private Node3D ikRoot;

	private List<Leg> legs = new();

	public bool LegStepping { get; set; } = false;
	public Queue<Leg> StepOrder = new();

	private List<LegDefinition> definitions = new()
	{
		new LegDefinition("FrontLeftArm",  "FrontArm.L",  "FrontArm2.L.001",  new Vector3(-0.7f, 0, 0.4f)),
		new LegDefinition("FrontRightArm", "FrontArm.R",  "FrontArm2.R.001",  new Vector3( 0.7f, 0, 0.4f)),
		new LegDefinition("BackLeftArm",   "BackArm.L",   "BackArm2.L.001",   new Vector3(-0.7f, 0, -0.4f)),
		new LegDefinition("BackRightArm",  "BackArm.R",   "BackArm2.R.001",   new Vector3( 0.7f, 0, -0.4f)),
	};

	public override void _Ready()
	{
		GD.Print("\n\n----- QuadrupedController -----");
		skeleton = GetParent() as Skeleton3D;
		if (skeleton == null)
		{
			GD.PrintErr("QuadrupedController must be a direct child of Skeleton3D.");
			return;
		}

		ikRoot = new Node3D();
		ikRoot.Name = "IK_ROOT";
		AddChild(ikRoot);

		foreach (var def in definitions)
		{
			if (skeleton.FindBone(def.RootBone) == -1)
			{
				GD.PrintErr($"Root bone '{def.RootBone}' not found in skeleton.");
				continue;
			}
			if (skeleton.FindBone(def.TipBone) == -1)
			{
				GD.PrintErr($"Tip bone '{def.TipBone}' not found in skeleton.");
				continue;
			}
			var leg = CreateLeg(def);
			StepOrder.Enqueue(leg);
			legs.Add(leg);
		}
		GD.Print("----- QuadrupedController -----\n\n");
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (var leg in legs)
		{
			leg.UpdateRaycast();
		}
	}

	public override void _Process(double delta)
	{
		if (LegStepping || StepOrder.Count == 0)
			return;

		Leg leg = StepOrder.Peek();

		if (leg.NeedsStep())
		{
			LegStepping = true;
			leg.Step();
		}
	}

	private MeshInstance3D CreateDebugSphere(Color color, float size = 0.05f)
	{
		var mesh = new MeshInstance3D();

		var sphere = new SphereMesh();
		sphere.Radius = size;
		sphere.Height = size * 2;

		mesh.Mesh = sphere;

		var mat = new StandardMaterial3D();
		mat.AlbedoColor = color;
		mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

		mesh.MaterialOverride = mat;

		return mesh;
	}

	private Leg CreateLeg(LegDefinition def)
	{
		Node3D legRoot = new Node3D();
		legRoot.Name = def.Name;
		legRoot.Position = def.Offset;
		ikRoot.AddChild(legRoot);

		Marker3D marker = new Marker3D();
		marker.TopLevel = true;
		legRoot.AddChild(marker);
		/// Debug sphere to visualize the marker position
		var markerDebug = CreateDebugSphere(Colors.Green);
		marker.AddChild(markerDebug);

		RayCast3D ray = new RayCast3D();
		ray.TargetPosition = new Vector3(0, -StepRayLength, 0);
		ray.Enabled = true;
		ray.CollideWithAreas = true;
		legRoot.AddChild(ray);

		Marker3D stepTarget = new Marker3D();
		stepTarget.Position = def.Offset;
		ray.AddChild(stepTarget);
		/// Debug sphere to visualize the step target position
		var stepDebug = CreateDebugSphere(Colors.Red);
		stepTarget.AddChild(stepDebug);

		SkeletonIK3D ik = new SkeletonIK3D();
		ik.RootBone = def.RootBone;
		ik.TipBone = def.TipBone;
		ik.TargetNode = marker.GetPath();
		ik.UseMagnet = true;
		ik.Magnet = new Vector3(def.Offset.X, 0.5f, 0);
		skeleton.AddChild(ik);
		ik.Start();

		return new Leg(this, marker, ray, stepTarget);
	}
}
