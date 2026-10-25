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
