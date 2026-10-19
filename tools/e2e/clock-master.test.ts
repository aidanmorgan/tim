// Story 9.4b R5 regression: clock probes are answered while the Simulation worker is busy.
// A 20 ms synchronous duty cycle is injected into the Simulation worker (yielding through MessageChannel, so no
// timer clamp hides it). Before the off-thread clock master, every Browser and Animation probe queued behind the
// busy loop, recovery exhausted its eight probes and the session failed within about 0.5 s. Now the probes are
// answered by the Simulation-owned clock context the worker's own work never blocks: no transport failure, no
// clock recalibration, and the Workshop keeps running afterwards.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import type { Worker } from 'playwright';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

const URL = process.env.CLOCK_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
// The retired-session screenshot is evidence, never a repository-root file (Godot imports any root image).
const SCREENSHOT = process.env.CLOCK_EVIDENCE_SCREENSHOT ??
    fileURLToPath(new globalThis.URL('../../_bmad-output/implementation-artifacts/evidence-9-4b/simulation-unresponsive.png', import.meta.url));
const DutyMilliseconds = 20;
const InjectionMilliseconds = 3000;

function simulationWorker(driver: WorkshopDriver): Worker {
    const worker = driver.page.workers().find(candidate => candidate.url().includes('/simulation/worker.js'));
    assert.ok(worker, 'The Simulation worker is running');
    return worker;
}

function workerAt(driver: WorkshopDriver, path: string): Worker {
    const worker = driver.page.workers().find(candidate => candidate.url().includes(path));
    assert.ok(worker, `The ${path} worker is running`);
    return worker;
}
const TILE_Y = 676, tileScreenX = (x: number) => Math.round(720 + 65.2 * x); // the cat-023b domino_effect recipe
const LAMP_TARGET = '5', HALF_ONE = 15360;

async function injectDutyCycle(worker: Worker, milliseconds: number): Promise<void> {
    await worker.evaluate(duty => {
        const channel = new MessageChannel();
        let stopped = false;
        channel.port1.onmessage = () => {
            if (stopped) return;
            const end = performance.now() + duty;
            while (performance.now() < end) { /* injected synchronous solver-like work */ }
            channel.port2.postMessage(0);
        };
        (globalThis as unknown as { ccStopInjection: () => void }).ccStopInjection = () => { stopped = true; channel.port1.close(); };
        channel.port2.postMessage(0);
    }, milliseconds);
}

async function stopDutyCycle(worker: Worker): Promise<void> {
    try { await worker.evaluate(() => (globalThis as unknown as { ccStopInjection?: () => void }).ccStopInjection?.()); }
    catch { /* a failed session has already terminated the worker; the assertions below report that failure */ }
}

// One synchronous block of the Simulation worker's event loop, started from a macrotask so evaluate returns first.
async function blockSimulation(worker: Worker, milliseconds: number): Promise<void> {
    await worker.evaluate(block => {
        setTimeout(() => {
            const end = performance.now() + block;
            while (performance.now() < end) { /* injected solver spin */ }
        }, 0);
    }, milliseconds);
}

// Murdoch-2 H2: the Simulation's typed derivation counters, recorded in every clock-master evidence line.
interface DerivationRecord { reportName: string; outcomeName: string; epoch: number; qualified: number; startupAttempts: number;
    startupInconclusive: number; startupUnanswered: number; checks: number; slews: number; rederivations: number;
    inconclusive: number; unanswered: number; checkRetries: number }
