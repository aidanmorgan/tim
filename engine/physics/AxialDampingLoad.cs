using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Passive viscous effort on an owned axial joint. Both travel directions
/// have explicit nonnegative coefficients; prediction evaluates relative speed.</summary>
public sealed record AxialDampingLoad
{
    public PhysicsJointId Joint { get; }
    public FrameJointKind Kind { get; }
    public double NegativeCoefficient { get; }
    public double PositiveCoefficient { get; }
    public AxialDampingLoad(PhysicsJointId joint,FrameJointKind kind,double negativeCoefficient,double positiveCoefficient)
    {
        if(kind is not (FrameJointKind.Slider or FrameJointKind.Hinge))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if(!double.IsFinite(negativeCoefficient)||negativeCoefficient<0)
            throw new ArgumentOutOfRangeException(nameof(negativeCoefficient));
        if(!double.IsFinite(positiveCoefficient)||positiveCoefficient<0)
            throw new ArgumentOutOfRangeException(nameof(positiveCoefficient));
        Joint=joint;Kind=kind;NegativeCoefficient=negativeCoefficient;PositiveCoefficient=positiveCoefficient;
    }
    public double Effort(double speed)
    {
        if(!double.IsFinite(speed))throw new ArgumentOutOfRangeException(nameof(speed));
        var effort=-(speed<0?NegativeCoefficient:PositiveCoefficient)*speed;
        if(!double.IsFinite(effort))throw new ArgumentOutOfRangeException(nameof(speed));
        return effort;
    }
    public PhysicsFrameJoint Resolve(IEnumerable<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if(joints.SingleOrDefault(j=>j.Id==Joint) is not PhysicsFrameJoint frame||frame.Kind!=Kind)
            throw new ArgumentException("Damping load requires its current owned axial joint.");
        return frame;
    }
}
