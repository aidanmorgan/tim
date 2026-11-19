using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

/// <summary>Reference charge is independent of body mass and runtime state.</summary>
public readonly record struct GasStoreDescriptor(LocalGasStoreId Id, PackageBodyIndex Body,
    GasMaterial Material, MassScalar ReferenceVolume, float ReferencePressure, float ReferenceTemperature,
    float GaugePressure, float OutletConductancePerReferenceMass)
{
    public GasEnvelopeState ReferenceCharge(IReadOnlyDictionary<ParameterId, float> parameters)
    {
        if (Id == default || !float.IsFinite(GaugePressure) || GaugePressure <= 0 ||
            !float.IsFinite(OutletConductancePerReferenceMass) || OutletConductancePerReferenceMass < 0)
            throw new ArgumentException("Invalid gas store declaration.");
        return IsobaricGas.Charge(Material, ReferenceVolume.Evaluate(parameters), ReferencePressure, ReferenceTemperature);
    }
}

public static class GasMassReferences
{
    public static MassSourceContext Resolve(PackageBodyIndex body, IReadOnlyList<GasStoreDescriptor> stores,
        IReadOnlyDictionary<ParameterId, float> parameters)
    {
        ArgumentNullException.ThrowIfNull(stores);
        var references = new Dictionary<LocalGasStoreId, ReferenceGasMass>();
        foreach (var store in stores)
            if (!references.TryAdd(store.Id, new((uint)store.Body.Value, store.ReferenceCharge(parameters).Mass)))
                throw new ArgumentException("Duplicate gas store identity.");
        return new((uint)body.Value, references);
    }
}
