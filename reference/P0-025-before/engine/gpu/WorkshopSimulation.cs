using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public enum WorkshopSimulationPhase { Uninitialized, Initializing, Building, Admitting, Starting, Running, Completed, Resetting, Faulted, Disposed }
public enum WorkshopCommandOutcome { Applied, Rejected, Superseded, Faulted, Cancelled }
public enum WorkshopRejection { None, Busy, InvalidConstruction, WrongRevision, WrongPhase, GpuAdmission, DeviceLost, InvalidRead, IdentityExhausted, StaleGeneration, Cancelled, AlreadyCommitted, Capacity, ReliableStalled, Transport }
public readonly record struct WorkshopCommandResult(WorkshopCommandOutcome Outcome, WorkshopRejection Reason);
public readonly record struct WorkshopRead(SimulationEpoch Epoch, SimulationTick Tick, CanonicalBody? Ball, AuthorityRevision Revision = default);
public readonly record struct WorkshopGpuCandidate(CommandSequence Sequence, WorkshopRead Read);

/// <summary>Implemented only by the simulation worker's WebGPU adapter. No numerical host alternative.</summary>
public interface IWorkshopGpuDevice : IAsyncDisposable
{
    ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, CommandSequence sequence);
    ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence);
    // Swap candidate buffers only after host validation. Discard is idempotent, nonthrowing, and
    // sequence-specific; a superseded request cannot free a newer candidate or committed buffers.
    void Commit(CommandSequence sequence);
    void Discard(CommandSequence sequence);
}
public sealed class GpuAdmissionException : Exception
{
    public GpuAdmissionException(string message) : base(message) { }
}

/// <summary>
/// Exclusive discrete owner in the simulation worker. An awaited GPU candidate cannot mutate a
/// newer operation. At most one active GPU request plus one superseding Reset can be retained.
/// Callers publish the value-only Committed read after an Applied result; no observer callback is
/// part of the authority transaction.
/// </summary>
public sealed class WorkshopSimulation : IAsyncDisposable
{
    private readonly IWorkshopGpuDevice _device;
    private ulong _operation;
    private ulong _cancelledOperation;
    private WorkshopSimulationPhase _priorPhase;
    public bool HasPendingGpuOperation => _pending != 0;
    public AuthorityRevision Revision => Committed.Revision;
    private ulong _epoch = 1;
    private int _pending;
    private bool _disposed;
    public SimulationEpoch Epoch => new(_epoch);
    public WorkshopConstruction Construction { get; private set; } = new(new(1), null);
    public WorkshopRead Committed { get; private set; }
    public WorkshopSimulationPhase Phase { get; private set; } = WorkshopSimulationPhase.Uninitialized;
    public WorkshopRejection Fault { get; private set; }
    public WorkshopSimulation(IWorkshopGpuDevice device) => _device = device ?? throw new ArgumentNullException(nameof(device));

