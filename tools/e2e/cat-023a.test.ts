// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-023a (Story 5.1).
// Verifies the Domino as a dynamic box rigid body on the shared solver:
// 1. Negative control / upright rest: a Domino lowered onto the bench through the real move gizmo stands for 3 s within the rest tolerances
// 2. Struck by a Basketball dropped onto its upper third it topples past 60 deg and settles flat without jitter
// 3. Reset restores the placed upright pose exactly and the next Run topples it again
// 4. Save / reload / Load restores the Domino kind and pose and the loaded construction behaves the same
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

const BENCH_Y = -0.46;
const HALF_HEIGHT = 0.55;
const HALF_WIDTH = 0.125;
const DOMINO_SCREEN = { x: 720, y: 485 };          // free workshop placement plane (y = 3 m) at x = 0, z = 0
const BALL_SCREEN = { x: 742, y: 492 };            // x = 0.4 m on the same plane: the ball's edge overlaps the tile's upper corner
const LOWER_TO_BENCH_PX = -162;                    // move-gizmo drag (about 56 px/m) that lowers the tile from 3 m to just above the bench
const DOMINO_ID = '1';
const BALL_ID = '2';

function tiltDegrees(b: WorkshopBodyPose): number {
    // Angle between the tile's local +Y axis and world up.
    const upY = 1 - 2 * (b.qx * b.qx + b.qz * b.qz);
    return Math.acos(Math.max(-1, Math.min(1, upY))) * 180 / Math.PI;
}

function speed(b: WorkshopBodyPose): number {
    return Math.hypot(b.vx, b.vy, b.vz);
}

function quaternionDelta(a: WorkshopBodyPose, b: WorkshopBodyPose): number {
    return Math.hypot(a.qx - b.qx, a.qy - b.qy, a.qz - b.qz, a.qw - b.qw);
}

async function body(driver: WorkshopDriver, id: string): Promise<WorkshopBodyPose> {
    const pose = await driver.readLatestPose();
    assert.ok(pose, 'Pose slot must be readable');
    const found = pose.bodies.find(b => b.id === id);
    assert.ok(found, `Body ${id} must be published (got ${pose.bodies.map(b => b.id).join(',')})`);
    return found;
}

async function sampleUntil(driver: WorkshopDriver, id: string, predicate: (b: WorkshopBodyPose) => boolean, timeoutMs: number): Promise<WorkshopBodyPose | null> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        const b = await body(driver, id);
        if (predicate(b)) return b;
        await driver.page.waitForTimeout(50);
    }
    return null;
}

// Place a Domino at the placement plane and lower it onto the bench with the real move gizmo (no setters, no numeric menus).
async function placeDominoOnBench(driver: WorkshopDriver): Promise<void> {
    await driver.selectTool('domino');
    await driver.placeOnCanvas(DOMINO_SCREEN.x, DOMINO_SCREEN.y);
    await driver.selectPartAt(DOMINO_SCREEN.x, DOMINO_SCREEN.y);
    await driver.setPartMode('move', 1); // the Domino's "Connect ActivationOut" row sits above the dock since CAT-023b
    await driver.liftSelectedPart(LOWER_TO_BENCH_PX, DOMINO_SCREEN.x, DOMINO_SCREEN.y);
}

async function assertUprightRest(driver: WorkshopDriver, label: string): Promise<WorkshopBodyPose> {
    const rest = await body(driver, DOMINO_ID);
    assert.ok(tiltDegrees(rest) < 2, `${label}: tilt ${tiltDegrees(rest).toFixed(3)} deg must stay below 2 deg`);
    assert.ok(Math.hypot(rest.px, rest.pz) < 0.005, `${label}: drift ${Math.hypot(rest.px, rest.pz).toFixed(4)} m must stay below 5 mm`);
    assert.ok(speed(rest) < 0.05, `${label}: speed ${speed(rest).toFixed(4)} m/s must stay below 0.05`);
    assert.ok(Math.abs(rest.py - (BENCH_Y + HALF_HEIGHT)) < 0.0005, `${label}: py ${rest.py.toFixed(5)} must sit at half-height above the bench within 0.5 mm`);
    return rest;
}

