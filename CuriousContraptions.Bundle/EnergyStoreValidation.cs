using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class EnergyStoreValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        var path = entry.Metadata.Id + ".EnergyStores";
        if (mechanical.EnergyStores is null)
        {
            yield return new(BundleErrorCode.DeclarationCount, path);
            yield break;
        }
        var identities = new HashSet<LocalEnergyStoreId>();
        foreach (var store in mechanical.EnergyStores)
        {
            if (store.Id.Value == 0 || !identities.Add(store.Id))
                yield return new(BundleErrorCode.DuplicateId, path);
            if (store.Owner.Value < 0 || store.Owner.Value >= entry.Footprint.Bodies)
                yield return new(BundleErrorCode.UnresolvedReference, path + ".Owner");
            if (!ValidSelection(entry, store))
                yield return new(BundleErrorCode.UnresolvedReference, path + ".Selection");
            if (!ValidAmounts(store, entry.Metadata.Parameters))
                yield return new(BundleErrorCode.OutOfRange, path + ".Amounts");
        }
    }

    private static bool ValidSelection(BundleEntry entry, EnergyStoreDescriptor store)
    {
        if (store.Selection is not { } selection) return true;
        try { _ = ChoiceDomain.Parameter(entry, selection); }
        catch (ArgumentException) { return false; }
        var mechanical = entry.Mechanical!;
        // These consumers have no selection predicate and therefore cannot refer to an absent store.
        if (mechanical.ContactWorks.Any(work => work.Store == store.Id) ||
            mechanical.FiniteWorkActuators.Any(work => work.Store == store.Id) ||
            (entry.Electrical?.SupplyGraph?.Nodes.OfType<CuriousContraptions.Abstractions.Electrical.SupplyConsumerDescriptor>()
                .Any(node => node.Targets.OfType<CuriousContraptions.Abstractions.Electrical.SupplyEnergyStoreTarget>()
                    .Any(target => target.Store == store.Id)) ?? false)) return false;
        return mechanical.AirflowSources.SelectMany(source => source.Inputs).OfType<StoredAirflowInput>()
            .Where(input => input.Store == store.Id).All(input => input.Selection == selection);
    }

    private static bool ValidAmounts(EnergyStoreDescriptor store, IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (!TryRange(store.Capacity, parameters, out var capacityMinimum, out _) ||
            !TryRange(store.InitialEnergy, parameters, out _, out var initialMaximum) ||
            !MechanicalValueDomain.TryRange(store.MaximumChargePower, parameters, ParameterUnit.Watt, out var power, out _) ||
            power < 0f)
            return false;
        // Identical expressions retain their correlation. Otherwise every declared combination must fit;
        // an unconstrained cross-parameter relation cannot be silently assumed by the compiler.
        return store.InitialEnergy == store.Capacity || initialMaximum <= capacityMinimum;
    }

    private static bool TryRange(EnergyExpression expression, IReadOnlyList<ParameterDescriptor> parameters,
        out float minimum, out float maximum)
    {
        minimum = maximum = 0f;
        if (expression.Kind == EnergyExpressionKind.Direct)
            return expression.Speed == MechanicalValue.Fixed(0f) &&
                MechanicalValueDomain.TryRange(expression.Value, parameters, ParameterUnit.Joule, out minimum, out maximum) &&
                minimum >= 0f;
        if (expression.Kind != EnergyExpressionKind.Kinetic ||
            !MechanicalValueDomain.TryRange(expression.Value, parameters, ParameterUnit.Kilogram, out var massMin, out var massMax) ||
            !MechanicalValueDomain.TryRange(expression.Speed, parameters, ParameterUnit.MetrePerSecond, out var speedMin, out var speedMax) ||
            massMin < 0f || speedMin < 0f)
            return false;
        minimum = .5f * massMin * speedMin * speedMin;
        maximum = .5f * massMax * speedMax * speedMax;
        return float.IsFinite(minimum) && float.IsFinite(maximum);
    }
}
