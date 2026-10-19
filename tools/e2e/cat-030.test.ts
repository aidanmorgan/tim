// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-030 (Story 6.11).
// Verifies Funnel hollow frustum inlet to standard bore:
// 1. Workshop selection: Select funnel from palette, place on canvas.
// 2. Negative control: Ball dropped on outer shell deflects/bounces without entering bore.
// 3. Centered drop: Ball dropped into wide 2.6 m inlet descends smoothly, guided by tapered walls, exits 1.3 m outlet into receiver below.
// 4. Off-center drop: Ball dropped at +0.70 m off-axis gathers into funnel without snagging and exits outlet.
// 5. Joined pipe route: Funnel outlet positioned above straight pipe, ball smoothly transitions from funnel into tube.
// 6. Reset restoration: Reset restores initial poses; second Run replays bit-for-bit.
// 7. Save / Load persistence roundtrip: Save, reload, Load restores construction, re-run reproduces traversal.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

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

    test('1. Workshop selection: Select funnel from palette, place on canvas', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: Ball dropped on outer shell deflects/bounces without entering bore', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // Place basketball offset at x = 1.8m (strikes outer shell/rim)
        const ballPt = screen(1.8, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballPt.x, ballPt.y);
        await driver.selectPartAt(ballPt.x, ballPt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(160, ballPt.x, ballPt.y);
        await driver.pressKey('Escape');

        // Place funnel at center, tilt -90 deg, lift by 80 px
        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(80, 720, 485, 130);
        await driver.pressKey('Escape');

        // Place receiver directly beneath funnel
        await driver.selectTool('receiver');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');

        // Run simulation
        await driver.run(3500);
        await driver.pauseSimulation();

        const slot = await driver.readLatestPose();
        const ball = slot?.bodies.find(b => b.id === '1');
        assert.ok(ball, 'Ball body must be present');
        // Ball strikes outer shell and bounces outward away from receiver
        assert.ok(ball.px > 3.0, `Ball must deflect outward past x=3.0, got px=${ball.px}`);
        assert.ok(ball.py < 1.0, `Ball must land on workbench floor, not in receiver, got py=${ball.py}`);

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Centered drop: Ball dropped into wide 2.6 m inlet descends smoothly, guided by tapered walls, exits 1.3 m outlet into receiver below', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // 1. Basketball placed at center and lifted 160 px
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(160, 720, 485);
        await driver.pressKey('Escape');

        // 2. Funnel placed at center, tilted -90 deg, lifted 80 px
        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(80, 720, 485, 130);
        await driver.pressKey('Escape');

        // 3. Receiver placed beneath funnel
        await driver.selectTool('receiver');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');

        // 4. Run and record trajectory samples
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose }[] = [];
        let prevSeq = -1n;

        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== prevSeq) {
                prevSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '1');
                if (ball) samples.push({ t: Date.now() - started, ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

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

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Off-center drop: Ball dropped at +0.70 m off-axis gathers into funnel without snagging and exits outlet', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // 1. Basketball placed at offset x = 0.7m, lifted 190 px
        const ballPt = screen(0.7, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballPt.x, ballPt.y);
        await driver.selectPartAt(ballPt.x, ballPt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(190, ballPt.x, ballPt.y);
        await driver.pressKey('Escape');

        // 2. Funnel placed at center, tilted -90 deg, lifted 80 px
        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(80, 720, 485, 130);
        await driver.pressKey('Escape');

        // 3. Receiver placed beneath funnel
        await driver.selectTool('receiver');
        await driver.placeOnCanvas(720, 485);
        await driver.pressKey('Escape');

        // 4. Run and record trajectory samples
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose }[] = [];
        let prevSeq = -1n;

        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== prevSeq) {
                prevSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '1');
                if (ball) samples.push({ t: Date.now() - started, ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

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

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Joined pipe route: Funnel outlet positioned above straight pipe, ball smoothly transitions from funnel into tube', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // 1. Funnel placed first at center (720, 485), tilted -90 deg, lifted 104 px (1.8m to y=4.8m)
        await driver.selectTool('funnel');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(104, 720, 485, 130);
        await driver.pressKey('Escape');

        // 2. Straight pipe placed at center (720, 485) beneath elevated funnel, tilted -90 deg
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.pressKey('Escape');

        // 3. Basketball placed at x = 0.3m, lifted 210 px (above funnel inlet at y ~ 6.5m)
        const ballPt = screen(0.3, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballPt.x, ballPt.y);
        await driver.selectPartAt(ballPt.x, ballPt.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(210, ballPt.x, ballPt.y);
        await driver.pressKey('Escape');

        // 4. Run and record trajectory
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose }[] = [];
        let prevSeq = -1n;

        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== prevSeq) {
                prevSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '3');
                if (ball) samples.push({ t: Date.now() - started, ball });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

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

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('6. Reset restoration: Reset restores initial poses; second Run replays bit-for-bit', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // Setup funnel drop into receiver
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(160, 720, 485);
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

        // First Run
        await driver.run(3000);
        await driver.pauseSimulation();

        const firstSlot = await driver.readLatestPose();
        const firstBall = firstSlot?.bodies.find(b => b.id === '1');
        assert.ok(firstBall, 'Ball body must be present after first run');
        assert.ok(firstBall.py > 2.8 && firstBall.py < 3.2, `First run ball in receiver, py=${firstBall.py}`);

        // Reset
        await driver.reset();

        // Second Run
        await driver.run(3000);
        await driver.pauseSimulation();

        const replaySlot = await driver.readLatestPose();
        const replayBall = replaySlot?.bodies.find(b => b.id === '1');
        assert.ok(replayBall, 'Ball body must be present after replay');
        assert.ok(Math.abs(replayBall.px - firstBall.px) < 0.01, `Replay px ${replayBall.px} must match first run ${firstBall.px}`);
        assert.ok(Math.abs(replayBall.py - firstBall.py) < 0.01, `Replay py ${replayBall.py} must match first run ${firstBall.py}`);
        assert.ok(Math.abs(replayBall.pz - firstBall.pz) < 0.01, `Replay pz ${replayBall.pz} must match first run ${firstBall.pz}`);

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('7. Save / Load persistence roundtrip: Save, reload, Load restores construction, re-run reproduces traversal', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;

        // Setup funnel drop into receiver
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(160, 720, 485);
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

        // Save via menu
        await driver.save();

        // Reload page
        await driver.reload();
        errors.length = 0;

        // Load via menu
        await driver.load();

        // Run simulation
        await driver.run(3000);
        await driver.pauseSimulation();

        const loadedSlot = await driver.readLatestPose();
        const loadedBall = loadedSlot?.bodies.find(b => b.id === '1');
        assert.ok(loadedBall, 'Ball body must be present after load');
        assert.ok(loadedBall.py > 2.8 && loadedBall.py < 3.2, `Loaded run ball must settle in receiver, py=${loadedBall.py}`);
        assert.ok(Math.abs(loadedBall.px) < 0.2 && Math.abs(loadedBall.pz) < 0.2, 'Loaded run ball must settle centered in receiver');

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
