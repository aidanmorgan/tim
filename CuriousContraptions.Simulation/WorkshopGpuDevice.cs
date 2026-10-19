using System.Buffers.Binary;
using System.Runtime.ExceptionServices;
using CuriousContraptions.Gpu;

internal interface IWorkshopGpuTransport
{
    Task Initialize(string preamble);
    Task Stage(byte[] input, WorkshopGpuOperation operation, PhysicsTickInputs inputs);
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
    private ExceptionDispatchInfo? _preparationFailure;
    private WorkshopGpuProfile _committedProfile;
    private WorkshopGpuProfile _candidateProfile;
    private bool _disposed;
    private byte[] _committedWorld = [];
    private byte[] _candidateWorld = [];
    private PhysicsCaptureRead _committedCaptures;
    private PhysicsCaptureRead _candidateCaptures;
    private PhysicsActivationRead _committedActivations, _candidateActivations;
    private PhysicsTimerRead _committedTimers, _candidateTimers;
    private ActivationNetwork? _committedNetwork, _candidateNetwork;
    // Switched supplies compiled once at Admit: routes as indices into the admitted prismatic and source tables.
    private sealed record CompiledSupply(SwitchedSupplyRoute[] Routes, int Prismatics, int Sources);
    private CompiledSupply? _committedSupply, _candidateSupply;
    private PassageSensorTracker _committedPassageTracker = PassageSensorTracker.Empty;
    private PassageSensorTracker _candidatePassageTracker = PassageSensorTracker.Empty;
#if PLAYTEST
    private byte[] _diagnosticCandidate = [];
    internal byte[] DiagnosticCommitted { get; private set; } = [];
#endif

    // Resource preparation owns the same gate as candidates, but never stages a world.
    public async Task Prepare()
    {
        await _gate.WaitAsync();
        try { await EnsureInitialized(); }
        catch (Exception error)
        {
            // Publish failure before releasing the gate: a queued candidate cannot retry.
            _preparationFailure = ExceptionDispatchInfo.Capture(error);
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _preparationFailure?.Throw();
        if (_initialized) return;
        await transport.Initialize(PhysicsGpuAbi.ShaderPreamble());
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!transport.DeviceReady()) throw new InvalidOperationException("GPU preparation did not retain a live device.");
        _initialized = true;
    }

