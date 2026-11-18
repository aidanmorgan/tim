using System;
using Google.FlatBuffers;
using CuriousContraptions.Abstractions.Mechanical;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static VectorOffset EncodeRetentions(FlatBufferBuilder builder, ReadOnlySpan<PrismaticRetentionRead> values)
    {
        if (values.IsEmpty) return default;
        var offsets = new Offset<W.PrismaticRetentionReadRecord>[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var r = values[i];
            offsets[i] = W.PrismaticRetentionReadRecord.CreatePrismaticRetentionReadRecord(builder,
                r.Id.Value, r.Owner.Value, r.Coordinate.Value, (byte)r.Mode, (byte)r.Phase, r.Compression, r.Potential,
                r.AcceptedElasticEnergy, r.ReleasedElasticEnergy, r.WindingWork, r.PositiveWindingTransfer,
                r.ContactWork, r.GravityWork, r.RequestResult is { } result ? (byte)result : null);
        }
        return W.WorkshopResponse.CreateRetentionsVector(builder, offsets);
    }

    private static PrismaticRetentionRead[] DecodeRetentions(W.WorkshopResponse response)
    {
        var values = new PrismaticRetentionRead[response.RetentionsLength];
        for (var i = 0; i < values.Length; i++)
        {
            var r = Required(response.Retentions(i), "retention observation");
            values[i] = new(new(r.Id), new(r.Owner), new(r.Coordinate),
                ObservationEnum<RetentionMode>(r.Mode), ObservationEnum<RetentionPhase>(r.Phase),
                r.Compression, r.Potential, r.AcceptedElasticEnergy, r.ReleasedElasticEnergy,
                r.WindingWork, r.PositiveWindingTransfer, r.ContactWork, r.GravityWork,
                r.RequestResult is { } result ? ObservationEnum<RetentionRequestResult>(result) : null);
        }
        return values;
    }
}
