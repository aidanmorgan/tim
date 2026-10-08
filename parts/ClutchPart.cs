using Godot;
using CuriousContraptions.Physics;
using System.Linq;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ClutchPhase { Open, Closing, Engaged, Opening }
public enum ClutchParameter { CloseSeconds }

/// <summary>Electrically engaged, normally open coupling between two shared finite-inertia shafts.</summary>
public partial class ClutchPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ClutchParameter>(fields);
    public static readonly JointSlot InputJoint=new(),OutputJoint=new(),CouplingJoint=new();
    public static readonly BodySlot InputBody=SceneRotaryShaft.Slot(p=>((ClutchPart)p)._input,.25,.28,.13);
    public static readonly BodySlot OutputBody=SceneRotaryShaft.Slot(p=>((ClutchPart)p)._output,.25,.28,.13);
    private TransmissionEngagement Engagement=>Phase==ClutchPhase.Engaged?TransmissionEngagement.Engaged:TransmissionEngagement.Open;
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        SceneRotaryShaft.Guide(this,InputJoint,InputBody,new(-.65f,0,.43f)),
        SceneRotaryShaft.Guide(this,OutputJoint,OutputBody,new(.65f,0,.43f)),
        new SceneTransmissionJoint(new(this,CouplingJoint),new(this,InputJoint),new(this,OutputJoint),1,Engagement)
    ];
    private readonly record struct ClosureState(ClutchPhase Phase,float Closure);
    private readonly SimulationState<ClosureState> _state=new(new(ClutchPhase.Open,0));
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[_state];
    public ClutchPhase Phase=>_state.Value.Phase;
    public float Closure=>_state.Value.Closure;
    public float InputSpeed=>(float)-ReadAxialMotion(InputJoint).Speed;
    public float OutputSpeed=>(float)-ReadAxialMotion(OutputJoint).Speed;
    public float InputAngle=>(float)-ReadAxialMotion(InputJoint).Coordinate;
    public float OutputAngle=>(float)-ReadAxialMotion(OutputJoint).Coordinate;
    private Node3D _input=null!,_output=null!,_leftPlate=null!,_rightPlate=null!;
    private StandardMaterial3D _indicator=null!;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.DriveIn,ConnectionDomain.Mechanical,PortDirection.Input,new(-.65f,0,.55f)),
        new(SocketId.Drive,ConnectionDomain.Mechanical,PortDirection.Output,new(.65f,0,.55f)),
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.35f,.5f))
    ];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.DriveIn,InputJoint,-1),new(SocketId.Drive,OutputJoint,-1)];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var seconds=parameters.Read(ClutchParameter.CloseSeconds);
        if(!float.IsFinite(seconds)||seconds<.05f||seconds>2)throw new ArgumentException("Clutch closing time must be between 0.05 and 2 seconds.");
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        var supplied=HasElectricalPower(SocketId.PowerIn);
        var closure=Mathf.MoveToward(Closure,supplied?1:0,delta/ReadParameter(ClutchParameter.CloseSeconds));
        var phase=supplied?(closure==1?ClutchPhase.Engaged:ClutchPhase.Closing):(closure==0?ClutchPhase.Open:ClutchPhase.Opening);
        _state.Value=new(phase,closure);
        Active=Phase==ClutchPhase.Engaged;
        var coupling=(PhysicsTransmissionJoint)world.CurrentJoint(new(this,CouplingJoint));
        if(coupling.Engagement!=Engagement)
        {
            var replacement=new PhysicsTransmissionJoint(coupling.Id,
                (PhysicsFrameJoint)world.CurrentJoint(new(this,InputJoint)),
                (PhysicsFrameJoint)world.CurrentJoint(new(this,OutputJoint)),1,Engagement);
            world.Physics.ReplaceJoints(world.Physics.Joints.ToArray().Select(j=>j.Id==coupling.Id?replacement:j));
        }
        _leftPlate.Position=new(-.09f-.16f*(1-Closure),0,0);
        _rightPlate.Position=new(.09f+.16f*(1-Closure),0,0);
        _indicator.AlbedoColor=Active?new("#f7cb52"):supplied?new("#66b8c9"):new("#556573");
        if(supplied)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        _leftPlate.Rotation=new(InputAngle,0,0);_rightPlate.Rotation=new(OutputAngle,0,0);
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(new(0,-.48f,0),new(1.9f,.16f,1.1f),new("#293954"));
        AddBox(new(-.65f,-.1f,0),new(.35f,.7f,.65f),new("#fff8e9"));
        AddBox(new(.65f,-.1f,0),new(.35f,.7f,.65f),new("#fff8e9"));
        _input=Pulley(-.65f,"InputPulley");_output=Pulley(.65f,"OutputPulley");
        SceneRotaryShaft.AddCollider(this,InputBody,.28,.13);
        SceneRotaryShaft.AddCollider(this,OutputBody,.28,.13);
        _leftPlate=Plate(-.25f,"InputPlate");_rightPlate=Plate(.25f,"OutputPlate");
        var axle=PartArt.Cylinder(Visual,.055f,1.3f,new("#293954"));axle.RotationDegrees=new(0,0,90);
        var coil=PartArt.Ring(Visual,.38f,.065f,new("#66b8c9"));coil.RotationDegrees=new(0,0,90);
        PartArt.Sphere(Visual,.08f,new("#e8b764"),new(0,-.35f,.5f));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.065f,new("#556573"),new(0,.4f,.18f)).MaterialOverride;
    }
    private Node3D Pulley(float x,string name)
    {
        var node=new Node3D {Name=name,Position=new(x,0,.43f)};Visual.AddChild(node);
        var wheel=PartArt.Cylinder(node,.28f,.13f,new("#fff8e9"));wheel.RotationDegrees=new(90,0,0);
        PartArt.Box(node,new(.44f,.065f,.04f),new("#e8b764"),new(0,0,.08f));return node;
    }
    private Node3D Plate(float x,string name)
    {
        var node=new Node3D {Name=name,Position=new(x,0,0)};Visual.AddChild(node);
        var disc=PartArt.Cylinder(node,.3f,.18f,new("#e8b764"));disc.RotationDegrees=new(0,0,90);
        PartArt.Box(node,new(.19f,.43f,.04f),new("#293954"));return node;
    }
}
