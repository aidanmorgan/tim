// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-020 (Story 9.2).
// Verifies Pulse Counter Event Accumulator:
// 1. Placement and wiring validation: Counter (target 3), Powered Gate (tilt -15 deg), Ball Detector (tilt -90 deg),
//    Upstream Basketball inside gate mouth, Battery, and Lamp.
//    Wired: Detector -> Counter -> Lamp (Activation), Battery -> Counter -> Gate (Electrical).
// 2. Negative control: 2 ball crossings do not reach target 3; gate remains closed, upstream ball stopped, lamp unlit.
// 3. Positive trigger: 3rd ball crossing reaches target count 3, illuminates lamp, retracts gate blade, and ball traverses downstream.
// 4. Saturation: 4th ball crossing does not glitch or re-trigger, saturated state maintains gate open.
// 5. State persistence & reset: Save / reload / Load preserves counter, gate, detector, lamp, and wiring; simulation replay triggers traversal.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';

const ConnectionDomain = { Activation: 1, Electrical: 2 } as const;
const Socket = { ActivationOut: 1, ActivationIn: 4, Supply: 7, PowerIn: 8 } as const;
const BLADE_BODY_ID = '4294967810'; // GpuBodyId: (1n << 32n) + 256n + 1n * 256n + 2n
const LAMP_TARGET = '5'; // Animation target of the Signal lamp's activation node: lamp id 3 + 2
// The detector (tilted −90°) counts a ball only if it starts armed: X < −(0.02 m clearance + 0.34 m radius) = −0.36 m
// (spec-6-8). One lift pixel is 1/57.9 m, so every ball is lifted at least 28 px (0.48 m). The former 18 px ball (0.31 m)
// started inside the clearance and was never counted; the recipe only passed through the deleted in-engine crossing shortcut.
const ARMED_LIFTS = [28, 42, 56, 70] as const;

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

async function click(driver: WorkshopDriver, x: number, y: number) {
    await driver.page.mouse.move(x, y);
    await driver.page.mouse.down();
    await driver.page.waitForTimeout(70);
    await driver.page.mouse.up();
    await driver.page.waitForTimeout(180);
}

async function saved(driver: WorkshopDriver): Promise<Buffer> {
    const value = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error('Existing save unavailable'));
        request.onsuccess = () => {
            const db = request.result;
            try {
                const tx = db.transaction('construction', 'readonly');
                const read = tx.objectStore('construction').get(1);
                tx.oncomplete = () => { db.close(); resolve(Array.from(read.result as Uint8Array)); };
                tx.onabort = () => { db.close(); reject(new Error('Read aborted')); };
            } catch (error) { db.close(); reject(error); }
        };
    }));
    return Buffer.from(value);
}

