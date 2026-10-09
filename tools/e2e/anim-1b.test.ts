// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ANIM-1b.
// Verifies Mechanical Cosmetic Bindings & Procedural Curves (Story 4.2):
// 1. Impact switch depression ramps 0→1 on the declared curve; an unconnected lamp emits no sample (negative control)
// 2. Delay progress fill wired switch→delay→lamp through the UI buttons rises monotonically to 1, the lamp lights after,
//    Reset stops every cosmetic sample and Save/Load re-animates the loaded construction
// 3. Bumper squash pulse rises above 0 and returns to 0 after a dropped ball strikes it; an un-struck bumper emits nothing
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver, type AnimationSampleRecord } from './workshop-driver.ts';

// delayed_signal authored identities: ball 1, switch 2, lamp 3; the placed Delay receives 4.
const SWITCH_TARGET = '4';            // activation node 2 + 2
const LAMP_TARGET = '5';              // activation node 3 + 2
const DELAY_TARGET = '4294967300';    // 2^32 + node 4
const BUMPER_BASE = 8589934592n;      // 2^33 + body
const SWITCH_SCREEN = { x: 524, y: 622 };
const LAMP_SCREEN = { x: 915, y: 612 };
const DELAY_SCREEN = { x: 720, y: 482 };

async function waitFor(fn: () => Promise<boolean>, timeoutMs: number, stepMs = 100): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (await fn()) return true;
        await new Promise(resolve => setTimeout(resolve, stepMs));
    }
    return fn();
}

function monotonic(values: number[]): boolean {
    for (let i = 1; i < values.length; i++) if (values[i] < values[i - 1]) return false;
    return true;
}

async function bumperSamples(driver: WorkshopDriver): Promise<Map<string, AnimationSampleRecord[]>> {
    const result = new Map<string, AnimationSampleRecord[]>();
    for (const sample of await driver.readAllAnimationSamples()) {
        if (BigInt(sample.target) < BUMPER_BASE) continue;
        result.set(sample.target, await driver.readAnimationSamplesForTarget(sample.target));
    }
    return result;
}

