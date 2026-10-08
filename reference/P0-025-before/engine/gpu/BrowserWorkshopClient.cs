using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopClientId(int Value);
public sealed class WorkshopTransportException(WorkshopCommandIdentity identity, WorkshopTransportState state, Exception cause)
    : Exception("Workshop operation is indeterminate; recovery may require page reload.", cause)
{
    public WorkshopCommandIdentity Identity { get; } = identity;
    public WorkshopTransportState State { get; } = state;
}

/// <summary>Main-thread transport only; canonical state and numerical decisions remain in the worker.</summary>
public sealed partial class BrowserWorkshopClient : IWorkshopClient
{
    private readonly WorkshopClientId _id;
    private ulong _sequence;
    private int _pending;
    private bool _disposed;
    private readonly WorkshopClientCursor _cursor = new();
    public WorkshopCommandIdentity? Pending { get; private set; }
    public WorkshopTransportState TransportState
    {
        get
        {
            if (_disposed) return WorkshopTransportState.RecoveryBlocked;
            var state = (WorkshopTransportState)Status(_id.Value);
            if (!Enum.IsDefined(state)) throw new ArgumentException("Unknown transport state.");
            return state;
        }
    }
    public SimulationEpoch Epoch => _cursor.Epoch;
    public AuthorityRevision Revision => _cursor.Revision;
    private BrowserWorkshopClient(WorkshopClientId id) => _id = id;

    [JSImport("create", "workshopClient")]
    private static partial Task<int> CreateClient(int[] states);
    [JSImport("qualified", "workshopClient")]
    private static partial void Qualified(int client);
    [JSImport("status", "workshopClient")]
    private static partial int Status(int client);
    [JSImport("send", "workshopClient")]
    private static partial Task Send(int client, byte[] command);
    [JSImport("acknowledgement", "workshopClient")]
    private static partial byte[] Acknowledgement(int client, byte[] sequence);
    [JSImport("read", "workshopClient")]
    private static partial byte[] Read(int client);
    [JSImport("dispose", "workshopClient")]
    private static partial void DisposeClient(int client);

    public static async Task<BrowserWorkshopClient> Create()
    {
        if (!OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException("This Workshop requires Chrome WebGPU with shader-f16.");
        await JSHost.ImportAsync("workshopClient", "./workshop-client.js");
        var client = new BrowserWorkshopClient(new(await CreateClient([(int)WorkshopTransportState.Ready, (int)WorkshopTransportState.Backpressure,
            (int)WorkshopTransportState.TimedOut, (int)WorkshopTransportState.Indeterminate, (int)WorkshopTransportState.RecoveryBlocked])));
        try
        {
            var initialized = await client.Execute(WorkshopCommandKind.Initialize);
            if (initialized.Response.Result.Outcome != WorkshopCommandOutcome.Applied)
                throw new InvalidOperationException("The WebGPU simulation worker could not initialize.");
            Qualified(client._id.Value);
            return client;
        }
        catch { DisposeClient(client._id.Value); throw; }
    }

    public async Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null, WorkshopCommandIdentity? target = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pending >= 2) throw new InvalidOperationException("Workshop command queue is full.");
        if (_sequence == ulong.MaxValue) throw new InvalidOperationException("Workshop command sequence is exhausted.");
        var sequence = new CommandSequence(_sequence + 1);
        var revisionKind = kind is WorkshopCommandKind.Reset or WorkshopCommandKind.Cancel or WorkshopCommandKind.Dispose
            ? ExpectedRevisionKind.Any : ExpectedRevisionKind.Exact;
        var command = new WorkshopCommand(sequence, kind, Epoch,
            revisionKind == ExpectedRevisionKind.Any ? default : Revision, construction, target, revisionKind);
        var bytes = WorkshopWire.Encode(command);
        _sequence = sequence.Value;
        _pending++;
        var dispatchedReadOrder = _cursor.ReadOrder;
        var identity = new WorkshopCommandIdentity(sequence, command.Epoch, command.Revision);
        if (kind != WorkshopCommandKind.Cancel) Pending = identity;
        try
        {
            try { await Send(_id.Value, bytes); }
            catch (Exception error) { throw new WorkshopTransportException(identity, TransportState, error); }
            var encodedIdentity = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(encodedIdentity, sequence.Value);
            var response = WorkshopWire.DecodeResponse(Acknowledgement(_id.Value, encodedIdentity));
            return _cursor.Acknowledge(command, response, dispatchedReadOrder, _disposed);
        }
        finally { _pending--; if (Pending == identity) Pending = null; }
    }

    public Task<WorkshopDelivery> CancelPending()
    {
        var target = Pending ?? throw new InvalidOperationException("No pending operation to cancel.");
        return Execute(WorkshopCommandKind.Cancel, target: target);
    }

    public bool TryRead(out WorkshopResponse response)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var bytes = Read(_id.Value);
        if (bytes.Length == 0) { response = default; return false; }
        response = WorkshopWire.DecodeResponse(bytes);
        return _cursor.AcceptRead(response);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            var disposal = Execute(WorkshopCommandKind.Dispose);
            if (await Task.WhenAny(disposal, Task.Delay(1000)) == disposal) await disposal;
            else _ = disposal.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        }
        finally { _disposed = true; DisposeClient(_id.Value); }
    }
}
