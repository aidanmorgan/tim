using System.Threading.Tasks;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>What one attempt to advance the committed world produced.</summary>
public enum TickAttempt { Committed, Superseded, Stopped }

/// <summary>The Simulation worker's side effects the run loop schedules; the worker supplies them, tests fake them.</summary>
public interface ITickLoopHost
{
    /// <summary>This loop still owns a Running world.</summary>
    bool Owns { get; }
    /// <summary>A command, GPU operation or installation owns the world; ticks wait for it.</summary>
    bool Blocked { get; }
    /// <summary>Simulation debt beyond the 100 ms bound (or an unresolved reliable stall): the explicit stopped fault.</summary>
    bool DebtExceeded { get; }
    bool AtRunLimit { get; }
    /// <summary>The committed tick has reached the tick the master clock says is due.</summary>
    bool CaughtUp { get; }
    ValueTask<TickAttempt> Advance();
    ValueTask StopForDebt();
    ValueTask CompleteRun();
    /// <summary>Ends this scheduler turn so controls and queued messages run, then resumes without a timer clamp.</summary>
    ValueTask YieldTurn();
    /// <summary>Waits for work (a due tick or a released block) on the ordinary wake-up timer.</summary>
    ValueTask AwaitWork();
}

public interface IWorkshopWorkerDevice : IWorkshopGpuDevice
{
    Task Prepare();
    byte[] DrainEvents();
#if PLAYTEST
    byte[] DiagnosticCommitted { get; }
#endif
}

public interface IWorkshopTrace
{
    IWorkshopDiagnostics Diagnostics { get; }
    void Begin(RuntimeSessionId session, ICompiledScene construction, SimulationEpoch epoch, ProjectionEpoch projection, WorkshopGpuProfile profile);
#if PLAYTEST
    void Append(byte[] record);
#endif
    void RecordTickDuration(SimulationTick tick, double milliseconds);
    void FinishCapture(TraceEnd reason);
    bool CanFlush(WorkshopSimulationPhase phase);
    void Flush(WorkshopSimulationPhase phase);
}
