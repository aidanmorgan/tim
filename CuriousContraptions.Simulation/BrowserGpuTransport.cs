using System;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Simulation;

internal sealed class BrowserGpuTransport(IEventRingPublication events) : IWorkshopGpuTransport
{
    // Backing reserve is distinct from the exact logical world declared by its header.
    internal const int DefaultWorldCapacity = 16 * 1024;
    private byte[] _currentBuffer = new byte[DefaultWorldCapacity];
    private byte[] _candidateBuffer = new byte[DefaultWorldCapacity];
    private byte[]? _preparedSpare;
    private int _currentLength, _candidateLength;
    private PhysicsInteractionSnapshot _currentInteractions=PhysicsInteractionSnapshot.Empty;
    private PhysicsInteractionSnapshot _candidateInteractions=PhysicsInteractionSnapshot.Empty;
    private WorkshopPhysicsEngine? _engine = new();
    private bool _readyCandidate;
    private bool _candidateAdmission;
    private bool _eventsPrepared;
    private bool _initialized;

    public Task Initialize()
    {
        (_engine ??= new()).Reset();
        _readyCandidate = false;
        _currentInteractions=PhysicsInteractionSnapshot.Empty;
        _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _initialized = true;
        return Task.CompletedTask;
    }

    public Task Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, PhysicsTickInputs inputs)
    {
        if (!_initialized) throw new InvalidOperationException("GPU transport is not initialized.");
        if (_readyCandidate) throw new InvalidOperationException("Previous candidate has not been read or committed.");

        _candidateAdmission = operation == WorkshopGpuOperation.Admit;
        if (operation == WorkshopGpuOperation.Admit)
        {
            if (input.Length != PhysicsGpuAbi.ByteLength(input.Span))
                throw new ArgumentException("Invalid GPU candidate input length.");
            (_engine ??= new()).Reset();
            var candidate = Candidate(input.Length, true);
            _preparedSpare = new byte[_candidateBuffer.Length];
            input.CopyTo(candidate);
            _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        }
        else if (operation == WorkshopGpuOperation.Advance)
        {
            _candidateInteractions=_engine!.Advance(_currentBuffer.AsMemory(0, _currentLength), Candidate(_currentLength, false), inputs, _currentInteractions);
        }
        else
        {
            throw new ArgumentException("Invalid GPU candidate operation.");
        }

        _readyCandidate = true;
        return Task.CompletedTask;
    }

    internal static int WorldCapacity(int current, int required)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(current);
        ArgumentOutOfRangeException.ThrowIfNegative(required);
        if (required > Array.MaxLength) throw new WorkshopStorageException("Physical world storage is not representable.");
        if (required <= 3L * current / 4) return current;
        var preferred = Math.Max(2L * current, (4L * required + 2) / 3);
        return (int)Math.Min(preferred, Array.MaxLength);
    }

    private Memory<byte> Candidate(int length, bool admission)
    {
        var baseline = admission ? DefaultWorldCapacity : _candidateBuffer.Length;
        var capacity = WorldCapacity(baseline, length);
        if (_candidateBuffer.Length != capacity) _candidateBuffer = new byte[capacity];
        _candidateLength = length;
        return _candidateBuffer.AsMemory(0, length);
    }

    public WorkshopTransportCandidate Read()
    {
        if (!_readyCandidate) throw new InvalidOperationException("Candidate readback has not completed.");
        return new(_candidateBuffer.AsMemory(0, _candidateLength),_candidateInteractions);
    }

    public void PrepareEvents(byte[] batch)
    {
        if (!_readyCandidate) throw new InvalidOperationException("No physical candidate owns this event preparation.");
        var result = events.Prepare(batch);
        if (result is EventPublicationPreparation.StorageUnavailable or EventPublicationPreparation.SequenceExhausted)
            throw new EventPreparationException(result);
        if (result != EventPublicationPreparation.Ready) throw new ArgumentException("Undefined event preparation result.");
        _eventsPrepared = true;
    }

    public void PublishEvents() => events.Publish();

    public void Commit()
    {
        if (!_readyCandidate || !_eventsPrepared) throw new InvalidOperationException("Physical and event candidates must both be prepared.");
        events.Commit();
        _eventsPrepared = false;
        var retired = _currentBuffer;
        _currentBuffer = _candidateBuffer;
        _candidateBuffer = _candidateAdmission ? _preparedSpare! : retired;
        _currentLength = _candidateLength;
        _preparedSpare = null;
        _candidateAdmission = false;
        _currentInteractions=_candidateInteractions;
        _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _readyCandidate = false;
    }

    public void Discard()
    {
        events.Discard();
        if (_candidateAdmission) _candidateBuffer = _preparedSpare ?? _candidateBuffer;
        _preparedSpare = null;
        _candidateLength = 0;
        _candidateAdmission = false;
        _eventsPrepared = false;
        _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _readyCandidate = false;
    }

    public bool DeviceReady() => _initialized;

    public void Dispose()
    {
        events.Dispose();
        _eventsPrepared = false;
        _initialized = false;
        _readyCandidate = false;
        _currentBuffer=[]; _candidateBuffer=[]; _preparedSpare=null;
        _currentLength=0; _candidateLength=0;
        _currentInteractions=PhysicsInteractionSnapshot.Empty; _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _engine = null;
    }
}
