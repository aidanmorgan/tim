using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public enum WorkshopSimulationPhase { Uninitialized, Initializing, Building, Admitting, Starting, Running, Paused, Completed, Resetting, Faulted, Disposed }
public enum WorkshopCommandOutcome { Applied, Rejected, Superseded, Faulted, Cancelled }
public enum WorkshopRejection { None, Busy, InvalidConstruction, WrongRevision, WrongPhase, GpuAdmission, DeviceLost, InvalidRead, IdentityExhausted, StaleGeneration, Cancelled, AlreadyCommitted, Capacity, ReliableStalled, Transport }
public readonly record struct WorkshopCommandResult(WorkshopCommandOutcome Outcome, WorkshopRejection Reason);
public readonly partial record struct WorkshopRead(SimulationEpoch Epoch, SimulationTick Tick, PhysicsBodyReadSet Bodies, AuthorityRevision Revision = default, WorkshopClockStamp? Capture = null,
    PhysicsCaptureRead Captures = default, PhysicsMotionRead? Motion = null, PhysicsActivationRead Activations = default, PhysicsTimerRead Timers = default, PhysicsContactWorkRead ContactWorks = default, PhysicsElectricalRead Electrical = default);
public readonly record struct WorkshopGpuCandidate(CommandSequence Sequence, WorkshopRead Read);

/// <summary>Implemented only by the simulation worker's WebGPU adapter. No numerical host alternative.</summary>
public interface IWorkshopGpuDevice : IAsyncDisposable
{
    ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence);
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
public sealed class WorkshopInstallationException : Exception { }

public interface IWorkshopInstallation
{
    void Abort(CommandSequence owner);
    void Retire();
    ValueTask Prepare(CommandSequence owner, WorkshopRead read, WorkshopConstruction construction, WorkshopGpuProfile profile, WorkshopSimulationPhase phase);
}

