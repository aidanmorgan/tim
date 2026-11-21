using System;

namespace CuriousContraptions.Gpu;

/// <summary>Exact committed read content; inline storage and decoded motion wrappers have no reference authority.</summary>
public readonly partial record struct WorkshopRead(SimulationEpoch Epoch, SimulationTick Tick, PhysicalCommitRevision PhysicalRevision, PhysicsBodyReadSet Bodies, AuthorityRevision Revision = default, WorkshopClockStamp? Capture = null,
    PhysicsCaptureRead Captures = default, IPhysicsMotionRead? Motion = null, PhysicsActivationRead Activations = default, PhysicsTimerRead Timers = default, PhysicsContactWorkRead ContactWorks = default, PhysicsElectricalRead Electrical = default,
    PhysicsMemoryRead Memory = default, PhysicsPresentationRead Presentation = default, PhysicsDriveTargetRead DriveTargets = default, PhysicsEnergyStoreRead EnergyStores = default,
    PhysicsChamberRead ChamberRegions = default, PhysicsFiniteWorkActuatorRead FiniteWorkActuators = default,
    PhysicsGasRead GasStores = default, PhysicsIntegrityRead IntegrityObservers = default, PhysicsAirflowRead Airflow = default)
{
    public bool HasSameContent(WorkshopRead other) => Epoch == other.Epoch && Tick == other.Tick &&
        Revision == other.Revision && PhysicalRevision == other.PhysicalRevision && Capture == other.Capture && SamePhysicalContent(this, other);
    public bool Equals(WorkshopRead other) => HasSameContent(other);
    // Identity hashes avoid rehashing a full motion record. Equal content always shares these identities.
    public override int GetHashCode() => HashCode.Combine(Epoch, Tick, PhysicalRevision, Revision, Capture);

    public static bool SamePhysicalContent(WorkshopRead first, WorkshopRead second)
    {
        if (first.PhysicalRevision != second.PhysicalRevision || !first.Bodies.HasSameBits(second.Bodies) ||
            first.Captures.Count != second.Captures.Count || first.Activations.Count != second.Activations.Count ||
            first.Timers.Count != second.Timers.Count || !first.ContactWorks.HasSameBits(second.ContactWorks) || !first.Electrical.HasSameBits(second.Electrical) ||
            !first.Memory.HasSameBits(second.Memory) || !first.Presentation.HasSameBits(second.Presentation) ||
            !first.DriveTargets.HasSameBits(second.DriveTargets) || !first.EnergyStores.HasSameBits(second.EnergyStores) ||
            !first.ChamberRegions.HasSameBits(second.ChamberRegions) || !first.FiniteWorkActuators.HasSameBits(second.FiniteWorkActuators) ||
            !first.GasStores.HasSameValues(second.GasStores) || !first.IntegrityObservers.HasSameValues(second.IntegrityObservers) ||
            !first.Airflow.HasSameValues(second.Airflow)) return false;
        if (!SameMotion(first.Motion, second.Motion)) return false;
        return SameCaptures(first.Captures, second.Captures) && SameActivations(first.Activations, second.Activations) &&
            SameTimers(first.Timers, second.Timers);
    }

    private static bool SameCaptures(PhysicsCaptureRead first, PhysicsCaptureRead second)
    {
        for (var i = 0; i < first.Count; i++)
        {
            var left = first[i]; var right = second[i];
            if (left.Sensor != right.Sensor || left.Target != right.Target || left.Phase != right.Phase || left.EventOrdinal != right.EventOrdinal ||
                !F32Bits.Equal(left.EventPhase, right.EventPhase)) return false;
        }
        return true;
    }

    private static bool SameActivations(PhysicsActivationRead first, PhysicsActivationRead second)
    {
        for (var i = 0; i < first.Count; i++)
        {
            var left = first[i]; var right = second[i];
            if (left != right || !F32Bits.Equal(left.EventPhase, right.EventPhase) ||
                !F32Bits.Equal(left.ApproachSpeed.Value, right.ApproachSpeed.Value)) return false;
        }
        return true;
    }

    private static bool SameTimers(PhysicsTimerRead first, PhysicsTimerRead second)
    {
        for (var i = 0; i < first.Count; i++) if (first[i] != second[i]) return false;
        return true;
    }
    private static bool SameMotion(IPhysicsMotionRead? first, IPhysicsMotionRead? second) =>
        first is null ? second is null : second is not null && first.Bytes.SequenceEqual(second.Bytes);

}
