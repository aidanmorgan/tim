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
    private static IReadOnlyList<IImpulseConstraint> Frames(FrameJointKind kind,PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate)
    {
        if(!double.IsFinite(correctionRate)||correctionRate<0) throw new ArgumentOutOfRangeException(nameof(correctionRate));
        var equations=JointEquations.Frames(kind,a,b,fa,fb);
        var rows=new ImpulseConstraint[equations.Length];
        for(var i=0;i<rows.Length;i++)
            rows[i]=new(a,b,equations[i].Jacobian,-correctionRate*equations[i].Error,double.NegativeInfinity,double.PositiveInfinity);
        return [new BilateralConstraintBlock(rows)];
    }
    public static IReadOnlyList<IImpulseConstraint> BallSocket(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)=>Frames(FrameJointKind.BallSocket,a,b,fa,fb,correctionRate);
    public static IReadOnlyList<IImpulseConstraint> Hinge(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)=>Frames(FrameJointKind.Hinge,a,b,fa,fb,correctionRate);
    public static IReadOnlyList<IImpulseConstraint> Slider(PhysicsBody a,PhysicsBody b,
        JointFrame fa,JointFrame fb,double correctionRate=0)=>Frames(FrameJointKind.Slider,a,b,fa,fb,correctionRate);

    /// <summary>Only currently active stops constrain velocity. Future arrival
    /// is handled by the world's continuous boundary event clock.</summary>
    public static IReadOnlyList<IImpulseConstraint> Limits(PhysicsBody a,PhysicsBody b,ConstraintJacobian jacobian,
        double coordinate,double lower,double upper,double activationTolerance)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id||!jacobian.IsFinite) throw new ArgumentException("Limit requires distinct bodies and a finite Jacobian.");
        if(!double.IsFinite(coordinate)||!double.IsFinite(lower)||!double.IsFinite(upper)||lower>upper||
            !double.IsFinite(activationTolerance)||activationTolerance<=0)
            throw new ArgumentException("Limit interval and activation precision must be finite and ordered.");
        var rows=new List<IImpulseConstraint>();
        if(coordinate-lower<=activationTolerance) rows.Add(new ImpulseConstraint(a,b,jacobian,0,0,double.PositiveInfinity));
        if(upper-coordinate<=activationTolerance) rows.Add(new ImpulseConstraint(a,b,jacobian,0,double.NegativeInfinity,0));
        return rows;
    }

    public static IReadOnlyList<IImpulseConstraint> Rope(PhysicsBody a,PhysicsBody b,CollisionVector pointA,CollisionVector pointB,
        double maximumLength,double activationTolerance)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id) throw new ArgumentException("Rope requires distinct body identities.");
        if(!pointA.IsFinite||!pointB.IsFinite||!double.IsFinite(maximumLength)||maximumLength<=0||
            !double.IsFinite(activationTolerance)||activationTolerance<=0||maximumLength<=activationTolerance)
            throw new ArgumentException("Rope geometry and activation precision must be finite and consistent.");
        if((pointA-pointB).Length<maximumLength-activationTolerance) return [];
        var equation=JointEquations.Rope(a,b,pointA,pointB,maximumLength);
        return [new ImpulseConstraint(a,b,equation.Jacobian,0,double.NegativeInfinity,0)];
    }
}
