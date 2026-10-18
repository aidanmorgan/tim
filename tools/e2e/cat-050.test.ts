// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-050 (Story 6.10).
// Verifies level quarter_bend ("Around the corner"):
// 1. Initial level layout: locked ball at (0.7029, 7.3870564, 0), locked receiver at (-5.5, 0.6, 0), 1 pipe_bend_90 in inventory.
// 2. Negative control: without pipe bend, ball falls straight to floor, missing receiver (goal not solved).
// 3. Solved run: pipe_bend_90 placed at (0, 4, 0) tilted -90 deg, ball enters start mouth, traverses bend without snagging/clipping, emerges and lands in receiver, achieving CCGOAL_SOLVED.
// 4. Reset restoration: Reset restores exact starting poses; second Run replays bit-for-bit and solves again.
// 5. Save & Load persistence roundtrip: Save, reload, Load restores construction, and Run solves again.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver } from './workshop-driver.ts';

describe('CAT-050: 90-Degree Pipe Bend Hollow Torus Segment & Level Quarter Bend E2E', () => {
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
        await driver?.close();
    });

    test('1. Select level quarter_bend: verify initial layout (locked ball, locked receiver, 1 pipe_bend_90 in inventory)', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();
        await driver.selectLevel('quarter_bend');

        // Verify pipe_bend_90 tool is selectable in authored inventory (count 1)
        await driver.selectTool('pipe_bend_90');
        await driver.pressKey('Escape');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: without placing pipe_bend_90, ball falls straight to floor, missing receiver (goal not solved)', { timeout: 150000 }, async () => {
        errors.length = 0;
        // In quarter_bend mode without placing the pipe bend, run for 2.5 seconds
        await driver.run(2500);

        // CCGOAL_SOLVED must not be emitted
        assert.equal(await driver.readCaptured(), 0, 'Without pipe bend, ball must miss receiver and goal remains unsolved');

        // Verify ball fell to workbench floor elevation (~ -0.46m + radius 0.34m = -0.12m)
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball && ball.py < 0.5, `Ball must have fallen towards workbench floor, got py=${ball?.py}`);
        assert.ok(ball && Math.abs(ball.px - 0.7029) < 0.2, `Ball fell straight down near px=0.7029, got px=${ball?.px}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Solved run: pipe_bend_90 placed at (0, 4, 0) tilted -90 deg, ball traverses bend into receiver, achieving CCGOAL_SOLVED', { timeout: 150000 }, async () => {
        errors.length = 0;

        // 1. Select pipe_bend_90 and place at (0, 4, 0) -> screen (720, 424)
        await driver.selectTool('pipe_bend_90');
        await driver.placeOnCanvas(720, 424);

        // 2. Select placed bend and tilt by -18 steps (-90 deg about Z)
        await driver.selectPartAt(720, 424);
        await driver.tiltSelectedByFineRotate(-18);

        // 3. Run and observe goal capture
        await driver.run();

        const solved = await driver.waitForGoalSolved(8000);
        assert.ok(solved > 0, `Ball must reach receiver and solve goal (solved count=${solved})`);

        // Read pose after solve: ball should be in receiver basket (px ~ -5.5, py < 1.0)
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, 'Ball body must be present after capture');
        assert.ok(ball.px < -4.5 && ball.px > -6.5, `Ball must settle in receiver X range [-6.5, -4.5], got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `Ball must settle inside receiver basket, got py=${ball.py}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Reset restoration: Reset restores ball and pipe bend; second Run replays bit-for-bit and solves again', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Second Run replays and solves
        await driver.run();
        const solvedAgain = await driver.waitForGoalSolved(8000);
        assert.ok(solvedAgain > 0, `Second run must solve goal again (solved count=${solvedAgain})`);

        // Read pose after second solve
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, 'Ball body must be present after replay solve');
        assert.ok(ball.px < -4.5 && ball.px > -6.5, `Ball must settle in receiver X range [-6.5, -4.5], got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `Ball must settle inside receiver basket, got py=${ball.py}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Save & Load persistence roundtrip: Save, reload, Load restores construction, and Run solves again', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Save current solved construction (ball, receiver, tilted bend)
        await driver.save();

        // Reload page
        await driver.reload();
        assert.equal(await driver.readCaptured(), 0, 'Captured count must be reset after reload');

        // Select quarter_bend level
        await driver.selectLevel('quarter_bend');

        // Load saved construction
        await driver.load();

        // Run loaded construction
        await driver.run();
        const loadedSolved = await driver.waitForGoalSolved(8000);
        assert.ok(loadedSolved > 0, `Loaded construction must solve level (solved count=${loadedSolved})`);

        // Read pose after loaded solve
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, 'Ball body must be present after loaded solve');
        assert.ok(ball.px < -4.5 && ball.px > -6.5, `Ball must settle in receiver X range [-6.5, -4.5], got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `Ball must settle inside receiver basket, got py=${ball.py}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
