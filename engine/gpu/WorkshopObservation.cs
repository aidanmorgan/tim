using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

public enum WorkshopObservationRefusal { None, Retired, ForeignSession, UnknownQuery, NotCommitted, ClosedCursor, InvalidCursor }
public enum WorkshopObservationQuery { Committed }
public readonly record struct WorkshopObservationCursor(RuntimeSessionId Session, ulong Value);
public readonly record struct WorkshopCommandReceipt(WorkshopObservationOrdinal Ordinal, RuntimeSessionId Session,
    CommandSequence Sequence, WorkshopCommandKind Kind, WorkshopCommandResult Result, bool AppliedToPresentation);
public readonly record struct WorkshopObservationResult<T>(WorkshopObservationRefusal Refusal, T? Value);
public readonly record struct WorkshopObservationTerminal(string Detail);
public readonly record struct WorkshopAuthoredScene(CompiledSceneId Scene, WorkshopCommand Command, WorkshopConstruction Construction);

/// <summary>A session's read-only port. It has no command or presentation-consumption capability.</summary>
public interface IWorkshopObservation
{
    RuntimeSessionId Session { get; }
    WorkshopObservationTerminal? Terminal { get; }
    WorkshopObservationResult<WorkshopDiagnosticReceipt[]> Diagnostics(RuntimeSessionId session, WorkshopObservationOrdinal after);
    WorkshopObservationResult<WorkshopAuthoredScene> Authored(RuntimeSessionId session, CompiledSceneId scene);
    WorkshopObservationResult<WorkshopResponse> Query(RuntimeSessionId session, WorkshopObservationQuery query);
    WorkshopObservationResult<WorkshopCommandReceipt[]> Receipts(RuntimeSessionId session, WorkshopObservationOrdinal after);
    WorkshopObservationResult<WorkshopObservationCursor> OpenEvents(RuntimeSessionId session);
    WorkshopObservationResult<EventRingDelivery> ReadEvents(WorkshopObservationCursor cursor);
    WorkshopObservationRefusal CloseEvents(WorkshopObservationCursor cursor);
}

/// <summary>Owned by the managed client; publication happens only after its normal admission succeeds.</summary>
internal sealed class WorkshopObservation : IWorkshopObservation
{
    private readonly WorkshopDiagnostics _diagnostics;
    private readonly List<WorkshopCommandReceipt> _receipts = new(32);
    private readonly HashSet<CommandSequence> _received = new(32);
    private readonly Dictionary<ulong, int> _events = new(4);
    private Func<int>? _openEvents;
    private Func<int, SimulationEpoch, byte[]>? _readEvents;
    private Action<int>? _closeEvents;
    private ulong _nextCursor;
    private WorkshopResponse? _committed;
    private readonly Dictionary<CompiledSceneId, WorkshopAuthoredScene> _authored = new();
    private readonly HashSet<CompiledSceneId> _pendingScenes = new();
    private readonly HashSet<CompiledSceneId> _captureScenes = new();
    private readonly Dictionary<CommandSequence, CompiledSceneId> _pendingRuns = new();
    private CompiledSceneId? _activeScene;
    private SimulationEpoch _epoch;
    public RuntimeSessionId Session { get; }
    private WorkshopObservationTerminal? _terminal;
    public WorkshopObservationTerminal? Terminal => _terminal;

    public WorkshopObservation(RuntimeSessionId session, WorkshopDiagnostics diagnostics)
    {
        session.Validate(); Session = session; _diagnostics = diagnostics;
        diagnostics.BecameIncomplete += Retire;
        if (diagnostics.Incomplete is { } detail) Retire(detail);
    }

