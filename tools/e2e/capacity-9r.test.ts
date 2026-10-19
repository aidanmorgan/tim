// Story 9R.2: the collider table is sized at Run from the compiled construction, so a full bench of bends runs.
// 1. Thirty-two pipe_bend_90 (32 x 72 + plane = 2,305 colliders, beyond the retired 2,048 ceiling) placed through the real drawer:
//    Run, Reset and a reload + Load each leave the committed read and the authored construction exact, with no worker fault.
// 2. Thirty bends, a cradle bend (rotated -135 degrees about Z) and a Basketball inside it (2,234 colliders): the ball rests on the
//    cradle's colliders, whose slots lie beyond 2,048; Run/Reset and Save/Load use the Story 9R.1 snapshot (one committed body).
// 3. Control: the same bench without the cradle bend; the ball falls to the bench.
// Every leg fails on a worker or console error, and WorkshopDriver.close() fails it on any session fault (debt fault, clock
// recalibration, rejected clock reply).
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores, assertSnapshotEqual, readSnapshot } from './run-reset-snapshot.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';

// Free-workshop camera (workshop-driver.ts FREE_VIEW_PX_PER_M): a point (x, 3, z) on the placement plane projects to the screen
// offset (53.8 x - 36.8 z, 17.0 x + 24.8 z) from (720, 485).
const screenOf = (x: number, z: number): { x: number; y: number } =>
    ({ x: Math.round(720 + 53.8 * x - 36.8 * z), y: Math.round(485 + 17.0 * x + 24.8 * z) });

// Thirty bends in a six-by-five grid on the left of the bench (x -6..-1 m, z -3..3 m).
const GRID = Array.from({ length: 30 }, (_, i) => screenOf(-6 + i % 6, -3 + Math.floor(i / 6) * 1.5));
// The two further bends of the full bench, and the cradle that holds the ball.
const FULL_EXTRA = [screenOf(3, -2), screenOf(3, 2)];
const CRADLE = screenOf(6, 0);
// -135 degrees about Z in fine-rotate steps of -5 degrees (cat-050: -18 steps is -90 degrees).
const CRADLE_STEPS = -27;
// The ball resting inside the cradle's tube (native: WorkbenchCapacityTests, 2.677 m), and the bench top (-0.46 m).
const CRADLE_REST = { min: 2.5, max: 2.9 } as const;

