// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ANIM-1c.
// Verifies Legacy Presentation Code Retirement (Story 4.3):
// 1. The Receiver halo is a declared Capture cosmetic on the shared part path (target 3·2^32 + body, ColourBlend ramp to 1),
//    the goal label is a declared UI binding (one CCGOAL_SOLVED), Reset retires both and Save/reload/Load/Run re-animates the halo
// 2. The hint is a declared UI binding: pressing Show hint while a wired Delay is counting reveals 0→1 on the shared lease without
//    any exception while the Delay fill keeps rising; the next press hides it and the hint target stops publishing
// 3. Negative controls: no capture, goal or hint sample exists before its real cause; a hidden hint publishes nothing more
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver, type AnimationSampleRecord } from './workshop-driver.ts';

const HINT_TARGET = '1';                        // fixed UI target (UiCurves.Hint)
const GOAL_TARGET = '18446744073709551615';     // fixed UI target (UiCurves.Goal)
const RECEIVER_HALO_TARGET = '12884901890';     // 3·2^32 + receiver body 2 (First principles authors ball 1, receiver 2)
const SWITCH_TARGET = '4';                      // delayed_signal activation node 2 + 2
const DELAY_TARGET = '4294967300';              // 2^32 + placed Delay node 4
const OPACITY = 3;
const COLOUR_BLEND = 5;
const HALF_ONE = 15360;
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

async function placeFirstPrinciplesRamps(driver: WorkshopDriver): Promise<void> {
    await driver.selectTool('ramp');
    await driver.placeOnCanvas(498, 404);
    await driver.tiltSelectedRamp(498, 404, -20);
    await driver.selectTool('ramp');
    await driver.placeOnCanvas(674, 509);
    await driver.tiltSelectedRamp(674, 509, -20);
}

