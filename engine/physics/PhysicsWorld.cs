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
public readonly record struct PhysicsJointStop(PhysicsJointId Joint,JointBoundary Boundary,double Time);
public sealed record PhysicsStepResult(int Substeps,int Events,int SweepIterations,int VelocityIterations,int PositionIterations);

/// <summary>World-owned bodies, convex-child contact graph and shared event clock.
/// Topology is immutable for this world instance. Every successful motion interval
/// is certified before all bodies commit it; a failed step restores body/cache state.</summary>
public sealed class PhysicsWorld
{
    internal sealed record Pair(int A,int B,ColliderChildId ChildA,ColliderChildId ChildB,
        ConvexInstance ShapeA,ConvexInstance ShapeB,PersistentContactPair Contact,ContactPositionConstraint Position,ColliderPairKey Key);
    private readonly PhysicsObject[] _objects;
    private readonly Dictionary<ColliderPairKey,Pair> _pairs=new();
    private readonly (int A,int B)[] _bodyPairs;
    public int RetainedContactPairs=>_pairs.Count;
    private readonly PhysicsJoint[] _joints;
    private readonly int[][] _jointBodies;
    private readonly PhysicsWorldSettings _settings;
    private PhysicsImpact[] _impacts=[];
    private PhysicsJointStop[] _jointStops=[];
    private PhysicsMotorUse[] _motorUse=[];
    public PhysicsWorldPhase Phase { get; private set; }
    public double Time { get; private set; }
    public ulong StepIndex { get; private set; }
    public ReadOnlySpan<PhysicsImpact> Impacts=>_impacts;
    public ReadOnlySpan<PhysicsJointStop> JointStops=>_jointStops;
    public ReadOnlySpan<PhysicsMotorUse> MotorUse=>_motorUse;

    public PhysicsWorld(IEnumerable<PhysicsObject> objects,IEnumerable<PhysicsJoint> joints,PhysicsWorldSettings settings)
    {
        ArgumentNullException.ThrowIfNull(objects); ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(joints);
        var declarations=joints.ToArray();
        if(declarations.Any(j=>j is null)) throw new ArgumentException("World joints cannot be null.",nameof(joints));
        _joints=declarations.OrderBy(j=>j.Id.Index).ToArray();
        if(_joints.Select(j=>j.Id).Distinct().Count()!=_joints.Length) throw new ArgumentException("World joint identities must be unique.");
        var supplied=objects.ToArray();
        if(supplied.Any(o=>o is null)) throw new ArgumentException("World objects cannot be null.",nameof(objects));
        _objects=supplied.OrderBy(o=>o.Body.Id.Index).ToArray(); _settings=settings;
        if(_objects.Select(o=>o.Body.Id).Distinct().Count()!=_objects.Length) throw new ArgumentException("World body identities must be unique.");
        var owned=_objects.ToDictionary(o=>o.Body.Id,o=>o.Body);
        foreach(var joint in _joints)
            foreach(var body in joint.Bodies)
                if(!owned.TryGetValue(body.Id,out var worldBody)||worldBody!=body)
                    throw new ArgumentException("Joint refers to a body state not owned by this world.");
        var indices=_objects.Select((o,i)=>(o.Body.Id,i)).ToDictionary(p=>p.Id,p=>p.i);
        _jointBodies=_joints.Select(j=>j.Bodies.ToArray().Select(b=>indices[b.Id]).ToArray()).ToArray();
        var pairs=new List<(int A,int B)>();
        for(var a=0;a<_objects.Length;a++)
        for(var b=a+1;b<_objects.Length;b++)
        {
            var first=_objects[a]; var second=_objects[b];
            // Two prescribed/immovable bodies cannot exchange a dynamic impulse.
            if(first.Body.MotionType!=PhysicsMotionType.Dynamic&&second.Body.MotionType!=PhysicsMotionType.Dynamic) continue;
            if(_joints.Any(j=>j.Collision==ConnectedBodyCollision.Disabled&&
                j.Connects(first.Body,second.Body))) continue;
            pairs.Add((a,b));
        }
        _bodyPairs=pairs.ToArray();
    }