function derivationEvidence(driver: WorkshopDriver) {
    const records = driver.clockDerivations.map(line => JSON.parse(line.slice('CCGPU_CLOCK_DERIVATION '.length)) as DerivationRecord);
    const startup = records.find(record => record.reportName === 'Startup');
    assert.ok(startup, 'the Simulation reported its typed startup derivation counters');
    assert.equal(startup.qualified, 16, 'sixteen narrow consistent startup exchanges qualified the master');
    // Per-check records report only re-derivations and the first slewed, inconclusive or unanswered check of each run,
    // and a Summary arrives every 100 checks (10 s). An empty checkReports list therefore means every running check of
    // the session so far was consistent; the Summary carries the counters when the session lasted long enough.
    const summary = records.filter(record => record.reportName === 'Summary').at(-1);
    return { startup: { attempts: startup.startupAttempts, inconclusive: startup.startupInconclusive, unanswered: startup.startupUnanswered },
        checkReports: records.filter(record => record.reportName === 'Check').map(record => record.outcomeName),
        summary: summary ? { checks: summary.checks, slews: summary.slews, rederivations: summary.rederivations,
            inconclusive: summary.inconclusive, unanswered: summary.unanswered, checkRetries: summary.checkRetries } : null };
}

test('Clock probes are answered while the Simulation worker runs a 20 ms synchronous duty cycle (Build)', { timeout: 120000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    const failures: { at: number; text: string }[] = [];
    const recalibrations: string[] = [];
    let injectedAt = 0;
    driver.page.on('console', message => {
        const text = message.text();
        if (text.startsWith('CCGPU_CLOCK_RECALIBRATED')) recalibrations.push(text);
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE')) failures.push({ at: Date.now() - injectedAt, text });
    });
    driver.page.on('pageerror', error => failures.push({ at: Date.now() - injectedAt, text: error.message }));
    try {
        const worker = simulationWorker(driver);
        injectedAt = Date.now();
        await injectDutyCycle(worker, DutyMilliseconds);
        try { await driver.page.waitForTimeout(InjectionMilliseconds); }
        finally { await stopDutyCycle(worker); }
        console.log(JSON.stringify({ firstFailureMs: failures[0]?.at ?? null, failures: failures.length, recalibrations: recalibrations.length,
            derivation: derivationEvidence(driver) }));
        assert.deepEqual(failures.map(f => f.text.slice(0, 160)), [], 'No transport failure while the Simulation worker is busy');
        assert.equal(recalibrations.length, 0, 'No clock recalibration while the Simulation worker is busy');
        // The session still works: a placed ball runs, publishes poses and resets.
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(1000);
        assert.ok(await driver.readLatestPose(), 'Run publishes poses after the injected load');
        await driver.reset();
        assert.deepEqual(failures.map(f => f.text.slice(0, 160)), []);
        assert.equal(recalibrations.length, 0);
    } finally { await driver.close(); }
});

// The same injection while a light scene Runs. The run loop executes at most four ticks per turn and yields through a
// MessageChannel macrotask (WorkshopTickLoop), so a tick cheaper than 3.33 ms keeps up with the 20 ms of competing
// work per turn (WorkshopTickLoopTests): no Capacity debt stop, no tick failure, no clock fault, poses keep publishing.
test('A running Basketball keeps up while the Simulation worker runs a 20 ms synchronous duty cycle (Running)', { timeout: 120000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    const failures: string[] = [];
    const recalibrations: string[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (text.startsWith('CCGPU_CLOCK_RECALIBRATED')) recalibrations.push(text);
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE') || text.startsWith('CCGPU_DEBT_FAULT') ||
            text.startsWith('CCGPU_TICK_FAILED')) failures.push(text.slice(0, 160));
    });
    driver.page.on('pageerror', error => failures.push(error.message.slice(0, 160)));
    try {
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(0);
        const worker = simulationWorker(driver);
        const timestamps: bigint[] = [];
        await injectDutyCycle(worker, DutyMilliseconds);
        try {
            for (let sample = 0; sample < InjectionMilliseconds / 250; sample++) {
                await driver.page.waitForTimeout(250);
                const pose = await driver.readLatestPose();
                assert.ok(pose, `sample ${sample}: the pose ring stays readable under the injected load`);
                timestamps.push(BigInt(pose.timestamp));
            }
        } finally { await stopDutyCycle(worker); }
        console.log(JSON.stringify({ samples: timestamps.length, failures: failures.length, recalibrations: recalibrations.length,
            spanMs: Number(timestamps[timestamps.length - 1] - timestamps[0]) / 1e6, derivation: derivationEvidence(driver) }));
        assert.deepEqual(failures, [], 'No debt stop, tick failure or transport failure while Running under the injected load');
        assert.equal(recalibrations.length, 0, 'No clock recalibration while Running under the injected load');
        for (let i = 1; i < timestamps.length; i++)
            assert.ok(timestamps[i] > timestamps[i - 1], `publication continues between samples ${i - 1} and ${i}`);
        await driver.reset();
        assert.deepEqual(failures, []);
    } finally { await driver.close(); }
});

