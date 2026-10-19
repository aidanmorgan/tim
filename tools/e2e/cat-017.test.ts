// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-017 (Story 9.3; lifecycle re-verified by Story 9R.1).
// Verifies System Clock Periodic Pulse Generator:
// 1. Placement and wiring validation: Clock (interval 1.0 s), Counter (target 3), Powered Gate (tilt -15 deg),
//    Upstream Basketball inside gate mouth, and Battery.
//    Wired: Clock -> Counter (Activation), Battery -> Clock (Electrical),
//           Battery -> Counter (Electrical), Counter -> Gate (Electrical).
// 2. Sub-threshold control: running for 2.2 s emits 2 pulses; counter reaches 2 (< target 3);
//    gate remains closed, upstream ball stopped.
// 3. Positive trigger: running for 4.2 s emits 3rd pulse at 3.0 s, reaching target count 3,
//    retracts gate blade, and ball traverses downstream.
// 4. Pause and Reset restoration: pausing mid-interval freezes simulation, reset restores the exact initial state with the
//    clock stopped (phase 0, no due tick).
// 5. State persistence & replay: Save / reload / Load preserves clock, counter, gate, battery, and wiring;
//    simulation replay triggers traversal.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';
import { MemoryKind, activationMemoryRows, assertLoadRestores, type ActivationMemoryRow } from './run-reset-snapshot.ts';

const ConnectionDomain = { Activation: 1, Electrical: 2 } as const;
const Socket = { ActivationOut: 1, ActivationIn: 4, Supply: 7, PowerIn: 8 } as const;
const BLADE_BODY_ID = '4294967810'; // GpuBodyId: (1n << 32n) + 256n + 1n * 256n + 2n
// The committed (dynamic) bodies of the bench: the basketball and the gate blade.
const BENCH = { bodies: 2 } as const;

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

async function click(driver: WorkshopDriver, x: number, y: number) {
    await driver.page.mouse.move(x, y);
    await driver.page.mouse.down();
    await driver.page.waitForTimeout(70);
    await driver.page.mouse.up();
    await driver.page.waitForTimeout(180);
}

async function saved(driver: WorkshopDriver): Promise<Buffer> {
    const value = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error('Existing save unavailable'));
        request.onsuccess = () => {
            const db = request.result;
            try {
                const tx = db.transaction('construction', 'readonly');
                const read = tx.objectStore('construction').get(1);
                tx.oncomplete = () => { db.close(); resolve(Array.from(read.result as Uint8Array)); };
                tx.onabort = () => { db.close(); reject(new Error('Read aborted')); };
            } catch (error) { db.close(); reject(error); }
        };
    }));
    return Buffer.from(value);
}

