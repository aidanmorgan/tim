// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-052 (Story 9.1).
// Verifies Pressure Plate Surface Contact Load Sensor:
// 1. Free workshop placement: place powered gate (tilt -15 deg), place basketball upstream,
//    place pressure plate at (920, 510), place battery at (1080, 510).
//    Wire Battery Supply -> PressurePlate PowerIn, and PressurePlate Supply -> PoweredGate PowerIn.
// 2. Negative control: unweighted pressure plate leaves internal contact open; powered gate
//    blade remains closed at y = 0, stopping the rolling basketball upstream.
// 3. Underweight control: placing lightweight domino on the plate does not trigger contact (load < 0.5 kg).
// 4. Loaded trigger: placing heavy bowling ball (5.0 kg >= 0.5 kg) on pressure plate closes contact;
//    actuator retracts gate blade to ~1.55 m, and basketball rolls through downstream.
// 5. State persistence: Save / reload / Load preserves gate, battery, pressure plate, and electrical
//    connections bit-identically; replay traverses.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState } from './workshop-wire.ts';

const ConnectionDomain = { Electrical: 2 } as const;
const Socket = { Supply: 7, PowerIn: 8 } as const;

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

const authored = (bytes: Buffer) => authoredState(bytes);

function assertPressurePlateWiring(bytes: Buffer, batterySlot: number, plateSlot: number, gateSlot: number) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    const count = construction.connectionsLength();
    assert.equal(count, 2, `Expected 2 connections in save, got ${count}`);
    const batteryId = construction.instances(batterySlot)!.id();
    const plateId = construction.instances(plateSlot)!.id();
    const gateId = construction.instances(gateSlot)!.id();

    const edges = [construction.connections(0)!, construction.connections(1)!];

    const batteryToPlate = edges.find(e => e.source() === batteryId && e.target() === plateId);
    assert.ok(batteryToPlate, 'Connection from Battery to PressurePlate should exist');
    assert.equal(batteryToPlate.domain(), ConnectionDomain.Electrical);
    assert.equal(batteryToPlate.fromSocket(), Socket.Supply);
    assert.equal(batteryToPlate.toSocket(), Socket.PowerIn);

    const plateToGate = edges.find(e => e.source() === plateId && e.target() === gateId);
    assert.ok(plateToGate, 'Connection from PressurePlate to PoweredGate should exist');
    assert.equal(plateToGate.domain(), ConnectionDomain.Electrical);
    assert.equal(plateToGate.fromSocket(), Socket.Supply);
    assert.equal(plateToGate.toSocket(), Socket.PowerIn);

    const plateInst = construction.instances(plateSlot)!;
    assert.equal(plateInst.partId(), 'pressure_plate');
    assert.equal(plateInst.threshold(), 0.5);
}

