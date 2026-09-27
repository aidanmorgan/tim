using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>One activation per forward centre crossing through the physical aperture.
/// A ball must clear the upstream side before it can trigger again.</summary>
public partial class BallDetectorPart : MachinePart, ITubePart
{
    public int CrossingCount { get; private set; }
    public float Pulse { get; private set; }
    private readonly Dictionary<MachinePart, Vector3> _before = new();
    private readonly HashSet<MachinePart> _armed = new();
    private StandardMaterial3D _indicator = null!;
    private const float PulseSeconds = .35f;
    public override bool CanSendActivation => true;
    public override float SurfaceBounce => .15f;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
        [new(SocketId.ActivationOut, ConnectionDomain.Activation, PortDirection.Output, new(0,1.12f,0))];
    public IEnumerable<TubeMouth> Mouths =>
    [
        new(TubeMouthId.Start, Vector3.Left * .28f, Vector3.Left, PipePart.BoreRadius),
        new(TubeMouthId.End, Vector3.Right * .28f, Vector3.Right, PipePart.BoreRadius)
    ];
    protected override void Build()
    {
        PickRadius = 1.15f;
        PipeArt.Cylinder(Visual,Transform3D.Identity,.28f,PipePart.BoreRadius,.82f,new("#fff8e9"),true);
        Tubes.Add(new(Transform3D.Identity,.28f,PipePart.BoreRadius,.82f,true));
        AddBox(new(0,.93f,0),new(.5f,.24f,.5f),new("#66b8c9"));
        PartArt.Line(Visual,new(-.16f,1.065f,-.10f),new(.16f,1.065f,0),new("#293954"),.022f);
        PartArt.Line(Visual,new(.16f,1.065f,0),new(-.16f,1.065f,.10f),new("#293954"),.022f);
        PartArt.Sphere(Visual,.07f,new("#e8b764"),new(0,1.12f,0));
        _indicator = (StandardMaterial3D)PartArt.Sphere(Visual,.075f,new("#556573"),new(0,0,.86f)).MaterialOverride;
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        _before.Clear();
        var inverse = Transform.AffineInverse();
        foreach (var body in world.Bodies)
        {
            if (!body.Visible || body.PhysicsOwner != body) { _armed.Remove(body); continue; }
            var position = inverse * body.Position;
            _before.Add(body,position);
            if (position.X <= -body.Radius - .02f) _armed.Add(body);
        }
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        Pulse = Mathf.Max(0,Pulse-delta/PulseSeconds);
        var inverse = Transform.AffineInverse();
        foreach (var body in world.Bodies)
        {
            if (!body.Visible || !_before.TryGetValue(body,out var before)) continue;
            var after = inverse * body.Position;
            if (before.X >= 0 || after.X < 0 || !_armed.Remove(body)) continue;
            var crossing = before.Lerp(after,-before.X/(after.X-before.X));
            var clearance = PipePart.BoreRadius-body.Radius;
            if (clearance < 0 || crossing.Y*crossing.Y+crossing.Z*crossing.Z > clearance*clearance+.00001f) continue;
            CrossingCount++;
            Pulse = 1;
            world.EmitActivation(this);
        }
        Active = Pulse > 0;
        var eased = Pulse*Pulse*(3-2*Pulse);
        _indicator.AlbedoColor = new Color("#556573").Lerp(new("#f7cb52"),eased);
    }
}
