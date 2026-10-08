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
    public WorkshopPhase WorkshopPhase { get; private set; } = WorkshopPhase.Uninitialized;
    public WorkshopConstruction Construction { get; private set; } = new(new(1), null);
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

    public async Task<WorkshopCompletion> ReplaceConstruction(WorkshopBall? ball)
    {
        if (_workshopPending || WorkshopPhase != WorkshopPhase.Building)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), false);
        if (Construction.Revision.Value == ulong.MaxValue)
            return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.IdentityExhausted), false);
        var next = new WorkshopConstruction(new(Construction.Revision.Value + 1), ball);
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
            _ => throw new ArgumentException("Unsupported scene command.")
        };
        try
        {
            var delivery = await client.Execute(kind, next);
            var response = delivery.Response;
            if (_workshopRemoved || operation != _workshopOperation || !delivery.Applicable)
                return new(response, false);
            if (response.Result.Outcome == WorkshopCommandOutcome.Applied)
                ValidateWorkshopRead(response.Read, next ?? Construction);
            WorkshopPhase = response.Phase;
            if (response.Result.Outcome == WorkshopCommandOutcome.Applied)
            {
                if (next is { } admitted) Construction = admitted;
                ApplyWorkshopRead(response.Read);
            }
            else if (response.Result.Outcome == WorkshopCommandOutcome.Rejected) WorkshopPhase = previous;
            LogWorkshop(response);
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
            if (!_workshopClient.TryRead(out var response)) return;
            if (response.Read.Epoch != _workshopClient.Epoch || response.Read.Epoch.Value < WorkshopRead.Epoch.Value ||
                (response.Read.Epoch == WorkshopRead.Epoch && response.Read.Tick.Value < WorkshopRead.Tick.Value)) return;
            ValidateWorkshopRead(response.Read, Construction);
            // Committed phase and pending command ownership are independent.
            WorkshopPhase = response.Phase;
            ApplyWorkshopRead(response.Read);
        }
        catch (Exception error) { WorkshopPhase = WorkshopPhase.Faulted; WorkshopFault = error.Message; Running = false; }
    }

    private void ApplyWorkshopRead(WorkshopRead read)
    {
        ValidateWorkshopRead(read, Construction);
        WorkshopRead = read;
        Ticks = checked((int)read.Tick.Value);
        DisplaySimulationTime = Ticks / 120.0; // External presentation clock.
        Running = WorkshopPhase == WorkshopPhase.Running;
        if (read.Ball is { } pose && _parts.Count == 1) _parts[0].Position = RenderPosition(pose.Cell, pose.Local);
    }

    private void ValidateWorkshopRead(WorkshopRead read, WorkshopConstruction construction)
    {
        if (read.Epoch != _workshopClient!.Epoch || read.Revision != _workshopClient.Revision || read.Tick.Value > 3600 ||
            (read.Ball is null) != (construction.Ball is null))
            throw new ArgumentException("Read does not own the admitted construction.");
        if (read.Ball is { } body && construction.Ball is { } ball)
        {
            body.Validate();
            if (body.Id != ball.Id || body.Epoch != read.Epoch.Value || body.Tick != read.Tick.Value ||
                body.Cell.X != ball.Cell.X || body.Cell.Z != ball.Cell.Z ||
                !HalfBits.Equal(body.Local.X, ball.Local.X) || !HalfBits.Equal(body.Local.Z, ball.Local.Z))
                throw new ArgumentException("Read identity differs from the admitted construction.");
            if (read.Tick.Value == 0 && (body.Cell != ball.Cell || !HalfBits.Equal(body.Local, ball.Local) ||
                !HalfBits.IsPositiveZero(body.Velocity)))
                throw new ArgumentException("Initial read changed canonical construction bits.");
        }
    }

    public void RestoreConstructionPresentation()
    {
        var ball = Construction.Ball;
        if (ball is null)
        {
            foreach (var part in _parts) { RemoveChild(part); part.Free(); }
            _parts.Clear(); _bodies.Clear();
            return;
        }
        if (_parts.Count == 0)
        {
            var part = Registry.Create(WorkshopPartKind.Basketball);
            part.EnsureConstructed();
            AddChild(part);
            _parts.Add(part); _bodies.Add(part);
        }
        var placed = _parts[0];
        placed.Position = RenderPosition(ball.Value.Cell, ball.Value.Local);
        var rotation = ball.Value.Rotation;
        placed.Quaternion = new((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W);
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
