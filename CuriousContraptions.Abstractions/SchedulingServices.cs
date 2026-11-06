namespace CuriousContraptions.Gpu;

public interface ISchedulingServices
{
    void Validate(WorkshopSchedule schedule);
    void ValidateDisplay(WorkshopSchedule schedule, PulseRate admittedDisplayRate);
    WorkshopSchedule Create(WorkshopCadenceSettings settings, CadenceRevision revision,
        ClockGeneration masterGeneration, MonotonicNanoseconds masterNativeOrigin, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAnimationApplied, MasterTimeNanoseconds priorPresentationApplied,
        WorkshopWorldProjection world);
    bool ExceedsPhysicalDebt(WorkshopSchedule schedule, SimulationTick committedTick, MasterTimeNanoseconds masterNow);
    void Match(ScheduleControlHeader header, WorkshopSchedule schedule);
    PulseOrdinal Due(PulseRate rate, MasterTimeNanoseconds elapsed);
    MasterTimeNanoseconds Deadline(PulseRate rate, PulseOrdinal ordinal);
    PulseOrdinal FirstAfter(PulseRate rate, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAppliedUpper);
    MasterTimeNanoseconds FromNative(MonotonicNanoseconds native,
        MonotonicNanoseconds masterOrigin);
    SimulationTick DueTick(WorkshopWorldProjection projection, WorkshopCadenceSettings settings, MasterTimeNanoseconds now);
}
