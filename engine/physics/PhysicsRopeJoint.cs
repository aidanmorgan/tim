using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct RopeAnchor
{
    public PhysicsBody Body { get; }
    public CollisionVector LocalPosition { get; }
    public CollisionVector Position=>Body.Pose.TransformPoint(LocalPosition);
    public RopeAnchor(PhysicsBody body,CollisionVector localPosition)
    {
        ArgumentNullException.ThrowIfNull(body);
        if(!localPosition.IsFinite) throw new ArgumentException("Rope attachment must be finite.");
        Body=body; LocalPosition=localPosition;
    }
}

/// <summary>Ordered point attachments. Consecutive points on the same rigid body
/// contribute a constant length; other spans exert reactions on both attachments.
/// This is point-guide geometry, not a round pulley sheave approximation.</summary>
public sealed class RopeRoute
{
    private readonly RopeAnchor[] _anchors;
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<RopeAnchor> Anchors=>_anchors;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public RopeRoute(IEnumerable<RopeAnchor> anchors)
    {
        ArgumentNullException.ThrowIfNull(anchors); _anchors=anchors.ToArray();
        if(_anchors.Length<2) throw new ArgumentException("Rope route needs at least two attachments.");
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var anchor in _anchors)
        {
            ArgumentNullException.ThrowIfNull(anchor.Body);
            if(!anchor.LocalPosition.IsFinite) throw new ArgumentException("Rope attachment must be finite.");
            if(bodies.TryGetValue(anchor.Body.Id,out var prior)&&prior!=anchor.Body)
                throw new ArgumentException("Rope identity refers to multiple body states.");
            bodies[anchor.Body.Id]=anchor.Body;
        }
        _bodies=bodies.Values.OrderBy(b=>b.Id.Index).ToArray();
    }
    public double CurrentLength
    {
        get
        {
            double length=0;
            for(var i=1;i<_anchors.Length;i++)
            {
                var a=_anchors[i-1]; var b=_anchors[i];
                length+=(a.Body==b.Body?a.LocalPosition-b.LocalPosition:a.Position-b.Position).Length;
            }
            if(!double.IsFinite(length)) throw new InvalidOperationException("Rope length exceeds numeric range.");
            return length;
        }
    }
    public PositionEquation Equation(double maximumLength)
    {
        if(!double.IsFinite(maximumLength)||maximumLength<=0) throw new ArgumentOutOfRangeException(nameof(maximumLength));
        var terms=new List<ConstraintTerm>();
        for(var i=1;i<_anchors.Length;i++)
        {
            var a=_anchors[i-1]; var b=_anchors[i];
            if(a.Body==b.Body) continue; // Rigidly attached segment length is invariant.
            var delta=a.Position-b.Position; var distance=delta.Length;
            if(!double.IsFinite(distance)||distance==0)
                throw new InvalidOperationException("An active rope span requires distinct attachment positions.");
            var axis=delta/distance;
            terms.Add(new(a.Body,axis,CollisionVector.Cross(a.Position-a.Body.Center,axis)));
            terms.Add(new(b.Body,-axis,-CollisionVector.Cross(b.Position-b.Body.Center,axis)));
        }
        if(terms.Count==0) throw new InvalidOperationException("A rigidly fixed route has no movable equation.");
        return new(new(terms.ToArray()),CurrentLength-maximumLength);
    }
}

public sealed class PhysicsRopeJoint : PhysicsJoint
{
    public RopeRoute Route { get; }
    public double MaximumLength { get; }
    public PhysicsRopeJoint(PhysicsJointId id,RopeRoute route,double maximumLength,ConnectedBodyCollision collision)
        :base(id,(route??throw new ArgumentNullException(nameof(route))).Bodies.ToArray(),collision)
    {
        if(!double.IsFinite(maximumLength)||maximumLength<=0) throw new ArgumentException("Rope length must be finite and positive.");
        Route=route; MaximumLength=maximumLength;
    }
    public override IReadOnlyList<IImpulseConstraint> VelocityConstraints(double activationTolerance)
    {
        if(!double.IsFinite(activationTolerance)||activationTolerance<=0||MaximumLength<=activationTolerance)
            throw new ArgumentOutOfRangeException(nameof(activationTolerance));
        if(Route.CurrentLength<MaximumLength-activationTolerance) return [];
        return [new ImpulseConstraint(Route.Equation(MaximumLength).Gradient,0,double.NegativeInfinity,0)];
    }
    public override JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance)=>
        JointBoundarySweep.Rope(this,paths,duration,tolerance);
    public override double Error(double queryTolerance)=>Math.Max(0,Route.CurrentLength-MaximumLength);
    public override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(Error(tolerance)<=tolerance) return;
        PositionEquations.Project([Route.Equation(MaximumLength)],projector);
    }
}