    private Pair PairFor(int a,int b,ColliderChildId ia,ColliderChildId ib)
    {
        var first=_objects[a]; var second=_objects[b];
        var key=new ColliderPairKey(first.Body.Id,ia,second.Body.Id,ib);
        if(_pairs.TryGetValue(key,out var pair)) return pair;
        var material=new ContactMaterial(Math.Max(first.Material.Restitution,second.Material.Restitution),
            Math.Max(first.Material.BounceThreshold,second.Material.BounceThreshold),
            Math.Sqrt(first.Material.Friction)*Math.Sqrt(second.Material.Friction));
        var shapeA=first.Geometry[ia]; var shapeB=second.Geometry[ib];
        pair=new(a,b,ia,ib,shapeA,shapeB,new(first.Body,shapeA,second.Body,shapeB,material,.01,.2),
            new(first.Body,shapeA,second.Body,shapeB),key);
        _pairs.Add(key,pair);
        return pair;
    }
    private IEnumerable<Pair> CandidatePairs(Func<PhysicsBody,IRigidTrajectory> path,double duration,double margin)
    {
        // Capture each body path once, then reuse immutable local hierarchies.
        var motions=_objects.Select(o=>new CompoundMotion(o.Geometry,path(o.Body))).ToArray();
        foreach(var (a,b) in _bodyPairs)
            foreach(var children in CompoundCollision.Candidates(motions[a],motions[b],duration,margin).Pairs)
                yield return PairFor(a,b,children.A,children.B);
    }
    private IEnumerable<ContactPositionConstraint> PositionContacts(Func<PhysicsBody,ConfigurationTrajectory> path,double duration)=>
        CandidatePairs(body=>path(body),duration,ConvexDistance.DefaultTolerance).Select(p=>p.Position);
    private IEnumerable<IPositionConstraint> Positions()=>PositionContacts(body=>new(body.Pose,default,default),0)
        .Cast<IPositionConstraint>().Concat(_joints);
    private Pair[] CurrentPairs()
    {
        var near=CandidatePairs(body=>body.CreateTrajectory(0),0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance).ToArray();
        // Previously supported pairs must observe release even outside the skin.
        return Order(near.Concat(_pairs.Values.Where(p=>p.Contact.Contacts.Length>0)).Distinct()).ToArray();
    }
    private static IOrderedEnumerable<Pair> Order(IEnumerable<Pair> pairs)=>pairs.OrderBy(p=>p.A).ThenBy(p=>p.B)
        .ThenBy(p=>p.ChildA.Index).ThenBy(p=>p.ChildB.Index);

