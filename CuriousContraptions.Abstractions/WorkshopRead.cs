using System;

namespace CuriousContraptions.Gpu;

/// <summary>Exact committed read content; inline storage and decoded motion wrappers have no reference authority.</summary>
public readonly partial record struct WorkshopRead(SimulationEpoch Epoch, SimulationTick Tick, PhysicsBodyReadSet Bodies, AuthorityRevision Revision = default, WorkshopClockStamp? Capture = null,
    PhysicsCaptureRead Captures = default, IPhysicsMotionRead? Motion = null, PhysicsActivationRead Activations = default, PhysicsTimerRead Timers = default, PhysicsContactWorkRead ContactWorks = default, PhysicsElectricalRead Electrical = default,
    PhysicsMemoryRead Memory = default, PhysicsPresentationRead Presentation = default, PhysicsDriveTargetRead DriveTargets = default)
{
    public bool HasSameContent(WorkshopRead other) => Epoch == other.Epoch && Tick == other.Tick &&
        Revision == other.Revision && Capture == other.Capture && SamePhysicalContent(this, other);
    public bool Equals(WorkshopRead other) => HasSameContent(other);
    // Identity hashes avoid rehashing a full motion record. Equal content always shares these identities.
    public override int GetHashCode() => HashCode.Combine(Epoch, Tick, Revision, Capture);

    public static bool SamePhysicalContent(WorkshopRead first, WorkshopRead second)
    {
        if (!first.Bodies.HasSameBits(second.Bodies) ||
            first.Captures.Count != second.Captures.Count || first.Activations.Count != second.Activations.Count ||
            first.Timers.Count != second.Timers.Count || !first.ContactWorks.HasSameBits(second.ContactWorks) || !first.Electrical.HasSameBits(second.Electrical) ||
            !first.Memory.HasSameBits(second.Memory) || !first.Presentation.HasSameBits(second.Presentation) ||
            !first.DriveTargets.HasSameBits(second.DriveTargets)) return false;
        if (!SameMotion(first.Motion, second.Motion)) return false;
        for (var i = 0; i < first.Captures.Count; i++)
        {
            var left = first.Captures[i]; var right = second.Captures[i];
            if (left.Sensor != right.Sensor || left.Target != right.Target || left.Phase != right.Phase || left.EventOrdinal != right.EventOrdinal ||
                !F32Bits.Equal(left.EventPhase, right.EventPhase)) return false;
        }
        for (var i = 0; i < first.Activations.Count; i++)
        {
            var left = first.Activations[i]; var right = second.Activations[i];
            if (left != right || !F32Bits.Equal(left.EventPhase, right.EventPhase) ||
                !F32Bits.Equal(left.ApproachSpeed.Value, right.ApproachSpeed.Value)) return false;
        }
        for (var i = 0; i < first.Timers.Count; i++) if (first.Timers[i] != second.Timers[i]) return false;
        return true;
    }
    private static bool SameMotion(IPhysicsMotionRead? first, IPhysicsMotionRead? second) =>
        first is null ? second is null : second is not null && first.Bytes.SequenceEqual(second.Bytes);

}
