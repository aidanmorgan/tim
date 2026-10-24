using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>
/// The Simulation run loop's real scheduling policy (WorkshopTickLoop, which the worker links) driven in virtual time.
/// The fake worker answers DebtExceeded and CaughtUp through the production WorkshopSchedule and WorkshopWorldProjection
/// rules; each tick, turn boundary and timer wake advances the virtual master clock by its modelled cost.
/// </summary>
public sealed class WorkshopTickLoopTests
{
    private const long Millisecond = 1_000_000;
    // engine-contracts.md §Clocks, histories and performance: at most four whole ticks per scheduler turn.
    private const int ContractTicksPerTurn = 4;
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();
    private static readonly long Period = 1_000_000_000 / (long)Settings.SimulationRate.Numerator;

    // cat-023b domino_effect on the production bundle, unloaded (evidence-9-4b/diagnosis/tick-cost): mean 4.3 ms, p95 6.4 ms.
    // A twenty-sample cycle with exactly that mean and p95, and a 12.4 ms outlier.
    private static readonly long[] Cat023bTickCost =
        [.. Enumerable.Repeat(3_400_000L, 16), 6_400_000, 6_400_000, 6_400_000, 12_400_000];

    private sealed class VirtualWorker(Func<ulong, long> tickCost, long competingWorkPerTurn = 0, long timerWake = 4 * Millisecond)
        : ITickLoopHost
    {
        private readonly WorkshopSchedule _schedule = WorkshopSchedule.Create(Settings, new(1), new(1), new(0), new(0),
            new(0), new(0), new(new(1), new(1), new(0), new(0), WorldPlayback.Running));
        private int _turnTicks;
        public long Now { get; set; }
        public ulong Committed { get; private set; }
        public List<int> Turns { get; } = [];
        public int DebtStops { get; private set; }
        public int Completions { get; private set; }
        public long MaximumDebt { get; private set; }
        public Queue<TickAttempt> Scripted { get; } = new();
        public long Debt => Now - (long)Committed * Period;

        public bool Owns => DebtStops == 0 && Completions == 0;
        /// <summary>The committed tick at which a command or installation owns the world once (the loop waits for it).</summary>
        public ulong? BlockAt { get; set; }
        public bool Blocked
        {
            get
            {
                if (BlockAt != Committed) return false;
                BlockAt = null; return true;
            }
        }
        public bool DebtExceeded
        {
            get
            {
                MaximumDebt = Math.Max(MaximumDebt, Debt);
                return _schedule.ExceedsPhysicalDebt(new(Committed), new(Now));
            }
        }
        public bool AtRunLimit => Committed == Settings.RunTickLimit;
        public bool CaughtUp => Committed >= _schedule.World.DueTick(Settings, new(Now)).Value;

        public ValueTask<TickAttempt> Advance()
        {
            if (Scripted.TryDequeue(out var scripted) && scripted != TickAttempt.Committed)
                return ValueTask.FromResult(scripted);
            Now += tickCost(Committed);
            Committed++;
            _turnTicks++;
            return ValueTask.FromResult(TickAttempt.Committed);
        }
        public ValueTask StopForDebt() { DebtStops++; return ValueTask.CompletedTask; }
        public ValueTask CompleteRun() { Completions++; return ValueTask.CompletedTask; }
        // A MessageChannel macrotask: the turn ends, queued competing work runs, then the loop resumes with no timer clamp.
        public ValueTask YieldTurn() { EndTurn(); Now += competingWorkPerTurn; return ValueTask.CompletedTask; }
        // Task.Delay(1) at the nested-timer clamp, behind the same competing work.
        public ValueTask AwaitWork() { EndTurn(); Now += competingWorkPerTurn + timerWake; return ValueTask.CompletedTask; }
        private void EndTurn() { Turns.Add(_turnTicks); _turnTicks = 0; }
    }

    private static long Cycle(long[] costs, ulong tick) => costs[(int)(tick % (ulong)costs.Length)];

