// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b3.
// Verifies Analytic CCD & Interval Library Removal (Story 2.3):
// Continuous collision is handled entirely by speculative contacts (d_spec = |v_rel . n| * dt + CONTACT_SLOP, v_target = -g/dt):
// 1. High-speed ball impact against thin wall resolves via speculative contacts CCD with 0 tunneling across 20 iterations
// 2. Multi-body ramp and wall solve under speculative contacts without analytic interval sweeps
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2b3: Analytic CCD & Interval Library Removal (Pure Speculative Contacts CCD)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. High-speed ball impact against thin wall resolves via speculative contacts CCD with 0 tunneling across 20 iterations', { timeout: 150000 }, async () => {
        await driver.reload();

        // Listen for any solver, sweep, or transport errors in console
        const errors: string[] = [];
        const errorHandler = (msg: any) => {
            const text = msg.text();
            if (
                text.includes('CCGPU_TRANSPORT_FAILURE') ||
                text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') ||
                text.includes('ScalarBoundarySweep') ||
                text.includes('ConvexSweep') ||
                text.includes('NaN')
            ) {
                errors.push(text);
            }
        };
        driver.page.on('console', errorHandler);

        // Place a Basketball at screen (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Lift Basketball to high elevation
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(150);

        // Place Wall beneath the ball
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Tilt the Wall (-20 deg) so impact has high closing velocity and tangential deflection
        await driver.tiltSelectedRamp(720, 485, -20);

        // Run 20 consecutive high-speed impact iterations
        for (let iteration = 1; iteration <= 20; iteration++) {
            await driver.toggleRun(0);

            let reboundInIteration = false;
            let minPyInIteration = Infinity;
            const runStartTime = Date.now();

            while (Date.now() - runStartTime < 1200) {
                const pose = await driver.readLatestPose();
                if (pose && pose.bodies.length > 0) {
                    const ball = pose.bodies[0];
                    if (ball.py < minPyInIteration) {
                        minPyInIteration = ball.py;
                    }
                    if (ball.py >= 4.0 && ball.vy > 0.3) {
                        reboundInIteration = true;
                    }
                }
                await driver.page.waitForTimeout(30);
            }

            // Reset simulation back to Build Mode
            await driver.toggleRun(200);

            assert.ok(
                minPyInIteration > 3.8,
                `Iteration ${iteration}: Ball must not tunnel through Wall (got min py=${minPyInIteration.toFixed(3)}, expected > 3.8 m)`
            );
            assert.ok(
                reboundInIteration,
                `Iteration ${iteration}: Ball must cleanly rebound off Wall via speculative contacts CCD`
            );
        }

        driver.page.off('console', errorHandler);
        assert.equal(errors.length, 0, `Zero errors expected across 20 iterations, got: ${errors.join('; ')}`);
    });

    test('2. Multi-body ramp and wall solve under speculative contacts without analytic interval sweeps', { timeout: 120000 }, async () => {
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

        assert.ok(captured > 0, `Receiver must capture ball via speculative contacts solve (captured count=${captured})`);
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
