using System;

namespace CuriousContraptions.Gpu;

public enum CaptureLatchPhase : uint { Clear, Latched }

/// <summary>Reusable discrete controller. Consume only a validated GPU candidate, then commit with that candidate.</summary>
public readonly record struct CaptureLatch(
    GpuSensorId Sensor, CaptureLatchPhase Phase, uint EventOrdinal, float EventPhase)
{
    public static CaptureLatch Clear(GpuSensorId sensor)
    {
        if (sensor.Value == 0) throw new ArgumentException("A sensor identity is required.", nameof(sensor));
        return new(sensor, CaptureLatchPhase.Clear, 0, 0f);
    }

    public CaptureLatch Consume(ResidenceRead candidate)
    {
        Validate();
        if (candidate.Id != Sensor) throw new ArgumentException("Qualification belongs to a different sensor.");
        if (candidate.OccurrenceCount > 1) throw new ArgumentException("Qualification batch exceeds its bound.");
        if (Phase == CaptureLatchPhase.Latched || candidate.OccurrenceCount == 0) return this;
        // The live episode may already be Outside. Its earned occurrence still belongs
        // to this candidate and must reach the latch before the joint world commit.
        if (!PhysicalPhase.IsAdmitted(candidate.EventOrdinal, candidate.EventPhase))
            throw new ArgumentException("Qualification time is invalid.");
        return new(Sensor, CaptureLatchPhase.Latched, candidate.EventOrdinal, candidate.EventPhase);
    }

    public void Validate()
    {
        if (Sensor.Value == 0 || !Enum.IsDefined(Phase) || !PhysicalPhase.IsAdmitted(EventOrdinal, EventPhase) ||
            (Phase == CaptureLatchPhase.Clear && (EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(EventPhase))))
            throw new ArgumentException("Capture latch is invalid.");
    }
}

/// <summary>The f32 phase of a physical time within its step ordinal: a finite value in [−2048, 2048) of the ordinal's 4096
/// phase units, never negative at ordinal zero.</summary>
public static class PhysicalPhase
{
    public const float Minimum = -2048f;
    public const float Limit = 2048f;
    public static bool IsAdmitted(uint ordinal, float phase) =>
        float.IsFinite(phase) && phase >= Minimum && phase < Limit && (ordinal != 0 || phase >= 0f);
}
