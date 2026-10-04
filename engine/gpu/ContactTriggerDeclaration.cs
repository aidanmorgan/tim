using System;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuContactTriggerId(ulong Value);

/// <summary>Qualifies a real impact by pre-response normal approach speed, independent of catalogue kind.</summary>
public readonly record struct ContactTriggerDeclaration(GpuContactTriggerId Id, GpuBodyId Owner,
    GpuBodyId Target, LinearSpeed Threshold)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Target.Value == 0 || Owner == Target)
            throw new ArgumentException("Invalid contact trigger identity.");
        PhysicsDeclarationBounds.Range(Threshold.Value, (Half)0, (Half)64);
    }
}

public readonly record struct ContactTriggerRead(GpuContactTriggerId Id, GpuBodyId Owner,
    GpuBodyId Target, uint OccurrenceCount, GpuColliderId Collider, uint EventOrdinal,
    Half EventPhase, LinearSpeed ApproachSpeed);
