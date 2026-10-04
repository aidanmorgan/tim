using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public enum WorkshopScheduleVersion : uint { SharedMaster = 1 }
public enum ScheduleControlKind : uint { Prepare = 1, Prepared = 2, Commit = 3, Applied = 4, Abort = 5, Presented = 6 }
public enum ScheduleTransition : uint { Configure = 1, Run = 2, Pause = 3, Resume = 4, Step = 5, Completed = 6, Reset = 7 }

public readonly record struct WorkshopSchedule(
    WorkshopCadenceSettings Settings, CadenceRevision Revision, ClockGeneration MasterGeneration,
    MonotonicNanoseconds MasterNativeOrigin, MasterTimeNanoseconds InstalledAt,
    PulseOrdinal FirstAnimation, PulseOrdinal FirstPresentation,
    MasterTimeNanoseconds PriorAnimationApplied, MasterTimeNanoseconds PriorPresentationApplied,
    WorkshopWorldProjection World)
{
    public void Validate()
    {
        Settings.Validate(); Revision.Validate(); MasterNativeOrigin.Validate(); InstalledAt.Validate();
        PriorAnimationApplied.Validate(); PriorPresentationApplied.Validate(); World.Validate(Settings);
        if (MasterGeneration.Value == 0 || World.AnchorMaster.Value > InstalledAt.Value ||
            FirstAnimation != WorkshopPulse.FirstAfter(Settings.AnimationRate, InstalledAt, PriorAnimationApplied) ||
            FirstPresentation != WorkshopPulse.FirstAfter(Settings.PresentationRate, InstalledAt, PriorPresentationApplied))
            throw new ArgumentException("Invalid shared-master schedule.");
        // A valid ordinal alone does not prove its timestamp fits the admitted clock representation.
        var animationDeadline = WorkshopPulse.Deadline(Settings.AnimationRate, FirstAnimation);
        var presentationDeadline = WorkshopPulse.Deadline(Settings.PresentationRate, FirstPresentation);
        checked
        {
            _ = MasterNativeOrigin.Value + InstalledAt.Value;
            _ = MasterNativeOrigin.Value + animationDeadline.Value;
            _ = MasterNativeOrigin.Value + presentationDeadline.Value;
        }
    }

    public void ValidateDisplay(PulseRate admittedDisplayRate)
    {
        Validate(); admittedDisplayRate.Validate();
        if (Settings.Presentation == PresentationCadence.AdmittedDisplay &&
            Settings.PresentationRate != admittedDisplayRate)
            throw new ArgumentException("Schedule does not match the admitted display rate.");
    }

    public static WorkshopSchedule Create(WorkshopCadenceSettings settings, CadenceRevision revision,
        ClockGeneration masterGeneration, MonotonicNanoseconds masterNativeOrigin, MasterTimeNanoseconds installedAt,
        MasterTimeNanoseconds priorAnimationApplied, MasterTimeNanoseconds priorPresentationApplied,
        WorkshopWorldProjection world)
    {
        var result = new WorkshopSchedule(settings, revision, masterGeneration, masterNativeOrigin, installedAt,
            WorkshopPulse.FirstAfter(settings.AnimationRate, installedAt, priorAnimationApplied),
            WorkshopPulse.FirstAfter(settings.PresentationRate, installedAt, priorPresentationApplied),
            priorAnimationApplied, priorPresentationApplied, world);
        result.Validate();
        return result;
    }

    /// <summary>Uncapped elapsed debt: a late final tick must not hide overload at the Run limit.</summary>
    public bool ExceedsPhysicalDebt(SimulationTick committedTick, MasterTimeNanoseconds masterNow)
    {
        Validate(); masterNow.Validate();
        if (committedTick.Value < World.AnchorTick.Value || committedTick.Value > Settings.RunTickLimit ||
            masterNow.Value < World.AnchorMaster.Value)
            throw new ArgumentException("Invalid physical debt endpoint.");
        if (World.Playback != WorldPlayback.Running) return false;
        var elapsedScaled = checked((Int128)(masterNow.Value - World.AnchorMaster.Value) * Settings.SimulationRate.Numerator);
        var committedScaled = checked((Int128)(committedTick.Value - World.AnchorTick.Value) * WorkshopPulse.NanosecondsPerSecond);
        return elapsedScaled - committedScaled > (Int128)100_000_000 * Settings.SimulationRate.Numerator;
    }
}

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

    public void Match(WorkshopSchedule schedule)
    {
        Validate(); schedule.Validate();
        if (Revision != schedule.Revision || Projection != schedule.World.Epoch || MasterGeneration != schedule.MasterGeneration)
            throw new ArgumentException("Header and schedule identities differ.");
    }
}

public static class WorkshopScheduleWire
{
    public const int HeaderBytes = 64;
    public const int ScheduleBytes = 144;
    public const int PrepareBytes = 80 + WorkshopWire.ResponseBytes;
    public const int ResultBytes = 16;
    public const int AbortBytes = 8;
    public const int MaximumControlBytes = HeaderBytes + PrepareBytes;

