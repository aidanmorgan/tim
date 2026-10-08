using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum ContactPersistence { New, Persisting }
public enum ContactPairPhase { Idle, Prepared, WarmStarted, Faulted }
public readonly record struct ContactPointId
{
    public long Value { get; }
    public ContactPointId(long value)
    {
        if(value<0) throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct ContactMaterial
{
    public double Restitution { get; }
    public double BounceThreshold { get; }
    public double Friction { get; }
    public ContactMaterial(double restitution,double bounceThreshold,double friction)
    {
        if(!double.IsFinite(restitution)||restitution<0||restitution>1||
            !double.IsFinite(bounceThreshold)||bounceThreshold<0||!double.IsFinite(friction)||friction<0)
            throw new ArgumentException("Contact material values must be finite and physically valid.");
        Restitution=restitution; BounceThreshold=bounceThreshold; Friction=friction;
    }

    /// <summary>Each surface retains a fraction of normal impact speed. Either
    /// surface can absorb it completely; neither can override the other's loss.
    /// Friction uses the symmetric geometric mean; both bounce thresholds apply.</summary>
    public static ContactMaterial Combine(ContactMaterial a,ContactMaterial b)=>new(
        a.Restitution*b.Restitution,Math.Max(a.BounceThreshold,b.BounceThreshold),
        Math.Sqrt(a.Friction)*Math.Sqrt(b.Friction));
}
public readonly record struct CachedContactPoint(ContactPointId Id,CollisionVector LocalA,CollisionVector LocalB,
    CollisionVector NormalInA,ContactImpulse ImpulseInA);
public readonly record struct PreparedContactPoint(ContactPointId Id,ContactPersistence Persistence,
    CollisionVector LocalA,CollisionVector LocalB,CollisionVector NormalInA,ContactConstraint Constraint);

/// <summary>Persistent state for exactly one declared convex child pair.
/// Prepare every pair before warm starting any pair, so restitution sees the
/// same pre-solve velocities. Complete only after the global constraint solve.</summary>
public sealed class PersistentContactPair
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    private readonly DrivenSurface[] _surfaces;
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public ContactPairPhase Phase { get; private set; }
    public ReadOnlySpan<CachedContactPoint> Contacts=>_contacts;
    public ReadOnlySpan<PreparedContactPoint> PreparedContacts=>_prepared;
    private readonly ConvexInstance _shapeA,_shapeB;
    private readonly ContactMaterial _material;
    private readonly double _matchDistanceSquared,_minimumNormalDot;
    private CachedContactPoint[] _contacts=[];
    private PreparedContactPoint[] _prepared=[];
    private ContactImpulse[] _seeds=[];
    private long _nextId,_preparedNextId;
    private double _duration,_preparedDuration;
    private ulong _revisionA,_revisionB;

    public PersistentContactPair(PhysicsBody a,ConvexInstance shapeA,PhysicsBody b,ConvexInstance shapeB,
        ContactMaterial material,double anchorMatchDistance,double maximumNormalAngle,IEnumerable<DrivenSurface> surfaces)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id) throw new ArgumentException("A contact pair needs two distinct bodies.");
        if(shapeA.Geometry is null||shapeB.Geometry is null) throw new ArgumentException("Both collider declarations are required.");
        if(!double.IsFinite(anchorMatchDistance)||anchorMatchDistance<=0||
            !double.IsFinite(anchorMatchDistance*anchorMatchDistance)||
            !double.IsFinite(maximumNormalAngle)||maximumNormalAngle<0||maximumNormalAngle>=Math.PI/2)
            throw new ArgumentOutOfRangeException(nameof(anchorMatchDistance));
        ArgumentNullException.ThrowIfNull(surfaces);
        _surfaces=surfaces.ToArray();
        if(_surfaces.Any(s=>s is null||s.Carrier!=a&&s.Carrier!=b))
            throw new ArgumentException("Contact surfaces must belong to a geometric owner.");
        _bodies=new ConstraintGradient([new(a,default,default),new(b,default,default),
            .._surfaces.SelectMany(s=>s.Drive.Bodies.ToArray()).Select(body=>new ConstraintTerm(body,default,default))]).Bodies.ToArray();
        A=a; B=b; _shapeA=shapeA; _shapeB=shapeB; _material=material;
        _matchDistanceSquared=anchorMatchDistance*anchorMatchDistance; _minimumNormalDot=Math.Cos(maximumNormalAngle);
    }

    /// <summary>Coupled normal/Coulomb contacts at a prediction stage. Capture
    /// the source friction regime before predicting; scratch accelerations must
    /// not masquerade as physical slip or change that regime during iteration.</summary>
    public ContactForce[] AccelerationContacts(
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> states,double velocityTolerance,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double time)
    {
        Require(ContactPairPhase.Idle);
        ArgumentNullException.ThrowIfNull(states);
        if(!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(velocityTolerance));
        if(!states.TryGetValue(A.Id,out var a)||a is null||a.Id!=A.Id||
            !states.TryGetValue(B.Id,out var b)||b is null||b.Id!=B.Id)
            throw new ArgumentException("Contact prediction requires both declared bodies.");
        var source=ContactGap.Query(A,_shapeA,B,_shapeB,ConvexSweep.ContactDistance,ConvexDistance.DefaultTolerance);
        var rows=new List<ContactForce>();
        foreach(var gap in source)
            if(gap.Rate<=velocityTolerance)
            {
                var material=new MaterialContact(gap,_surfaces);
                rows.Add(material.Rebind(states).Force(_material.Friction,
                    material.Slip.Length>velocityTolerance?FrictionRegime.Sliding:FrictionRegime.Sticking,paths,time));
            }
        return rows.ToArray();
    }

    internal MaterialContact[] SlidingFeatures(double velocityTolerance)
    {
        Require(ContactPairPhase.Idle);
        if(!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(velocityTolerance));
        if(_material.Friction==0) return [];
        var result=new List<MaterialContact>();
        foreach(var gap in ContactGap.Query(A,_shapeA,B,_shapeB,ConvexSweep.ContactDistance,ConvexDistance.DefaultTolerance))
        {
            var material=new MaterialContact(gap,_surfaces);
            if(gap.Rate<=velocityTolerance&&material.Slip.Length>velocityTolerance) result.Add(material);
        }
        return result.ToArray();
    }

    public void Prepare(double duration)
    {
        Require(ContactPairPhase.Idle);
        if(!double.IsFinite(duration)||duration<=0) throw new ArgumentOutOfRangeException(nameof(duration));
        var manifold=ContactManifold.Query(new ConvexPose(_shapeA,A.Pose),
            new ConvexPose(_shapeB,B.Pose));
        var points=manifold.Points;
        var prepared=new PreparedContactPoint[points.Length]; var seeds=new ContactImpulse[points.Length];
        var used=new bool[_contacts.Length];
        var nextId=_nextId;
        var localNormal=A.Pose.Rotation.Inverse().Apply(manifold.Normal);
        var ratio=_duration==0?0:duration/_duration;
        if(!double.IsFinite(ratio)) throw new InvalidOperationException("Contact step ratio exceeds numeric range.");
        for(var i=0;i<points.Length;i++)
        {
            var point=points[i];
            var localA=A.Pose.InverseTransformPoint(point.PointA); var localB=B.Pose.InverseTransformPoint(point.PointB);
            var match=-1; var best=double.PositiveInfinity;
            for(var j=0;j<_contacts.Length;j++)
            {
                if(used[j]) continue;
                var candidate=_contacts[j];
                var da=(localA-candidate.LocalA).LengthSquared; var db=(localB-candidate.LocalB).LengthSquared;
                if(da>_matchDistanceSquared||db>_matchDistanceSquared||
                    CollisionVector.Dot(localNormal,candidate.NormalInA)<_minimumNormalDot) continue;
                var cost=da+db;
                if(cost<best) { best=cost; match=j; }
            }
            var persistence=match<0?ContactPersistence.New:ContactPersistence.Persisting;
            ContactPointId id;
            if(match<0) { id=new(nextId); nextId=checked(nextId+1); }
            else
            {
                used[match]=true; var prior=_contacts[match]; id=prior.Id;
                seeds[i]=new(prior.ImpulseInA.Normal*ratio,A.Pose.Rotation.Apply(prior.ImpulseInA.Tangent)*ratio);
            }
            // Restitution belongs to the onset of a pair's contact episode, not
            // to every newly clipped corner while the pair is already touching.
            var restitution=_contacts.Length==0?_material.Restitution:0;
            var rigid=ContactKinematics.AtPoint(A,B,(point.PointA+point.PointB)*.5,manifold.Normal);
            var material=MaterialContact.Map(rigid,_surfaces.Where(s=>s.Matches(A,B,manifold.Normal)));
            var constraint=new ContactConstraint(material,restitution,_material.BounceThreshold,_material.Friction);
            prepared[i]=new(id,persistence,localA,localB,localNormal,constraint);
        }
        // Geometry may enumerate the same anchors in a different tangent basis.
        // Stable identities also fix the solver order across those changes.
        Array.Sort(prepared,seeds,Comparer<PreparedContactPoint>.Create((x,y)=>x.Id.Value.CompareTo(y.Id.Value)));
        _prepared=prepared; _seeds=seeds; _preparedDuration=duration; _preparedNextId=nextId;
        _revisionA=A.PoseRevision; _revisionB=B.PoseRevision; Phase=ContactPairPhase.Prepared;
    }

    internal void WarmStart()
    {
        Require(ContactPairPhase.Prepared); ValidatePose();
        Phase=ContactPairPhase.Faulted;
        for(var i=0;i<_prepared.Length;i++) _prepared[i].Constraint.WarmStart(_seeds[i]);
        Phase=ContactPairPhase.WarmStarted;
    }

    public void Complete(double tolerance=1e-8)
    {
        Require(ContactPairPhase.WarmStarted); ValidatePose();
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var contacts=new CachedContactPoint[_prepared.Length];
        for(var i=0;i<_prepared.Length;i++)
        {
            var point=_prepared[i];
            if(point.Constraint.Residual>tolerance) throw new InvalidOperationException("Unsolved contact cannot enter the persistent cache.");
            var impulse=point.Constraint.Impulse;
            contacts[i]=new(point.Id,point.LocalA,point.LocalB,point.NormalInA,
                new(impulse.Normal,A.Pose.Rotation.Inverse().Apply(impulse.Tangent)));
        }
        _contacts=contacts; _duration=_preparedDuration; _nextId=_preparedNextId;
        _prepared=[]; _seeds=[]; Phase=ContactPairPhase.Idle;
    }

    private void Require(ContactPairPhase phase)
    {
        if(Phase!=phase) throw new InvalidOperationException("Invalid persistent-contact lifecycle phase.");
    }
    private void ValidatePose()
    {
        if(A.PoseRevision!=_revisionA||B.PoseRevision!=_revisionB)
            throw new InvalidOperationException("Prepared contacts are stale after a pose change.");
    }

    /// <summary>A newly detected impact invalidates the preceding support
    /// estimate and restitution episode, but never reuses its point identities.</summary>
    public void BeginImpact()
    {
        Require(ContactPairPhase.Idle);
        _contacts=[]; _duration=0;
    }

    public Snapshot Capture()
    {
        Require(ContactPairPhase.Idle);
        return new(this,_bodies.Select(body=>body.Snapshot()).ToArray(),_contacts,_duration,_nextId);
    }
    public void Restore(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if(snapshot.Owner!=this||!_bodies.Select(body=>body.Snapshot()).SequenceEqual(snapshot.Bodies))
            throw new ArgumentException("Restore all material-contact participants before their contact cache.",nameof(snapshot));
        _contacts=snapshot.Contacts.ToArray(); _duration=snapshot.Duration; _nextId=snapshot.NextId;
        _prepared=[]; _seeds=[]; Phase=ContactPairPhase.Idle;
    }

    public sealed class Snapshot
    {
        internal PersistentContactPair Owner { get; }
        internal PhysicsBodySnapshot[] Bodies { get; }
        private readonly CachedContactPoint[] _contacts;
        public ReadOnlySpan<CachedContactPoint> Contacts=>_contacts;
        public double Duration { get; }
        public long NextId { get; }
        internal Snapshot(PersistentContactPair owner,PhysicsBodySnapshot[] bodies,
            CachedContactPoint[] contacts,double duration,long nextId)
        {
            Owner=owner; Bodies=(PhysicsBodySnapshot[])bodies.Clone(); _contacts=(CachedContactPoint[])contacts.Clone(); Duration=duration; NextId=nextId;
        }
    }
}
