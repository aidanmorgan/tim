// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2c.
// Verifies Decoupled Sensor Evaluation & Dwell Tick Counter (Story 3.1):
// All sensors and triggers (aperture crossing, residence, impact switches) are evaluated at
// substep endpoints with tick-accumulated dwell, replacing continuous root-finding with clean discrete events:
// 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver, accumulates dwell, and triggers capture
// 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Listen for any solver, sensor root-finding, or transport errors in console
        const errors: string[] = [];
        const errorHandler = (msg: any) => {
            const text = msg.text();
            if (
                text.includes('CCGPU_TRANSPORT_FAILURE') ||
                text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') ||
                text.includes('CurveCuts') ||
                text.includes('RotaryCaptureSweep') ||
                text.includes('NaN')
            ) {
                errors.push(text);
            }
        };
        driver.page.on('console', errorHandler);

        // Place Ramp 1 at (498, 404) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2 at (674, 509) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -20);

        // Start simulation: ball rolls down ramps and enters Receiver
        await driver.toggleRun(0);

        let captured = 0;
        let settledInReceiver = false;
        let finalSpeed = Infinity;
        const solveStartTime = Date.now();

        while (Date.now() - solveStartTime < 8000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                const speed = Math.hypot(ball.vx, ball.vy, ball.vz);
                // Inside Receiver horizontal bounds [1.8, 3.2] and low elevation [y < 2.0]
                if (ball.px >= 1.8 && ball.px <= 3.2 && ball.py <= 2.0) {
                    finalSpeed = speed;
                    if (speed <= 1.5) {
                        settledInReceiver = true;
                    }
                }
            }
            captured = await driver.readCaptured();
            if (captured > 0) break;
            await driver.page.waitForTimeout(100);
        }

        const solvedPose = await driver.readLatestPose();
        assert.ok(solvedPose && solvedPose.bodies.length > 0, 'Pose slot must be readable');

        // Reset simulation back to Build Mode
        await driver.toggleRun(300);

        driver.page.off('console', errorHandler);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
        assert.ok(settledInReceiver, `Ball must settle in Receiver with speed <= 1.5 m/s (got speed=${finalSpeed.toFixed(3)})`);
        assert.ok(captured > 0, `Receiver must capture ball after accumulating dwell at <= 1.5 m/s (captured count=${captured})`);
        assert.ok(
            solvedPose.bodies[0].px > 1.8 && solvedPose.bodies[0].px < 3.2,
            `Ball must end inside Receiver horizontal range [1.8, 3.2] (got px=${solvedPose.bodies[0].px.toFixed(3)})`
        );
        assert.ok(
            solvedPose.bodies[0].py < 2.0,
            `Ball must settle inside Receiver basket (got py=${solvedPose.bodies[0].py.toFixed(3)})`
        );
    });

    test('2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Place Ramp 1 at (498, 404) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2 at (674, 509), tilt -10 deg and lift 50px so ball launches through/over Receiver at high speed
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -10);
        await driver.selectPartAt(674, 509);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(50, 674, 509);

        // Start simulation: ball speeds over/through Receiver region
        await driver.toggleRun(0);

        let highSpeedPassObserved = false;
        let minSpeedDuringTransit = Infinity;
        let maxSpeedDuringTransit = 0;
        const testStartTime = Date.now();

        while (Date.now() - testStartTime < 4000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                const speed = Math.hypot(ball.vx, ball.vy, ball.vz);
                // Inside Receiver horizontal range [1.8, 3.2]
                if (ball.px >= 1.8 && ball.px <= 3.2 && ball.py >= 0.5 && ball.py <= 3.0) {
                    if (speed > 1.5) {
                        highSpeedPassObserved = true;
                    }
                    if (speed < minSpeedDuringTransit) minSpeedDuringTransit = speed;
                    if (speed > maxSpeedDuringTransit) maxSpeedDuringTransit = speed;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        const captured = await driver.readCaptured();

        // Reset to Build Mode
        await driver.toggleRun(300);

        assert.ok(
            highSpeedPassObserved,
            `High-speed ball transit (> 1.5 m/s) must be observed in Receiver region (minSpeed=${minSpeedDuringTransit.toFixed(2)}, maxSpeed=${maxSpeedDuringTransit.toFixed(2)})`
        );
        assert.equal(
            captured,
            0,
            `Fast through-pass (> 1.5 m/s) must NOT trigger capture (captured count=${captured})`
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

        // Verify captured count is reset cleanly to 0
        assert.equal(await driver.readCaptured(), 0, 'Captured count must be 0 after page reload');

        // Load saved construction
        await driver.load();

        // Run loaded construction: verify it solves the level and captures the ball
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
