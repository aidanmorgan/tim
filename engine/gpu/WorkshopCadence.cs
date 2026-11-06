using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>Absolute ordinal arithmetic. No rounded period is accumulated.</summary>
public static class WorkshopPulse
{
    public const long NanosecondsPerSecond = 1_000_000_000;

    public static PulseOrdinal Due(PulseRate rate, MasterTimeNanoseconds elapsed)
    {
        rate.Validate(); elapsed.Validate();
        var numerator = checked((Int128)elapsed.Value * rate.Numerator);
        var denominator = checked((Int128)NanosecondsPerSecond * rate.Denominator);
        return new(checked((ulong)(numerator / denominator)));
    }

    /// <summary>Earliest integral timestamp at which the exact rational pulse is due.</summary>
    public static MasterTimeNanoseconds Deadline(PulseRate rate, PulseOrdinal ordinal)
    {
        rate.Validate();
        var numerator = checked((Int128)ordinal.Value * NanosecondsPerSecond * rate.Denominator);
        var denominator = (Int128)rate.Numerator;
        var ceiling = numerator / denominator + (numerator % denominator == 0 ? 0 : 1);
        return new(checked((long)ceiling));
    }

    public static PulseOrdinal FirstAfter(PulseRate rate, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAppliedUpper)
    {
        installedAt.Validate(); priorAppliedUpper.Validate();
        var due = Due(rate, new(Math.Max(installedAt.Value, priorAppliedUpper.Value)));
        return new(checked(due.Value + 1));
    }

    public static MasterTimeNanoseconds FromNative(MonotonicNanoseconds native,
        MonotonicNanoseconds masterOrigin)
    {
        native.Validate(); masterOrigin.Validate();
        if (native.Value < masterOrigin.Value)
            throw new ArgumentException("Native time precedes the admitted master origin.");
        return new(checked(native.Value - masterOrigin.Value));
    }
}

public static class WorkshopProjection
{
    public static SimulationTick DueTick(this WorkshopWorldProjection projection, WorkshopCadenceSettings settings, MasterTimeNanoseconds now)
    {
        projection.Validate(settings); now.Validate();
        if (now.Value < projection.AnchorMaster.Value) throw new ArgumentException("Master time precedes the projection anchor.");
        if (projection.Playback != WorldPlayback.Running) return projection.AnchorTick;
        var due = WorkshopPulse.Due(settings.SimulationRate, new(now.Value - projection.AnchorMaster.Value));
        var remaining = settings.RunTickLimit - projection.AnchorTick.Value;
        return new(projection.AnchorTick.Value + Math.Min(due.Value, remaining));
    }
}

/// <summary>The sole current settings encoding. Noncanonical values and reserved bytes reject.</summary>
public static class WorkshopCadenceWire
{
    public const int ByteLength = 32;

    public static void Write(WorkshopCadenceSettings settings, Span<byte> destination)
    {
        settings.Validate();
        if (destination.Length != ByteLength) throw new ArgumentException("Invalid settings length.");
        destination.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)settings.Simulation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], (uint)settings.Physical);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], (uint)settings.Animation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], (uint)settings.Presentation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[16..], settings.PresentationRate.Numerator);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[20..], settings.PresentationRate.Denominator);
    }

    /// <summary>Six u32 fields and 8 zero padding bytes (one bundle, one schema). Enum ranges and the canonical
    /// ratio are checked by <see cref="WorkshopCadenceSettings.Validate"/>.</summary>
    public static WorkshopCadenceSettings Read(ReadOnlySpan<byte> source)
    {
        if (source.Length != ByteLength || BinaryPrimitives.ReadUInt64LittleEndian(source[24..]) != 0)
            throw new ArgumentException("Unsupported settings shape.");
        var settings = new WorkshopCadenceSettings(
            (SimulationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source),
            (PhysicalStepProfile)BinaryPrimitives.ReadUInt32LittleEndian(source[4..]),
            (AnimationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source[8..]),
            (PresentationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source[12..]),
            new(BinaryPrimitives.ReadUInt32LittleEndian(source[16..]),
                BinaryPrimitives.ReadUInt32LittleEndian(source[20..])));
        settings.Validate();
        return settings;
    }
}