public sealed class WorkshopSimulation : IAsyncDisposable
{
    private readonly Dictionary<GpuBodyId, ElectricalEnable> _electricalControls = new();
    public ElectricalEnable ElectricalState(GpuBodyId owner, ElectricalEnable previous) =>
        _electricalControls.TryGetValue(owner, out var state) ? state : previous;
    public WorkshopCommandResult ConfigureElectrical(WorkshopElectricalControl control)
    {
        control.Validate();
        if (_pending != 0) return new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy);
        if (Phase is not (WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused))
            return new(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase);
        foreach (var instance in Construction.Instances)
            if (instance is WorkshopBattery battery && battery.Id == control.Owner && !battery.Locked)
            {
                _electricalControls[control.Owner] = control.Enabled;
                return new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
            }
        return new(WorkshopCommandOutcome.Rejected, WorkshopRejection.InvalidConstruction);
    }
    private readonly IWorkshopGpuDevice _device;
    private readonly IWorkshopCaptureClock _clock;
    private readonly IWorkshopInstallation _installation;
    public WorkshopGpuProfile Profile { get; private set; }
    private ulong _operation;
    private ulong _cancelledOperation;
    private WorkshopSimulationPhase _priorPhase;
    public bool HasPendingGpuOperation => _pending != 0;
    public AuthorityRevision Revision => Committed.Revision;
    private ulong _epoch = 1;
    private int _pending;
    private bool _disposed;
    public SimulationEpoch Epoch => new(_epoch);
    public WorkshopConstruction Construction { get; private set; }
    public WorkshopRead Committed { get; private set; }
    public WorkshopSimulationPhase Phase { get; private set; } = WorkshopSimulationPhase.Uninitialized;
    public WorkshopRejection Fault { get; private set; }
    public WorkshopSimulation(IWorkshopGpuDevice device, IWorkshopCaptureClock clock, WorkshopCadenceSettings settings, CadenceRevision revision, IWorkshopInstallation installation)
    {
        settings.Validate(); Profile = new(settings.Simulation, settings.Physical, revision); Profile.Validate();
        Construction = new(new(1), settings, WorkshopInstances.Empty);
        _installation = installation ?? throw new ArgumentNullException(nameof(installation));
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (clock.Generation.Value == 0) throw new ArgumentException("A qualified native clock generation is required.");
    }

    private WorkshopRead CaptureForCommit(WorkshopRead candidate)
    {
        // Every fallible clock operation occurs before the physical buffer swap.
        var capture = _clock.Capture();
        capture.Validate();
        if (capture.Domain != WorkshopClockDomain.SimulationMonotonic || capture.Generation != _clock.Generation ||
            (Committed.Capture is { } prior && (prior.Generation != capture.Generation || capture.Time.Value < prior.Time.Value)))
            throw new ArgumentException("Physical capture clock reversed or changed origin.");
        return candidate with { Capture = capture };
    }

    public async ValueTask<WorkshopCommandResult> Initialize()
    {
        if (Phase != WorkshopSimulationPhase.Uninitialized) return Reject(WorkshopRejection.WrongPhase);
        if (!Begin(WorkshopSimulationPhase.Initializing, false, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            // Device capability/schema and the empty generation must all be admitted.
            var candidate = await _device.Admit(Construction, epoch, Profile, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, Construction, epoch);
            read = CaptureForCommit(read);
            await _installation.Prepare(new(owner), read, Construction, Profile, WorkshopSimulationPhase.Building);
            if (!Owns(owner)) return Superseded(owner);
            _device.Commit(new(owner));
            Committed = read;
            Phase = WorkshopSimulationPhase.Building;
            return Applied();
        }
        catch (WorkshopInstallationException) { if (Owns(owner)) Phase = _priorPhase; return Reject(WorkshopRejection.Transport); }
        catch (Exception error)
        {
            Console.Error.WriteLine("CCGPU_INITIALIZE_EXCEPTION " + error);
            return Failure(owner, WorkshopRejection.InvalidRead);
        }
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
            var gpuCandidate = await _device.Admit(candidate, epoch, Profile, new(owner));
            var read = CandidateRead(gpuCandidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, candidate, epoch);
            read = CaptureForCommit(read with { Revision = new(Revision.Value + 1) });
            await _installation.Prepare(new(owner), read, candidate, Profile, WorkshopSimulationPhase.Building);
            if (!Owns(owner)) return Superseded(owner);
            _device.Commit(new(owner));
            _electricalControls.Clear();
            Construction = candidate;
            _electricalControls.Clear();
            Committed = read;
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
        catch (WorkshopInstallationException) { if (Owns(owner)) Phase = _priorPhase; return Reject(WorkshopRejection.Transport); }
        catch (Exception) { return Failure(owner, WorkshopRejection.InvalidRead); }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public WorkshopCommandResult Save()
    {
        if (Phase != WorkshopSimulationPhase.Building) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        if (!CanAdvanceRevision()) return Reject(WorkshopRejection.IdentityExhausted);
        Committed = Committed with { Revision = new(Revision.Value + 1) };
        return Applied();
    }

    public ValueTask<WorkshopCommandResult> Run() => ChangePlayback(WorkshopSimulationPhase.Building, WorkshopSimulationPhase.Running);
    public ValueTask<WorkshopCommandResult> Pause() => ChangePlayback(WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused);
    public ValueTask<WorkshopCommandResult> Resume() => ChangePlayback(WorkshopSimulationPhase.Paused, WorkshopSimulationPhase.Running);
    private async ValueTask<WorkshopCommandResult> ChangePlayback(WorkshopSimulationPhase expected, WorkshopSimulationPhase target)
    {
        if (Phase != expected) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        if (!CanAdvanceRevision() || !Begin(expected, false, out var owner, out _)) return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var read = Committed with { Revision = new(Revision.Value + 1) };
            await _installation.Prepare(new(owner), read, Construction, Profile, target);
            if (!Owns(owner)) return Superseded(owner);
            if (expected == WorkshopSimulationPhase.Building) _electricalControls.Clear();
            Committed = read; Phase = target;
            return Applied();
        }
        catch (WorkshopInstallationException) { if (Owns(owner)) Phase = _priorPhase; return Reject(WorkshopRejection.Transport); }
        finally { _pending--; }
    }

    public async ValueTask<WorkshopCommandResult> Configure(WorkshopCadenceSettings settings, CadenceRevision revision)
    {
        if (Phase != WorkshopSimulationPhase.Building) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        settings.Validate(); revision.Validate();
        if (revision.Value <= Profile.Revision.Value) return Reject(WorkshopRejection.StaleGeneration);
        var profile = new WorkshopGpuProfile(settings.Simulation, settings.Physical, revision);
        var construction = Construction with { Settings = settings };
        if (!Begin(WorkshopSimulationPhase.Admitting, false, out var owner, out var epoch))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var candidate = await _device.Admit(construction, epoch, profile, new(owner));
            if (!Owns(owner)) return Superseded(owner);
            var read = CandidateRead(candidate, owner);
            ValidateInitialRead(read, construction, epoch);
            read = CaptureForCommit(read with { Revision = new(Revision.Value + 1) });
            await _installation.Prepare(new(owner), read, construction, profile, WorkshopSimulationPhase.Building);
            if (!Owns(owner)) return Superseded(owner);
            _device.Commit(new(owner));
            _electricalControls.Clear();
            Construction = construction; Profile = profile; Committed = read; Phase = WorkshopSimulationPhase.Building;
            return Applied();
        }
        catch { if (Owns(owner)) Phase = _priorPhase; throw; }
        finally { _device.Discard(new(owner)); _pending--; }
    }

    public ValueTask<WorkshopCommandResult> Advance() => AdvanceCore(false);
    public ValueTask<WorkshopCommandResult> Step() => AdvanceCore(true);
    private async ValueTask<WorkshopCommandResult> AdvanceCore(bool step)
    {
        if (!CanAdvanceRevision()) return new(WorkshopCommandOutcome.Faulted, WorkshopRejection.IdentityExhausted);
        if (Phase != (step ? WorkshopSimulationPhase.Paused : WorkshopSimulationPhase.Running)) return Reject(WorkshopRejection.WrongPhase);
        if (Committed.Tick.Value >= Profile.RunTickLimit) return Reject(WorkshopRejection.WrongPhase);
        if (_pending != 0) return Reject(WorkshopRejection.Busy);
        var source = Committed;
        if (!Begin(step ? WorkshopSimulationPhase.Paused : WorkshopSimulationPhase.Running, false, out var owner, out _))
            return Reject(WorkshopRejection.IdentityExhausted);
        try
        {
            var candidate = await _device.Advance(source, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateAdvancedRead(source, read);
            read = CaptureForCommit(read with { Revision = new(Revision.Value + 1) });
            if (step) await _installation.Prepare(new(owner), read, Construction, Profile, WorkshopSimulationPhase.Paused);
            if (!Owns(owner)) return Superseded(owner);
            _device.Commit(new(owner));
            Committed = read;
            return Applied();
        }
        catch (WorkshopInstallationException) { if (Owns(owner)) Phase = _priorPhase; return Reject(WorkshopRejection.Transport); }
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
            var candidate = await _device.Admit(Construction, epoch, Profile, new(owner));
            var read = CandidateRead(candidate, owner);
            if (!Owns(owner)) return Superseded(owner);
            ValidateInitialRead(read, Construction, epoch);
            read = CaptureForCommit(read with { Revision = new(Revision.Value + 1) });
            await _installation.Prepare(new(owner), read, Construction, Profile, WorkshopSimulationPhase.Building);
            if (!Owns(owner)) return Superseded(owner);
            _device.Commit(new(owner));
            Committed = read;
            _epoch = epoch.Value;
            Phase = WorkshopSimulationPhase.Building;
            Fault = WorkshopRejection.None;
            return Applied();
        }
        catch (WorkshopInstallationException) { if (Owns(owner)) Phase = _priorPhase; return Reject(WorkshopRejection.Transport); }
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
        _installation.Abort(new(_operation));
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

    public async ValueTask CompleteRun()
    {
        if (Phase != WorkshopSimulationPhase.Running || _pending != 0 || Committed.Tick.Value != Profile.RunTickLimit)
            throw new InvalidOperationException("Only the committed Workshop timeout may complete a Run.");
        if (!Begin(WorkshopSimulationPhase.Running, false, out var owner, out _)) throw new InvalidOperationException("Completion owner exhausted.");
        try
        {
            await _installation.Prepare(new(owner), Committed, Construction, Profile, WorkshopSimulationPhase.Completed);
            if (!Owns(owner)) { _installation.Abort(new(owner)); throw new WorkshopInstallationException(); }
            Phase = WorkshopSimulationPhase.Completed;
        }
        finally { _pending--; }
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
        _installation.Retire();
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
    private WorkshopCommandResult Superseded(ulong owner)
    {
        _installation.Abort(new(owner));
        return owner == _cancelledOperation ? new(WorkshopCommandOutcome.Cancelled, WorkshopRejection.Cancelled)
            : new(WorkshopCommandOutcome.Superseded, WorkshopRejection.None);
    }
    private static WorkshopCommandResult Reject(WorkshopRejection reason) => new(WorkshopCommandOutcome.Rejected, reason);

    private static WorkshopRead CandidateRead(WorkshopGpuCandidate candidate, ulong owner)
    {
        if (candidate.Sequence.Value != owner || candidate.Read.Revision.Value != 0) throw new ArgumentException("GPU candidate command sequence does not match.");
        return candidate.Read;
    }

    private static void ValidateInitialRead(WorkshopRead read, WorkshopConstruction construction, SimulationEpoch epoch)
    {
        var physics = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));
        if (read.ContactWorks.Count != physics.ContactWorks.Length) throw new ArgumentException("Admission work population changed.");
        for (var i = 0; i < read.ContactWorks.Count; i++)
        {
            var work = physics.ContactWorks[i];
            var expected = new ContactWorkRead(work.Id, work.Owner, 0, work.InitialEnergy);
            if (read.ContactWorks[i] != expected || !read.ContactWorks[i].RemainingEnergy.HasSameBits(expected.RemainingEnergy)) throw new ArgumentException("Admission retained spent contact work.");
        }
        if (read.Electrical.Count != physics.Electrical.Sources.Length)
            throw new ArgumentException("Admission source population changed.");
        for (var i = 0; i < read.Electrical.Count; i++)
        {
            var source = physics.Electrical.Sources[i]; var actual = read.Electrical[i];
            if (actual.Id != source.Id || actual.Owner != source.Owner || actual.Enabled != source.Enabled ||
                !actual.Remaining.HasSameBits(source.InitialEnergy) || actual.Debit.Value != 0)
                throw new ArgumentException("Admission retained source work.");
        }
        read.ContactWorks.ValidateScene(physics,read.Bodies);
        for (var i=0; i<read.ContactWorks.OccurrenceCount; i++)
            if (read.ContactWorks.Occurrence(i).Sequence != 0) throw new ArgumentException("Admission retained contact occurrence.");
        var network = WorkshopActivationCompiler.Compile(construction);
        var initial = network.Clear();
        var timers = network.ClearTimers();
        if (read.Timers.Count != timers.Count) throw new ArgumentException("Admission timer population changed.");
        for (var i = 0; i < timers.Count; i++)
            if (read.Timers[i] != timers[i]) throw new ArgumentException("Admission retained timer state.");
        if (read.Activations.Count != initial.Count) throw new ArgumentException("Admission activation population changed.");
        for (var i = 0; i < initial.Count; i++)
            if (read.Activations[i] != initial[i]) throw new ArgumentException("Admission retained activation state.");
        if (read.Epoch != epoch || read.Tick.Value != 0)
            throw new ArgumentException("GPU admission read does not match the construction transaction.");
        for (var index = 0; index < read.Captures.Count; index++)
            if (read.Captures[index].Phase != CaptureLatchPhase.Clear)
                throw new ArgumentException("Fresh admission contains a capture latch.");
        read.Bodies.ValidateScene(physics, epoch, read.Tick);
    }

    private void ValidateAdvancedRead(WorkshopRead source, WorkshopRead read)
    {
        if (source.Tick.Value == ulong.MaxValue || read.Epoch != source.Epoch ||
            read.Tick.Value != source.Tick.Value + 1 || read.Bodies.Count != source.Bodies.Count)
            throw new ArgumentException("GPU tick read does not match the active transaction.");
        if (read.Captures.Count != source.Captures.Count ||
            read.Activations.Count != source.Activations.Count || read.Timers.Count != source.Timers.Count || read.ContactWorks.Count != source.ContactWorks.Count)
            throw new ArgumentException("Physical read population changed.");
        read.ContactWorks.ValidateAdvance(source.ContactWorks);
        read.Electrical.ValidateAdvance(source.Electrical);
        if (read.Motion is { } motion)
        {
            var hasPhase = (motion.LastOrdinal + 3) / 4 > (motion.FirstOrdinal + 3) / 4;
            for (var i = 0; i < read.Electrical.Count; i++)
            {
                var expected = hasPhase ? ElectricalState(source.Electrical[i].Owner, source.Electrical[i].Enabled) : source.Electrical[i].Enabled;
                if (read.Electrical[i].Enabled != expected) throw new ArgumentException("Electrical phase did not apply the authorised enable state.");
            }
        }
        read.Bodies.ValidateTime(read.Epoch, read.Tick);
        for (var i = 0; i < read.Captures.Count; i++)
            if (read.Captures[i].Sensor != source.Captures[i].Sensor ||
                (source.Captures[i].Phase == CaptureLatchPhase.Latched && read.Captures[i] != source.Captures[i]))
                throw new ArgumentException("Committed capture identity or latch changed.");
        for (var i = 0; i < read.Activations.Count; i++)
        {
            var before = source.Activations[i]; var after = read.Activations[i]; after.Validate();
            if (after.Node != before.Node || after.Owner != before.Owner ||
                (before.Phase == ActivationPhase.Latched && after != before))
                throw new ArgumentException("Committed activation identity or latch changed.");
        }
        for (var i = 0; i < read.Timers.Count; i++)
        {
            var before = source.Timers[i]; var after = read.Timers[i]; after.Validate();
            if (before.Node != after.Node || after.Phase < before.Phase ||
                (before.Phase != ActivationTimerPhase.Ready &&
                    (after.StartedTick != before.StartedTick || after.DueTick != before.DueTick || after.Input != before.Input)))
                throw new ArgumentException("Committed timer identity or countdown changed.");
        }
        for (var i = 0; i < read.Bodies.Count; i++)
        {
            var value = read.Bodies[i]; value.Validate();
            if (value.Body.Id != source.Bodies[i].Body.Id ||
                !HalfBits.Equal(value.LocalCentreOfMass, source.Bodies[i].LocalCentreOfMass))
                throw new ArgumentException("GPU body identity or centre of mass changed.");
        }
    }
}