// Reviewer probe R-chain-anim-duty20 (Story 9.4b): loading the Animation worker lapses its own clock mapping. The lapse
// is a counted recalibration of that requester only, and every animation control that arrives meanwhile waits for
// requalification instead of faulting the session: the running chain still lights its lamp and solves.
test('A running wired chain survives a 20 ms duty cycle on the Animation worker (Running)', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL, allowClockRecalibrations: true });
    const failures: string[] = [];
    const deferrals: { cause: number; causeName: string }[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (text.startsWith('CCGPU_ANIMATION_CONTROL_DEFERRED ')) deferrals.push(JSON.parse(text.slice('CCGPU_ANIMATION_CONTROL_DEFERRED '.length)));
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE') || text.startsWith('CCGPU_DEBT_FAULT') ||
            text.startsWith('CCGPU_TICK_FAILED')) failures.push(text.slice(0, 160));
    });
    driver.page.on('pageerror', error => failures.push(error.message.slice(0, 160)));
    try {
        await driver.selectLevel('domino_effect');
        for (const x of [-4, -3, -2, -1]) { await driver.selectTool('domino'); await driver.placeOnCanvas(tileScreenX(x), TILE_Y); }
        await driver.connectActivation({ x: tileScreenX(0.1), y: TILE_Y, panel: 'locked' }, { x: 915, y: 612 });
        await driver.run(0);
        const animation = workerAt(driver, '/animation/worker.js');
        await injectDutyCycle(animation, DutyMilliseconds);
        try { await driver.page.waitForTimeout(InjectionMilliseconds); } finally { await stopDutyCycle(animation); }
        if (driver.capturedCount === 0) await driver.waitForGoalSolved(15000);
        const solved = driver.capturedCount;
        let lit = false;
        for (const deadline = Date.now() + 3000; !lit && Date.now() < deadline; await driver.page.waitForTimeout(50))
            lit = (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === HALF_ONE;
        const roles = driver.clockRecalibrations.map(line => JSON.parse(line.slice('CCGPU_CLOCK_RECALIBRATED '.length)).role);
        const recalibrating = deferrals.filter(deferral => deferral.causeName === 'Recalibrating');
        // Evidence only (Story 9.4b round 3): each recalibration's cause and prior state, and any late renewal.
        const recalibrations = driver.clockRecalibrations.map(line => JSON.parse(line.slice('CCGPU_CLOCK_RECALIBRATED '.length)))
            .map(record => ({ role: record.role, causeName: record.causeName, stateBefore: record.stateBefore }));
        console.log(JSON.stringify({ solved, lit, failures: failures.length, recalibrationRoles: roles, recalibrations, lateRenewals: driver.clockLateRenewals.length, deferrals }));
        assert.deepEqual(failures, [], 'No session failure while the Animation worker is loaded');
        assert.ok(roles.every(role => role === 3), 'Only the loaded Animation requester recalibrates');
        // Verification gap V4: the load actually exercised the path this test is about. The Animation mapping
        // recalibrated at least once, and at least one control was held for it and then applied (the lamp lights).
        assert.ok(roles.length >= 1, 'The loaded Animation requester recalibrated at least once');
        assert.ok(recalibrating.length >= 1, `At least one control was deferred while the mapping recalibrated (${JSON.stringify(deferrals)})`);
        assert.equal(solved, 1, 'The chain still solves');
        assert.ok(lit, 'The lamp still lights after the deferred controls applied');
        await driver.reset();
        assert.deepEqual(failures, []);
    } finally { await driver.close(); }
});

