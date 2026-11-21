using System.Buffers.Binary;
using System.Runtime.ExceptionServices;
using CuriousContraptions.Gpu;

internal readonly record struct WorkshopTransportCandidate(ReadOnlyMemory<byte> World, PhysicsInteractionSnapshot Interactions);

internal interface IWorkshopGpuTransport
{
    Task Initialize();
    Task Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, PhysicsTickInputs inputs);
    WorkshopTransportCandidate Read();
    void PrepareEvents(byte[] batch);
    void PublishEvents();
    void Commit();
    void Discard();
    bool DeviceReady();
    void Dispose();
}

internal sealed class WorkshopGpuDevice(IWorkshopGpuTransport transport, PhysicsDocumentId document) : IWorkshopWorkerDevice
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CommandSequence _active, _lastIssuedSequence;
    private WorkshopRead _committedRead, _candidateRead;
    private PhysicsTickInputs _committedInputs = PhysicsTickInputs.Admission, _candidateInputs = PhysicsTickInputs.Admission;
    private bool _initialized;
    private ExceptionDispatchInfo? _preparationFailure;
    private WorkshopGpuProfile _committedProfile;
    private WorkshopGpuProfile _candidateProfile;
    private bool _disposed;
    private ReadOnlyMemory<byte> _committedWorld;
    private ReadOnlyMemory<byte> _candidateWorld;
    private PhysicsInteractionSnapshot _committedInteractions=PhysicsInteractionSnapshot.Empty;
    private PhysicsInteractionSnapshot _candidateInteractions=PhysicsInteractionSnapshot.Empty;
    private PhysicsCaptureRead _candidateCaptures;
    // Story 9R.5b: read scratch sized from the admitted world's populations, reallocated only when a world needs more (per Run).
    private ContactTriggerRead[] _triggerScratch = new ContactTriggerRead[4];
    private OrientationSensorRead[] _orientationScratch = new OrientationSensorRead[4];
    private ContactLoadSensorRead[] _loadScratch = new ContactLoadSensorRead[4];

    /// <summary>A cleared span of exactly <paramref name="count"/> rows, growing the owned array only when it is too short.</summary>
    private static Span<T> Scratch<T>(ref T[] array, int count)
    {
        if (count > Array.MaxLength) throw new WorkshopStorageException("Read scratch storage is not representable.");
        if (count > 3L * array.Length / 4)
        {
            var capacity = (int)Math.Min(Array.MaxLength, Math.Max(2L * array.Length, (4L * count + 2) / 3));
            if (capacity != array.Length) array = new T[capacity];
        }
        var span = array.AsSpan(0, count); span.Clear();
        return span;
    }
    private PhysicsActivationRead _committedActivations, _candidateActivations;
    private PhysicsTimerRead _committedTimers, _candidateTimers;
    private PhysicsMemoryRead _committedMemory, _candidateMemory;
    private IActivationProgram? _committedNetwork, _candidateNetwork;
    // Supply routes compiled once at Admit: routes as indices into the admitted drive-slot and source tables.
    private sealed record CompiledSupply(ICompiledSupplyGraph Graph, int Sources, int Drives, AirflowSourceDeclaration[] Airflow);
    private CompiledSupply? _committedSupply, _candidateSupply;
    private PassageSensorTracker _committedPassageTracker = PassageSensorTracker.Empty;
    private PassageSensorTracker _candidatePassageTracker = PassageSensorTracker.Empty;
    // Story 9A.22: the event producer of the admitted world, the row states its records derive from, and the committed outbox.
    // A candidate's records are staged here and sequenced only by Commit; Discard and any failure drop them.
    private EventRecordProducer? _committedEvents, _candidateEvents;
    private EventSourceRows _committedSources, _candidateSources;
    private readonly EventRingOutbox _outbox = new();
    /// <summary>The committed records awaiting publication, with the ring's epoch, capacity and sequence.</summary>
    internal EventRingOutbox Events => _outbox;
    /// <summary>The committed world's compiled event rows.</summary>
    /// <summary>The committed world's device-private contact-work ledger (Story 9A.23 D9): the occurrence rows behind its Impact records.</summary>
    internal ContactWorkLedger CommittedContactWork => _committedSources.ContactWorks;
    /// <summary>The committed world memory, read only through <see cref="PhysicsGpuAbi"/>'s defined row readers (for example a compliant
    /// surface's committed per-sample contact states, Story 10.5).</summary>
    internal ReadOnlyMemory<byte> CommittedWorld => _committedWorld;
    /// <summary>The publication of every committed record not yet published (the worker copies it into the event ring).</summary>
    public byte[] DrainEvents()
    {
        if (_outbox.HasCandidate) return [];
        transport.PublishEvents();
        return _outbox.Drain();
    }