    [Fact]
    public async Task NoSchedulerTurnExecutesMoreThanFourCommittedTicks()
    {
        // An 80 ms stall leaves a backlog that 5 ms ticks clear only through full batches.
        var worker = new VirtualWorker(tick => tick == 10 ? 80 * Millisecond : 5 * Millisecond);
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(1, worker.Completions);
        Assert.Equal(0, worker.DebtStops);
        Assert.All(worker.Turns, ticks => Assert.InRange(ticks, 0, ContractTicksPerTurn));
        Assert.Contains(ContractTicksPerTurn, worker.Turns);
    }

    [Fact]
    public async Task AWaitForWorkEndsTheTurnSoTheNextTurnRunsAFullBatch()
    {
        // Behind from the start and after a 60 ms first tick; after two ticks a command owns the world, so the loop waits.
        // (Story 9A.23 deleted the reliable-read reservation that was the other wait: every read is now the latest.)
        var worker = new VirtualWorker(tick => tick == 0 ? 60 * Millisecond : 5 * Millisecond) { Now = 20 * Millisecond, BlockAt = 2 };
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(2, worker.Turns[0]);
        Assert.Equal(ContractTicksPerTurn, worker.Turns[1]);
    }

    [Fact]
    public async Task SupersededAttemptsCountTowardTheBatchSoARunOfThemStillYields()
    {
        // Murdoch-2 M2 (Story 9.4b): a superseded attempt costs a whole tick of work, so a run of supersedes must still
        // end the turn every four attempts, or it would starve controls and the derivation/heartbeat timer.
        var worker = new VirtualWorker(tick => tick == 0 ? 60 * Millisecond : 5 * Millisecond) { Now = 20 * Millisecond };
        worker.Scripted.Enqueue(TickAttempt.Committed);
        for (var i = 0; i < 39; i++) worker.Scripted.Enqueue(TickAttempt.Superseded);
        await WorkshopTickLoop.Run(worker);
        Assert.Equal([1, 0, 0, 0, 0, 0, 0, 0, 0, 0], worker.Turns.Take(10));
        Assert.Equal(ContractTicksPerTurn, worker.Turns[10]);
    }

    [Fact]
    public async Task CatchUpAfterAnEightyMillisecondStallNeverDebtFaultsAtTheCat023bTickCost()
    {
        const ulong stallTick = 240;
        var stallStarted = 0L;
        var recoveredAt = 0L;
        var worker = new VirtualWorker(tick => tick == stallTick ? 80 * Millisecond : Cycle(Cat023bTickCost, tick));
        await WorkshopTickLoop.Run(new Observed(worker, before: () =>
        {
            if (worker.Committed == stallTick) stallStarted = worker.Now;
        }, after: () =>
        {
            if (recoveredAt == 0 && worker.Committed > stallTick + 1 && worker.CaughtUp) recoveredAt = worker.Now;
        }));
        Assert.Equal(0, worker.DebtStops);
        Assert.Equal(1, worker.Completions);
        Assert.True(worker.MaximumDebt < 100 * Millisecond, $"maximum debt {worker.MaximumDebt / 1e6:F1} ms");
        Assert.True(recoveredAt > 0 && recoveredAt - stallStarted < 400 * Millisecond,
            $"caught up {(recoveredAt - stallStarted) / 1e6:F1} ms after the stall began");
    }

    [Theory]
    [InlineData(1_000_000L)]
    [InlineData(3_200_000L)]
    public async Task UnderTheTwentyMillisecondInjectionASceneBelowTheTurnBoundKeepsUp(long tickCost)
    {
        // Twenty milliseconds of competing work at every turn boundary (tools/e2e/clock-master.test.ts): four ticks cover
        // 33.3 ms of master time, so the loop keeps up while 4c + 20 ms < 33.3 ms, that is c < 3.33 ms.
        var worker = new VirtualWorker(_ => tickCost, 20 * Millisecond);
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(0, worker.DebtStops);
        Assert.Equal(1, worker.Completions);
        Assert.True(worker.MaximumDebt < 100 * Millisecond, $"maximum debt {worker.MaximumDebt / 1e6:F1} ms");
    }

