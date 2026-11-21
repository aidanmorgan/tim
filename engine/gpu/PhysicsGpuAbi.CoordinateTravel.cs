using System;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int CoordinateTravelBytes = 32;
    public const int CoordinateTravelOwnerOffset = 8;
    public const int CoordinateTravelCoordinateOffset = 12;
    public const int CoordinateTravelThresholdOffset = 16;
    public const int CoordinateTravelAccumulatedOffset = 20;
    public const int CoordinateTravelPhaseOffset = 24;
    public const int CoordinateTravelOrdinalOffset = 28;

    private static void AdmitCoordinateTravel(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.CoordinateTravelObservers.Length; i++)
        {
            var observer = scene.CoordinateTravelObservers[i];
            var row = data.Slice(scene.World.CoordinateTravelOffset(i), CoordinateTravelBytes);
            U64(row, RecordIdentityOffset, observer.Id.Value);
            U32(row, CoordinateTravelOwnerOffset, BodySlot(scene, observer.Owner));
            U32(row, CoordinateTravelCoordinateOffset, checked((uint)scene.Layout.GetRevoluteSlot(observer.Coordinate.Value)));
            F32(row, CoordinateTravelThresholdOffset, observer.Threshold);
        }
    }

    public static CoordinateTravelRead[] ReadCoordinateTravel(ReadOnlySpan<byte> world)
    {
        var layout = Layout(world);
        var values = new CoordinateTravelRead[layout.CoordinateTravelObservers];
        for (var i = 0; i < values.Length; i++)
        {
            var row = world.Slice(layout.CoordinateTravelOffset(i), CoordinateTravelBytes);
            var owner = R32(row, CoordinateTravelOwnerOffset);
            if (owner >= layout.Bodies || R32(row, CoordinateTravelCoordinateOffset) >= layout.Revolutes ||
                R32(row, CoordinateTravelPhaseOffset) > (uint)CoordinateTravelPhase.Reached)
                throw new ArgumentException("Unresolved coordinate travel row.");
            values[i] = new(new(R64(row, RecordIdentityOffset)), new(R64(world, layout.BodyOffset((int)owner))),
                RF(row, CoordinateTravelThresholdOffset), RF(row, CoordinateTravelAccumulatedOffset),
                (CoordinateTravelPhase)R32(row, CoordinateTravelPhaseOffset), R32(row, CoordinateTravelOrdinalOffset));
            values[i].Validate();
            if (values[i].Ordinal > R32(world, HeaderSourceOrdinalOffset))
                throw new ArgumentException("Coordinate travel occurrence is outside committed time.");
        }
        return values;
    }

    private static void ValidateCoordinateTravelCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var layout = Layout(source);
        var before = ReadCoordinateTravel(source);
        var after = ReadCoordinateTravel(candidate);
        for (var i = 0; i < after.Length; i++)
        {
            var offset = layout.CoordinateTravelOffset(i);
            if (!candidate.Slice(offset, CoordinateTravelAccumulatedOffset).SequenceEqual(source.Slice(offset, CoordinateTravelAccumulatedOffset)))
                throw new ArgumentException("Coordinate travel declaration changed.");
            CoordinateTravelReadValidation.Advance(after[i], before[i], R32(source, HeaderSourceOrdinalOffset));
        }
    }
}
