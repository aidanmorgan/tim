using WorkshopPhase = CuriousContraptions.Gpu.WorkshopSimulationPhase;
using Godot;
using CuriousContraptions.Gpu;
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
    public WorkshopConstruction Construction { get; private set; } = new(new(1), null, WorkshopCadenceSettings.Default());
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
    public PartDefinition BasketballDefinition => Registry.Definitions[WorkshopPartKind.Basketball];

    public void ControlHint(HintControlKind kind, bool visible) => _workshopClient?.ControlHint(kind, visible);
    public bool TryHint(out WorkshopHintSample sample)
    {
        if (_workshopClient is null) { sample = default; return false; }
        return _workshopClient.TryHint(Engine.GetProcessFrames(), out sample);
    }
    public void RecordHintPresentation() => _workshopClient?.RecordHintPresentation(Engine.GetProcessFrames());
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
            WorkshopRead = new(client.Epoch, new(0), null);
            WorkshopPhase = WorkshopPhase.Building;
        }
        catch (Exception error)
        {
            if (!_workshopRemoved) { WorkshopPhase = WorkshopPhase.Faulted; WorkshopFault = error.Message; }
            throw;
        }
    }

    public WorkshopBall CaptureBasketball(GpuBodyId id, Vector3 position, Quaternion rotation)
    {
        var material = BasketballDefinition.Basketball?.Capture()
            ?? throw new ArgumentException("Basketball requires canonical resource values.");
        var q = rotation.Normalized(); // Named Godot input boundary, before canonical quantization.
        return WorkshopInput.Basketball(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W)
            with { Material = material };
    }

    public WorkshopReceiver CaptureReceiver(GpuBodyId id, Vector3 position, Quaternion rotation)
    {
        var q = rotation.Normalized();
        return WorkshopInput.Receiver(id, position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W);
    }

    private MachinePart? Part(WorkshopPartKind kind) => _parts.FirstOrDefault(p => p.Definition.WorkshopKind == kind);

    public async Task<WorkshopCompletion> ReplaceConstruction(WorkshopBall? ball, WorkshopReceiver? receiver)
    {
        if (_workshopPending || WorkshopPhase != WorkshopPhase.Building)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), false);
        if (Construction.Revision.Value == ulong.MaxValue)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.IdentityExhausted), false);
        var next = new WorkshopConstruction(new(Construction.Revision.Value + 1), ball, Construction.Settings, receiver);
        next.Validate();
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Construct, next);
        var response = delivery.Response;
        if (delivery.Applicable && response.Result.Outcome == WorkshopCommandOutcome.Applied)
        {
            Construction = next;
            RestoreConstructionPresentation();
        }
        return new(response.Result, delivery.Applicable);
    }

    public async Task<WorkshopCompletion> AcknowledgeConstructionSave()
    {
        var delivery = await ExecuteWorkshop(WorkshopCommandKind.Save);
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

    private async Task<WorkshopDelivery> ExecuteWorkshop(WorkshopCommandKind kind, WorkshopConstruction? next = null)
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
            WorkshopCommandKind.Pause or WorkshopCommandKind.Resume or WorkshopCommandKind.Step or WorkshopCommandKind.Save => previous,
            _ => throw new ArgumentException("Unsupported scene command.")
        };
        try
        {
            var delivery = await client.Execute(kind, next);
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
        if (_workshopClient is null || _workshopRemoved) return;
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
                _workshopPresentation = presentation;
                DisplaySimulationTime = presentation.SimulationTime.Seconds;
                if (presentation.Body is { } pose && Part(WorkshopPartKind.Basketball) is { } physical)
                {
                    ApplyWorkshopPosition(RenderPosition(pose.Cell, pose.Local));
                    var q = pose.Rotation;
                    physical.Quaternion = new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
                }
            }
            var frame = Engine.GetProcessFrames();
            if (Part(WorkshopPartKind.Receiver) is BasketPart receiver &&
                _workshopClient.TryCaptureOpacity(frame, _workshopPresentation, out var opacity))
            { receiver.ApplyHalo(opacity); _workshopClient.RecordCapturePresentation(frame); }
            var position = Part(WorkshopPartKind.Basketball)?.Position ?? Vector3.Zero;
            var rotation = Part(WorkshopPartKind.Basketball)?.Quaternion ?? Quaternion.Identity;
            _workshopClient.RecordPresentation(presentation, selected, new(frame,
                frame == _workshopWriteFrame ? _workshopPositionWrites : (byte)0, checked((byte)_parts.Count), DisplaySimulationTime,
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, rotation.W));
        }
        catch (Exception error) { WorkshopPhase = WorkshopPhase.Faulted; WorkshopFault = error.Message; Running = false; }
    }

    private void ApplyWorkshopRead(WorkshopRead read)
    {
        ValidateWorkshopRead(read, Construction);
        WorkshopRead = read;
        Ticks = checked((int)read.Tick.Value);
        Running = WorkshopPhase == WorkshopPhase.Running;
    }

    private void ValidateWorkshopRead(WorkshopRead read, WorkshopConstruction construction)
    {
        if (read.Epoch != _workshopClient!.Epoch || read.Revision != _workshopClient.Revision || read.Tick.Value > construction.Settings.RunTickLimit ||
            (read.Ball is null) != (construction.Ball is null))
            throw new ArgumentException("Read does not own the admitted construction.");
        if (read.Ball is { } body && construction.Ball is { } ball)
        {
            body.Validate();
            if (body.Id != ball.Id || body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value)
                throw new ArgumentException("Read identity differs from the admitted construction.");
            if (read.Tick.Value == 0 && (body.Cell != ball.Cell || !HalfBits.Equal(body.Local, ball.Local) ||
                !HalfBits.IsPositiveZero(body.Velocity)))
                throw new ArgumentException("Initial read changed canonical construction bits.");
        }
    }

    public void RestoreConstructionPresentation()
    {
        Restore(WorkshopPartKind.Basketball, Construction.Ball?.Cell, Construction.Ball?.Local, Construction.Ball?.Rotation);
        Restore(WorkshopPartKind.Receiver, Construction.Receiver?.Cell, Construction.Receiver?.Local, Construction.Receiver?.Rotation);
        if (Part(WorkshopPartKind.Receiver) is BasketPart receiver) receiver.ApplyHalo((Half)0);
    }

    private void Restore(WorkshopPartKind kind, CellOrigin? cell, LocalPosition? local, CanonicalRotation? rotation)
    {
        var part = Part(kind);
        if (cell is null)
        {
            if (part is not null) { _parts.Remove(part); _bodies.Remove(part); RemoveChild(part); part.Free(); }
            return;
        }
        if (part is null)
        {
            part = Registry.Create(kind); part.EnsureConstructed(); AddChild(part);
            _parts.Add(part); _bodies.Add(part);
        }
        part.Position = RenderPosition(cell.Value, local!.Value);
        var q = rotation!.Value;
        part.Quaternion = new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
    }

    private void ApplyWorkshopPosition(Vector3 position)
    {
        // One final physical position writer. Repeated unchanged endpoint application is clean.
        var part = Part(WorkshopPartKind.Basketball);
        if (part is null || part.Position == position) return;
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
