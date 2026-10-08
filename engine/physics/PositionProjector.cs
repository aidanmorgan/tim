using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public delegate IEnumerable<ContactPositionConstraint> PositionContactQuery(Func<PhysicsBody,ConfigurationTrajectory> path,double duration);

public readonly record struct BodyCorrection(PhysicsBody Body,CollisionVector Translation,CollisionVector Rotation);

/// <summary>Commits only a continuously certified prefix of each proposed
/// correction. All collider pairs share the same path parameter and fraction.
/// Existing overlap gets one fixed initial-depth budget, never a per-iteration
/// allowance that could accumulate through an obstacle.</summary>
public sealed class PositionProjector
{
    private readonly Dictionary<PhysicsBodyId,PhysicsBody> _bodies;
    private readonly Dictionary<PhysicsBodyId,ulong> _revisions=new();
    private readonly PositionContactQuery _contacts;
    private readonly Dictionary<ContactPositionConstraint,double> _minimum=new();
    private readonly Dictionary<PhysicsBodyId,RigidPose> _initialPoses=new();
    private readonly double _maximumPenetration;
    public int SweepIterations { get; private set; }
    public int ClippedCorrections { get; private set; }
    public PositionProjector(IEnumerable<PhysicsBody> bodies,PositionContactQuery contacts,double maximumPenetration)
    {
        ArgumentNullException.ThrowIfNull(bodies); ArgumentNullException.ThrowIfNull(contacts);
        if(!double.IsFinite(maximumPenetration)||maximumPenetration<=ConvexDistance.DefaultTolerance)
            throw new ArgumentOutOfRangeException(nameof(maximumPenetration));
        _bodies=new();
        foreach(var body in bodies)
        {
            ArgumentNullException.ThrowIfNull(body);
            if(!_bodies.TryAdd(body.Id,body)) throw new ArgumentException("Projection body identities must be unique.");
            _revisions.Add(body.Id,body.PoseRevision);
            _initialPoses.Add(body.Id,body.Pose);
        }
        _contacts=contacts; _maximumPenetration=maximumPenetration;
    }
    private double Minimum(ContactPositionConstraint contact)
    {
        ValidateBody(contact.A); ValidateBody(contact.B);
        if(_minimum.TryGetValue(contact,out var value)) return value;
        var a=contact.MotionA(new ConfigurationTrajectory(_initialPoses[contact.A.Id],default,default)).At(0);
        var b=contact.MotionB(new ConfigurationTrajectory(_initialPoses[contact.B.Id],default,default)).At(0);
        var initial=ConvexSeparation.Query(a,b,ConvexDistance.DefaultTolerance*.25);
        value=initial.LowerBound < -_maximumPenetration?
            initial.LowerBound-4*ConvexDistance.DefaultTolerance:-_maximumPenetration;
        _minimum.Add(contact,value);
        return value;
    }
    internal void ValidateBody(PhysicsBody body)
    {
        if(body is null||!_bodies.TryGetValue(body.Id,out var owned)||owned!=body)
            throw new ArgumentException("Correction refers to a foreign body state.");
        if(body.PoseRevision!=_revisions[body.Id]) throw new InvalidOperationException("Projection geometry changed outside its correction transaction.");
    }
    internal double Apply(ReadOnlySpan<BodyCorrection> corrections)
    {
        foreach(var body in _bodies.Values) ValidateBody(body);
        var paths=new Dictionary<PhysicsBodyId,ConfigurationTrajectory>();
        var states=new PhysicsBodySnapshot[corrections.Length];
        var changed=false;
        for(var i=0;i<corrections.Length;i++)
        {
            var correction=corrections[i]; ValidateBody(correction.Body);
            if(!correction.Translation.IsFinite||!correction.Rotation.IsFinite)
                throw new ArgumentException("Correction increments must be finite.");
            var moving=correction.Translation!=default||correction.Rotation!=default;
            if(moving&&correction.Body.MotionType!=PhysicsMotionType.Dynamic)
                throw new ArgumentException("Only dynamic bodies can receive configuration corrections.");
            if(!paths.TryAdd(correction.Body.Id,new(correction.Body.Pose,correction.Translation,correction.Rotation)))
                throw new ArgumentException("Correction batch repeats a body.");
            states[i]=correction.Body.Snapshot(); changed|=moving;
        }
        if(!changed) return 1;
        ConfigurationTrajectory Path(PhysicsBody body)=>paths.TryGetValue(body.Id,out var path)?
            path:new(body.Pose,default,default);
        double fraction=1;
        foreach(var contact in _contacts(Path,fraction))
        {
            ArgumentNullException.ThrowIfNull(contact);
            ValidateBody(contact.A); ValidateBody(contact.B);
            if(!paths.ContainsKey(contact.A.Id)&&!paths.ContainsKey(contact.B.Id)) continue;
            var a=contact.MotionA(Path(contact.A)); var b=contact.MotionB(Path(contact.B));
            if(CollisionBounds.Swept(a,fraction).DistanceLowerBound(CollisionBounds.Swept(b,fraction))>0) continue;
            var hit=ConvexSweep.Cast(a,b,fraction,Minimum(contact));
            SweepIterations+=hit.Iterations;
            if(hit.Status!=ConvexSweepStatus.Clear) fraction=Math.Min(fraction,hit.Time);
        }
        if(fraction<1) ClippedCorrections++;
        if(fraction==0) return 0;
        try
        {
            for(var i=0;i<corrections.Length;i++)
            {
                var correction=corrections[i];
                if(correction.Body.MotionType==PhysicsMotionType.Dynamic)
                    correction.Body.CorrectPose(correction.Translation*fraction,correction.Rotation*fraction);
            }
        }
        catch
        {
            for(var i=0;i<corrections.Length;i++)
            {
                corrections[i].Body.RestoreState(states[i]);
                _revisions[corrections[i].Body.Id]=corrections[i].Body.PoseRevision;
            }
            throw;
        }
        foreach(var correction in corrections) _revisions[correction.Body.Id]=correction.Body.PoseRevision;
        return fraction;
    }
}
