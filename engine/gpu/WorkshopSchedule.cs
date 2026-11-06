using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static class WorkshopScheduleRules
{

    public static void Validate(this WorkshopSchedule schedule)
    {
        schedule.Settings.Validate(); schedule.Revision.Validate(); schedule.MasterNativeOrigin.Validate(); schedule.InstalledAt.Validate();
        schedule.PriorAnimationApplied.Validate(); schedule.PriorPresentationApplied.Validate(); schedule.World.Validate(schedule.Settings);
        if (schedule.MasterGeneration.Value == 0 || schedule.World.AnchorMaster.Value > schedule.InstalledAt.Value ||
            schedule.FirstAnimation != WorkshopPulse.FirstAfter(schedule.Settings.AnimationRate, schedule.InstalledAt, schedule.PriorAnimationApplied) ||
            schedule.FirstPresentation != WorkshopPulse.FirstAfter(schedule.Settings.PresentationRate, schedule.InstalledAt, schedule.PriorPresentationApplied))
            throw new ArgumentException("Invalid shared-master schedule.");
        // A valid ordinal alone does not prove its timestamp fits the admitted clock representation.
        var animationDeadline = WorkshopPulse.Deadline(schedule.Settings.AnimationRate, schedule.FirstAnimation);
        var presentationDeadline = WorkshopPulse.Deadline(schedule.Settings.PresentationRate, schedule.FirstPresentation);
        checked
        {
            _ = schedule.MasterNativeOrigin.Value + schedule.InstalledAt.Value;
            _ = schedule.MasterNativeOrigin.Value + animationDeadline.Value;
            _ = schedule.MasterNativeOrigin.Value + presentationDeadline.Value;
        }
    }

    public static void ValidateDisplay(this WorkshopSchedule schedule, PulseRate admittedDisplayRate)
    {
        schedule.Validate(); admittedDisplayRate.Validate();
        if (schedule.Settings.Presentation == PresentationCadence.AdmittedDisplay &&
            schedule.Settings.PresentationRate != admittedDisplayRate)
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
    public static bool ExceedsPhysicalDebt(this WorkshopSchedule schedule, SimulationTick committedTick, MasterTimeNanoseconds masterNow)
    {
        schedule.Validate(); masterNow.Validate();
        if (committedTick.Value < schedule.World.AnchorTick.Value || committedTick.Value > schedule.Settings.RunTickLimit ||
            masterNow.Value < schedule.World.AnchorMaster.Value)
            throw new ArgumentException("Invalid physical debt endpoint.");
        if (schedule.World.Playback != WorldPlayback.Running) return false;
        var elapsedScaled = checked((Int128)(masterNow.Value - schedule.World.AnchorMaster.Value) * schedule.Settings.SimulationRate.Numerator);
        var committedScaled = checked((Int128)(committedTick.Value - schedule.World.AnchorTick.Value) * WorkshopPulse.NanosecondsPerSecond);
        return elapsedScaled - committedScaled > (Int128)100_000_000 * schedule.Settings.SimulationRate.Numerator;
    }
    public static void Match(this ScheduleControlHeader header, WorkshopSchedule schedule)
    {
        header.Validate(); schedule.Validate();
        if (header.Revision != schedule.Revision || header.Projection != schedule.World.Epoch || header.MasterGeneration != schedule.MasterGeneration)
            throw new ArgumentException("Header and schedule identities differ.");
    }
}

public static class WorkshopScheduleWire
{
    public const int HeaderBytes = 64;
    public const int ScheduleBytes = 144;
    /// <summary>Prepare's fixed fields before its embedded endpoint response.</summary>
    public const int PrepareFieldBytes = 80;
    public const int ResultBytes = 16;
    public const int AbortBytes = 8;

    /// <summary>A Prepare payload: its fixed fields, then the endpoint's complete size-prefixed FlatBuffers response, whose width is
    /// that read's own populations (Story 9R.5b: no fixed response capacity). The endpoint decoder rejects any other content.</summary>
    public static int PrepareBytes(int endpointBytes) => endpointBytes > 0
        ? checked(PrepareFieldBytes + endpointBytes)
        : throw new ArgumentException("A prepared endpoint is empty.");

    public static int PayloadBytes(ScheduleControlKind kind) => kind switch
    {
        ScheduleControlKind.Commit => ScheduleBytes,
        ScheduleControlKind.Prepared or ScheduleControlKind.Applied or ScheduleControlKind.Presented => ResultBytes,
        ScheduleControlKind.Abort => AbortBytes,
        ScheduleControlKind.Prepare => throw new ArgumentException("A Prepare's width is its endpoint's (PrepareBytes)."),
        _ => throw new ArgumentException("Undefined schedule-control kind.")
    };

    /// <summary>Writes a fixed-width control's header.</summary>
    public static void WriteHeader(ScheduleControlHeader header, Span<byte> destination) =>
        WriteHeader(header, destination, PayloadBytes(header.Kind));

    /// <summary>Writes a header declaring its payload width (a Prepare's comes from its endpoint).</summary>
    public static void WriteHeader(ScheduleControlHeader header, Span<byte> destination, int payloadBytes)
    {
        header.Validate();
        if (header.Kind == ScheduleControlKind.Prepare ? payloadBytes <= PrepareFieldBytes
            : payloadBytes != PayloadBytes(header.Kind))
            throw new ArgumentException("Invalid schedule-control payload width.");
        if (destination.Length != HeaderBytes) throw new ArgumentException("Invalid control header length.");
        destination.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], (uint)header.Kind);
        header.Session.WriteTo(destination[8..24]);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], header.MasterGeneration.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[32..], header.Revision.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[40..], header.Projection.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[48..], (uint)header.Sender);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[52..], (uint)header.Recipient);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[56..], checked((uint)payloadBytes));
    }

    public static ScheduleControlHeader ReadHeader(ReadOnlySpan<byte> control)
    {
        if (control.Length < HeaderBytes ||
            BinaryPrimitives.ReadUInt32LittleEndian(control) != 0 || // bytes 0..3: zero padding (one bundle, one schema)
            BinaryPrimitives.ReadUInt32LittleEndian(control[60..]) != 0)
            throw new ArgumentException("Unsupported schedule-control header.");
        var kind = (ScheduleControlKind)BinaryPrimitives.ReadUInt32LittleEndian(control[4..]);
        var declared = BinaryPrimitives.ReadUInt32LittleEndian(control[56..]);
        // A Prepare declares its own width (its endpoint's populations); every other kind is fixed.
        if (!Enum.IsDefined(kind) || (kind == ScheduleControlKind.Prepare
                ? declared <= PrepareFieldBytes
                : declared != PayloadBytes(kind)) ||
            control.Length != HeaderBytes + (long)declared)
            throw new ArgumentException("Unsupported schedule-control length.");
        var header = new ScheduleControlHeader(kind, RuntimeSessionCodec.Read(control[8..24]),
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
