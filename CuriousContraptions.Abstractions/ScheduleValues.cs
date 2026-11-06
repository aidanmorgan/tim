using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum ScheduleControlKind : uint { Prepare = 1, Prepared = 2, Commit = 3, Applied = 4, Abort = 5, Presented = 6 }
public enum ScheduleTransition : uint { Configure = 1, Run = 2, Pause = 3, Resume = 4, Step = 5, Completed = 6, Reset = 7 }

public readonly record struct WorkshopSchedule(
    WorkshopCadenceSettings Settings, CadenceRevision Revision, ClockGeneration MasterGeneration,
    MonotonicNanoseconds MasterNativeOrigin, MasterTimeNanoseconds InstalledAt,
    PulseOrdinal FirstAnimation, PulseOrdinal FirstPresentation,
    MasterTimeNanoseconds PriorAnimationApplied, MasterTimeNanoseconds PriorPresentationApplied,
    WorkshopWorldProjection World);

public readonly record struct ScheduleControlHeader(
    ScheduleControlKind Kind, RuntimeSessionId Session, ClockGeneration MasterGeneration,
    CadenceRevision Revision, ProjectionEpoch Projection,
    WorkshopRuntimeRole Sender, WorkshopRuntimeRole Recipient)
{
    public void Validate()
    {
        Session.Validate(); Revision.Validate(); Projection.Validate();
        if (MasterGeneration.Value == 0 || !Enum.IsDefined(Kind) || !Enum.IsDefined(Sender) || !Enum.IsDefined(Recipient))
            throw new ArgumentException("Invalid schedule-control identity.");
        var downstream = Kind is ScheduleControlKind.Prepare or ScheduleControlKind.Commit or ScheduleControlKind.Abort;
        var validRoute = downstream
            ? Sender == WorkshopRuntimeRole.Simulation && Recipient is WorkshopRuntimeRole.Browser or WorkshopRuntimeRole.Animation
            : Recipient == WorkshopRuntimeRole.Simulation && Sender is WorkshopRuntimeRole.Browser or WorkshopRuntimeRole.Animation;
        if (!validRoute || (Kind == ScheduleControlKind.Presented && Sender != WorkshopRuntimeRole.Browser))
            throw new ArgumentException("Unsupported schedule-control route.");
    }

}