#if PLAYTEST
    private byte[] _diagnosticCandidate = [];
    public byte[] DiagnosticCommitted { get; private set; } = [];
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
        finally { ReleaseGate(); }
    }

    private async Task EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _preparationFailure?.Throw();
        if (_initialized) return;
        await transport.Initialize();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!transport.DeviceReady()) throw new InvalidOperationException("GPU preparation did not retain a live device.");
        _initialized = true;
    }

    public ValueTask<WorkshopGpuCandidate> Admit(ICompiledScene construction, SimulationEpoch epoch, WorkshopGpuProfile profile, CommandSequence sequence)
    {
        if (construction.Settings.Simulation != profile.Cadence || construction.Settings.Physical != profile.Physical)
            throw new ArgumentException("Construction and physical profile differ.");
        var scene = construction.Physics;
        if (scene.Document != document) throw new ArgumentException("Compiled scene belongs to another document.");
        var guides = scene.RopeGuides.ToArray();
        return Stage(PhysicsGpuAbi.Admission(scene, epoch, profile), WorkshopGpuOperation.Admit, profile, new(0), sequence, construction.Activation,
            new(scene.SupplyGraph, scene.Electrical.Sources.Length, scene.RevoluteDrives.Length, scene.AirflowSources.ToArray()), new PhysicsTickInputs([], []) { Guides = guides, AdmissionDrivenSurfaces = scene.DrivenSurfaces.ToArray() }, scene);
    }
    public ValueTask<WorkshopGpuCandidate> Advance(WorkshopRead committed, CommandSequence sequence, WorkshopTickControls controls)
    {
        ArgumentNullException.ThrowIfNull(controls);
        if (_committedWorld.Length < PhysicsGpuAbi.HeaderBytes || _committedWorld.Length != PhysicsGpuAbi.ByteLength(_committedWorld.Span) ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.Span.Slice(PhysicsGpuAbi.HeaderEpochOffset)) != committed.Epoch.Value ||
            BinaryPrimitives.ReadUInt64LittleEndian(_committedWorld.Span.Slice(PhysicsGpuAbi.HeaderTickOffset)) != committed.Tick.Value ||
            PhysicsGpuAbi.ReadPhysicalRevision(_committedWorld.Span) != committed.PhysicalRevision)
            throw new ArgumentException("Advance does not own the committed physical world.");
        var network = _committedNetwork ?? throw new InvalidOperationException("No committed activation declarations.");
        var supply = _committedSupply ?? throw new InvalidOperationException("No committed supply-route table.");
        controls.Validate(supply.Sources, supply.Drives);
        // One-way: committed discrete switch state and the player's source enables decide this step's supply routes.
        var targetTick = new SimulationTick(checked(committed.Tick.Value + 1));
        return Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.Advance, _committedProfile, targetTick, sequence,
            network, supply, PhysicsTickInputs.Admission, controls: controls);
    }

    public ValueTask<WorkshopGpuCandidate> ApplyFiniteWork(WorkshopRead committed, CommandSequence sequence, OwnedFiniteWorkRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_gate.Wait(0)) throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.Busy);
        try
        {
            if (_committedWorld.IsEmpty) throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.NoWorld);
            if (sequence.Value == 0 || sequence.Value <= _lastIssuedSequence.Value)
                throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.InvalidSequence);
            if (committed.Epoch != _committedRead.Epoch || request.Epoch != _committedRead.Epoch)
                throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.StaleEpoch);
            if (committed.Tick != _committedRead.Tick || committed.PhysicalRevision != _committedRead.PhysicalRevision)
                throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.StaleCommit);
            if (!committed.PhysicalRevision.TryAdvance(out _))
                throw new FiniteWorkTransactionException(FiniteWorkTransactionRefusal.IdentityExhausted);
            _ = PhysicsGpuAbi.ValidateFiniteWorkRequest(_committedWorld.Span, request);
            var inputs = _committedInputs with { OwnedFiniteWork = request, PhysicalRequests = [], Cuts = [] };
            return Stage(ReadOnlyMemory<byte>.Empty, WorkshopGpuOperation.ApplyFiniteWork, _committedProfile,
                committed.Tick, sequence, _committedNetwork!, _committedSupply!, inputs, ownsGate: true);
        }
        catch { ReleaseGate(); throw; }
    }

    private PhysicsCandidateRead ValidatePhysicalCandidate(ReadOnlyMemory<byte> bytes, ReadOnlyMemory<byte> input,
        WorkshopGpuOperation operation, WorkshopGpuProfile profile, SimulationTick expectedTick)
    {
            if (bytes.Length < PhysicsGpuAbi.HeaderBytes || bytes.Length != PhysicsGpuAbi.ByteLength(bytes.Span))
                throw new ArgumentException("Invalid GPU candidate length.");
            var failure = PhysicsGpuAbi.ReadFailure(bytes.Span);
            if (failure != PhysicsFailure.None)
            {
                if (operation == WorkshopGpuOperation.Admit)
                    throw new GpuAdmissionException("Construction is outside the admitted generic mechanical capability.");
                throw new PhysicsNumericalException(failure);
            }
            if (operation == WorkshopGpuOperation.Admit && !bytes.Span.SequenceEqual(input.Span))
                throw new ArgumentException("GPU admission changed canonical declaration bytes.");
            var source = operation == WorkshopGpuOperation.Admit ? input : _committedWorld;
            var validated = PhysicsGpuAbi.ValidateCandidate(bytes.Span, source.Span, expectedTick,_candidateInteractions,
                operation==WorkshopGpuOperation.Admit ? PhysicsInteractionSnapshot.Empty : _committedInteractions);
            if (PhysicsGpuAbi.ReadProfile(bytes.Span) != profile)
                throw new ArgumentException("GPU candidate changed the admitted cadence identity.");
        return validated;
    }

    private async ValueTask<WorkshopGpuCandidate> Stage(ReadOnlyMemory<byte> input, WorkshopGpuOperation operation, WorkshopGpuProfile profile, SimulationTick expectedTick, CommandSequence sequence, IActivationProgram network,
        CompiledSupply supply, PhysicsTickInputs inputs, IPhysicsSceneDeclaration? scene = null, WorkshopTickControls? controls = null, bool ownsGate = false)
    {
        profile.Validate();
        if (!ownsGate) await _gate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_active.Value != 0 || sequence.Value == 0) throw new InvalidOperationException("Invalid GPU candidate ownership.");
            _active = sequence;
            if (sequence.Value > _lastIssuedSequence.Value) _lastIssuedSequence = sequence;
            IActivationSchedule? scheduled = null;
            Func<SupplyNodeId, bool>? contactClosed = null;
            if (operation == WorkshopGpuOperation.Advance)
            {
                if (controls is null) throw new ArgumentException("Advance requires tick controls.");
                scheduled = network.AdvanceScheduled(new(_committedActivations, _committedTimers, _committedMemory), expectedTick);
                var checkpoint = scheduled.State;
                var endpoints = FreezeEndpointContacts(supply.Graph, _committedWorld.Span);
                contactClosed = id => endpoints.TryGetValue(id, out var closed) ? closed :
                    network.ContactClosed(checkpoint.Activations, checkpoint.Timers, id);
                inputs = TickControlComposition.Compose(supply.Graph, controls,
                    contactClosed,
                    PhysicsGpuAbi.ReadElectrical(_committedWorld.Span), supply.Airflow,
                    id => network.StateLatchPhase(checkpoint.Activations, id) == SimulationLatchPhase.On) with
                { PhysicalRequests = PhysicalRequestComposition.Compose(network, _committedActivations) };
            }
            if(operation==WorkshopGpuOperation.Admit)
            {
                _triggerScratch = new ContactTriggerRead[4]; _orientationScratch = new OrientationSensorRead[4]; _loadScratch = new ContactLoadSensorRead[4];
                inputs = new([], [], supply.Graph.NeutralReads()) { Cuts = inputs.Cuts, Guides = inputs.Guides, AdmissionDrivenSurfaces = inputs.AdmissionDrivenSurfaces };
            }
            await EnsureInitialized();
            await transport.Stage(input, operation, inputs);
            ObjectDisposedException.ThrowIf(_disposed, this);
            var candidate=transport.Read();
            var bytes=candidate.World;
            _candidateInteractions=candidate.Interactions;
            var validated = ValidatePhysicalCandidate(bytes, input, operation, profile, expectedTick);
            var bodies = validated.Bodies;
            var layout=PhysicsGpuAbi.Layout(bytes.Span);
            _candidateCaptures=PhysicsGpuAbi.ReadCaptures(_candidateInteractions);
            // Every physical byte/event is validated before discrete evaluation. Neither
            // candidate is visible until the existing ownership-checked Commit below.
            var triggerCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.Span.Slice(PhysicsGpuAbi.HeaderTriggerCountOffset)));
            Span<ContactTriggerRead> triggers = Scratch(ref _triggerScratch, triggerCount);
            for (var i = 0; i < triggerCount; i++) triggers[i] = PhysicsGpuAbi.ReadTrigger(bytes.Span, i);
            var orientationCount = layout.OrientationSensors;
            Span<OrientationSensorRead> orientations = Scratch(ref _orientationScratch, orientationCount);
            for (var i = 0; i < orientationCount; i++) orientations[i] = PhysicsGpuAbi.ReadOrientationSensor(bytes.Span, i);
            if (operation == WorkshopGpuOperation.Admit)
            {
                _candidatePassageTracker = PassageSensorTracker.Create(network.PassageSensors,
                    scene ?? throw new ArgumentException("Admission requires its compiled scene."), bodies);
            }
            else if (operation == WorkshopGpuOperation.ApplyFiniteWork)
                _candidatePassageTracker = _committedPassageTracker;
            else
            {
                _candidatePassageTracker = _committedPassageTracker.Advance(expectedTick, bodies, validated.Motion);
            }
            var electrical = PhysicsGpuAbi.ReadElectrical(bytes.Span);
            // Story 9R.4: every contact-load row's committed phase (tick N) feeds its plate's level; its route closes at tick N+1.
            var loadCount = PhysicsGpuAbi.ReadContactLoadCount(bytes.Span);
            Span<ContactLoadSensorRead> loads = Scratch(ref _loadScratch, loadCount);
            for (var i = 0; i < loadCount; i++) loads[i] = PhysicsGpuAbi.ReadContactLoadSensor(bytes.Span, i);
            var effectiveSupply = operation == WorkshopGpuOperation.ApplyFiniteWork ? default :
                ResolveEffectiveSupply(operation, supply, controls, contactClosed, electrical);
            var logical = operation == WorkshopGpuOperation.Admit ? new ActivationCheckpoint(network.Clear(), network.ClearTimers(), network.ClearMemory()) :
                operation == WorkshopGpuOperation.ApplyFiniteWork ? new ActivationCheckpoint(_committedActivations, _committedTimers, _committedMemory) :
                network.ConsumeScheduled(scheduled ?? throw new InvalidOperationException("Scheduled checkpoint missing."),
                    triggers[..triggerCount], expectedTick, orientations[..orientationCount], _candidatePassageTracker.Fired, effectiveSupply, loads[..loadCount]);
            _candidateActivations = logical.Activations; _candidateTimers = logical.Timers; _candidateMemory = logical.Memory;            _candidateNetwork = network; _candidateSupply = supply;
            // Story 9A.23 D9: the contact-work occurrence rows are device-private. They feed only the Impact records and the reservoir
            // accounting (ValidateCandidate checks the ledger's advance); the read carries the owners' stores.
            var contactWorks=PhysicsGpuAbi.ReadContactWorks(bytes.Span,_candidateInteractions);
            // Story 9A.22: admission compiles the world's event rows and publishes nothing; an Advance stages its tick's records from
            // the committed and candidate row states and the network's transitions. Nothing is sequenced before Commit.
            var actuators = new PhysicsFiniteWorkActuatorRead(PhysicsGpuAbi.ReadFiniteWorkActuators(bytes.Span));
            var integrity = new PhysicsIntegrityRead(PhysicsGpuAbi.ReadIntegrity(bytes.Span));
            var sources = new EventSourceRows(_candidateCaptures, contactWorks, electrical, _candidatePassageTracker, actuators, integrity);
            if (operation == WorkshopGpuOperation.Admit)
            {
                contactWorks.ValidateScene(scene!, bodies);
                contactWorks.ValidateAdmission();
                _candidateEvents = EventRecordProducer.Compile(scene!, network, _candidatePassageTracker, profile);
                _outbox.PrepareAdmission(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes.Span.Slice(PhysicsGpuAbi.HeaderEpochOffset))));
            }
            else
            {
                _candidateEvents = _committedEvents ?? throw new InvalidOperationException("No committed event rows.");
                var records = operation == WorkshopGpuOperation.ApplyFiniteWork
                    ? EventRecordProducer.StageFiniteWork(expectedTick, BinaryPrimitives.ReadUInt32LittleEndian(bytes.Span[PhysicsGpuAbi.HeaderSourceOrdinalOffset..]),
                        _committedSources.FiniteWork, actuators)
                    : _candidateEvents.Stage(expectedTick, _committedSources, sources, logical.Transitions, loads[..loadCount]);
                _outbox.PreparePhysicalCommit(validated.PhysicalRevision, BinaryPrimitives.ReadUInt32LittleEndian(bytes.Span.Slice(PhysicsGpuAbi.HeaderSourceOrdinalOffset)), records);
            }
            transport.PrepareEvents(_outbox.PreparedBatch);
            _candidateSources = sources;
            var read = new WorkshopRead(new(BinaryPrimitives.ReadUInt64LittleEndian(bytes.Span.Slice(PhysicsGpuAbi.HeaderEpochOffset))),
                expectedTick, validated.PhysicalRevision, bodies,
                Captures: _candidateCaptures, Activations: _candidateActivations, Timers: _candidateTimers,
                Motion: validated.Motion, ContactWorks: contactWorks.Stores, Electrical: electrical, Memory: _candidateMemory,
                Presentation: PhysicsGpuAbi.ReadPresentation(bytes.Span, inputs, operation == WorkshopGpuOperation.Admit),
                DriveTargets: PhysicsGpuAbi.ReadDriveTargets(bytes.Span),
                EnergyStores: new PhysicsEnergyStoreRead(PhysicsGpuAbi.ReadEnergyStores(bytes.Span)),
                ChamberRegions: new PhysicsChamberRead(PhysicsGpuAbi.ReadChamberRegions(bytes.Span)),
                FiniteWorkActuators: actuators,
                GasStores: new PhysicsGasRead(PhysicsGpuAbi.ReadGas(bytes.Span)),
                IntegrityObservers: integrity, Airflow: new PhysicsAirflowRead(PhysicsGpuAbi.ReadAirflow(bytes.Span)));
            _candidateRead = read;
            _candidateInputs = inputs with { OwnedFiniteWork = null, AdmissionDrivenSurfaces = [], PhysicalRequests = [], Cuts = [] };
            if (operation == WorkshopGpuOperation.Admit)
                _candidateInputs = _candidateInputs with { SupplyClosed = new bool[layout.Prismatics + layout.Revolutes] };
            _candidateWorld = bytes;
            _candidateProfile = profile;
