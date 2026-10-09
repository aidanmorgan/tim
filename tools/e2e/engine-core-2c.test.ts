// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2c.
// Verifies Decoupled Sensor Evaluation & Dwell Tick Counter (Story 3.1):
// All sensors and triggers (aperture crossing, residence, impact switches) are evaluated at
// substep endpoints with tick-accumulated dwell, replacing continuous root-finding with clean discrete events:
// 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver, accumulates dwell, and triggers capture
// 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
// Test 3 is proven by the shared recipe engine-core-2a3.test.ts test 5, which carries ENGINE-CORE-2c in its name and also asserts
// the captured count is 0 after the reload (owner decision 9 Oct 2026, Story 6.1d); this suite keeps tests 1 and 2.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
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
        await driver.selectLevel('first_principles');

        // Listen for any solver, sensor root-finding, or transport errors in console
        const errors: string[] = [];
        const errorHandler = (msg: ConsoleMessage) => {
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
        await driver.run();

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
        await driver.reset(300);

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
        await driver.run();

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
        await driver.reset();

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
});
