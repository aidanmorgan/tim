using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public static class ConveyorParameters
{
    public const string Length = "length";
    public const string Width = "width";
    public const string SurfacePerRadian = "surface_per_radian";
    public const string Traction = "traction";
}

/// <summary>Mechanically driven, finite-width conveyor. Traction acts only on bodies contacting its top.</summary>
public partial class ConveyorPart : MachinePart
{
    private readonly List<MeshInstance3D> _treads = new();
    private Node3D _directionArrow = null!;
    private float _phase;
    private float _length;
    private readonly List<Node3D> _pulleys = new();
    public float ShaftSpeed { get; private set; }
    public float SurfaceSpeed { get; private set; }
    public float ShaftAngle { get; private set; }
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.DriveIn, ConnectionDomain.Mechanical, PortDirection.Input,
            new(-Properties[ConveyorParameters.Length] / 2 + .15f, -.07f, Properties[ConveyorParameters.Width] / 2 + .18f)),
        new(SocketId.Drive, ConnectionDomain.Mechanical, PortDirection.Output,
            new(Properties[ConveyorParameters.Length] / 2 - .15f, -.07f, Properties[ConveyorParameters.Width] / 2 + .18f))
    ];
    public override IEnumerable<MechanicalRoute> MechanicalRoutes => [new(SocketId.DriveIn, SocketId.Drive, 1, true)];
    public override float SurfaceBounce => .05f;

    protected override void Build()
    {
        _length = Properties[ConveyorParameters.Length];
        var width = Properties[ConveyorParameters.Width];
        ClearMechanicalDrive();
        PickRadius = .85f;
        AddBox(Vector3.Zero, new(_length, .24f, width), new("#273744"));
        foreach (var z in new[] { -width / 2, width / 2 })
            PartArt.Box(Visual, new(_length, .16f, .08f), Definition.Color, new(0, -.12f, z));
        foreach (var x in new[] { -_length / 2 + .15f, _length / 2 - .15f })
        {
            var roller = PartArt.Cylinder(Visual, .16f, width + .1f, Definition.Color, new(x, -.07f, 0));
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

    public override void MechanicalStep(MachineWorld world, float delta)
    {
        ShaftSpeed = MechanicalSpeed(SocketId.DriveIn);
        SurfaceSpeed = ShaftSpeed * Properties[ConveyorParameters.SurfacePerRadian];
        Active = ShaftSpeed != 0;
        ShaftAngle = Mathf.PosMod(ShaftAngle + ShaftSpeed * delta, Mathf.Tau);
        foreach (var pulley in _pulleys) pulley.Rotation = new(0, 0, -ShaftAngle);
        if (Active) _directionArrow.Rotation = new(0, SurfaceSpeed < 0 ? Mathf.Pi : 0, 0);
        _phase = Mathf.PosMod(_phase + SurfaceSpeed * delta, _length / 10);
        for (var i = 0; i < _treads.Count; i++)
            _treads[i].Position = new(-_length / 2 + i * _length / 10 + _phase, .13f, 0);
    }

    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (!Active) return;
        var local = ToLocal(body.Position);
        // A side/underside hit is a collision, not contact with the moving top belt.
        if (local.Y < .12f + body.Radius * .8f || Mathf.Abs(local.X) > _length / 2) return;
        var direction = Basis.X.Normalized();
        var along = body.Velocity.Dot(direction);
        var traction = Properties[ConveyorParameters.Traction] / body.Mass;
        var driven = Mathf.MoveToward(along, SurfaceSpeed, traction * MachineWorld.Tick / MachineWorld.Substeps);
        body.Velocity += direction * (driven - along);
        world.Events.TryAdd(new MachineEvent(MachineEventKind.Transported, Uid, body.Uid), world.Ticks);
    }
}
