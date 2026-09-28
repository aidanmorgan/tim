using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum PhysicsWorldPhase { Idle, Stepping }
public sealed record PhysicsObject
{
    public PhysicsBody Body { get; }
    public CompoundGeometry Geometry { get; }
    public ContactMaterial Material { get; }
    public PhysicsObject(PhysicsBody body,CompoundGeometry geometry,ContactMaterial material)
    {
        ArgumentNullException.ThrowIfNull(body); ArgumentNullException.ThrowIfNull(geometry);
        Body=body; Geometry=geometry; Material=material;
    }
}
public sealed class PhysicsWorldSettings
{
    public CollisionVector Gravity { get; }
    public double MaximumStep { get; }
    public double MaximumPenetration { get; }
    public double PositionTolerance { get; }
    public double VelocityTolerance { get; }
    public int MaximumEvents { get; }
    public int MaximumSubsteps { get; }
    public PhysicsWorldSettings(CollisionVector gravity,double maximumStep=1.0/120,double maximumPenetration=1e-6,
        double positionTolerance=1e-7,double velocityTolerance=1e-8,int maximumEvents=256,int maximumSubsteps=4096)
    {
        if(!gravity.IsFinite||!double.IsFinite(maximumStep)||maximumStep<=0||
            !double.IsFinite(positionTolerance)||positionTolerance<ConvexDistance.DefaultTolerance||
            !double.IsFinite(maximumPenetration)||maximumPenetration<=positionTolerance+ConvexDistance.DefaultTolerance||
            !double.IsFinite(velocityTolerance)||velocityTolerance<=0||maximumEvents<1||maximumSubsteps<1)
            throw new ArgumentException("World limits must be finite, positive and precision-consistent.");
        Gravity=gravity; MaximumStep=maximumStep; MaximumPenetration=maximumPenetration;
        PositionTolerance=positionTolerance; VelocityTolerance=velocityTolerance;
        MaximumEvents=maximumEvents; MaximumSubsteps=maximumSubsteps;
    }
}
public readonly record struct ColliderPairKey(PhysicsBodyId A,ColliderChildId ChildA,PhysicsBodyId B,ColliderChildId ChildB);
public readonly record struct PhysicsImpact(ColliderPairKey Pair,double Time,ConvexSeparationResult Separation);
public sealed record PhysicsStepResult(int Substeps,int Events,int SweepIterations,int VelocityIterations,int PositionIterations);

/// <summary>World-owned bodies, convex-child contact graph and shared event clock.
/// Topology is immutable for this world instance. Every successful motion interval
/// is certified before all bodies commit it; a failed step restores body/cache state.</summary>
public sealed class PhysicsWorld
{
    private sealed record Pair(int A,int B,ColliderChildId ChildA,ColliderChildId ChildB,
        ConvexInstance ShapeA,ConvexInstance ShapeB,PersistentContactPair Contact,ContactPositionConstraint Position,ColliderPairKey Key);
    private readonly PhysicsObject[] _objects;
    private readonly Pair[] _pairs;
    private readonly IPositionConstraint[] _positions;
    private readonly PhysicsWorldSettings _settings;
    private PhysicsImpact[] _impacts=[];
    public PhysicsWorldPhase Phase { get; private set; }
    public double Time { get; private set; }
    public ulong StepIndex { get; private set; }
    public ReadOnlySpan<PhysicsImpact> Impacts=>_impacts;

