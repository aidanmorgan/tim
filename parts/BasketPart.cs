using Godot;
using System.Collections.Generic;
namespace CuriousContraptions;

public partial class BasketPart : MachinePart
{
    public override float SurfaceBounce => .12f;
    private readonly Dictionary<string, float> _residence = new();
    private MeshInstance3D _rim = null!;
    protected override void Build()
    {
        PickRadius = .85f;
        var color = Definition.Color;
        AddBox(new(0, -.45f, 0), new(1.5f, .15f, 1.5f), color);
        AddBox(new(-.75f, 0, 0), new(.12f, 1, 1.6f), color);
        AddBox(new(.75f, 0, 0), new(.12f, 1, 1.6f), color);
        AddBox(new(0, 0, -.75f), new(1.5f, 1, .12f), color);
        AddBox(new(0, -.2f, .75f), new(1.5f, .6f, .12f), color);
        _rim = PartArt.Ring(Visual, .94f, .025f, new("#bdf4bd"), new(0, .55f, 0));
        foreach (var x in new[] { -.55f, -.27f, 0, .27f, .55f })
            PartArt.Box(Visual, new(.025f, .45f, .025f), color.Lightened(.35f), new(x, -.15f, .825f));
    }
    public override void UpdateAssistance(float precision)
    {
        // Assistance changes the acceptance window without moving solid walls.
        // Moving the walls changes trajectories and can invalidate precise solutions.
        _rim.Scale = Vector3.One * (1 + Assistance(precision).CaptureMargin);
    }

    public override void BeforeStep(MachineWorld world, float delta)
    {
        var assistance = Assistance(world.Precision).GuideAcceleration;
        if (assistance <= 0) return;
        foreach (var body in world.Bodies)
        {
            if (!body.Visible) continue;
            var local = ToLocal(body.Position);
            var velocity = Basis.Inverse() * body.Velocity;
            // A bounded lateral guide above the open mouth, only on descent.
            // It cannot pull bodies through the basket walls or from another plane.
            if (velocity.Y >= 0 || local.Y < .5f + body.Radius ||
                local.Y > 1.5f || Mathf.Abs(local.X) > 1.1f || Mathf.Abs(local.Z) > 1.1f) continue;
            var guide = new Vector3(-local.X, 0, -local.Z).LimitLength(1) * assistance;
            body.Velocity += Basis * guide * delta;
        }
    }

    public override void AfterStep(MachineWorld world, float delta)
    {
        var settings = Assistance(world.Precision);
        foreach (var body in world.Bodies)
        {
            if (!body.Visible) continue;
            var local = ToLocal(body.Position);
            var inside = Mathf.Abs(local.X) < .66f && Mathf.Abs(local.Z) < .66f &&
                         local.Y > -.4f && local.Y < .45f + settings.CaptureMargin;
            if (inside && body.Velocity.Length() < settings.CaptureSpeed)
            {
                _residence[body.Uid] = _residence.GetValueOrDefault(body.Uid) + delta;
                if (_residence[body.Uid] >= settings.CaptureDwell)
                {
                    world.Events.TryAdd(new MachineEvent(MachineEventKind.Captured, Uid, body.Uid), world.Ticks);
                    Active = true;
                }
            }
            else _residence[body.Uid] = 0;
        }
        if (Active) ((StandardMaterial3D)_rim.MaterialOverride).AlbedoColor = Colors.White;
    }
}