function assertClockWiring(bytes: Buffer, gateSlot: number, ballSlot: number, counterSlot: number, battSlot: number, clockSlot: number) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    const count = construction.connectionsLength();
    assert.equal(count, 4, `Expected 4 connections in save, got ${count}`);
    const gateId = construction.instances(gateSlot)!.id();
    const counterId = construction.instances(counterSlot)!.id();
    const battId = construction.instances(battSlot)!.id();
    const clockId = construction.instances(clockSlot)!.id();

    const hasEdge = (src: bigint, tgt: bigint, dom: number, from: number, to: number) => {
        for (let i = 0; i < count; i++) {
            const edge = construction.connections(i)!;
            if (edge.source() === src && edge.target() === tgt && edge.domain() === dom && edge.fromSocket() === from && edge.toSocket() === to) return true;
        }
        return false;
    };
    assert(hasEdge(clockId, counterId, ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn), 'Missing Clock -> Counter activation link');
    assert(hasEdge(counterId, gateId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Counter -> Gate electrical link');
    assert(hasEdge(battId, counterId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Battery -> Counter electrical link');
    assert(hasEdge(battId, clockId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Battery -> Clock electrical link');
    const clockInst = construction.instances(clockSlot)!;
    assert.equal(clockInst.partId(), 'clock');
    assert.equal(clockInst.threshold(), 1.0);
}

// The single committed row of one memory kind (the bench has one clock and one counter).
function row(snapshot: RunResetSnapshot, kind: string): ActivationMemoryRow {
    const rows = activationMemoryRows(snapshot).filter(candidate => candidate.kind === kind);
    assert.equal(rows.length, 1, `Expected one committed memory row of kind ${kind}, got ${rows.length}`);
    return rows[0];
}

// Story 9R.1 AC: after Reset the System Clock is stopped (phase 0) with no due tick and no pulses, and the counter is at 0.
function assertClockStopped(snapshot: RunResetSnapshot): void {
    const clock = row(snapshot, MemoryKind.Oscillator);
    assert.deepEqual([clock.phase, clock.due_tick, clock.count], ['0', '0', '0'], 'Reset: clock phase 0, no due tick, no pulses');
    assert.equal(row(snapshot, MemoryKind.Counter).count, '0', 'Reset: counter count 0');
}

describe('CAT-017: System Clock Periodic Pulse Generator E2E', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            console.log(`[E2E-CONSOLE ${msg.type()}] ${msg.text()}`);
            if (isWorkshopConsoleFailure(msg.type(), msg.text())) errors.push(msg.text());
        });
        driver.page.on('pageerror', (err: Error) => errors.push(err.message));
        const hookWorker = (w: Worker) => w.on('console', (msg: ConsoleMessage) => console.log('WORKER:', msg.text()));
        driver.page.on('worker', hookWorker);
        for (const w of driver.page.workers()) {
            hookWorker(w);
        }
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    // Gate (tilt -15 deg), upstream ball, counter, battery and clock, wired through the actual connection buttons;
    // captureSnapshot then saves the construction. Instance slots: 0=Gate, 1=Ball, 2=Counter, 3=Battery, 4=Clock.
    async function buildClockBench(): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);
        await driver.selectTool('counter');
        await driver.placeOnCanvas(920, 510);
        await driver.selectTool('battery');
        await driver.placeOnCanvas(1080, 510);
        await driver.selectTool('clock');
        await driver.placeOnCanvas(450, 485);

        // Wire Link 1: Clock ActivationOut -> Counter ActivationIn (Clock row 1 is at 789)
        await driver.selectPartAt(450, 485);
        await click(driver, 130, 789); // "Connect ActivationOut" on clock
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "ActivationOut → ActivationIn"
        // Wire Link 2: Counter Supply -> Powered Gate PowerIn (Counter now has 3 buttons, row 1 is at 745)
        await driver.selectPartAt(920, 510);
        await click(driver, 100, 745); // "Connect Supply" on counter
        await click(driver, 720, 485); // Click Gate
        await click(driver, 133, 736); // Choice "Supply → PowerIn"
        // Wire Link 3: Battery Supply -> Counter PowerIn (Battery row 1 is at 789)
        await driver.selectPartAt(1080, 510);
        await click(driver, 130, 789); // "Connect Supply" on battery
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "Supply → PowerIn"
        // Wire Link 4: Battery Supply -> Clock PowerIn (Battery now has 3 buttons, row 1 is at 745)
        await driver.selectPartAt(1080, 510);
        await click(driver, 130, 745); // "Connect Supply" on battery
        await click(driver, 450, 485); // Click Clock
        await click(driver, 133, 736); // Choice "Supply → PowerIn"
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        const s0 = await driver.captureSnapshot(BENCH);
        assertClockStopped(s0);
        return s0;
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        assertClockStopped(await driver.expectResetMatches(s0));
    }

    // A 4.2 s run: the 3rd pulse at 3.0 s reaches the counter target, retracts the blade and lets the ball through.
    async function runTraverses(s0: RunResetSnapshot, label: string): Promise<void> {
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;
        while (Date.now() - started < 4200) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) {
                    samples.push({ t: Date.now() - started, ball, blade });
                }
            }
            await driver.page.waitForTimeout(15);
        }
        const paused = await driver.expectRunAdvanced(s0);

        assert.ok(samples.length >= 30, `${label}: expected >=30 distinct pose publications, got ${samples.length}`);
        // Blade must have retracted upward under power from counter contact (py > 4.2)
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(bladeSamples.length > 0, `${label}: blade body poses should be observed`);
        const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
        assert.ok(maxBladeY > 4.2, `${label}: blade must retract open when counter reaches 3, max py=${maxBladeY}`);
        // Upstream ball must pass through open gate downstream (px > 0.15, vx > 0.2)
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px > 0.15, `${label}: ball must traverse through gate bore downstream, got px=${finalBall.px}`);
        assert.ok(finalBall.vx > 0.2, `${label}: ball should exit with positive velocity down slope, got vx=${finalBall.vx}`);
        // The committed counter reached its target from committed clock pulses.
        assert.ok(Number(row(paused, MemoryKind.Counter).count) >= 3, `${label}: committed counter count reaches the target 3`);
        assert.equal(row(paused, MemoryKind.Oscillator).phase, '1', `${label}: the powered clock is running`);
    }

    test('1. Placement and wiring validation', { timeout: 150000 }, async () => {
        await buildClockBench();
        assertClockWiring(await saved(driver), 0, 1, 2, 3, 4);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Sub-threshold control: 2 pulses emitted do not reach target 3; gate remains closed, upstream ball stopped', { timeout: 150000 }, async () => {
        const s0 = await buildClockBench();

        // Run simulation for 2.2 seconds (Clock emits at 1.0s and 2.0s; Counter reaches 2, target is 3)
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;
        while (Date.now() - started < 2200) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) {
                    samples.push({ t: Date.now() - started, ball, blade });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        const paused = await driver.expectRunAdvanced(s0);

        assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);
        // Gate blade remains closed (py < 3.2)
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        if (bladeSamples.length > 0) {
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY < 3.2, `Gate blade must remain closed before 3rd pulse, got max py=${maxBladeY}`);
        }
        // Upstream ball remains stopped by gate (px < 0.2, py > 2.8)
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < 0.2, `Upstream ball must remain stopped upstream, got px=${finalBall.px}`);
        assert.ok(finalBall.py > 2.8, `Upstream ball must remain stopped upstream at height, got py=${finalBall.py}`);
        // Boundary: the committed counter is at target - 1 before the Reset.
        assert.equal(row(paused, MemoryKind.Counter).count, '2', 'Counter at target - 1 (2 of 3) before Reset');

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Positive trigger: 3rd pulse at 3.0 s reaches Counter target 3, retracts gate blade, and ball traverses downstream', { timeout: 150000 }, async () => {
        const s0 = await buildClockBench();
        await runTraverses(s0, 'first run');
        await resetMatches(s0);
        await runTraverses(s0, 'second run');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Pause and Reset restoration: pausing freezes simulation, reset cleanly restores initial state', { timeout: 150000 }, async () => {
        const s0 = await buildClockBench();

        await driver.run(1500); // 1 pulse fired
        const paused = await driver.expectRunAdvanced(s0);
        // Boundary: the clock is mid-interval at the pause: running, one pulse committed, the next due at 2.0 s (tick 240).
        const clock = row(paused, MemoryKind.Oscillator);
        assert.deepEqual([clock.phase, clock.count, clock.due_tick], ['1', '1', '240'], 'Paused mid-interval: running, 1 pulse, due at tick 240');
        const pausedSlot = await driver.readLatestPose();
        assert.ok(pausedSlot, 'Pose slot readable while paused');
        const ballPaused = pausedSlot.bodies.find(b => b.id === '2');
        assert.ok(ballPaused, 'Ball pose readable while paused');
        assert.ok(ballPaused.px < 0.2, 'Ball still held upstream at pause');

        await driver.page.waitForTimeout(500);
        const afterPauseSlot = await driver.readLatestPose();
        const ballAfterPause = afterPauseSlot?.bodies.find(b => b.id === '2');
        assert.ok(ballAfterPause, 'Ball pose readable after the pause window');
        assert.ok(Math.abs(ballAfterPause.px - ballPaused.px) < 0.01, 'No motion while paused');

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence & replay: Save / reload / Load preserves clock wiring, replay triggers full traversal', { timeout: 150000 }, async () => {
        // captureSnapshot saves the wired bench.
        const s0 = await buildClockBench();
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        assertClockWiring(await saved(driver), 0, 1, 2, 3, 4);

        // Replay simulation on restored construction
        await runTraverses(loaded, 'replay after reload');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
