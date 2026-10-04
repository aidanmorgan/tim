using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopDelivery(WorkshopResponse Response, bool Applicable);
public readonly record struct WorkshopCompletion(WorkshopCommandResult Result, bool Applicable);

/// <summary>Staged presentation admission. Original reliable results are never rewritten.</summary>
public sealed class WorkshopClientCursor
{
    private readonly RuntimeSessionId _session;
    public SimulationEpoch Epoch { get; private set; } = new(1);
    public AuthorityRevision Revision { get; private set; }
    public ulong ReadOrder { get; private set; }
    private SimulationTick _tick;
    private PublicationSequence _publication;
    public WorkshopSimulationPhase Phase { get; private set; }
    private ulong _version;

    public WorkshopClientCursor(RuntimeSessionId session) { session.Validate(); _session = session; }

    public readonly record struct Prepared(WorkshopClientCursor Owner, ulong Version,
        WorkshopResponse Response, bool Applicable, ulong ReadOrder, PublicationSequence Publication);

    public Prepared PrepareRead(WorkshopResponse response)
    {
        if (response.Kind != WorkshopResponseKind.Read || response.Session != _session || response.Publication.Value == 0)
            throw new ArgumentException("Expected an owned physical publication.");
        if (response.Read.Epoch.Value > Epoch.Value) throw new ArgumentException("An unsolicited read cannot install a generation.");
        var applicable = response.Publication.Value > _publication.Value &&
            response.Read.Epoch == Epoch && response.Read.Revision.Value >= Revision.Value &&
            response.Read.Tick.Value >= _tick.Value;
        if (applicable && (ReadOrder == ulong.MaxValue || _version == ulong.MaxValue))
            throw new InvalidOperationException("Presentation read order exhausted.");
        return new(this, _version, response, applicable, applicable ? ReadOrder + 1 : ReadOrder,
            response.Publication.Value > _publication.Value ? response.Publication : _publication);
    }

    public Prepared PrepareAcknowledgement(WorkshopCommand command, WorkshopResponse response, ulong dispatchedReadOrder, bool disposed)
    {
        if (command.Session != _session || response.Session != _session ||
            response.Kind != WorkshopResponseKind.Acknowledgement || response.Sequence != command.Sequence ||
            response.Publication.Value != 0)
            throw new ArgumentException("Worker acknowledgement does not own this command.");
        var applicable = !disposed && response.Read.Epoch.Value >= Epoch.Value;
        var nextEpoch = response.Read.Epoch.Value > Epoch.Value;
        if (applicable && nextEpoch && (response.Result.Outcome != WorkshopCommandOutcome.Applied ||
            command.Kind is not (WorkshopCommandKind.Construct or WorkshopCommandKind.Reset) ||
            command.Epoch != Epoch || Epoch.Value == ulong.MaxValue ||
            response.Read.Epoch.Value != Epoch.Value + 1))
            throw new ArgumentException("Acknowledgement cannot install this generation.");
        if ((!nextEpoch && response.Read.Tick.Value < _tick.Value) ||
            response.Read.Revision.Value < Revision.Value ||
            (!nextEpoch && response.Read.Revision == Revision && dispatchedReadOrder != ReadOrder))
            applicable = false;
        if (applicable && _version == ulong.MaxValue) throw new InvalidOperationException("Presentation cursor exhausted.");
        return new(this, _version, response, applicable, ReadOrder, _publication);
    }

    public Prepared PrepareInstallation(WorkshopResponse response)
    {
        if (response.Session != _session || response.Read.Epoch.Value < Epoch.Value ||
            response.Read.Revision.Value < Revision.Value || _version == ulong.MaxValue)
            throw new ArgumentException("Invalid schedule endpoint cursor.");
        return new(this, _version, response, true, ReadOrder, _publication);
    }

    public void Commit(Prepared prepared)
    {
        if (!ReferenceEquals(prepared.Owner, this) || prepared.Version != _version)
            throw new InvalidOperationException("Prepared cursor no longer owns this recipient.");
        if (!prepared.Applicable) return;
        Phase = prepared.Response.Phase;
        Epoch = prepared.Response.Read.Epoch;
        Revision = prepared.Response.Read.Revision;
        _tick = prepared.Response.Read.Tick;
        ReadOrder = prepared.ReadOrder;
        _publication = prepared.Publication;
        _version++;
    }
}
