using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum MotorParameter { Speed, Torque }

public partial class MotorPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<MotorParameter>(fields);
    public static readonly JointSlot ShaftJoint=new();
    public static readonly ScalarInputSlot SpeedInput=new();
    public override IReadOnlyList<SceneScalarInputDeclaration> ScalarInputs=>
        [new(new(this,SpeedInput),ReadParameter(MotorParameter.Speed),0,20)];
    public static readonly BodySlot ShaftBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((MotorPart)p)._rotor.Transform),
        _=>new(PhysicsMotionType.Dynamic,RotorMass,new(TransverseInertia,TransverseInertia,AxialInertia),default,default),
        BodyQueryPolicy.ExcludeFromStaticQueries,p=>p!.InitialContactMaterial,
        p=>[new(((MotorPart)p!)._rotor,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,global::CuriousContraptions.Geometry.RigidPose.Identity))]);
    private const double RotorMass=.5, RotorRadius=.3, RotorWidth=.13;
    private const double AxialInertia=RotorMass*RotorRadius*RotorRadius/2;
    private const double TransverseInertia=RotorMass*(3*RotorRadius*RotorRadius+RotorWidth*RotorWidth)/12;
    private static readonly Vector3 RotorCenter=new(0,0,.54f);
    public double SuppliedWork=>GetParent() is MachineWorld {HasPhysicsState:true} world?
        world.Physics.MotorTotal(world.CurrentJoint(new(this,ShaftJoint)).Id).SuppliedWork:0;
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        new SceneFrameJoint(new(this,ShaftJoint),FrameJointKind.Hinge,new(this,ShaftBody),new(default,RigidRotation.Identity),
            new(this,RootBody),new(SceneGeometryAdapter.CaptureVector(RotorCenter),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both)
    ];
    private Node3D _rotor = null!, _spin = null!;
    public override IReadOnlyList<SceneAngularVelocityAnimation> AngularVelocityAnimations=>
        [new(_spin,new(this,ShaftBody),new(this,RootBody),AnimationRotationAxis.Z,AnimationRotationAxis.Z,
            AnimationDirection.Forward,AnimationClock.Simulation)];
    private MeshInstance3D _indicator = null!;
    private static readonly Color InactiveColour=new("#556573"),ActiveColour=new("#f7cb52");
    private static readonly AnimationDefinition IndicatorTransition=new(0,1,.1,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
        [new(_indicator,IndicatorTransition,InactiveColour,ActiveColour,SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public float ShaftSpeed=>(float)-ReadAxialMotion(ShaftJoint).Speed;
    public float ShaftAngle=>(float)-ReadAxialMotion(ShaftJoint).Coordinate;
    public float ShaftTravel=>(float)ReadAngularTravel(ShaftJoint).Distance;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(-.55f, 0, 0)),
        new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output, new(0, 0, .65f))
    ];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.Drive,ShaftJoint,-1)];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var speed = parameters.Read(MotorParameter.Speed);
        var torque = parameters.Read(MotorParameter.Torque);
        if (!float.IsFinite(speed) || speed < 0 || speed > 20)
            throw new ArgumentException("Motor speed must be between 0 and 20.");
        if (!float.IsFinite(torque) || torque <= 0 || torque > 100)
            throw new ArgumentException("Motor torque must be greater than zero and at most 100.");
    }
    protected override void Build()
    {

        PickRadius = .9f;
        AddBox(new(0, -.43f, 0), new(1.25f, .16f, 1), new("#293954"));
        AddBox(Vector3.Zero, new(1, .8f, .85f), Definition.Color);
        var housing = PartArt.Cylinder(Visual, .48f, .86f, Definition.Color);
        housing.RotationDegrees = new(90, 0, 0);
        PartArt.Sphere(Visual, .1f, new("#f7cb52"), new(-.55f, 0, 0));
        _rotor = new Node3D { Name = "Shaft", Position = RotorCenter };
        Visual.AddChild(_rotor);
        var vertices=new List<CollisionVector>();
        for(var i=0;i<24;i++)
        foreach(var sign in new[]{-1,1})
        {
            var angle=Math.Tau*i/24;
            vertices.Add(new(Math.Cos(angle)*RotorRadius,Math.Sin(angle)*RotorRadius,sign*RotorWidth/2));
        }
        ConvexShapes.Add(new(new(new ConvexHull(vertices.ToArray()),AffineTransform.Identity),ShaftBody));
        var wheel = PartArt.Cylinder(_rotor, .3f, .13f, new("#fff8e9"));
        wheel.RotationDegrees = new(90, 0, 0);
        // The functional wheel retains the shaft pose; only its decorative index has a cosmetic phase.
        _spin = new Node3D { Position = RotorCenter };
        Visual.AddChild(_spin);
        PartArt.Box(_spin, new(.48f, .07f, .04f), new("#f7cb52"), new(0, 0, .08f));
        _indicator = PartArt.Sphere(Visual, .075f, InactiveColour, new(.32f, .28f, .45f));
    }
    public override void PreparePhysics(MachineWorld world, float delta)
    {
        Active = HasElectricalPower(SocketId.PowerIn);
        var target=world.ReadScalarInput(new(this,SpeedInput));
        var torque=Active?(double)ReadParameter(MotorParameter.Torque):0;
        // Clockwise-positive socket motion is negative around the hinge's +Z.
        world.DriveMotor(new(this,ShaftJoint),-target,torque,torque*target*delta,torque*target);
        if (Active) world.Events.TryAdd(new MachineEvent(MachineEventKind.Powered, Uid), world.Ticks);
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        if(ReadAngularTravel(ShaftJoint).Distance>=Math.Tau) world.Events.TryAdd(new(MachineEventKind.Turned,Uid),world.Ticks);
    }
}
