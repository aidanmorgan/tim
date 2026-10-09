// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b4.
// Verifies Guide Horizon & Departure Ownership Removal (Story 2.4):
// Player assistance forces are modeled strictly as declared spatial acceleration fields
// evaluated at current state without continuous trajectory sweep predictors or departure ownership routines:
// 1. first_principles level with receiver assistance knots evaluated as declared spatial acceleration fields (smooth entrance without moving collider walls)
// 2. Multi-body ramp rolling and assistance knot interaction under pure spatial force regions
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
// Test 3 is proven by the shared recipe engine-core-2a3.test.ts test 5, which carries ENGINE-CORE-2b4 in its name
// (owner decision 9 Oct 2026, Story 6.1d); this suite keeps tests 1 and 2.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2b4: Guide Horizon & Departure Ownership Removal (Declarative Spatial Force Regions)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. First principles receiver assistance knots evaluated as declared spatial acceleration fields (smooth entrance without moving collider walls)', { timeout: 120000 }, async () => {
        await driver.selectLevel('first_principles');

        // Listen for any solver, guide predictor, or transport errors in console
        const errors: string[] = [];
        const errorHandler = (msg: ConsoleMessage) => {
            const text = msg.text();
            if (
                text.includes('CCGPU_TRANSPORT_FAILURE') ||
                text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') ||
                text.includes('PlanarGuideBoundaryPath') ||
                text.includes('GuideHorizon') ||
                text.includes('departure') ||
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

        // Start simulation: ball rolls down ramps and enters Receiver assistance field
        await driver.run();

        let captured = 0;
        let enteredAssistanceRegion = false;
        const solveStartTime = Date.now();
        while (Date.now() - solveStartTime < 8000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                // Receiver force region is located around px ~ 2.5, py in [1.5, 3.5]
                if (ball.px >= 1.5 && ball.px <= 3.5 && ball.py <= 3.5 && ball.py >= 1.5) {
                    enteredAssistanceRegion = true;
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
        assert.ok(enteredAssistanceRegion, 'Ball must enter the Receiver spatial assistance force region');
        assert.ok(captured > 0, `Receiver must capture ball via declared spatial acceleration fields (captured count=${captured})`);
        assert.ok(
            solvedPose.bodies[0].px > 1.8 && solvedPose.bodies[0].px < 3.2,
            `Ball must end inside Receiver horizontal range [1.8, 3.2] (got px=${solvedPose.bodies[0].px.toFixed(3)})`
        );
        assert.ok(
            solvedPose.bodies[0].py < 2.0,
            `Ball must settle inside Receiver basket without penetrating walls (got py=${solvedPose.bodies[0].py.toFixed(3)})`
        );
    });

    test('2. Multi-body ramp rolling and assistance knot interaction under pure spatial force regions', { timeout: 120000 }, async () => {
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

        // Start simulation
        await driver.run();

        let contactedRamp1 = false;
        let rolledToRamp2 = false;
        let guidedIntoReceiver = false;
        let captured = 0;
        const testStartTime = Date.now();

        while (Date.now() - testStartTime < 8000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                if (ball.py <= 4.8 && ball.py >= 3.8 && ball.px < -2.0) {
                    contactedRamp1 = true;
                }
                if (ball.px > -1.0 && ball.px < 1.5 && ball.py < 3.8) {
                    rolledToRamp2 = true;
                }
                if (ball.px >= 1.8 && ball.px <= 3.2 && ball.py < 2.5) {
                    guidedIntoReceiver = true;
                }
            }
            captured = await driver.readCaptured();
            if (captured > 0) break;
            await driver.page.waitForTimeout(50);
        }

        await driver.reset();

        assert.ok(contactedRamp1, 'Ball must contact Ramp 1');
        assert.ok(rolledToRamp2, 'Ball must transition smoothly to Ramp 2');
        assert.ok(guidedIntoReceiver, 'Ball must be guided into Receiver via spatial acceleration field');
        assert.ok(captured > 0, `Ball must be captured in Receiver (captured count=${captured})`);
    });
});