    [Theory]
    [InlineData(3_500_000L)]
    [InlineData(4_300_000L)]
    public async Task UnderTheTwentyMillisecondInjectionAHeavierSceneStopsWithTheContractedCapacityFault(long tickCost)
    {
        // engine-contracts §Clocks: more than 100 ms of debt stops at the last commit, at the first check past the bound
        // (a check precedes every tick, and a tick or a turn boundary is the most that runs between checks).
        var worker = new VirtualWorker(_ => tickCost, 20 * Millisecond);
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(1, worker.DebtStops);
        Assert.Equal(0, worker.Completions);
        Assert.InRange(worker.Debt, 100 * Millisecond + 1, 100 * Millisecond + 20 * Millisecond + 4 * Millisecond);
    }

    [Fact]
    public async Task ADebtAlreadyBeyondTheBoundStopsBeforeAnyTick()
    {
        var worker = new VirtualWorker(_ => Millisecond) { Now = 101 * Millisecond };
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(1, worker.DebtStops);
        Assert.Equal(0UL, worker.Committed);
    }

    [Fact]
    public async Task AStoppedAttemptEndsTheLoopWithoutAnotherTick()
    {
        var worker = new VirtualWorker(_ => Millisecond) { Now = 50 * Millisecond };
        worker.Scripted.Enqueue(TickAttempt.Committed);
        worker.Scripted.Enqueue(TickAttempt.Stopped);
        await WorkshopTickLoop.Run(worker);
        Assert.Equal(1UL, worker.Committed);
        Assert.Equal(0, worker.DebtStops + worker.Completions);
    }

    [Fact]
    public async Task AnUndefinedAttemptRejects()
    {
        var worker = new VirtualWorker(_ => Millisecond) { Now = 50 * Millisecond };
        worker.Scripted.Enqueue((TickAttempt)99);
        await Assert.ThrowsAsync<InvalidOperationException>(() => WorkshopTickLoop.Run(worker));
    }

    // Edge-case E2 (Story 9.4b final round): turn identities wrap instead of overflowing, and skip any still outstanding.
    [Fact]
    public void SchedulerTurnIdentitiesWrapPastIntMaxValueAndSkipOutstandingTurns()
    {
        var turns = new WorkshopTurnRegistry(int.MaxValue - 1);
        var (last, lastResumed) = turns.Begin();
        var (wrapped, wrappedResumed) = turns.Begin();
        Assert.Equal((int.MaxValue, 1), (last, wrapped));
        turns.Resume(last);
        Assert.True(lastResumed.IsCompletedSuccessfully);
        Assert.False(wrappedResumed.IsCompleted);
        var skipping = new WorkshopTurnRegistry(int.MaxValue);
        Assert.Equal(1, skipping.Begin().Turn);
        for (var i = 2; i < 5; i++) Assert.Equal(i, skipping.Begin().Turn);
        Assert.Throws<InvalidOperationException>(() => turns.Resume(last)); // already resumed
        Assert.Throws<InvalidOperationException>(() => turns.Resume(42)); // never requested
        Assert.Equal(1, turns.Outstanding);
    }

    /// <summary>Calls back around every tick attempt, for stall and recovery timing.</summary>
    private sealed class Observed(VirtualWorker inner, Action before, Action after) : ITickLoopHost
    {
        public bool Owns => inner.Owns;
        public bool Blocked => inner.Blocked;
        public bool DebtExceeded => inner.DebtExceeded;
        public bool AtRunLimit => inner.AtRunLimit;
        public bool CaughtUp => inner.CaughtUp;
        public async ValueTask<TickAttempt> Advance() { before(); var attempt = await inner.Advance(); after(); return attempt; }
        public ValueTask StopForDebt() => inner.StopForDebt();
        public ValueTask CompleteRun() => inner.CompleteRun();
        public ValueTask YieldTurn() => inner.YieldTurn();
        public ValueTask AwaitWork() => inner.AwaitWork();
    }
}
