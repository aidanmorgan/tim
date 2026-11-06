using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreSchedulingServices : ISchedulingServices
{
    public void Validate(WorkshopSchedule schedule) => WorkshopScheduleRules.Validate(schedule);
    public void ValidateDisplay(WorkshopSchedule schedule, PulseRate admittedDisplayRate) => WorkshopScheduleRules.ValidateDisplay(schedule, admittedDisplayRate);
    public WorkshopSchedule Create(WorkshopCadenceSettings settings, CadenceRevision revision,
        ClockGeneration masterGeneration, MonotonicNanoseconds masterNativeOrigin, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAnimationApplied, MasterTimeNanoseconds priorPresentationApplied,
        WorkshopWorldProjection world) => WorkshopScheduleRules.Create(settings, revision, masterGeneration, masterNativeOrigin, installedAt, priorAnimationApplied, priorPresentationApplied, world);
    public bool ExceedsPhysicalDebt(WorkshopSchedule schedule, SimulationTick committedTick, MasterTimeNanoseconds masterNow) => WorkshopScheduleRules.ExceedsPhysicalDebt(schedule, committedTick, masterNow);
    public void Match(ScheduleControlHeader header, WorkshopSchedule schedule) => WorkshopScheduleRules.Match(header, schedule);
    public PulseOrdinal Due(PulseRate rate, MasterTimeNanoseconds elapsed) => WorkshopPulse.Due(rate, elapsed);
    public MasterTimeNanoseconds Deadline(PulseRate rate, PulseOrdinal ordinal) => WorkshopPulse.Deadline(rate, ordinal);
    public PulseOrdinal FirstAfter(PulseRate rate, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAppliedUpper) => WorkshopPulse.FirstAfter(rate, installedAt, priorAppliedUpper);
    public MasterTimeNanoseconds FromNative(MonotonicNanoseconds native,
        MonotonicNanoseconds masterOrigin) => WorkshopPulse.FromNative(native, masterOrigin);
    public SimulationTick DueTick(WorkshopWorldProjection projection, WorkshopCadenceSettings settings, MasterTimeNanoseconds now) => WorkshopProjection.DueTick(projection, settings, now);
}
