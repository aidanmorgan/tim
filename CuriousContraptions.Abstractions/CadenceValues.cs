using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum PhysicalStepProfile : uint { Canonical480Hz = 1 }
public enum AnimationCadence : uint { Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4 }
public enum PresentationCadence : uint { AdmittedDisplay = 0, Hz30 = 1, Hz60 = 2, Hz90 = 3, Hz120 = 4, Hz144 = 5 }
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
    internal const string NoncanonicalMessage = "Unsupported or noncanonical pulse rate.";

    public void Validate()
    {
        if (!IsCanonical) throw new ArgumentException(NoncanonicalMessage);
    }

    internal bool IsCanonical => Numerator != 0 && Numerator <= 240_000 && Denominator != 0 && Denominator <= 10_000 &&
        Numerator >= 30UL * Denominator && Numerator <= 240UL * Denominator && GreatestCommonDivisor(Numerator, Denominator) == 1;

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
        _ => throw new InvalidOperationException("Unknown simulation cadence.")
    };

    public PulseRate AnimationRate => Animation switch
    {
        AnimationCadence.Hz30 => new(30, 1),
        AnimationCadence.Hz60 => new(60, 1),
        AnimationCadence.Hz90 => new(90, 1),
        AnimationCadence.Hz120 => new(120, 1),
        _ => throw new InvalidOperationException("Unknown animation cadence.")
    };

    public uint PhysicalStepsPerCommit => Simulation switch
    {
        SimulationCadence.Hz60 => 8,
        SimulationCadence.Hz120 => 4,
        SimulationCadence.Hz240 => 2,
        _ => throw new InvalidOperationException("Unknown simulation cadence.")
    };

    public ulong RunTickLimit => checked(30UL * SimulationRate.Numerator);
    public const uint PhysicalFrequency = 480;
    public const ulong PhysicalOrdinalLimit = 30UL * PhysicalFrequency;

    public void Validate()
    {
        if (Rejection() is { } reason) throw new ArgumentException(reason);
    }

    /// <summary>Why these settings are not admitted, or null when they are: the one rule <see cref="Validate"/> enforces and a
    /// stored save's pre-validation reports as its typed refusal.</summary>
    public string? Rejection()
    {
        if (!Enum.IsDefined(Simulation) || Physical != PhysicalStepProfile.Canonical480Hz ||
            !Enum.IsDefined(Animation) || !Enum.IsDefined(Presentation))
            return "Unsupported cadence selection.";
        if ((ulong)SimulationRate.Numerator * PhysicalStepsPerCommit != PhysicalFrequency)
            return "Physical profile rate is not exactly representable.";
        if (!PresentationRate.IsCanonical) return PulseRate.NoncanonicalMessage;
        var fixedRate = Presentation switch
        {
            PresentationCadence.AdmittedDisplay => PresentationRate,
            PresentationCadence.Hz30 => new PulseRate(30, 1),
            PresentationCadence.Hz60 => new PulseRate(60, 1),
            PresentationCadence.Hz90 => new PulseRate(90, 1),
            PresentationCadence.Hz120 => new PulseRate(120, 1),
            PresentationCadence.Hz144 => new PulseRate(144, 1),
            _ => default,
        };
        return PresentationRate != fixedRate ? "Fixed presentation cadence has a different rate." : null;
    }

    private WorkshopCadenceSettings Validated() { Validate(); return this; }
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


}