    public static int PayloadBytes(ScheduleControlKind kind) => kind switch
    {
        ScheduleControlKind.Prepare => PrepareBytes,
        ScheduleControlKind.Commit => ScheduleBytes,
        ScheduleControlKind.Prepared or ScheduleControlKind.Applied or ScheduleControlKind.Presented => ResultBytes,
        ScheduleControlKind.Abort => AbortBytes,
        _ => throw new ArgumentException("Undefined schedule-control kind.")
    };

    public static void WriteHeader(ScheduleControlHeader header, Span<byte> destination)
    {
        header.Validate();
        if (destination.Length != HeaderBytes) throw new ArgumentException("Invalid control header length.");
        destination.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)WorkshopScheduleVersion.SharedMaster);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], (uint)header.Kind);
        WorkshopWire.WriteSession(destination[8..24], header.Session);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], header.MasterGeneration.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[32..], header.Revision.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[40..], header.Projection.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[48..], (uint)header.Sender);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[52..], (uint)header.Recipient);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[56..], checked((uint)PayloadBytes(header.Kind)));
    }

    public static ScheduleControlHeader ReadHeader(ReadOnlySpan<byte> control)
    {
        if (control.Length < HeaderBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(control) != (uint)WorkshopScheduleVersion.SharedMaster ||
            BinaryPrimitives.ReadUInt32LittleEndian(control[60..]) != 0)
            throw new ArgumentException("Unsupported schedule-control header.");
        var kind = (ScheduleControlKind)BinaryPrimitives.ReadUInt32LittleEndian(control[4..]);
        var payloadBytes = PayloadBytes(kind);
        if (control.Length != HeaderBytes + payloadBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(control[56..]) != payloadBytes)
            throw new ArgumentException("Unsupported schedule-control length.");
        var header = new ScheduleControlHeader(kind, WorkshopWire.ReadSession(control[8..24]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(control[24..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(control[32..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(control[40..])),
            (WorkshopRuntimeRole)BinaryPrimitives.ReadUInt32LittleEndian(control[48..]),
            (WorkshopRuntimeRole)BinaryPrimitives.ReadUInt32LittleEndian(control[52..]));
        header.Validate();
        return header;
    }

    public static void WriteSchedule(WorkshopSchedule schedule, Span<byte> destination)
    {
        schedule.Validate();
        if (destination.Length != ScheduleBytes) throw new ArgumentException("Invalid schedule length.");
        destination.Clear();
        WorkshopCadenceWire.Write(schedule.Settings, destination[..32]);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[32..], schedule.Revision.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[40..], schedule.MasterGeneration.Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination[48..], schedule.MasterNativeOrigin.Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination[56..], schedule.InstalledAt.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[64..], schedule.FirstAnimation.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[72..], schedule.FirstPresentation.Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination[80..], schedule.PriorAnimationApplied.Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination[88..], schedule.PriorPresentationApplied.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[96..], schedule.World.WorldGeneration.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[104..], schedule.World.AnchorTick.Value);
        BinaryPrimitives.WriteInt64LittleEndian(destination[112..], schedule.World.AnchorMaster.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[120..], (uint)schedule.World.Playback);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[128..], schedule.World.Epoch.Value);
    }

    public static WorkshopSchedule ReadSchedule(ReadOnlySpan<byte> source)
    {
        if (source.Length != ScheduleBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(source[124..]) != 0 ||
            BinaryPrimitives.ReadUInt64LittleEndian(source[136..]) != 0)
            throw new ArgumentException("Unsupported schedule shape.");
        var result = new WorkshopSchedule(WorkshopCadenceWire.Read(source[..32]),
            new(BinaryPrimitives.ReadUInt64LittleEndian(source[32..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(source[40..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(source[48..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(source[56..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(source[64..])),
            new(BinaryPrimitives.ReadUInt64LittleEndian(source[72..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(source[80..])),
            new(BinaryPrimitives.ReadInt64LittleEndian(source[88..])),
            new(new(BinaryPrimitives.ReadUInt64LittleEndian(source[96..])),
                new(BinaryPrimitives.ReadUInt64LittleEndian(source[128..])),
                new(BinaryPrimitives.ReadUInt64LittleEndian(source[104..])),
                new(BinaryPrimitives.ReadInt64LittleEndian(source[112..])),
                (WorldPlayback)BinaryPrimitives.ReadUInt32LittleEndian(source[120..])));
        result.Validate();
        return result;
    }

    public static void WriteCommit(ScheduleControlHeader header, WorkshopSchedule schedule, Span<byte> destination)
    {
        header.Match(schedule);
        if (header.Kind != ScheduleControlKind.Commit || destination.Length != HeaderBytes + ScheduleBytes)
            throw new ArgumentException("Invalid schedule Commit.");
        // No writes precede complete admission, including first-deadline representability.
        WriteHeader(header, destination[..HeaderBytes]);
        WriteSchedule(schedule, destination[HeaderBytes..]);
    }

    public static WorkshopSchedule ReadCommit(ReadOnlySpan<byte> control, out ScheduleControlHeader header)
    {
        var admittedHeader = ReadHeader(control);
        if (admittedHeader.Kind != ScheduleControlKind.Commit) throw new ArgumentException("Expected schedule Commit.");
        var schedule = ReadSchedule(control[HeaderBytes..]);
        admittedHeader.Match(schedule);
        header = admittedHeader;
        return schedule;
    }
}
