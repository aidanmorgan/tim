using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum MotorParameter { Speed, Torque }

public partial class MotorPart : MachinePart
{
    private Node3D _rotor = null!;
    private StandardMaterial3D _indicator = null!;
    public float ShaftSpeed { get; private set; }
    public float ShaftAngle { get; private set; }
    public float ShaftTravel { get; private set; }
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(-.55f, 0, 0)),
        new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output, new(0, 0, .65f))
    ];
    public override IEnumerable<MechanicalSource> MechanicalSources => [new(SocketId.Drive, ShaftSpeed, Active ? ReadParameter(MotorParameter.Torque) : 0)];
    public override void ValidateParameters()
    {
        var speed = ReadParameter(MotorParameter.Speed);
        var torque = ReadParameter(MotorParameter.Torque);
        if (!float.IsFinite(speed) || speed < 0 || speed > 20)
            throw new ArgumentException("Motor speed must be between 0 and 20.");
        if (!float.IsFinite(torque) || torque <= 0 || torque > 100)
            throw new ArgumentException("Motor torque must be greater than zero and at most 100.");
    }
    protected override void Build()
    {
        ClearMechanicalDrive();
        PickRadius = .9f;
        AddBox(new(0, -.43f, 0), new(1.25f, .16f, 1), new("#293954"));
        AddBox(Vector3.Zero, new(1, .8f, .85f), Definition.Color);
        var housing = PartArt.Cylinder(Visual, .48f, .86f, Definition.Color);
        housing.RotationDegrees = new(90, 0, 0);
        PartArt.Sphere(Visual, .1f, new("#f7cb52"), new(-.55f, 0, 0));
        _rotor = new Node3D { Name = "Shaft", Position = new(0, 0, .54f) };
        Visual.AddChild(_rotor);
        var wheel = PartArt.Cylinder(_rotor, .3f, .13f, new("#fff8e9"));
        wheel.RotationDegrees = new(90, 0, 0);
        PartArt.Box(_rotor, new(.48f, .07f, .04f), new("#f7cb52"), new(0, 0, .08f));
        var indicator = PartArt.Sphere(Visual, .075f, new("#556573"), new(.32f, .28f, .45f));
        _indicator = (StandardMaterial3D)indicator.MaterialOverride;
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        Active = HasElectricalPower(SocketId.PowerIn);
        var target = Active ? ReadParameter(MotorParameter.Speed) : 0;
        ShaftSpeed = Mathf.MoveToward(ShaftSpeed, target, 18 * delta);
        ShaftTravel += ShaftSpeed * delta;
        ShaftAngle = Mathf.PosMod(ShaftTravel, Mathf.Tau);
        if (ShaftTravel >= Mathf.Tau) world.Events.TryAdd(new MachineEvent(MachineEventKind.Turned, Uid), world.Ticks);
        _rotor.Rotation = new(0, 0, -ShaftAngle);
        _indicator.AlbedoColor = Active ? new("#f7cb52") : new("#556573");
        if (Active) world.Events.TryAdd(new MachineEvent(MachineEventKind.Powered, Uid), world.Ticks);
    }
}