    public PhysicsWorld(IEnumerable<PhysicsObject> objects,PhysicsWorldSettings settings)
    {
        ArgumentNullException.ThrowIfNull(objects); ArgumentNullException.ThrowIfNull(settings);
        var supplied=objects.ToArray();
        if(supplied.Any(o=>o is null)) throw new ArgumentException("World objects cannot be null.",nameof(objects));
        _objects=supplied.OrderBy(o=>o.Body.Id.Index).ToArray(); _settings=settings;
        if(_objects.Select(o=>o.Body.Id).Distinct().Count()!=_objects.Length) throw new ArgumentException("World body identities must be unique.");
        var pairs=new List<Pair>();
        for(var a=0;a<_objects.Length;a++)
        for(var b=a+1;b<_objects.Length;b++)
        {
            var first=_objects[a]; var second=_objects[b];
            // Two prescribed/immovable bodies cannot exchange a dynamic impulse.
            if(first.Body.MotionType!=PhysicsMotionType.Dynamic&&second.Body.MotionType!=PhysicsMotionType.Dynamic) continue;
            var material=new ContactMaterial(Math.Max(first.Material.Restitution,second.Material.Restitution),
                Math.Max(first.Material.BounceThreshold,second.Material.BounceThreshold),
                Math.Sqrt(first.Material.Friction)*Math.Sqrt(second.Material.Friction));
            for(var i=0;i<first.Geometry.Count;i++)
            for(var j=0;j<second.Geometry.Count;j++)
            {
                var ia=new ColliderChildId(i); var ib=new ColliderChildId(j);
                var shapeA=first.Geometry[ia]; var shapeB=second.Geometry[ib];
                pairs.Add(new(a,b,ia,ib,shapeA,shapeB,new(first.Body,shapeA,second.Body,shapeB,material,.01,.2),
                    new(first.Body,shapeA,second.Body,shapeB),new(first.Body.Id,ia,second.Body.Id,ib)));
            }
        }
        _pairs=pairs.ToArray(); _positions=_pairs.Select(p=>(IPositionConstraint)p.Position).ToArray();
    }

    public PhysicsStepResult Step(double duration)
    {
        RequireIdle();
        if(!double.IsFinite(duration)||duration<=0||!double.IsFinite(Time+duration)||Time+duration<=Time)
            throw new ArgumentOutOfRangeException(nameof(duration));
        var countValue=Math.Ceiling(duration/_settings.MaximumStep);
        if(countValue<1||countValue>_settings.MaximumSubsteps) throw new ArgumentOutOfRangeException(nameof(duration),"World substep budget exceeded.");
        var count=(int)countValue; var step=duration/count;
        var nextIndex=checked(StepIndex+1);
        var before=Capture();
        Phase=PhysicsWorldPhase.Stepping;
        var impacts=new List<PhysicsImpact>();
        var events=0; var sweeps=0; var velocityIterations=0; var positionIterations=0;
        try
        {
            for(var substep=0;substep<count;substep++)
            {
                foreach(var item in _objects)
                    if(item.Body.MotionType==PhysicsMotionType.Dynamic)
                        item.Body.ApplyWrench(_settings.Gravity/item.Body.InverseMass,default,step);
                double elapsed=0;
                while(true)
                {
                    positionIterations+=PositionSolver.Solve(_positions,_settings.PositionTolerance).Iterations;
                    foreach(var pair in _pairs) pair.Contact.Prepare(step);
                    var constraints=_pairs.SelectMany(p=>p.Contact.PreparedContacts.ToArray()).Select(p=>(IImpulseConstraint)p.Constraint).ToArray();
                    foreach(var pair in _pairs) pair.Contact.WarmStart();
                    velocityIterations+=ImpulseSolver.Solve(constraints,tolerance:_settings.VelocityTolerance).Iterations;
                    foreach(var pair in _pairs) pair.Contact.Complete(_settings.VelocityTolerance);
                    if(elapsed>=step) break;
                    var remaining=step-elapsed;
                    var paths=_objects.Select(o=>o.Body.CreateTrajectory(remaining)).ToArray();
                    var travel=remaining; Pair? hitPair=null; ConvexSweepResult earliest=default;
                    foreach(var pair in _pairs)
                    {
                        var a=new ConvexMotion(pair.ShapeA,paths[pair.A]); var b=new ConvexMotion(pair.ShapeB,paths[pair.B]);
                        var boundary=pair.Contact.Contacts.Length==0?
                            ConvexSweep.ContactDistance-ConvexDistance.DefaultTolerance:-_settings.MaximumPenetration;
                        if(CollisionBounds.Swept(a,travel).DistanceLowerBound(CollisionBounds.Swept(b,travel))>
                            Math.Max(0,boundary)+ConvexDistance.DefaultTolerance) continue;
                        var hit=ConvexSweep.Cast(a,b,travel,boundary); sweeps+=hit.Iterations;
                        if(hit.Status==ConvexSweepStatus.Clear) continue;
                        if(hitPair is not null&&hit.Time>=travel) continue;
                        travel=hit.Time; earliest=hit; hitPair=pair;
                    }
                    if(travel<=0||elapsed+travel<=elapsed)
                        throw new InvalidOperationException("World collision event made no temporal progress.");
                    for(var i=0;i<_objects.Length;i++) _objects[i].Body.Advance(paths[i],travel);
                    elapsed=travel>=remaining?step:elapsed+travel;
                    if(hitPair is null) break;
                    if(++events>_settings.MaximumEvents) throw new InvalidOperationException("World collision event budget exceeded.");
                    impacts.Add(new(hitPair.Key,Time+substep*step+elapsed,earliest.Separation));
                    // A fresh swept impact ends the preceding support estimate.
                    // This also restores restitution after release/re-contact
                    // within one substep, without skipping the previously touching pair.
                    hitPair.Contact.BeginImpact();
                }
            }
            Time=before.Time+duration; StepIndex=nextIndex; _impacts=impacts.ToArray();
            Phase=PhysicsWorldPhase.Idle;
            return new(count,events,sweeps,velocityIterations,positionIterations);
        }
        catch
        {
            RestoreState(before);
            Phase=PhysicsWorldPhase.Idle;
            throw;
        }
    }

