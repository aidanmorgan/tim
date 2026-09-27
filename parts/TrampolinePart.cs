using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum ElasticContactPhase { Compressing, Returning, Settled }
public enum TrampolineParameter { Tension, DampingRatio }

/// <summary>Finite unilateral contact springs, not a powered launcher or a full cloth solver.</summary>
public partial class TrampolinePart : MachinePart
{
    public const string CatalogId = "trampoline";
    public const float RestHeight = .2f;
    public const float MaximumStroke = .65f;
    public static readonly Vector2 BedHalf = new(1.2f, .9f);
    private sealed class Contact
    {
        public Vector2 At;
        public float Depth;
        public float Radius;
        public ElasticContactPhase Phase;
    }
    private readonly Dictionary<MachinePart, Contact> _contacts = new();
    private MeshInstance3D _membrane = null!;
    private bool _meshDirty;
    public int ContactCount => _contacts.Count;
    public int ImpactCount { get; private set; }
    public float Compression => _contacts.Count == 0 ? 0 : _contacts.Values.Max(c => c.Depth);
    public float StoredElasticEnergy => _contacts.Values.Sum(c => .5f * ReadParameter(TrampolineParameter.Tension) * c.Depth * c.Depth);
    public override float SurfaceBounce => 0; // The rigid rim/back absorb; only the membrane stores energy.

    public override void ValidateParameters()
    {
        var tension = ReadParameter(TrampolineParameter.Tension);
        var damping = ReadParameter(TrampolineParameter.DampingRatio);
        if (!float.IsFinite(tension) || tension < 120 || tension > 1200 ||
            !float.IsFinite(damping) || damping < .08f || damping > .8f)
            throw new ArgumentException("Trampoline tension must be 120–1200 and damping ratio 0.08–0.8.");
    }

    public override void BeforeStep(MachineWorld world, float delta)
    {
        foreach (var body in _contacts.Keys.ToArray())
            if (!body.Visible || !world.Bodies.Contains(body)) { _contacts.Remove(body); _meshDirty = true; }
    }

    public override void ResolveCompliantContact(MachinePart body, MachineWorld world, float delta)
    {
        if (!body.Dynamic || !body.Visible) return;
        var local = Transform.AffineInverse() * body.Position;
        var normal = Basis.Y.Normalized();
        var speed = body.Velocity.Dot(normal);
        var depth = RestHeight - (local.Y - body.Radius);
        // The entire projected body footprint must clear the rigid rim.
        if (Mathf.Abs(local.X) + body.Radius >= BedHalf.X ||
            Mathf.Abs(local.Z) + body.Radius >= BedHalf.Y || depth <= 0)
        {
            if (_contacts.Remove(body)) _meshDirty = true;
            return;
        }
        if (!_contacts.TryGetValue(body, out var contact))
        {
            // Accept entry from the top only, not a side/underside arrival inside the bed volume.
            var previousBottom = local.Y - body.Radius - speed * delta;
            if (speed > 0 || previousBottom < RestHeight - .015f) return;
            contact = new Contact();
            _contacts.Add(body, contact);
            if (-speed >= .45f)
            {
                ImpactCount++;
                world.Events.TryAdd(new(MachineEventKind.Bounced, Uid, body.Uid), world.Ticks);
            }
        }
        contact.At = new(local.X, local.Z);
        contact.Depth = Mathf.Clamp(depth, 0, MaximumStroke);
        _meshDirty = true;
        contact.Radius = body.Radius;
        contact.Phase = Mathf.Abs(speed) < .03f ? ElasticContactPhase.Settled :
            speed < 0 ? ElasticContactPhase.Compressing : ElasticContactPhase.Returning;
        var stiffness = ReadParameter(TrampolineParameter.Tension);
        var damping = 2 * ReadParameter(TrampolineParameter.DampingRatio) * Mathf.Sqrt(stiffness * body.Mass);
        // Unilateral spring/damper: it may push, never pull or impose a launch velocity.
        var force = Mathf.Max(0, stiffness * contact.Depth - damping * speed);
        body.Velocity += normal * (force / body.Mass * delta);
    }