describe('Story 9R.2: a full bench of bends compiles and runs', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    const watch = (source: string) => (message: ConsoleMessage): void => {
        if (isWorkshopConsoleFailure(message.type(), message.text())) errors.push(`${source}: ${message.text().slice(0, 400)}`);
    };

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', watch('page'));
        driver.page.on('pageerror', (error: Error) => errors.push(`page error: ${String(error).slice(0, 400)}`));
        driver.page.on('worker', (worker: Worker) => worker.on('console', watch(`worker ${worker.url()}`)));
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    async function placeBends(points: readonly { x: number; y: number }[]): Promise<void> {
        for (const point of points) {
            await driver.selectTool('pipe_bend_90');
            await driver.placeOnCanvas(point.x, point.y);
        }
    }

    async function savedPartIds(): Promise<string[]> {
        const { slot } = await driver.save();
        const construction = decodeSavedConstruction(slot).construction();
        assert.ok(construction, 'The saved slot must hold a construction');
        return Array.from({ length: construction.instancesLength() }, (_, i) => construction.instances(i)!.partId() ?? '');
    }

    // A static bench has no committed body, so the Story 9R.1 snapshot (which requires one) does not apply: Reset is compared on
    // the Save acknowledgement's committed read and the stored slot, byte for byte.
    async function staticSnapshot(): Promise<{ read: RunResetSnapshot; slot: Uint8Array }> {
        const { acknowledgement, slot } = await driver.save();
        return { read: readSnapshot(acknowledgement), slot };
    }

    // Runs for a window, then pauses through the menu: a session that faulted while running rejects the Pause (WrongPhase).
    async function runFor(label: string, windowMs: number): Promise<void> {
        const before = await driver.readLatestPose();
        await driver.run(windowMs);
        const during = await driver.readLatestPose();
        assert.ok(before && during && BigInt(during.sequence) > BigInt(before.sequence),
            `${label}: the running world keeps publishing (pose ring sequence ${before?.sequence} -> ${during?.sequence})`);
        await driver.pauseSimulation();
    }

    test('1. Thirty-two pipe_bend_90 (2,305 colliders) run, reset exactly and save/load with no worker fault', { timeout: 600000 }, async () => {
        errors.length = 0;
        await driver.reload();
        await placeBends([...GRID, ...FULL_EXTRA]);
        assert.deepEqual(await savedPartIds(), Array(32).fill('pipe_bend_90'), 'The bench holds exactly 32 pipe_bend_90');
        const s0 = await staticSnapshot();
        assert.equal(s0.read.bodies, 0, 'A bench of bends has no moving body');

        for (const cycle of ['first', 'second']) {
            await runFor(`${cycle} run`, 2000);
            await driver.reset();
            const s1 = await staticSnapshot();
            assertSnapshotEqual(s0.read, s1.read, `${cycle} Reset`);
            assert.deepEqual(s1.slot, s0.slot, `${cycle} Reset: the stored construction is unchanged`);
        }

        await driver.reload();
        await driver.load();
        assert.deepEqual(await savedPartIds(), Array(32).fill('pipe_bend_90'), 'Load restores the 32 bends');
        const loaded = await staticSnapshot();
        assertSnapshotEqual(s0.read, loaded.read, 'Load');
        const original = decodeSavedConstruction(s0.slot).construction()!;
        const restored = decodeSavedConstruction(loaded.slot).construction()!;
        for (let i = 0; i < 32; i++) {
            const a = original.instances(i)!; const b = restored.instances(i)!;
            assert.deepEqual([b.id(), b.cell()!.x(), b.cell()!.y(), b.cell()!.z(), b.rotation()!.z(), b.rotation()!.w()],
                [a.id(), a.cell()!.x(), a.cell()!.y(), a.cell()!.z(), a.rotation()!.z(), a.rotation()!.w()], `Load restores bend ${i}`);
        }
        await runFor('loaded run', 2000);
        await driver.reset();
        assertSnapshotEqual(loaded.read, (await staticSnapshot()).read, 'loaded Reset');
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });

    async function cradleBench(withCradle: boolean): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await placeBends(GRID);
        if (withCradle) {
            await placeBends([CRADLE]);
            await driver.selectPartAt(CRADLE.x, CRADLE.y);
            await driver.tiltSelectedByFineRotate(CRADLE_STEPS);
        }
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(CRADLE.x, CRADLE.y);
        return driver.captureSnapshot({ bodies: 1 });
    }

    async function ballHeightAfterRun(label: string): Promise<number> {
        await driver.run(2500);
        const pose = await driver.readLatestPose();
        assert.equal(pose?.bodies.length, 1, `${label}: the Basketball is the one committed body`);
        return pose!.bodies[0].py;
    }

    test('2. Thirty bends, a cradle bend and a Basketball (2,234 colliders): the ball rests in the cradle; Run/Reset and Save/Load are exact', { timeout: 600000 }, async () => {
        const s0 = await cradleBench(true);
        for (const cycle of ['first', 'second']) {
            const height = await ballHeightAfterRun(`${cycle} run`);
            assert.ok(height > CRADLE_REST.min && height < CRADLE_REST.max,
                `${cycle} run: the ball rests inside the cradle bend (height ${height} m)`);
            await driver.expectRunAdvanced(s0);
            await driver.reset();
            await driver.expectResetMatches(s0);
        }
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot({ bodies: 1 });
        assertLoadRestores(s0, loaded);
        const height = await ballHeightAfterRun('loaded run');
        assert.ok(height > CRADLE_REST.min && height < CRADLE_REST.max, `loaded run: the ball rests inside the cradle (height ${height} m)`);
        await driver.expectRunAdvanced(loaded);
        await driver.reset();
        await driver.expectResetMatches(loaded);
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });

    test('3. Control: without the cradle bend the ball falls to the bench', { timeout: 600000 }, async () => {
        const s0 = await cradleBench(false);
        const height = await ballHeightAfterRun('control run');
        assert.ok(height < 0, `Without the cradle the ball falls to the bench (height ${height} m)`);
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        await driver.expectResetMatches(s0);
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });
});
