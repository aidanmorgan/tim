// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-002 (Story 6.8).
// Verifies Generic Aperture Sensor with Directional Rearm (Ball Detector):
// 1. Placement, wiring, and forward passage: Ball detector placed and wired to Signal lamp in Free Workshop.
//    Ball traverses forward (+X) through aperture; detector pulses gold indicator and connected lamp illuminates.
// 2. Negative control (reverse passage): Reverse traversal (-X) does NOT trigger sensor or lamp.
// 3. Negative control (glancing trajectory): Outside trajectory (R > 0.65m) does NOT trigger sensor or lamp.
// 4. Directional rearm and reset: Reset restores upstream state and second Run replays forward trigger bit-for-bit.
// 5. Persistence: Save / reload / Load IndexedDB roundtrip reproduces forward passage and lamp activation.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type AnimationSampleRecord } from './workshop-driver.ts';

const detPos = { x: 720, y: 485 };
const lampPos = { x: 900, y: 420 };
const outsidePos = { x: 655, y: 465 };

async function waitFor(fn: () => Promise<boolean>, timeoutMs: number, stepMs = 50): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (await fn()) return true;
        await new Promise(resolve => setTimeout(resolve, stepMs));
    }
    return fn();
}

describe('CAT-002: Ball Detector Aperture Passage Sensor', () => {
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

    test('1. Placement, wiring, and forward passage: Ball detector pulses indicator and illuminates connected lamp', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place Ball detector at center
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');

        // 2. Place Signal lamp at (900, 420)
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);

        // 3. Connect detector ActivationOut -> lamp ActivationIn
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);

        // 4. Orient detector: tilt -18 steps (-90 deg about Z) so local +X points straight DOWN (-Y)
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(-18);

        // 5. Place Basketball directly above the aperture and lift
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, detPos.x, detPos.y);

        // Verify no premature animation samples prior to run
        const preRunSamples = await driver.readAllAnimationSamples();
        assert.equal(preRunSamples.length, 0, 'No animation samples before run');

        // 6. Run simulation: ball drops under gravity straight down through the detector
        await driver.run();

        // 7. Verify both detector and lamp emit cosmetic activation feedback
        // Detector indicator (SmoothStep 0.35s) and Lamp glow (SmoothStep 0.16s)
        const lit = await waitFor(async () => {
            const all = await driver.readAllAnimationSamples();
            return all.length >= 2 && all.some((s: AnimationSampleRecord) => s.valBits === 15360 || s.value >= 0.5);
        }, 5000);

        assert.ok(lit, 'Both ball detector indicator and connected lamp must illuminate upon forward passage');

        const allSamples = await driver.readAllAnimationSamples();
        assert.ok(allSamples.length >= 2, 'Detector indicator and lamp must publish samples');

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control (reverse passage): Traversal in -X does NOT trigger sensor or lamp', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place Ball detector
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');

        // 2. Place Signal lamp
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);

        // 3. Connect detector -> lamp
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);

        // 4. Rotate detector +90 deg: local +X points UP
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(18);

        // 5. Place ball above detector and lift
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, detPos.x, detPos.y);

        await driver.run();

        // Allow ball to fall completely through the collar
        await driver.page.waitForTimeout(2000);

        // Reverse passage must NOT trigger either detector or lamp
        const reverseSamples = await driver.readAllAnimationSamples();
        assert.equal(reverseSamples.length, 0, `Reverse passage must not emit animation samples, got: ${reverseSamples.length}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Negative control (glancing trajectory): Outside trajectory (R > 0.65m) does NOT trigger sensor or lamp', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place Ball detector
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');

        // 2. Place Signal lamp
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);

        // 3. Connect detector -> lamp
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);

        // 4. Orient detector -90 deg: local +X down
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(-18);

        // 5. Drop ball at outside glancing position (R > 0.65m)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(outsidePos.x, outsidePos.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, outsidePos.x, outsidePos.y);

        await driver.run();

        // Allow ball to fall past the detector level
        await driver.page.waitForTimeout(2000);

        // Outside trajectory must NOT trigger either detector or lamp
        const outsideSamples = await driver.readAllAnimationSamples();
        assert.equal(outsideSamples.length, 0, `Outside glancing trajectory must not emit animation samples, got: ${outsideSamples.length}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Directional rearm and reset: Reset restores upstream state and second Run replays forward trigger bit-for-bit', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place Ball detector
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');

        // 2. Place Signal lamp
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);

        // 3. Connect detector -> lamp
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);

        // 4. Orient detector: tilt -18 steps (-90 deg about Z) so local +X points straight DOWN (-Y)
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(-18);

        // 5. Place ball above detector
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, detPos.x, detPos.y);

        // First run: triggers lamp
        await driver.run();
        const firstLit = await waitFor(async () => {
            const all = await driver.readAllAnimationSamples();
            return all.length >= 2 && all.some((s: AnimationSampleRecord) => s.valBits === 15360 || s.value >= 0.5);
        }, 5000);
        assert.ok(firstLit, 'First run must illuminate lamp');

        // Reset: restores upstream starting arrangement
        await driver.reset(500);

        // Second run: ball clears upstream by definition and triggers sensor again
        await driver.run();
        const secondLit = await waitFor(async () => {
            const all = await driver.readAllAnimationSamples();
            return all.length >= 2 && all.some((s: AnimationSampleRecord) => s.valBits === 15360 || s.value >= 0.5);
        }, 5000);
        assert.ok(secondLit, 'Second run after Reset must illuminate lamp again');

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Persistence: Save / reload / Load IndexedDB persistence roundtrip reproduces forward passage and lamp activation', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place Ball detector
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');

        // 2. Place Signal lamp
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);

        // 3. Connect detector -> lamp
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);

        // 4. Orient detector: tilt -18 steps (-90 deg about Z) so local +X points straight DOWN (-Y)
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(-18);

        // 5. Place ball above detector
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, detPos.x, detPos.y);

        // Save construction to IndexedDB
        await driver.save();

        // Fresh page reload (wipes volatile memory and canvas state)
        await driver.reload();

        // Load construction back from IndexedDB
        await driver.load();

        // Run the restored construction
        await driver.run();

        const loadedLit = await waitFor(async () => {
            const all = await driver.readAllAnimationSamples();
            return all.length >= 2 && all.some((s: AnimationSampleRecord) => s.valBits === 15360 || s.value >= 0.5);
        }, 5000);

        assert.ok(loadedLit, 'Loaded construction must detect passage and illuminate connected lamp');

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
