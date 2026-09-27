using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Powered, finite-width conveyor. Traction acts only on bodies contacting its top.</summary>
public partial class ConveyorPart : MachinePart
{
    private readonly List<MeshInstance3D> _treads = new();
    private float _phase;
    private float _length;
    public override bool CanReceivePower => true;
    public override float SurfaceBounce => .05f;

    protected override void Build()
    {
        _length = Parameter("length", 3);
        var width = Parameter("width", 1.2f);
        Active = Parameter("powered", 1) > .5f;
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
        for (var i = 0; i < 10; i++)
            _treads.Add(PartArt.Box(Visual, new(.045f, .015f, width * .9f), new("#88b7a9"),
                new(-_length / 2 + i * _length / 10, .13f, 0)));
        PartArt.Line(Visual, new(-.4f, .15f, 0), new(.4f, .15f, 0), new("#f2d78c"), .025f);
        PartArt.Line(Visual, new(.4f, .15f, 0), new(.15f, .15f, .18f), new("#f2d78c"), .025f);
        PartArt.Line(Visual, new(.4f, .15f, 0), new(.15f, .15f, -.18f), new("#f2d78c"), .025f);
    }

    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (!Active) return;
        _phase = Mathf.PosMod(_phase + Parameter("speed", 4) * delta, _length / 10);
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
        var traction = Parameter("traction", 18) / body.Mass;
        var driven = Mathf.MoveToward(along, Parameter("speed", 4), traction * MachineWorld.Tick / MachineWorld.Substeps);
        body.Velocity += direction * (driven - along);
        world.Events.TryAdd("transported:" + Uid + ":" + body.Uid, world.Ticks);
    }
}

