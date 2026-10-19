// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-051 (Story 8.2).
// Verifies Powered Gate Passageway Barrier:
// 1. Free workshop placement: place powered gate, tilt -15 deg, place basketball upstream.
// 2. Negative control: unpowered gate blocks rolling ball upstream, blade remains closed at y = 0.
// 3. Powered run: wire battery Supply to gate PowerIn; actuator drive retracts blade to 1.55 m, ball rolls through downstream.
// 4. Disconnecting battery: return spring brings blade back closed, blocking rolling ball upstream again.
// 5. State persistence: Save / reload / Load preserves gate, battery, and electrical link bit-identically; replay traverses.
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

function assertElectricalLink(bytes: Buffer, sourceSlot: number, targetSlot: number) {
    const saved = decodeSavedConstruction(bytes);
    const construction = saved.construction()!;
    const count = construction.connectionsLength();
    assert.equal(count, 1, `Expected 1 connection in save, got ${count}`);
    const sourceId = construction.instances(sourceSlot)!.id();
    const targetId = construction.instances(targetSlot)!.id();
    const edge = construction.connections(0)!;
    assert.equal(edge.source(), sourceId, 'Link source should match battery body ID');
    assert.equal(edge.target(), targetId, 'Link target should match gate body ID');
    assert.equal(edge.domain(), ConnectionDomain.Electrical, 'Domain should be Electrical');
    assert.equal(edge.fromSocket(), Socket.Supply, 'Output socket should be Supply');
    assert.equal(edge.toSocket(), Socket.PowerIn, 'Input socket should be PowerIn');
}

describe('CAT-051: Powered Gate Passageway Barrier E2E', () => {
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

    test('1. Free workshop placement: place powered gate, tilt -15 deg, and place basketball upstream', { timeout: 150000 }, async () => {
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

        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: unpowered gate blocks rolling ball upstream, blade remains closed at y = 0', { timeout: 150000 }, async () => {
        errors.length = 0;

        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) {
                    samples.push({ t: Date.now() - started, ball, blade });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

        console.log('SAMPLE 0:', samples[0].ball, 'SAMPLE LAST:', samples[samples.length - 1].ball);
        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;

        // Ball starts upstream inside mouth entrance
        assert.ok(initial.px >= -0.4 && initial.px <= -0.1, `Initial px ${initial.px} should be upstream`);

        // Ball rolls downhill but is stopped by the closed blade
        assert.ok(final.px < initial.px, `Ball should have moved downhill from initial ${initial.px}, got ${final.px}`);
        assert.ok(final.px >= -0.5, `Ball must be stopped by closed blade, got px=${final.px}`);

        // Gate blade must remain closed near baseline y (~ 3.0 m in world space)
        if (samples[samples.length - 1].blade) {
            const blade = samples[samples.length - 1].blade!;
            assert.ok(blade.py < 3.2, `Blade must remain closed at bottom position, got py=${blade.py}`);
        }

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Powered run: wire battery Supply to gate PowerIn; actuator retracts blade to 1.55 m, ball rolls through', { timeout: 150000 }, async () => {
        errors.length = 0;

        // 1. Select battery and place at (920, 510)
        await driver.selectTool('battery');
        await driver.placeOnCanvas(920, 510);

        // 2. Connect battery Supply to gate PowerIn
        await driver.selectPartAt(920, 510);
        await click(driver, 130, 789); // "Connect Supply"
        await click(driver, 720, 485); // Click gate
        await click(driver, 130, 736); // "Supply → PowerIn"
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Verify save binary records the electrical link (Battery is slot 2, Gate is slot 0)
        await driver.save();
        const savedBytes = await saved(driver);
        assertElectricalLink(savedBytes, 2, 0);

        // 3. Run and observe blade retraction and ball traversal
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) {
                    samples.push({ t: Date.now() - started, ball, blade });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

        // Blade must have retracted upward by ~1.55 m along local +Y (world py rises to ~4.5 m)
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(bladeSamples.length > 0, 'Blade body poses should be observed');
        const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
        assert.ok(maxBladeY > 4.2, `Blade must retract open under power, max py=${maxBladeY}`);

        // Ball must pass through the open gate and emerge downstream (px < -0.6)
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < -0.6, `Ball must traverse through gate bore downstream, got px=${finalBall.px}`);
        assert.ok(finalBall.vx < -0.2, `Ball should exit with negative velocity down slope, got vx=${finalBall.vx}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Disconnecting battery closes gate: blade returns closed via spring, blocking rolling ball again', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Disconnect supply: select gate at (720, 485) and click "Disconnect supply"
        await driver.selectPartAt(720, 485);
        await click(driver, 133, 789); // "Disconnect supply" button
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Verify save binary records 0 connections
        await driver.save();
        const savedBytes = await saved(driver);
        assert.equal(decodeSavedConstruction(savedBytes).construction()!.connectionsLength(), 0, 'Connections count should be 0 after disconnect');

        // Run unpowered: gate should remain closed and ball stopped upstream
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                const blade = slot.bodies.find(b => b.id === BLADE_BODY_ID);
                if (ball) {
                    samples.push({ t: Date.now() - started, ball, blade });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);

        const final = samples[samples.length - 1].ball;
        assert.ok(final.px >= -0.5, `Ball must be stopped upstream after disconnect, got px=${final.px}`);

        // Blade stayed closed
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        if (bladeSamples.length > 0) {
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY < 3.2, `Blade must remain closed when disconnected, got max py=${maxBladeY}`);
        }

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence: Save / reload / Load preserves gate, battery and electrical link bit-identically', { timeout: 150000 }, async () => {
        errors.length = 0;

        // Reconnect battery to gate
        await driver.selectPartAt(920, 510);
        await click(driver, 130, 789); // "Connect Supply"
        await click(driver, 720, 485); // Click gate
        await click(driver, 133, 746); // "Supply → PowerIn"
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // Save
        await driver.save();
        const initialSaved = await saved(driver);
        assertElectricalLink(initialSaved, 2, 0);

        // Reload page
        await driver.reload();

        // Load saved state
        await driver.load();
        const loadedSaved = await saved(driver);
        assert.deepEqual(authored(loadedSaved), authored(initialSaved), 'Authored state must roundtrip bit-identically');
        assertElectricalLink(loadedSaved, 2, 0);

        // Run simulation: gate opens and ball rolls downstream
        await driver.run();
        const started = Date.now();
        const samples: { t: number; ball: WorkshopBodyPose }[] = [];
        let previousSeq = -1n;

        while (Date.now() - started < 1800) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                const ball = slot.bodies.find(b => b.id === '2');
                if (ball) {
                    samples.push({ t: Date.now() - started, ball });
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.pauseSimulation();

        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < -0.6, `Reloaded/loaded simulation must allow ball downstream, got px=${finalBall.px}`);

        await driver.reset();
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
