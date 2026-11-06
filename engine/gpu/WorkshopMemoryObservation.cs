using System;
using System.Diagnostics;

namespace CuriousContraptions.Gpu;

public static class WorkshopMemorySampler
{
    public static WorkshopMemoryMeasurement Capture()
    {
        var used = GC.GetTotalMemory(false);
        var collections = GC.CollectionCount(GC.MaxGeneration);
        var allocated = GC.GetTotalAllocatedBytes(precise: true);
        var started = Stopwatch.GetTimestamp();
        var after = GC.GetTotalMemory(true);
        var elapsed = checked(Stopwatch.GetTimestamp() - started);
        var measurement = new WorkshopMemoryMeasurement(used, after, elapsed, Stopwatch.Frequency,
            allocated, GC.GetTotalAllocatedBytes(precise: true), collections, GC.CollectionCount(GC.MaxGeneration));
        measurement.Validate();
        return measurement;
    }
}

/// <summary>
/// The stopped memory observation query (Story 9A.3): a typed command, <see cref="WorkshopCommandKind.ObserveMemory"/>, routed with
/// its sequence like every other command and answered by its acknowledgement, which carries the Simulation worker's measurement.
/// The query is admitted only while the world is stopped and the transport is quiet, so its blocking collection never delays a tick.
/// </summary>
public static class WorkshopMemoryQuery
{
    public static bool IsStopped(WorkshopSimulationPhase phase) =>
        phase is WorkshopSimulationPhase.Building or WorkshopSimulationPhase.Completed or WorkshopSimulationPhase.Faulted;

    /// <summary>The worker's admission: a stopped phase, no GPU operation in progress and every published read receipted.</summary>
    public static WorkshopCommandResult Admit(WorkshopSimulationPhase phase, bool gpuOperationPending, bool readsReceipted) =>
        !IsStopped(phase) ? new(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase)
        : gpuOperationPending || !readsReceipted ? new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy)
        : new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);

    /// <summary>A measurement rides only on the acknowledgement of an applied memory query, and that acknowledgement always has one.</summary>
    public static void ValidateCarriage(WorkshopCommandKind kind, WorkshopResponse acknowledgement)
    {
        var expected = kind == WorkshopCommandKind.ObserveMemory && acknowledgement.Result.Outcome == WorkshopCommandOutcome.Applied;
        if (expected != acknowledgement.Memory.HasValue)
            throw new ArgumentException(expected
                ? "An applied memory query acknowledgement has no measurement."
                : $"A {kind} acknowledgement carries a memory measurement.");
    }

    /// <summary>Pairs an applied query's Simulation measurement with the browser's own, captured after it; a refused query captures
    /// nothing.</summary>
    public static WorkshopMemoryReply Complete(WorkshopCommandKind kind, WorkshopResponse acknowledgement,
        bool applicable, Func<WorkshopMemoryMeasurement> captureBrowser)
    {
        ArgumentNullException.ThrowIfNull(captureBrowser);
        ValidateCarriage(kind, acknowledgement);
        if (!applicable) return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), null);
        return acknowledgement.Memory is { } simulation
            ? new(acknowledgement.Result, new(captureBrowser(), simulation))
            : new(acknowledgement.Result, null);
    }
}

/// <summary>Owns the browser's stopped interval through the query acknowledgement and synchronous collector capture.</summary>
public sealed class WorkshopMemoryQueryOwner : IMemoryQueryOwner
{
    public bool Active { get; private set; }

    public void AdmitCommand(WorkshopCommandKind kind)
    {
        if (Active && kind != WorkshopCommandKind.Dispose)
            throw new InvalidOperationException("A stopped memory observation owns the client.");
    }

    public async System.Threading.Tasks.Task<WorkshopMemoryReply> Execute(int pending,
        Func<System.Threading.Tasks.Task<WorkshopDelivery>> query, Func<bool> current,
        Func<WorkshopMemoryMeasurement> captureBrowser)
    {
        if (Active || pending != 0) return new(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy), null);
        Active = true;
        try
        {
            var delivery = await query();
            return WorkshopMemoryQuery.Complete(WorkshopCommandKind.ObserveMemory, delivery.Response,
                delivery.Applicable && current(), captureBrowser);
        }
        finally { Active = false; }
    }
}