    public ValueTask<WorkshopGpuCandidate> Admit(WorkshopConstruction construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence)
    {
        if (construction.Settings.Simulation != profile.Cadence || construction.Settings.Physical != profile.Physical)
            throw new ArgumentException("Construction and physical profile differ.");
        var scene = WorkshopPhysicsCompiler.Compile(construction, document);
        return Stage(PhysicsGpuAbi.Admission(scene, epoch, profile), WorkshopGpuOperation.Admit, profile, new(0), sequence, WorkshopActivationCompiler.Compile(construction),
            new(scene.SwitchedSupplies.ToArray(), scene.Prismatics.Length, scene.Electrical.Sources.Length), PhysicsTickInputs.Admission, scene.Colliders.ToArray());
    }
    public ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence, WorkshopTickControls controls)
    {
        ArgumentNullException.ThrowIfNull(controls);
        if (_committedWorld.Length != PhysicsGpuAbi.ByteLength ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.AsSpan(PhysicsGpuAbi.HeaderEpochOffset)) != committed.Epoch.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.AsSpan(PhysicsGpuAbi.HeaderTickOffset)) != committed.Tick.Value)
            throw new ArgumentException("Advance does not own the committed physical world.");
        var network = _committedNetwork ?? throw new InvalidOperationException("No committed activation declarations.");
        var supply = _committedSupply ?? throw new InvalidOperationException("No committed switched-supply table.");
        controls.Validate(supply.Sources);
        // One-way: committed discrete switch state and the player's source enables decide this step's switched supplies.
        var activations = _committedActivations;
        var inputs = PhysicsTickInputs.Compose(supply.Routes, supply.Prismatics, controls, owner => network.ContactClosed(activations, owner));
        return Stage([], WorkshopGpuOperation.Advance, _committedProfile, new(checked(committed.Tick.Value + 1)), sequence,
            network, supply, inputs);
    }

    private async ValueTask<WorkshopGpuCandidate> Stage(byte[] input, WorkshopGpuOperation operation, WorkshopGpuProfile profile, SimulationTick expectedTick, CommandSequence sequence, ActivationNetwork network,
        CompiledSupply supply, PhysicsTickInputs inputs, ColliderDeclaration[]? colliders = null)
    {
        profile.Validate();
        await _gate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_active.Value != 0 || sequence.Value == 0) throw new InvalidOperationException("Invalid GPU candidate ownership.");
            _active = sequence;
            await EnsureInitialized();
            await transport.Stage(input, operation, inputs);
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
            var validated = PhysicsGpuAbi.ValidateCandidate(bytes, source, expectedTick);
            if (PhysicsGpuAbi.ReadProfile(bytes) != profile)
                throw new ArgumentException("GPU candidate changed the admitted cadence identity.");
            var bodies = validated.Bodies;
            var sensorCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderSensorCountOffset)));
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
            // Every physical byte/event is validated before discrete evaluation. Neither
            // candidate is visible until the existing ownership-checked Commit below.
            var triggerCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderTriggerCountOffset)));
            Span<ContactTriggerRead> triggers = stackalloc ContactTriggerRead[PhysicsSceneDeclaration.TriggerCapacity];
            for (var i = 0; i < triggerCount; i++) triggers[i] = PhysicsGpuAbi.ReadTrigger(bytes, i);
            var orientationCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderOrientationSensorCountOffset)));
            Span<OrientationSensorRead> orientations = stackalloc OrientationSensorRead[PhysicsSceneDeclaration.OrientationSensorCapacity];
            for (var i = 0; i < orientationCount; i++) orientations[i] = PhysicsGpuAbi.ReadOrientationSensor(bytes, i);
            Span<PassageSensorRead> passages = stackalloc PassageSensorRead[8];
            int passageCount = 0;
            if (operation == WorkshopGpuOperation.Admit)
            {
                _candidatePassageTracker = PassageSensorTracker.Create(network.PassageSensors, colliders, bodies);
            }
            else
            {
                _candidatePassageTracker = _committedPassageTracker.Advance(expectedTick, bodies, validated.Motion, passages, out passageCount);
            }
            var electrical = PhysicsGpuAbi.ReadElectrical(bytes);
            var logical = operation == WorkshopGpuOperation.Admit ? new ActivationCheckpoint(network.Clear(), network.ClearTimers()) :
                network.Consume(_committedActivations, _committedTimers, triggers[..triggerCount], expectedTick, orientations[..orientationCount], passages[..passageCount], electrical);
            _candidateActivations = logical.Activations; _candidateTimers = logical.Timers;
            _candidateNetwork = network; _candidateSupply = supply;
            var contactWorks=PhysicsGpuAbi.ReadContactWorks(bytes);
            var read = new WorkshopRead(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderEpochOffset))),
                expectedTick, bodies,
                Captures: _candidateCaptures, Activations: _candidateActivations, Timers: _candidateTimers,
                Motion: validated.Motion, ContactWorks: contactWorks, Electrical: electrical);
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
            _candidateActivations = default; _candidateTimers = default; _candidateNetwork = null; _candidateSupply = null; _candidatePassageTracker = _committedPassageTracker;
            transport.Discard();
            _gate.Release();
            throw;
        }
    }
    public void Commit(CommandSequence sequence)
    {
        if (_disposed || _active != sequence || sequence.Value == 0)
            throw new InvalidOperationException("GPU candidate is not owned by this command.");
        transport.Commit();
        _committedProfile = _candidateProfile;
        _committedWorld = _candidateWorld; _committedCaptures = _candidateCaptures;
        _committedActivations = _candidateActivations; _committedTimers = _candidateTimers; _committedNetwork = _candidateNetwork; _committedSupply = _candidateSupply; _committedPassageTracker = _candidatePassageTracker;
#if PLAYTEST
        DiagnosticCommitted = _diagnosticCandidate;
#endif
    }
    public void Discard(CommandSequence sequence)
    {
        if (_active != sequence || sequence.Value == 0) return;
        _active = default;
        _candidateWorld = []; _candidateCaptures = default;
        _candidateActivations = default; _candidateTimers = default; _candidateNetwork = null; _candidateSupply = null; _candidatePassageTracker = _committedPassageTracker;
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
        _committedActivations = default; _candidateActivations = default;
        _committedTimers = default; _candidateTimers = default;
        _committedNetwork = null; _candidateNetwork = null; _committedSupply = null; _candidateSupply = null; _committedPassageTracker = PassageSensorTracker.Empty; _candidatePassageTracker = PassageSensorTracker.Empty;
        transport.Dispose();
        return ValueTask.CompletedTask;
    }
}
