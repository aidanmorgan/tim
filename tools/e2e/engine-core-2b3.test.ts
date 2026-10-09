// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b3.
// Verifies Analytic CCD & Interval Library Removal (Story 2.3):
// Continuous collision is handled entirely by speculative contacts (d_spec = |v_rel . n| * dt + CONTACT_SLOP, v_target = -g/dt):
// 1. High-speed ball impact against thin wall resolves via speculative contacts CCD with 0 tunneling across 20 iterations
// 2. Multi-body ramp and wall solve under speculative contacts without analytic interval sweeps
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
// Shared recipes (owner decision 9 Oct 2026, Story 6.1d): test 1 is the one 20-impact test and also carries ENGINE-CORE-2a4 #2
// (union of both copies' checks); tests 2 and 3 are proven by engine-core-2a3.test.ts tests 4 and 5, which carry ENGINE-CORE-2b3.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2b3: Analytic CCD & Interval Library Removal (Pure Speculative Contacts CCD)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. [ENGINE-CORE-2a4, ENGINE-CORE-2b3] High-speed ball impact against thin wall resolves via speculative contacts CCD with 0 tunneling across 20 iterations (high-speed impact across 20 iterations never tunnels through thin Wall)', { timeout: 150000 }, async () => {
        // Listen for any solver, sweep, or transport errors in console
        const errors: string[] = [];
        const errorHandler = (msg: ConsoleMessage) => {
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
        await driver.tiltSelectedWall(-20);

        // Run 20 consecutive high-speed impact iterations
        for (let iteration = 1; iteration <= 20; iteration++) {
            await driver.run();

            let reboundInIteration = false;
            let minPyInIteration = Infinity;
            let horizontalMotion = false;
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
                    if (Math.abs(ball.vx) > 0.2) {
                        horizontalMotion = true;
                    }
                }
                await driver.page.waitForTimeout(30);
            }

            // Reset simulation back to Build Mode
            await driver.reset(200);

            assert.ok(
                minPyInIteration > 3.8,
                `Iteration ${iteration}: Ball must not tunnel through Wall (got min py=${minPyInIteration.toFixed(3)}, expected > 3.8 m)`
            );
            assert.ok(
                reboundInIteration,
                `Iteration ${iteration}: Ball must cleanly rebound off Wall via speculative contacts CCD`
            );
            assert.ok(horizontalMotion, `Iteration ${iteration}: the tilted Wall deflects the ball sideways (|vx| > 0.2 m/s)`);
        }

        driver.page.off('console', errorHandler);
        assert.equal(errors.length, 0, `Zero errors expected across 20 iterations, got: ${errors.join('; ')}`);
    });
});
