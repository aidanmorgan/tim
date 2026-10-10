using System;

namespace CuriousContraptions.Gpu;

public readonly record struct Joules(float Value)
{
    public void Validate(float maximum = 200f)
    {
        if (!float.IsFinite(maximum) || maximum < 0 || !float.IsFinite(Value) || Value < 0 || Value > maximum)
            throw new ArgumentException("Work exceeds its admitted domain.");
    }
    public bool HasSameBits(Joules other) =>
        BitConverter.SingleToInt32Bits(Value) == BitConverter.SingleToInt32Bits(other.Value);
}
public readonly record struct ContactSpeed(float Value)
{
    public void Validate(float maximum)
    {
        if (!float.IsFinite(Value) || Value < 0 || Value > maximum)
            throw new ArgumentException("Contact speed exceeds its admitted domain.");
    }
    public bool HasSameBits(ContactSpeed other) =>
        BitConverter.SingleToInt32Bits(Value) == BitConverter.SingleToInt32Bits(other.Value);
}
public readonly record struct WorkMass(float Value);
public readonly record struct ContactWorkCalibration(ContactSpeed TargetSpeed, WorkMass ReferenceMass, Joules InitialEnergy)
{
    public void Validate()
    {
        TargetSpeed.Validate(20f);
        InitialEnergy.Validate();
        if (!float.IsFinite(ReferenceMass.Value) || ReferenceMass.Value <= 0 || ReferenceMass.Value > 100)
            throw new ArgumentException("Invalid contact-work reference mass.");
    }
}

public readonly record struct GpuContactWorkId(ulong Value);
public readonly record struct ContactWorkSlot(ushort Value);
public readonly record struct ColliderSlot(ushort Value);
public enum BodyTargetKind : byte { NamedBody = 1, AllDynamic = 2 }
public readonly record struct BodyTargetSet(BodyTargetKind Kind, GpuBodyId Body)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Kind) || (Kind == BodyTargetKind.NamedBody ? Body.Value == 0 : Body.Value != 0))
            throw new ArgumentException("Invalid declared body target set.");
    }
    public bool Contains(GpuBodyId body) => body.Value != 0 && (Kind == BodyTargetKind.AllDynamic || Body == body);
}
public enum ContactWorkEffect : byte { Passive = 0, Paid = 1 }

/// <summary>One finite reservoir shared by all declared dynamic targets.</summary>
public readonly record struct ContactWorkDeclaration(GpuContactWorkId Id, GpuBodyId Owner,
    BodyTargetSet Targets, ContactSpeed TargetSpeed, Joules InitialEnergy, ContactSpeed Threshold,
    uint CooldownPhysicalSteps)
{
    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Owner.Value == 0 || Targets.Body == Owner ||
            CooldownPhysicalSteps is < 9 or > 14400)
            throw new ArgumentException("Invalid contact-work identity or cooldown.");
        TargetSpeed.Validate(20f);
        InitialEnergy.Validate();
        Threshold.Validate(64f);
    }
}

/// <summary>Owner state; energy is never duplicated for individual targets.</summary>
public readonly record struct ContactWorkRead(GpuContactWorkId Id, GpuBodyId Owner,
    uint OccurrenceCount, Joules RemainingEnergy, Joules SuppliedEnergy = default)
{
    public const uint MaximumOccurrences = 1601 * PhysicsBodyReadSet.Capacity;
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || OccurrenceCount > MaximumOccurrences)
            throw new ArgumentException("Invalid contact-work observation identity.");
        RemainingEnergy.Validate();
        SuppliedEnergy.Validate(8f);
    }
}

/// <summary>Last qualified occurrence for one owner/target; slots bind to the installed immutable scene.</summary>
public readonly record struct ContactWorkOccurrence(ContactWorkSlot Work, ColliderSlot Collider,
    GpuBodyId Target, uint Sequence, uint EventOrdinal, Half EventPhase, ContactSpeed ApproachSpeed,
    Joules Debit, ContactWorkEffect Effect)
{
    public void Validate()
    {
        if (Work.Value >= PhysicsSceneDeclaration.ContactWorkCapacity || Collider.Value >= PhysicsSceneDeclaration.ColliderCapacity ||
            Target.Value == 0 || Sequence > ContactWorkRead.MaximumOccurrences || !Enum.IsDefined(Effect))
            throw new ArgumentException("Invalid contact-work occurrence identity.");
        ApproachSpeed.Validate(4096f);
        Debit.Validate();
        if (Sequence == 0)
        {
            if (Collider.Value != 0 || EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(EventPhase) ||
                !ApproachSpeed.HasSameBits(default) || !Debit.HasSameBits(default) ||
                Effect != ContactWorkEffect.Passive)
                throw new ArgumentException("Unused contact target retained an occurrence.");
        }
        else if (EventOrdinal > 14400 || !Half.IsFinite(EventPhase) ||
            EventPhase < (Half)(-2048) || EventPhase >= (Half)2048 || (EventOrdinal == 0 && EventPhase < (Half)0) ||
            (Effect == ContactWorkEffect.Passive ? !Debit.HasSameBits(default) : Debit.Value <= 0f))
            throw new ArgumentException("Invalid contact-work event time or effect accounting.");
    }

    public bool HasSameBits(ContactWorkOccurrence other) => Work == other.Work && Collider == other.Collider &&
        Target == other.Target && Sequence == other.Sequence && EventOrdinal == other.EventOrdinal && Effect == other.Effect &&
        HalfBits.Equal(EventPhase, other.EventPhase) && ApproachSpeed.HasSameBits(other.ApproachSpeed) &&
        Debit.HasSameBits(other.Debit);
}