#if PLAYTEST
            try { _diagnosticCandidate = WorkshopTraceRecord.Encode(new(read.Epoch, read.Tick, read.Bodies, read.Captures), profile); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
            { _diagnosticCandidate = []; } // Missing diagnostic data cannot reject a physical candidate.
#endif
            return new(sequence, read);
        }
        catch
        {
            _initialized = !_disposed && transport.DeviceReady();
            _active = default;
            _candidateWorld = default; _candidateCaptures = default; _candidateInteractions=PhysicsInteractionSnapshot.Empty;
            _candidateActivations = default; _candidateTimers = default; _candidateMemory = default; _candidateNetwork = null; _candidateSupply = null; _candidatePassageTracker = _committedPassageTracker;
            DropCandidateEvents();
            transport.Discard();
            ReleaseGate();
            throw;
        }
    }
    private static SupplyOutputRead ResolveEffectiveSupply(WorkshopGpuOperation operation, CompiledSupply supply,
        WorkshopTickControls? controls, Func<SupplyNodeId, bool>? contactClosed, PhysicsElectricalRead electrical)
    {
        if (operation == WorkshopGpuOperation.Admit) return default;
        if (controls is null) throw new InvalidOperationException("Tick controls missing.");
        if (contactClosed is null) throw new InvalidOperationException("Frozen contact predicates missing.");
        return supply.Graph.Resolve(controls, contactClosed, electrical).Outputs;
    }

    private static Dictionary<SupplyNodeId, bool> FreezeEndpointContacts(ICompiledSupplyGraph graph, ReadOnlySpan<byte> world)
    {
        var contacts = new Dictionary<SupplyNodeId, bool>();
        var declarations = graph.Nodes.ToArray().Where(node => node.CoordinateContact.HasValue).ToArray();
        if (declarations.Length == 0) return contacts;
        var actuators = PhysicsGpuAbi.ReadPresentation(world, PhysicsTickInputs.Admission, admission: true).Actuators.ToArray();
        foreach (var node in declarations)
        {
            var endpoint = node.CoordinateContact!.Value;
            var actuator = actuators.Single(read => read.Drive == SupplyConsumer.Prismatic(endpoint.Joint));
            contacts.Add(node.Id, endpoint.Endpoint switch
            {
                CuriousContraptions.Abstractions.CoordinateEndpoint.Lower =>
                    actuator.Coordinate <= actuator.Lower + CuriousContraptions.Abstractions.PhysicalTolerance.ActuatorStopSlopMetres,
                CuriousContraptions.Abstractions.CoordinateEndpoint.Upper =>
                    actuator.Coordinate >= actuator.Upper - CuriousContraptions.Abstractions.PhysicalTolerance.ActuatorStopSlopMetres,
                _ => throw new ArgumentException("Undefined coordinate endpoint.")
            });
        }
        return contacts;
    }

    public void Commit(CommandSequence sequence)
    {
        if (_disposed || _active != sequence || sequence.Value == 0)
            throw new InvalidOperationException("GPU candidate is not owned by this command.");
        transport.Commit();
        _committedRead = _candidateRead; _committedInputs = _candidateInputs;
        _committedProfile = _candidateProfile;
        _committedWorld = _candidateWorld;
        _committedInteractions=_candidateInteractions;
        _committedActivations = _candidateActivations; _committedTimers = _candidateTimers; _committedMemory = _candidateMemory; _committedNetwork = _candidateNetwork; _committedSupply = _candidateSupply; _committedPassageTracker = _candidatePassageTracker;
        // The commit point of the event ring: an admission restarts it under its epoch, a tick sequences its staged records.
        _committedEvents = _candidateEvents; _committedSources = _candidateSources;
        _outbox.Commit();
        DropCandidateEvents();
#if PLAYTEST
        DiagnosticCommitted = _diagnosticCandidate;
#endif
        _active = default;
        ReleaseGate();
    }
    private void DropCandidateEvents()
    {
        _candidateEvents = null; _candidateSources = default; _outbox.Discard();
    }
    public void Discard(CommandSequence sequence)
    {
        if (_active != sequence || sequence.Value == 0) return;
        _active = default;
        _candidateWorld = default; _candidateCaptures = default; _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _candidateActivations = default; _candidateTimers = default; _candidateMemory = default; _candidateNetwork = null; _candidateSupply = null; _candidatePassageTracker = _committedPassageTracker;        DropCandidateEvents();
#if PLAYTEST
        _diagnosticCandidate = [];
#endif
        if (!_disposed) transport.Discard();
        ReleaseGate();
    }
    // The gate is held from Stage until Commit or Discard, and operations may queue for it. It is disposed exactly when
    // it is idle after device disposal. Release hands the count straight to a queued waiter, so CurrentCount is 1 only
    // when nothing holds or awaits the gate: that is at disposal itself, or at the release of the last operation that
    // held or awaited it then (a queued operation acquires, sees the disposal and releases normally).
    private void DisposeGateIfIdle()
    {
        if (_disposed && _gate.CurrentCount == 1) _gate.Dispose();
    }
    private void ReleaseGate()
    {
        _gate.Release();
        DisposeGateIfIdle();
    }
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        DisposeGateIfIdle();
        _committedWorld = default; _candidateWorld = default;
        _triggerScratch = []; _orientationScratch = []; _loadScratch = [];
        _candidateCaptures = default;
        _committedInteractions=PhysicsInteractionSnapshot.Empty; _candidateInteractions=PhysicsInteractionSnapshot.Empty;
        _committedActivations = default; _candidateActivations = default;
        _committedTimers = default; _candidateTimers = default;
        _committedMemory = default; _candidateMemory = default;
        _committedNetwork = null; _candidateNetwork = null; _committedSupply = null; _candidateSupply = null; _committedPassageTracker = PassageSensorTracker.Empty; _candidatePassageTracker = PassageSensorTracker.Empty;
        _committedEvents = null; _committedSources = default; DropCandidateEvents(); _outbox.Dispose();
        transport.Dispose();
        return ValueTask.CompletedTask;
    }
}
