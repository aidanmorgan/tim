// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-052 (Story 9.1; lifecycle re-verified by Story 9R.1).
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
// Story 9R.4 (each plate evaluates its own compiled contact-load row):
// 6. Sum: two 0.4 kg dominoes standing on the plate sum to 0.8 kg >= 0.5 kg, so the gate opens and the ball passes.
// 7. Removal: on a plate tilted 30° (six fine-rotate steps) the bowling ball lands, loads the plate (the blade rises), then rolls
//    off; the contact opens on the next tick, so the blade returns closed. Reset matches the snapshot (plate Empty, gate closed).
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run; each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState } from './workshop-wire.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

const ConnectionDomain = { Electrical: 2 } as const;
const Socket = { Supply: 7, PowerIn: 8 } as const;

const screen = (x: number, z: number) => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});

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

// What rests on the plate: nothing, a light domino, two dominoes, a 4 kg bowling ball lifted just above it, or that ball lifted
// above the plate tilted 30° so that it rolls off.
type PlateLoad = 'none' | 'domino' | 'dominoes' | 'bowling' | 'rolloff';
// Two standing dominoes on the plate face (plate centre world (3, -1.05)), 0.8 m apart along x.
const DOMINO_LEFT = screen(2.6, -1.05);
const DOMINO_RIGHT = screen(3.4, -1.05);
// Move-gizmo lift (about 56 px/m): 0.68 m raises a standing domino's centre from the 3 m plane to 3.68 m, its foot 0.05 m above
// the plate face.
const DOMINO_LIFT_PX = 38;

