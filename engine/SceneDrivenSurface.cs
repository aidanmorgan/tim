using System;
using System.Collections.Generic;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Construction declaration of material motion through an owned hinge.
/// Runtime motion and work remain in the shared bodies and motor.</summary>
public sealed record SceneDrivenSurface(SceneJointKey Drive,CollisionVector LocalNormal,
    CollisionVector LocalDirection,double TravelPerRadian)
{
    public DrivenSurface Bind(IReadOnlyDictionary<SceneJointKey,PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        if(!joints.TryGetValue(Drive,out var joint)||joint is not PhysicsFrameJoint frame)
            throw new ArgumentException("Driven surface must refer to a declared frame joint.");
        return new(frame,LocalNormal,LocalDirection,TravelPerRadian);
    }
}
