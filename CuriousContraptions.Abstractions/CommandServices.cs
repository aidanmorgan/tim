using System;
using System.Threading.Tasks;

namespace CuriousContraptions.Gpu;

/// <summary>Environment ownership around the single command implementation. Hosts never apply commands.</summary>
public interface IWorkshopCommandHost
{
    RuntimeSessionId Session { get; }
    WorkshopSchedule? Schedule { get; }
    ClockGeneration Generation { get; }
    bool InstallationPending { get; }
    bool ReadsIdle { get; }
    IWorkshopCaptureClock Clock { get; }
    IWorkshopInstallation Installation { get; }
    ValueTask Before(WorkshopCommandKind kind, IWorkshopSimulation? simulation);
    ValueTask After(WorkshopCommandKind kind, WorkshopCommandResult result, IWorkshopSimulation simulation);
    WorkshopMemoryMeasurement CaptureMemory();
}

/// <summary>A dispatched command's sequence (its acknowledgement's key, handed to the browser with the response so no script
/// parses a buffer) and its encoded response.</summary>
public readonly record struct WorkshopRoutedCommand(CommandSequence Sequence, Task<byte[]> Response);

public enum ReadReceiptRejection
{
    /// <summary>Not the 24-byte receipt identity (session, then publication) the worker published with its read.</summary>
    Malformed = 1,
    /// <summary>Another runtime session's receipt, or none.</summary>
    WrongSession,
    /// <summary>A publication this worker has not published.</summary>
    Unpublished,
    /// <summary>A publication an earlier receipt already retired: a duplicate or out-of-order receipt.</summary>
    Retired,
}

/// <summary>A refused physical read receipt. It retires nothing, and the worker reports it as a transport failure.</summary>
public sealed class ReadReceiptRejectedException : Exception
{
    public ReadReceiptRejectedException(ReadReceiptRejection reason, string message) : base(message)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined read receipt rejection.");
        Reason = reason;
    }

    public ReadReceiptRejection Reason { get; }
}

/// <summary>A published physical read the browser has not yet receipted, and when it was published on the master clock.</summary>
public readonly record struct UnreceiptedRead(PublicationSequence Publication, MasterTimeNanoseconds Published);

public enum WorkshopCaptureMode : uint { Production = 1, Diagnostic = 2 }

public static class WorkshopBuild
{
#if PLAYTEST
    public const WorkshopCaptureMode CaptureMode = WorkshopCaptureMode.Diagnostic;
#else
    public const WorkshopCaptureMode CaptureMode = WorkshopCaptureMode.Production;
#endif
}

public interface IWorkshopCommandExecutor : IAsyncDisposable
{
    IWorkshopSimulation Simulation { get; }
    WorkshopCommandIdentity? Pending { get; }
    Task<byte[]> Execute(WorkshopCommand command);
    byte[] Encode(WorkshopResponse response);
    WorkshopResponse Populate(WorkshopResponse response);
}

public interface IWorkshopCommandRouter
{
    WorkshopRoutedCommand Dispatch(byte[] bytes);
}

public interface IReliableReadLedger
{
    bool Idle { get; }
    UnreceiptedRead? Oldest { get; }
    void Publish(PublicationSequence publication, MasterTimeNanoseconds published);
    PublicationSequence Receipt(ReadOnlySpan<byte> receipt);
    bool Stalled(MasterTimeNanoseconds now);
    WorkshopRejection DebtReason(MasterTimeNanoseconds now);
}

public interface IWorkshopTurnRegistry
{
    int Outstanding { get; }
    (int Turn, Task Resumed) Begin();
    void ResumeTurn(int turn);
}

public interface IMemoryQueryOwner
{
    bool Active { get; }
    void AdmitCommand(WorkshopCommandKind kind);
    Task<WorkshopMemoryReply> Execute(int pending, Func<Task<WorkshopDelivery>> query, Func<bool> current, Func<WorkshopMemoryMeasurement> captureBrowser);
}

public interface IMemoryServices
{
    WorkshopMemoryMeasurement Capture();
    bool IsStopped(WorkshopSimulationPhase phase);
    WorkshopCommandResult Admit(WorkshopSimulationPhase phase, bool gpuOperationPending, bool readsReceipted);
    void ValidateCarriage(WorkshopCommandKind kind, WorkshopResponse acknowledgement);
    WorkshopMemoryReply Complete(WorkshopCommandKind kind, WorkshopResponse acknowledgement, bool applicable, Func<WorkshopMemoryMeasurement> captureBrowser);
    IMemoryQueryOwner CreateOwner();
}
