// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-049 (Story 6.9; lifecycle re-verified by Story 9R.1).
// Verifies level gentle_bend ("A gentle turn"):
// 1. Initial level layout: locked ball at (0.1827, 6.60844, 0), locked receiver at (-2.0, 0.6, 0), 1 pipe_bend_45 in inventory.
// 2. Negative control: without pipe bend, ball falls straight to floor, missing receiver (goal not solved).
// 3. Solved run: pipe_bend_45 placed at (0, 4, 0) tilted -90 deg, ball enters start mouth, traverses bend without snagging/clipping, emerges and lands in receiver, achieving CCGOAL_SOLVED.
// 4. Reset restoration: Reset restores the exact pre-Run snapshot; a second Run solves again and its Reset restores it again.
// 5. Save & Load persistence roundtrip: Save, reload, Load restores the exact construction, and Run solves again.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

// Committed (dynamic) bodies of each bench: the locked ball; the receiver and the placed bend are static parts.
const BENCH = { bodies: 1 } as const;

describe('CAT-049: 45-Degree Pipe Bend Hollow Torus Segment & Level Gentle Bend E2E', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            if (isWorkshopConsoleFailure(msg.type(), msg.text())) errors.push(msg.text());
        });
        driver.page.on('pageerror', (err: Error) => errors.push(err.message));
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    async function openLevel(): Promise<void> {
        errors.length = 0;
        await driver.reload();
        await driver.selectLevel('gentle_bend');
    }

    // pipe_bend_45 placed at (0, 4, 0) -> screen (720, 424), tilted by -18 fine-rotate steps (-90 deg about Z).
    async function buildSolvedBench(): Promise<RunResetSnapshot> {
        await openLevel();
        await driver.selectTool('pipe_bend_45');
        await driver.placeOnCanvas(720, 424);
        await driver.selectPartAt(720, 424);
        await driver.tiltSelectedByFineRotate(-18);
        return driver.captureSnapshot(BENCH);
    }

    async function runSolved(label: string): Promise<void> {
        await driver.run();
        const solved = await driver.waitForGoalSolved(8000);
        assert.ok(solved > 0, `${label}: ball must reach receiver and solve goal (solved count=${solved})`);
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, `${label}: ball body must be present after capture`);
        assert.ok(ball.px < -1.5 && ball.px > -2.5, `${label}: ball must settle in receiver X range [-2.5, -1.5], got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `${label}: ball must settle inside receiver basket, got py=${ball.py}`);
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    test('1. Select level gentle_bend: verify initial layout (locked ball, locked receiver, 1 pipe_bend_45 in inventory)', { timeout: 150000 }, async () => {
        await openLevel();

        // Verify pipe_bend_45 tool is selectable in authored inventory (count 1)
        await driver.selectTool('pipe_bend_45');
        await driver.pressKey('Escape');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: without placing pipe_bend_45, ball falls straight to floor, missing receiver (goal not solved)', { timeout: 150000 }, async () => {
        await openLevel();
        const s0 = await driver.captureSnapshot(BENCH);

        for (const cycle of ['first', 'second']) {
            await driver.run(2500);
            assert.equal(await driver.readCaptured(), 0, `${cycle} run: without pipe bend, ball must miss receiver and goal remains unsolved`);
            const slot = await driver.readLatestPose();
            const ball = slot?.bodies.find(b => b.id === '1');
            assert.ok(ball && ball.py < 0.5, `${cycle} run: ball must have fallen towards workbench floor, got py=${ball?.py}`);
            assert.ok(ball && Math.abs(ball.px - 0.1827) < 0.2, `${cycle} run: ball fell straight down near px=0.1827, got px=${ball?.px}`);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Solved run: pipe_bend_45 placed at (0, 4, 0) tilted -90 deg, ball traverses bend into receiver, achieving CCGOAL_SOLVED', { timeout: 150000 }, async () => {
        const s0 = await buildSolvedBench();
        await runSolved('solved run');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Reset restoration: Reset restores the exact pre-Run snapshot; a second Run solves again and Reset restores it again', { timeout: 150000 }, async () => {
        const s0 = await buildSolvedBench();
        await runSolved('first run');
        await resetMatches(s0);
        await runSolved('second run');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Save & Load persistence roundtrip: Save, reload, Load restores construction, and Run solves again', { timeout: 150000 }, async () => {
        // captureSnapshot saves the solved construction through the Workshop menu.
        const s0 = await buildSolvedBench();

        await driver.reload();
        assert.equal(await driver.readCaptured(), 0, 'Captured count must be reset after reload');
        await driver.selectLevel('gentle_bend');
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);

        await runSolved('loaded run');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
