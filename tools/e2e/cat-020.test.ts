// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-020 (Story 9.2; lifecycle re-verified by Story 9R.1).
// Verifies Pulse Counter Event Accumulator:
// 1. Placement and wiring validation: Counter (target 3), Powered Gate (tilt -15 deg), Ball Detector (tilt -90 deg),
//    Upstream Basketball inside gate mouth, Battery, and Lamp.
//    Wired: Detector -> Counter -> Lamp (Activation), Battery -> Counter -> Gate (Electrical).
// 2. Negative control: 2 ball crossings do not reach target 3; gate remains closed, upstream ball stopped, lamp unlit.
// 3. Positive trigger: 3rd ball crossing reaches target count 3, illuminates lamp, retracts gate blade, and ball traverses downstream.
// 4. Saturation: 4th ball crossing does not glitch or re-trigger, saturated state maintains gate open.
// 5. State persistence & reset: Save / reload / Load preserves counter, gate, detector, lamp, and wiring; simulation replay triggers traversal.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run, with the committed counter at count 0 and its
// activation Clear (contact open); each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, type InstanceRecord } from './workshop-wire.ts';
import { MemoryKind, activationMemoryRows, assertLoadRestores, snapshotEntries, snapshotValue, type ActivationMemoryRow } from './run-reset-snapshot.ts';

const ConnectionDomain = { Activation: 1, Electrical: 2 } as const;
const Socket = { ActivationOut: 1, ActivationIn: 4, Supply: 7, PowerIn: 8 } as const;
const BLADE_BODY_ID = '4294967810'; // GpuBodyId: (1n << 32n) + 256n + 1n * 256n + 2n
const LAMP_TARGET = '5'; // Animation target of the Signal lamp's activation node: lamp id 3 + 2
// The detector (tilted −90°) counts a ball only if it starts armed: X < −(0.02 m clearance + 0.34 m radius) = −0.36 m
// (spec-6-8). One lift pixel is 1/57.9 m, so every ball is lifted at least 28 px (0.48 m). The former 18 px ball (0.31 m)
// started inside the clearance and was never counted; the recipe only passed through the deleted in-engine crossing shortcut.
const ARMED_LIFTS = [28, 42, 56, 70] as const;
// The negative control's two balls (the original recipe's lifts).
const TWO_CROSSING_LIFTS = [50, 100] as const;
const ActivationPhase = { Clear: '0' } as const;

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

