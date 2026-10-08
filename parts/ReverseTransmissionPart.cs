using Godot;
using CuriousContraptions.Physics;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>A one-to-one reversing gearbox. Direction is relative to the marked shaft faces.</summary>
public partial class ReverseTransmissionPart : MachinePart
{
    public static readonly JointSlot InputJoint=new(),OutputJoint=new(),GearJoint=new();
    public static readonly BodySlot InputBody=SceneRotaryShaft.Slot(p=>((ReverseTransmissionPart)p)._inputWheel,.25,.32,.14);
    public static readonly BodySlot OutputBody=SceneRotaryShaft.Slot(p=>((ReverseTransmissionPart)p)._outputWheel,.25,.32,.14);
    public override IReadOnlyList<SceneJointDeclaration> PhysicsJoints=>
    [
        SceneRotaryShaft.Guide(this,InputJoint,InputBody,new(-.36f,0,.32f)),
        SceneRotaryShaft.Guide(this,OutputJoint,OutputBody,new(.36f,0,.32f)),
        new SceneTransmissionJoint(new(this,GearJoint),new(this,InputJoint),new(this,OutputJoint),-1,TransmissionEngagement.Engaged)
    ];
    private Node3D _inputWheel = null!;
    private Node3D _outputWheel = null!;
    public float InputSpeed=>(float)-ReadAxialMotion(InputJoint).Speed;
    public float OutputSpeed=>(float)-ReadAxialMotion(OutputJoint).Speed;
    public float InputAngle=>(float)-ReadAxialMotion(InputJoint).Coordinate;
    public float OutputAngle=>(float)-ReadAxialMotion(OutputJoint).Coordinate;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, new(-.36f, 0, .4f)),
        new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output, new(.36f, 0, .4f))
    ];
    public override IReadOnlyList<MechanicalBinding> MechanicalBindings=>[new(SocketId.DriveIn,InputJoint,-1),new(SocketId.Drive,OutputJoint,-1)];

    protected override void Build()
    {
        PickRadius = .9f;

        AddBox(new(0, -.4f, 0), new(1.5f, .16f, .9f), new("#293954"));
        AddBox(new(0, 0, -.08f), new(1.4f, .65f, .5f), new("#fff8e9"));
        _inputWheel = Wheel(-.36f);
        _outputWheel = Wheel(.36f);
        SceneRotaryShaft.AddCollider(this,InputBody,.32,.14);
        SceneRotaryShaft.AddCollider(this,OutputBody,.32,.14);
    }
    private Node3D Wheel(float x)
    {
        var wheel = new Node3D { Position = new(x, 0, .32f) };
        Visual.AddChild(wheel);
        var disc = PartArt.Cylinder(wheel, .32f, .14f, Definition.Color);
        disc.RotationDegrees = new(90, 0, 0);
        PartArt.Box(wheel, new(.48f, .065f, .04f), new("#293954"), new(0, 0, .09f));
        PartArt.Sphere(wheel, .07f, new("#f7cb52"), new(.21f, 0, .1f));
        return wheel;
    }
    public override void ObservePhysics(MachineWorld world,float delta)
    {
        Active=System.Math.Abs(InputSpeed)>1e-6;
    }
}
