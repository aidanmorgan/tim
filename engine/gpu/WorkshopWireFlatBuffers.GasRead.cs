using System;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{

    private static VectorOffset EncodeGasStoreRead(FlatBufferBuilder builder, PhysicsGasRead values)
    {
        W.WorkshopResponse.StartGasStoresVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var v = values[i];
            W.GasStoreReadRecord.CreateGasStoreReadRecord(builder, v.Id.Value, v.Body.Value, v.State.Mass, v.State.Volume, v.State.Energy, (uint)v.Outlet,
                v.EnthalpyOut, v.BoundaryWork, v.ElasticReleased, v.MomentumOut.X, v.MomentumOut.Y, v.MomentumOut.Z,
                v.KineticOut, v.PreparationWork, v.PreparationElastic);
        }
        return builder.EndVector();
    }

    private static PhysicsGasRead DecodeGasStoreRead(W.WorkshopResponse response)
    {
        var values = new GasStoreRead[response.GasStoresLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = response.GasStores(i)!.Value;
            values[i] = new(new(r.Id), new(r.Body), new(r.Mass, r.Volume, r.Energy), ObservationEnum<GasOutletPhase>(r.Outlet),
                r.EnthalpyOut, r.BoundaryWork, r.ElasticReleased, new(r.MomentumX, r.MomentumY, r.MomentumZ),
                r.KineticOut, r.PreparationWork, r.PreparationElastic);
        }
        return new(values);
    }

    private static VectorOffset EncodeIntegrityRead(FlatBufferBuilder builder, PhysicsIntegrityRead values)
    {
        W.WorkshopResponse.StartIntegrityObserversVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var v = values[i];
            W.IntegrityReadRecord.CreateIntegrityReadRecord(builder, v.Id.Value, (uint)v.Phase, v.Ordinal, v.EventPhase);
        }
        return builder.EndVector();
    }

    private static PhysicsIntegrityRead DecodeIntegrityRead(W.WorkshopResponse response)
    {
        var values = new IntegrityRead[response.IntegrityObserversLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = response.IntegrityObservers(i)!.Value;
            values[i] = new(new(r.Id), ObservationEnum<IntegrityPhase>(r.Phase), r.Ordinal, r.EventPhase);
        }
        return new(values);
    }
}
