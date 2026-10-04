using System;

namespace CuriousContraptions.Gpu;

public enum PresentationBoundary { Continuous, Seed, Run, Completed }
public enum PresentationQuality { AwaitingHistory, Interpolated, ClockInvalid, Stale, Terminal, Faulted, Retired }
public readonly record struct PresentationSimulationTime(double Seconds);
public readonly record struct PresentedBody(GpuBodyId Id, CellOrigin Cell, LocalPosition Local, CanonicalRotation Rotation);
public readonly record struct WorkshopPresentationSample(PresentedBody? Body, MonotonicNanoseconds? DisplayWall,
    PresentationSimulationTime SimulationTime, PresentationQuality Quality, WorkshopPresentationEvidence Evidence = default, PhysicsCaptureRead Captures = default, WorkshopClockStamp? FeedbackCapture = null);
public readonly record struct PresentationEndpoint(SimulationTick Tick, WorkshopClockStamp Capture, MappedCapture? Mapped);
public readonly record struct WorkshopPresentationEvidence(MonotonicNanoseconds SelectedAt, ulong ClockEpoch, ulong DisplayEpoch,
    SimulationEpoch WorldEpoch, PresentationEndpoint? Latest, PresentationEndpoint? Before, PresentationEndpoint? After);

/// <summary>
/// Sole browser physical history: seven complete canonical records. Rendered poses have no
/// authoritative tick or velocity; interpolation cannot become a simulation input.
/// </summary>
public sealed class WorkshopPoseHistory
{
    public const int Capacity = 7;
    public const long DelayNanoseconds = 16_666_667;
    public const long StaleNanoseconds = 50_000_000;
    public const int ReservedHistoryBytes = Capacity * WorkshopWire.ResponseBytes + 1024;
    private readonly Entry[] _entries = new Entry[Capacity];
    private byte _count, _next;
    private sbyte _latest = -1, _terminal = -1;
    private ulong _version, _clockEpoch, _displayEpoch;
    private long _displayWall;
    private double _simulationTime;
    private bool _dirty, _retired, _displayTimingValid;
    private WorkshopSimulationPhase _phase;
    private WorkshopSchedule? _schedule;
    public PresentationQuality Quality { get; private set; } = PresentationQuality.AwaitingHistory;
    public int Count => _count;
    public WorkshopResponse? Latest => _latest < 0 ? null : _entries[_latest].Response;

    private readonly record struct Entry(WorkshopResponse Response, MappedCapture? Mapped);

    public Prepared PrepareInstall(WorkshopSchedule schedule, WorkshopResponse seed,
        MappedCapture? mapped, PresentationBoundary boundary, ulong clockEpoch)
    {
        schedule.Validate();
        if (_retired || boundary == PresentationBoundary.Continuous || (_schedule is { } old &&
            (schedule.MasterGeneration != old.MasterGeneration || schedule.MasterNativeOrigin != old.MasterNativeOrigin ||
             schedule.Revision.Value < old.Revision.Value || schedule.World.Epoch.Value <= old.World.Epoch.Value)))
            throw new ArgumentException("Stale or foreign presentation schedule.");
        if (seed.Read.Tick != schedule.World.AnchorTick || !MatchesPlayback(seed.Phase, schedule.World.Playback))
            throw new ArgumentException("Prepared endpoint does not match its projection anchor.");
        return PrepareCore(seed, mapped, boundary, clockEpoch, schedule);
    }

    public static bool MatchesPlayback(WorkshopSimulationPhase phase, WorldPlayback playback) => playback switch
    {
        WorldPlayback.Building => phase == WorkshopSimulationPhase.Building,
        WorldPlayback.Running => phase == WorkshopSimulationPhase.Running,
        WorldPlayback.Paused => phase == WorkshopSimulationPhase.Paused,
        WorldPlayback.Completed => phase == WorkshopSimulationPhase.Completed,
        WorldPlayback.Faulted => phase == WorkshopSimulationPhase.Faulted,
        _ => false
    };

    private WorkshopSchedule Schedule => _schedule ?? throw new InvalidOperationException("Presentation schedule is not installed.");
    private double SimulationFrequency => Schedule.Settings.SimulationRate.Numerator;

