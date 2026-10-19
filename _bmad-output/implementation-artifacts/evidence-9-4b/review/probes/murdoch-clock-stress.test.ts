// Murdoch-2 Story 9.4b clock-stress probe (adversarial; not the implementer's clock-master.test.ts).
// Injects 20 ms and 40 ms synchronous duty cycles, single 600 ms (and 950 ms boundary) blocks, and CPU oversubscription
// into each context (Simulation worker, Animation worker, Browser main thread), in Build and while Running. Asserts:
//   - zero session terminations (transport failure, clock fault, worker fault, page error, worker close) in every
//     scenario except the INFO liveness edge;
//   - zero legacy CCGPU_CLOCK_RECOVERY_FAULT lines (the fault path must be gone);
//   - zero recalibrations when only the Simulation worker is loaded (the clock master is off its event loop; a revert of
//     the master onto the Simulation thread is caught here even though recalibration no longer terminates);
//   - the session requalifies and works afterwards (pose ring readable, Run publishes, Reset acknowledged, goal solves).
// Recalibrations under Browser/Animation load are counted and printed as MURDOCH_CLOCK JSON lines, not failed, because a
// recalibration is the declared continue path for a lapse of that requester's own event loop.
// Filter: MURDOCH_CLOCK_FILTER=<regex on scenario name>. Run serially (one Chrome, one preview on :8060).
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';
import {
    PREVIEW_URL, SessionWatch, type ContextTarget, startDutyCycle, stopDutyCycle, blockOnce, startHogs, stopHogs,
    hardwareConcurrency, masterContextVisible, buildWiredChain, waitUntil, sleep,
} from './murdoch-common.ts';

type Mode = 'build' | 'run-ball' | 'run-chain';
type Load =
    | { kind: 'duty'; target: ContextTarget; busyMs: number; idleMs?: number; durationMs: number }
    | { kind: 'block'; target: ContextTarget; ms: number }
    | { kind: 'hogs'; perCore: number; durationMs: number };
interface Scenario {
    name: string; mode: Mode; load: Load;
    /** INFO scenarios record a declared limit; they never fail. */
    info?: boolean;
    /** A Simulation-thread stall while Running may stop the Run with the declared physics-debt bound (Capacity). */
    allowCapacityDebt?: boolean;
}

const duty = (target: ContextTarget, busyMs: number, idleMs = 0, durationMs = 3000): Load => ({ kind: 'duty', target, busyMs, idleMs, durationMs });
const block = (target: ContextTarget, ms: number): Load => ({ kind: 'block', target, ms });

