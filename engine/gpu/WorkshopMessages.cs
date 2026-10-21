using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopCommandKind : uint { Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save, ConfigureElectrical }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, WorkshopConstruction? Construction, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact, RuntimeSessionId Session = default,
    CadenceRevision Cadence = default, ProjectionEpoch Projection = default, WorkshopCadenceSettings? Settings = null, WorkshopElectricalControl? Electrical = null);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision, CadenceRevision Cadence, ProjectionEpoch Projection);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read, RuntimeSessionId Session = default, PublicationSequence Publication = default,
    CadenceRevision Cadence = default, ClockGeneration MasterGeneration = default,
    ProjectionEpoch Projection = default, PulseOrdinal SourcePulse = default);

/// <summary>The domain rules of Workshop commands and responses that every encoder and decoder of the one codec
/// (<see cref="WorkshopWireFlatBuffers"/>) applies.</summary>
public static class WorkshopMessages
{
    public static bool Matches(WorkshopCommand command, SimulationEpoch epoch, AuthorityRevision revision,
        RuntimeSessionId session, CadenceRevision cadence, ProjectionEpoch projection) =>
        command.Session == session && command.Epoch == epoch && command.Cadence == cadence && command.Projection == projection &&
        (command.RevisionKind == ExpectedRevisionKind.Any || command.Revision == revision);

    /// <summary>A committed read past admission carries its committed motion; admission (tick 0) has no preceding interval.</summary>
    internal static void ValidatePhysicalRead(WorkshopRead read)
    {
        read.Bodies.ValidateTime(read.Epoch, read.Tick);
        for (var i = 0; i < read.Bodies.Count; i++) read.Bodies[i].Validate();
        if (read.Motion is { } motion) motion.ValidateBinding(read.Bodies, read.Tick);
        else if (read.Tick.Value != 0) throw new ArgumentException("Committed motion description is required.");
        for (var i = 0; i < read.Activations.Count; i++)
        {
            var value = read.Activations[i]; value.Validate();
            var requiresDynamicBody = value.Kind is not (ActivationOccurrenceKind.OscillatorTick or ActivationOccurrenceKind.CounterReached or
                ActivationOccurrenceKind.TimerElapsed or ActivationOccurrenceKind.ContactLoad);
            if (value.Phase == ActivationPhase.Latched && ((requiresDynamicBody && !read.Bodies.TryGet(value.Body, out _)) ||
                value.EventOrdinal > read.Tick.Value * 8 || read.Tick.Value == 0))
                throw new ArgumentException("Activation event does not belong to a running physical world.");
        }
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var value = read.Captures[i]; value.Validate();
            if (value.EventOrdinal > WorkshopCadenceSettings.PhysicalOrdinalLimit)
                throw new ArgumentException("Capture time exceeds the world duration.");
        }
    }

    internal static void ValidateScheduleIdentity(WorkshopResponse response)
    {
        if (response.Read.Epoch.Value == 0)
        {
            if (response.Cadence.Value != 0 || response.Projection.Value != 0 || response.MasterGeneration.Value != 0 || response.SourcePulse.Value != 0)
                throw new ArgumentException("Uninstalled response contains a schedule identity.");
        }
        else if (response.Cadence.Value == 0 || response.Projection.Value == 0 || response.MasterGeneration.Value == 0 ||
            response.Read.Capture?.Generation != response.MasterGeneration || response.SourcePulse.Value != response.Read.Tick.Value)
            throw new ArgumentException("Physical response does not bind its installed master schedule.");
    }

    internal static void ValidateCapture(WorkshopRead read)
    {
        if (read.Capture is { } capture)
        {
            capture.Validate();
            if (capture.Domain != WorkshopClockDomain.SimulationMonotonic || read.Epoch.Value == 0)
                throw new ArgumentException("Physical read requires its simulation native clock.");
        }
        else if (read.Epoch.Value != 0 || read.Tick.Value != 0 || read.Revision.Value != 0 || read.Bodies.Count != 0 || read.Captures.Count != 0 || read.Activations.Count != 0 || read.Timers.Count != 0 || read.ContactWorks.Count != 0 || read.ContactWorks.OccurrenceCount != 0 || read.Electrical.Count != 0 || read.Memory.Count != 0)
            throw new ArgumentException("An installed physical read requires its original capture.");
    }
}
