using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum GateState { Closed, Opening, Open, Closing, Blocked }

/// <summary>Electric retracting shutter. The blade's visible pose is also its collision pose.</summary>
public partial class PoweredGatePart : MachinePart, ITubePart
{
    public const float Stroke = 1.55f;
    public float Opening { get; private set; }
    public float BladeSpeed { get; private set; }
    public GateState State { get; private set; }
    private int _bladeIndex;
    private MeshInstance3D _blade = null!;
    private StandardMaterial3D _indicator = null!;
    private static readonly Vector3 BladeHalf = new(.06f, .72f, .72f);
    public override float SurfaceBounce => .1f;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketIds.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(0, 1.65f, .92f))];
    public IEnumerable<TubeMouth> Mouths =>
    [
        new(TubeMouthId.Start, Vector3.Left * .49f, Vector3.Left, PipePart.BoreRadius),
        new(TubeMouthId.End, Vector3.Right * .49f, Vector3.Right, PipePart.BoreRadius)
    ];

    protected override void Build()
    {
        PickRadius = 1.5f;
        PipeArt.Cylinder(Visual, Transform3D.Identity, .4f, PipePart.BoreRadius, .70f, new(.40f,.72f,.79f,.16f), false);
        Tubes.Add(new(Transform3D.Identity, .4f, PipePart.BoreRadius, .70f, false));
        foreach (var x in new[] { -.4f, .4f })
        {
            var pose = new Transform3D(Basis.Identity, new(x,0,0));
            PipeArt.Cylinder(Visual, pose, .09f, PipePart.BoreRadius, .78f, new("#fff8e9"), true);
            Tubes.Add(new(pose, .09f, PipePart.BoreRadius, .78f, true));
        }
        AddBox(new(0,1.65f,0), new(.8f,.65f,1.8f), new("#293954"));
        foreach (var z in new[] { -.82f, .82f })
            AddBox(new(0,.65f,z), new(.24f,1.7f,.12f), new("#fff8e9"));
        _bladeIndex = Boxes.Count;
        AddBox(Vector3.Zero, BladeHalf * 2, new("#f7cb52"), false);
        _blade = PartArt.Box(Visual, BladeHalf * 2, new("#f7cb52"));
        PartArt.Sphere(Visual, .09f, new("#e8b764"), new(0,1.65f,.92f));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual, .07f, new("#556573"),
            new(.42f,1.65f,.55f)).MaterialOverride;
    }

    public override void BeforeStep(MachineWorld world, float delta)
    {
        var powered = HasElectricalPower(SocketIds.PowerIn);
        var target = powered ? Stroke : 0;
        var distance = target - Opening;
        // Acceleration and stopping-distance limits keep motion continuous at both endpoints.
        var targetSpeed = Mathf.Sign(distance) * Mathf.Min(2.8f, Mathf.Sqrt(2 * 14 * Mathf.Abs(distance)));
        BladeSpeed = Mathf.MoveToward(BladeSpeed, targetSpeed, 14 * delta);
        var next = Mathf.Clamp(Opening + BladeSpeed * delta, 0, Stroke);
        var blocked = next < Opening && Obstructed(world, next);
        if (blocked) { next = Opening; BladeSpeed = 0; }
        Opening = next;
        if (Mathf.Abs(target - Opening) < .0001f) { Opening = target; BladeSpeed = 0; }
        _blade.Position = Vector3.Up * Opening;
        Boxes[_bladeIndex] = new(_blade.Position, BladeHalf);
        Active = powered;
        State = blocked ? GateState.Blocked : Opening == 0 ? GateState.Closed :
            Opening == Stroke ? GateState.Open : powered ? GateState.Opening : GateState.Closing;
        _indicator.AlbedoColor = State == GateState.Blocked ? new("#e8b764") :
            powered ? new("#f7cb52") : new("#556573");
        if (powered) world.Events.TryAdd(new MachineEvent(MachineEventKind.Powered, Uid), world.Ticks);
    }

    private bool Obstructed(MachineWorld world, float opening)
    {
        var inverse = Transform.AffineInverse();
        foreach (var body in world.Bodies)
        {
            if (!body.Visible) continue;
            var local = inverse * body.Position - Vector3.Up * opening;
            var closest = local.Clamp(-BladeHalf, BladeHalf);
            if (local.DistanceSquaredTo(closest) <= body.Radius * body.Radius) return true;
        }
        return false;
    }
}
