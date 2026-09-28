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
        if(!double.IsFinite(duration)||duration<=0||!double.IsFinite(correctionRate)||correctionRate<0)
            throw new ArgumentException("Rope duration must be positive and correction rate nonnegative.");
        var equation=JointEquations.Rope(a,b,pointA,pointB,maximumLength);
        var target=equation.Error<=0?-equation.Error/duration:-equation.Error*correctionRate;
        return new(a,b,equation.Jacobian,target,double.NegativeInfinity,0);
    }
}
