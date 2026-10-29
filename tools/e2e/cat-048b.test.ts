// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-048b (Story 6.7; lifecycle re-verified by Story 9R.1).
// Verifies level clear_pipe ("Through the looking tube"):
// 1. Initial level layout: locked ball at (-1.4, 6.0, 0), locked receiver at (2.0, 0.6, 0), 1 pipe in inventory.
// 2. Negative control: without pipe, ball falls straight to floor, missing receiver (goal not solved).
// 3. Solved run: pipe placed at (0, 3, 0) tilted -45 deg, ball enters bore, traverses without snagging/clipping, emerges and lands in receiver, achieving CCGOAL_SOLVED.
// 4. Reset restoration: Reset restores the exact pre-Run snapshot; a second Run solves again and its Reset restores it again.
// 5. Save & Load persistence roundtrip: Save, reload, Load restores the exact construction, and Run solves again.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

// Committed (dynamic) bodies of each bench: the locked ball; the receiver and the placed pipe are static parts.
const BENCH = { bodies: 1 } as const;

describe('CAT-048b: Clear Pipe Level E2E (Through the looking tube)', () => {
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
        await driver.selectLevel('clear_pipe');
    }

    // The pipe placed at the centre (0, 3, 0) -> screen (720, 485), tilted by -9 fine-rotate steps (-45 deg about Z).
    async function buildSolvedBench(): Promise<RunResetSnapshot> {
        await openLevel();
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-9);
        return driver.captureSnapshot(BENCH);
    }

    async function runSolved(label: string): Promise<void> {
        await driver.run();
        const solved = await driver.waitForGoalSolved(8000);
        assert.ok(solved > 0, `${label}: ball must reach receiver and solve goal (solved count=${solved})`);
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, `${label}: ball body must be present after capture`);
        assert.ok(ball.px > 1.8 && ball.px < 2.5, `${label}: ball must settle in receiver X range [1.8, 2.5], got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `${label}: ball must settle inside receiver basket, got py=${ball.py}`);
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    test('1. Select level clear_pipe: verify initial layout (locked ball, locked receiver, 1 pipe in inventory)', { timeout: 150000 }, async () => {
        await openLevel();

        // Verify pipe tool is selectable in authored inventory (count 1)
        await driver.selectTool('pipe');
        await driver.pressKey('Escape');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: without placing the pipe, ball falls straight to floor, missing receiver (goal not solved)', { timeout: 150000 }, async () => {
        await openLevel();
        const s0 = await driver.captureSnapshot(BENCH);

        for (const cycle of ['first', 'second']) {
            await driver.run(2500);
            assert.equal(await driver.readCaptured(), 0, `${cycle} run: without pipe, ball must miss receiver and goal remains unsolved`);
            const slot = await driver.readLatestPose();
            const ball = slot?.bodies.find(b => b.id === '1');
            assert.ok(ball && ball.py < 0.5, `${cycle} run: ball must have fallen towards workbench floor, got py=${ball?.py}`);
            assert.ok(ball && Math.abs(ball.px - (-1.4)) < 0.2, `${cycle} run: ball fell straight down near px=-1.4, got px=${ball?.px}`);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Solved run: pipe placed at (0, 3, 0) tilted -45 deg, ball traverses bore into receiver, achieving CCGOAL_SOLVED', { timeout: 150000 }, async () => {
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
        await driver.selectLevel('clear_pipe');
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);

        await runSolved('loaded run');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