// Murdoch-2 H1 (Story 9.4b): the clock master keeps answering when the Simulation's own event loop hangs, so clock
// replies cannot prove the Simulation alive; its ring heartbeat (one beat per 100 ms check turn) does. A 600 ms block
// (reviewer probe B-sim-block600) stays within the 1000 ms liveness bound and the session continues. A solver spin
// longer than the bound faults the session explicitly: one typed SimulationUnresponsive transport failure, and the
// Workshop shows the retired session.
test('A Simulation spin beyond the liveness bound faults the session as SimulationUnresponsive; a 600 ms block does not', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    const failures: { at: number; text: string }[] = [];
    let spunAt = 0;
    driver.page.on('console', message => {
        const text = message.text();
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE')) failures.push({ at: Date.now() - spunAt, text });
    });
    driver.page.on('pageerror', error => failures.push({ at: Date.now() - spunAt, text: 'pageerror ' + error.message }));
    try {
        // In Build (a Running world would take the contracted 100 ms Capacity debt stop after a 600 ms block).
        const worker = simulationWorker(driver);
        await blockSimulation(worker, 600);
        await driver.page.waitForTimeout(2000);
        assert.deepEqual(failures.map(f => f.text.slice(0, 160)), [], 'a 600 ms Simulation block stays within the liveness bound');
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(0);
        assert.ok(await driver.readLatestPose(), 'the session continues and Runs after the 600 ms block');
        spunAt = Date.now();
        const spinMs = 4000;
        await blockSimulation(worker, spinMs);
        for (const deadline = Date.now() + 6000; failures.length === 0 && Date.now() < deadline;) await driver.page.waitForTimeout(25);
        await driver.page.waitForTimeout(1500); // the retired session reaches RecoveryBlocked on screen
        await driver.page.screenshot({ path: SCREENSHOT });
        const notice = await driver.page.locator('#status-notice[role="alert"]').textContent();
        assert.equal(notice, "The Simulation worker's event loop has not run for 1000 ms.", 'the player sees the explicit fault');
        assert.ok(await driver.page.locator('#status-notice').isVisible());
        console.log(JSON.stringify({ firstFailureMs: failures[0]?.at ?? null, failures: failures.map(f => f.text.slice(0, 200)),
            derivation: derivationEvidence(driver) }));
        // One transport failure, and the page's failure notice (index.html displayFailureNotice logs the same error
        // it shows the player); nothing else, in particular no engine-loop exception from the retired session.
        assert.equal(failures.filter(f => f.text.includes('CCGPU_TRANSPORT_FAILURE')).length, 1, 'exactly one transport failure');
        assert.match(failures[0].text, /CCGPU_TRANSPORT_FAILURE/);
        assert.ok(failures.every(f => f.text.startsWith('CCGPU_TRANSPORT_FAILURE 3 SimulationUnresponsive: ') ||
            f.text.startsWith('SimulationUnresponsive: ')), 'every reported failure is the typed SimulationUnresponsive fault');
        // The last beat precedes the spin by at most one 100 ms check interval, so the fault cannot come before the
        // bound; load only delays it. Its cause is the spin itself: it is reported while the Simulation is still
        // spinning (blind hunter 13: a fixed 2000 ms upper edge measured the host's load, not the liveness bound).
        assert.ok(failures[0].at >= 850 && failures[0].at < spinMs,
            `the fault follows the 1000 ms liveness bound during the ${spinMs} ms spin, not a clock lapse (${failures[0].at} ms)`);
    } finally { await driver.close(); }
});

