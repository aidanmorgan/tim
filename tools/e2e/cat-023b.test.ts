// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-023b (Story 5.2).
// Verifies the domino cascade and the orientation-threshold sensor through the real Workshop UI:
// 1. domino_effect: four placed dominoes wired end -> lamp cascade in sequence, the lamp lights and CCGOAL_SOLVED prints exactly once
// 2. Three dominoes (one tile omitted) break the chain: the end Domino never tilts, the lamp never samples, the goal stays unsolved
// 3. Free workshop: a Domino tilted 10 deg through Fine rotate and wired to a lamp settles without emitting
// 4. Save / reload / Load restores the wired construction and solves; Reset rearms the sensor and a second Run solves again
//    (test 4 loads the construction test 1 saved: run the suite serially and in file order)
import assert from 'node:assert/strict';
import { after, before, beforeEach, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type AnimationSampleRecord, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeWorkshopResponse, type BodyRead, type WorkshopResponse } from './workshop-wire.ts';

function bodyState(b: BodyRead) {
    return {
        id: b.id(),
        cell: { x: b.cell()!.x(), y: b.cell()!.y(), z: b.cell()!.z() },
        local: { x: b.local()!.x(), y: b.local()!.y(), z: b.local()!.z() },
        rotation: { x: b.rotation()!.x(), y: b.rotation()!.y(), z: b.rotation()!.z(), w: b.rotation()!.w() },
        com: { x: b.com()!.x(), y: b.com()!.y(), z: b.com()!.z() },
        velocity: { x: b.velocity()!.x(), y: b.velocity()!.y(), z: b.velocity()!.z() },
        angularVelocity: { x: b.angularVelocity()!.x(), y: b.angularVelocity()!.y(), z: b.angularVelocity()!.z() },
    };
}

// domino_effect authored identities: ball 1, end Domino 2, lamp 3; placed tiles receive 4..7 in placement order.
const BALL_ID = '1', END_ID = '2';
const TILE_IDS = ['4', '5', '6', '7'];
const LAMP_TARGET = '5';                  // activation node 3 + 2
const HALF_ONE = 15360;
// Front view of the authored level: screen x = 720 + 65.2 m, tiles stand at y = 0.1 m on screen row 676 (calibrated through the pose ring).
const TILE_Y = 676;
const tileScreenX = (x: number) => Math.round(720 + 65.2 * x);
const CHAIN_X = [-4, -3, -2, -1];
const END_SCREEN = { x: tileScreenX(0.1), y: TILE_Y };
const LAMP_SCREEN = { x: 915, y: 612 };
// Free workshop (3D view, placement plane y = 3 m).
const FREE_DOMINO_SCREEN = { x: 720, y: 485 };
const FREE_LAMP_SCREEN = { x: 900, y: 420 };
const LOWER_TILTED_TO_BENCH_PX = -156;    // cat-023a lowers an upright tile by 162 px; a 10 deg tile keeps 3 cm more corner clearance
const LOWERED_DOMINO_SCREEN = { x: 720, y: 641 };
const BENCH_Y = -0.46;
const HALF_HEIGHT = 0.55;

function tiltDegrees(b: WorkshopBodyPose): number {
    const upY = 1 - 2 * (b.qx * b.qx + b.qz * b.qz);
    return Math.acos(Math.max(-1, Math.min(1, upY))) * 180 / Math.PI;
}

// Rotation from a reference pose: the sensor's own measure, 2·acos|<q, q0>|.
function angleFrom(reference: WorkshopBodyPose, b: WorkshopBodyPose): number {
    const dot = Math.abs(reference.qx * b.qx + reference.qy * b.qy + reference.qz * b.qz + reference.qw * b.qw);
    return 2 * Math.acos(Math.min(1, dot)) * 180 / Math.PI;
}

