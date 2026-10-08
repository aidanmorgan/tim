using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

public enum TrampolineParameter { Tension, DampingRatio }

/// <summary>Finite unilateral contact springs, not a powered launcher or a full cloth solver.</summary>
public partial class TrampolinePart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<TrampolineParameter>(fields);
    public const string CatalogId = "trampoline";
    public const float RestHeight = .2f;
    public const float MaximumStroke = .65f;
    public static readonly Vector2 BedHalf = new(1.2f, .9f);
    private sealed record MembranePatch(Vector2 At, float Depth, float Radius, SupportFootprint Footprint);
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(TrampolinePart owner) : SimulationTransactionParticipant
    {
        private readonly List<KeyValuePair<SceneBodyKey,MembranePatch>> _patches = new();
        private ulong? _observedStep;
        private int _impactCount;
        private bool _meshDirty;
        protected override void CaptureCheckpoint()
        {
            _patches.Clear(); _patches.AddRange(owner._patches);
            _observedStep=owner._observedStep; _impactCount=owner.ImpactCount;
            _meshDirty=owner._meshDirty;
        }
        protected override void RestoreCheckpoint()
        {
            owner._patches.Clear();
            foreach(var pair in _patches)owner._patches.Add(pair.Key,pair.Value);
            owner._observedStep=_observedStep; owner.ImpactCount=_impactCount;
            owner._meshDirty=_meshDirty;
        }
    }
    private readonly Dictionary<SceneBodyKey, MembranePatch> _patches = new();
    private ulong? _observedStep;
    private MeshInstance3D _membrane = null!;
    private bool _meshDirty;
    public int ContactCount => _patches.Count;
    public int ImpactCount { get; private set; }
    public float Compression => _patches.Count == 0 ? 0 : _patches.Values.Max(c => c.Depth);
    public float StoredElasticEnergy => _patches.Values.Sum(c => .5f * ReadParameter(TrampolineParameter.Tension) * c.Depth * c.Depth);
    public override Physics.ContactMaterial InitialContactMaterial => new(0,.1,.3); // The rigid rim/back absorb; only the membrane stores energy.

    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var tension = parameters.Read(TrampolineParameter.Tension);
        var damping = parameters.Read(TrampolineParameter.DampingRatio);
        if (!float.IsFinite(tension) || tension < 120 || tension > 1200 ||
            !float.IsFinite(damping) || damping < .08f || damping > .8f)
            throw new ArgumentException("Trampoline tension must be 120–1200 and damping ratio 0.08–0.8.");
    }

    public override IReadOnlyList<SceneCompliantSurface> PhysicsCompliantSurfaces=>
        [new(new(this,RootBody),BedHalf.X,BedHalf.Y,RestHeight,MaximumStroke,
            ReadParameter(TrampolineParameter.Tension),ReadParameter(TrampolineParameter.DampingRatio),
            CompliantContactInitialState.Unloaded)];

    public override void ObservePhysics(MachineWorld world,float delta)
    {
        var frame=world.PhysicsAssembly.Body(new(this,RootBody)).Id;
        _patches.Clear();
        foreach(var state in world.Physics.CompliantContacts)
        {
            if(state.Key.Frame!=frame||state.Phase!=CompliantContactPhase.Engaged)continue;
            var key=world.PhysicsAssembly.Key(state.Key.Body);
            var sample=state.Footprint;
            _patches.Add(key,new(new((float)sample.LowestPoint.X,(float)sample.LowestPoint.Z),
                (float)Math.Clamp(RestHeight-sample.LowestPoint.Y,0,MaximumStroke),
                (float)sample.RoundingRadius,sample));
        }
        if(_observedStep!=world.Physics.StepIndex)
        {
            foreach(var entry in world.Physics.CompliantEntries)
            {
                if(entry.Contact.Frame!=frame||entry.ApproachSpeed<.45)continue;
                var owner=world.PhysicsAssembly.Owner(entry.Contact.Body);
                if(owner is null)throw new InvalidOperationException("A membrane payload needs a scene owner.");
                ImpactCount++;
                world.Events.TryAdd(new(MachineEventKind.Bounced,Uid,owner.Uid),world.Ticks);
            }
            _observedStep=world.Physics.StepIndex;
        }
        _meshDirty=true;Active=ContactCount>0;
    }

    // Frame-bounded visual patches describe the massless spring approximation, not cloth waves.
    public float MembraneHeight(Vector2 at)
    {
        if (Mathf.Abs(at.X) >= BedHalf.X || Mathf.Abs(at.Y) >= BedHalf.Y) return RestHeight;
        var depth = 0f;
        foreach (var contact in _patches.Values)
        {
            var offset = at - contact.At;
            var distance = offset.Length();
            // Distance to the fixed frame along this ray, not the nearest edge
            // in every direction: an edge impact can pull the interior fabric down.
            var direction = distance > 0 ? offset / distance : Vector2.Right;
            var reachX = direction.X == 0 ? float.PositiveInfinity :
                (BedHalf.X - Mathf.Sign(direction.X) * contact.At.X) / Mathf.Abs(direction.X);
            var reachZ = direction.Y == 0 ? float.PositiveInfinity :
                (BedHalf.Y - Mathf.Sign(direction.Y) * contact.At.Y) / Mathf.Abs(direction.Y);
            var support = Mathf.Min(reachX, reachZ);
            foreach (var other in _patches.Values)
                if (!ReferenceEquals(contact, other))
                {
                    var separation = contact.At.DistanceTo(other.At) * .49f;
                    if (separation > contact.Radius) support = Mathf.Min(support, separation);
                }
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
            // Flat supporting features need a flat lower bound under their
            // footprint; rounded loads retain the approved curved indentation.
            if(contact.Radius==0&&at.X>=contact.Footprint.MinimumX&&at.X<=contact.Footprint.MaximumX&&
                at.Y>=contact.Footprint.MinimumZ&&at.Y<=contact.Footprint.MaximumZ)
                dent=Mathf.Max(dent,contact.Depth);
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
