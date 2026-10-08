using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ConveyorParameter { Length, Width, SurfacePerRadian }

/// <summary>Mechanically driven, finite-width conveyor. Traction acts only on bodies contacting its top.</summary>
public partial class ConveyorPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ConveyorParameter>(fields);
    private readonly List<MeshInstance3D> _treads = new();
    private Node3D _directionArrow = null!;
    private float _length;
    private Node3D _shaftVisual=null!;
    private const double RollerMass=.5;
    // Mechanical ports are clockwise-positive viewed from +Z; rigid hinges are right-handed.
    private const double ClockwiseToHinge=-1;
    private const float RollerRadius=.16f;
    private Vector3 ShaftCenter=>new(-_length/2+.15f,-.07f,0);
    public static readonly JointSlot ShaftJoint=new();
    public static readonly BodySlot ShaftBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((ConveyorPart)p)._shaftVisual.Transform),
        p=>((ConveyorPart)p!).ShaftDynamics,BodyQueryPolicy.ExcludeFromStaticQueries,
        p=>p!.InitialContactMaterial,p=>[new(((ConveyorPart)p!)._shaftVisual,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,global::CuriousContraptions.Geometry.RigidPose.Identity))]);
    private BodyDynamics ShaftDynamics
    {
        get
        {
            var width=(double)ReadParameter(ConveyorParameter.Width)+.1;
            var axial=RollerMass*RollerRadius*RollerRadius/2;
            var transverse=RollerMass*(3*RollerRadius*RollerRadius+width*width)/12;
            return new(PhysicsMotionType.Dynamic,RollerMass,new(transverse,transverse,axial),default,default);
        }
    }
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        new SceneFrameJoint(new(this,ShaftJoint),FrameJointKind.Hinge,new(this,ShaftBody),new(default,RigidRotation.Identity),
            new(this,RootBody),new(SceneGeometryAdapter.CaptureVector(ShaftCenter),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both)
    ];
    public override IReadOnlyList<SceneDrivenSurface> PhysicsSurfaces=>
    [
        new(new(this,ShaftJoint),new(0,1,0),new(1,0,0),ClockwiseToHinge*ReadParameter(ConveyorParameter.SurfacePerRadian))
    ];
    private readonly List<Node3D> _pulleys = new();
    public float ShaftSpeed=>(float)(ClockwiseToHinge*ReadAxialMotion(ShaftJoint).Speed);
    public float ShaftAngle=>(float)(ClockwiseToHinge*ReadAxialMotion(ShaftJoint).Coordinate);
    public float SurfaceSpeed
    {
        get
        {
            if(GetParent() is not MachineWorld {HasPhysicsState:true} world) return 0;
            var joint=world.CurrentJoint(new(this,ShaftJoint)).Id;
            foreach(var surface in world.Physics.Surfaces)
                if(surface.Drive.Id==joint) return (float)surface.Speed;
            throw new InvalidOperationException("Conveyor requires its captured driven surface.");
        }
    }
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input,
            new(-ReadParameter(ConveyorParameter.Length) / 2 + .15f, -.07f, ReadParameter(ConveyorParameter.Width) / 2 + .18f)),
        new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output,
            new(ReadParameter(ConveyorParameter.Length) / 2 - .15f, -.07f, ReadParameter(ConveyorParameter.Width) / 2 + .18f))
    ];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.DriveIn,ShaftJoint,-1),new(SocketId.Drive,ShaftJoint,-1)];
    public override Physics.ContactMaterial InitialContactMaterial => new(.05f,.1,.3);

    protected override void ValidateParameters(PartParameterValues parameters)
    {
        foreach (var key in new[] { ConveyorParameter.Length, ConveyorParameter.Width })
            if (!float.IsFinite(parameters.Read(key)) || parameters.Read(key) <= 0)
                throw new ArgumentException("Conveyor dimensions must be finite and positive.");
        var ratio = parameters.Read(ConveyorParameter.SurfacePerRadian);
        if (!float.IsFinite(ratio) || ratio == 0)
            throw new ArgumentException("Conveyor surface travel per radian must be finite and nonzero.");
    }

    protected override void Build()
    {
        _length = ReadParameter(ConveyorParameter.Length);
        var width = ReadParameter(ConveyorParameter.Width);

        PickRadius = .85f;
        AddBox(Vector3.Zero, new(_length, .24f, width), new("#273744"));
        foreach (var z in new[] { -width / 2, width / 2 })
            PartArt.Box(Visual, new(_length, .16f, .08f), Definition.Color, new(0, -.12f, z));
        _shaftVisual=new Node3D { Position=ShaftCenter };
        Visual.AddChild(_shaftVisual);
        var vertices=new List<CollisionVector>();
        for(var i=0;i<24;i++)
        foreach(var sign in new[]{-1,1})
        {
            var angle=Math.Tau*i/24;
            vertices.Add(new(Math.Cos(angle)*RollerRadius,Math.Sin(angle)*RollerRadius,sign*(width+.1f)/2));
        }
        ConvexShapes.Add(new(new(new ConvexHull(vertices.ToArray()),AffineTransform.Identity),ShaftBody));
        foreach (var x in new[] { -_length / 2 + .15f, _length / 2 - .15f })
        {
            var driven=x==ShaftCenter.X;
            var roller = PartArt.Cylinder(driven?_shaftVisual:Visual, RollerRadius, width + .1f,
                Definition.Color, driven?Vector3.Zero:new(x, -.07f, 0));
            roller.RotationDegrees = new(90, 0, 0);
            PartArt.Box(Visual, new(.12f, .6f, width * .8f), new("#546876"), new(x, -.4f, 0));
        }
        foreach (var port in ConnectionPorts)
        {
            var pulley = new Node3D { Position = port.LocalPosition };
            Visual.AddChild(pulley);
            var wheel = PartArt.Cylinder(pulley, .22f, .1f, new("#fff8e9"));
            wheel.RotationDegrees = new(90, 0, 0);
            PartArt.Box(pulley, new(.33f, .045f, .035f), new("#f7cb52"), new(0, 0, .06f));
            _pulleys.Add(pulley);
        }
        for (var i = 0; i < 10; i++)
            _treads.Add(PartArt.Box(Visual, new(.045f, .015f, width * .9f), new("#88b7a9"),
                new(-_length / 2 + i * _length / 10, .13f, 0)));
        _directionArrow = new Node3D();
        Visual.AddChild(_directionArrow);
        PartArt.Line(_directionArrow, new(-.4f, .15f, 0), new(.4f, .15f, 0), new("#f2d78c"), .025f);
        PartArt.Line(_directionArrow, new(.4f, .15f, 0), new(.15f, .15f, .18f), new("#f2d78c"), .025f);
        PartArt.Line(_directionArrow, new(.4f, .15f, 0), new(.15f, .15f, -.18f), new("#f2d78c"), .025f);
    }

    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(this,ShaftJoint));
        Active=Math.Abs(ShaftSpeed)>1e-6;
        foreach(var pulley in _pulleys) pulley.Rotation=new(0,0,-ShaftAngle);
        if(Active) _directionArrow.Rotation=new(0,SurfaceSpeed<0?Mathf.Pi:0,0);
        var phase=Mathf.PosMod((float)(ClockwiseToHinge*ReadAngularTravel(ShaftJoint).Winding*
            ReadParameter(ConveyorParameter.SurfacePerRadian)),_length/10);
        for(var i=0;i<_treads.Count;i++)
            _treads[i].Position=new(-_length/2+i*_length/10+phase,.13f,0);
        foreach(var contact in world.Physics.SurfaceContacts())
        {
            if(contact.Drive!=joint.Id||Math.Abs(contact.SurfaceSpeed)<=1e-6) continue;
            var key=world.PhysicsAssembly.Key(contact.Receiver);
            var body=world.PhysicsAssembly.Body(key);
            if(key.Owner is null||body.MotionType!=PhysicsMotionType.Dynamic) continue;
            var along=CollisionVector.Dot(body.PointVelocity(contact.Point),contact.Direction);
            if(along*contact.SurfaceSpeed>0)
                world.Events.TryAdd(new(MachineEventKind.Transported,Uid,key.Owner.Uid),world.Ticks);
        }
    }
}