async function bodies(driver: WorkshopDriver): Promise<Map<string, WorkshopBodyPose>> {
    const pose = await driver.readLatestPose();
    assert.ok(pose, 'Pose slot must be readable');
    return new Map(pose.bodies.map(b => [b.id, b]));
}

async function waitFor(fn: () => Promise<boolean>, timeoutMs: number, stepMs = 50): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (await fn()) return true;
        await new Promise(resolve => setTimeout(resolve, stepMs));
    }
    return fn();
}

async function placeChain(driver: WorkshopDriver, xs: number[]): Promise<void> {
    for (const x of xs) {
        await driver.selectTool('domino');
        await driver.placeOnCanvas(tileScreenX(x), TILE_Y);
    }
}

// Placed tiles stand 1 cm above the bench (placement snaps to 0.1 m) and may already show landing micro-rotation in the first read;
// the locked end Domino starts at canonical f32 x=0.1. Tick-zero restoration is checked byte-for-byte separately;
// live post-Run contact settling may introduce sub-f32-unit motion even before the cascade arrives.
const END_X = Math.fround(0.1);
const REST_POSITION_RESIDUAL = 2 ** -23; // One f32 ULP at the one-metre scene scale, not a gameplay placement window.
function assertUprightPlacement(b: WorkshopBodyPose, x: number, label: string): void {
    assert.ok(tiltDegrees(b) < 0.05, `${label}: upright (tilt ${tiltDegrees(b).toFixed(4)} deg)`);
    assert.ok(Math.abs(b.px - x) < 0.002 && Math.abs(b.pz) < 0.002, `${label}: placed at x=${x} (got ${b.px.toFixed(4)}, ${b.pz.toFixed(4)})`);
    assert.ok(b.py > BENCH_Y + HALF_HEIGHT - 0.01 && b.py < 0.2, `${label}: stands on the bench (py=${b.py.toFixed(3)})`);
}
function assertEndDominoBeforeImpact(b: WorkshopBodyPose, label: string): void {
    assert.deepEqual([b.qx, b.qy, b.qz, b.qw], [0, 0, 0, 1], `${label}: identity orientation bit-for-bit`);
    assert.ok(Math.abs(b.px - END_X) <= REST_POSITION_RESIDUAL && Math.abs(b.pz) <= REST_POSITION_RESIDUAL,
        `${label}: live resting x/z remain at the authored position within f32 rounding`);
    assert.ok(Math.abs(b.py - (BENCH_Y + HALF_HEIGHT)) < 0.0005, `${label}: sits on the bench (py=${b.py.toFixed(4)})`);
}

// Watch the cascade until every body has passed the threshold; returns the pose-ring sequence at which each first passed 45 deg
// (the committed physics order, not the poll clock).
async function watchCascade(driver: WorkshopDriver, ids: string[], timeoutMs: number): Promise<Map<string, number>> {
    const passed = new Map<string, number>();
    const start = Date.now();
    while (Date.now() - start < timeoutMs && passed.size < ids.length) {
        const pose = await driver.readLatestPose();
        assert.ok(pose, 'Pose slot must be readable');
        const sequence = Number(pose.sequence);
        for (const id of ids) {
            const b = pose.bodies.find(body => body.id === id);
            if (b && !passed.has(id) && tiltDegrees(b) > 45) passed.set(id, sequence);
        }
        await driver.page.waitForTimeout(40);
    }
    return passed;
}

