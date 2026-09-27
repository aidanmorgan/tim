using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>A round, spring-loaded bumper. Its decorative pulse never changes collision geometry.</summary>
public partial class BumperPart : MachinePart
{
    private readonly Dictionary<string, int> _cooldown = new();
    private MeshInstance3D _pulseRing = null!;
    private StandardMaterial3D _ringMaterial = null!;
    private readonly List<float> _pulseAges = new();
    private float _strength;
    private static readonly Color RestColor = new("#fff0c2");
    public int HitCount { get; private set; }
    public float PulseAmount { get; private set; }
    public override float SurfaceBounce => 1;

    protected override void Build()
    {
        const float radius = .65f;
        PickRadius = .8f;
        _strength = Mathf.Clamp(Parameter("strength", 8), 0, 20);
        Spheres.Add(new(Vector3.Zero, radius));
        var head = PartArt.Sphere(Visual, radius, Definition.Color);
        head.Name = "BumperHead";
        // Decorative pedestal is wholly inside the spherical collision envelope.
        PartArt.Cylinder(Visual, .3f, .12f, new("#273744"), new(0, -.46f, 0));
        PartArt.Cylinder(Visual, .25f, .12f, new("#f7cb52"), new(0, .59f, 0));
        _pulseRing = PartArt.Ring(Visual, .66f, .035f, RestColor);
        _pulseRing.Name = "ImpactRing";
        _ringMaterial = (StandardMaterial3D)_pulseRing.MaterialOverride;
        PartArt.Ring(Visual, .49f, .018f, RestColor, new(0, .43f, 0));
    }

    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (!body.Dynamic || speed < .05f ||
            _cooldown.GetValueOrDefault(body.Uid, -1000) + 18 > world.Ticks) return;
        _cooldown[body.Uid] = world.Ticks;
        var outward = (body.Position - Position).Normalized();
        if (outward.LengthSquared() < .5f) outward = Vector3.Up;
        // Preserve tangential motion and any stronger natural rebound.
        var radialSpeed = body.Velocity.Dot(outward);
        body.Velocity += outward * Mathf.Max(0, _strength - radialSpeed);
        HitCount++;
        _pulseAges.Add(0);
        Active = true;
        world.Events.TryAdd(new MachineEvent(MachineEventKind.Bumped, Uid), world.Ticks);
        world.Events.TryAdd(new MachineEvent(MachineEventKind.Bumped, Uid, body.Uid), world.Ticks);
    }

    public override void _Process(double delta)
    {
        // Presentation time lets the feedback settle even if the goal stops physics.
        var amount = 0f;
        for (var i = _pulseAges.Count - 1; i >= 0; i--)
        {
            _pulseAges[i] += (float)delta;
            if (_pulseAges[i] >= .32f) { _pulseAges.RemoveAt(i); continue; }
            var wave = Mathf.Sin(Mathf.Pi * _pulseAges[i] / .32f);
            amount += wave * wave; // Zero velocity at both ends; overlapping hits do not snap.
        }
        PulseAmount = Mathf.Min(1, amount);
        _pulseRing.Scale = Vector3.One * (1 + .24f * PulseAmount);
        _ringMaterial.AlbedoColor = RestColor.Lerp(new("#f7cb52"), PulseAmount);

    }
}