// Murdoch-2 H2/M1 (Story 9.4b): the running derivation check is live, not inert. Skewing the Simulation's own native
// reading by +0.05 ms (beyond the 0.0158 ms bound, below the 0.1 ms re-derivation threshold) is slewed back at most
// 0.005 ms per check under the same epoch, with no requester recalibration; a further +1 ms skew re-derives under a new
// epoch and each requester mapping recalibrates once as counted MasterRederived telemetry, and the session keeps running.
test('A skewed Simulation clock is slewed back under the same epoch; a large skew re-derives and requesters recalibrate', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL, allowClockRecalibrations: true });
    const failures: string[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE')) failures.push(text.slice(0, 160));
    });
    driver.page.on('pageerror', error => failures.push(error.message.slice(0, 160)));
    const records = () => driver.clockDerivations.map(line => JSON.parse(line.slice('CCGPU_CLOCK_DERIVATION '.length)) as DerivationRecord);
    const skew = (milliseconds: number) => simulationWorker(driver).evaluate(offset => {
        const read = performance.now.bind(performance);
        performance.now = () => read() + offset;
    }, milliseconds);
    try {
        await skew(0.05);
        let summary: DerivationRecord | undefined;
        for (const deadline = Date.now() + 20000; !summary && Date.now() < deadline; await driver.page.waitForTimeout(100))
            summary = records().find(record => record.reportName === 'Summary');
        assert.ok(summary, 'a typed derivation summary arrives within 100 checks');
        const slewed = records().filter(record => record.outcomeName === 'Slewed');
        console.log(JSON.stringify({ slewedRecords: slewed.length, summary, recalibrations: driver.clockRecalibrations.length }));
        assert.ok(slewed.length >= 1 && slewed.every(record => record.epoch === 0), 'the skew is slewed under the unchanged epoch');
        assert.ok(summary.slews >= 3 && summary.slews <= 12, `slewed back in bounded 5 us steps (${summary.slews})`);
        assert.equal(summary.rederivations, 0);
        assert.equal(driver.clockRecalibrations.length, 0, 'no requester mapping recalibrated for a slew');
        await skew(1);
        for (const deadline = Date.now() + 5000; driver.clockRecalibrations.length < 2 && Date.now() < deadline;) await driver.page.waitForTimeout(50);
        const rederived = records().filter(record => record.outcomeName === 'Rederived');
        const recalibrated = driver.clockRecalibrations.map(line => JSON.parse(line.slice('CCGPU_CLOCK_RECALIBRATED '.length)) as { role: number; causeName: string });
        console.log(JSON.stringify({ rederived: rederived.map(record => ({ epoch: record.epoch, rederivations: record.rederivations })), recalibrated }));
        assert.equal(rederived.length, 1, 'one re-derivation');
        assert.deepEqual(recalibrated.map(item => item.causeName), ['MasterRederived', 'MasterRederived']);
        assert.deepEqual(recalibrated.map(item => item.role).sort(), [1, 3], 'the Browser and the Animation requester each recalibrate once');
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(1000);
        assert.ok(await driver.readLatestPose(), 'the session keeps running after the re-derivation');
        await driver.reset();
        assert.deepEqual(failures, []);
    } finally { await driver.close(); }
});

