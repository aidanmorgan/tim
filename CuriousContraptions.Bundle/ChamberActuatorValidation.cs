using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class ChamberActuatorValidation
{
    public static IEnumerable<BundleError> Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) yield break;
        var path = entry.Metadata.Id + ".ChamberRegions";
        if (mechanical.ChamberRegions is null || mechanical.FiniteWorkActuators is null)
        {
            yield return new(BundleErrorCode.DeclarationCount, path);
            yield break;
        }
        var regions = new HashSet<LocalChamberRegionId>();
        foreach (var region in mechanical.ChamberRegions)
        {
            if (region.Id.Value == 0 || !regions.Add(region.Id))
                yield return new(BundleErrorCode.DuplicateId, path);
            var colliders = PackageBodyGeometry.Colliders(entry, region.Frame);
            if (colliders is null || region.ParticipationCollider.Value < 0 || region.ParticipationCollider.Value >= colliders.Count)
                yield return new(BundleErrorCode.UnresolvedReference, path + ".ParticipationCollider");
            if (!ValidRegion(region)) yield return new(BundleErrorCode.OutOfRange, path + ".Geometry");
        }
        var actuators = new HashSet<LocalFiniteWorkActuatorId>();
        foreach (var actuator in mechanical.FiniteWorkActuators)
        {
            path = entry.Metadata.Id + ".FiniteWorkActuators";
            if (actuator.Id.Value == 0 || !actuators.Add(actuator.Id))
                yield return new(BundleErrorCode.DuplicateId, path);
            if (!regions.Contains(actuator.Region) || !(mechanical.EnergyStores ?? []).Any(store => store.Id == actuator.Store))
                yield return new(BundleErrorCode.UnresolvedReference, path + ".StoreOrRegion");
            if (!MechanicalValueDomain.TryRange(actuator.TargetSpeed, entry.Metadata.Parameters, ParameterUnit.MetrePerSecond, out var minimum, out var maximum) ||
                minimum < 0f || maximum > LinearVelocity.MaximumSpeed || !float.IsFinite(actuator.ExitPadding) || actuator.ExitPadding < 0f)
                yield return new(BundleErrorCode.OutOfRange, path + ".Release");
        }
    }

    private static bool ValidRegion(ChamberRegionDescriptor region)
    {
        if (!Enum.IsDefined(region.Target.Kind) || (region.Target.Kind == TargetSelectorKind.Named
            ? string.IsNullOrWhiteSpace(region.Target.NamedTarget) : region.Target.NamedTarget is not null))
            return false;
        try
        {
            new ChamberRegionDeclaration(new(1), new(1), new(1), new(BodyTargetKind.AllDynamic, default),
                new(new(region.Position.X, region.Position.Y, region.Position.Z),
                    new(region.Rotation.X, region.Rotation.Y, region.Rotation.Z, region.Rotation.W)),
                region.Radius, region.HalfLength, region.MinimumWidth, region.MaximumWidth, region.ContainmentAllowance,
                region.MaximumRelativeSpeed, region.Participation).Validate();
            return true;
        }
        catch (ArgumentException) { return false; }
    }
}
