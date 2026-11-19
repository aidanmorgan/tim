using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class GasStoreDomain
{
    public static void Validate(MechanicalPayload mechanical, IReadOnlyList<ParameterDescriptor> parameters)
    {
        ArgumentNullException.ThrowIfNull(mechanical.GasStores);
        var frames = new[] { mechanical.MassFrame }
            .Concat(mechanical.Sliders.Select(s => s.MovingBody.MassFrame))
            .Concat(mechanical.Hinges.Select(h => h.MovingBody.MassFrame)).ToArray();
        var domain = new MassPolynomialDomain(parameters);
        var ids = new HashSet<LocalGasStoreId>();
        foreach (var store in mechanical.GasStores)
        {
            if (!ids.Add(store.Id) || store.Id == default || store.Body.Value < 0 || store.Body.Value >= frames.Length)
                throw new ArgumentException("Gas store has a duplicate identity or unresolved body.");
            var source = frames[store.Body.Value].Source;
            if (source.Kind != BodyMassSourceKind.CenteredInventory || source.Stores is null || source.Stores.Count(s => s == store.Id) != 1)
                throw new ArgumentException("Gas inventory must have exactly one declared mass owner.");
            var volume = domain.Bounds((store.ReferenceVolume, 1));
            foreach (var endpoint in new[] { volume.Min, volume.Max })
            {
                var reference = IsobaricGas.Charge(store.Material, endpoint, store.ReferencePressure, store.ReferenceTemperature);
                foreach (var preset in new[] { AtmospherePreset.Standard, AtmospherePreset.Vacuum })
                    _ = IsobaricGas.Prepare(store.Material, reference, AtmosphereBoundary.For(preset).Pressure, store.GaugePressure);
                if (!float.IsFinite(store.OutletConductancePerReferenceMass * reference.Mass))
                    throw new ArgumentException("Gas outlet conductance overflow.");
            }
            if (store.ReferenceVolume.Parameter is { } key &&
                !parameters.Any(p => p.Key == key.Value && p.Kind == ParameterKind.Real && p.RunEdit == ParameterRunEdit.BuildOnly))
                throw new ArgumentException("Gas volume requires a BuildOnly real parameter.");
        }
    }
}