// Murdoch-2 H3 (Story 9.4b): root cause of clock-v2/clock-master-run2.log ({"solved":1,"lit":false}). The lamp's
// cosmetic is a 0.16 s SmoothStep (CosmeticCurves.SignalLamp) that starts at the activation that solves the goal, so
// the former single read right after the solve saw a partly lit lamp whenever it landed inside those 0.16 s; the load
// in that run only moved where the read landed. Unloaded, this replays the former read and proves the ramp on
// master-time animation ordinals, independent of wall-clock read timing. The chain test above polls instead.
test('H3: the lamp ramps over 0.16 s of master time after the solve, so a read at the solve instant is not yet lit', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    const failures: string[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (message.type() === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE')) failures.push(text.slice(0, 160));
    });
    driver.page.on('pageerror', error => failures.push(error.message.slice(0, 160)));
    try {
        await driver.selectLevel('domino_effect');
        for (const x of [-4, -3, -2, -1]) { await driver.selectTool('domino'); await driver.placeOnCanvas(tileScreenX(x), TILE_Y); }
        await driver.connectActivation({ x: tileScreenX(0.1), y: TILE_Y, panel: 'locked' }, { x: 915, y: 612 });
        await driver.run(0);
        await driver.waitForGoalSolved(15000);
        const immediate = await driver.readLastAnimationSample(LAMP_TARGET); // the former assertion's single read
        let lit = false;
        for (const deadline = Date.now() + 3000; !lit && Date.now() < deadline; await driver.page.waitForTimeout(50))
            lit = (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === HALF_ONE;
        const lamp = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
        const rising = lamp.find(sample => sample.valBits !== 0);
        const full = lamp.find(sample => sample.valBits === HALF_ONE);
        assert.ok(rising && full, 'the lamp ramp was sampled');
        const rampOrdinals = Number(BigInt(full.ordinal) - BigInt(rising.ordinal));
        console.log(JSON.stringify({ immediate: immediate ? { value: immediate.value, ordinal: immediate.ordinal } : null,
            firstLit: { value: rising.value, ordinal: rising.ordinal }, full: full.ordinal, rampOrdinals, lit }));
        assert.ok(rising.valBits !== HALF_ONE, 'the first lit lamp sample is partial');
        // 0.16 s on the 60 Hz animation lattice is 9.6 ordinals.
        assert.ok(rampOrdinals >= 9, `the lamp reaches full only after its 0.16 s ramp (${rampOrdinals} ordinals)`);
        assert.ok(lit, 'polling observes the lit lamp');
        assert.deepEqual(failures, []);
    } finally { await driver.close(); }
});

// R2-F1 decision (coordinator, 19 Oct 2026; Story 9.4b): Murdoch-2's cat-017 failure was the Browser's own event loop
// stalling ~0.5 s after ready, past the 500 ms mapping lifetime, with no probe outstanding. Such a lapse is renewed and
// checked: a concluding exchange (narrow enough to qualify the mapping; wider ones are inconclusive) consistent with the
// old mapping keeps the calibration (typed CCGPU_CLOCK_RENEWED_LATE, reported, never a failure), provided the old mapping
// drift-widened to that exchange still fits the mapped width (stalls up to about 1.2 s); one that disagrees is a
// recalibration exactly as before (D23, R3-F1, R4-F1).
const STALL_MS = 700, AFTER_READY_MS = 500;
async function stallBrowser(driver: WorkshopDriver, perturbMilliseconds: number): Promise<void> {
    await driver.page.evaluate(({ block, perturb }: { block: number; perturb: number }) => {
        const end = performance.now() + block;
        while (performance.now() < end) { /* injected main-thread stall */ }
        // Control only: the Browser's native clock jumps forward, so the fresh exchange disagrees with the old mapping.
        if (perturb > 0) {
            const original = performance.now.bind(performance);
            performance.now = () => original() + perturb;
        }
    }, { block: STALL_MS, perturb: perturbMilliseconds });
}
async function until(condition: () => boolean, timeoutMs: number, driver: WorkshopDriver): Promise<void> {
    for (const deadline = Date.now() + timeoutMs; !condition() && Date.now() < deadline;) await driver.page.waitForTimeout(25);
}
const roleOf = (line: string) => JSON.parse(line.slice(line.indexOf(' ') + 1)).role as number;
// Build mode publishes nothing (workshop-driver.ts), so the placed construction is observed through Run, and exact
// restoration as in cat-014: a ball dropped from the 3 m placement plane is in free fall for about 0.79 s, py is strictly
// monotonic there and identifies the committed tick, so a restored world replays the first Run's states bit-for-bit.
const FREE_FALL_MS = 500;
async function recordFall(driver: WorkshopDriver, label: string): Promise<WorkshopBodyPose[]> {
    const states: WorkshopBodyPose[] = [];
    for (const start = Date.now(); Date.now() - start < FREE_FALL_MS;) {
        const slot = await driver.readLatestPose(5, 1);
        assert.ok(slot, `${label}: the pose ring is readable`);
        assert.equal(slot.bodies.length, 1, `${label}: exactly the placed ball is published`);
        states.push(slot.bodies[0]);
    }
    return states;
}
function assertReplaysExactly(original: WorkshopBodyPose[], restored: WorkshopBodyPose[], label: string): void {
    const byHeight = new Map(original.map(b => [b.py, b]));
    const matched = new Set<number>();
    for (const b of restored) {
        const o = byHeight.get(b.py);
        if (!o) continue;
        assert.deepEqual([b.px, b.py, b.pz, b.vx, b.vy, b.vz, b.qx, b.qy, b.qz, b.qw], [o.px, o.py, o.pz, o.vx, o.vy, o.vz, o.qx, o.qy, o.qz, o.qw],
            `${label}: the restored committed state at py ${b.py} equals the original bit-for-bit`);
        matched.add(b.py);
    }
    assert.ok(matched.size >= 3, `${label}: at least three restored free-fall states coincide with the original Run (${matched.size} of ${restored.length} reads)`);
}