    public async ValueTask<WorkshopCommandResult> Initialize()
    {
        if (Phase != WorkshopSimulationPhase.Uninitialized) return Reject(WorkshopRejection.WrongPhase);
        if (!Begin(WorkshopSimulationPhase.Initializing, false, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            // Device capability/schema/pipeline and the empty generation must all be admitted.
            var candidate = await _device.Admit(Construction, epoch, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, Construction, epoch);
            _device.Commit(new(owner));
            Committed = read;
            Phase = WorkshopSimulationPhase.Building;
            return Applied();
        }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public async ValueTask<WorkshopCommandResult> Construct(WorkshopConstruction candidate)
    {
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        if (Phase != WorkshopSimulationPhase.Building) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        try { candidate.Validate(); }
        catch (ArgumentException) { return Reject(WorkshopRejection.InvalidConstruction); }
        if (Construction.Revision.Value == ulong.MaxValue ||
            candidate.Revision.Value != Construction.Revision.Value + 1)
            return Reject(WorkshopRejection.WrongRevision);
        if (!Begin(WorkshopSimulationPhase.Admitting, true, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var gpuCandidate = await _device.Admit(candidate, epoch, new(owner));
            var read = CandidateRead(gpuCandidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, candidate, epoch);
            _device.Commit(new(owner));
            Construction = candidate;
            Committed = read with { Revision = new(Revision.Value + 1) };
            _epoch = epoch.Value;
            Phase = WorkshopSimulationPhase.Building;
            return Applied();
        }
        catch (GpuAdmissionException)
        {
            if (!Owns(owner)) return Superseded(owner);
            Phase = WorkshopSimulationPhase.Building;
            return Reject(WorkshopRejection.GpuAdmission);
        }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public async ValueTask<WorkshopCommandResult> Run()
    {
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        if (Phase != WorkshopSimulationPhase.Building) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        if (!Begin(WorkshopSimulationPhase.Starting, false, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            // Run preserves world generation. GPU completion defines the first committed Run read.
            var candidate = await _device.Admit(Construction, epoch, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, Construction, epoch);
            _device.Commit(new(owner));
            Committed = read with { Revision = new(Revision.Value + 1) };
            Phase = WorkshopSimulationPhase.Running;
            return Applied();
        }
        catch (GpuAdmissionException)
        {
            if (!Owns(owner)) return Superseded(owner);
            Phase = WorkshopSimulationPhase.Building;
            return Reject(WorkshopRejection.GpuAdmission);
        }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public async ValueTask<WorkshopCommandResult> Advance()
    {
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        if (Phase != WorkshopSimulationPhase.Running) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        var source = Committed;
        if (!Begin(WorkshopSimulationPhase.Running, false, out var owner, out _))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var candidate = await _device.Advance(source, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateAdvancedRead(source, read);
            _device.Commit(new(owner));
            Committed = read with { Revision = new(Revision.Value + 1) };
            return Applied();
        }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        // Counts every request, independently of ownership. A stale completion cannot clear a new one.
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public async ValueTask<WorkshopCommandResult> Reset()
    {
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        if (Phase is WorkshopSimulationPhase.Uninitialized or WorkshopSimulationPhase.Initializing or WorkshopSimulationPhase.Disposed)
            return Reject(WorkshopRejection.WrongPhase);
        if (Phase == WorkshopSimulationPhase.Resetting || _pending >= 2)
            return Reject(WorkshopRejection.Busy);
        if (!Begin(WorkshopSimulationPhase.Resetting, true, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var candidate = await _device.Admit(Construction, epoch, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, Construction, epoch);
            _device.Commit(new(owner));
            Committed = read with { Revision = new(Revision.Value + 1) };
            _epoch = epoch.Value;
            Phase = WorkshopSimulationPhase.Building;
            Fault = WorkshopRejection.None;
            return Applied();
        }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    // Invalidates only an operation that has not crossed its synchronous Commit boundary.
    public WorkshopCommandResult CancelPending()
    {
        if (_pending == 0 || Phase is not (WorkshopSimulationPhase.Admitting or WorkshopSimulationPhase.Starting or WorkshopSimulationPhase.Resetting))
            return Reject(WorkshopRejection.AlreadyCommitted);
        if (_operation == ulong.MaxValue) return Reject(WorkshopRejection.IdentityExhausted);
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        _cancelledOperation = _operation++;
        Phase = _priorPhase;
        Committed = Committed with { Revision = new(Revision.Value + 1) };
        return Applied();
    }

    public void Stop(WorkshopRejection reason)
    {
        if (_disposed) return;
        if (reason is not (WorkshopRejection.Capacity or WorkshopRejection.ReliableStalled or WorkshopRejection.Transport))
            throw new ArgumentException("Unsupported scheduling fault.");
        Phase = WorkshopSimulationPhase.Faulted;
        Fault = reason;
    }

    public void CompleteRun()
    {
        if (Phase != WorkshopSimulationPhase.Running || _pending != 0 || Committed.Tick.Value != 3600)
            throw new InvalidOperationException("Only the committed Workshop timeout may complete a Run.");
        Phase = WorkshopSimulationPhase.Completed;
    }

    public void DeviceLost()
    {
        if (_disposed) return;
        Phase = WorkshopSimulationPhase.Faulted;
        Fault = WorkshopRejection.DeviceLost;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Phase = WorkshopSimulationPhase.Disposed;
        // Disposal never needs to allocate another identity, including at counter exhaustion.
        await _device.DisposeAsync();
    }

    private bool CanAdvanceRevision()
    {
        if (Revision.Value != ulong.MaxValue) return true;
        Phase = WorkshopSimulationPhase.Faulted;
        Fault = WorkshopRejection.IdentityExhausted;
        return false;
    }

    private bool Begin(WorkshopSimulationPhase phase, bool nextEpoch, out ulong owner, out SimulationEpoch epoch)
    {
        owner = _operation;
        epoch = new(_epoch);
        if (_operation == ulong.MaxValue || (nextEpoch && _epoch == ulong.MaxValue)) return false;
        owner = ++_operation;
        if (nextEpoch) epoch = new(_epoch + 1);
        _pending++;
        _priorPhase = Phase;
        Phase = phase;
        return true;
    }
    private bool Owns(ulong operation) => !_disposed && _operation == operation &&
        Phase != WorkshopSimulationPhase.Faulted;
    private WorkshopCommandResult Failure(ulong owner, WorkshopRejection reason)
    {
        if (!Owns(owner)) return Superseded(owner);
        Phase = WorkshopSimulationPhase.Faulted;
        Fault = reason;
        return new(WorkshopCommandOutcome.Faulted, reason);
    }
    private static WorkshopCommandResult Applied() => new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
    private WorkshopCommandResult Superseded(ulong owner) => owner == _cancelledOperation
        ? new(WorkshopCommandOutcome.Cancelled, WorkshopRejection.Cancelled)
        : new(WorkshopCommandOutcome.Superseded, WorkshopRejection.None);
    private static WorkshopCommandResult Reject(WorkshopRejection reason) => new(WorkshopCommandOutcome.Rejected, reason);

    private static WorkshopRead CandidateRead(WorkshopGpuCandidate candidate, ulong owner)
    {
        if (candidate.Sequence.Value != owner || candidate.Read.Revision.Value != 0) throw new ArgumentException("GPU candidate command sequence does not match.");
        return candidate.Read;
    }

    private static void ValidateInitialRead(WorkshopRead read, WorkshopConstruction construction, SimulationEpoch epoch)
    {
        if (read.Epoch != epoch || read.Tick.Value != 0 ||
            (read.Ball is null) != (construction.Ball is null))
            throw new ArgumentException("GPU admission read does not match the construction transaction.");
        if (read.Ball is { } body && construction.Ball is { } ball)
        {
            body.Validate();
            if (body.Epoch != epoch.Value || body.Tick != 0 || body.Id != ball.Id ||
                body.Cell != ball.Cell || !HalfBits.Equal(body.Local, ball.Local) || !HalfBits.IsPositiveZero(body.Velocity))
                throw new ArgumentException("GPU admission changed canonical construction.");
        }
    }
    private static void ValidateAdvancedRead(WorkshopRead source, WorkshopRead read)
    {
        if (source.Tick.Value == ulong.MaxValue || read.Epoch != source.Epoch ||
            read.Tick.Value != source.Tick.Value + 1 || (read.Ball is null) != (source.Ball is null))
            throw new ArgumentException("GPU tick read does not match the active transaction.");
        if (read.Ball is { } body && source.Ball is { } previous)
        {
            body.Validate();
            if (body.Id != previous.Id || body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value ||
                body.Cell.X != previous.Cell.X || body.Cell.Z != previous.Cell.Z ||
                !HalfBits.Equal(body.Local.X, previous.Local.X) || !HalfBits.Equal(body.Local.Z, previous.Local.Z))
                throw new ArgumentException("GPU body identity does not match its committed read.");
        }
    }
}
