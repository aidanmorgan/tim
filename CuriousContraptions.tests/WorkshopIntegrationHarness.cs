using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>One production wire/command/device path. Native time advances explicitly, never while observing.</summary>
internal sealed class WorkshopIntegrationHarness : IAsyncDisposable
{
    internal sealed class Clock : IWorkshopCaptureClock
    {
        public long Nanoseconds { get; private set; }
        private ulong _physicalSteps;
        public ClockGeneration Generation => new(1);
        public WorkshopClockStamp Capture() => new(WorkshopClockDomain.SimulationMonotonic, Generation,
            new(Nanoseconds), new(0));
        public void Tick(WorkshopCadenceSettings settings)
        {
            _physicalSteps = checked(_physicalSteps + settings.PhysicalStepsPerCommit);
            Nanoseconds = checked((long)((UInt128)_physicalSteps * 1_000_000_000 / 480));
        }
    }

    private sealed class Host(WorkshopIntegrationHarness owner) : IWorkshopCommandHost, IWorkshopInstallation
    {
        public RuntimeSessionId Session => new(101, 206);
        public WorkshopSchedule? Schedule { get; private set; }
        public ClockGeneration Generation => owner.Time.Generation;
        public bool InstallationPending => _candidate is not null;
        public bool ReadsIdle => owner.ReadsIdle;
        public IWorkshopCaptureClock Clock => owner.Time;
        public IWorkshopInstallation Installation => this;
        private WorkshopSchedule? _candidate;
        private CommandSequence _owner;
        private ulong _projection;
        public TaskCompletionSource? Hold { get; set; }
        public bool FailNextStep { get; set; }

        public async ValueTask Prepare(CommandSequence command, WorkshopRead read, WorkshopCompiledScene construction,
            WorkshopGpuProfile profile, WorkshopSimulationPhase phase)
        {
            _owner = command;
            var playback = phase switch
            {
                WorkshopSimulationPhase.Building => WorldPlayback.Building,
                WorkshopSimulationPhase.Running => WorldPlayback.Running,
                WorkshopSimulationPhase.Paused => WorldPlayback.Paused,
                WorkshopSimulationPhase.Completed => WorldPlayback.Completed,
                _ => throw new ArgumentException("Unsupported installation phase.")
            };
            var now = new MasterTimeNanoseconds(owner.Time.Nanoseconds);
            _candidate = WorkshopSchedule.Create(construction.Settings, profile.Revision, Generation, new(0), now, now, now,
                new(read.Epoch, new(++_projection), read.Tick, now, playback));
            if (FailNextStep && phase == WorkshopSimulationPhase.Paused)
                throw new WorkshopInstallationException(new InvalidOperationException("Native installation refused."));
            if (Hold is { } held) await held.Task;
        }

        public void Abort(CommandSequence command) { if (_owner == command) Retire(); }
        public void Retire()
        {
            _candidate = null; _owner = default;
            Hold?.TrySetException(new WorkshopInstallationException()); Hold = null;
        }
        public ValueTask Before(WorkshopCommandKind kind, WorkshopSimulation? simulation)
        {
            return ValueTask.CompletedTask;
        }
        public ValueTask After(WorkshopCommandKind kind, WorkshopCommandResult result, WorkshopSimulation simulation)
        {
            if (result.Outcome == WorkshopCommandOutcome.Applied && _candidate is { } installed)
            { Schedule = installed; _candidate = null; _owner = default; }
            return ValueTask.CompletedTask;
        }
        public WorkshopMemoryMeasurement CaptureMemory() => WorkshopMemoryMeasurement.Capture();
    }

    public Clock Time { get; } = new();
    public EventRingPublication Publication { get; } = new();
    public WorkshopGpuDevice Device { get; }
    public WorkshopCommandExecutor Executor { get; }
    public WorkshopCommandRouter Router { get; }
    public WorkshopSimulation Simulation => Executor.Simulation;
    public WorkshopResponse Latest { get; private set; }
    public WorkshopRead Committed => Latest.Read;
    public WorkshopConstruction Construction => _authored[Simulation.Scene.Identity];
    private readonly Dictionary<CompiledSceneId, WorkshopConstruction> _authored = new();
    public WorkshopSchedule Schedule => _host.Schedule ?? throw new InvalidOperationException("No schedule installed.");
    public bool ReadsIdle { get; set; } = true;
    public bool FailNextStep { set => _host.FailNextStep = value; }
    public EventRingMemory? Shared => Publication.Memory;
    public List<SimulationEventRecord> Records { get; } = [];
    public List<byte[]> Publications { get; } = [];
    public List<WorkshopRead> Reads { get; } = [];
    // Accounting diagnostics are explicit; ordinary integration assertions use decoded reads and ring deliveries.
    public List<ContactWorkLedger> Ledgers { get; } = [];
    public WorkshopDeclaredPresentation? Presentation { get; private set; }
    private readonly Host _host;
    private readonly EventRingConsumer _consumer = new();
    private ulong _sequence, _publication;
    private bool _disposed;