test('A 700 ms Browser stall after ready renews its mapping late: no recalibration; placement, Run/Reset and Save/Load are unaffected', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL }); // the strict suite policy: any recalibration fails at close
    try {
        await driver.page.waitForTimeout(AFTER_READY_MS);
        await stallBrowser(driver, 0);
        await until(() => driver.clockLateRenewals.length > 0, 3000, driver);
        console.log(JSON.stringify({ renewals: driver.clockLateRenewals.map(line => line.slice(0, 300)), recalibrations: driver.clockRecalibrations.length }));
        assert.deepEqual(driver.clockLateRenewals.map(roleOf), [1], 'exactly one late renewal, of the Browser mapping');
        assert.deepEqual(driver.clockRecalibrations, [], 'no recalibration');
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(0);
        const original = await recordFall(driver, 'first Run');
        assert.ok(original[original.length - 1].py < original[0].py, 'the ball falls under Run');
        await driver.reset();
        await driver.run(0);
        assertReplaysExactly(original, await recordFall(driver, 'Run after Reset'), 'Reset');
        await driver.reset();
        await driver.save();
        await driver.reload();
        await driver.load();
        await driver.run(0);
        assertReplaysExactly(original, await recordFall(driver, 'Run after Load'), 'Load');
        await driver.reset();
        assert.deepEqual(driver.clockRecalibrations, [], 'still no recalibration after the whole session');
        assert.equal(driver.clockLateRenewals.length, 1);
    } finally { await driver.close(); }
});

test('The same stall with the Browser clock jumping 5 ms disagrees with the old mapping: one recalibration, and the driver fails the suite', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    let closed = false;
    try {
        await driver.page.waitForTimeout(AFTER_READY_MS);
        await stallBrowser(driver, 5);
        await until(() => driver.clockRecalibrations.length > 0, 3000, driver);
        await driver.page.waitForTimeout(500);
        console.log(JSON.stringify({ recalibrations: driver.clockRecalibrations.map(line => line.slice(0, 300)), renewals: driver.clockLateRenewals.length }));
        assert.equal(driver.clockRecalibrations.length, 1, 'exactly one recalibration');
        const record = JSON.parse(driver.clockRecalibrations[0].slice('CCGPU_CLOCK_RECALIBRATED '.length));
        assert.equal(record.role, 1);
        assert.equal(record.causeName, 'MappingLifetime');
        assert.equal(record.stateBefore, 6, 'the disagreeing fresh exchange ended a renewal (ClockMappingState.Renewing)');
        assert.deepEqual(driver.clockLateRenewals, []);
        // The suite policy: closing this session fails its suite.
        closed = true;
        await assert.rejects(driver.closeAfterSuite(), /recalibrated a clock mapping 1 time/);
    } finally { if (!closed) await driver.close(); }
});