    private void RequireIdle()
    {
        if(Phase!=PhysicsWorldPhase.Idle) throw new InvalidOperationException("World mutation requires an idle phase.");
    }
    public Snapshot Capture()
    {
        RequireIdle();
        return new(this,_objects.Select(o=>o.Body.Snapshot()).ToArray(),_pairs.Select(p=>p.Contact.Capture()).ToArray(),
            Time,StepIndex,_impacts);
    }
    public void Restore(Snapshot snapshot)
    {
        RequireIdle(); ArgumentNullException.ThrowIfNull(snapshot);
        if(snapshot.Owner!=this) throw new ArgumentException("Snapshot belongs to another world.",nameof(snapshot));
        RestoreState(snapshot);
    }
    private void RestoreState(Snapshot snapshot)
    {
        for(var i=0;i<_objects.Length;i++) _objects[i].Body.Restore(snapshot.Bodies[i]);
        for(var i=0;i<_pairs.Length;i++) _pairs[i].Contact.Restore(snapshot.Pairs[i]);
        Time=snapshot.Time; StepIndex=snapshot.StepIndex; _impacts=(PhysicsImpact[])snapshot.ImpactData.Clone();
    }
    public sealed class Snapshot
    {
        internal PhysicsWorld Owner { get; }
        internal PhysicsBodySnapshot[] Bodies { get; }
        internal PersistentContactPair.Snapshot[] Pairs { get; }
        internal PhysicsImpact[] ImpactData { get; }
        public double Time { get; }
        public ulong StepIndex { get; }
        public ReadOnlySpan<PhysicsBodySnapshot> BodyStates=>Bodies;
        internal Snapshot(PhysicsWorld owner,PhysicsBodySnapshot[] bodies,PersistentContactPair.Snapshot[] pairs,
            double time,ulong stepIndex,PhysicsImpact[] impacts)
        {
            Owner=owner; Bodies=bodies; Pairs=pairs; Time=time; StepIndex=stepIndex; ImpactData=(PhysicsImpact[])impacts.Clone();
        }
    }
}