    public override void AfterStep(MachineWorld world, float delta)
    {
        foreach (var (body, contact) in _contacts.ToArray())
        {
            var at = Transform.AffineInverse() * body.Position;
            var depth = RestHeight - (at.Y - body.Radius);
            if (!body.Visible || depth <= 0 ||
                Mathf.Abs(at.X) + body.Radius >= BedHalf.X ||
                Mathf.Abs(at.Z) + body.Radius >= BedHalf.Y)
            {
                _contacts.Remove(body);
                _meshDirty = true;
                continue;
            }
            contact.At = new(at.X, at.Z);
            contact.Depth = Mathf.Clamp(depth, 0, MaximumStroke);
        }
        Active = ContactCount > 0;
    }

    // Local contact patches describe the massless spring approximation. They are not cloth waves.
    public float MembraneHeight(Vector2 at)
    {
        if (Mathf.Abs(at.X) >= BedHalf.X || Mathf.Abs(at.Y) >= BedHalf.Y) return RestHeight;
        var depth = 0f;
        foreach (var contact in _contacts.Values)
        {
            var support = Mathf.Min(BedHalf.X - Mathf.Abs(contact.At.X), BedHalf.Y - Mathf.Abs(contact.At.Y));
            foreach (var other in _contacts.Values)
                if (!ReferenceEquals(contact, other))
                {
                    var separation = contact.At.DistanceTo(other.At) * .49f;
                    if (separation > contact.Radius) support = Mathf.Min(support, separation);
                }
            var distance = at.DistanceTo(contact.At);
            if (support <= 0 || distance >= support) continue;
            var fraction = distance / support;
            var weight = 1 - fraction * fraction;
            var dent = contact.Depth * weight * weight;
            // Keep the rendered skin below the sphere, including deep off-centre contacts.
            if (distance < contact.Radius)
                dent = Mathf.Max(dent, contact.Depth - contact.Radius +
                    Mathf.Sqrt(contact.Radius * contact.Radius - distance * distance));
            else if (support > contact.Radius)
            {
                var t = Mathf.Clamp((distance - contact.Radius) / (support - contact.Radius), 0, 1);
                dent = Mathf.Max(dent, Mathf.Max(0, contact.Depth-contact.Radius) *
                    (1-t)*(1-t)*(1+2*t));
            }
            depth = Mathf.Max(depth, dent);
        }
        return RestHeight - depth;
    }

    private ImmediateMesh MembraneMesh()
    {
        const int columns = 24, rows = 18;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(int x, int z)
        {
            var at = new Vector2(Mathf.Lerp(-BedHalf.X, BedHalf.X, (float)x / columns),
                Mathf.Lerp(-BedHalf.Y, BedHalf.Y, (float)z / rows));
            return new(at.X, MembraneHeight(at), at.Y);
        }
        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            mesh.SurfaceSetNormal((b - a).Cross(c - a).Normalized());
            mesh.SurfaceAddVertex(a); mesh.SurfaceAddVertex(b); mesh.SurfaceAddVertex(c);
        }
        for (var x = 0; x < columns; x++)
        for (var z = 0; z < rows; z++)
        {
            var a = Point(x,z); var b = Point(x+1,z);
            var c = Point(x,z+1); var d = Point(x+1,z+1);
            Triangle(a,c,b); Triangle(b,c,d);
        }
        mesh.SurfaceEnd();
        return mesh;
    }

    public override void _Process(double delta)
    {
        if (!_meshDirty) return;
        _meshDirty = false;
        var old = _membrane.Mesh;
        _membrane.Mesh = MembraneMesh();
        old.Dispose();
    }

    protected override void Build()
    {
        PickRadius = 1.5f;
        AddBox(new(0, RestHeight-MaximumStroke-.06f, 0), new(2.56f,.12f,1.96f), new("#293954"));
        foreach (var x in new[] { -1.28f, 1.28f })
            AddBox(new(x,.1f,0), new(.16f,.2f,2.12f), new("#fff8e9"));
        foreach (var z in new[] { -.98f, .98f })
            AddBox(new(0,.1f,z), new(2.4f,.2f,.16f), new("#fff8e9"));
        foreach (var x in new[] { -1.15f, 1.15f })
        foreach (var z in new[] { -.85f, .85f })
            PartArt.Cylinder(Visual,.055f,.6f,new("#e8b764"),new(x,-.2f,z));
        _membrane = PartArt.Mesh(Visual, MembraneMesh(), new("#66b8c9"));
        ((StandardMaterial3D)_membrane.MaterialOverride).CullMode = BaseMaterial3D.CullModeEnum.Disabled;
    }
}
