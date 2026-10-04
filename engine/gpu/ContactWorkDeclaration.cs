using System;

namespace CuriousContraptions.Gpu;

public readonly record struct Joules(Half Value);
public readonly record struct GpuContactWorkId(ulong Value);

/// <summary>A finite work owner qualified by actual impact; catalogue meaning is absent from the law.</summary>
public readonly record struct ContactWorkDeclaration(GpuContactWorkId Id, GpuBodyId Owner,
    GpuBodyId Target, LinearSpeed TargetSpeed, Joules InitialEnergy, LinearSpeed Threshold,
    uint CooldownPhysicalSteps)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Target.Value == 0 || Owner == Target ||
            CooldownPhysicalSteps is < 9 or > 14400)
            throw new ArgumentException("Invalid contact-work identity or cooldown.");
        PhysicsDeclarationBounds.Range(TargetSpeed.Value, (Half)0, (Half)20);
        PhysicsDeclarationBounds.Range(InitialEnergy.Value, (Half)0, (Half)200);
        PhysicsDeclarationBounds.Range(Threshold.Value, (Half)0, (Half)64);
    }
}
public readonly record struct ContactWorkRead(GpuContactWorkId Id, GpuBodyId Owner, GpuBodyId Target,
    uint OccurrenceCount, GpuColliderId Collider, uint EventOrdinal, Half EventPhase,
    LinearSpeed ApproachSpeed, Joules RemainingEnergy, Joules LastDebit)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Target.Value == 0 || Owner == Target || OccurrenceCount > 1601)
            throw new ArgumentException("Invalid contact-work observation identity.");
        PhysicsDeclarationBounds.Range(RemainingEnergy.Value, (Half)0, (Half)200);
        PhysicsDeclarationBounds.Range(LastDebit.Value, (Half)0, (Half)200);
        if (OccurrenceCount == 0)
        {
            if (Collider.Value != 0 || EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(EventPhase) ||
                !PhysicsDeclarationBounds.Zero(ApproachSpeed.Value) || !PhysicsDeclarationBounds.Zero(LastDebit.Value))
                throw new ArgumentException("Unused contact work retained an occurrence.");
        }
        else if (Collider.Value == 0 || EventOrdinal > 14400 || !Half.IsFinite(EventPhase) ||
            EventPhase < (Half)(-2048) || EventPhase >= (Half)2048 || (EventOrdinal == 0 && EventPhase < (Half)0))
            throw new ArgumentException("Invalid contact-work event time.");
        PhysicsDeclarationBounds.Range(ApproachSpeed.Value, (Half)0, (Half)128);
    }
}
