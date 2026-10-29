// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-051 (Story 8.2; lifecycle re-verified by Story 9R.1).
// Verifies Powered Gate Passageway Barrier:
// 1. Free workshop placement: place powered gate, tilt -15 deg, place basketball upstream.
// 2. Negative control: unpowered gate blocks rolling ball upstream, blade remains closed at y = 0.
// 3. Powered run: wire battery Supply to gate PowerIn; actuator drive retracts blade to 1.55 m, ball rolls through downstream.
// 4. Disconnecting battery: return spring brings blade back closed, blocking rolling ball upstream again.
// 5. State persistence: Save / reload / Load preserves gate, battery, and electrical link bit-identically; replay traverses.
// Story 9R.1: every test reloads and builds its own bench, and every Reset is followed by the exact snapshot assertion
// (driver.expectResetMatches) against the snapshot taken before the first Run, including the blade's authored closed pose;
// each Run cycle first proves it advanced.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState } from './workshop-wire.ts';
import { assertLoadRestores, snapshotEntries } from './run-reset-snapshot.ts';

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
    const BLADE = `read.bodies[id=${BLADE_BODY_ID}]`;
    // The committed (dynamic) bodies of every bench: the basketball and the gate's prismatic blade.
    const BENCH = { bodies: 2 } as const;

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

    // Powered gate at the canvas centre (0, 3, 0) tilted -15 deg (3 fine-rotate steps), a basketball upstream in the mouth.
    async function buildGate(): Promise<void> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-3);
        const ballScreen = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ballScreen.x, ballScreen.y);
    }

    // The gate bench plus a battery at (920, 510) wired Supply -> gate PowerIn.
    async function buildPoweredGate(): Promise<void> {
        await buildGate();
        await driver.selectTool('battery');
        await driver.placeOnCanvas(920, 510);
        await driver.selectPartAt(920, 510);
        await click(driver, 130, 789); // "Connect Supply"
        await click(driver, 720, 485); // Click gate
        await click(driver, 130, 736); // "Supply → PowerIn"
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);
    }

    async function runAndSample(s0: RunResetSnapshot): Promise<{ ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[]> {
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
                if (ball) samples.push({ ball, blade });
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.expectRunAdvanced(s0);
        assert.ok(samples.length > 20, `Expected >20 distinct pose publications, got ${samples.length}`);
        return samples;
    }

    // After Reset the snapshot is exact, and the blade's committed body read (cell, local, rotation, centre of mass, velocities) is
    // bit-equal to its authored closed pose: the admission read before the first Run.
    async function resetMatchesWithBladeClosed(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        const s1 = await driver.expectResetMatches(s0);
        const authoredClosed = snapshotEntries(s0, BLADE);
        assert.ok(authoredClosed.length > 0, `The committed read publishes the blade body ${BLADE_BODY_ID}`);
        assert.deepEqual(snapshotEntries(s1, BLADE), authoredClosed, 'After Reset the blade is at its authored closed pose');
    }

    function assertBlocked(samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[], label: string): void {
        const initial = samples[0].ball;
        const final = samples[samples.length - 1].ball;
        assert.ok(initial.px >= -0.4 && initial.px <= -0.1, `${label}: initial px ${initial.px} should be upstream`);
        assert.ok(final.px < initial.px, `${label}: ball should have moved downhill from initial ${initial.px}, got ${final.px}`);
        assert.ok(final.px >= -0.5, `${label}: ball must be stopped by closed blade, got px=${final.px}`);
        const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        if (bladeSamples.length > 0) {
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY < 3.2, `${label}: blade must remain closed at bottom position, got max py=${maxBladeY}`);
        }
    }

    test('1. Free workshop placement: place powered gate, tilt -15 deg, and place basketball upstream', { timeout: 150000 }, async () => {
        await buildGate();
        const s0 = await driver.captureSnapshot(BENCH);
        assert.ok(snapshotEntries(s0, BLADE).length > 0, `The committed read publishes the blade body ${BLADE_BODY_ID}`);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Negative control: unpowered gate blocks rolling ball upstream, blade remains closed at y = 0', { timeout: 150000 }, async () => {
        await buildGate();
        const s0 = await driver.captureSnapshot(BENCH);

        for (const cycle of ['first run', 'second run']) {
            assertBlocked(await runAndSample(s0), cycle);
            await resetMatchesWithBladeClosed(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Powered run: wire battery Supply to gate PowerIn; actuator retracts blade to 1.55 m, ball rolls through', { timeout: 150000 }, async () => {
        await buildPoweredGate();
        const s0 = await driver.captureSnapshot(BENCH);
        // The saved slot records the electrical link (Battery is slot 2, Gate is slot 0)
        assertElectricalLink(await saved(driver), 2, 0);

        for (const cycle of ['first run', 'second run']) {
            const samples = await runAndSample(s0);
            // Blade must have retracted upward by ~1.55 m along local +Y (world py rises to ~4.5 m)
            const bladeSamples = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
            assert.ok(bladeSamples.length > 0, `${cycle}: blade body poses should be observed`);
            const maxBladeY = Math.max(...bladeSamples.map(b => b.py));
            assert.ok(maxBladeY > 4.2, `${cycle}: blade must retract open under power, max py=${maxBladeY}`);
            // Ball must pass through the open gate and emerge downstream (px < -0.6)
            const finalBall = samples[samples.length - 1].ball;
            assert.ok(finalBall.px < -0.6, `${cycle}: ball must traverse through gate bore downstream, got px=${finalBall.px}`);
            assert.ok(finalBall.vx < -0.2, `${cycle}: ball should exit with negative velocity down slope, got vx=${finalBall.vx}`);
            await resetMatchesWithBladeClosed(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Disconnecting battery closes gate: blade returns closed via spring, blocking rolling ball again', { timeout: 150000 }, async () => {
        await buildPoweredGate();

        // Disconnect supply: select gate at (720, 485) and click "Disconnect supply"
        await driver.selectPartAt(720, 485);
        await click(driver, 133, 789); // "Disconnect supply" button
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);

        // The saved slot records 0 connections
        const s0 = await driver.captureSnapshot(BENCH);
        assert.equal(decodeSavedConstruction(await saved(driver)).construction()!.connectionsLength(), 0, 'Connections count should be 0 after disconnect');

        // Run unpowered: gate should remain closed and ball stopped upstream
        assertBlocked(await runAndSample(s0), 'disconnected run');
        await resetMatchesWithBladeClosed(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. State persistence: Save / reload / Load preserves gate, battery and electrical link bit-identically', { timeout: 150000 }, async () => {
        await buildPoweredGate();
        const s0 = await driver.captureSnapshot(BENCH);
        const initialSaved = await saved(driver);
        assertElectricalLink(initialSaved, 2, 0);

        // Reload page, then Load the saved state
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        const loadedSaved = await saved(driver);
        assert.deepEqual(authored(loadedSaved), authored(initialSaved), 'Authored state must roundtrip bit-identically');
        assertElectricalLink(loadedSaved, 2, 0);

        // Run simulation: gate opens and ball rolls downstream
        const samples = await runAndSample(loaded);
        const finalBall = samples[samples.length - 1].ball;
        assert.ok(finalBall.px < -0.6, `Reloaded/loaded simulation must allow ball downstream, got px=${finalBall.px}`);

        await resetMatchesWithBladeClosed(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
