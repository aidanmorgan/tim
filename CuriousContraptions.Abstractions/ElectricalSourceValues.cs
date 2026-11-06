using System;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

public readonly record struct ElectricalSourceId(ulong Value);
public readonly record struct Watts(float Value);

/// <summary>One compiled finite store, from its owner's declared store parameters (Story 9A.39). Its values lie inside
/// <see cref="ElectricalSourceEnvelope"/>.</summary>
public readonly record struct ElectricalSourceDeclaration(ElectricalSourceId Id, GpuBodyId Owner,
    Joules Capacity, Watts MaximumPower, float InitialFraction, ElectricalEnable Enabled)
{
    public Joules InitialEnergy => new(Capacity.Value * InitialFraction);
    public void Validate()
    {
        ValidateStore(Capacity, MaximumPower, InitialFraction, Enabled);
        if (Id.Value == 0 || Owner.Value == 0)
            throw new ArgumentException("Invalid finite electrical source identity.");
    }

    /// <summary>The store's runtime domain: capacity and power inside the envelope, an initial fraction in [0, 1] and a defined
    /// enable state.</summary>
    public static void ValidateStore(Joules capacity, Watts maximumPower, float initialFraction, ElectricalEnable enabled)
    {
        capacity.Validate(ElectricalSourceEnvelope.MaximumCapacity);
        if (capacity.Value < ElectricalSourceEnvelope.MinimumCapacity || !float.IsFinite(maximumPower.Value) ||
            maximumPower.Value is < ElectricalSourceEnvelope.MinimumPower or > ElectricalSourceEnvelope.MaximumPower ||
            !float.IsFinite(initialFraction) || initialFraction is < 0f or > 1f || !Enum.IsDefined(enabled))
            throw new ArgumentException("Invalid electrical source store.");
    }
}

public readonly record struct ElectricalStorageBinding(ElectricalSourceId Source, GpuContactWorkId Storage);

public enum ElectricalEnable : byte { Disabled = 0, Enabled = 1 }
