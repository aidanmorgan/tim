using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum SimulationCadence : uint { Hz60 = 1, Hz120 = 2, Hz240 = 3 }
public enum PhysicalStepProfile : uint { Canonical480Hz = 1 }
public enum AnimationCadence : uint { Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4 }
public enum PresentationCadence : uint { AdmittedDisplay = 0, Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4, Hz144 = 5 }
public enum WorkshopSettingsVersion : uint { SharedMaster = 1 }
public enum WorldPlayback : uint { Building, Running, Paused, Completed, Faulted }

public readonly record struct CadenceRevision(ulong Value)
{
    public void Validate() { if (Value == 0) throw new ArgumentOutOfRangeException(nameof(Value)); }
}
public readonly record struct ProjectionEpoch(ulong Value)
{
    public void Validate() { if (Value == 0) throw new ArgumentOutOfRangeException(nameof(Value)); }
}
public readonly record struct PulseOrdinal(ulong Value);

/// <summary>A reduced external scheduling ratio, not a game-value or integration timestep.</summary>
public readonly record struct PulseRate(uint Numerator, uint Denominator)
{
    public void Validate()
    {
        if (Numerator == 0 || Numerator > 240_000 || Denominator == 0 || Denominator > 10_000 ||
            Numerator < 30UL * Denominator || Numerator > 240UL * Denominator ||
            GreatestCommonDivisor(Numerator, Denominator) != 1)
            throw new ArgumentException("Unsupported or noncanonical pulse rate.");
    }

    private static uint GreatestCommonDivisor(uint left, uint right)
    {
        while (right != 0) { var remainder = left % right; left = right; right = remainder; }
        return left;
    }
}

public readonly record struct WorkshopCadenceSettings(
    SimulationCadence Simulation,
    PhysicalStepProfile Physical,
    AnimationCadence Animation,
    PresentationCadence Presentation,
    PulseRate PresentationRate)
{
    public static WorkshopCadenceSettings Default() =>
        new WorkshopCadenceSettings(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz,
            AnimationCadence.Hz60, PresentationCadence.Hz60, new(60, 1)).Validated();

    public PulseRate SimulationRate => Simulation switch
    {
        SimulationCadence.Hz60 => new(60, 1),
        SimulationCadence.Hz120 => new(120, 1),
        SimulationCadence.Hz240 => new(240, 1),
        _ => throw new ArgumentException("Unknown simulation cadence.")
    };

    public PulseRate AnimationRate => Animation switch
    {
        AnimationCadence.Hz30 => new(30, 1),
        AnimationCadence.Hz60 => new(60, 1),
        AnimationCadence.Hz90 => new(90, 1),
        AnimationCadence.Hz120 => new(120, 1),
        _ => throw new ArgumentException("Unknown animation cadence.")
    };

    public uint PhysicalStepsPerCommit => Simulation switch
    {
        SimulationCadence.Hz60 => 8,
        SimulationCadence.Hz120 => 4,
        SimulationCadence.Hz240 => 2,
        _ => throw new ArgumentException("Unknown simulation cadence.")
    };

    public ulong RunTickLimit => checked(30UL * SimulationRate.Numerator);
    public const uint PhysicalFrequency = 480;
    public const ulong PhysicalOrdinalLimit = 30UL * PhysicalFrequency;

    public void Validate()
    {
        if (!Enum.IsDefined(Simulation) || Physical != PhysicalStepProfile.Canonical480Hz ||
            !Enum.IsDefined(Animation) || !Enum.IsDefined(Presentation))
            throw new ArgumentException("Unsupported cadence selection.");
        if ((ulong)SimulationRate.Numerator * PhysicalStepsPerCommit != PhysicalFrequency ||
            (uint)(Half)PhysicalFrequency != PhysicalFrequency)
            throw new ArgumentException("Physical profile rate is not exactly representable.");
        PresentationRate.Validate();
        var fixedRate = Presentation switch
        {
            PresentationCadence.AdmittedDisplay => PresentationRate,
            PresentationCadence.Hz30 => new PulseRate(30, 1),
            PresentationCadence.Hz60 => new PulseRate(60, 1),
            PresentationCadence.Hz90 => new PulseRate(90, 1),
            PresentationCadence.Hz120 => new PulseRate(120, 1),
            PresentationCadence.Hz144 => new PulseRate(144, 1),
            _ => throw new ArgumentException("Unknown presentation cadence.")
        };
        if (PresentationRate != fixedRate)
            throw new ArgumentException("Fixed presentation cadence has a different rate.");
    }

    private WorkshopCadenceSettings Validated() { Validate(); return this; }
}

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

/// <summary>
/// One world projection on the continuously advancing master. Paused world time does not
/// pause autonomous consumers; they continue using the master directly.
/// </summary>
public readonly record struct WorkshopWorldProjection(
    SimulationEpoch WorldGeneration, ProjectionEpoch Epoch, SimulationTick AnchorTick,
    MasterTimeNanoseconds AnchorMaster, WorldPlayback Playback)
{
    public void Validate(WorkshopCadenceSettings settings)
    {
        settings.Validate(); Epoch.Validate(); AnchorMaster.Validate();
        if (WorldGeneration.Value == 0 || !Enum.IsDefined(Playback) || AnchorTick.Value > settings.RunTickLimit ||
            (Playback == WorldPlayback.Building && AnchorTick.Value != 0) ||
            (Playback == WorldPlayback.Completed && AnchorTick.Value != settings.RunTickLimit))
            throw new ArgumentException("Invalid world projection.");
    }

    public SimulationTick DueTick(WorkshopCadenceSettings settings, MasterTimeNanoseconds now)
    {
        Validate(settings); now.Validate();
        if (now.Value < AnchorMaster.Value) throw new ArgumentException("Master time precedes the projection anchor.");
        if (Playback != WorldPlayback.Running) return AnchorTick;
        var due = WorkshopPulse.Due(settings.SimulationRate, new(now.Value - AnchorMaster.Value));
        var remaining = settings.RunTickLimit - AnchorTick.Value;
        return new(AnchorTick.Value + Math.Min(due.Value, remaining));
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
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)WorkshopSettingsVersion.SharedMaster);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], (uint)settings.Simulation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], (uint)settings.Physical);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], (uint)settings.Animation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[16..], (uint)settings.Presentation);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[20..], settings.PresentationRate.Numerator);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[24..], settings.PresentationRate.Denominator);
    }

    public static WorkshopCadenceSettings Read(ReadOnlySpan<byte> source)
    {
        if (source.Length != ByteLength ||
            BinaryPrimitives.ReadUInt32LittleEndian(source) != (uint)WorkshopSettingsVersion.SharedMaster ||
            BinaryPrimitives.ReadUInt32LittleEndian(source[28..]) != 0)
            throw new ArgumentException("Unsupported settings shape.");
        var settings = new WorkshopCadenceSettings(
            (SimulationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source[4..]),
            (PhysicalStepProfile)BinaryPrimitives.ReadUInt32LittleEndian(source[8..]),
            (AnimationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source[12..]),
            (PresentationCadence)BinaryPrimitives.ReadUInt32LittleEndian(source[16..]),
            new(BinaryPrimitives.ReadUInt32LittleEndian(source[20..]),
                BinaryPrimitives.ReadUInt32LittleEndian(source[24..])));
        settings.Validate();
        return settings;
    }
}

