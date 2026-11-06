using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Gpu;

public static class CaptureLatchController
{
    public static CaptureLatch Consume(this CaptureLatch latch, ResidenceRead candidate)
    {
        latch.Validate();
        if (candidate.Id != latch.Sensor || candidate.Target != latch.Target) throw new ArgumentException("Qualification belongs to a different sensor.");
        if (candidate.OccurrenceCount > 1) throw new ArgumentException("Qualification batch exceeds its bound.");
        if (latch.Phase == CaptureLatchPhase.Latched || candidate.OccurrenceCount == 0) return latch;
        // The live episode may already be Outside. Its earned occurrence still belongs
        // to this candidate and must reach the latch before the joint world commit.
        if (!PhysicalPhase.IsAdmitted(candidate.EventOrdinal, candidate.EventPhase))
            throw new ArgumentException("Qualification time is invalid.");
        return new(latch.Sensor, latch.Target, CaptureLatchPhase.Latched, candidate.EventOrdinal, candidate.EventPhase);
    }

}
