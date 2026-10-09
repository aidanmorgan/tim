// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b1.
// Verifies Host Validation & Error Lane Removal (Story 2.1):
// Direct SAB pose ring publishing without intermediate host shadow checking:
// 1. Real-time ball drop and Workbench interaction without host validation overhead or errors
// 2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) with direct pose ring publishing
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
// Tests 2 and 3 are proven by the shared recipes engine-core-2a3.test.ts tests 4 and 5, which carry ENGINE-CORE-2b1 in their names
// (owner decision 9 Oct 2026, Story 6.1d); this suite keeps test 1.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
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
        // Listen for any transport or validation failure logged in console
        const errors: string[] = [];
        const errorHandler = (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') || text.includes('Unhandled exception')) {
                errors.push(text);
            }
        };
        driver.page.on('console', errorHandler);

        // Select Basketball and place on canvas at (720, 485) -> (px ~ 0, py ~ 3.0, pz ~ 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Run simulation: ball drops from py ~ 3.0 m down to workbench floor
        await driver.run();

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
        await driver.reset(300);

        driver.page.off('console', errorHandler);

        assert.equal(errors.length, 0, `Zero host validation/transport errors expected, got: ${errors.join('; ')}`);
        assert.ok(sequenceAdvanced, `Pose ring sequence must advance steadily (from ${initialSequence} to ${latestSequence})`);
        assert.ok(initialPy > 2.0, `Ball must start near py ~ 3.0 m (got initial py=${initialPy.toFixed(3)})`);
        assert.ok(finalPy < 1.0, `Ball must fall towards workbench floor (got final py=${finalPy.toFixed(3)})`);
    });
});