describe('ANIM-1c: Legacy presentation code retirement', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') || text.includes('animation control is pending') ||
                text.includes('Animation control is unavailable')) errors.push(text);
        });
        driver.page.on('pageerror', (error: Error) => errors.push(error.message));
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Capture halo and goal label animate through declared bindings; Reset retires them and Save/Load re-animates', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');
        assert.equal(await driver.isAnimationQualified(), true, 'Animation worker must be qualified');
        await placeFirstPrinciplesRamps(driver);
        await driver.save();

        // Negative control: nothing is captured or solved before Run.
        assert.equal(await driver.readLastAnimationSample(RECEIVER_HALO_TARGET), null, 'No capture sample before Run');
        assert.equal(await driver.readLastAnimationSample(GOAL_TARGET), null, 'No goal sample before Run');

        await driver.toggleRun(0);
        const captured = await waitFor(async () => (await driver.readLastAnimationSample(RECEIVER_HALO_TARGET))?.valBits === HALF_ONE, 12000);
        assert.ok(captured, 'Capture target sample must ramp to Half 1.0 once the ball is captured');
        const halo = await driver.readAnimationSamplesForTarget(RECEIVER_HALO_TARGET);
        assert.ok(halo.every((s: AnimationSampleRecord) => s.property === COLOUR_BLEND), 'Capture halo samples are ColourBlend channels');
        const ramp = halo.map((s: AnimationSampleRecord) => s.value);
        assert.ok(ramp.length >= 3, `Declared 0.5 s SmoothStep ramp publishes several 60 Hz samples (got ${ramp.length})`);
        assert.ok(ramp.some(v => v > 0 && v < 1), `Ramp passes through intermediate blends (got ${ramp.slice(0, 8).join(',')}...)`);
        assert.ok(monotonic(ramp), 'Capture ramp is monotonic');

        const solved = await waitFor(async () => (await driver.readLastAnimationSample(GOAL_TARGET))?.valBits === HALF_ONE, 5000);
        assert.ok(solved, 'Goal UI target sample must reach 1');
        assert.equal((await driver.readLastAnimationSample(GOAL_TARGET))!.property, OPACITY, 'Goal samples are Opacity channels');
        await driver.page.waitForTimeout(800);
        assert.equal(await driver.readCaptured(), 1, 'Exactly one CCGOAL_SOLVED per solved world');
        // Negative control: an unpressed hint only ever holds its declared neutral (the Hide registration at level select).
        assert.ok((await driver.readAnimationSamplesForTarget(HINT_TARGET)).every((s: AnimationSampleRecord) => s.value === 1),
            'Hint target never leaves its neutral opacity while unpressed');

        // Reset: the retired world's targets stop publishing.
        await driver.toggleRun(900);
        const haloAtReset = (await driver.readAnimationSamplesForTarget(RECEIVER_HALO_TARGET)).length;
        const goalAtReset = (await driver.readAnimationSamplesForTarget(GOAL_TARGET)).length;
        await driver.page.waitForTimeout(700);
        assert.equal((await driver.readAnimationSamplesForTarget(RECEIVER_HALO_TARGET)).length, haloAtReset, 'Reset retires capture samples');
        assert.equal((await driver.readAnimationSamplesForTarget(GOAL_TARGET)).length, goalAtReset, 'Reset retires goal samples');
        assert.equal(await driver.readCaptured(), 1, 'Reset does not announce the goal again');

        // Save/Load: a fresh page loads the construction and the halo re-animates on re-solve.
        await driver.reload();
        await driver.selectLevel('first_principles');
        await driver.load();
        await driver.toggleRun(0);
        const recaptured = await waitFor(async () => (await driver.readLastAnimationSample(RECEIVER_HALO_TARGET))?.valBits === HALF_ONE, 12000);
        assert.ok(recaptured, 'Loaded construction must re-animate the capture halo to Half 1.0');
        assert.equal(await driver.readCaptured(), 1, 'Loaded construction solves the level once');
        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Show hint while a wired Delay counts reveals on the shared lease without exception, the fill continues, and the next press hides', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('delayed_signal');
        await driver.selectTool('delay');
        await driver.placeOnCanvas(DELAY_SCREEN.x, DELAY_SCREEN.y);
        await driver.connectActivation({ ...SWITCH_SCREEN, panel: 'locked' }, DELAY_SCREEN);
        await driver.connectActivation({ ...DELAY_SCREEN, panel: 'delay' }, LAMP_SCREEN);
        // Negative control: before the button is pressed the hint only holds its declared neutral (no reveal ramp).
        assert.ok((await driver.readAnimationSamplesForTarget(HINT_TARGET)).every((s: AnimationSampleRecord) => s.value === 1),
            'No hint reveal before the button is pressed');

        await driver.toggleRun(0);
        const counting = await waitFor(async () => (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).length >= 2, 10000);
        assert.ok(counting, 'The wired Delay must start counting after the ball lands on the switch');
        const fillBefore = (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).length;
        const hintBefore = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).length;

        await driver.showHint();
        const revealed = await waitFor(async () => {
            const fresh = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).slice(hintBefore);
            return fresh.some((s: AnimationSampleRecord) => s.value < 1) && fresh[fresh.length - 1]?.valBits === HALF_ONE;
        }, 5000);
        assert.ok(revealed, 'Hint target must reveal from 0 to Half 1.0 while the Delay counts');
        const fresh = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).slice(hintBefore);
        assert.ok(fresh.every((s: AnimationSampleRecord) => s.property === OPACITY), 'Hint samples are Opacity channels');
        // Neutral pulses already in flight before the Reveal clip registered precede the ramp; the ramp starts at its first sub-unit sample.
        const reveal = fresh.slice(fresh.findIndex((s: AnimationSampleRecord) => s.value < 1));
        const revealValues = reveal.map((s: AnimationSampleRecord) => s.value);
        assert.ok(revealValues.some(v => v > 0 && v < 1), `Reveal passes through intermediate opacities (got ${revealValues.join(',')})`);
        assert.ok(monotonic(revealValues), `Reveal is monotonic 0→1 (got ${revealValues.join(',')})`);

        const fillAfter = (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).length;
        assert.ok(fillAfter > fillBefore, `Delay fill keeps publishing while the hint animates (${fillBefore} → ${fillAfter})`);
        const fill = (await driver.readAnimationSamplesForTarget(DELAY_TARGET)).map((s: AnimationSampleRecord) => s.value);
        assert.ok(monotonic(fill), 'Delay fill stays monotonic across the hint control');
        assert.ok((await driver.readLastAnimationSample(SWITCH_TARGET))?.valBits === HALF_ONE, 'Switch depression already reached 1');

        // Hide: the label is hidden by its control and the hint target rests on its declared neutral opacity (1) from then on.
        const hintAtHide = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).length;
        await driver.showHint();
        await driver.page.waitForTimeout(600);
        const afterHide = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).slice(hintAtHide);
        assert.ok(afterHide.length > 0 && afterHide.every((s: AnimationSampleRecord) => s.value === 1),
            `A hidden hint holds its neutral opacity (got ${afterHide.map(s => s.value).join(',')})`);

        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Hint reveal and hide in Build mode leave every world-bound target silent', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');
        const hintBefore = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).length;
        await driver.showHint();
        const revealed = await waitFor(async () => {
            const fresh = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).slice(hintBefore);
            return fresh.some((s: AnimationSampleRecord) => s.value < 1) && fresh[fresh.length - 1]?.valBits === HALF_ONE;
        }, 5000);
        assert.ok(revealed, 'Hint reveals 0→1 in Build mode without a running world');
        const hintAtHide = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).length;
        await driver.showHint();
        await driver.page.waitForTimeout(600);
        const afterHide = (await driver.readAnimationSamplesForTarget(HINT_TARGET)).slice(hintAtHide);
        assert.ok(afterHide.length > 0 && afterHide.every((s: AnimationSampleRecord) => s.value === 1), 'Hidden hint holds its neutral opacity');
        const samples = await driver.readAllAnimationSamples();
        assert.ok(samples.length > 0, 'The hint published through the worker');
        assert.ok(samples.every((s: AnimationSampleRecord) => s.target === HINT_TARGET), `Only the hint target may publish in Build mode (got ${samples.map(s => s.target).join(',')})`);
        assert.equal(await driver.readCaptured(), 0, 'No goal is announced in Build mode');
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Free workshop: a dropped ball captured by a placed Receiver animates the same declared capture target with no goal', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('free_workshop');
        // Free play authors the Basketball as body 1 and the Receiver as body 2; its compiled capture sensor is first+16, not the authored id.
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('receiver', 150);
        assert.equal(await driver.readLastAnimationSample(RECEIVER_HALO_TARGET), null, 'No capture sample before Run');

        await driver.toggleRun(0);
        const captured = await waitFor(async () => (await driver.readLastAnimationSample(RECEIVER_HALO_TARGET))?.valBits === HALF_ONE, 12000);
        assert.ok(captured, 'Free-workshop Receiver capture target must ramp to Half 1.0 when the dropped ball settles in it');
        const halo = await driver.readAnimationSamplesForTarget(RECEIVER_HALO_TARGET);
        assert.ok(halo.every((s: AnimationSampleRecord) => s.property === COLOUR_BLEND), 'Capture halo samples are ColourBlend channels');
        assert.ok(monotonic(halo.map((s: AnimationSampleRecord) => s.value)), 'Capture ramp is monotonic in free play');
        assert.equal(await driver.readLastAnimationSample(GOAL_TARGET), null, 'Free workshop has no goal, so the goal target stays silent');
        assert.equal(await driver.readCaptured(), 0, 'No CCGOAL_SOLVED in free workshop');

        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
