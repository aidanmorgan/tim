// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-030 (Story 6.11; lifecycle re-verified by Story 9R.1).
// Verifies Funnel hollow frustum inlet to standard bore:
// 1. Workshop selection: Select funnel from palette, place on canvas.
// 2. Negative control: Ball dropped on outer shell deflects/bounces without entering bore.
// 3. Centered drop: Ball dropped into wide 2.6 m inlet descends smoothly, guided by tapered walls, exits 1.3 m outlet into receiver below.
// 4. Off-center drop: Ball dropped at +0.70 m off-axis gathers into funnel without snagging and exits outlet.
// 5. Joined pipe route: a straight pipe snapped to the Funnel outlet (Story 9R.6, replacing the hand-stacked overlap); the ball
//    transitions from funnel into tube.
// 6. Reset restoration: Reset restores the exact pre-Run snapshot; a second Run settles again and its Reset restores it again.
// 7. Save / Load persistence roundtrip: Save, reload, Load restores the exact construction, re-run reproduces traversal.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';
import { assertJoined, screen as tubeScreen } from './tube-mouths.ts';

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

// The committed (dynamic) bodies of every bench: the basketball; the funnel, pipe and receiver are static.
const BENCH = { bodies: 1 } as const;

describe('CAT-030: Funnel Hollow Frustum Inlet to Standard Bore E2E', () => {
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

    // A basketball at x = ballX lifted liftPx, then a funnel at the centre tilted -90 deg and lifted 80 px, and a receiver beneath.
    async function buildFunnelDrop(ballX: number, liftPx: number): Promise<RunResetSnapshot> {
        await driver.reload();
        errors.length = 0;
        const ballPt = screen(ballX, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballPt.x, ballPt.y);
        await driver.selectPartAt(ballPt.x, ballPt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(liftPx, ballPt.x, ballPt.y);
        await driver.pressKey('Escape');

        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(80, 720, 485, 130);
        await driver.pressKey('Escape');

        await driver.selectTool('receiver');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');
        return driver.captureSnapshot(BENCH);
    }

    // Runs, samples the ball's distinct pose publications for 3.5 s, then pauses through the menu and proves the run advanced.
    async function runAndSample(s0: RunResetSnapshot, ballId: string): Promise<{ ball: WorkshopBodyPose }[]> {
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose }[] = [];
        let prevSeq = -1n;
        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== prevSeq) {
                prevSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === ballId);
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

    // A 3 s run of the centred funnel drop: the ball settles in the receiver basket.
    async function runSettlesInReceiver(s0: RunResetSnapshot, label: string): Promise<void> {
        await driver.run(3000);
        await driver.expectRunAdvanced(s0);
        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, `${label}: ball body must be present`);
        assert.ok(ball.py > 2.8 && ball.py < 3.2, `${label}: ball must settle in receiver, py=${ball.py}`);
        assert.ok(Math.abs(ball.px) < 0.2 && Math.abs(ball.pz) < 0.2, `${label}: ball must settle centered in receiver`);
    }

    test('1. Workshop selection: Select funnel from palette, place on canvas', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: Ball dropped on outer shell deflects/bounces without entering bore', { timeout: 150000 }, async () => {
        // Basketball offset at x = 1.8m (strikes outer shell/rim), lifted 160 px
        const s0 = await buildFunnelDrop(1.8, 160);

        await driver.run(3500);
        await driver.expectRunAdvanced(s0);

        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, 'Ball body must be present');
        // Ball strikes outer shell and bounces outward away from receiver
        assert.ok(ball.px > 3.0, `Ball must deflect outward past x=3.0, got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `Ball must land on workbench floor, not in receiver, got py=${ball.py}`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Centered drop: Ball dropped into wide 2.6 m inlet descends smoothly, guided by tapered walls, exits 1.3 m outlet into receiver below', { timeout: 150000 }, async () => {
        const s0 = await buildFunnelDrop(0, 160);
        const samples = await runAndSample(s0, '1');

        assert.ok(samples.length > 30, `Expected >30 distinct pose publications, got ${samples.length}`);

        const initial = samples[0].ball;
        const middle = samples[Math.floor(samples.length / 2)].ball;
        const final = samples[samples.length - 1].ball;

        // Ball starts above inlet
        assert.ok(initial.py > 5.0, `Initial py ${initial.py} should be above funnel inlet`);
        assert.ok(Math.abs(initial.px) < 0.1 && Math.abs(initial.pz) < 0.1, 'Initial ball should be centered');

        // Ball descends through funnel
        assert.ok(middle.py < initial.py - 1.0, `Middle py ${middle.py} should descend from initial ${initial.py}`);

        // Ball settles inside receiver basket
        assert.ok(Math.abs(final.px) < 0.15, `Final px ${final.px} should remain centered in receiver`);
        assert.ok(Math.abs(final.pz) < 0.15, `Final pz ${final.pz} should remain centered in receiver`);
        assert.ok(final.py > 2.8 && final.py < 3.2, `Final py ${final.py} should rest inside receiver basket (~2.96m)`);
        assert.ok(Math.abs(final.vy) < 0.1, `Ball must come to rest vertically, vy=${final.vy}`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Off-center drop: Ball dropped at +0.70 m off-axis gathers into funnel without snagging and exits outlet', { timeout: 150000 }, async () => {
        // Basketball at offset x = 0.7m, lifted 190 px
        const s0 = await buildFunnelDrop(0.7, 190);
        const samples = await runAndSample(s0, '1');

        assert.ok(samples.length > 30, `Expected >30 distinct pose publications, got ${samples.length}`);

        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;

        // Ball starts off-axis near x = 0.7m
        assert.ok(initial.px > 0.55 && initial.px < 0.85, `Initial px ${initial.px} should be near 0.7m offset`);
        assert.ok(initial.py > 5.0, `Initial py ${initial.py} should be elevated`);

        // Funnel tapered wall redirects ball inward; final ball settles in receiver
        assert.ok(Math.abs(final.px) < 0.35, `Final px ${final.px} must gather inward into receiver basket`);
        assert.ok(final.py > 2.8 && final.py < 3.2, `Final py ${final.py} must rest inside receiver basket (~2.96m)`);
        assert.ok(Math.abs(final.vy) < 0.1, `Ball must come to rest vertically, vy=${final.vy}`);

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Joined pipe route: a straight pipe snapped to the Funnel outlet carries the ball smoothly from funnel into tube', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // 1. Basketball (id 1) placed at x = 0.3 m and lifted 230 px (y ~ 7.0 m, above the funnel inlet at y 6.49 m)
        const ballPt = screen(0.3, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballPt.x, ballPt.y);
        await driver.selectPartAt(ballPt.x, ballPt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(230, ballPt.x, ballPt.y);
        await driver.pressKey('Escape');

        // 2. Funnel (id 2) placed at the centre, tilted -90 deg (inlet up) and raised five 0.5 m Height steps to y 5.5 (outlet 4.51 m)
        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.liftSelectedByHeight(5);

        // 3. Story 9R.6: the straight pipe (id 3) snaps to the funnel outlet. Its preview is tilted -90 deg on a placement plane
        // lowered to y 2.5, offered 0.12 m below the outlet; the snap joins the mouths exactly (pipe centre y 2.62).
        await driver.selectTool('pipe');
        await driver.adjustPreview({ tilt: -18, height: -1 });
        const pipeAt = tubeScreen(0, 2.5, 0);
        await driver.hoverCanvas(pipeAt.x, pipeAt.y);
        await driver.placeOnCanvas(pipeAt.x, pipeAt.y);
        await driver.pressKey('Escape');
        const s0 = await driver.captureSnapshot(BENCH);
        assertJoined(s0, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'funnel outlet to pipe');

        // 4. Run and record trajectory
        const samples = await runAndSample(s0, '1');

        assert.ok(samples.length > 30, `Expected >30 distinct pose publications, got ${samples.length}`);

        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;

        // Ball starts above funnel inlet
        assert.ok(initial.py > 6.0, `Initial py ${initial.py} should be above funnel inlet (~6.5m)`);

        // Ball traverses both funnel and pipe, emerging out pipe bottom towards workbench floor
        assert.ok(final.py < 1.0, `Ball must traverse tube and emerge past pipe bottom (py=${final.py})`);

        // While inside funnel and pipe bore (py >= 2.1m), ball stays within bore
        for (const s of samples) {
            if (s.ball.py >= 2.1 && s.ball.py <= 3.9) {
                const r = Math.sqrt(s.ball.px * s.ball.px + s.ball.pz * s.ball.pz);
                assert.ok(r < 0.75, `Radial position r=${r} must stay inside pipe bore during descent`);
            } else if (s.ball.py > 3.9 && s.ball.py <= 6.0) {
                const r = Math.sqrt(s.ball.px * s.ball.px + s.ball.pz * s.ball.pz);
                assert.ok(r < 1.40, `Radial position r=${r} must stay inside funnel bore during descent`);
            }
        }

        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('6. Reset restoration: Reset restores the exact pre-Run snapshot; a second Run settles again and Reset restores it again', { timeout: 150000 }, async () => {
        const s0 = await buildFunnelDrop(0, 160);

        await runSettlesInReceiver(s0, 'first run');
        await resetMatches(s0);
        await runSettlesInReceiver(s0, 'second run');
        await resetMatches(s0);

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('7. Save / Load persistence roundtrip: Save, reload, Load restores construction, re-run reproduces traversal', { timeout: 150000 }, async () => {
        // captureSnapshot saves the construction through the Workshop menu.
        const s0 = await buildFunnelDrop(0, 160);

        // Reload page, then Load via menu
        await driver.reload();
        errors.length = 0;
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);

        await runSettlesInReceiver(loaded, 'loaded run');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
