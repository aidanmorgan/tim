using System;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed class BrowserGpuTransport : IWorkshopGpuTransport
{
    private readonly byte[] _currentBuffer = new byte[PhysicsGpuAbi.ByteLength];
    private readonly byte[][] _candidateBuffers = [
        new byte[PhysicsGpuAbi.ByteLength],
        new byte[PhysicsGpuAbi.ByteLength]
    ];
    private int _candidateIndex;
    private readonly WorkshopPhysicsEngine _engine = new();
    private bool _readyCandidate;
    private bool _initialized;

    public Task Initialize(string preamble)
    {
        _engine.Reset();
        _engine.Warmup();
        _readyCandidate = false;
        _candidateIndex = 0;
        _initialized = true;
        return Task.CompletedTask;
    }

    public Task Stage(byte[] input, WorkshopGpuOperation operation, PhysicsTickInputs inputs)
    {
        if (!_initialized) throw new InvalidOperationException("GPU transport is not initialized.");
        if (_readyCandidate) throw new InvalidOperationException("Previous candidate has not been read or committed.");

        var candidate = _candidateBuffers[_candidateIndex];
        if (operation == WorkshopGpuOperation.Admit)
        {
            if (input.Length != PhysicsGpuAbi.ByteLength)
                throw new ArgumentException("Invalid GPU candidate input length.");
            _engine.Reset();
            Array.Copy(input, candidate, PhysicsGpuAbi.ByteLength);
        }
        else if (operation == WorkshopGpuOperation.Advance)
        {
            _engine.Advance(_currentBuffer, candidate, inputs);
        }
        else
        {
            throw new ArgumentException("Invalid GPU candidate operation.");
        }

        _readyCandidate = true;
        return Task.CompletedTask;
    }

    public byte[] Read()
    {
        if (!_readyCandidate) throw new InvalidOperationException("Candidate readback has not completed.");
        return _candidateBuffers[_candidateIndex];
    }

    public void Commit()
    {
        if (!_readyCandidate) throw new InvalidOperationException("Candidate is not ready to commit.");
        Array.Copy(_candidateBuffers[_candidateIndex], _currentBuffer, PhysicsGpuAbi.ByteLength);
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
