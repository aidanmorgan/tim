namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopDelivery(WorkshopResponse Response, bool Applicable);
public readonly record struct WorkshopCompletion(WorkshopCommandResult Result, bool Applicable);

public interface ICursorPreparation
{
    WorkshopResponse Response { get; }
    bool Applicable { get; }
}

public interface IPoseHistoryPreparation { }

public interface IWorkshopClientCursor
{
    SimulationEpoch Epoch { get; }
    AuthorityRevision Revision { get; }
    ulong ReadOrder { get; }
    WorkshopSimulationPhase Phase { get; }
    ICursorPreparation PrepareRead(WorkshopResponse response);
    ICursorPreparation PrepareAcknowledgement(WorkshopCommand command, WorkshopResponse response, ulong dispatchedReadOrder, bool disposed);
    ICursorPreparation PrepareInstallation(WorkshopResponse response);
    void Commit(ICursorPreparation candidate);
}

public interface IWorkshopPoseHistory
{
    PresentationQuality Quality { get; }
    int Count { get; }
    WorkshopResponse? Latest { get; }
    IPoseHistoryPreparation PrepareInstall(WorkshopSchedule schedule, WorkshopResponse seed, MappedCapture? mapped, PresentationBoundary boundary, ulong clockEpoch);
    IPoseHistoryPreparation Prepare(WorkshopResponse response, MappedCapture? mapped, PresentationBoundary boundary, ulong clockEpoch);
    void Commit(IPoseHistoryPreparation candidate);
    bool TryTakeLatest(out WorkshopResponse response);
    bool TryPresent(MonotonicNanoseconds now, MasterTimeNanoseconds selectedMaster, IClockMapping clock, out WorkshopPresentationSample sample);
    long LatestAge(MonotonicNanoseconds now);
    void Retire();
}