const SCENARIOS: Scenario[] = [
    // Build: no tick debt exists, so every failure here is a clock/transport failure.
    { name: 'B-sim-duty20', mode: 'build', load: duty('simulation', 20) },
    { name: 'B-sim-duty40', mode: 'build', load: duty('simulation', 40) },
    { name: 'B-sim-block600', mode: 'build', load: block('simulation', 600) },
    { name: 'B-anim-duty20', mode: 'build', load: duty('animation', 20) },
    { name: 'B-anim-duty40', mode: 'build', load: duty('animation', 40) },
    { name: 'B-anim-block600', mode: 'build', load: block('animation', 600) },
    { name: 'B-anim-block700', mode: 'build', load: block('animation', 700) },
    { name: 'B-main-duty20', mode: 'build', load: duty('main', 20) },
    { name: 'B-main-duty40', mode: 'build', load: duty('main', 40) },
    { name: 'B-main-block600', mode: 'build', load: block('main', 600) },
    { name: 'B-main-block700', mode: 'build', load: block('main', 700) },
    { name: 'B-hogs-2x', mode: 'build', load: { kind: 'hogs', perCore: 2, durationMs: 5000 } },
    // Running the heaviest measured solver scene (cat-023b chain) while another context is loaded.
    { name: 'R-chain-main-duty20', mode: 'run-chain', load: duty('main', 20) },
    { name: 'R-chain-main-duty40', mode: 'run-chain', load: duty('main', 40) },
    { name: 'R-chain-main-block600', mode: 'run-chain', load: block('main', 600) },
    { name: 'R-chain-anim-duty20', mode: 'run-chain', load: duty('animation', 20) },
    { name: 'R-chain-anim-duty40', mode: 'run-chain', load: duty('animation', 40) },
    { name: 'R-chain-anim-block600', mode: 'run-chain', load: block('animation', 600) },
    // 2x CPU oversubscription may legitimately exceed the 100 ms physics debt: the contracted Capacity stop is allowed,
    // a clock/transport termination is not.
    { name: 'R-chain-hogs-2x', mode: 'run-chain', load: { kind: 'hogs', perCore: 2, durationMs: 5000 }, allowCapacityDebt: true },
    // D13 heartbeat boundary: one beat per 100 ms check turn, fault after 1000 ms without a beat. An 800 ms Simulation
    // block (Build, no debt) must survive; 1100 ms must raise the typed SimulationUnresponsive failure (INFO records it).
    { name: 'B-sim-block800', mode: 'build', load: block('simulation', 800) },
    { name: 'INFO-sim-block1100', mode: 'build', load: block('simulation', 1100), info: true },
    // Simulation loaded while Running: a 50% duty keeps tick debt under 100 ms; a 600 ms stall exceeds it by contract.
    { name: 'R-ball-sim-duty20-idle20', mode: 'run-ball', load: duty('simulation', 20, 20) },
    { name: 'R-chain-sim-duty20', mode: 'run-chain', load: duty('simulation', 20), allowCapacityDebt: true },
    { name: 'R-ball-sim-block600', mode: 'run-ball', load: block('simulation', 600), allowCapacityDebt: true },
    // Declared 1000 ms reply-liveness edge, measured from the last ACCEPTED reply (refresh every 250 ms), so the survivable
    // block is 1000 ms minus up to 250 ms of reply age: 700 ms above must survive; 950/1100 ms are recorded only.
    { name: 'INFO-main-block950', mode: 'build', load: block('main', 950), info: true },
    { name: 'INFO-anim-block950', mode: 'build', load: block('animation', 950), info: true },
    { name: 'INFO-main-block1100', mode: 'build', load: block('main', 1100), info: true },
    { name: 'INFO-anim-block1100', mode: 'build', load: block('animation', 1100), info: true },
];

const filter = process.env.MURDOCH_CLOCK_FILTER ? new RegExp(process.env.MURDOCH_CLOCK_FILTER) : undefined;

async function prepare(driver: WorkshopDriver, mode: Mode): Promise<void> {
    if (mode === 'run-chain') { await buildWiredChain(driver); await driver.run(); }
    else if (mode === 'run-ball') { await driver.selectTool('basketball'); await driver.placeOnCanvas(720, 485); await driver.run(); }
}

async function applyLoad(driver: WorkshopDriver, load: Load): Promise<void> {
    if (load.kind === 'duty') {
        await startDutyCycle(driver, load.target, load.busyMs, load.idleMs ?? 0);
        try { await sleep(driver, load.durationMs); } finally { await stopDutyCycle(driver, load.target); }
    } else if (load.kind === 'block') {
        await blockOnce(driver, load.target, load.ms);
    } else {
        await startHogs(driver, load.perCore * await hardwareConcurrency(driver));
        try { await sleep(driver, load.durationMs); } finally { await stopHogs(driver); }
    }
}

