using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct JointFrame
{
    public CollisionVector Anchor { get; }
    public RigidRotation Orientation { get; }
    public JointFrame(CollisionVector anchor,RigidRotation orientation)
    {
        if(!anchor.IsFinite||!orientation.IsValid) throw new ArgumentException("Joint frame must be finite and rigid.");
        Anchor=anchor; Orientation=orientation;
    }
}

/// <summary>Build instantaneous joint equations from world-space frames.
/// Local Z is the hinge/slider axis. CorrectionRate has units 1/s; zero gives
/// pure velocity constraints. Position correction can be solved separately with
/// zero pseudo-velocities, without introducing energy into physical velocities.</summary>
public static class JointConstraints
{
    private enum AxisReference { World, BodyB }
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static void Validate(JointFrame a,JointFrame b,double correctionRate)
    {
        if(!a.Orientation.IsValid||!b.Orientation.IsValid||!double.IsFinite(correctionRate)||correctionRate<0)
            throw new ArgumentException("Joint frames and correction rate must be valid.");
    }
    private static ImpulseConstraint Linear(PhysicsBody a,PhysicsBody b,JointFrame fa,JointFrame fb,
        CollisionVector axis,double correctionRate,AxisReference reference=AxisReference.World)=>new(a,b,
            new(axis,CollisionVector.Cross(fa.Anchor-a.Center,axis),
                -axis,-CollisionVector.Cross((reference==AxisReference.BodyB?fa.Anchor:fb.Anchor)-b.Center,axis)),
            -correctionRate*CollisionVector.Dot(fa.Anchor-fb.Anchor,axis),
            double.NegativeInfinity,double.PositiveInfinity);
    private static ImpulseConstraint Angular(PhysicsBody a,PhysicsBody b,CollisionVector axis,double target)=>
        new(a,b,new(default,axis,default,-axis),target,double.NegativeInfinity,double.PositiveInfinity);

    public static IReadOnlyList<IImpulseConstraint> BallSocket(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)
    {
        Validate(fa,fb,correctionRate);
        return [new BilateralConstraintBlock([Linear(a,b,fa,fb,X,correctionRate),Linear(a,b,fa,fb,Y,correctionRate),Linear(a,b,fa,fb,Z,correctionRate)])];
    }
    public static IReadOnlyList<IImpulseConstraint> Hinge(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)
    {
        Validate(fa,fb,correctionRate);
        var axisA=fa.Orientation.Apply(Z); var axisB=fb.Orientation.Apply(Z);
        var cross=CollisionVector.Cross(axisB,axisA); var sine=cross.Length;
        var cosine=Math.Clamp(CollisionVector.Dot(axisA,axisB),-1,1);
        // At exactly antiparallel axes there is no unique shortest swing axis.
        // The declared frame X defines the half-turn, preserving authoring intent.
        var error=sine>1e-14?cross*(Math.Atan2(sine,cosine)/sine):
            cosine<0?fa.Orientation.Apply(X)*Math.PI:default;
        var u=fa.Orientation.Apply(X); var v=fa.Orientation.Apply(Y);
        var rows=new List<ImpulseConstraint>
        {
            Linear(a,b,fa,fb,X,correctionRate),Linear(a,b,fa,fb,Y,correctionRate),Linear(a,b,fa,fb,Z,correctionRate),
            Angular(a,b,u,-correctionRate*CollisionVector.Dot(error,u)),
            Angular(a,b,v,-correctionRate*CollisionVector.Dot(error,v))
        };
        return [new BilateralConstraintBlock(rows)];
    }
    public static IReadOnlyList<IImpulseConstraint> Slider(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)
    {
        Validate(fa,fb,correctionRate);
        var error=(fa.Orientation*fb.Orientation.Inverse()).RotationVector();
        return [new BilateralConstraintBlock([
            Linear(a,b,fa,fb,fb.Orientation.Apply(X),correctionRate,AxisReference.BodyB),
            Linear(a,b,fa,fb,fb.Orientation.Apply(Y),correctionRate,AxisReference.BodyB),
            Angular(a,b,X,-correctionRate*error.X),
            Angular(a,b,Y,-correctionRate*error.Y),
            Angular(a,b,Z,-correctionRate*error.Z)
        ])];
    }

    /// <summary>Two unilateral velocity rows keep a coordinate inside its range
    /// over the upcoming interval. Error correction outside the range is explicit.
    /// The Jacobian must be the derivative of the supplied coordinate.</summary>
    public static IReadOnlyList<IImpulseConstraint> Limits(PhysicsBody a,PhysicsBody b,ConstraintJacobian jacobian,
        double coordinate,double lower,double upper,double duration,double correctionRate=0)
    {
        if(!double.IsFinite(coordinate)||!double.IsFinite(lower)||!double.IsFinite(upper)||lower>upper||
            !double.IsFinite(duration)||duration<=0||!double.IsFinite(correctionRate)||correctionRate<0)
            throw new ArgumentException("Limit interval and duration must be finite and ordered.");
        var lowError=coordinate-lower; var highError=upper-coordinate;
        var lowTarget=lowError>=0?-lowError/duration:-lowError*correctionRate;
        var highTarget=highError>=0?highError/duration:highError*correctionRate;
        return [new ImpulseConstraint(a,b,jacobian,lowTarget,0,double.PositiveInfinity),
            new ImpulseConstraint(a,b,jacobian,highTarget,double.NegativeInfinity,0)];
    }

    public static ImpulseConstraint Rope(PhysicsBody a,PhysicsBody b,CollisionVector pointA,CollisionVector pointB,
        double maximumLength,double duration,double correctionRate=0)
    {
        if(!pointA.IsFinite||!pointB.IsFinite||!double.IsFinite(maximumLength)||maximumLength<=0||
            !double.IsFinite(duration)||duration<=0||!double.IsFinite(correctionRate)||correctionRate<0)
            throw new ArgumentException("Rope geometry and duration must be finite and positive.");
        var delta=pointA-pointB; var length=delta.Length;
        if(!double.IsFinite(length)||length==0)
            throw new ArgumentException("Coincident rope endpoints have no defined constraint gradient.");
        var axis=delta/length;
        var slack=maximumLength-length;
        var target=slack>=0?slack/duration:slack*correctionRate;
        return new(a,b,new(axis,CollisionVector.Cross(pointA-a.Center,axis),
            -axis,-CollisionVector.Cross(pointB-b.Center,axis)),target,double.NegativeInfinity,0);
    }
}