describe('CAT-023a: Dynamic box rigid body & upright stability (Domino)', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') || text.includes('NaN')) errors.push(text);
        });
        driver.page.on('pageerror', (error: Error) => errors.push(error.message));
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Negative control: an upright Domino alone on the bench stands still for 3 s within the rest tolerances', { timeout: 120000 }, async () => {
        await placeDominoOnBench(driver);

        await driver.run();
        const first = await sampleUntil(driver, DOMINO_ID, () => true, 5000);
        assert.ok(first, 'The Domino publishes through the pose ring once Run starts');
        assert.ok(first.py < 0.3 && first.py > BENCH_Y + HALF_HEIGHT - 0.01, `Domino starts just above the bench (got py=${first.py.toFixed(3)})`);
        assert.equal(tiltDegrees(first), 0, 'The placed tile is exactly upright');
        await driver.page.waitForTimeout(3000);
        await assertUprightRest(driver, 'after 3 s');
        const pose = await driver.readLatestPose();
        assert.equal(pose!.bodies.length, 1, 'No other body is published: nothing can tilt the tile');
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Struck by a Basketball dropped onto its upper third the Domino topples past 60 deg and settles flat without jitter', { timeout: 150000 }, async () => {
        await driver.reload();
        await placeDominoOnBench(driver);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(BALL_SCREEN.x, BALL_SCREEN.y);
        await driver.save();

        await driver.run();
        const ball = await body(driver, BALL_ID);
        assert.ok(Math.abs(ball.px - 0.4) < 0.06 && ball.py > 2.0, `Ball starts beside the tile at x≈0.4 m, 3 m up (got ${ball.px.toFixed(2)}, ${ball.py.toFixed(2)})`);
        const toppling = await sampleUntil(driver, DOMINO_ID, b => tiltDegrees(b) > 60, 4000);
        assert.ok(toppling, 'Tilt must pass 60 deg within the 4 s strike window');

        // Settle: tilt near 90 deg, lying at half-width above the bench, and no frame-to-frame orientation jitter for 1 s.
        await driver.page.waitForTimeout(2500);
        const samples: WorkshopBodyPose[] = [];
        const start = Date.now();
        while (Date.now() - start < 1000) {
            samples.push(await body(driver, DOMINO_ID));
            await driver.page.waitForTimeout(40);
        }
        const final = samples[samples.length - 1];
        assert.ok(Math.abs(tiltDegrees(final) - 90) < 5, `Settled tilt ${tiltDegrees(final).toFixed(2)} deg must be near 90 deg`);
        assert.ok(Math.abs(final.py - (BENCH_Y + HALF_WIDTH)) < 0.005, `Lying py ${final.py.toFixed(4)} must sit at half-width above the bench`);
        assert.ok(speed(final) < 0.05, `Settled speed ${speed(final).toFixed(4)} m/s`);
        let maxDelta = 0;
        for (let i = 1; i < samples.length; i++) maxDelta = Math.max(maxDelta, quaternionDelta(samples[i - 1], samples[i]));
        assert.ok(samples.length >= 10, `Sampled the settled second (${samples.length} reads)`);
        assert.ok(maxDelta < 0.002, `Frame-to-frame orientation change ${maxDelta.toExponential(2)} stays below the jitter bound for 1 s`);
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Reset restores the placed upright pose exactly and the next Run topples the tile again', { timeout: 150000 }, async () => {
        // Self-contained: a fresh page loads the construction saved in test 2 (Domino on the bench, ball above its upper corner).
        await driver.reload();
        await driver.load();
        await driver.run();
        const toppledFirst = await sampleUntil(driver, DOMINO_ID, b => tiltDegrees(b) > 60, 4000);
        assert.ok(toppledFirst, 'The loaded tile topples on the first Run');
        await driver.reset(); // Reset
        await driver.run();   // Run the restored construction
        const restored = await sampleUntil(driver, DOMINO_ID, () => true, 5000);
        assert.ok(restored, 'The reset construction publishes again on Run');
        assert.deepEqual([restored.qx, restored.qy, restored.qz, restored.qw], [0, 0, 0, 1], 'Reset restores the identity orientation bit-for-bit');
        assert.deepEqual([restored.px, restored.pz], [0, 0], 'Reset restores the placed x/z exactly');
        assert.ok(restored.py > BENCH_Y + HALF_HEIGHT - 0.01 && restored.py < 0.3, `Reset restores the placed height (got py=${restored.py.toFixed(3)})`);
        const toppled = await sampleUntil(driver, DOMINO_ID, b => tiltDegrees(b) > 60, 4000);
        assert.ok(toppled, 'The restored tile topples again when struck');
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Save / reload / Load restores the Domino kind and pose and the loaded construction topples the same way', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.load();
        await driver.run();
        const loaded = await sampleUntil(driver, DOMINO_ID, () => true, 5000);
        assert.ok(loaded, 'The loaded Domino publishes on Run');
        const pose = await driver.readLatestPose();
        assert.deepEqual(pose!.bodies.map(b => b.id).sort(), [DOMINO_ID, BALL_ID], 'Load restores both the Domino and the Basketball');
        assert.deepEqual([loaded.qx, loaded.qy, loaded.qz, loaded.qw], [0, 0, 0, 1], 'Loaded Domino keeps its upright orientation');
        assert.deepEqual([loaded.px, loaded.pz], [0, 0], 'Loaded Domino keeps its placed x/z');
        assert.ok(loaded.py > BENCH_Y + HALF_HEIGHT - 0.01 && loaded.py < 0.3, `Loaded Domino keeps its placed height (got py=${loaded.py.toFixed(3)})`);
        const toppled = await sampleUntil(driver, DOMINO_ID, b => tiltDegrees(b) > 60, 4000);
        assert.ok(toppled, 'The loaded tile is a dynamic box: the ball topples it');
        await driver.page.waitForTimeout(2500);
        const final = await body(driver, DOMINO_ID);
        assert.ok(Math.abs(tiltDegrees(final) - 90) < 5, `Loaded tile settles near 90 deg (got ${tiltDegrees(final).toFixed(2)})`);
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
