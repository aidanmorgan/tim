using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum PusherPhase { Holding, Extending, Retracting, Blocked, Conflict, Unpowered }
public enum PusherParameter { Stroke, Speed, Acceleration, Force }

/// <summary>Ideal self-locking electric linear servo. Supply is binary, not a battery-energy simulation.</summary>
public partial class LinearPusherPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<PusherParameter>(fields);
    public const string CatalogId="linear_pusher";
    public const float HeadRadius=.25f;
    public const float RestHeadX=.85f;
    public const double HeadMass=.5;
    private const double EndpointTolerance=1e-6;
    private static readonly ColliderChildId ShaftCollider=new(2);
    public static readonly JointSlot HeadGuide=new();
    public static readonly BodySlot HeadBody=new(p=>SceneGeometryAdapter.CaptureRigidPose(((LinearPusherPart)p)._head.Transform),
        _=>BodyDynamics.SolidSphere(HeadMass,HeadRadius,default,default),
        BodyQueryPolicy.Include,p=>p!.InitialContactMaterial,
        p=>((LinearPusherPart)p!).PoseAssets);
    public override IReadOnlyList<SceneServoDeclaration> PhysicsServos=>
    [
        new(new(this,HeadGuide),ReadParameter(PusherParameter.Speed),ReadParameter(PusherParameter.Acceleration),
            ReadParameter(PusherParameter.Force),(double)ReadParameter(PusherParameter.Force)*ReadParameter(PusherParameter.Speed))
    ];
    private PhysicsServoState? RuntimeServo=>GetParent() is MachineWorld {HasPhysicsState:true} world?
        world.Physics.Servo(world.PhysicsAssembly.JointId(new(this,HeadGuide))):null;
    private PusherPhase RequestedPhase=>!HasElectricalPower(SocketId.PowerIn)?PusherPhase.Unpowered:
        HasElectricalPower(SocketId.ExtendIn)&&HasElectricalPower(SocketId.RetractIn)?PusherPhase.Conflict:
        HasElectricalPower(SocketId.ExtendIn)?PusherPhase.Extending:
        HasElectricalPower(SocketId.RetractIn)?PusherPhase.Retracting:PusherPhase.Holding;
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints
    {
        get
        {
            var axis=RigidRotation.FromRotationVector(new(0,Math.PI/2,0));
            return [new SceneFrameJoint(new(this,HeadGuide),FrameJointKind.Slider,
                new(this,HeadBody),new(default,axis),
                new(this,RootBody),new(new(RestHeadX,0,0),axis),
                ConnectedBodyCollision.Disabled,new(0,ReadParameter(PusherParameter.Stroke)),
                JointTravelDirection.Both)];
        }
    }
    public float Extension=>(float)ReadAxialMotion(HeadGuide).Coordinate;
    public float TravelSpeed=>(float)ReadAxialMotion(HeadGuide).Speed;
    public double DeliveredWork=>GetParent() is MachineWorld {HasPhysicsState:true} world?
        world.Physics.MotorTotal(world.CurrentJoint(new(this,HeadGuide)).Id).SuppliedWork:0;
    public double LastDriveImpulse
    {
        get
        {
            if(GetParent() is not MachineWorld {HasPhysicsState:true} world) return 0;
            var joint=world.CurrentJoint(new(this,HeadGuide)).Id;
            foreach(var use in world.Physics.MotorUse)
                if(use.Joint==joint) return use.AbsoluteImpulse;
            return 0; // No command for this joint in the last committed step.
        }
    }
    public PusherPhase Phase
    {
        get
        {
            var command=RequestedPhase;
            if(command is not (PusherPhase.Extending or PusherPhase.Retracting))return command;
            return (command==PusherPhase.Extending&&Extended)||(command==PusherPhase.Retracting&&Retracted)
                ?PusherPhase.Holding:Math.Abs(TravelSpeed)<1e-6?PusherPhase.Blocked:command;
        }
    }
    public bool Extended=>RuntimeServo is { } servo&&servo.AtEndpoint(PhysicsServoEndpoint.Upper,EndpointTolerance);
    public bool Retracted=>RuntimeServo is not { } servo||servo.AtEndpoint(PhysicsServoEndpoint.Lower,EndpointTolerance);
    public Vector3 HeadPosition=>GetParent() is MachineWorld {HasPhysicsState:true} world
        ?world.PhysicsAssembly.Body(new(this,HeadBody)).Pose.ToScene().Origin:Transform*_head.Position;
    private Node3D _head=null!;
    private MeshInstance3D _rod=null!;
    private StandardMaterial3D _indicator=null!;
    public override Physics.ContactMaterial InitialContactMaterial => new(0,.1,.3);
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.45f,-.4f,.5f)),
        new(SocketId.ExtendIn,ConnectionDomain.Electrical,PortDirection.Input,new(.15f,-.4f,.5f)),
        new(SocketId.RetractIn,ConnectionDomain.Electrical,PortDirection.Input,new(.55f,-.4f,.5f)),
        new(SocketId.RetractedOut,ConnectionDomain.Electrical,PortDirection.Output,new(-.45f,.4f,.45f)),
        new(SocketId.ExtendedOut,ConnectionDomain.Electrical,PortDirection.Output,new(.45f,.4f,.45f))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
    [
        new(SocketId.PowerIn,SocketId.RetractedOut,ElectricalContactSignal.ServoAtEndpoint(new(this,HeadGuide),PhysicsServoEndpoint.Lower,EndpointTolerance)),
        new(SocketId.PowerIn,SocketId.ExtendedOut,ElectricalContactSignal.ServoAtEndpoint(new(this,HeadGuide),PhysicsServoEndpoint.Upper,EndpointTolerance))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        Check(parameters,PusherParameter.Stroke,.25f,3);
        Check(parameters,PusherParameter.Speed,.25f,4);
        Check(parameters,PusherParameter.Acceleration,1,30);
        Check(parameters,PusherParameter.Force,1,100);
    }
    private static void Check(PartParameterValues parameters,PusherParameter key,float minimum,float maximum)
    {
        var value=parameters.Read(key);
        if(!float.IsFinite(value)||value<minimum||value>maximum)
            throw new ArgumentException($"Pusher parameter must be between {minimum} and {maximum}.");
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        var mode=RequestedPhase switch
        {
            PusherPhase.Extending=>PhysicsServoMode.Upper,PusherPhase.Retracting=>PhysicsServoMode.Lower,
            PusherPhase.Unpowered or PusherPhase.Conflict or PusherPhase.Holding=>PhysicsServoMode.Hold,
            _=>throw new InvalidOperationException("Invalid pusher input phase.")
        };
        world.Physics.SetServoMode(world.PhysicsAssembly.JointId(new(this,HeadGuide)),mode);
    }

    public override void ObservePhysics(MachineWorld world,float delta)
    {
        Active=Phase is PusherPhase.Extending or PusherPhase.Retracting;
        _indicator.AlbedoColor=Phase switch {
            PusherPhase.Extending or PusherPhase.Retracting=>new("#66b8c9"),
            PusherPhase.Blocked or PusherPhase.Conflict=>new("#f7cb52"),
            _=>new("#556573")
        };
        // Explicit shared-world geometry replacement for the telescoping shaft.
        // Continuous deformation within a substep remains a qualification gap.
        var root=world.PhysicsAssembly.Body(new(this,RootBody));
        var head=world.PhysicsAssembly.Body(new(this,HeadBody));
        var key=new SceneBodyKey(this,RootBody);
        var geometry=world.CollisionGeometry(key);
        var shaft=geometry[ShaftCollider];
        if(shaft.Shape.Geometry is not ConvexBox box||shaft.Surface!=SweepSurfaceKind.Box||
            shaft.Source!=ColliderQuerySource.PartProxy||!shaft.Opaque)
            throw new InvalidOperationException("Pusher shaft declaration must remain an opaque box.");
        var desired=ShaftBox((float)root.Pose.InverseTransformPoint(head.Center).X);
        var half=SceneGeometryAdapter.CaptureVector(desired.Half);
        var pose=new AffineTransform(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(desired.At));
        if(box.Half==half&&shaft.Shape.Pose==pose)return;
        var replacement=geometry.WithChild(ShaftCollider,shaft with {Shape=new(new ConvexBox(half),pose)});
        var collider=world.Physics.Collider(root.Id).Declaration;
        world.ReplaceCollisionGeometry(key,replacement,collider.Material,collider.Participation);
    }

    private IReadOnlyList<ScenePoseAsset> PoseAssets =>
    [
        new(_head,RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity)),
        new(_rod,RootPoseReference,ScenePoseMap.AxisAffine(PoseReadSpace.Relative,PoseMapAxis.X,RigidRotation.Identity,
            new(.3f,0,0),new(.5,0,0),new(-.6f,1,1),new(1,0,0)))
    ];
    private void PresentShaft()
    {
        var x=_head.Position.X;
        _rod.Position=new((.6f+x)*.5f,0,0);
        _rod.Scale=new(x-.6f,1,1);
    }
    private static BoxProxy ShaftBox(float x)=>
        new(new((.6f+x)*.5f,0,0),new((x-.6f)*.5f,.06f,.06f),RootBody);
    protected override void Build()
    {
        PickRadius=1.15f;
        AddBox(new(0,-.5f,0),new(1.6f,.2f,1),new("#293954"));
        AddBox(Vector3.Zero,new(1.2f,.65f,.65f),new("#fff8e9"));
        Boxes.Add(ShaftBox(RestHeadX));
        _rod=PartArt.Box(Visual,new(1,.12f,.12f),new("#e8b764"));
        _head=new Node3D {Position=new(RestHeadX,0,0)};Visual.AddChild(_head);
        PartArt.Sphere(_head,HeadRadius,new("#fff8e9"));
        var ring=PartArt.Ring(_head,.2f,.035f,new("#66b8c9"));ring.RotationDegrees=new(0,0,90);
        Spheres.Add(new(Vector3.Zero,HeadRadius,HeadBody));
        for(var i=0;i<5;i++)
            PartArt.Box(Visual,new(.025f,.025f,.25f),new("#e8b764"),new(-.4f+i*.2f,.34f,0));
        foreach(var port in ConnectionPorts)
            PartArt.Sphere(Visual,.065f,port.Direction==PortDirection.Output?new("#66b8c9"):new("#e8b764"),port.LocalPosition);
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.07f,new("#556573"),new(0,.42f,0)).MaterialOverride;
        PresentShaft();
    }
}
