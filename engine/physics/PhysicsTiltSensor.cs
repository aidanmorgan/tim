using System;

namespace CuriousContraptions.Physics;

public enum PhysicsTiltPhase { Waiting, Triggered }

/// <summary>One latched angular-deviation sensor per rigid body. The reference
/// direction is captured from the owned pose when installed, not from rendering.</summary>
public sealed record PhysicsTiltSensor
{
    public PhysicsBodyId Body { get; }
    public CollisionVector LocalDirection { get; }
    public double ThresholdCosine { get; }
    public const double CosineTolerance=1e-10;
    public PhysicsTiltSensor(PhysicsBodyId body,CollisionVector localDirection,double thresholdCosine)
    {
        if(!localDirection.IsFinite||!double.IsFinite(localDirection.Length)||localDirection.Length<=0)
            throw new ArgumentException("Tilt direction must be finite and nonzero.",nameof(localDirection));
        if(!double.IsFinite(thresholdCosine)||thresholdCosine<=-1||thresholdCosine>=1-CosineTolerance)
            throw new ArgumentOutOfRangeException(nameof(thresholdCosine));
        Body=body;LocalDirection=localDirection/localDirection.Length;ThresholdCosine=thresholdCosine;
    }
}

public readonly record struct PhysicsTiltState(PhysicsTiltSensor Declaration,
    CollisionVector ReferenceDirection,PhysicsTiltPhase Phase,double? TriggerTime);