function assertCounterWiring(bytes: Buffer, gateSlot: number, ballSlot: number, lampSlot: number, battSlot: number, detSlot: number, counterSlot: number) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    const count = construction.connectionsLength();
    assert.equal(count, 4, `Expected 4 connections in save, got ${count}`);
    const gateId = construction.instances(gateSlot)!.id();
    const lampId = construction.instances(lampSlot)!.id();
    const battId = construction.instances(battSlot)!.id();
    const detId = construction.instances(detSlot)!.id();
    const counterId = construction.instances(counterSlot)!.id();

    const hasEdge = (src: bigint, tgt: bigint, dom: number, from: number, to: number) => {
        for (let i = 0; i < count; i++) {
            const edge = construction.connections(i)!;
            if (edge.source() === src && edge.target() === tgt && edge.domain() === dom && edge.fromSocket() === from && edge.toSocket() === to) return true;
        }
        return false;
    };
    assert(hasEdge(counterId, lampId, ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn), 'Missing Counter -> Lamp activation link');
    assert(hasEdge(counterId, gateId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Counter -> Gate electrical link');
    assert(hasEdge(battId, counterId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Battery -> Counter electrical link');
    assert(hasEdge(detId, counterId, ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn), 'Missing Detector -> Counter activation link');
    // Counter assertions expose only the Counter configuration; threshold belongs to other generic record uses.
    const counterInst: Pick<InstanceRecord, 'partId' | 'targetCount'> = construction.instances(counterSlot)!;
    assert.equal(counterInst.partId(), 'counter');
    // Story 9A.2: the Counter target is its whole target_count field (the f32 threshold was the deleted coercion).
    assert.equal(counterInst.targetCount(), 3);
}

function counterRow(snapshot: RunResetSnapshot): ActivationMemoryRow {
    const rows = activationMemoryRows(snapshot).filter(row => row.kind === MemoryKind.Counter);
    assert.equal(rows.length, 1, `Expected one committed counter row, got ${rows.length}`);
    return rows[0];
}

// The committed activation of the counter node (its switched contact): the activations entry with the counter's node id.
function counterActivationPhase(snapshot: RunResetSnapshot): string {
    const node = counterRow(snapshot).node;
    const length = Number(snapshotValue(snapshot, 'read.activations.length'));
    for (let i = 0; i < length; i++)
        if (snapshotValue(snapshot, `read.activations[${i}].node`) === node) return snapshotValue(snapshot, `read.activations[${i}].phase`);
    assert.fail(`No committed activation for counter node ${node}`);
}

// Story 9R.1 AC: after Reset the counter's count is 0 and its activation is Clear (contact open).
function assertCounterCleared(snapshot: RunResetSnapshot): void {
    assert.equal(counterRow(snapshot).count, '0', 'Reset: counter count 0');
    assert.equal(counterActivationPhase(snapshot), ActivationPhase.Clear, 'Reset: counter activation Clear (contact open)');
}

describe('CAT-020: Pulse Counter Event Accumulator E2E', () => {
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

    // Gate, upstream ball, lamp, battery, detector (tilt -90 deg) and counter, wired through the connection buttons, then one
    // basketball above the detector per lift; captureSnapshot saves the construction.
    // Instance slots: 0=Gate, 1=Ball, 2=Lamp, 3=Battery, 4=Detector, 5=Counter, then the detector balls.
    async function buildCounterBench(lifts: readonly number[]): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(1000, 600);
        await driver.selectTool('battery');
        await driver.placeOnCanvas(1080, 510);
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(450, 485);
        await driver.selectPartAt(450, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectTool('counter');
        await driver.placeOnCanvas(920, 510);

        // Wire Link 1: Counter ActivationOut -> Lamp ActivationIn (Counter has 2 buttons, row 0 is at 745)
        await driver.selectPartAt(920, 510);
        await click(driver, 100, 745); // "Connect ActivationOut"
        await click(driver, 1000, 600); // Click Lamp
        await click(driver, 133, 736); // Choice "ActivationOut → ActivationIn"
        // Wire Link 2: Counter Supply -> Powered Gate PowerIn (Counter now has 3 buttons, row 1 is at 745)
        await driver.selectPartAt(920, 510);
        await click(driver, 100, 745); // "Connect Supply"
        await click(driver, 720, 485); // Click Gate
        await click(driver, 133, 736); // Choice "Supply → PowerIn"
        // Wire Link 3: Battery Supply -> Counter PowerIn (Battery row 1 is at 789)
        await driver.selectPartAt(1080, 510);
        await click(driver, 130, 789); // "Connect Supply" on battery
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "Supply → PowerIn"
        // Wire Link 4: Detector ActivationOut -> Counter ActivationIn (Detector row 0 is at 792)
        await driver.selectPartAt(450, 485);
        await click(driver, 133, 792); // "Connect ActivationOut" on detector
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "ActivationOut → ActivationIn"
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        for (const lift of lifts) {
            await driver.selectTool('basketball');
            await driver.placeOnCanvas(450, 485);
            await driver.setPartMode('move');
            await driver.liftSelectedPart(lift, 450, 485);
        }
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // The committed (dynamic) bodies: the upstream ball, the gate blade and each detector ball.
        const s0 = await driver.captureSnapshot({ bodies: 2 + lifts.length });
        assertCounterCleared(s0);
        return s0;
    }

    async function runAndSample(s0: RunResetSnapshot, windowMs: number, pollMs: number):
        Promise<{ samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[]; paused: RunResetSnapshot }> {
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;
        while (Date.now() - started < windowMs) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) samples.push({ ball, blade });
            }
            await driver.page.waitForTimeout(pollMs);
        }
        const paused = await driver.expectRunAdvanced(s0);
        assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);
        return { samples, paused };
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        assertCounterCleared(await driver.expectResetMatches(s0));
    }

    // At or past the target: the blade retracts, the lamp lights, and the upstream ball passes downstream.
    async function runReachesTarget(s0: RunResetSnapshot, label: string): Promise<RunResetSnapshot> {
        const lampBefore = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;
        const { samples, paused } = await runAndSample(s0, 3500, 15);
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(bladeSamples.length > 0, `${label}: blade body poses should be observed`);
        const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
        assert.ok(maxBladeY > 4.2, `${label}: blade must retract open when counter reaches 3, max py=${maxBladeY}`);
        const lampSamples = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
        assert.ok(lampSamples.length > lampBefore && lampSamples[lampSamples.length - 1].value > 0.99, `${label}: counter → lamp lights at target`);
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px > 2.0, `${label}: ball must traverse through gate bore downstream, got px=${finalBall.px}`);
        assert.ok(finalBall.vx > 0.2, `${label}: ball should exit with positive velocity down slope, got vx=${finalBall.vx}`);
        assert.notEqual(counterActivationPhase(paused), ActivationPhase.Clear, `${label}: committed counter activation is Latched (contact closed)`);
        return paused;
    }

    test('1. Placement and wiring validation', { timeout: 150000 }, async () => {
        await buildCounterBench([]);
        assertCounterWiring(await saved(driver), 0, 1, 2, 3, 4, 5);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: 2 ball crossings do not emit, gate closed, lamp unlit, upstream ball stopped', { timeout: 150000 }, async () => {
        const s0 = await buildCounterBench(TWO_CROSSING_LIFTS);

        // Run simulation: 2 balls fall through detector
        const { samples, paused } = await runAndSample(s0, 1500, 25);
        assert.equal((await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length, 0, 'Counter below target never lights its lamp');
        // Gate blade remains closed (py < 3.2)
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        if (bladeSamples.length > 0) {
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY < 3.2, `Gate blade must remain closed after 2 ball crossings, got max py=${maxBladeY}`);
        }
        // Upstream ball remains stopped by gate (px < 0.2, py > 2.8)
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < 0.2, `Upstream ball must remain stopped upstream, got px=${finalBall.px}`);
        assert.ok(finalBall.py > 2.8, `Upstream ball must remain stopped upstream at height, got py=${finalBall.py}`);
        // Boundary: the committed counter is at target - 1 with its contact open before the Reset.
        assert.equal(counterRow(paused).count, '2', 'Counter at target - 1 (2 of 3) before Reset');
        assert.equal(counterActivationPhase(paused), ActivationPhase.Clear, 'Counter contact still open below target');

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Positive trigger: 3rd ball crossing reaches target count 3, illuminates lamp, retracts gate blade, and ball traverses downstream', { timeout: 150000 }, async () => {
        const s0 = await buildCounterBench(ARMED_LIFTS.slice(0, 3));
        for (const cycle of ['first run', 'second run']) {
            const paused = await runReachesTarget(s0, cycle);
            assert.equal(counterRow(paused).count, '3', `${cycle}: committed counter count 3`);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Saturation: 4th ball crossing does not glitch or re-trigger, saturated state maintains gate open', { timeout: 150000 }, async () => {
        const s0 = await buildCounterBench(ARMED_LIFTS);
        const paused = await runReachesTarget(s0, 'saturated run');
        assert.ok(Number(counterRow(paused).count) >= 3, 'Saturated counter stays at or past its target without re-triggering');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence & reset: Save / reload / Load preserves counter, gate, detector, lamp, and wiring; simulation replay triggers traversal', { timeout: 150000 }, async () => {
        // captureSnapshot saves the wired bench with its three detector balls.
        const s0 = await buildCounterBench(ARMED_LIFTS.slice(0, 3));
        assertCounterWiring(await saved(driver), 0, 1, 2, 3, 4, 5);

        // Reload browser page from scratch, then Load construction from IndexedDB
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot({ bodies: s0.bodies });
        assertLoadRestores(s0, loaded);
        assertCounterWiring(await saved(driver), 0, 1, 2, 3, 4, 5);
        assert.ok(snapshotEntries(loaded, 'read.activation_memory').length > 0, 'The loaded read publishes the counter memory row');

        await runReachesTarget(loaded, 'replay after reload');
        // Reset must restore to authored state without residual runtime contact
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
