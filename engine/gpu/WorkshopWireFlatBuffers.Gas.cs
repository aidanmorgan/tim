using System;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{

    private static VectorOffset EncodeGasStores(FlatBufferBuilder builder, ReadOnlySpan<GasStoreDeclaration> values)
    {
        W.CompiledSceneRecord.StartGasStoresVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var v = values[i];
            W.GasStoreDeclarationRecord.CreateGasStoreDeclarationRecord(builder, v.Id.Value, v.Body.Value, v.Material.R, v.Material.Cv, v.Material.MinimumTemperature,
                v.Material.MaximumTemperature, v.Material.MaximumDensity, v.Material.MaximumPressure,
                v.Reference.Mass, v.Reference.Volume, v.Reference.Energy, v.BoundaryPressure, v.GaugePressure, v.Conductance);
        }
        return builder.EndVector();
    }

    private static GasStoreDeclaration[] DecodeGasStores(W.CompiledSceneRecord scene)
    {
        var values = new GasStoreDeclaration[scene.GasStoresLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = scene.GasStores(i)!.Value;
            values[i] = new(new(r.Id), new(r.Body), new(r.GasR, r.GasCv, r.MinimumTemperature, r.MaximumTemperature, r.MaximumDensity, r.MaximumPressure),
                new(r.ReferenceMass, r.ReferenceVolume, r.ReferenceEnergy), r.BoundaryPressure, r.GaugePressure, r.Conductance);
        }
        return values;
    }

    private static VectorOffset EncodeGasShapes(FlatBufferBuilder builder, ReadOnlySpan<GasShapeDeclaration> values)
    {
        W.CompiledSceneRecord.StartGasShapesVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var v = values[i];
            W.GasShapeDeclarationRecord.CreateGasShapeDeclarationRecord(builder, v.Store.Value, v.Collider.Value, v.DryRadius);
        }
        return builder.EndVector();
    }

    private static GasShapeDeclaration[] DecodeGasShapes(W.CompiledSceneRecord scene)
    {
        var values = new GasShapeDeclaration[scene.GasShapesLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = scene.GasShapes(i)!.Value;
            values[i] = new(new(r.Store), new(r.Collider), r.DryRadius);
        }
        return values;
    }

    private static VectorOffset EncodeInventoryMasses(FlatBufferBuilder builder, ReadOnlySpan<InventoryMassDeclaration> values)
    {
        W.CompiledSceneRecord.StartInventoryMassesVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var v = values[i];
            W.InventoryMassDeclarationRecord.CreateInventoryMassDeclarationRecord(builder, v.Body.Value, v.DryMass);
        }
        return builder.EndVector();
    }

    private static InventoryMassDeclaration[] DecodeInventoryMasses(W.CompiledSceneRecord scene)
    {
        var values = new InventoryMassDeclaration[scene.InventoryMassesLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = scene.InventoryMasses(i)!.Value;
            values[i] = new(new(r.Body), r.DryMass);
        }
        return values;
    }

    private static VectorOffset EncodeAmbientLoads(FlatBufferBuilder builder, ReadOnlySpan<AmbientLoadDeclaration> values)
    {
        W.CompiledSceneRecord.StartAmbientLoadsVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var v = values[i];
            W.AmbientLoadDeclarationRecord.CreateAmbientLoadDeclarationRecord(builder, v.Body.Value, v.InventoryFraction?.Value ?? 0, v.Acceleration.X, v.Acceleration.Y, v.Acceleration.Z, v.LinearDrag);
        }
        return builder.EndVector();
    }

    private static AmbientLoadDeclaration[] DecodeAmbientLoads(W.CompiledSceneRecord scene)
    {
        var values = new AmbientLoadDeclaration[scene.AmbientLoadsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = scene.AmbientLoads(i)!.Value;
            values[i] = new(new(r.Body), new(r.AccelerationX, r.AccelerationY, r.AccelerationZ), r.LinearDrag, r.InventoryStore == 0 ? null : new GasStoreId(r.InventoryStore));
        }
        return values;
    }

    private static VectorOffset EncodeIntegrityObservers(FlatBufferBuilder builder, ReadOnlySpan<ContactIntegrityDeclaration> values)
    {
        W.CompiledSceneRecord.StartIntegrityObserversVector(builder, values.Length);
        for (var i = values.Length - 1; i >= 0; i--)
        {
            var v = values[i];
            W.IntegrityDeclarationRecord.CreateIntegrityDeclarationRecord(builder, v.Id.Value, v.Collider.Value, v.Store.Value, v.Threshold, v.MinimumApproachSpeed);
        }
        return builder.EndVector();
    }

    private static ContactIntegrityDeclaration[] DecodeIntegrityObservers(W.CompiledSceneRecord scene)
    {
        var values = new ContactIntegrityDeclaration[scene.IntegrityObserversLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = scene.IntegrityObservers(i)!.Value;
            values[i] = new(new(r.Id), new(r.Collider), new(r.Store), r.Threshold, r.MinimumApproachSpeed);
        }
        return values;
    }
}