    public PhysicsStepResult Step(ReadOnlySpan<PhysicsMotorCommand> motors,double duration)
    {
        RequireIdle();
        if(!double.IsFinite(duration)||duration<=0||!double.IsFinite(Time+duration)||Time+duration<=Time)
            throw new ArgumentOutOfRangeException(nameof(duration));
        var countValue=Math.Ceiling(duration/_settings.MaximumStep);
        if(countValue<1||countValue>_settings.MaximumSubsteps) throw new ArgumentOutOfRangeException(nameof(duration),"World substep budget exceeded.");
        var count=(int)countValue; var step=duration/count;
        var commands=motors.ToArray().OrderBy(m=>m.Joint.Index).ToArray();
        if(commands.Select(c=>c.Joint).Distinct().Count()!=commands.Length) throw new ArgumentException("Only one motor command per joint is allowed.");
        var budgets=new PhysicsMotorBudget[commands.Length];
        for(var i=0;i<commands.Length;i++)
        {
            var joint=_joints.SingleOrDefault(j=>j.Id==commands[i].Joint);
            if(joint is not PhysicsFrameJoint frame||frame.Kind==FrameJointKind.BallSocket)
                throw new ArgumentException("Motor command must address a world-owned hinge or slider.");
            budgets[i]=new(frame,commands[i],duration);
        }
        var nextIndex=checked(StepIndex+1);
        var before=Capture();
        Phase=PhysicsWorldPhase.Stepping;
        var impacts=new List<PhysicsImpact>();
        var stops=new List<PhysicsJointStop>();
        var events=0; var sweeps=0; var velocityIterations=0; var positionIterations=0;
        try
        {
            for(var substep=0;substep<count;substep++)
            {
                foreach(var item in _objects)
                    if(item.Body.MotionType==PhysicsMotionType.Dynamic)
                        item.Body.ApplyWrench(_settings.Gravity/item.Body.InverseMass,default,step);
                double elapsed=0;
                var actuationPending=true;
                while(true)
                {
                    var projector=new PositionProjector(_objects.Select(o=>o.Body),PositionContacts,_settings.MaximumPenetration);
                    positionIterations+=PositionSolver.Solve(Positions,projector,_settings.PositionTolerance).Iterations;
                    if(actuationPending)
                    {
                        foreach(var budget in budgets) budget.Apply(step);
                        actuationPending=false;
                    }
                    var currentPairs=CurrentPairs();
                    foreach(var pair in currentPairs) pair.Contact.Prepare(step);
                    var constraints=currentPairs.SelectMany(p=>p.Contact.PreparedContacts.ToArray()).Select(p=>(IImpulseConstraint)p.Constraint)
                        .Concat(_joints.SelectMany(j=>j.VelocityConstraints(_settings.PositionTolerance))).ToArray();
                    foreach(var pair in currentPairs) pair.Contact.WarmStart();
                    velocityIterations+=ImpulseSolver.Solve(constraints,tolerance:_settings.VelocityTolerance).Iterations;
                    foreach(var pair in currentPairs) pair.Contact.Complete(_settings.VelocityTolerance);
                    if(elapsed>=step) break;
                    var remaining=step-elapsed;
                    var paths=_objects.Select(o=>o.Body.CreateTrajectory(remaining)).ToArray();
                    var travel=remaining; Pair? hitPair=null; ConvexSweepResult earliest=default;
                    var pathById=_objects.Select((o,i)=>(o.Body.Id,Path:paths[i])).ToDictionary(p=>p.Id,p=>p.Path);
                    foreach(var pair in CandidatePairs(body=>pathById[body.Id],remaining,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance))
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
                    PhysicsJoint? hitJoint=null; JointBoundary? hitBoundary=null;
                    for(var i=0;i<_joints.Length;i++)
                    {
                        var participants=_jointBodies[i].Select(index=>paths[index]).ToArray();
                        var hit=_joints[i].Sweep(participants,travel,_settings.PositionTolerance);
                        sweeps+=hit.Iterations;
                        if(hit.Status==JointSweepStatus.Clear||(hitPair is not null||hitJoint is not null)&&hit.Time>=travel) continue;
                        travel=hit.Time; hitJoint=_joints[i]; hitBoundary=hit.Boundary; hitPair=null;
                    }
                    if(travel<=0||elapsed+travel<=elapsed)
                        throw new InvalidOperationException("World collision event made no temporal progress.");
                    for(var i=0;i<_objects.Length;i++) _objects[i].Body.Advance(paths[i],travel);
                    elapsed=travel>=remaining?step:elapsed+travel;
                    if(hitPair is null&&hitJoint is null) continue;
                    if(++events>_settings.MaximumEvents) throw new InvalidOperationException("World event budget exceeded.");
                    if(hitJoint is not null)
                    {
                        stops.Add(new(hitJoint.Id,hitBoundary!.Value,Time+substep*step+elapsed));
                        continue;
                    }
                    impacts.Add(new(hitPair!.Key,Time+substep*step+elapsed,earliest.Separation));
                    // A fresh swept impact ends the preceding support estimate.
                    // This also restores restitution after release/re-contact
                    // within one substep, without skipping the previously touching pair.
                    hitPair.Contact.BeginImpact();
                }
            }
            Time=before.Time+duration; StepIndex=nextIndex; _impacts=impacts.ToArray(); _jointStops=stops.ToArray(); _motorUse=budgets.Select(b=>b.Report).ToArray();
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
        return new(this,_objects.Select(o=>o.Body.Snapshot()).ToArray(),Order(_pairs.Values).Select(p=>(p,p.Contact.Capture())).ToArray(),
            Time,StepIndex,_impacts,_jointStops,_motorUse);
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
        _pairs.Clear();
        foreach(var (pair,state) in snapshot.Pairs)
        {
            pair.Contact.Restore(state); _pairs.Add(pair.Key,pair);
        }
        Time=snapshot.Time; StepIndex=snapshot.StepIndex; _impacts=(PhysicsImpact[])snapshot.ImpactData.Clone();
        _jointStops=(PhysicsJointStop[])snapshot.StopData.Clone();
        _motorUse=(PhysicsMotorUse[])snapshot.MotorData.Clone();
    }
    public sealed class Snapshot
    {
        internal PhysicsWorld Owner { get; }
        internal PhysicsBodySnapshot[] Bodies { get; }
        internal (Pair Pair,PersistentContactPair.Snapshot State)[] Pairs { get; }
        internal PhysicsImpact[] ImpactData { get; }
        internal PhysicsJointStop[] StopData { get; }
        internal PhysicsMotorUse[] MotorData { get; }
        public double Time { get; }
        public ulong StepIndex { get; }
        public ReadOnlySpan<PhysicsBodySnapshot> BodyStates=>Bodies;
        internal Snapshot(PhysicsWorld owner,PhysicsBodySnapshot[] bodies,(Pair Pair,PersistentContactPair.Snapshot State)[] pairs,
            double time,ulong stepIndex,PhysicsImpact[] impacts,PhysicsJointStop[] stops,PhysicsMotorUse[] motors)
        {
            Owner=owner; Bodies=bodies; Pairs=pairs; Time=time; StepIndex=stepIndex; ImpactData=(PhysicsImpact[])impacts.Clone(); StopData=(PhysicsJointStop[])stops.Clone(); MotorData=(PhysicsMotorUse[])motors.Clone();
        }
    }
}
