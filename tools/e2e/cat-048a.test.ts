// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-048a (Story 6.6; lifecycle re-verified by Story 9R.1).
// Verifies the Straight Pipe compound cylindrical collider:
// 1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely without snagging or clipping.
// 2. Reset restores the exact pre-Run snapshot; a second Run replays the traversal and its Reset restores the snapshot again.
// 3. Save / reload / Load roundtrip via IndexedDB restores the exact construction; subsequent Run reproduces the traversal.
// 4. Negative control & mouth entrance containment: Ball placed outside mouth does not enter bore.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

// The committed (dynamic) bodies of the bench: the ball; the pipe is static.
const BENCH = { bodies: 1 } as const;

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

    // A pipe at the centre (0, 3, 0) tilted -15 deg (3 fine-rotate steps) and a basketball dropped at x = ballX.
    async function buildBench(ballX: number): Promise<void> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);
        const at = screen(ballX, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(at.x, at.y);
    }

    // Runs, samples the ball's distinct pose publications for windowMs, then pauses through the menu and proves the run advanced.
    async function runAndSample(s0: RunResetSnapshot, windowMs: number): Promise<{ ball: WorkshopBodyPose }[]> {
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;
        while (Date.now() - started < windowMs) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                if (ball) samples.push({ ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.expectRunAdvanced(s0);
        return samples;
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    function assertTraversed(samples: { ball: WorkshopBodyPose }[], label: string): void {
        assert.ok(samples.length > 40, `${label}: expected >40 distinct pose publications, got ${samples.length}`);
        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;
        assert.ok(initial.px >= -1.3 && initial.px <= -1.0, `${label}: initial px ${initial.px} should be near entrance (-1.2)`);
        assert.ok(Math.abs(initial.pz) < 0.05, `${label}: initial pz ${initial.pz} should be centered`);
        assert.ok(final.px > 1.8, `${label}: final px ${final.px} should emerge past pipe mouth at X=1.8`);
        assert.ok(final.vx > 1.5, `${label}: exit velocity should be substantial, vx=${final.vx}`);
    }

    test('1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely', { timeout: 150000 }, async () => {
        await buildBench(-1.2);
        const s0 = await driver.captureSnapshot(BENCH);
        const samples = await runAndSample(s0, 3500);

        assertTraversed(samples, 'first run');
        const initial = samples[0].ball;
        const middle = samples[Math.floor(samples.length / 2)].ball;
        // Ball rolls down along X (X increases, Y decreases along slope)
        assert.ok(middle.px > initial.px + 0.5, `Middle px ${middle.px} should progress down pipe from initial ${initial.px}`);
        assert.ok(middle.vx > 0.5, `Ball should accelerate down slope, vx=${middle.vx}`);
        // No lateral drift or clipping through tube walls during traversal
        for (const s of samples) {
            if (s.ball.px <= 1.8) {
                assert.ok(Math.abs(s.ball.pz) < 0.2, `Lateral drift pz=${s.ball.pz} should stay within bore`);
            }
            // Y should stay above workbench floor (-0.46)
            assert.ok(s.ball.py > -0.46, `Ball py=${s.ball.py} must not fall below workbench`);
        }

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Reset restores the exact pre-Run snapshot; a second Run replays the traversal and its Reset restores it again', { timeout: 150000 }, async () => {
        await buildBench(-1.2);
        const s0 = await driver.captureSnapshot(BENCH);

        assertTraversed(await runAndSample(s0, 3500), 'first run');
        await resetMatches(s0);
        assertTraversed(await runAndSample(s0, 3500), 'replay');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Save / reload / Load roundtrip via IndexedDB restores the pipe and ball; subsequent Run reproduces the traversal', { timeout: 150000 }, async () => {
        await buildBench(-1.2);
        // captureSnapshot saves the construction through the Workshop menu.
        const s0 = await driver.captureSnapshot(BENCH);

        // Reload page to wipe memory state, then Load from IndexedDB
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);

        assertTraversed(await runAndSample(loaded, 3500), 'loaded run');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Negative control & mouth entrance containment: Ball placed outside mouth does not enter bore', { timeout: 150000 }, async () => {
        // Basketball outside the mouth entrance at X = -2.6 m (behind mouth_start at -1.74m)
        await buildBench(-2.6);
        const s0 = await driver.captureSnapshot(BENCH);
        const samples = await runAndSample(s0, 2500);

        assert.ok(samples.length > 30, `Expected >30 pose publications, got ${samples.length}`);
        const finalBall = samples[samples.length - 1].ball;
        // Negative control: Ball dropped outside the mouth falls to workbench floor without entering pipe
        assert.ok(finalBall.px < -2.0, `Negative control: Ball px ${finalBall.px} stays outside pipe entrance (< -2.0)`);
        assert.ok(finalBall.px < 1.8, `Negative control: Ball never reaches exit mouth (X > 1.8)`);
        assert.ok(finalBall.py < 0.5, `Negative control: Ball falls down onto workbench (py=${finalBall.py})`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
