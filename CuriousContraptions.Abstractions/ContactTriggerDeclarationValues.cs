using System;

namespace CuriousContraptions.Gpu;

public readonly record struct GpuContactTriggerId(ulong Value);

/// <summary>Qualifies a real impact by pre-response normal approach speed, independent of catalogue kind.</summary>
public readonly record struct ContactTriggerDeclaration(GpuContactTriggerId Id, GpuBodyId Owner,
    BodyTargetSet Targets, ContactSpeed Threshold)
{
    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Owner.Value == 0 || Owner == Targets.Body)
            throw new ArgumentException("Invalid contact trigger identity.");
        Threshold.Validate(64f);
    }
}

public readonly record struct ContactTriggerRead(GpuContactTriggerId Id, GpuBodyId Owner,
    GpuBodyId Target, uint OccurrenceCount, GpuColliderId Collider, uint EventOrdinal,
    float EventPhase, ContactSpeed ApproachSpeed);
