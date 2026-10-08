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
    public double ConvectiveAcceleration()
    {
        double bias=0;
        for(var i=1;i<_anchors.Length;i++)
        {
            var a=_anchors[i-1]; var b=_anchors[i];
            if(a.Body==b.Body) continue;
            var delta=a.Position-b.Position; var distance=delta.Length;
            if(!double.IsFinite(distance)||distance==0)
                throw new InvalidOperationException("An active rope span requires distinct attachment positions.");
            var axis=delta/distance;
            var velocity=a.Body.PointVelocity(a.Position)-b.Body.PointVelocity(b.Position);
            var ra=a.Position-a.Body.Center; var rb=b.Position-b.Body.Center;
            var wa=a.Body.AngularVelocity; var wb=b.Body.AngularVelocity;
            var centripetal=CollisionVector.Cross(wa,CollisionVector.Cross(wa,ra))-
                CollisionVector.Cross(wb,CollisionVector.Cross(wb,rb));
            var tangent=velocity-axis*CollisionVector.Dot(velocity,axis);
            bias+=CollisionVector.Dot(axis,centripetal)+tangent.LengthSquared/distance;
        }
        if(!double.IsFinite(bias)) throw new InvalidOperationException("Rope acceleration exceeds numeric range.");
        return bias;
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
    public override ReadOnlySpan<PhysicsJoint> Dependencies=>[];
    public override PhysicsJoint Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)=>
        new PhysicsRopeJoint(Id,new(Route.Anchors.ToArray().Select(a=>new RopeAnchor(ReboundBody(a.Body,bodies),a.LocalPosition))),
            MaximumLength,Collision);
    // A rope supplies tension only; it has no unconditional equality row.
    public override IReadOnlyList<ConstraintGradient> BilateralVelocityGradients()=>[];
    public override IReadOnlyList<IImpulseConstraint> UnilateralVelocityConstraints(double activationTolerance)
    {
        if(!double.IsFinite(activationTolerance)||activationTolerance<=0||MaximumLength<=activationTolerance)
            throw new ArgumentOutOfRangeException(nameof(activationTolerance));
        if(Route.CurrentLength<MaximumLength-activationTolerance) return [];
        return [new ImpulseConstraint(Route.Equation(MaximumLength).Gradient,0,double.NegativeInfinity,0)];
    }
    public override IReadOnlyList<ConstraintAcceleration> AccelerationConstraints(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,double positionTolerance,double velocityTolerance)
    {
        if(!double.IsFinite(positionTolerance)||positionTolerance<=0||!double.IsFinite(velocityTolerance)||velocityTolerance<=0)
            throw new ArgumentOutOfRangeException(nameof(positionTolerance));
        if(Route.CurrentLength<MaximumLength-positionTolerance) return [];
        var gradient=Route.Equation(MaximumLength).Gradient;
        if(gradient.Speed < -velocityTolerance) return [];
        var stage=(PhysicsRopeJoint)Rebind(sample);
        return [new(stage.Route.Equation(MaximumLength).Gradient,stage.Route.ConvectiveAcceleration(),AccelerationRelation.Nonpositive)];
    }
    public override JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance,double velocityTolerance)
    {
        ValidatePaths(paths,duration,tolerance,velocityTolerance);
        return new(JointSweepStatus.Clear,duration,null,0);
    }
    public override double Error(double queryTolerance)=>Math.Max(0,Route.CurrentLength-MaximumLength);
    internal override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(Error(tolerance)<=tolerance) return;
        PositionEquations.Project([Route.Equation(MaximumLength)],projector);
    }
}
