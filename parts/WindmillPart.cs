using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Physics;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum WindmillParameter { RadiansPerForce }

/// <summary>Air torque acts on a finite-inertia rotor in the shared world. Aerodynamic resistance depends on solved shaft speed.</summary>
public partial class WindmillPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<WindmillParameter>(fields);
    public const float MaximumSpeed=12;
    public const float CutInForce=.05f;
    public const float TorqueArm=.4f;
    private static readonly Vector3 RotorAt=new(-.12f,0,0);
    public static readonly JointSlot RotorJoint=new();
    private static readonly RigidRotation AxisFrame=RigidRotation.FromRotationVector(new(0,Math.PI/2,0));
    private const double RotorMass=1, RotorRadius=.7, RotorThickness=.075;
    private const double AxialInertia=RotorMass*RotorRadius*RotorRadius/2;
    private const double TransverseInertia=RotorMass*(3*RotorRadius*RotorRadius+RotorThickness*RotorThickness)/12;
    public static readonly BodySlot RotorBody=new(
        p=>SceneGeometryAdapter.CaptureRigidPose(((WindmillPart)p)._rotor.Transform),
        _=>new(PhysicsMotionType.Dynamic,RotorMass,new(AxialInertia,TransverseInertia,TransverseInertia),default,default),
        BodyQueryPolicy.ExcludeFromStaticQueries,p=>p!.InitialContactMaterial,
        p=>[new(((WindmillPart)p!)._rotor,p!.RootPoseReference,ScenePoseMap.Rigid(PoseReadSpace.Relative,global::CuriousContraptions.Geometry.RigidPose.Identity))]);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        new SceneFrameJoint(new(this,RotorJoint),FrameJointKind.Hinge,new(this,RotorBody),new(default,AxisFrame),
            new(this,RootBody),new(SceneGeometryAdapter.CaptureVector(RotorAt),AxisFrame),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both)
    ];
    private static readonly AirflowSample[] RotorSamples=
    [
        new(new(-.12f,.42f,0),.25f,RootBody,AirflowResponse.Rotary,new(),RotorJoint),new(new(-.12f,-.42f,0),.25f,RootBody,AirflowResponse.Rotary,new(),RotorJoint),
        new(new(-.12f,0,.42f),.25f,RootBody,AirflowResponse.Rotary,new(),RotorJoint),new(new(-.12f,0,-.42f),.25f,RootBody,AirflowResponse.Rotary,new(),RotorJoint)
    ];
    public override IReadOnlyList<AirflowSample> AirflowSamples=>RotorSamples;
    public override IReadOnlyList<RotaryAirflowCapture> RotaryAirflowCaptures=>
        [new(RotorJoint,new(TorqueArm,ReadParameter(WindmillParameter.RadiansPerForce),MaximumSpeed,CutInForce),
            AirflowNetwork.ControlForceTolerance)];
    private readonly SimulationState<float> _axialForce = new(0);
    public float AxialForce => _axialForce.Value;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_axialForce];
    public float ShaftSpeed=>(float)ReadAxialMotion(RotorJoint).Speed;
    public float ShaftAngle=>(float)ReadAxialMotion(RotorJoint).Coordinate;
    public float ShaftTravel=>(float)ReadAngularTravel(RotorJoint).Distance;
    private Node3D _rotor=null!;
    private Node3D _pulley=null!;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.Drive,ConnectionDomain.Mechanical,PortDirection.Output,new(.25f,-.1f,.52f))
    ];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.Drive,RotorJoint,1)];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var gain=parameters.Read(WindmillParameter.RadiansPerForce);
        if(!float.IsFinite(gain)||gain<.1f||gain>4)throw new ArgumentException("Windmill response must be between 0.1 and 4 radians per unit force.");
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(this,RotorJoint));
        var force=AirflowNetwork.ReadReceiverForce(world,this,delta);
        _axialForce.Value=(float)CollisionVector.Dot(SceneGeometryAdapter.CaptureVector(force),joint.FrameB.Orientation.Apply(new(0,0,1)));
        Active=Math.Abs(ShaftSpeed)>1e-6;
        if(ReadAngularTravel(RotorJoint).Distance>=Math.Tau)world.Events.TryAdd(new(MachineEventKind.Turned,Uid),world.Ticks);
        _pulley.Rotation=new(0,0,-ShaftAngle);
    }
    protected override void Build()
    {
        PickRadius=1.2f;
        AddBox(new(.25f,-1.08f,0),new(1.1f,.16f,.9f),new("#293954"));
        AddBox(new(.25f,-.55f,0),new(.22f,1.1f,.25f),new("#fff8e9"));
        AddBox(new(.25f,-.1f,0),new(.65f,.45f,.55f),new("#fff8e9"));
        var axle=PartArt.Cylinder(Visual,.11f,.55f,new("#e8b764"),new(.14f,0,0));
        axle.RotationDegrees=new(0,0,90);
        var guard=PartArt.Ring(Visual,.78f,.035f,new("#fff8e9"),RotorAt);
        guard.RotationDegrees=new(0,0,90);
        Tubes.Add(new(new Transform3D(Basis.Identity,RotorAt),.035f,.745f,.815f,true));
        Spheres.Add(new(Vector3.Zero,.14f,RotorBody));
        _rotor=new Node3D {Name="WindRotor",Position=RotorAt};Visual.AddChild(_rotor);
        for(var i=0;i<4;i++)
        {
            var arm=new Node3D {Rotation=new(i*Mathf.Pi*.5f,0,0)};_rotor.AddChild(arm);
            var blade=PartArt.Box(arm,new(.075f,.52f,.21f),new("#66b8c9"),new(0,.4f,0));
            blade.RotationDegrees=new(0,30,0);
            ConvexShapes.Add(new(new(new ConvexBox(new(.0375,.26,.105)),SceneGeometryAdapter.CaptureAffine(arm.Transform*blade.Transform)),RotorBody));
            PartArt.Box(arm,new(.08f,.07f,.21f),new("#e8b764"),new(0,.63f,0));
        }
        PartArt.Sphere(_rotor,.14f,new("#e8b764"),Vector3.Zero);
        _pulley=new Node3D {Name="OutputPulley",Position=new(.25f,-.1f,.44f)};Visual.AddChild(_pulley);
        var wheel=PartArt.Cylinder(_pulley,.24f,.12f,new("#fff8e9"));
        wheel.RotationDegrees=new(90,0,0);
        PartArt.Box(_pulley,new(.38f,.05f,.035f),new("#e8b764"),new(0,0,.08f));
    }
}
