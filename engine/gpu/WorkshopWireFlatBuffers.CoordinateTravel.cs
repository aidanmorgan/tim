using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Gpu;

public static partial class WorkshopWireFlatBuffers
{
    private static Offset<W.CoordinateTravelDeclarationRecord> EncodeCoordinateTravel(
        FlatBufferBuilder builder, CoordinateTravelDeclaration value) =>
        W.CoordinateTravelDeclarationRecord.CreateCoordinateTravelDeclarationRecord(builder,
            value.Id.Value, value.Owner.Value, value.Coordinate.Value, value.Threshold);

    private static CoordinateTravelDeclaration DecodeCoordinateTravel(W.CoordinateTravelDeclarationRecord row) =>
        new(new(row.Id), new(row.Owner), new(row.Coordinate), row.Threshold);

    private static VectorOffset EncodeCoordinateTravelRead(FlatBufferBuilder builder, PhysicsCoordinateTravelRead values)
    {
        W.WorkshopResponse.StartCoordinateTravelVector(builder, values.Count);
        for (var i = values.Count - 1; i >= 0; i--)
        {
            var value = values[i];
            W.CoordinateTravelReadRecord.CreateCoordinateTravelReadRecord(builder, value.Id.Value, value.Owner.Value,
                value.Threshold, value.Travel, (uint)value.Phase, value.Ordinal);
        }
        return builder.EndVector();
    }

    private static PhysicsCoordinateTravelRead DecodeCoordinateTravelRead(W.WorkshopResponse response)
    {
        var values = new CoordinateTravelRead[response.CoordinateTravelLength];
        for (var i = 0; i < values.Length; i++)
        {
            var row = Required(response.CoordinateTravel(i), "coordinate travel read");
            values[i] = new(new(row.Id), new(row.Owner), row.Threshold, row.Travel,
                ObservationEnum<CoordinateTravelPhase>(row.Phase), row.Ordinal);
        }
        return new(values);
    }
}
