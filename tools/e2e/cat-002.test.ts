// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-002 (Story 6.8; lifecycle re-verified by Story 9R.1).
// Verifies Generic Aperture Sensor with Directional Rearm (Ball Detector):
// 1. Placement, wiring, and forward passage: Ball detector placed and wired to Signal lamp in Free Workshop.
//    Ball traverses forward (+X) through aperture; detector pulses gold indicator and connected lamp illuminates.
// 2. Negative control (reverse passage): Reverse traversal (-X) does NOT trigger sensor or lamp.
// 3. Negative control (glancing trajectory): Outside trajectory (R > 0.65m) does NOT trigger sensor or lamp.
// 4. Directional rearm and reset: Reset restores the exact pre-Run snapshot and a second Run triggers forward again.
// 5. Persistence: Save / reload / Load IndexedDB roundtrip restores the exact construction and reproduces forward passage.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type AnimationSampleRecord, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

const detPos = { x: 720, y: 485 };
const lampPos = { x: 900, y: 420 };
const outsidePos = { x: 655, y: 465 };
// Free-bench identities in placement order (the Workshop allocator starts at 1 and advances per placement, ui/Workshop.cs
// `_nextId`): detector 1, lamp 2, basketball 3. An activation part's animation target is its node + 2
// (the anim-1b and cat-023b convention), so this is the lamp's own target: the detector's indicator pulse cannot satisfy it
// (Story 9R.9; browser qualification pending until the post-catalogue E2E phase).
const LAMP_TARGET = '4';
// The committed (dynamic) bodies of every bench: the basketball; the detector and lamp are static.
const BENCH = { bodies: 1 } as const;

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
        await driver?.closeAfterSuite();
    });

    // Detector at the centre wired to a lamp, tilted by `steps` fine-rotate steps (-18: local +X down, +18: local +X up), and a
    // basketball placed at `ballAt` and lifted 120 px.
    async function buildDetectorBench(steps: number, ballAt: { x: number; y: number }): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(detPos.x, detPos.y);
        await driver.page.keyboard.press('Escape');
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(lampPos.x, lampPos.y);
        await driver.connectActivation({ ...detPos, panel: 'detector' }, lampPos);
        await driver.selectPartAt(detPos.x, detPos.y);
        await driver.tiltSelectedByFineRotate(steps);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballAt.x, ballAt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120, ballAt.x, ballAt.y);
        return driver.captureSnapshot(BENCH);
    }

    // The lamp target's own dark-to-lit transitions, counted per animated property (each property records the same activation).
    async function lampActivations(): Promise<number> {
        const edges = new Map<number, { lit: boolean; count: number }>();
        for (const s of await driver.readAnimationSamplesForTarget(LAMP_TARGET)) {
            const on = s.valBits === 15360 || s.value >= 0.5;
            const state = edges.get(s.property) ?? { lit: false, count: 0 };
            if (on && !state.lit) state.count++;
            state.lit = on;
            edges.set(s.property, state);
        }
        return Math.max(0, ...[...edges.values()].map(state => state.count));
    }

    // Exactly one more lamp activation than `before`: the lamp target's sample history survives Reset (anim-1b, cat-023b).
    // C-Lamp review F5 (Story 9A.22): reaching before + 1 is not "exactly once". After it, hold for the lamp glow (SmoothStep 0.16 s)
    // plus 500 ms and re-read, so a later second flash fails.
    const glowMs = 160, settleMs = 500;
    async function lightsOnceMore(before: number): Promise<boolean> {
        if (!(await waitFor(async () => (await lampActivations()) === before + 1, 5000))) return false;
        await driver.page.waitForTimeout(glowMs + settleMs);
        return (await lampActivations()) === before + 1;
    }

    async function resetMatches(s0: RunResetSnapshot, holdMs = 0): Promise<void> {
        await driver.expectRunAdvanced(s0);
        await driver.reset(holdMs);
        await driver.expectResetMatches(s0);
    }

    test('1. Placement, wiring, and forward passage: Ball detector pulses indicator and illuminates connected lamp', { timeout: 150000 }, async () => {
        const s0 = await buildDetectorBench(-18, detPos);

        // Verify no premature animation samples prior to run
        const preRunSamples = await driver.readAllAnimationSamples();
        assert.equal(preRunSamples.length, 0, 'No animation samples before run');

        // Run simulation: ball drops under gravity straight down through the detector
        await driver.run();
        // Lamp glow (SmoothStep 0.16s) on the lamp's own target: exactly one activation
        assert.ok(await lightsOnceMore(0), 'The connected lamp must illuminate exactly once upon forward passage');
        const allSamples = await driver.readAllAnimationSamples();
        assert.ok(allSamples.length >= 2, 'Detector indicator and lamp must publish samples');

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control (reverse passage): Traversal in -X does NOT trigger sensor or lamp', { timeout: 150000 }, async () => {
        // Rotate detector +90 deg: local +X points UP
        const s0 = await buildDetectorBench(18, detPos);

        await driver.run();
        // Allow ball to fall completely through the collar
        await driver.page.waitForTimeout(2000);
        // Reverse passage must NOT trigger either detector or lamp
        const reverseSamples = await driver.readAllAnimationSamples();
        assert.equal(reverseSamples.length, 0, `Reverse passage must not emit animation samples, got: ${reverseSamples.length}`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Negative control (glancing trajectory): Outside trajectory (R > 0.65m) does NOT trigger sensor or lamp', { timeout: 150000 }, async () => {
        // Drop ball at outside glancing position (R > 0.65m), detector local +X down
        const s0 = await buildDetectorBench(-18, outsidePos);

        await driver.run();
        // Allow ball to fall past the detector level
        await driver.page.waitForTimeout(2000);
        // Outside trajectory must NOT trigger either detector or lamp
        const outsideSamples = await driver.readAllAnimationSamples();
        assert.equal(outsideSamples.length, 0, `Outside glancing trajectory must not emit animation samples, got: ${outsideSamples.length}`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Directional rearm and reset: Reset restores the exact pre-Run snapshot and a second Run triggers forward again', { timeout: 150000 }, async () => {
        const s0 = await buildDetectorBench(-18, detPos);

        // First run: triggers lamp
        await driver.run();
        assert.ok(await lightsOnceMore(0), 'First run must illuminate lamp');
        // Reset (draining the retired world's cosmetic pulses): restores the upstream starting arrangement exactly
        await resetMatches(s0, 500);

        // Second run: ball clears upstream by definition and triggers sensor again
        const before = await lampActivations();
        await driver.run();
        assert.ok(await lightsOnceMore(before), 'Second run after Reset must illuminate lamp again');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Persistence: Save / reload / Load IndexedDB persistence roundtrip reproduces forward passage and lamp activation', { timeout: 150000 }, async () => {
        // captureSnapshot saves the construction to IndexedDB through the Workshop menu.
        const s0 = await buildDetectorBench(-18, detPos);

        // Fresh page reload (wipes volatile memory and canvas state), then Load construction back from IndexedDB
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);

        // Run the restored construction
        const before = await lampActivations();
        await driver.run();
        assert.ok(await lightsOnceMore(before), 'Loaded construction must detect passage and illuminate connected lamp');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
