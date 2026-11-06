using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Gpu;

public enum CaptureLatchPhase : uint { Clear, Latched }

/// <summary>Reusable discrete controller. Consume only a validated GPU candidate, then commit with that candidate.</summary>
public readonly record struct CaptureLatch(
    GpuSensorId Sensor, GpuBodyId Target, CaptureLatchPhase Phase, uint EventOrdinal, float EventPhase)
{
    public static CaptureLatch Clear(GpuSensorId sensor, GpuBodyId target)
    {
        if (sensor.Value == 0 || target.Value == 0) throw new ArgumentException("A sensor identity is required.", nameof(sensor));
        return new(sensor, target, CaptureLatchPhase.Clear, 0, 0f);
    }

    public void Validate()
    {
        if (Sensor.Value == 0 || Target.Value == 0 || !Enum.IsDefined(Phase) || !PhysicalPhase.IsAdmitted(EventOrdinal, EventPhase) ||
            (Phase == CaptureLatchPhase.Clear && (EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(EventPhase))))
            throw new ArgumentException("Capture latch is invalid.");
    }
}
