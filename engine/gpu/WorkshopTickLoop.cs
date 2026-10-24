using System;
using System.Threading.Tasks;

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

/// <summary>
/// The Simulation run loop (engine-contracts §Clocks, histories and performance): it executes at most
/// <see cref="TicksPerTurn"/> whole ticks per scheduler turn and then yields for controls. Since Story 9.4b the
/// worker is no longer the clock master's thread, so the bound protects command and receipt latency, not probe
/// stamping. The yield is an immediate macrotask (no 4 ms timer clamp), so catch-up keeps whole-tick throughput:
/// a worker that can execute a tick faster than its period catches up after a hiccup without a debt fault.
/// </summary>
public static class WorkshopTickLoop
{
    public const int TicksPerTurn = 4;

    public static async Task Run(ITickLoopHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        // Tick attempts since the current scheduler turn began (committed or superseded: a superseded attempt costs a
        // whole tick of work too, so a run of supersedes still yields to controls and timers); every wait or yield ends
        // the turn.
        var batch = 0;
        while (host.Owns)
        {
            if (host.Blocked) { batch = 0; await host.AwaitWork(); continue; }
            if (host.DebtExceeded) { await host.StopForDebt(); return; }
            if (host.AtRunLimit) { await host.CompleteRun(); return; }
            if (host.CaughtUp) { batch = 0; await host.AwaitWork(); continue; }
            var attempt = await host.Advance();
            switch (attempt)
            {
                case TickAttempt.Stopped: return;
                case TickAttempt.Superseded: break;
                case TickAttempt.Committed: break;
                default: throw new InvalidOperationException($"Unknown tick attempt {attempt}.");
            }
            if (++batch == TicksPerTurn) { batch = 0; await host.YieldTurn(); }
        }
    }
}
