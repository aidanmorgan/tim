using CuriousContraptions.Abstractions;
using System;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct Joules(float Value)
{
    public void Validate(float maximum = float.MaxValue)
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
public readonly record struct GpuContactWorkId(ulong Value);
/// <summary>Table slots are u32: no work or collider table has a fixed bound (Story 9R.5b).</summary>
public readonly record struct ContactWorkSlot(uint Value);
public readonly record struct ColliderSlot(uint Value);
public enum BodyTargetKind : byte { NamedBody = 1, AllDynamic = 2, AllPayloads = 3 }
public readonly record struct BodyTargetSet(BodyTargetKind Kind, GpuBodyId Body)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Kind) || (Kind == BodyTargetKind.NamedBody ? Body.Value == 0 : Body.Value != 0))
            throw new ArgumentException("Invalid declared body target set.");
    }
    public bool Contains(GpuBodyId body, PayloadPolicy payload) => body.Value != 0 && Kind switch
    {
        BodyTargetKind.NamedBody => Body==body,
        BodyTargetKind.AllDynamic => true,
        BodyTargetKind.AllPayloads => payload==PayloadPolicy.CaptureTarget,
        _ => throw new ArgumentException("Undefined target policy.")
    };
}
public enum ContactWorkEffect : byte { Passive = 0, Paid = 1 }

/// <summary>One contact-triggered discharge; its finite reservoir is independently identified.</summary>
public readonly record struct ContactWorkDeclaration(GpuContactWorkId Id, GpuBodyId Owner,
    BodyTargetSet Targets, ContactSpeed TargetSpeed, EnergyStoreId Store, ContactSpeed Threshold,
    uint CooldownPhysicalSteps)
{
    // A read retains one occurrence per pair/commit. The slowest admitted commit contains eight
    // physical steps, so its cooldown must extend beyond that interval. The upper bound is the
    // existing finite run horizon, not a product-specific Bumper setting.
    public static uint MinimumCooldownPhysicalSteps { get; } = MaximumStepsPerCommit() + 1;
    public const uint MaximumCooldownPhysicalSteps = (uint)WorkshopCadenceSettings.PhysicalOrdinalLimit;
    public static bool AdmitsCooldown(uint steps) => steps >= MinimumCooldownPhysicalSteps && steps <= MaximumCooldownPhysicalSteps;

    private static uint MaximumStepsPerCommit()
    {
        uint maximum = 0;
        foreach (var cadence in Enum.GetValues<SimulationCadence>())
            maximum = Math.Max(maximum, (WorkshopCadenceSettings.Default() with { Simulation = cadence }).PhysicalStepsPerCommit);
        return maximum;
    }

    public void Validate()
    {
        Targets.Validate();
        if (Id.Value == 0 || Owner.Value == 0 || Store.Value == 0 || Targets.Body == Owner ||
            !AdmitsCooldown(CooldownPhysicalSteps))
            throw new ArgumentException("Invalid contact-work identity or cooldown.");
        TargetSpeed.Validate(LinearVelocity.MaximumSpeed);
        Threshold.Validate(LinearVelocity.MaximumSpeed);
    }
}

/// <summary>Owner state; energy is never duplicated for individual targets.</summary>
public readonly record struct ContactWorkRead(GpuContactWorkId Id, GpuBodyId Owner,
    uint OccurrenceCount, EnergyStoreId Store)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Store.Value == 0)
            throw new ArgumentException("Invalid contact-work observation identity.");
    }
}

/// <summary>Last qualified occurrence for one owner/target; slots bind to the installed immutable scene.</summary>
public readonly record struct ContactWorkOccurrence(ContactWorkSlot Work, ColliderSlot Collider,
    GpuBodyId Target, uint Sequence, uint EventOrdinal, float EventPhase, ContactSpeed ApproachSpeed,
    Joules Debit, ContactWorkEffect Effect)
{
    public void Validate()
    {
        // The collider slot is bound to the installed scene's own collider count where the world is read (ReadContactWorks).
        // The work slot is bound to its read's own work count (PhysicsContactWorkRead); no table bound applies here.
        if (Target.Value == 0 || !Enum.IsDefined(Effect))
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
        else if (EventOrdinal > WorkshopCadenceSettings.PhysicalOrdinalLimit || !PhysicalPhase.IsAdmitted(EventOrdinal, EventPhase) ||
            (Effect == ContactWorkEffect.Passive ? !Debit.HasSameBits(default) : Debit.Value <= 0f))
            throw new ArgumentException("Invalid contact-work event time or effect accounting.");
    }

    public bool HasSameBits(ContactWorkOccurrence other) => Work == other.Work && Collider == other.Collider &&
        Target == other.Target && Sequence == other.Sequence && EventOrdinal == other.EventOrdinal && Effect == other.Effect &&
        F32Bits.Equal(EventPhase, other.EventPhase) && ApproachSpeed.HasSameBits(other.ApproachSpeed) &&
        Debit.HasSameBits(other.Debit);
}
