using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopDelivery(WorkshopResponse Response, bool Applicable);
public readonly record struct WorkshopCompletion(WorkshopCommandResult Result, bool Applicable);

/// <summary>Presentation admission only. Original reliable results are never rewritten.</summary>
public sealed class WorkshopClientCursor
{
    public SimulationEpoch Epoch { get; private set; } = new(1);
    public AuthorityRevision Revision { get; private set; }
    public ulong ReadOrder { get; private set; }
    private SimulationTick _tick;

    public bool AcceptRead(WorkshopResponse response)
    {
        if (response.Kind != WorkshopResponseKind.Read) throw new ArgumentException("Expected a physical read.");
        if (response.Read.Epoch.Value > Epoch.Value) throw new ArgumentException("An unsolicited read cannot install a generation.");
        if (response.Read.Epoch != Epoch || response.Read.Revision.Value < Revision.Value ||
            response.Read.Tick.Value < _tick.Value) return false;
        if (ReadOrder == ulong.MaxValue) throw new InvalidOperationException("Presentation read order exhausted.");
        ReadOrder++;
        Revision = response.Read.Revision;
        _tick = response.Read.Tick;
        return true;
    }

    public WorkshopDelivery Acknowledge(WorkshopCommand command, WorkshopResponse response, ulong dispatchedReadOrder, bool disposed)
    {
        if (response.Kind != WorkshopResponseKind.Acknowledgement || response.Sequence != command.Sequence)
            throw new ArgumentException("Worker acknowledgement does not own this command.");
        if (disposed || response.Read.Epoch.Value < Epoch.Value)
            return new(response, false);
        var nextEpoch = response.Read.Epoch.Value > Epoch.Value;
        if (nextEpoch && (response.Result.Outcome != WorkshopCommandOutcome.Applied ||
            command.Kind is not (WorkshopCommandKind.Construct or WorkshopCommandKind.Reset) ||
            command.Epoch != Epoch || Epoch.Value == ulong.MaxValue ||
            response.Read.Epoch.Value != Epoch.Value + 1))
            throw new ArgumentException("Acknowledgement cannot install this generation.");
        if ((!nextEpoch && response.Read.Tick.Value < _tick.Value) ||
            response.Read.Revision.Value < Revision.Value ||
            (!nextEpoch && response.Read.Revision == Revision && dispatchedReadOrder != ReadOrder))
            return new(response, false);
        Epoch = response.Read.Epoch;
        Revision = response.Read.Revision;
        _tick = response.Read.Tick;
        return new(response, true);
    }
}
