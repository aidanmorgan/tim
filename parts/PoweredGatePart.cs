using Godot;
using CuriousContraptions.Presentation;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Electric retracting shutter. Shared rigid-body motion drives the visible blade.</summary>
public partial class PoweredGatePart : MachinePart, ITubePart
{
    public static readonly JointSlot BladeGuide=new();
    public static readonly BodySlot BladeBody=new(p=>SceneGeometryAdapter.CaptureRigidPose(((PoweredGatePart)p)._blade.Transform),
        p=>((PoweredGatePart)p!)._motion.InitialDynamics,BodyQueryPolicy.Include,
        p=>p!.InitialContactMaterial,p=>[new(((PoweredGatePart)p!)._blade,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,global::CuriousContraptions.Geometry.RigidPose.Identity))]);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>[_motion.Declare(this,BladeBody,BladeGuide)];
    public const float Stroke = 1.55f;
    private readonly SlidingBlade _motion=new(BladeHalf,Stroke);
    public float Opening=>(float)ReadAxialMotion(BladeGuide).Coordinate;
    public float BladeSpeed=>(float)ReadAxialMotion(BladeGuide).Speed;
    public GateState State=>_motion.Classify(ReadAxialMotion(BladeGuide),HasElectricalPower(SocketId.PowerIn));
    private MeshInstance3D _blade = null!;
    private StandardMaterial3D _indicator = null!;
    private static readonly Vector3 BladeHalf = new(.06f, .72f, .72f);
    public override Physics.ContactMaterial InitialContactMaterial => new(.1f,.1,.3);
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(0, 1.65f, .92f))];
    public IEnumerable<TubeMouth> Mouths =>
    [
        new(TubeMouthId.Start, Vector3.Left * .49f, Vector3.Left, PipePart.BoreRadius),
        new(TubeMouthId.End, Vector3.Right * .49f, Vector3.Right, PipePart.BoreRadius)
    ];

    protected override void Build()
    {
        PickRadius = 1.5f;
        PipeArt.Cylinder(Visual, Transform3D.Identity, .4f, PipePart.BoreRadius, .70f, new(.40f,.72f,.79f,.16f), false);
        Tubes.Add(new(Transform3D.Identity, .4f, PipePart.BoreRadius, .70f, false));
        foreach (var x in new[] { -.4f, .4f })
        {
            var pose = new Transform3D(Basis.Identity, new(x,0,0));
            PipeArt.Cylinder(Visual, pose, .09f, PipePart.BoreRadius, .78f, new("#fff8e9"), true);
            Tubes.Add(new(pose, .09f, PipePart.BoreRadius, .78f, true));
        }
        AddBox(new(0,1.65f,0), new(.8f,.65f,1.8f), new("#293954"));
        foreach (var z in new[] { -.82f, .82f })
            AddBox(new(0,.65f,z), new(.24f,1.7f,.12f), new("#fff8e9"));
        Boxes.Add(new(Vector3.Zero, BladeHalf, BladeBody));
        _blade = PartArt.Box(Visual, BladeHalf * 2, new("#f7cb52"));
        PartArt.Sphere(Visual, .09f, new("#e8b764"), new(0,1.65f,.92f));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual, .07f, new("#556573"),
            new(.42f,1.65f,.55f)).MaterialOverride;
    }


    public override void PreparePhysics(MachineWorld world,float delta)
        =>_motion.Prepare(world,this,BladeGuide,HasElectricalPower(SocketId.PowerIn),delta);

    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var powered=HasElectricalPower(SocketId.PowerIn);
        Active=powered;
        _indicator.AlbedoColor=State==GateState.Blocked?new("#e8b764"):powered?new("#f7cb52"):new("#556573");
        if(powered)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
}
