using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Every Workshop command and query (closed set). <see cref="WorkshopCommandKind.ObserveMemory"/> is the stopped memory
/// observation query (Story 9A.3), answered by its acknowledgement's <see cref="WorkshopResponse.Memory"/>.</summary>
public enum WorkshopCommandKind : uint { Initialize, Construct, Run, Reset, Dispose, Cancel, ConfigureCadence, Pause, Resume, Step, Save, ConfigureElectrical, ObserveMemory }
public enum WorkshopResponseKind : uint { Acknowledgement, Read }
public enum ExpectedRevisionKind : uint { Any = 1, Exact = 2 }
public readonly record struct WorkshopCommand(CommandSequence Sequence, WorkshopCommandKind Kind, SimulationEpoch Epoch, AuthorityRevision Revision, WorkshopCompiledScene? Scene, WorkshopCommandIdentity? Target = null, ExpectedRevisionKind RevisionKind = ExpectedRevisionKind.Exact, RuntimeSessionId Session = default,
    CadenceRevision Cadence = default, ProjectionEpoch Projection = default, WorkshopElectricalControl? Electrical = null);
public readonly record struct WorkshopCommandIdentity(CommandSequence Sequence, SimulationEpoch Epoch, AuthorityRevision Revision, CadenceRevision Cadence, ProjectionEpoch Projection);
public readonly record struct WorkshopResponse(CommandSequence Sequence, WorkshopResponseKind Kind,
    WorkshopCommandResult Result, WorkshopSimulationPhase Phase, WorkshopRead Read, RuntimeSessionId Session = default, PublicationSequence Publication = default,
    CadenceRevision Cadence = default, ClockGeneration MasterGeneration = default,
    ProjectionEpoch Projection = default, PulseOrdinal SourcePulse = default, WorkshopMemoryMeasurement? Memory = null, WorkshopCompiledScene? Scene = null);

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
            var requiresDynamicBody = value.Kind is not (ActivationOccurrenceKind.OscillatorTick or ActivationOccurrenceKind.ThresholdReached or
                ActivationOccurrenceKind.TimerElapsed or ActivationOccurrenceKind.ContactLoad);
            if (value.Phase == ActivationPhase.Latched && ((requiresDynamicBody && !read.Bodies.TryGet(value.Body, out _)) ||
                value.EventOrdinal > read.Tick.Value * 8 || read.Tick.Value == 0))
                throw new ArgumentException("Activation event does not belong to a running physical world.");
        }
        for (var i = 0; i < read.Captures.Count; i++)
        {
            var value = read.Captures[i]; value.Validate();
            if (!read.Bodies.TryGet(value.Target,out _) || value.EventOrdinal > WorkshopCadenceSettings.PhysicalOrdinalLimit)
                throw new ArgumentException("Capture time exceeds the world duration.");
        }
    }

    internal static void ValidatePresentationOwners(WorkshopRead read, WorkshopCompiledScene construction)
    {
        for(var i=0;i<read.Captures.Count;i++)
            if(!construction.OwnsCapture(new(read.Captures[i].Sensor,read.Captures[i].Target)))
                throw new ArgumentException("Capture observation names a foreign policy or target.");
        // Render body reads omit static bodies; ownership belongs to the authored construction.
        foreach (var actuator in read.Presentation.Actuators)
            if (!HasOwner(actuator.Owner)) throw new ArgumentException("Actuator observation names a foreign owner.");
        foreach (var load in read.Presentation.Loads)
            if (!HasOwner(load.Frame)) throw new ArgumentException("Contact-load observation names a foreign owner.");
        var expectedLogic=0;
        foreach(var node in construction.Physics.SupplyGraph.Nodes)
        {
            if(node.Kind != SupplyNodeKind.LogicGate) continue;
            expectedLogic++;
            var found=false;
            foreach(var logic in read.Presentation.Logic)
                if(logic.Owner==node.Owner)
                {
                    found=true;
                    var expected=logic.Supplied && CompiledSupplyGraph.Evaluate(node.Operation!.Value,
                        logic.At(WorkshopSocket.FirstIn),logic.At(WorkshopSocket.SecondIn));
                    if(logic.Active!=expected || (read.Tick.Value==0 &&
                        (logic.Inputs!=ElectricalInputState.Neither || logic.Supplied || logic.Active)))
                        throw new ArgumentException("Electrical logic observation contradicts its declared operation or admission state.");
                }
            if(read.Epoch.Value!=0 && !found)
                throw new ArgumentException("Electrical logic observation omits a declared owner.");
        }
        if(read.Presentation.Logic.Length!=(read.Epoch.Value==0 ? 0 : expectedLogic))
            throw new ArgumentException("Electrical logic observation population differs from its installed owners.");
        bool HasOwner(GpuBodyId owner) => construction.Owners.Contains(owner);
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
        else if (read.Epoch.Value != 0 || read.Tick.Value != 0 || read.Revision.Value != 0 || read.Bodies.Count != 0 || read.Captures.Count != 0 || read.Activations.Count != 0 || read.Timers.Count != 0 || read.ContactWorks.Count != 0 || read.Electrical.Count != 0 || read.Memory.Count != 0 || read.Presentation.Actuators.Length != 0 || read.Presentation.Loads.Length != 0 || read.Presentation.Logic.Length != 0)
            throw new ArgumentException("An installed physical read requires its original capture.");
    }
}