function assertCounterWiring(bytes: Buffer, gateSlot: number, ballSlot: number, lampSlot: number, battSlot: number, detSlot: number, counterSlot: number) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    const count = construction.connectionsLength();
    assert.equal(count, 4, `Expected 4 connections in save, got ${count}`);
    const gateId = construction.instances(gateSlot)!.id();
    const lampId = construction.instances(lampSlot)!.id();
    const battId = construction.instances(battSlot)!.id();
    const detId = construction.instances(detSlot)!.id();
    const counterId = construction.instances(counterSlot)!.id();

    const hasEdge = (src: bigint, tgt: bigint, dom: number, from: number, to: number) => {
        for (let i = 0; i < count; i++) {
            const edge = construction.connections(i)!;
            if (edge.source() === src && edge.target() === tgt && edge.domain() === dom && edge.fromSocket() === from && edge.toSocket() === to) return true;
        }
        return false;
    };
    assert(hasEdge(counterId, lampId, ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn), 'Missing Counter -> Lamp activation link');
    assert(hasEdge(counterId, gateId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Counter -> Gate electrical link');
    assert(hasEdge(battId, counterId, ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn), 'Missing Battery -> Counter electrical link');
    assert(hasEdge(detId, counterId, ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn), 'Missing Detector -> Counter activation link');
    const counterInst = construction.instances(counterSlot)!;
    assert.equal(counterInst.partId(), 'counter');
    assert.equal(counterInst.threshold(), 3);
}

describe('CAT-020: Pulse Counter Event Accumulator E2E', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            console.log(`[E2E-CONSOLE ${msg.type()}] ${msg.text()}`);
            if (isWorkshopConsoleFailure(msg.type(), msg.text())) errors.push(msg.text());
        });
        driver.page.on('pageerror', (err: Error) => errors.push(err.message));
        const hookWorker = (w: Worker) => w.on('console', (msg: ConsoleMessage) => console.log('WORKER:', msg.text()));
        driver.page.on('worker', hookWorker);
        for (const w of driver.page.workers()) {
            hookWorker(w);
        }
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    test('1. Placement and wiring validation', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Powered gate at (720, 485), tilt -3 steps (-15 deg)
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);

        // Top-row parts:
        // 2. Upstream basketball inside mouth
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);

        // 3. Lamp at (1000, 600)
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(1000, 600);

        // 4. Battery at (1080, 510)
        await driver.selectTool('battery');
        await driver.placeOnCanvas(1080, 510);

        // Bottom-row parts:
        // 5. Ball detector at (450, 485), tilt -18 steps (-90 deg)
        await driver.selectTool('ball_detector');
        await driver.placeOnCanvas(450, 485);
        await driver.selectPartAt(450, 485);
        await driver.tiltSelectedByFineRotate(-18);

        // 6. Counter at (920, 510)
        await driver.selectTool('counter');
        await driver.placeOnCanvas(920, 510);

        // Wire Link 1: Counter ActivationOut -> Lamp ActivationIn (Counter has 2 buttons, row 0 is at 745)
        await driver.selectPartAt(920, 510);
        await click(driver, 100, 745); // "Connect ActivationOut"
        await click(driver, 1000, 600); // Click Lamp
        await click(driver, 133, 736); // Choice "ActivationOut → ActivationIn"

        // Wire Link 2: Counter Supply -> Powered Gate PowerIn (Counter now has 3 buttons, row 1 is at 745)
        await driver.selectPartAt(920, 510);
        await click(driver, 100, 745); // "Connect Supply"
        await click(driver, 720, 485); // Click Gate
        await click(driver, 133, 736); // Choice "Supply → PowerIn"

        // Wire Link 3: Battery Supply -> Counter PowerIn (Battery row 1 is at 789)
        await driver.selectPartAt(1080, 510);
        await click(driver, 130, 789); // "Connect Supply" on battery
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "Supply → PowerIn"

        // Wire Link 4: Detector ActivationOut -> Counter ActivationIn (Detector row 0 is at 792)
        await driver.selectPartAt(450, 485);
        await click(driver, 133, 792); // "Connect ActivationOut" on detector
        await click(driver, 920, 510); // Click Counter
        await click(driver, 133, 736); // Choice "ActivationOut → ActivationIn"

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Verify save binary records all 4 links
        await driver.save();
        const savedBytes = await saved(driver);
        // Instance slots: 0=Gate, 1=Ball, 2=Lamp, 3=Battery, 4=Detector, 5=Counter
        assertCounterWiring(savedBytes, 0, 1, 2, 3, 4, 5);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: 2 ball crossings do not emit, gate closed, lamp unlit, upstream ball stopped', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Place 2 basketballs above ball detector at (450, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(50, 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(100, 450, 485);

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Run simulation: 2 balls fall through detector
        await driver.run();
        try {
            const started = Date.now();
            const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
            let previousSeq = -1n;

            while (Date.now() - started < 1500) {
                const slot = await driver.readLatestPose();
                if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                    previousSeq = BigInt(slot.sequence);
                    const ball = slot.bodies.find(b => b.id === '2');
                    const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                    if (ball) {
                        samples.push({ ball, blade });
                    }
                }
                await driver.page.waitForTimeout(25);
            }

            assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);

            assert.equal((await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length, 0, 'Counter below target never lights its lamp');

            // Gate blade remains closed (py < 3.2)
            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            if (bladeSamples.length > 0) {
                const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
                assert.ok(maxBladeY < 3.2, `Gate blade must remain closed after 2 ball crossings, got max py=${maxBladeY}`);
            }

            // Upstream ball remains stopped by gate (px < 0.2, py > 2.8)
            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px < 0.2, `Upstream ball must remain stopped upstream, got px=${finalBall.px}`);
            assert.ok(finalBall.py > 2.8, `Upstream ball must remain stopped upstream at height, got py=${finalBall.py}`);
        } finally {
            await driver.reset();
            await driver.load();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Positive trigger: 3rd ball crossing reaches target count 3, illuminates lamp, retracts gate blade, and ball traverses downstream', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Place 3 basketballs above ball detector at (450, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[0], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[1], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[2], 450, 485);

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Run simulation: 3 balls fall through detector
        const lampBefore = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;
        await driver.run();
        try {
            const started = Date.now();
            const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
            let previousSeq = -1n;
            let distinctSeqCount = 0;
            let ballMissingCount = 0;

            while (Date.now() - started < 3500) {
                const slot = await driver.readLatestPose();
                if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                    previousSeq = BigInt(slot.sequence);
                    distinctSeqCount++;
                    const ball = slot.bodies.find(b => b.id === '2');
                    const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                    if (ball) {
                        samples.push({ ball, blade });
                    } else ballMissingCount++;
                }
                await driver.page.waitForTimeout(15);
            }

            assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);
            // Blade must have retracted upward under power from counter contact (py > 4.2)
            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            assert.ok(bladeSamples.length > 0, 'Blade body poses should be observed');
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY > 4.2, `Blade must retract open when counter reaches 3, max py=${maxBladeY}`);
            const lampSamples = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
            assert.ok(lampSamples.length > lampBefore && lampSamples[lampSamples.length - 1].value > 0.99, 'Counter → lamp lights at target');

            // Upstream ball must pass through open gate downstream (px > 2.0, vx > 0.2)
            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px > 2.0, `Ball must traverse through gate bore downstream, got px=${finalBall.px}`);
            assert.ok(finalBall.vx > 0.2, `Ball should exit with positive velocity down slope, got vx=${finalBall.vx}`);
        } finally {
            await driver.reset();
            await driver.load();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Saturation: 4th ball crossing does not glitch or re-trigger, saturated state maintains gate open', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Place 4 basketballs above ball detector at (450, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[0], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[1], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[2], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[3], 450, 485);

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        const lampBefore = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;
        await driver.run();
        try {
            const started = Date.now();
            const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
            let previousSeq = -1n;

            while (Date.now() - started < 3500) {
                const slot = await driver.readLatestPose();
                if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                    previousSeq = BigInt(slot.sequence);
                    const ball = slot.bodies.find(b => b.id === '2');
                    const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                    if (ball) {
                        samples.push({ ball, blade });
                    }
                }
                await driver.page.waitForTimeout(15);
            }

            assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);

            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            assert.ok(bladeSamples.length > 0, 'Blade body poses should be observed');
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY > 4.2, `Blade must retract and stay open in saturation, max py=${maxBladeY}`);
            const lampSamples = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
            assert.ok(lampSamples.length > lampBefore && lampSamples[lampSamples.length - 1].value > 0.99, 'Saturated counter keeps its lamp lit');

            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px > 2.0, `Ball must traverse through gate bore downstream in saturation, got px=${finalBall.px}`);
            assert.ok(finalBall.vx > 0.2, `Ball should exit with positive velocity down slope, got vx=${finalBall.vx}`);
        } finally {
            await driver.reset();
            await driver.load();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence & reset: Save / reload / Load preserves counter, gate, detector, lamp, and wiring; simulation replay triggers traversal', { timeout: 150000 }, async () => {
        errors.length = 0;

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Verify base save bytes
        await driver.save();
        const savedBytesBefore = await saved(driver);
        assertCounterWiring(savedBytesBefore, 0, 1, 2, 3, 4, 5);

        // Reload browser page from scratch
        await driver.reload();

        // Load construction from IndexedDB
        await driver.load();
        const loadedBytes = await saved(driver);
        assertCounterWiring(loadedBytes, 0, 1, 2, 3, 4, 5);

        // Place 3 basketballs to replay trigger
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[0], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[1], 450, 485);

        await driver.selectTool('basketball');
        await driver.placeOnCanvas(450, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(ARMED_LIFTS[2], 450, 485);

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        const lampBefore = (await driver.readAnimationSamplesForTarget(LAMP_TARGET)).length;
        await driver.run();
        try {
            const started = Date.now();
            const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
            let previousSeq = -1n;

            while (Date.now() - started < 3500) {
                const slot = await driver.readLatestPose();
                if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                    previousSeq = BigInt(slot.sequence);
                    const ball = slot.bodies.find(b => b.id === '2');
                    const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                    if (ball) {
                        samples.push({ ball, blade });
                    }
                }
                await driver.page.waitForTimeout(15);
            }

            assert.ok(samples.length >= 20, `Expected >=20 distinct pose publications, got ${samples.length}`);
            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY > 4.2, `Replay after reload must retract blade open, max py=${maxBladeY}`);
            const lampSamples = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
            assert.ok(lampSamples.length > lampBefore && lampSamples[lampSamples.length - 1].value > 0.99, 'Replay lights the Counter → lamp output');

            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px > 2.0, `Replay after reload must traverse downstream, got px=${finalBall.px}`);
        } finally {
            // Reset must restore to authored state without residual runtime contact
            await driver.reset();
            await driver.load();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