describe('CAT-052: Pressure Plate Surface Contact Load Sensor E2E', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    const BLADE_BODY_ID = '4294967810'; // GpuBodyId: (1n << 32n) + 256n + 1n * 256n + 2n
    // The committed (dynamic) bodies: the basketball and the gate blade, plus the domino or bowling ball when one is placed.
    const bench = (load: PlateLoad) => ({ bodies: load === 'none' ? 2 : load === 'dominoes' ? 4 : 3 });

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

    // Gate at the centre tilted -15 deg, basketball upstream, plate at (920, 510), battery at (1080, 510), wired
    // Plate Supply -> Gate PowerIn then Battery Supply -> Plate PowerIn, then the plate's load.
    async function buildPlateBench(load: PlateLoad): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);
        await driver.selectTool('pressure_plate');
        await driver.placeOnCanvas(920, 510);
        await driver.selectTool('battery');
        await driver.placeOnCanvas(1080, 510);

        await driver.selectPartAt(920, 510);
        await driver.clickAt(130, 789); // "Connect Supply" on pressure plate
        await driver.clickAt(720, 485); // Click powered gate
        await driver.clickAt(133, 736); // "Supply → PowerIn"
        await driver.selectPartAt(1080, 510);
        await driver.clickAt(130, 789); // "Connect Supply" on battery
        await driver.clickAt(920, 510); // Click pressure plate
        await driver.clickAt(133, 736); // "Supply → PowerIn"

        if (load === 'rolloff') {
            await driver.selectPartAt(920, 510);
            await driver.tiltSelectedByFineRotate(6);
        }
        if (load === 'domino' || load === 'dominoes') {
            // A domino placed at the 3 m placement plane straddles the plate's top; the move gizmo lifts it so its foot rests
            // just above the face (y 3.08) and it stands on the plate.
            for (const at of load === 'domino' ? [{ x: 920, y: 510 }] : [DOMINO_LEFT, DOMINO_RIGHT]) {
                await driver.selectTool('domino');
                await driver.placeOnCanvas(at.x, at.y);
                await driver.setPartMode('move');
                await driver.liftSelectedPart(DOMINO_LIFT_PX, at.x, at.y);
                await driver.page.keyboard.press('Escape');
                await driver.page.waitForTimeout(200);
            }
        } else if (load === 'bowling' || load === 'rolloff') {
            await driver.selectTool('bowling');
            await driver.placeOnCanvas(920, 510);
            await driver.setPartMode('move');
            await driver.liftSelectedPart(12, 920, 510);
            await driver.page.keyboard.press('Escape');
            await driver.page.waitForTimeout(200);
        }
        return driver.captureSnapshot(bench(load));
    }

    async function runAndSample(s0: RunResetSnapshot, windowMs: number): Promise<{ ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[]> {
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;
        while (Date.now() - started < windowMs) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) samples.push({ ball, blade });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.expectRunAdvanced(s0);
        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);
        return samples;
    }

    // Control that a domino row is not vacuous: after the run each domino (instances 5 and 6, after gate, ball, plate and battery)
    // still stands on the plate face, its centre 0.55 m above y 3.08 and inside the face's x extent [2.1, 3.9].
    async function assertDominoesStand(ids: string[]): Promise<void> {
        const slot = await driver.readLatestPose();
        assert.ok(slot, 'the paused run publishes a pose');
        for (const id of ids) {
            const domino = slot.bodies.find(b => b.id === id);
            assert.ok(domino, `domino ${id} is in the pose ring`);
            assert.ok(Math.abs(domino.py - 3.63) < 0.03 && domino.px > 2.1 && domino.px < 3.9,
                `domino ${id} stands on the plate face, got (${domino.px}, ${domino.py}, ${domino.pz})`);
        }
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    function assertBlocked(samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[], label: string): void {
        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;
        assert.ok(final.px < initial.px, `${label}: ball should move downhill from initial ${initial.px}, got ${final.px}`);
        assert.ok(final.px >= -0.5, `${label}: ball must be stopped by closed blade, got px=${final.px}`);
        const blade = samples[samples.length - 1].blade;
        if (blade) assert.ok(blade.py < 3.2, `${label}: blade must remain closed at bottom position, got py=${blade.py}`);
    }

    function assertOpened(samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[], label: string): void {
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(bladeSamples.length > 0, `${label}: blade body poses should be observed`);
        const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
        assert.ok(maxBladeY > 4.2, `${label}: blade must retract open when pressure plate is loaded, max py=${maxBladeY}`);
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < -0.6, `${label}: ball must traverse through gate bore downstream, got px=${finalBall.px}`);
        assert.ok(finalBall.vx < -0.2, `${label}: ball should exit with negative velocity down slope, got vx=${finalBall.vx}`);
    }

    test('1. Free workshop placement & wiring: place gate, basketball, plate, and battery; wire Battery -> Plate -> Gate', { timeout: 150000 }, async () => {
        await buildPlateBench('none');
        // The saved slot records both electrical links (Gate is slot 0, Plate is slot 2, Battery is slot 3)
        assertPressurePlateWiring(await saved(driver), 3, 2, 0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: unweighted pressure plate leaves contact open, gate remains closed, blocking basketball', { timeout: 150000 }, async () => {
        const s0 = await buildPlateBench('none');
        for (const cycle of ['first run', 'second run']) {
            assertBlocked(await runAndSample(s0, 1800), cycle);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Underweight control: placing lightweight domino on pressure plate does not trigger contact (< 0.5 kg)', { timeout: 150000 }, async () => {
        const s0 = await buildPlateBench('domino');
        const samples = await runAndSample(s0, 1800);
        const final = samples[samples.length - 1].ball;
        assert.ok(final.px >= -0.5, `Ball must remain stopped upstream under light load, got px=${final.px}`);
        const blade = samples[samples.length - 1].blade;
        if (blade) assert.ok(blade.py < 3.2, `Blade must remain closed under light load, got py=${blade.py}`);
        await assertDominoesStand(['5']);
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Loaded trigger: bowling ball (5.0 kg >= 0.5 kg) resting on plate closes contact, opens gate, ball rolls through', { timeout: 150000 }, async () => {
        const s0 = await buildPlateBench('bowling');
        for (const cycle of ['first run', 'second run']) {
            assertOpened(await runAndSample(s0, 3500), cycle);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence: Save / reload / Load preserves gate, battery, plate and electrical links bit-identically', { timeout: 150000 }, async () => {
        // captureSnapshot saves the bench with the bowling ball on the plate.
        const s0 = await buildPlateBench('bowling');
        const initialSaved = await saved(driver);
        assertPressurePlateWiring(initialSaved, 3, 2, 0);

        // Reload page, then Load the saved state
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(bench('bowling'));
        assertLoadRestores(s0, loaded);
        const loadedSaved = await saved(driver);
        assert.deepEqual(authored(loadedSaved), authored(initialSaved), 'Authored state must roundtrip bit-identically');
        assertPressurePlateWiring(loadedSaved, 3, 2, 0);

        // Run simulation: gate opens and ball rolls downstream
        const samples = await runAndSample(loaded, 3500);
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < -0.6, `Reloaded/loaded simulation must allow ball downstream, got px=${finalBall.px}`);
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('6. Sum: two dominoes (0.8 kg) on the plate close its contact, the gate opens and the ball passes', { timeout: 150000 }, async () => {
        const s0 = await buildPlateBench('dominoes');
        const plate = decodeSavedConstruction(await saved(driver)).construction()!.instances(2)!;
        assert.equal(plate.threshold(), 0.5, 'the plate keeps its 0.5 kg minimum');
        assertOpened(await runAndSample(s0, 3500), 'two dominoes');
        await assertDominoesStand(['5', '6']);
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('7. Removal: the ball rolls off a 30° plate, its contact opens and the gate closes again', { timeout: 150000 }, async () => {
        const s0 = await buildPlateBench('rolloff');
        const samples = await runAndSample(s0, 4500);
        const blades = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(blades.length > 20, `blade poses observed, got ${blades.length}`);
        const highest = Math.max(...blades.map(b => b.py));
        const last = blades[blades.length - 1].py;
        assert.ok(highest > 3.4, `the landed ball loads the plate and the blade rises, max py=${highest}`);
        assert.ok(last < 3.2, `the ball rolled off, the contact opened and the blade returned closed, final py=${last}`);
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
