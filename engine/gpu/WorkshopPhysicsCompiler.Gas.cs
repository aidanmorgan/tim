using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopPhysicsCompiler
{
    private sealed partial class Compilation
    {
        private readonly List<GasStoreDeclaration> gasStores = [];
        private readonly List<GasShapeDeclaration> gasShapes = [];
        private readonly List<InventoryMassDeclaration> inventoryMasses = [];
        private readonly List<AmbientLoadDeclaration> ambientLoads = [];
        private readonly List<ContactIntegrityDeclaration> integrityObservers = [];

        private void CompileGas()
        {
            var boundary = AtmosphereBoundary.For(construction.Atmosphere);
            foreach (var instance in construction.Instances)
            {
                var mechanical = DeclaredPhysics.Of(instance.PartId).Mechanical;
                if (mechanical is null) continue;
                var values = DeclaredPhysics.Parameters(instance).ToDictionary(p => new ParameterId(p.Key), p => p.Value);
                var rows = identities[instance.Id];
                foreach (var store in mechanical.GasStores.OrderBy(s => s.Id.Value))
                {
                    var reference = store.ReferenceCharge(values);
                    gasStores.Add(new(rows.GasStores[store.Id], identities.Body(instance.Id, store.Body), store.Material,
                        reference, boundary.Pressure, store.GaugePressure, store.OutletConductancePerReferenceMass * reference.Mass));
                }
                CompileInventoryMasses(instance.Id, mechanical, values);
                CompileGasShapes(instance.Id, mechanical);
                foreach (var load in mechanical.AmbientLoads)
                    ambientLoads.Add(new(identities.Body(instance.Id, load.Body), load.Acceleration.Evaluate(values) * boundary.RelativePressure,
                        load.LinearDrag.Evaluate(values) * boundary.RelativePressure,
                        load.InventoryFraction is { } id ? rows.GasStores[id] : null));
                foreach (var observer in mechanical.IntegrityObservers)
                    integrityObservers.Add(new(rows.IntegrityObservers[observer.Id],
                        identities.Collider(instance.Id, observer.Body, observer.Collider), rows.GasStores[observer.Store],
                        observer.Threshold, observer.MinimumApproachSpeed));
            }
        }

        private void CompileInventoryMasses(GpuBodyId owner, CuriousContraptions.Bundle.MechanicalPayload mechanical,
            IReadOnlyDictionary<ParameterId, float> parameters)
        {
            var frames = new[] { mechanical.MassFrame }.Concat(mechanical.Sliders.Select(s => s.MovingBody.MassFrame))
                .Concat(mechanical.Hinges.Select(h => h.MovingBody.MassFrame)).ToArray();
            for (var i = 0; i < frames.Length; i++)
            {
                var source = frames[i].Source;
                if (source.Kind != BodyMassSourceKind.CenteredInventory) continue;
                var resolved = source.Resolve(parameters, GasMassReferences.Resolve(new(i), mechanical.GasStores, parameters));
                inventoryMasses.Add(new(identities.Body(owner, new(i)), resolved.DryMass));
            }
        }

        private void CompileGasShapes(GpuBodyId owner, CuriousContraptions.Bundle.MechanicalPayload mechanical)
        {
            foreach (var shape in mechanical.GasShapes)
            {
                var storeId = identities[owner].GasStores[shape.Store];
                var store = gasStores.Single(s => s.Id == storeId);
                var colliderId = identities.Collider(owner, shape.Body, shape.Collider);
                var index = colliders.FindIndex(c => c.Id == colliderId);
                if (index < 0) throw new ArgumentException("Gas shape collider is absent.");
                var radius = IsobaricGas.OuterRadius(shape.DryRadius, store.Prepared.State.Volume);
                colliders[index] = colliders[index] with { Radius = new(radius) };
                gasShapes.Add(new(storeId, colliderId, shape.DryRadius));
            }
        }
    }
}
