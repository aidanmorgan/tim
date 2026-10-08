using System;

namespace CuriousContraptions.Physics;

/// <summary>World-space force and centre-of-mass torque held along one captured
/// trajectory. The zero value explicitly declares unforced motion.</summary>
public readonly record struct BodyWrench
{
    public CollisionVector Force { get; }
    public CollisionVector Torque { get; }
    public BodyWrench(CollisionVector force,CollisionVector torque)
    {
        if(!force.IsFinite||!torque.IsFinite) throw new ArgumentException("Trajectory wrench must be finite.");
        Force=force; Torque=torque;
    }
}
