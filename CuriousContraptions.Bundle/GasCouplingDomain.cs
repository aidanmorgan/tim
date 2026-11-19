using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

internal static class GasCouplingDomain
{
    public static void Validate(BundleEntry entry)
    {
        if (entry.Mechanical is not { } mechanical) return;
        ArgumentNullException.ThrowIfNull(mechanical.GasShapes);
        ArgumentNullException.ThrowIfNull(mechanical.IntegrityObservers);
        ArgumentNullException.ThrowIfNull(mechanical.AmbientLoads);
        Shapes(entry, mechanical);
        Integrity(entry, mechanical);
        Ambient(entry, mechanical);
    }

    private static GasStoreDescriptor Store(MechanicalPayload mechanical, LocalGasStoreId id, PackageBodyIndex body)
    {
        var matches = mechanical.GasStores.Where(s => s.Id == id && s.Body == body).ToArray();
        if (id == default || matches.Length != 1) throw new ArgumentException("Unresolved exact gas store owner.");
        return matches[0];
    }

    private static ColliderDescriptor Collider(BundleEntry entry, PackageBodyIndex body, BodyColliderIndex collider)
    {
        var shapes = BundleMaterial.Shapes(entry, body);
        if (collider.Value < 0 || collider.Value >= shapes.Count)
            throw new ArgumentException("Unresolved exact gas collider.");
        return shapes[collider.Value];
    }

    private static void Shapes(BundleEntry entry, MechanicalPayload mechanical)
    {
        var writers = new HashSet<(PackageBodyIndex, BodyColliderIndex)>();
        var domain = new MassPolynomialDomain(entry.Metadata.Parameters);
        foreach (var shape in mechanical.GasShapes)
        {
            var store = Store(mechanical, shape.Store, shape.Body);
            var collider = Collider(entry, shape.Body, shape.Collider);
            if (!writers.Add((shape.Body, shape.Collider)) || collider.ShapeKind != Abstractions.ColliderShapeKind.Sphere ||
                !float.IsFinite(shape.DryRadius) || shape.DryRadius < 1f / 32f || shape.DryRadius > 16f ||
                mechanical.Extrusions.Any(e => e.MaterialBody == shape.Body && e.Collider == shape.Collider))
                throw new ArgumentException("Gas geometry requires one sphere writer and a positive dry core.");
            var volume = domain.Bounds((store.ReferenceVolume, 1));
            foreach (var endpoint in new[] { volume.Min, volume.Max })
                foreach (var preset in new[] { AtmospherePreset.Standard, AtmospherePreset.Vacuum })
                {
                    var reference = IsobaricGas.Charge(store.Material, endpoint, store.ReferencePressure, store.ReferenceTemperature);
                    var prepared = IsobaricGas.Prepare(store.Material, reference, AtmosphereBoundary.For(preset).Pressure, store.GaugePressure);
                    var radius = IsobaricGas.OuterRadius(shape.DryRadius, prepared.State.Volume);
                    if (radius > 16) throw new ArgumentException("Prepared gas shape leaves the collider domain.");
                }
        }
    }

    private static void Integrity(BundleEntry entry, MechanicalPayload mechanical)
    {
        var ids = new HashSet<LocalIntegrityObservationId>();
        var outlets = new HashSet<LocalGasStoreId>();
        foreach (var observer in mechanical.IntegrityObservers)
        {
            _ = Store(mechanical, observer.Store, observer.Body);
            _ = Collider(entry, observer.Body, observer.Collider);
            if (observer.Id == default || !ids.Add(observer.Id) || !outlets.Add(observer.Store) ||
                !float.IsFinite(observer.Threshold) || observer.Threshold < 0 ||
                !float.IsFinite(observer.MinimumApproachSpeed) || observer.MinimumApproachSpeed < 0)
                throw new ArgumentException("Invalid integrity identity, outlet writer or contact threshold.");
        }
    }

    private static void Ambient(BundleEntry entry, MechanicalPayload mechanical)
    {
        var frames = new[] { mechanical.MassFrame }.Concat(mechanical.Sliders.Select(s => s.MovingBody.MassFrame))
            .Concat(mechanical.Hinges.Select(h => h.MovingBody.MassFrame)).ToArray();
        var owners = new HashSet<PackageBodyIndex>();
        var parameters = entry.Metadata.Parameters;
        var values = parameters.ToDictionary(p => new ParameterId(p.Key), p => p.Default);
        var domain = new MassPolynomialDomain(parameters);
        foreach (var load in mechanical.AmbientLoads)
        {
            if (!owners.Add(load.Body) || load.Body.Value < 0 || load.Body.Value >= frames.Length ||
                load.Body.Value == 0 && mechanical.KinematicMode == KinematicMode.Static)
                throw new ArgumentException("Ambient load requires one dynamic body owner.");
            if (load.InventoryFraction is { } store) _ = Store(mechanical, store, load.Body);
            Parameter(load.LinearDrag.Parameter, ParameterUnit.InverseSecond, parameters);
            Parameter(load.Acceleration.Parameter, ParameterUnit.MetrePerSecondSquared, parameters);
            domain.Range(load.LinearDrag, 0, float.MaxValue);
            var frame = frames[load.Body.Value];
            var drag = domain.Bounds((frame.LinearDrag, 1), (load.LinearDrag, 1));
            if (!float.IsFinite(drag.Max)) throw new ArgumentException("Combined drag overflows.");
            var keys = new[] { frame.Gravity.Parameter, load.Acceleration.Parameter }.OfType<ParameterId>().Distinct().ToArray();
            foreach (var endpointValues in Corners(keys, parameters, values))
            {
                var baseAcceleration = frame.Gravity.Evaluate(endpointValues);
                var ambient = load.Acceleration.Evaluate(endpointValues);
                if ((baseAcceleration + ambient).LengthSquared() > 256)
                    throw new ArgumentException("Combined ambient acceleration leaves the physical domain.");
            }
        }
    }

    private static IEnumerable<Dictionary<ParameterId, float>> Corners(ParameterId[] keys,
        IReadOnlyList<ParameterDescriptor> parameters, Dictionary<ParameterId, float> defaults)
    {
        for (var mask = 0; mask < 1 << keys.Length; mask++)
        {
            var values = new Dictionary<ParameterId, float>(defaults);
            for (var i = 0; i < keys.Length; i++)
            {
                var p = parameters.Single(p => p.Key == keys[i].Value);
                values[keys[i]] = (mask & 1 << i) == 0 ? p.Minimum : p.Maximum;
            }
            yield return values;
        }
    }

    private static void Parameter(ParameterId? key, ParameterUnit unit, IReadOnlyList<ParameterDescriptor> parameters)
    {
        if (key is { } id && !parameters.Any(p => p.Key == id.Value && p.Kind == ParameterKind.Real &&
                p.Unit == unit && p.RunEdit == ParameterRunEdit.BuildOnly))
            throw new ArgumentException("Ambient load requires an exact BuildOnly parameter with physical units.");
    }
}