describe('CAT-023b: Domino cascade mechanics & orientation-threshold sensor', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    // Typed clock telemetry (Story 9.4b): a clock lapse recalibrates and continues, so it is not an error line; this suite
    // still requires zero recalibrations, so a regression of the off-thread clock master is caught.
    const recalibrations: string[] = [];
    let errorsAtTestStart = 0, recalibrationsAtTestStart = 0;
    // Each test checks only the console failures of its own slice; the suite total is checked once after the last test,
    // so one failure reports once and failures logged between tests are still caught.
    function assertCleanSlice(): void {
        const own = errors.slice(errorsAtTestStart);
        assert.equal(own.length, 0, `Zero errors expected in this test, got: ${own.join('; ')}`);
        assert.deepEqual(recalibrations.slice(recalibrationsAtTestStart), [], 'Zero clock recalibrations in this test');
    }
    const reads: WorkshopResponse[] = [];
    let authoredEnd: ReturnType<typeof bodyState> | undefined;
    function assertEndRestored(label: string): void {
        const read = reads.at(-1);
        assert.ok(read, `${label}: actual committed read exists`);
        assert.equal(read.tick(), 0n, `${label}: initial physical tick`);
        let end: BodyRead | undefined;
        for (let i = 0; i < read.bodiesLength(); i++) {
            const body = read.bodies(i)!;
            if (body.id() === BigInt(END_ID)) end = body;
        }
        assert.ok(end, `${label}: locked end body exists`);
        assert.deepEqual([end.rotation()!.x(), end.rotation()!.y(), end.rotation()!.z(), end.rotation()!.w()], [0, 0, 0, 1]);
        assert.equal(Math.fround((end.cell()!.x() + end.local()!.x()) / 16), END_X);
        assert.equal(end.cell()!.z(), 0); assert.equal(end.local()!.z(), 0);
        const state = bodyState(end);
        if (authoredEnd) assert.deepEqual(state, authoredEnd, `${label}: complete authored body restored byte-for-byte`);
        else authoredEnd = state;
    }

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.startsWith('CCGPU ')) reads.push(decodeWorkshopResponse(Buffer.from(text.slice(6), 'base64')));
            if (isWorkshopConsoleFailure(msg.type(), text)) errors.push(text);
            if (text.startsWith('CCGPU_CLOCK_RECALIBRATED')) recalibrations.push(text);
        });
        driver.page.on('pageerror', (error: Error) => errors.push(error.message));
    });

    beforeEach(() => { errorsAtTestStart = errors.length; recalibrationsAtTestStart = recalibrations.length; });

    after(async () => {
        // Both the suite's own checks and its session close findings fail this suite; neither masks the other.
        const failures: unknown[] = [];
        try {
            assert.equal(errors.length, 0, `Zero errors expected across the suite, got: ${errors.join('; ')}`);
            assert.deepEqual(recalibrations, [], 'Zero clock recalibrations across the suite');
        } catch (error) { failures.push(error); }
        try { if (driver) await driver.closeAfterSuite(); } catch (error) { failures.push(error); }
        if (failures.length === 1) throw failures[0];
        if (failures.length > 1) throw new AggregateError(failures, 'The suite checks and its session close both failed.');
    });

    test('1. Four dominoes wired end → lamp topple in sequence, the lamp lights and CCGOAL_SOLVED prints exactly once', { timeout: 150000 }, async () => {
        await driver.selectLevel('domino_effect');
        await placeChain(driver, CHAIN_X);
        await driver.connectActivation({ ...END_SCREEN, panel: 'locked' }, LAMP_SCREEN);
        await driver.save();
        assertEndRestored('authored saved end Domino');
        assert.equal(await driver.readLastAnimationSample(LAMP_TARGET), null, 'No lamp sample before Run');

        await driver.run();
        const first = await bodies(driver);
        assert.deepEqual([...first.keys()].sort((a, b) => BigInt(a) < BigInt(b) ? -1 : BigInt(a) > BigInt(b) ? 1 : 0), [BALL_ID, END_ID, ...TILE_IDS], 'Ball, end Domino and four tiles are published');
        TILE_IDS.forEach((id, i) => assertUprightPlacement(first.get(id)!, CHAIN_X[i], `tile ${id}`));
        assertEndDominoBeforeImpact(first.get(END_ID)!, 'end Domino');
        const ball = first.get(BALL_ID)!;
        assert.ok(Math.abs(ball.px + 4.4) < 0.01 && ball.py > 2.5, `The locked ball starts above the first tile position (got ${ball.px.toFixed(2)}, ${ball.py.toFixed(2)})`);

        const passed = await watchCascade(driver, [...TILE_IDS, END_ID], 8000);
        assert.equal(passed.size, 5, `Every tile and the end Domino pass 45 deg (passed: ${[...passed.keys()].join(',')})`);
        const order = [...passed.entries()].sort((a, b) => a[1] - b[1]).map(([id]) => id);
        assert.deepEqual(order, [...TILE_IDS, END_ID], 'The tiles topple in chain order, the end Domino last');
        assert.equal(new Set(passed.values()).size, 5, 'Each body passes the threshold in a distinct committed sample');

        const lit = await waitFor(async () => (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === HALF_ONE, 3000);
        assert.ok(lit, 'The lamp lights once the end Domino passes the threshold');
        const solved = await waitFor(async () => (await driver.readCaptured()) === 1, 3000);
        assert.ok(solved, 'CCGOAL_SOLVED prints once');

        // Hover and settle: no second emission. The lamp ramps up once and holds; the console line stays single.
        await driver.page.waitForTimeout(2500);
        assert.equal(await driver.readCaptured(), 1, 'CCGOAL_SOLVED prints exactly once');
        const lampHistory = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).map((s: AnimationSampleRecord) => s.value);
        assert.ok(lampHistory.length >= 5, `The lamp publishes its glow ramp (${lampHistory.length} samples)`);
        for (let i = 1; i < lampHistory.length; i++) assert.ok(lampHistory[i] >= lampHistory[i - 1], 'The lamp glow never restarts: one emission');
        assert.equal(lampHistory[lampHistory.length - 1], 1, 'The lamp holds full glow');
        const end = (await bodies(driver)).get(END_ID)!;
        assert.ok(tiltDegrees(end) > 60, `The end Domino lies toppled (${tiltDegrees(end).toFixed(1)} deg)`);
        await driver.reset(500);
        assertCleanSlice();
    });

    test('2. Three dominoes leave a gap: the chain breaks, the end Domino stays upright and the lamp never lights', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('domino_effect');
        const threeChain = [-4, -3, -1];
        await placeChain(driver, threeChain);
        await driver.connectActivation({ ...END_SCREEN, panel: 'locked' }, LAMP_SCREEN);

        await driver.run();
        const placedIds = ['4', '5', '6'];
        const first = await bodies(driver);
        placedIds.forEach((id, i) => assertUprightPlacement(first.get(id)!, threeChain[i], `tile ${id}`));
        const passed = await watchCascade(driver, ['4', '5'], 6000);
        assert.deepEqual([...passed.keys()].sort(), ['4', '5'], 'The ball topples the first two tiles');
        await driver.page.waitForTimeout(4000);
        const all = await bodies(driver);
        assert.ok(tiltDegrees(all.get('6')!) < 2, `The tile beyond the gap is never struck (${tiltDegrees(all.get('6')!).toFixed(2)} deg)`);
        assert.ok(tiltDegrees(all.get(END_ID)!) < 2, `The end Domino stays upright (${tiltDegrees(all.get(END_ID)!).toFixed(2)} deg)`);
        assert.equal(await driver.readLastAnimationSample(LAMP_TARGET), null, 'The lamp never samples: no orientation occurrence');
        assert.equal(await driver.readCaptured(), 0, 'The goal stays unsolved');
        await driver.reset(500);
        assertCleanSlice();
    });

    test('3. A Domino tilted 10 deg through Fine rotate and wired to a lamp rocks back without emitting', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectTool('domino');
        await driver.placeOnCanvas(FREE_DOMINO_SCREEN.x, FREE_DOMINO_SCREEN.y);
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(FREE_LAMP_SCREEN.x, FREE_LAMP_SCREEN.y);
        await driver.selectPartAt(FREE_DOMINO_SCREEN.x, FREE_DOMINO_SCREEN.y);
        await driver.setPartMode('move', 1);
        await driver.liftSelectedPart(LOWER_TILTED_TO_BENCH_PX, FREE_DOMINO_SCREEN.x, FREE_DOMINO_SCREEN.y);
        await driver.tiltSelectedByFineRotate(2);                        // 2 × 5° about Z
        await driver.connectActivation({ ...LOWERED_DOMINO_SCREEN, panel: 'domino' }, FREE_LAMP_SCREEN);

        await driver.run();
        const first = (await bodies(driver)).get('1')!;
        assert.ok(Math.abs(tiltDegrees(first) - 10) < 1, `The tile starts 10 deg off upright (got ${tiltDegrees(first).toFixed(2)} deg)`);
        let maxFromInitial = 0;
        const start = Date.now();
        while (Date.now() - start < 4000) {
            const b = (await bodies(driver)).get('1')!;
            maxFromInitial = Math.max(maxFromInitial, angleFrom(first, b));
            await driver.page.waitForTimeout(40);
        }
        const final = (await bodies(driver)).get('1')!;
        assert.ok(maxFromInitial < 45, `The tile never turns 45 deg from its admitted pose (max ${maxFromInitial.toFixed(1)} deg)`);
        assert.ok(tiltDegrees(final) < 2, `The tile settles upright (${tiltDegrees(final).toFixed(2)} deg)`);
        assert.equal(await driver.readLastAnimationSample('3'), null, 'The wired lamp (node 2 + 2 → target 3) never samples');
        assert.equal((await driver.readAllAnimationSamples()).length, 0, 'No cosmetic target samples at all');
        await driver.reset(500);
        assertCleanSlice();
    });

    test('4. Save / reload / Load restores the wired chain and solves; Reset rearms the sensor and a second Run solves again', { timeout: 150000 }, async () => {
        // Ordering dependency: a fresh page loads the construction test 1 saved to IndexedDB (four tiles, end → lamp wire).
        await driver.reload();
        await driver.selectLevel('domino_effect');
        await driver.load();
        assertEndRestored('loaded end Domino');
        await driver.run();
        const loaded = await bodies(driver);
        assert.deepEqual([...loaded.keys()].sort((a, b) => BigInt(a) < BigInt(b) ? -1 : BigInt(a) > BigInt(b) ? 1 : 0), [BALL_ID, END_ID, ...TILE_IDS], 'Load restores the ball, the end Domino and the four tiles');
        TILE_IDS.forEach((id, i) => assertUprightPlacement(loaded.get(id)!, CHAIN_X[i], `loaded tile ${id}`));
        assertEndDominoBeforeImpact(loaded.get(END_ID)!, 'loaded end Domino');
        assert.ok(await waitFor(async () => (await driver.readCaptured()) === 1, 10000), 'The loaded wired chain solves');
        assert.ok(await waitFor(async () => (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === HALF_ONE, 3000), 'The loaded lamp lights');
        const litSamples = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;

        await driver.reset(800);                                  // Reset; drain the retired world's in-flight lamp pulses
        assertEndRestored('reset end Domino');
        await driver.run();                                       // Run the restored construction
        const restored = await bodies(driver);
        TILE_IDS.forEach((id, i) => assertUprightPlacement(restored.get(id)!, CHAIN_X[i], `reset tile ${id}`));
        assertEndDominoBeforeImpact(restored.get(END_ID)!, 'reset end Domino');
        assert.ok(await waitFor(async () => (await driver.readCaptured()) === 2, 10000), 'The second Run solves again: the sensor was rearmed by Reset');
        assert.ok((await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length > litSamples, 'The lamp lights again in the new world');
        assert.ok(await waitFor(async () => (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === HALF_ONE, 3000), 'The lamp reaches full glow again');
        await driver.reset(500);
        assertCleanSlice();
    });
});
