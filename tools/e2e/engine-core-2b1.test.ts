// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b1.
// Verifies Host Validation & Error Lane Removal (Story 2.1):
// Direct SAB pose ring publishing without intermediate host shadow checking:
// 1. Real-time ball drop and Workbench interaction without host validation overhead or errors
// 2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) with direct pose ring publishing
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2b1: Host Validation & Error Lane Removal (Direct SAB Pose Publishing)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Real-time ball drop and Workbench interaction without host validation overhead or errors', { timeout: 120000 }, async () => {
        await driver.reload();

        // Listen for any transport or validation failure logged in console
        const errors: string[] = [];
        const errorHandler = (msg: any) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') || text.includes('Unhandled exception')) {
                errors.push(text);
            }
        };
        driver.page.on('console', errorHandler);

        // Select Basketball and place on canvas at (720, 485) -> (px ~ 0, py ~ 3.0, pz ~ 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485, 400);

        // Run simulation: ball drops from py ~ 3.0 m down to workbench floor
        await driver.toggleRun(0);

        let sequenceAdvanced = false;
        let initialPy = -1;
        let finalPy = -1;
        let initialSequence = 0n;
        let latestSequence = 0n;
        const startTime = Date.now();

        while (Date.now() - startTime < 3000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const seq = BigInt(pose.sequence);
                if (initialSequence === 0n && seq > 0n) {
                    initialSequence = seq;
                    initialPy = pose.bodies[0].py;
                }
                if (seq > initialSequence) {
                    sequenceAdvanced = true;
                    latestSequence = seq;
                    finalPy = pose.bodies[0].py;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        // Reset simulation back to Build Mode
        await driver.toggleRun(300);

        driver.page.off('console', errorHandler);

        assert.equal(errors.length, 0, `Zero host validation/transport errors expected, got: ${errors.join('; ')}`);
        assert.ok(sequenceAdvanced, `Pose ring sequence must advance steadily (from ${initialSequence} to ${latestSequence})`);
        assert.ok(initialPy > 2.0, `Ball must start near py ~ 3.0 m (got initial py=${initialPy.toFixed(3)})`);
        assert.ok(finalPy < 1.0, `Ball must fall towards workbench floor (got final py=${finalPy.toFixed(3)})`);
    });

    test('2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) with direct pose ring publishing', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Place Ramp 1 at (498, 404) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2 at (674, 509) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -20);

        // Start simulation: ball rolls down Ramp 1, transitions to Ramp 2, and enters Receiver
        await driver.toggleRun(0);

        let captured = 0;
        const solveStartTime = Date.now();
        while (Date.now() - solveStartTime < 8000) {
            captured = await driver.readCaptured();
            if (captured > 0) break;
            await driver.page.waitForTimeout(300);
        }

        const solvedPose = await driver.readLatestPose();
        assert.ok(solvedPose && solvedPose.bodies.length > 0, 'Pose slot must be readable');

        // Reset simulation back to Build Mode
        await driver.toggleRun(300);

        assert.ok(captured > 0, `Receiver must capture ball via direct pose publishing (captured count=${captured})`);
        assert.ok(
            solvedPose.bodies[0].px > 1.8 && solvedPose.bodies[0].px < 3.2,
            `Ball must end inside Receiver horizontal range [1.8, 3.2] (got px=${solvedPose.bodies[0].px.toFixed(3)})`
        );
        assert.ok(
            solvedPose.bodies[0].py < 2.0,
            `Ball must settle inside Receiver basket (got py=${solvedPose.bodies[0].py.toFixed(3)})`
        );
    });

    test('3. Exact Reset and Save/Load persistence roundtrip in Chrome', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Place Ramp 1
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -20);

        // Save construction
        await driver.save();

        // Run simulation until solved
        await driver.toggleRun(0);
        const runStartTime = Date.now();
        while (Date.now() - runStartTime < 6000) {
            if (await driver.readCaptured() > 0) break;
            await driver.page.waitForTimeout(300);
        }

        // Reset simulation: ball must return to starting elevation py ~ 6.5
        await driver.toggleRun(500);

        // Run briefly to verify reset starting state
        await driver.toggleRun(100);
        const resetPose = await driver.readLatestPose();
        assert.ok(resetPose && resetPose.bodies.length > 0, 'Pose slot must be readable after reset');
        assert.ok(
            resetPose.bodies[0].py > 5.5,
            `Reset must restore ball to starting elevation py > 5.5 m (got py=${resetPose.bodies[0].py.toFixed(3)})`
        );
        await driver.toggleRun(300);

        // Reload page to start with blank First Principles state
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Load saved construction
        await driver.load();

        // Run loaded construction: verify it solves the level
        await driver.toggleRun(0);
        let loadedCaptured = 0;
        const loadRunStartTime = Date.now();
        while (Date.now() - loadRunStartTime < 8000) {
            loadedCaptured = await driver.readCaptured();
            if (loadedCaptured > 0) break;
            await driver.page.waitForTimeout(300);
        }

        await driver.toggleRun(300);

        assert.ok(
            loadedCaptured > 0,
            `Loaded construction must solve level and capture ball in receiver (got captured=${loadedCaptured})`
        );
    });
});
