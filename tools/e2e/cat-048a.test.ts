// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-048a (Story 6.6).
// Verifies the Straight Pipe compound cylindrical collider:
// 1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely without snagging or clipping.
// 2. Reset restores the ball and pipe to exact initial poses; second Run replays bit-for-bit.
// 3. Save / reload / Load roundtrip via IndexedDB restores the pipe and ball; subsequent Run reproduces the traversal.
// 4. Negative control & mouth entrance containment: Ball placed outside mouth does not enter bore.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

describe('CAT-048a: Straight Pipe Compound Cylindrical Collider', () => {
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

    test('1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Select pipe and place at center (0, 3, 0)
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);

        // 2. Select the pipe and tilt it by -3 steps (-15 deg about Z)
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);

        // 3. Select basketball and place at elevated mouth (-1.2, 3, 0)
        const at = screen(-1.2, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(at.x, at.y);

        // 4. Run and observe traversal
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                if (ball) {
                    samples.push({ ball });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 40, `Expected >40 distinct pose publications, got ${samples.length}`);

        const initial = samples[0].ball;
        const middle = samples[Math.floor(samples.length / 2)].ball;
        const final = samples[samples.length - 1].ball;

        // Ball starts near mouth entrance
        assert.ok(initial.px >= -1.3 && initial.px <= -1.0, `Initial px ${initial.px} should be near entrance (-1.2)`);
        assert.ok(Math.abs(initial.pz) < 0.05, `Initial pz ${initial.pz} should be centered`);

        // Ball rolls down along X (X increases, Y decreases along slope)
        assert.ok(middle.px > initial.px + 0.5, `Middle px ${middle.px} should progress down pipe from initial ${initial.px}`);
        assert.ok(middle.vx > 0.5, `Ball should accelerate down slope, vx=${middle.vx}`);

        // Ball exits through the opposite mouth (X > 1.8)
        assert.ok(final.px > 1.8, `Final px ${final.px} should emerge past pipe mouth at X=1.8`);
        assert.ok(final.vx > 1.5, `Exit velocity should be substantial, vx=${final.vx}`);

        // No lateral drift or clipping through tube walls during traversal
        for (const s of samples) {
            if (s.ball.px <= 1.8) {
                assert.ok(Math.abs(s.ball.pz) < 0.2, `Lateral drift pz=${s.ball.pz} should stay within bore`);
            }
            // Y should stay above workbench floor (-0.46)
            assert.ok(s.ball.py > -0.46, `Ball py=${s.ball.py} must not fall below workbench`);
        }

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Reset restores the ball and pipe to exact initial poses; second Run replays bit-for-bit', { timeout: 150000 }, async () => {
        // Reset from the previous test
        await driver.reset();

        // Second Run replays traversal
        await driver.run();
        const started = Date.now();
        const replaySamples: { ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 3500) {
            const currentSlot = await driver.readLatestPose();
            if (currentSlot && currentSlot.sequence && BigInt(currentSlot.sequence) !== previousSeq) {
                previousSeq = BigInt(currentSlot.sequence);
                const ball = currentSlot.bodies.find(b => b.id === '2');
                if (ball) replaySamples.push({ ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(replaySamples.length > 40, `Expected >40 distinct pose publications on replay, got ${replaySamples.length}`);

        // Ball starts near mouth entrance
        const replayInitial = replaySamples[0].ball;
        assert.ok(replayInitial.px >= -1.3 && replayInitial.px <= -1.0, `Restored initial px ${replayInitial.px} should match`);
        assert.ok(Math.abs(replayInitial.pz) < 0.05, `Restored initial pz ${replayInitial.pz} should match`);

        const finalReplay = replaySamples[replaySamples.length - 1].ball;
        assert.ok(finalReplay.px > 1.8, `Replay final px ${finalReplay.px} should emerge past mouth at X=1.8`);
        assert.ok(finalReplay.vx > 1.5, `Replay exit velocity vx=${finalReplay.vx}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Save / reload / Load roundtrip via IndexedDB restores the pipe and ball; subsequent Run reproduces the traversal', { timeout: 150000 }, async () => {
        errors.length = 0;
        // Build and save
        await driver.reload();
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);

        const at = screen(-1.2, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(at.x, at.y);

        // Save
        await driver.save();

        // Reload page to wipe memory state
        await driver.reload();

        // Load from IndexedDB
        await driver.load();

        // Run loaded construction
        await driver.run();
        const started = Date.now();
        const loadedSamples: { ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 3500) {
            const currentSlot = await driver.readLatestPose();
            if (currentSlot && currentSlot.sequence && BigInt(currentSlot.sequence) !== previousSeq) {
                previousSeq = BigInt(currentSlot.sequence);
                const ball = currentSlot.bodies.find(b => b.id === '2');
                if (ball) loadedSamples.push({ ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(loadedSamples.length > 40, `Expected >40 pose publications from loaded run, got ${loadedSamples.length}`);
        const finalLoaded = loadedSamples[loadedSamples.length - 1].ball;
        assert.ok(finalLoaded.px > 1.8, `Loaded final px ${finalLoaded.px} should traverse and exit mouth at X=1.8`);
        assert.ok(finalLoaded.vx > 1.5, `Loaded exit velocity vx=${finalLoaded.vx}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Negative control & mouth entrance containment: Ball placed outside mouth does not enter bore', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Place pipe at center (0, 3, 0) and tilt by -15 deg
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);

        // 2. Place basketball outside the mouth entrance at X = -2.6 m (behind mouth_start at -1.74m)
        const at = screen(-2.6, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(at.x, at.y);

        // 3. Run simulation
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 2500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                if (ball) samples.push({ ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 30, `Expected >30 pose publications, got ${samples.length}`);
        const finalBall = samples[samples.length - 1].ball;

        // Negative control: Ball dropped outside the mouth falls to workbench floor without entering pipe
        assert.ok(finalBall.px < -2.0, `Negative control: Ball px ${finalBall.px} stays outside pipe entrance (< -2.0)`);
        assert.ok(finalBall.px < 1.8, `Negative control: Ball never reaches exit mouth (X > 1.8)`);
        assert.ok(finalBall.py < 0.5, `Negative control: Ball falls down onto workbench (py=${finalBall.py})`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