    private WorkshopIntegrationHarness()
    {
        var entries = PartRegistry.Read(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "elements", "elements.bundle.json")));
        DeclaredPhysics.Install(entries);
        DeclaredPorts.Install(ElementPortCatalogue.FromBundle(entries));
        Device = new(new BrowserGpuTransport(Publication), new(101, 206));
        _host = new(this);
        Executor = new(Device, _host);
        Router = new(Executor.Execute);
    }

    public static async Task<WorkshopIntegrationHarness> Initialized(WorkshopCadenceSettings settings)
    {
        var harness = new WorkshopIntegrationHarness();
        await harness.Require(WorkshopCommandKind.Initialize, settings: settings);
        return harness;
    }
    public static async Task<WorkshopIntegrationHarness> Building(WorkshopConstruction construction)
    {
        var harness = await Initialized(construction.Settings);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        return harness;
    }
    public static async Task<WorkshopIntegrationHarness> Running(WorkshopConstruction construction)
    {
        var harness = await Building(construction);
        await harness.Run();
        return harness;
    }

    public WorkshopCommand Command(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
    {
        var sequence = new CommandSequence(checked(_sequence + 1));
        WorkshopCompiledScene? scene = null;
        WorkshopConstruction? source = kind switch
        {
            WorkshopCommandKind.Initialize => new WorkshopConstruction(new(1), settings!.Value, WorkshopInstances.Empty),
            WorkshopCommandKind.Construct => construction!.Value,
            WorkshopCommandKind.ConfigureCadence => Construction with { Settings = settings!.Value },
            _ => null
        };
        if (source is { } authored)
        {
            scene = WorkshopSceneCompiler.Compile(authored, new(101, 206), new(sequence.Value));
            _authored.Add(scene.Identity, authored);
        }
        return new(sequence, kind, Latest.Read.Epoch.Value == 0 ? new(1) : Latest.Read.Epoch,
            Latest.Read.Revision, scene, target, Session: _host.Session,
            Cadence: _host.Schedule?.Revision ?? default, Projection: _host.Schedule?.World.Epoch ?? default,
            Electrical: electrical);
    }

    private readonly WorkshopClientCursor _cursor = new(new(101, 206));

    public async Task<WorkshopResponse> Dispatch(WorkshopCommand command)
    {
        var readOrder = _cursor.ReadOrder;
        var routed = Router.Dispatch(WorkshopWireFlatBuffers.EncodeCommand(command));
        _sequence = Math.Max(_sequence, command.Sequence.Value);
        var bytes = await routed.Response;
        var response = WorkshopWireFlatBuffers.DecodeResponse(bytes);
        var prepared = _cursor.PrepareAcknowledgement(command, response, readOrder, false);
        _cursor.Commit(prepared);
        if (prepared.Applicable)
        {
            Accept(response);
        }
        return response;
    }
    public Task<WorkshopResponse> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null) =>
        Dispatch(Command(kind, construction, target, settings, electrical));
    public async Task<WorkshopCommandResult> Require(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
        WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
    {
        var response = await Execute(kind, construction, settings: settings, electrical: electrical);
        Assert.Equal(WorkshopCommandOutcome.Applied, response.Result.Outcome);
        return response.Result;
    }
    private void Accept(WorkshopResponse response)
    {
        Latest = response;
        if (response.Read.Epoch.Value != 0 && response.Phase != WorkshopSimulationPhase.Disposed)
        {
            if (Presentation is null || _presentationProjection != response.Projection)
            {
                if (Presentation is not null && Presentation.CanRetain(Simulation.Scene, response))
                    Presentation.Rebind(Simulation.Scene, response);
                else Presentation = new(Simulation.Scene, response);
                _presentationProjection = response.Projection;
            }
        }
        Publish();
        if (response.Kind == WorkshopResponseKind.Read) Presentation?.Accept(response);
    }
    private ProjectionEpoch _presentationProjection;
    public byte[] Publish()
    {
        var batch = Device.DrainEvents();
        if (batch.Length != 0) Publications.Add(batch);
        if (Shared is not { } memory) return batch;
        var before = _consumer.Lapped;
        var delivered = new List<SimulationEventRecord>();
        while (_consumer.TryRead(memory, SimulationDomain.None, out var record)) delivered.Add(record);
        Records.AddRange(delivered);
        if (Presentation is not null && _host.Schedule is { } schedule)
            Presentation.AcceptEvents(EventRingDelivery.Read(EventRingDelivery.Write(memory.Epoch,
                _consumer.Lapped - before, delivered.ToArray())), schedule.World);
        return batch;
    }
    public async Task Run()
    {
        await Require(WorkshopCommandKind.Run);
        Reads.Clear(); Ledgers.Clear(); Record();
    }
    public async Task<List<WorkshopRead>> Advance(int ticks)
    {
        var result = new List<WorkshopRead>(ticks);
        for (var i = 0; i < ticks; i++)
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await AdvanceOne()).Outcome);
            Record(); result.Add(Committed);
        }
        return result;
    }
    public async Task<WorkshopCommandResult> AdvanceOne()
    {
        Time.Tick(Simulation.Scene.Settings);
        var result = await Simulation.Advance();
        AcceptRead(WorkshopWireFlatBuffers.DecodeResponse(Executor.Encode(new(default, WorkshopResponseKind.Read,
            result, Simulation.Phase, Simulation.Committed, Publication: new(++_publication)))));
        return result;
    }
    public async Task<List<WorkshopRead>> AdvanceIncludingCurrent(int ticks)
    {
        var before = Committed;
        return [before, .. await Advance(ticks)];
    }
    public async Task<byte[]> AdvanceUndrained(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            Time.Tick(Simulation.Scene.Settings);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Advance()).Outcome);
            Latest = WorkshopWireFlatBuffers.DecodeResponse(Executor.Encode(new(default, WorkshopResponseKind.Read,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), Simulation.Phase, Simulation.Committed,
                Publication: new(++_publication))));
            Record();
        }
        var batch = Publish();
        if (ticks > 0) AcceptRead(Latest);
        return batch;
    }
    private void AcceptRead(WorkshopResponse response)
    {
        var prepared = _cursor.PrepareRead(response);
        _cursor.Commit(prepared);
        if (prepared.Applicable) Accept(response);
    }
    private void Record() { Reads.Add(Committed); Ledgers.Add(Device.CommittedContactWork); }
    public Task<WorkshopCommandResult> Reset() => Require(WorkshopCommandKind.Reset);
    public IEnumerable<SimulationEventRecord> Of(SimulationEventKind kind) => Records.Where(record => record.Kind == kind);
    public TaskCompletionSource HoldInstallation() => _host.Hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void CompleteInstallation() { var held = _host.Hold; _host.Hold = null; held?.SetResult(); }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await Require(WorkshopCommandKind.Dispose);
        _disposed = true;
    }

    public static async Task<List<WorkshopRead>> Observe(WorkshopConstruction construction, int ticks) =>
        (await ObserveAccounting(construction, ticks)).Reads;
    public static async Task<(WorkshopRead Built, List<WorkshopRead> Run, WorkshopRead Reset)> ObserveReset(WorkshopConstruction construction, int ticks,
        SimulationCadence? cadence = null, bool includeBuilt = false)
    {
        if (cadence is { } selected) construction = construction with { Settings = construction.Settings with { Simulation = selected } };
        await using var harness = await Building(construction);
        var built = harness.Committed;
        await harness.Run();
        var reads = await harness.Advance(ticks);
        if (includeBuilt) reads.Insert(0, built);
        await harness.Reset();
        return (built, reads, harness.Committed);
    }
    public static async Task<(List<WorkshopRead> Reads, List<ContactWorkLedger> Ledgers)> ObserveAccounting(WorkshopConstruction construction, int ticks)
    {
        await using var harness = await Running(construction);
        await harness.Advance(ticks);
        return (harness.Reads, harness.Ledgers);
    }
    public static async Task<List<List<WorkshopRead>>> ObservePhases(WorkshopConstruction construction,
        params (WorkshopElectricalControl[] Controls, int Ticks)[] phases)
    {
        await using var harness = await Running(construction);
        var result = new List<List<WorkshopRead>>();
        foreach (var (controls, ticks) in phases)
        {
            foreach (var control in controls) await harness.Require(WorkshopCommandKind.ConfigureElectrical, electrical: control);
            result.Add(await harness.Advance(ticks));
        }
        return result;
    }
}
