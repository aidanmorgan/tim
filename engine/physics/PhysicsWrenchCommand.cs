using System;

namespace CuriousContraptions.Physics;

/// <summary>World-space force and centre-of-mass torque held for one Step.
/// Multiple sources on a body sum in declaration order. Gravity is supplied by
/// world settings and must not also be included in this command.</summary>
public readonly record struct PhysicsWrenchCommand
{
    public PhysicsBodyId Body { get; }
    public CollisionVector Force { get; }
    public CollisionVector Torque { get; }
    public PhysicsWrenchCommand(PhysicsBodyId body,CollisionVector force,CollisionVector torque)
    {
        if(!force.IsFinite||!torque.IsFinite) throw new ArgumentException("Applied force and torque must be finite.");
        Body=body; Force=force; Torque=torque;
    }
}
