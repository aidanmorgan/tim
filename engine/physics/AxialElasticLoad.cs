using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Immutable potential bound to an owned axial coordinate, resolved
/// against the current joint declaration for each prediction interval.</summary>
public sealed record AxialElasticLoad
{
    public PhysicsJointId Joint { get; }
    public FrameJointKind Kind { get; }
    public AxialElasticPotential Potential { get; }
    public AxialElasticLoad(PhysicsJointId joint,FrameJointKind kind,AxialElasticPotential potential)
    {
        if(kind is not (FrameJointKind.Slider or FrameJointKind.Hinge))
            throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentNullException.ThrowIfNull(potential);
        Joint=joint;Kind=kind;Potential=potential;
    }
    public PhysicsFrameJoint Resolve(IEnumerable<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if(joints.SingleOrDefault(j=>j.Id==Joint) is not PhysicsFrameJoint frame||frame.Kind!=Kind)
            throw new ArgumentException("Elastic load requires its current owned axial joint.");
        return frame;
    }
}
