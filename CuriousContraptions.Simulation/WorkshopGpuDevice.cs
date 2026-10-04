using System.Buffers.Binary;
using CuriousContraptions.Gpu;

internal interface IWorkshopGpuTransport
{
    Task Initialize(string preamble);
    Task Stage(byte[] input, WorkshopGpuOperation operation);
    byte[] Read();
    void Commit();
    void Discard();
    bool DeviceReady();
    void Dispose();
}

internal sealed class WorkshopGpuDevice(IWorkshopGpuTransport transport, PhysicsDocumentId document) : IWorkshopGpuDevice
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CommandSequence _active;
    private bool _initialized;
    private WorkshopGpuProfile _committedProfile;
    private WorkshopGpuProfile _candidateProfile;
    private bool _disposed;
    private byte[] _committedWorld = [];
    private byte[] _candidateWorld = [];
    private PhysicsCaptureRead _committedCaptures;
    private PhysicsCaptureRead _candidateCaptures;
#if PLAYTEST
    private byte[] _diagnosticCandidate = [];
    internal byte[] DiagnosticCommitted { get; private set; } = [];
#endif

    public ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence)
    {
        if (construction.Settings.Simulation != profile.Cadence || construction.Settings.Physical != profile.Physical)
            throw new ArgumentException("Construction and physical profile differ.");
        var scene = WorkshopPhysicsCompiler.Compile(construction, document);
        return Stage(PhysicsGpuAbi.Admission(scene, epoch, profile), WorkshopGpuOperation.Admit, profile, new(0), sequence);
    }
    public ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence)
    {
        if (_committedWorld.Length != PhysicsGpuAbi.ByteLength ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.AsSpan(32)) != committed.Epoch.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.AsSpan(40)) != committed.Tick.Value)
            throw new ArgumentException("Advance does not own the committed physical world.");
        return Stage([], WorkshopGpuOperation.Advance, _committedProfile, new(checked(committed.Tick.Value + 1)), sequence);
    }

    private async ValueTask<WorkshopGpuCandidate> Stage(byte[] input, WorkshopGpuOperation operation, WorkshopGpuProfile profile, SimulationTick expectedTick, CommandSequence sequence)
    {
        profile.Validate();
        await _gate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_active.Value != 0 || sequence.Value == 0) throw new InvalidOperationException("Invalid GPU candidate ownership.");
            _active = sequence;
            if (!_initialized)
            {
                await transport.Initialize(PhysicsGpuAbi.ShaderPreamble());
                ObjectDisposedException.ThrowIf(_disposed, this);
                _initialized = true;
            }
            await transport.Stage(input, operation);
            ObjectDisposedException.ThrowIf(_disposed, this);
            var bytes = transport.Read();
            if (bytes.Length != PhysicsGpuAbi.ByteLength) throw new ArgumentException("Invalid GPU candidate length.");
            var failure = PhysicsGpuAbi.ReadFailure(bytes);
            if (failure != PhysicsFailure.None)
            {
                if (operation == WorkshopGpuOperation.Admit)
                    throw new GpuAdmissionException("Construction is outside the admitted generic mechanical capability.");
                throw new PhysicsNumericalException(failure);
            }
            if (operation == WorkshopGpuOperation.Admit && !bytes.AsSpan().SequenceEqual(input))
                throw new ArgumentException("GPU admission changed canonical declaration bytes.");
            var source = operation == WorkshopGpuOperation.Admit ? input : _committedWorld;
            var motion = PhysicsGpuAbi.ValidateCandidate(bytes, source, expectedTick);
            if (PhysicsGpuAbi.ReadProfile(bytes) != profile)
                throw new ArgumentException("GPU candidate changed the admitted cadence identity.");
            var body = PhysicsGpuAbi.ReadDynamicBody(bytes);
            var sensorCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
            Span<CaptureLatch> captures = stackalloc CaptureLatch[PhysicsSceneDeclaration.SensorCapacity];
            if (operation == WorkshopGpuOperation.Advance && _committedCaptures.Count != sensorCount)
                throw new ArgumentException("The discrete controller population changed.");
            for (var i = 0; i < sensorCount; i++)
            {
                var sensor = PhysicsGpuAbi.ReadSensor(bytes, i);
                var previous = operation == WorkshopGpuOperation.Admit ? CaptureLatch.Clear(sensor.Id) : _committedCaptures[i];
                captures[i] = previous.Consume(sensor);
            }
            _candidateCaptures = new(captures[..sensorCount]);
            var read = new WorkshopRead(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(32))),
                expectedTick, body?.Body, Rotation: body?.Rotation, Angular: body?.AngularVelocity ?? default,
                Captures: _candidateCaptures,
                Motion: motion);
            _candidateWorld = bytes;
            _candidateProfile = profile;
#if PLAYTEST
            try { _diagnosticCandidate = WorkshopTraceRecord.Encode(read, profile); }
            catch { _diagnosticCandidate = []; } // Missing diagnostic data cannot reject a physical candidate.
#endif
            return new(sequence, read);
        }
        catch
        {
            _initialized = !_disposed && transport.DeviceReady();
            _active = default;
            _candidateWorld = []; _candidateCaptures = default;
            transport.Discard();
            _gate.Release();
            throw;
        }
    }
    public void MarkLost() => _initialized = false;
    public void Commit(CommandSequence sequence)
    {
        if (_disposed || _active != sequence || sequence.Value == 0)
            throw new InvalidOperationException("GPU candidate is not owned by this command.");
        transport.Commit();
        _committedProfile = _candidateProfile;
        _committedWorld = _candidateWorld; _committedCaptures = _candidateCaptures;
#if PLAYTEST
        DiagnosticCommitted = _diagnosticCandidate;
#endif
    }
    public void Discard(CommandSequence sequence)
    {
        if (_active != sequence || sequence.Value == 0) return;
        _active = default;
        _candidateWorld = []; _candidateCaptures = default;
#if PLAYTEST
        _diagnosticCandidate = [];
#endif
        if (!_disposed) transport.Discard();
        _gate.Release();
    }
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _committedWorld = []; _candidateWorld = [];
        _committedCaptures = default; _candidateCaptures = default;
        transport.Dispose();
        return ValueTask.CompletedTask;
    }
}
