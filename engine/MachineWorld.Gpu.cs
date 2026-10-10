using WorkshopPhase = CuriousContraptions.Gpu.WorkshopSimulationPhase;
using Godot;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CuriousContraptions;

/// <summary>Current admitted game path. Scene nodes are render/input adapters; the worker owns physics.</summary>
public partial class MachineWorld
{
    private IWorkshopClient? _workshopClient;
    private bool _workshopPending;
    private bool _workshopRemoved;
    private ulong _workshopOperation;
    private ulong _workshopWriteFrame;
    private byte _workshopPositionWrites;
    private WorkshopPresentationSample _workshopPresentation;
    public WorkshopPhase WorkshopPhase { get; private set; } = WorkshopPhase.Uninitialized;
    public WorkshopConstruction Construction { get; private set; } = new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty);
    public WorkshopRead WorkshopRead { get; private set; }
    public string? WorkshopFault { get; private set; }
    public bool HasPendingCommand => _workshopClient?.Pending is not null;
    public WorkshopTransportState TransportState => _workshopClient?.TransportState ?? WorkshopTransportState.Ready;
    public async Task<WorkshopCompletion> CancelPendingWorkshop()
    {
        var client = _workshopClient ?? throw new InvalidOperationException("Worker is unavailable.");
        var delivery = await client.CancelPending();
        var response = delivery.Response;
        if (_workshopRemoved || !delivery.Applicable) return new(response.Result, false);
        if (response.Result.Outcome == WorkshopCommandOutcome.Applied)
        {
            WorkshopPhase = response.Phase;
            Running = response.Phase == WorkshopPhase.Running;
            WorkshopFault = "Pending candidate cancelled. Its original result remains tracked; Reset after completion or reload if recovery is blocked.";
        }
        return new(response.Result, delivery.Applicable);
    }
    public WorkshopGoalPhase GoalPhase => _workshopClient is null ? WorkshopGoalPhase.NotApplicable :
        WorkshopGoalEvaluator.Evaluate(Construction.Puzzle.Goal, WorkshopRead, _workshopClient.Epoch);

    public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) => _workshopClient?.ControlUi(target, kind, visible);
    public bool TryUiFrame(WorkshopUiTarget target, out AnimationOpacity opacity)
    {
        opacity = default;
        return _workshopClient is not null && _workshopClient.TryUiFrame(Engine.GetProcessFrames(), _workshopPresentation, target, out opacity);
    }
    public Task InitializeWorkshop() => InitializeWorkshop(async () => await BrowserWorkshopClient.Create());

    internal async Task InitializeWorkshop(Func<Task<IWorkshopClient>> createClient)
    {
        if (WorkshopPhase != WorkshopPhase.Uninitialized) throw new InvalidOperationException("Workshop already initialized.");
        WorkshopPhase = WorkshopPhase.Initializing;
        try
        {
            var client = await createClient();
            if (_workshopRemoved) { await client.DisposeAsync(); return; }
            _workshopClient = client;
            Construction = Construction with { Settings = client.Settings };
            WorkshopRead = new(client.Epoch, new(0), default);
            WorkshopPhase = WorkshopPhase.Building;
        }
        catch (Exception error)
        {
            if (!_workshopRemoved) { WorkshopPhase = WorkshopPhase.Faulted; WorkshopFault = error.Message; }
            throw;
        }
    }

    /// <summary>Any ball kind: the catalogue resource's bits are captured and must equal the kind's declared material.</summary>
    public WorkshopBall CaptureBall(WorkshopPartKind kind, GpuBodyId id, Vector3 position, Quaternion rotation)
    {
        if (kind is not (WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall) || !Registry.Definitions.TryGetValue(kind, out var definition))
            throw new ArgumentException("Unsupported ball kind.");
        var material = definition.Ball?.Capture(kind)
            ?? throw new ArgumentException("A ball requires canonical resource values.");
        var q = rotation.Normalized(); // Named Godot input boundary, before canonical quantization.
        return WorkshopInput.Ball(kind, id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W)
            with { Material = material };
    }

    public WorkshopReceiver CaptureReceiver(GpuBodyId id, Vector3 position, Quaternion rotation)
    {
        var q = rotation.Normalized();
        return WorkshopInput.Receiver(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W);
    }

    public IWorkshopInstance CaptureInstance(WorkshopPartKind kind, GpuBodyId id, Vector3 position, Quaternion rotation, RampDimensions? rampDimensions = null, WallDimensions? wallDimensions = null)
    {
        var q = rotation.Normalized();
        return kind switch
        {
            WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall => CaptureBall(kind, id, position, q),
            WorkshopPartKind.Receiver => CaptureReceiver(id, position, q),
            WorkshopPartKind.Ramp => WorkshopInput.Ramp(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                rampDimensions ?? Registry.Definitions[WorkshopPartKind.Ramp].Ramp!.Capture()),
            WorkshopPartKind.Wall => WorkshopInput.Wall(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                wallDimensions ?? Registry.Definitions[WorkshopPartKind.Wall].Wall!.Capture()),
            WorkshopPartKind.ImpactSwitch => WorkshopInput.Switch(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                Construction.Instances.FirstOrDefault(instance => instance.Id == id) is WorkshopSwitch current ? current.Trigger : ContactTriggerSettings.Default),
            WorkshopPartKind.Delay => WorkshopInput.Delay(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                Construction.Instances.FirstOrDefault(instance => instance.Id == id) is WorkshopDelay timer ? timer.Duration :
                    Registry.Definitions[WorkshopPartKind.Delay].Delay!.Capture()),
            WorkshopPartKind.Springboard => WorkshopInput.Springboard(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                Construction.Instances.FirstOrDefault(instance => instance.Id == id) is WorkshopSpringboard spring ? spring.Settings :
                    Registry.Definitions[WorkshopPartKind.Springboard].Springboard!.Capture()),
            WorkshopPartKind.Battery => WorkshopInput.Battery(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                Construction.Instances.FirstOrDefault(instance => instance.Id == id) is WorkshopBattery battery ? battery.Settings :
                    Registry.Definitions[WorkshopPartKind.Battery].ElectricalSource!.Capture()),
            WorkshopPartKind.PinballBumper => WorkshopInput.Bumper(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W,
                Construction.Instances.FirstOrDefault(instance => instance.Id == id) is WorkshopBumper bumper ? bumper.Work :
                    BumperWork.FromCalibration(Registry.Definitions[WorkshopPartKind.PinballBumper].ContactWork!.Capture())),
            WorkshopPartKind.SignalLamp => WorkshopInput.Lamp(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W),
            WorkshopPartKind.Domino => WorkshopInput.Domino(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W),
            _ => throw new ArgumentException("Unsupported instance kind.")
        };
    }

    public async Task<WorkshopCompletion> ReplaceConstruction(WorkshopConstruction proposed)
    {
        if (_workshopPending || WorkshopPhase != WorkshopPhase.Building)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), false);
        if (Construction.Revision.Value == ulong.MaxValue)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.IdentityExhausted), false);
        var next = proposed with { Revision = new(Construction.Revision.Value + 1), Settings = Construction.Settings };
        next.Validate();
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Construct, next);
        var response = delivery.Response;
        return new(response.Result, delivery.Applicable);
    }

    public async Task<WorkshopCompletion> AcknowledgeConstructionSave()
    {
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Save);
        return new(delivery.Response.Result, delivery.Applicable);
    }

    public async Task<WorkshopCompletion> ConfigureElectrical(GpuBodyId owner, ElectricalEnable enabled)
    {
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.ConfigureElectrical, electrical: new(owner, enabled));
        return new(delivery.Response.Result, delivery.Applicable);
    }

    public async Task<WorkshopCompletion> PlaybackWorkshop(WorkshopCommandKind kind)
    {
        if (kind is not (WorkshopCommandKind.Pause or WorkshopCommandKind.Resume or WorkshopCommandKind.Step))
            throw new ArgumentException("Unsupported playback command.");
        var delivery = await ExecuteWorkshop(kind);
        return new(delivery.Response.Result, delivery.Applicable);
    }
    public async Task<WorkshopCompletion> RunWorkshop()
    {
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Run);
        return new(delivery.Response.Result, delivery.Applicable);
    }
    public async Task<WorkshopCompletion> ResetWorkshop()
    {
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Reset);
        var response = delivery.Response;
        if (delivery.Applicable && response.Result.Outcome == WorkshopCommandOutcome.Applied) RestoreConstructionPresentation();
        return new(response.Result, delivery.Applicable);
    }

    private async Task<WorkshopDelivery> ExecuteWorkshop(WorkshopCommandKind kind, WorkshopConstruction? next = null, WorkshopElectricalControl? electrical = null)
    {
        var client = _workshopClient ?? throw new InvalidOperationException("GPU worker is not ready.");
        if (_workshopPending) throw new InvalidOperationException("A Workshop command is pending.");
        _workshopPending = true;
        var operation = ++_workshopOperation;
        var previous = WorkshopPhase;
        WorkshopPhase = kind switch
        {
            WorkshopCommandKind.Construct => WorkshopPhase.Admitting,
            WorkshopCommandKind.Run => WorkshopPhase.Starting,
            WorkshopCommandKind.Reset => WorkshopPhase.Resetting,
            WorkshopCommandKind.Pause or WorkshopCommandKind.Resume or WorkshopCommandKind.Step or WorkshopCommandKind.Save or WorkshopCommandKind.ConfigureElectrical => previous,
            _ => throw new ArgumentException("Unsupported scene command.")
        };
        try
        {
            var delivery = await client.Execute(kind, next, electrical: electrical);
            var response = delivery.Response;
            if (_workshopRemoved || operation != _workshopOperation) return new(response, false);
            LogWorkshop(response); // Original command truth is independent of presentation admission.
            if (!delivery.Applicable) return new(response, false);
            if (response.Result.Outcome == WorkshopCommandOutcome.Applied)
                ValidateWorkshopRead(response.Read, next ?? Construction);
            WorkshopPhase = response.Phase;
            if (response.Result.Outcome == WorkshopCommandOutcome.Applied)
            {
                if (next is { } admitted) Construction = admitted;
                Construction = Construction with { Settings = client.Settings };
                if (next.HasValue) RestoreConstructionPresentation();
                WorkshopFault = null;
                ApplyWorkshopRead(response.Read);
            }
            else if (response.Result.Outcome == WorkshopCommandOutcome.Rejected) WorkshopPhase = previous;
            return delivery;
        }
        catch (Exception error)
        {
            if (!_workshopRemoved)
            {
                WorkshopPhase = WorkshopPhase.Faulted;
                WorkshopFault = error.Message;
                Running = false;
            }
            throw;
        }
        finally { if (operation == _workshopOperation) _workshopPending = false; }
    }

    private void PresentWorkshopRead()
    {
        if (_workshopClient is null || _workshopRemoved ||
            (_workshopPending && WorkshopPhase == WorkshopPhase.Admitting)) return;
        try
        {
            if (_workshopClient.TryRead(out var response))
            {
                if (response.Read.Epoch != _workshopClient.Epoch || response.Read.Epoch.Value < WorkshopRead.Epoch.Value ||
                    (response.Read.Epoch == WorkshopRead.Epoch && response.Read.Tick.Value < WorkshopRead.Tick.Value))
                    throw new ArgumentException("Presentation read regressed its admitted generation.");
                ValidateWorkshopRead(response.Read, Construction);
                WorkshopPhase = response.Phase;
                ApplyWorkshopRead(response.Read);
            }
            var selected = _workshopClient.TryPresent(Engine.GetProcessFrames(), out var presentation);
            if (selected)
            {
                // Resolve the complete validated set before mutating any rendered body.
                var targets = new Node3D[presentation.Bodies.Count];
                for (var index = 0; index < targets.Length; index++)
                {
                    var id = presentation.Bodies[index].Id;
                    targets[index] = _parts.Select(part => part.PhysicalVisual(id)).SingleOrDefault(target => target is not null)
                        ?? throw new ArgumentException("Presented body has no unique owned visual binding.");
                }
                _workshopPresentation = presentation;
                DisplaySimulationTime = presentation.SimulationTime.Seconds;
                for (var index = 0; index < targets.Length; index++)
                {
                    var pose = presentation.Bodies[index]; var target = targets[index];
                    var q = pose.Rotation;
                    var worldPose = GlobalTransform * new Transform3D(new Basis(new Quaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W)),
                        RenderPosition(pose.Cell, pose.Local));
                    var localPose = ((Node3D)target.GetParent()).GlobalTransform.AffineInverse() * worldPose;
                    ApplyWorkshopPosition(target, localPose.Origin);
                    target.Basis = localPose.Basis;
                }
                foreach (var part in _parts) part.ApplyPhysicalSpans();
            }
            var frame = Engine.GetProcessFrames();
            foreach (var part in _parts)
                if (part.HasCosmeticBindings && _workshopClient.TryCosmeticFrame(frame, _workshopPresentation, part.AuthoredId, out var cosmetic))
                    part.ApplyCosmetic(cosmetic);
            foreach (var part in _parts)
                if (part.HasElectricalBindings && _workshopClient.TryElectricalFrame(frame, _workshopPresentation, part.AuthoredId, out var electrical))
                    part.ApplyElectrical(electrical);
            // Telemetry follows the lowest-identity ball of either kind; zero only while the construction has no ball.
            var ballId = Construction.Instances.OfType<WorkshopBall>().Select(b => b.Id).OrderBy(id => id.Value).FirstOrDefault();
            var ball = _parts.FirstOrDefault(part => part.AuthoredId == ballId);
            var position = ball?.Position ?? Vector3.Zero;
            var rotation = ball?.Quaternion ?? Quaternion.Identity;
            _workshopClient.RecordPresentation(presentation, selected, new(frame,
                frame == _workshopWriteFrame ? _workshopPositionWrites : (byte)0, checked((byte)_parts.Count), DisplaySimulationTime,
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, rotation.W));
        }
        catch (Exception error) { WorkshopPhase = WorkshopPhase.Faulted; WorkshopFault = error.Message; Running = false; }
    }

    private void ApplyWorkshopRead(WorkshopRead read)
    {
        ValidateWorkshopRead(read, Construction);
        if (read.Epoch != WorkshopRead.Epoch)
            foreach (var part in _parts) if (part.HasCosmeticBindings) part.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
        WorkshopRead = read;
        Ticks = checked((int)read.Tick.Value);
        Running = WorkshopPhase == WorkshopPhase.Running;
    }

    private void ValidateWorkshopRead(WorkshopRead read, WorkshopConstruction construction)
    {
        if (read.Epoch != _workshopClient!.Epoch || read.Revision != _workshopClient.Revision ||
            read.Tick.Value > construction.Settings.RunTickLimit)
            throw new ArgumentException("Read does not own the admitted construction.");
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 1));
        read.Bodies.ValidateScene(scene, read.Epoch, read.Tick);
    }

    public void RestoreConstructionPresentation()
    {
        foreach (var part in _parts.ToArray())
            if (!Construction.Instances.Any(instance => instance.Id == part.AuthoredId && instance.Kind == part.Definition.WorkshopKind))
            { _parts.Remove(part); _bodies.Remove(part); RemoveChild(part); part.Free(); }
        foreach (var instance in Construction.Instances)
        {
            var part = _parts.FirstOrDefault(candidate => candidate.AuthoredId == instance.Id);
            if (part is null)
            {
                part = Registry.Create(instance.Kind); part.AuthoredId = instance.Id;
                part.EnsureConstructed(); AddChild(part); _parts.Add(part); _bodies.Add(part);
            }
            if (part.Definition.WorkshopKind != instance.Kind) throw new ArgumentException("Authored identity changed part kind.");
            if (instance is WorkshopRamp ramp && part is RampPart rampPart) rampPart.ApplyDimensions(ramp.Dimensions);
            if (instance is WorkshopWall wall && part is WallPart wallPart) wallPart.ApplyDimensions(wall.Dimensions);
            if (instance is WorkshopDelay delay && part is DelayPart delayPart) delayPart.ApplyDuration(delay.Duration);
            if (instance is WorkshopBattery battery && part is BatteryPart batteryPart)
            {
                batteryPart.ApplySource(battery.Settings);
                var fraction = battery.Settings.InitialFraction;
                var supply = battery.Settings.Enabled == ElectricalEnable.Enabled && fraction > 0 ? 1f : 0f;
                batteryPart.ApplyElectrical(new(supply, fraction >= .25f ? 1f : 0f, fraction >= .5f ? 1f : 0f,
                    fraction >= .75f ? 1f : 0f, fraction >= 1f ? 1f : 0f));
            }
            if (instance is WorkshopSpringboard spring && part is SpringboardPart springPart)
            {
                springPart.ApplySpring(spring.Settings);
                part.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, WorkshopPhysicsCompiler.SpringboardPlate(instance.Id));
            }
            if (instance is WorkshopBumper bumper && part is BumperPart bumperPart) bumperPart.ApplyWork(bumper.Work);
            part.Locked = instance.Locked;
            part.Position = RenderPosition(instance.Cell, instance.Local);
            var q = instance.Rotation;
            part.Quaternion = new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
            if (part.HasCosmeticBindings != instance.Cosmetic.IsDeclared || (part.HasCosmeticBindings && part.Cosmetic != instance.Cosmetic))
                throw new ArgumentException("Part artwork and its instance must declare the same cosmetic curve.");
            if (part.HasCosmeticBindings) part.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
            part.ResetPhysicalVisuals();
        }
    }

    private void ApplyWorkshopPosition(Node3D part, Vector3 position)
    {
        // One final physical position writer. Repeated unchanged endpoint application is clean.
        if (part.Position == position) return;
        var frame = Engine.GetProcessFrames();
        if (frame != _workshopWriteFrame) { _workshopWriteFrame = frame; _workshopPositionWrites = 0; }
        part.Position = position;
        _workshopPositionWrites = checked((byte)(_workshopPositionWrites + 1));
    }

    private static Vector3 RenderPosition(CellOrigin cell, LocalPosition local) =>
        new((cell.X + (float)local.X) / 16, (cell.Y + (float)local.Y) / 16, (cell.Z + (float)local.Z) / 16);

    private static void LogWorkshop(WorkshopResponse response)
    {
        // Exact read-only binary diagnostics, also available without arbitrary browser evaluation.
        GD.Print("CCGPU " + Convert.ToBase64String(WorkshopWire.Encode(response)));
    }

    private async Task DisposeWorkshop()
    {
        _workshopRemoved = true;
        _workshopOperation++;
        WorkshopPhase = WorkshopPhase.Disposed;
        if (_workshopClient is { } client)
        {
            _workshopClient = null;
            try { await client.DisposeAsync(); }
            catch (Exception error) { GD.PrintErr(error.Message); }
        }
    }
}