    public readonly struct Prepared
    {
        internal readonly WorkshopPoseHistory Owner;
        internal readonly ulong Version, ClockEpoch;
        internal readonly WorkshopResponse Response;
        internal readonly MappedCapture? Mapped;
        internal readonly bool Reseed, ReplaceLatest, Terminal;
        internal readonly WorkshopSchedule? Installing;
        internal Prepared(WorkshopPoseHistory owner, ulong version, ulong clockEpoch,
            WorkshopResponse response, MappedCapture? mapped, bool reseed, bool replaceLatest, bool terminal, WorkshopSchedule? installing)
        {
            Owner = owner; Version = version; ClockEpoch = clockEpoch; Response = response;
            Mapped = mapped; Reseed = reseed; ReplaceLatest = replaceLatest; Terminal = terminal; Installing = installing;
        }
    }

    public Prepared Prepare(WorkshopResponse response, MappedCapture? mapped, PresentationBoundary boundary, ulong clockEpoch) =>
        PrepareCore(response, mapped, boundary, clockEpoch, null);

    private Prepared PrepareCore(WorkshopResponse response, MappedCapture? mapped, PresentationBoundary boundary,
        ulong clockEpoch, WorkshopSchedule? installing)
    {
        if (_retired) throw new ObjectDisposedException(nameof(WorkshopPoseHistory));
        if (!Enum.IsDefined(boundary) || clockEpoch == 0 || _version == ulong.MaxValue)
            throw new ArgumentException("Invalid presentation ownership.");
        if (response.Read.Capture is not { } capture) throw new ArgumentException("Physical history requires a capture.");
        capture.Validate();
        var schedule = installing ?? Schedule;
        if (response.Cadence != schedule.Revision || response.Projection != schedule.World.Epoch ||
            response.MasterGeneration != schedule.MasterGeneration || response.Read.Epoch != schedule.World.WorldGeneration)
            throw new ArgumentException("Read belongs to another installed presentation schedule.");
        if (mapped is { } mapping && (mapping.BrowserInterval.Width < 0 ||
            mapping.BrowserInterval.Width > WorkshopClockMapping.MaximumMappedWidth ||
            mapping.BrowserInterval.Lower < 0 || (mapping.BrowserInterval.Lower + mapping.BrowserInterval.Upper) / 2 > long.MaxValue))
            throw new ArgumentException("Invalid mapped capture interval.");
        // Physical publication can overtake the original Run ACK. Its committed phase starts
        // the new segment even when the ACK's presentation read is already obsolete.
        var startsRun = _latest >= 0 && _phase == WorkshopSimulationPhase.Building &&
            response.Phase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Faulted;
        var reseed = boundary is PresentationBoundary.Seed or PresentationBoundary.Run or PresentationBoundary.Completed ||
            _clockEpoch != clockEpoch || startsRun;
        var replace = false;
        if (_latest >= 0)
        {
            var previous = _entries[_latest];
            if (response.Session != previous.Response.Session || response.Read.Epoch.Value < previous.Response.Read.Epoch.Value)
                throw new ArgumentException("History belongs to another or retired session.");
            if (response.Read.Epoch != previous.Response.Read.Epoch) reseed = true;
            if (boundary == PresentationBoundary.Completed && response.Read.Epoch == previous.Response.Read.Epoch &&
                response.Read.Tick == previous.Response.Read.Tick)
            {
                if (response.Read.Capture != previous.Response.Read.Capture ||
                    !SameBody(response.Read, previous.Response.Read))
                    throw new ArgumentException("A terminal physical endpoint cannot be restamped or changed.");
                if (_clockEpoch == clockEpoch) mapped = previous.Mapped;
            }
            if (!reseed)
            {
                if (response.Read.Tick.Value < previous.Response.Read.Tick.Value ||
                    response.Read.Revision.Value < previous.Response.Read.Revision.Value)
                    throw new ArgumentException("Physical history cannot regress.");
                if (response.Read.Ball?.Id != previous.Response.Read.Ball?.Id)
                    throw new ArgumentException("A body identity change requires a committed discontinuity.");
                if (response.Read.Tick == previous.Response.Read.Tick)
                {
                    if (response.Read.Capture != previous.Response.Read.Capture ||
                        !SameBody(response.Read, previous.Response.Read))
                        throw new ArgumentException("A repeated physical endpoint cannot be restamped or changed.");
                    replace = true;
                    mapped = previous.Mapped; // Preserve the original uncertainty, including no valid mapping.
                }
            }
        }
        return new(this, _version, clockEpoch, response, mapped, reseed, replace,
            boundary is PresentationBoundary.Seed or PresentationBoundary.Run or PresentationBoundary.Completed, installing);
    }