describe('CAT-052: Pressure Plate Surface Contact Load Sensor E2E', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    const BLADE_BODY_ID = '4294967810'; // GpuBodyId: (1n << 32n) + 256n + 1n * 256n + 2n

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            console.log(`[E2E-CONSOLE ${msg.type()}] ${msg.text()}`);
            if (isWorkshopConsoleFailure(msg.type(), msg.text())) errors.push(msg.text());
        });
        driver.page.on('pageerror', (err: Error) => errors.push(err.message));
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    test('1. Free workshop placement & wiring: place gate, basketball, plate, and battery; wire Battery -> Plate -> Gate', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();

        // 1. Select powered_gate and place at canvas center (0, 3, 0) -> screen (720, 485)
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);

        // 2. Select placed gate and tilt by -3 steps (-15 deg about Z)
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);

        // 3. Select basketball and place upstream inside the mouth entrance
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);

        // 4. Select pressure_plate and place at (920, 510)
        await driver.selectTool('pressure_plate');
        await driver.placeOnCanvas(920, 510);

        // 5. Select battery and place at (1080, 510)
        await driver.selectTool('battery');
        await driver.placeOnCanvas(1080, 510);

        // 6. Connect pressure plate Supply to powered gate PowerIn FIRST
        await driver.selectPartAt(920, 510);
        await driver.clickAt(130, 789); // "Connect Supply" on pressure plate
        await driver.clickAt(720, 485); // Click powered gate
        await driver.clickAt(133, 736); // "Supply → PowerIn"

        // 7. Connect battery Supply to pressure plate PowerIn SECOND
        await driver.selectPartAt(1080, 510);
        await driver.clickAt(130, 789); // "Connect Supply" on battery
        await driver.clickAt(920, 510); // Click pressure plate
        await driver.clickAt(133, 736); // "Supply → PowerIn"

        // Verify save binary records both electrical links (Gate is slot 0, Plate is slot 2, Battery is slot 3)
        await driver.save();
        const savedBytes = await saved(driver);
        assertPressurePlateWiring(savedBytes, 3, 2, 0);

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: unweighted pressure plate leaves contact open, gate remains closed, blocking basketball', { timeout: 150000 }, async () => {
        errors.length = 0;

        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
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

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;

        // Ball rolls downhill but is stopped by closed blade
        assert.ok(final.px < initial.px, `Ball should move downhill from initial ${initial.px}, got ${final.px}`);
        assert.ok(final.px >= -0.5, `Ball must be stopped by closed blade, got px=${final.px}`);

        // Gate blade remains closed near baseline y (~ 3.0 m)
        if (samples[samples.length - 1].blade) {
            const blade = samples[samples.length - 1].blade!;
            assert.ok(blade.py < 3.2, `Blade must remain closed at bottom position, got py=${blade.py}`);
        }

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Underweight control: placing lightweight domino on pressure plate does not trigger contact (< 0.5 kg)', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Place a domino at (920, 510)
        await driver.selectTool('domino');
        await driver.placeOnCanvas(920, 510);

        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
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

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

        const final = samples[samples.length - 1].ball;
        assert.ok(final.px >= -0.5, `Ball must remain stopped upstream under light load, got px=${final.px}`);

        if (samples[samples.length - 1].blade) {
            const blade = samples[samples.length - 1].blade!;
            assert.ok(blade.py < 3.2, `Blade must remain closed under light load, got py=${blade.py}`);
        }

        await driver.reset();
        await driver.load();

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Loaded trigger: bowling ball (5.0 kg >= 0.5 kg) resting on plate closes contact, opens gate, ball rolls through', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Place bowling ball at (920, 510) and lift above pressure plate
        await driver.selectTool('bowling');
        await driver.placeOnCanvas(920, 510);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(12, 920, 510);
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Run and observe blade retraction and ball traversal
        await driver.run();
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
            await driver.page.waitForTimeout(25);
        }

        try {
            assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

            // Blade must have retracted upward under power from the pressure plate
            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            assert.ok(bladeSamples.length > 0, 'Blade body poses should be observed');
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY > 4.2, `Blade must retract open when pressure plate is loaded, max py=${maxBladeY}`);

            // Basketball must pass through the open gate and emerge downstream (px < -0.6)
            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px < -0.6, `Ball must traverse through gate bore downstream, got px=${finalBall.px}`);
            assert.ok(finalBall.vx < -0.2, `Ball should exit with negative velocity down slope, got vx=${finalBall.vx}`);
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence: Save / reload / Load preserves gate, battery, plate and electrical links bit-identically', { timeout: 150000 }, async () => {
        errors.length = 0;

        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Save initial state with bowling ball on plate
        await driver.save();
        const initialSaved = await saved(driver);
        assertPressurePlateWiring(initialSaved, 3, 2, 0);

        // Reload page
        await driver.reload();

        // Load saved state
        await driver.load();
        const loadedSaved = await saved(driver);
        assert.deepEqual(authored(loadedSaved), authored(initialSaved), 'Authored state must roundtrip bit-identically');
        assertPressurePlateWiring(loadedSaved, 3, 2, 0);

        // Run simulation: gate opens and ball rolls downstream
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

        try {
            assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);
            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px < -0.6, `Reloaded/loaded simulation must allow ball downstream, got px=${finalBall.px}`);
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