// After the load: the mappings requalify, the pose ring still serves, and an ordinary Run/Reset (and the chain goal) works.
async function proveAlive(driver: WorkshopDriver, watch: SessionWatch, mode: Mode, solvedBefore: number): Promise<string[]> {
    const problems: string[] = [];
    const qualified = await waitUntil(async () => await driver.page.evaluate(() =>
        globalThis.WorkshopPoseRing?.hasPoseRing(1) === true && globalThis.WorkshopAnimation?.isAnimationQualified(1) === true), 3000);
    if (!qualified) problems.push('pose ring / Animation qualification not restored within 3 s of the load ending');
    if (mode === 'run-chain' && watch.debtFaults.length === 0) {
        const solved = await waitUntil(() => watch.solved.length > solvedBefore, 15000);
        if (!solved) problems.push('the running chain never printed CCGOAL_SOLVED after the load');
    }
    try {
        if (mode !== 'build') await driver.reset(500);
        if (mode === 'build') { await driver.selectTool('basketball'); await driver.placeOnCanvas(720, 485); }
        await driver.run(1000);
        if (!await driver.readLatestPose()) problems.push('Run after the load published no readable pose');
        await driver.reset(300);
    } catch (error) { problems.push('Run/Reset after the load failed: ' + (error instanceof Error ? error.message : String(error))); }
    return problems;
}

for (const scenario of SCENARIOS) {
    const skip = filter !== undefined && !filter.test(scenario.name);
    test(`Murdoch clock stress ${scenario.name}`, { timeout: 180000, skip }, async () => {
        // This probe deliberately starves requester contexts and asserts the typed recalibration records itself.
        const driver = await WorkshopDriver.launch({ baseUrl: PREVIEW_URL, allowClockRecalibrations: true });
        const watch = new SessionWatch(driver);
        let alive: string[] = [];
        try {
            await prepare(driver, scenario.mode);
            const solvedBefore = watch.solved.length;
            const baselineRecalibrations = watch.recalibrations.length;
            watch.mark();
            await applyLoad(driver, scenario.load);
            await sleep(driver, 1500); // let a lapsed mapping recalibrate and requalify before judging liveness
            alive = await proveAlive(driver, watch, scenario.mode, solvedBefore);
            const summary = {
                scenario: scenario.name, masterContextVisible: masterContextVisible(driver),
                recalibrationsBeforeLoad: baselineRecalibrations, alive, ...watch.summary(),
            };
            console.log('MURDOCH_CLOCK ' + JSON.stringify(summary));
            if (scenario.info) return;

            assert.deepEqual(watch.terminations.map(t => t.text.slice(0, 200)), [], `${scenario.name}: zero session terminations`);
            assert.deepEqual(watch.closedWorkers, [], `${scenario.name}: no game worker closed`);
            assert.deepEqual(watch.legacyClockFaults, [], `${scenario.name}: the clock-fault path is gone`);
            assert.equal(baselineRecalibrations, 0, `${scenario.name}: no recalibration in the post-ready idle before the load (SessionWatch attaches after launch's ready wait)`);
            assert.equal(watch.recalibrations.filter(r => r.role === null).length, 0, `${scenario.name}: every recalibration is a parsable typed record`);
            if (scenario.load.kind !== 'hogs' && scenario.load.target === 'simulation')
                assert.equal(watch.recalibrations.length, 0,
                    `${scenario.name}: loading only the Simulation worker must not lapse any mapping (master is off-thread): ${watch.recalibrations.map(r => r.raw.slice(0, 120)).join(' | ')}`);
            // A ReliableStalled stop is the raw C2 flag path (receipts pending > 500 ms). AC5 makes it a 9.4b obligation only
            // if it caused cat-023b, so it is classified and printed for Story 9A.3, never asserted here.
            const stalls = watch.debtFaults.filter(d => d.reliableStalled);
            if (stalls.length) console.log('MURDOCH_C2_STALL_STOP ' + JSON.stringify({ scenario: scenario.name, stalls }));
            const capacity = watch.debtFaults.filter(d => !d.reliableStalled);
            if (!scenario.allowCapacityDebt) assert.deepEqual(capacity, [], `${scenario.name}: no physics-debt (Capacity) fault`);
            assert.equal(watch.tickFailures.length, 0, `${scenario.name}: no failed tick`);
            assert.deepEqual(alive, [], `${scenario.name}: the session works after the load`);
        } finally { await driver.close(); }
    });
}