    public void Commit(Prepared prepared)
    {
        if (!ReferenceEquals(prepared.Owner, this) || prepared.Version != _version || _retired)
            throw new InvalidOperationException("Prepared history no longer owns this recipient.");
        // Schedule and its matching seed cross one versioned, synchronous commit boundary.
        if (prepared.Installing is { } installed) _schedule = installed;
        if (prepared.Reseed)
        {
            Array.Clear(_entries); _count = _next = 0; _latest = _terminal = -1;
            _simulationTime = prepared.Response.Read.Tick.Value / SimulationFrequency;
        }
        int index = prepared.ReplaceLatest && _latest >= 0 ? _latest : _next;
        if (_terminal == index) _terminal = -1;
        _entries[index] = new(prepared.Response, prepared.Mapped);
        if (!prepared.ReplaceLatest)
        {
            _next = checked((byte)((index + 1) % Capacity));
            _count = checked((byte)Math.Min(_count + 1, Capacity));
        }
        _latest = checked((sbyte)index);
        if (prepared.Terminal) _terminal = checked((sbyte)index);
        _clockEpoch = prepared.ClockEpoch; _phase = prepared.Response.Phase;
        _version++; _dirty = true;
    }

    public bool TryTakeLatest(out WorkshopResponse response)
    {
        if (!_dirty || _latest < 0) { response = default; return false; }
        response = _entries[_latest].Response; _dirty = false; return true;
    }

    public bool TryPresent(MonotonicNanoseconds now, MasterTimeNanoseconds selectedMaster, WorkshopClockMapping clock, out WorkshopPresentationSample sample)
    {
        var selected = TrySelect(now, selectedMaster, clock, out sample, out var before, out var after);
        var latest = _latest < 0 ? (Entry?)null : _entries[_latest];
        sample = sample with
        {
            DisplayWall = selected ? sample.DisplayWall : _displayTimingValid ? new(_displayWall) : null,
            SimulationTime = new(_simulationTime), Quality = Quality,
            Evidence = new(now, _clockEpoch, _displayEpoch, latest?.Response.Read.Epoch ?? default,
                Endpoint(latest), Endpoint(before), Endpoint(after))
        };
        return selected;
    }

    private static PresentationEndpoint? Endpoint(Entry? entry) => entry is { } value &&
        value.Response.Read.Capture is { } capture ? new(value.Response.Read.Tick, capture, value.Mapped) : null;