describe('ANIM-1b: Mechanical cosmetic bindings and procedural curves', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') || text.includes('Impulse occurrence dropped')) errors.push(text);
        });
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Switch depression ramps to 1 on the declared curve while the unconnected lamp stays neutral', { timeout: 120000 }, async () => {
        await driver.selectLevel('delayed_signal');
        assert.equal(await driver.isAnimationQualified(), true, 'Animation worker must be qualified');

        await driver.run();
        const pressed = await waitFor(async () => (await driver.readLastAnimationSample(SWITCH_TARGET))?.valBits === 15360, 8000);
        assert.ok(pressed, 'Switch target sample must reach Half 1.0 after the locked ball lands on it');

        const ramp = (await driver.readAnimationSamplesForTarget(SWITCH_TARGET)).map((s: AnimationSampleRecord) => s.value);
        assert.ok(ramp.length >= 2, `Declared 0.16 s SmoothStep ramp must publish several 60 Hz samples (got ${ramp.length})`);
        assert.ok(ramp.some(v => v > 0 && v < 1), `Ramp must pass through intermediate blends (got ${ramp.join(',')})`);
        assert.ok(monotonic(ramp), `Ramp must be monotonic (got ${ramp.join(',')})`);
        assert.equal((await driver.readAllAnimationSamples()).every((s: AnimationSampleRecord) => s.property === 5 || s.property === 3), true,
            'Cosmetic samples are 0..1 ColourBlend channels; halo/goal remain Opacity');

        await driver.page.waitForTimeout(600);
        assert.equal(await driver.readLastAnimationSample(LAMP_TARGET), null, 'Unconnected lamp must emit no sample (negative control)');
        assert.equal(await driver.readLastAnimationSample(DELAY_TARGET), null, 'No Delay is placed, so no timer target may sample');

        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Delay progress fill wired through UI buttons rises monotonically to 1, the lamp lights after, Reset neutralises and Save/Load re-animates', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('delayed_signal');

        await driver.selectTool('delay');
        await driver.placeOnCanvas(DELAY_SCREEN.x, 480);
        await driver.connectActivation({ ...SWITCH_SCREEN, panel: 'locked' }, DELAY_SCREEN);
        await driver.connectActivation({ ...DELAY_SCREEN, panel: 'delay' }, LAMP_SCREEN);
        await driver.save();

        await driver.run();
        const lit = await waitFor(async () => (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === 15360, 10000);
        assert.ok(lit, 'Lamp must light once the wired Delay finishes its countdown');

        const progress = await driver.readAnimationSamplesForTarget(DELAY_TARGET);
        const values = progress.map((s: AnimationSampleRecord) => s.value);
        assert.ok(values.length >= 10, `One-second Delay must publish many 60 Hz progress samples (got ${values.length})`);
        assert.ok(monotonic(values), `Progress must increase monotonically (got ${values.slice(0, 12).join(',')}...)`);
        assert.ok(values.some(v => v > 0.1 && v < 0.9), 'Progress must sample the interior of the declared linear fill');
        assert.equal(values[values.length - 1], 1, 'Progress holds 1 once the timer finishes');

        const lampHistory = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
        const switchHistory = await driver.readAnimationSamplesForTarget(SWITCH_TARGET);
        assert.ok(progress[0].timestamp < lampHistory[0].timestamp, 'Lamp lights only after the Delay began counting');
        assert.ok(lampHistory[0].timestamp - progress[0].timestamp >= 800, 'Lamp waits roughly the declared one-second countdown');

        // Reset: once the retired world's last in-flight pulse lands, no cosmetic target publishes again.
        await driver.reset(900);
        const switchAtReset = (await driver.readAnimationSamplesForTarget(SWITCH_TARGET)).length;
        const delayAtReset = (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).length;
        const lampAtReset = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;
        await driver.page.waitForTimeout(700);
        assert.equal((await driver.readAnimationSamplesForTarget(SWITCH_TARGET)).length, switchAtReset, 'Reset retires switch samples');
        assert.equal((await driver.readAnimationSamplesForTarget(DELAY_TARGET)).length, delayAtReset, 'Reset retires Delay samples');
        assert.equal((await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length, lampAtReset, 'Reset retires lamp samples');

        // Save/Load: a fresh page loads the wired construction and animates it again.
        await driver.reload();
        await driver.selectLevel('delayed_signal');
        await driver.load();
        await driver.run();
        const relit = await waitFor(async () => (await driver.readLastAnimationSample(LAMP_TARGET))?.valBits === 15360, 10000);
        assert.ok(relit, 'Loaded construction must re-animate switch → delay → lamp');
        const reloadedProgress = (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).map((s: AnimationSampleRecord) => s.value);
        assert.ok(reloadedProgress.length >= 10 && monotonic(reloadedProgress), 'Loaded Delay fills again from its declared curve');
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Bumper squash pulse rises and returns to neutral when struck; an un-struck bumper emits nothing', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('bumper', 150);
        await driver.run();

        const struck = await waitFor(async () => {
            for (const history of (await bumperSamples(driver)).values())
                if (history.some((s: AnimationSampleRecord) => s.value > 0.5) && history[history.length - 1].value === 0) return true;
            return false;
        }, 8000, 50);
        assert.ok(struck, 'Struck bumper must publish a pulse that rises above 0.5 and returns to 0');
        const pulses = await bumperSamples(driver);
        assert.equal(pulses.size, 1, `Exactly one bumper target may pulse (got ${[...pulses.keys()].join(',')})`);
        const [target, history] = [...pulses.entries()][0];
        assert.ok(BigInt(target) > BUMPER_BASE, 'Bumper target is 2^33 + body');
        const values = history.map((s: AnimationSampleRecord) => s.value);
        const peak = Math.max(...values);
        assert.ok(peak > 0.5 && peak <= 1, `Declared SineSquaredPulse peaks within (0.5, 1] (got ${peak})`);
        assert.ok(values[0] < peak && values[values.length - 1] === 0, 'Pulse rises from its first sample and settles back to 0');
        assert.ok(history.every((s: AnimationSampleRecord) => s.property === 5), 'Bumper samples are ColourBlend blends');

        await driver.reset(900);
        const settled = (await driver.readAnimationSamplesForTarget(target)).length;
        await driver.page.waitForTimeout(600);
        assert.equal((await driver.readAnimationSamplesForTarget(target)).length, settled, 'Reset stops bumper samples');

        // Negative control: the same ball drop beside an un-struck bumper emits no pulse.
        await driver.reload();
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(150, 720, 485);
        await driver.selectTool('bumper');
        await driver.placeOnCanvas(1000, 600);
        await driver.run();
        await driver.page.waitForTimeout(3000);
        assert.equal((await bumperSamples(driver)).size, 0, 'Un-struck bumper emits no sample');
        await driver.reset(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
