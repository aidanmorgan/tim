// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ANIM-1a.
// Verifies Dedicated 60 Hz Animation Worker Pipeline & Core Feedback (Story 4.1):
// 1. Dedicated WebAssembly animation worker qualification & Receiver capture halo pulse upon goal solve
// 2. Cosmetic animation evaluation continues continuously at 60 Hz independent of simulation pause
// 3. Exact Reset and Save/Load persistence roundtrip with clean animation re-activation
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ANIM-1a: Dedicated 60 Hz WebAssembly Animation Worker Pipeline & Core Feedback', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Animation worker qualification & Receiver capture halo pulse upon goal solve', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Verify dedicated WebAssembly animation worker is bootstrapped and clock-qualified
        const qualified = await driver.isAnimationQualified();
        assert.equal(qualified, true, 'Dedicated WebAssembly animation worker must be qualified');

        // Listen for transport/solver errors in console
        const errors: string[] = [];
        const errorHandler = (msg: any) => {
            const text = msg.text();
            if (
                text.includes('CCGPU_TRANSPORT_FAILURE') ||
                text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') ||
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

        // Start simulation: ball rolls down ramps into Receiver
        await driver.toggleRun(0);

        let captured = 0;
        let settledInReceiver = false;
        const solveStartTime = Date.now();

        while (Date.now() - solveStartTime < 8000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                const speed = Math.hypot(ball.vx, ball.vy, ball.vz);
                if (ball.px >= 1.8 && ball.px <= 3.2 && ball.py <= 2.0 && speed <= 1.5) {
                    settledInReceiver = true;
                }
            }
            captured = await driver.readCaptured();
            if (captured > 0) break;
            await driver.page.waitForTimeout(100);
        }

        driver.page.off('console', errorHandler);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
        assert.ok(settledInReceiver, 'Ball must settle inside Receiver with speed <= 1.5 m/s');
        assert.ok(captured > 0, `Receiver capture must emit CCGOAL_SOLVED (captured count=${captured})`);

        // Verify Animation Worker delivered samples
        const sampleCount = await driver.readAnimationSampleCount();
        assert.ok(sampleCount > 0, `Animation worker must deliver 60 Hz samples (sampleCount=${sampleCount})`);

        // Verify Receiver halo pulse sample (Target 2)
        const haloSample = await driver.readLastAnimationSample(2);
        assert.ok(haloSample !== null, 'Receiver halo pulse animation sample (Target 2) must be received');
        assert.equal(haloSample.target, '2', 'Sample target must match Receiver halo Target ID (2)');
        assert.equal(haloSample.property, 3, 'Sample property must be AnimationProperty.Opacity (3)');
        assert.equal(haloSample.valBits, 15360, 'Sample value must reach full opacity (Half 1.0 = 15360)');

        // Verify Goal solved animation sample (Target ulong.MaxValue)
        const goalSample = await driver.readLastAnimationSample('18446744073709551615');
        assert.ok(goalSample !== null, 'Goal solved animation sample must be received');
        assert.equal(goalSample.property, 3, 'Goal sample property must be AnimationProperty.Opacity (3)');
    });

    test('2. Cosmetic animation evaluation continues at 60 Hz independent of simulation pause', { timeout: 120000 }, async () => {
        // Pause simulation via explicit UI playback control (WorldPlayback.Paused)
        await driver.pauseSimulation();

        // Record initial sample count and pose sequence immediately after pause is active
        const countAtPauseStart = await driver.readAnimationSampleCount();
        const poseAtPauseStart = await driver.readLatestPose();
        assert.ok(poseAtPauseStart, 'Pose must be readable at pause start');

        // Wait 500ms and read sample count and pose during pause
        await driver.page.waitForTimeout(500);
        const countDuringPause1 = await driver.readAnimationSampleCount();
        const poseDuringPause1 = await driver.readLatestPose();
        assert.ok(poseDuringPause1, 'Pose must be readable during pause at 500ms');

        // Wait another 500ms (1000ms total elapsed during pause)
        await driver.page.waitForTimeout(500);
        const countDuringPause2 = await driver.readAnimationSampleCount();
        const poseDuringPause2 = await driver.readLatestPose();
        assert.ok(poseDuringPause2, 'Pose must be readable during pause at 1000ms');

        // Verify physics simulation is frozen while paused (pose sequence does not advance)
        assert.equal(
            poseDuringPause1.sequence,
            poseAtPauseStart.sequence,
            `Physics pose sequence must remain frozen at 500ms (start=${poseAtPauseStart.sequence}, at500=${poseDuringPause1.sequence})`
        );
        assert.equal(
            poseDuringPause2.sequence,
            poseAtPauseStart.sequence,
            `Physics pose sequence must remain frozen at 1000ms (start=${poseAtPauseStart.sequence}, at1000=${poseDuringPause2.sequence})`
        );

        // Verify animation worker continued advancing at 60 Hz (~30 samples / 500ms, ~60 samples / 1000ms)
        const delta1 = countDuringPause1 - countAtPauseStart;
        const delta2 = countDuringPause2 - countAtPauseStart;
        assert.ok(
            delta1 >= 15,
            `Animation samples must advance during first 500ms of pause (expected >= 15, got delta=${delta1})`
        );
        assert.ok(
            delta2 >= 35,
            `Animation samples must advance during 1000ms of pause (expected >= 35, got delta=${delta2})`
        );

        // Resume simulation
        await driver.resumeSimulation();
        await driver.page.waitForTimeout(300);

        // Reset back to Build Mode to leave clean state
        await driver.toggleRun(500);
    });

    test('3. Exact Reset and Save/Load persistence roundtrip with clean animation re-activation', { timeout: 120000 }, async () => {
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

        // Save construction in Build Mode
        await driver.save();

        // Run simulation until solved
        await driver.toggleRun(0);
        const runStartTime = Date.now();
        while (Date.now() - runStartTime < 6000) {
            if (await driver.readCaptured() > 0) break;
            await driver.page.waitForTimeout(300);
        }

        // Verify halo sample is active from animation worker
        const initialHalo = await driver.readLastAnimationSample(2);
        assert.ok(initialHalo !== null, 'Receiver halo pulse must be active upon solve');

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

        // Load saved construction from IndexedDB
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

        // Verify animation samples continue delivering on re-solve
        const reHaloSample = await driver.readLastAnimationSample(2);
        assert.ok(reHaloSample !== null, 'Receiver halo pulse animation must re-activate after Load roundtrip');
        assert.equal(reHaloSample.valBits, 15360, 'Re-activated halo sample must reach full opacity');
    });
});