    internal static WorkshopObservation Begin(WorkshopDiagnostics diagnostics)
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new(RuntimeSessionId.Read(bytes), diagnostics);
    }

    internal void BindEvents(Func<int> open, Func<int, SimulationEpoch, byte[]> read, Action<int> close)
    {
        if (_openEvents is not null || Terminal is not null) throw new InvalidOperationException("Observation session cannot be rebound.");
        _openEvents = open; _readEvents = read; _closeEvents = close;
    }

    internal void Associate(WorkshopCommand command, WorkshopConstruction construction)
    {
        if (Terminal is not null) return;
        try
        {
            if (command.Session != Session || command.Scene is not { } scene || scene.Revision != construction.Revision ||
                scene.Settings != construction.Settings || !_authored.TryAdd(scene.Identity, new(scene.Identity, command with { Scene = null }, construction)))
                Retire("Missing, foreign or duplicate authored scene association.");
            else _pendingScenes.Add(scene.Identity);
        }
        catch (OutOfMemoryException error) { Retire("Authored scene association unavailable: " + error.Message); }
    }

    internal void BeginRun(WorkshopCommand command)
    {
        if (Terminal is not null) return;
        try
        {
            if (command.Kind != WorkshopCommandKind.Run || command.Session != Session ||
                _activeScene is not { } scene || !_authored.ContainsKey(scene) || !_pendingRuns.TryAdd(command.Sequence, scene))
                Retire("Run has no unique original authored scene association.");
        }
        catch (OutOfMemoryException error) { Retire("Run scene association unavailable: " + error.Message); }
    }

    internal void CompleteCandidate(WorkshopCommand command)
    {
        if (command.Scene is { } scene) _pendingScenes.Remove(scene.Identity);
        _pendingRuns.Remove(command.Sequence);
        ReleaseUnusedScenes();
    }

    private void ReleaseUnusedScenes()
    {
        foreach (var entry in _authored)
            if (entry.Key != _activeScene && !_pendingScenes.Contains(entry.Key) && !_captureScenes.Contains(entry.Key) &&
                !_pendingRuns.ContainsValue(entry.Key))
                _authored.Remove(entry.Key);
    }

    public WorkshopObservationResult<WorkshopAuthoredScene> Authored(RuntimeSessionId session, CompiledSceneId scene)
    {
        var refusal = Validate(session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, default);
        if (_authored.TryGetValue(scene, out var authored)) return new(WorkshopObservationRefusal.None, authored);
        Retire("Capture has no original authored scene association.");
        return new(WorkshopObservationRefusal.NotCommitted, default);
    }

    internal void Receipt(WorkshopCommand command, WorkshopResponse response, bool applicable)
    {
        if (Terminal is not null) return;
        try
        {
            if (!_received.Add(command.Sequence)) return;
            if (applicable && response.Result.Outcome == WorkshopCommandOutcome.Applied && command.Scene is { } installed)
                _activeScene = installed.Identity;
            // Only a successful Run can start a production capture. Keep its source metadata for late delivery.
            if (command.Kind == WorkshopCommandKind.Run && response.Result.Outcome == WorkshopCommandOutcome.Applied)
            {
                if (!_pendingRuns.TryGetValue(command.Sequence, out var captured))
                { Retire("Run acknowledgement has no original authored scene association."); return; }
                _captureScenes.Add(captured);
            }
            _receipts.Add(new(new((ulong)_receipts.Count + 1), Session, command.Sequence, command.Kind, response.Result, applicable));
        }
        catch (OutOfMemoryException error)
        {
            // Observation allocation must not turn an admitted terminal ACK into a gameplay failure.
            Retire("Observation receipt unavailable: " + error.Message);
        }
    }

    internal void Commit(WorkshopResponse response)
    {
        if (Terminal is not null) return;
        // Retain the already admitted managed value. Detachment belongs to an explicit query,
        // never the simulation publication hot path.
        _committed = response;
        if (response.Scene is { } scene) _activeScene = scene.Identity;
        ReleaseUnusedScenes();
        _epoch = response.Read.Epoch;
    }

    public WorkshopObservationResult<WorkshopDiagnosticReceipt[]> Diagnostics(RuntimeSessionId session, WorkshopObservationOrdinal after)
    {
        var refusal = Validate(session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, null);
        if (after.Value > (ulong)_diagnostics.Count) return new(WorkshopObservationRefusal.InvalidCursor, null);
        var records = _diagnostics.Since((int)after.Value);
        foreach (var record in records)
            if (record is WorkshopCaptureDiagnostic capture && (capture.Session != Session || capture.Scene is { } scene && !_authored.ContainsKey(scene)))
            {
                Retire("Capture has no original authored scene association.");
                return new(WorkshopObservationRefusal.NotCommitted, null);
            }
        var receipts = new WorkshopDiagnosticReceipt[records.Length];
        for (var i = 0; i < records.Length; i++) receipts[i] = new(new(after.Value + (ulong)i + 1), records[i]);
        return new(WorkshopObservationRefusal.None, receipts);
    }

    public WorkshopObservationResult<WorkshopResponse> Query(RuntimeSessionId session, WorkshopObservationQuery query)
    {
        var refusal = Validate(session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, default);
        if (!Enum.IsDefined(query)) return new(WorkshopObservationRefusal.UnknownQuery, default);
        return _committed is null ? new(WorkshopObservationRefusal.NotCommitted, default)
            : new(WorkshopObservationRefusal.None, WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(_committed.Value)));
    }

    public WorkshopObservationResult<WorkshopCommandReceipt[]> Receipts(RuntimeSessionId session, WorkshopObservationOrdinal after)
    {
        var refusal = Validate(session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, null);
        if (after.Value > (ulong)_receipts.Count) return new(WorkshopObservationRefusal.InvalidCursor, null);
        return new(WorkshopObservationRefusal.None, _receipts.GetRange((int)after.Value, _receipts.Count - (int)after.Value).ToArray());
    }

    public WorkshopObservationResult<WorkshopObservationCursor> OpenEvents(RuntimeSessionId session)
    {
        var refusal = Validate(session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, default);
        if (_nextCursor == ulong.MaxValue) return new(WorkshopObservationRefusal.InvalidCursor, default);
        if (_openEvents is null) return new(WorkshopObservationRefusal.NotCommitted, default);
        var transport = _openEvents();
        var cursor = ++_nextCursor;
        _events.Add(cursor, transport);
        return new(WorkshopObservationRefusal.None, new(Session, cursor));
    }

    public WorkshopObservationResult<EventRingDelivery> ReadEvents(WorkshopObservationCursor cursor)
    {
        var refusal = Validate(cursor.Session);
        if (refusal != WorkshopObservationRefusal.None) return new(refusal, null);
        return _events.TryGetValue(cursor.Value, out var transport)
            ? new(WorkshopObservationRefusal.None, EventRingDelivery.Read(_readEvents!(transport, _epoch)))
            : new(WorkshopObservationRefusal.ClosedCursor, null);
    }

    public WorkshopObservationRefusal CloseEvents(WorkshopObservationCursor cursor)
    {
        var refusal = Validate(cursor.Session);
        if (refusal != WorkshopObservationRefusal.None) return refusal;
        if (!_events.Remove(cursor.Value, out var transport)) return WorkshopObservationRefusal.ClosedCursor;
        _closeEvents!(transport);
        return WorkshopObservationRefusal.None;
    }

    internal void Retire(string detail)
    {
        if (_terminal is not null) return;
        _terminal = new(detail);
        _diagnostics.BecameIncomplete -= Retire;
        foreach (var transport in _events.Values)
        {
            try { _closeEvents!(transport); }
            catch (Exception error) when (error is InvalidOperationException or System.Runtime.InteropServices.JavaScript.JSException) { _terminal = new(detail + " Cursor release: " + error.Message); }
        }
        _events.Clear();
        _authored.Clear();
        _pendingScenes.Clear();
        _captureScenes.Clear();
        _pendingRuns.Clear();
        _activeScene = null;
        _committed = null;
    }

    private WorkshopObservationRefusal Validate(RuntimeSessionId session) => session != Session
        ? WorkshopObservationRefusal.ForeignSession : Terminal is not null
            ? WorkshopObservationRefusal.Retired : WorkshopObservationRefusal.None;
}
