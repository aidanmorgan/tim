using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>A one-to-one reversing gearbox. Direction is relative to the marked shaft faces.</summary>
public partial class ReverseTransmissionPart : MachinePart
{
    private Node3D _inputWheel = null!;
    private Node3D _outputWheel = null!;
    public float InputSpeed { get; private set; }
    public float OutputSpeed { get; private set; }
    public float InputAngle { get; private set; }
    public float OutputAngle { get; private set; }
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketIds.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input, new(-.36f, 0, .4f)),
        new(SocketIds.Drive, ConnectionDomain.Mechanical, PortDirection.Output, new(.36f, 0, .4f))
    ];
    public override IEnumerable<MechanicalRoute> MechanicalRoutes => [new(SocketIds.DriveIn, SocketIds.Drive, -1, true)];

    protected override void Build()
    {
        PickRadius = .9f;
        ClearMechanicalDrive();
        AddBox(new(0, -.4f, 0), new(1.5f, .16f, .9f), new("#293954"));
        AddBox(new(0, 0, -.08f), new(1.4f, .65f, .5f), new("#fff8e9"));
        _inputWheel = Wheel(-.36f);
        _outputWheel = Wheel(.36f);
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
    public override void MechanicalStep(MachineWorld world, float delta)
    {
        InputSpeed = MechanicalSpeed(SocketIds.DriveIn);
        OutputSpeed = MechanicalSpeed(SocketIds.Drive);
        Active = InputSpeed != 0;
        InputAngle = Mathf.PosMod(InputAngle + InputSpeed * delta, Mathf.Tau);
        OutputAngle = Mathf.PosMod(OutputAngle + OutputSpeed * delta, Mathf.Tau);
        _inputWheel.Rotation = new(0, 0, -InputAngle);
        _outputWheel.Rotation = new(0, 0, -OutputAngle);
    }
}
