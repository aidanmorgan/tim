using System;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed class BrowserGpuTransport : IWorkshopGpuTransport
{
    // Story 9R.2: every world is exactly as long as its own header says (PhysicsGpuAbi.ByteLength), so the buffers take the
    // admitted world's length and are reallocated only when an admission changes it.
    private byte[] _currentBuffer = [];
    private readonly byte[][] _candidateBuffers = [[], []];
    private int _candidateIndex;
    private readonly WorkshopPhysicsEngine _engine = new();
    private bool _readyCandidate;
    private bool _initialized;

    public Task Initialize()
    {
        _engine.Reset();
        _readyCandidate = false;
        _candidateIndex = 0;
        _initialized = true;
        return Task.CompletedTask;
    }

    public Task Stage(byte[] input, WorkshopGpuOperation operation, PhysicsTickInputs inputs)
    {
        if (!_initialized) throw new InvalidOperationException("GPU transport is not initialized.");
        if (_readyCandidate) throw new InvalidOperationException("Previous candidate has not been read or committed.");

        if (operation == WorkshopGpuOperation.Admit)
        {
            if (input.Length != PhysicsGpuAbi.ByteLength(input))
                throw new ArgumentException("Invalid GPU candidate input length.");
            _engine.Reset();
            Array.Copy(input, Candidate(input.Length), input.Length);
        }
        else if (operation == WorkshopGpuOperation.Advance)
        {
            _engine.Advance(_currentBuffer, Candidate(_currentBuffer.Length), inputs);
        }
        else
        {
            throw new ArgumentException("Invalid GPU candidate operation.");
        }

        _readyCandidate = true;
        return Task.CompletedTask;
    }

    private byte[] Candidate(int length)
    {
        if (_candidateBuffers[_candidateIndex].Length != length) _candidateBuffers[_candidateIndex] = new byte[length];
        return _candidateBuffers[_candidateIndex];
    }

    public byte[] Read()
    {
        if (!_readyCandidate) throw new InvalidOperationException("Candidate readback has not completed.");
        return _candidateBuffers[_candidateIndex];
    }

    public void Commit()
    {
        if (!_readyCandidate) throw new InvalidOperationException("Candidate is not ready to commit.");
        var candidate = _candidateBuffers[_candidateIndex];
        if (_currentBuffer.Length != candidate.Length) _currentBuffer = new byte[candidate.Length];
        Array.Copy(candidate, _currentBuffer, candidate.Length);
        _candidateIndex = 1 - _candidateIndex;
        _readyCandidate = false;
    }

    public void Discard()
    {
        _readyCandidate = false;
    }

    public bool DeviceReady() => _initialized;

    public void Dispose()
    {
        _initialized = false;
        _readyCandidate = false;
        _engine.Reset();
    }
}