    private bool TrySelect(MonotonicNanoseconds now, MasterTimeNanoseconds selectedMaster, WorkshopClockMapping clock,
        out WorkshopPresentationSample sample, out Entry? selectedBefore, out Entry? selectedAfter)
    {
        sample = default; selectedBefore = selectedAfter = null;
        if (_retired) { Quality = PresentationQuality.Retired; return false; }
        clock.Observe(now);
        if (_phase == WorkshopSimulationPhase.Faulted)
        { Quality = PresentationQuality.Faulted; _terminal = -1; return false; }
        if (_terminal >= 0)
        {
            var entry = _entries[_terminal];
            var endpoint = entry.Response.Read;
            var wall = entry.Mapped is { } mapped ? new MonotonicNanoseconds(Math.Max(_displayWall, mapped.BrowserInterval.Midpoint)) : (MonotonicNanoseconds?)null;
            _terminal = -1;
            selectedBefore = selectedAfter = entry;
            _displayTimingValid = wall is not null;
            _displayEpoch = _displayTimingValid ? _clockEpoch : 0;
            _simulationTime = endpoint.Tick.Value / SimulationFrequency;
            Quality = PresentationQuality.Terminal;
            if (wall is { } mappedWall) _displayWall = mappedWall.Value;
            sample = new(Pose(endpoint), wall, new(_simulationTime), Quality, Captures: endpoint.Captures, FeedbackCapture: endpoint.Capture);
            return true;
        }
        if (!clock.IsQualified || clock.DisplayEpoch != _clockEpoch)
        { Quality = PresentationQuality.ClockInvalid; return false; }
        if (_phase != WorkshopSimulationPhase.Running) return false;
        selectedMaster.Validate();
        var requestedMaster = selectedMaster.Value - DelayNanoseconds;
        if (requestedMaster < Schedule.World.AnchorMaster.Value)
        { Quality = PresentationQuality.AwaitingHistory; return false; }
        var requestedTick = Schedule.World.AnchorTick.Value +
            (double)(requestedMaster - Schedule.World.AnchorMaster.Value) * SimulationFrequency / WorkshopPulse.NanosecondsPerSecond;
        Entry? before = null, after = null;
        for (var i = 0; i < _count; i++)
        {
            var item = _entries[i];
            if (item.Mapped is null) continue;
            var tick = item.Response.Read.Tick.Value;
            if (tick <= requestedTick && (before is null || tick > before.Value.Response.Read.Tick.Value)) before = item;
            if (tick >= requestedTick && (after is null || tick < after.Value.Response.Read.Tick.Value)) after = item;
        }
        if (before is not { } first || after is not { } last)
        {
            Quality = LatestAge(now) > StaleNanoseconds ? PresentationQuality.Stale : PresentationQuality.AwaitingHistory;
            return false;
        }
        var a = first.Mapped!.Value.BrowserInterval.Midpoint;
        var b = last.Mapped!.Value.BrowserInterval.Midpoint;
        if (b < a || last.Response.Read.Tick.Value < first.Response.Read.Tick.Value ||
            (a == b && last.Response.Read.Tick != first.Response.Read.Tick))
        { Quality = PresentationQuality.AwaitingHistory; return false; }
        var fraction = first.Response.Read.Tick == last.Response.Read.Tick ? 0 :
            (requestedTick - first.Response.Read.Tick.Value) / (last.Response.Read.Tick.Value - first.Response.Read.Tick.Value);
        var timeSeconds = requestedTick / SimulationFrequency;
        var requested = checked(a + (long)((b - a) * fraction));
        if (!double.IsFinite(timeSeconds) || timeSeconds < _simulationTime || requested < _displayWall)
        { Quality = PresentationQuality.AwaitingHistory; return false; }
        if (!TrySampleWorld(first.Response.Read, last.Response.Read, requestedTick, out var body))
        { Quality = PresentationQuality.AwaitingHistory; return false; }
        selectedBefore = first; selectedAfter = last;
        _displayTimingValid = true; _displayEpoch = _clockEpoch;
        _displayWall = requested; _simulationTime = timeSeconds;
        Quality = LatestAge(now) > StaleNanoseconds ? PresentationQuality.Stale : PresentationQuality.Interpolated;
        sample = new(body, new(requested), new(timeSeconds), Quality, Captures: first.Response.Read.Captures,
            FeedbackCapture: first.Response.Read.Capture);
        return true;
    }

    public long LatestAge(MonotonicNanoseconds now)
    {
        if (_latest < 0 || _entries[_latest].Mapped is not { } mapped) return long.MaxValue;
        var age = (Int128)now.Value - mapped.BrowserInterval.Lower;
        return age > long.MaxValue ? long.MaxValue : (long)Int128.Max(0, age);
    }

    private static PresentedBody? Pose(WorkshopRead read) =>
        read.Ball is { } value ? new(value.Id, value.Cell, value.Local,
            read.Rotation ?? throw new ArgumentException("Missing physical orientation.")) : null;

    public static bool TrySampleWorld(WorkshopRead from, WorkshopRead to, double requestedTick, out PresentedBody? pose)
    {
        if (!double.IsFinite(requestedTick) || requestedTick < from.Tick.Value || requestedTick > to.Tick.Value ||
            from.Epoch != to.Epoch || (from.Ball is null) != (to.Ball is null))
            throw new ArgumentException("Invalid committed motion selection.");
        if (requestedTick == from.Tick.Value) { pose = Pose(from); return true; }
        if (requestedTick == to.Tick.Value) { pose = Pose(to); return true; }
        if (to.Tick.Value != from.Tick.Value + 1) { pose = null; return false; }
        if (from.Ball is null) { pose = null; return true; }
        if (from.Ball.Value.Id != to.Ball!.Value.Id) throw new ArgumentException("Motion body changed.");
        if (to.Motion is { } motion && motion.TrySample(requestedTick * motion.Substeps, out var sampled))
        { pose = sampled; return true; }
        pose = null; return false; // Missing/coalesced commits never authorize an invented path.
    }
    private static bool SameBody(WorkshopRead first, WorkshopRead second) => WorkshopRead.SamePhysicalContent(first, second);

    public void Retire()
    {
        Array.Clear(_entries); _count = _next = 0; _latest = _terminal = -1; _dirty = false; _retired = true;
        Quality = PresentationQuality.Retired;
    }
}
