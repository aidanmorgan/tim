using System;

namespace CuriousContraptions.Gpu;

public readonly record struct Joules(Half Value);
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
    BodyTargetSet Targets, LinearSpeed TargetSpeed, Joules InitialEnergy, LinearSpeed Threshold,
    uint CooldownPhysicalSteps)
{
    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Owner.Value == 0 || Targets.Body == Owner ||
            CooldownPhysicalSteps is < 9 or > 14400)
            throw new ArgumentException("Invalid contact-work identity or cooldown.");
        PhysicsDeclarationBounds.Range(TargetSpeed.Value, (Half)0, (Half)20);
        PhysicsDeclarationBounds.Range(InitialEnergy.Value, (Half)0, (Half)200);
        PhysicsDeclarationBounds.Range(Threshold.Value, (Half)0, (Half)64);
    }
}

/// <summary>Owner state; energy is never duplicated for individual targets.</summary>
public readonly record struct ContactWorkRead(GpuContactWorkId Id, GpuBodyId Owner,
    uint OccurrenceCount, Joules RemainingEnergy)
{
    public const uint MaximumOccurrences = 1601 * PhysicsBodyReadSet.Capacity;
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || OccurrenceCount > MaximumOccurrences)
            throw new ArgumentException("Invalid contact-work observation identity.");
        PhysicsDeclarationBounds.Range(RemainingEnergy.Value, (Half)0, (Half)200);
    }
}

/// <summary>Last qualified occurrence for one owner/target; slots bind to the installed immutable scene.</summary>
public readonly record struct ContactWorkOccurrence(ContactWorkSlot Work, ColliderSlot Collider,
    GpuBodyId Target, uint Sequence, uint EventOrdinal, Half EventPhase, LinearSpeed ApproachSpeed,
    Joules Debit, ContactWorkEffect Effect)
{
    public void Validate()
    {
        if (Work.Value >= PhysicsSceneDeclaration.ContactWorkCapacity || Collider.Value >= PhysicsSceneDeclaration.ColliderCapacity ||
            Target.Value == 0 || Sequence > ContactWorkRead.MaximumOccurrences || !Enum.IsDefined(Effect))
            throw new ArgumentException("Invalid contact-work occurrence identity.");
        PhysicsDeclarationBounds.Range(ApproachSpeed.Value, (Half)0, (Half)4096);
        PhysicsDeclarationBounds.Range(Debit.Value, (Half)0, (Half)200);
        if (Sequence == 0)
        {
            if (Collider.Value != 0 || EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(EventPhase) ||
                !PhysicsDeclarationBounds.Zero(ApproachSpeed.Value) || !PhysicsDeclarationBounds.Zero(Debit.Value) ||
                Effect != ContactWorkEffect.Passive)
                throw new ArgumentException("Unused contact target retained an occurrence.");
        }
        else if (EventOrdinal > 14400 || !Half.IsFinite(EventPhase) ||
            EventPhase < (Half)(-2048) || EventPhase >= (Half)2048 || (EventOrdinal == 0 && EventPhase < (Half)0) ||
            (Effect == ContactWorkEffect.Passive ? !PhysicsDeclarationBounds.Zero(Debit.Value) : Debit.Value <= (Half)0))
            throw new ArgumentException("Invalid contact-work event time or effect accounting.");
    }

    public bool HasSameBits(ContactWorkOccurrence other) => Work == other.Work && Collider == other.Collider &&
        Target == other.Target && Sequence == other.Sequence && EventOrdinal == other.EventOrdinal && Effect == other.Effect &&
        HalfBits.Equal(EventPhase, other.EventPhase) && HalfBits.Equal(ApproachSpeed.Value, other.ApproachSpeed.Value) &&
        HalfBits.Equal(Debit.Value, other.Debit.Value);
}
