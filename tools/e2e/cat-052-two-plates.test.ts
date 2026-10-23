// Pure TypeScript Playwright-driven E2E acceptance suite for Story 9R.4: each Pressure Plate switches only its own gate.
// Every bench is built with the actual Workshop UI (palette, canvas clicks, the lift handle, the contextual Connect Supply /
// Supply → PowerIn choices). Observation is read-only: the pose ring, the Save acknowledgement and the save slot.
// Two circuits, B1 → P1 → G1 and B2 → P2 → G2, each plate at the default 0.5 kg minimum:
// 1. A 4 kg bowling ball resting on P1 opens G1 only; G2 stays closed throughout (negative control for cross-talk). Two cycles,
//    each Reset matching the pre-Run snapshot (both plates Empty, both gates closed).
// 2. The mirror: the ball on P2 opens G2 only; G1 stays closed throughout. Reset matches.
// 3. Save/Load preserves both circuits (four Supply → PowerIn links) and both plates' minimum_mass bit-identically, and the
//    loaded bench replays: only G1 opens.
// 4. No activation-slot limit (owner decision, 20 Oct 2026): 24 activation parts (9 plates, 5 counters, 5 clocks, 5 latches) are
//    admitted; only G1 opens; Reset matches the snapshot and Save/Load restores it exactly.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState, parameter } from './workshop-wire.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

const ConnectionDomain = { Electrical: 2 } as const;
const Socket = { Supply: 7, PowerIn: 8 } as const;

type Point = { x: number; y: number };
// World (x, z) on the bench to its screen pixel (the cat-051 projection).
const screen = (x: number, z: number): Point => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});
const GATE_1: Point = { x: 720, y: 485 };
const PLATE_1: Point = { x: 920, y: 510 };    // the cat-052 plate position, world (3, -1.05)
const BATTERY_1: Point = { x: 1080, y: 510 };
const GATE_2: Point = screen(0, 4);           // (573, 584): 4 m nearer the viewer than gate 1
const PLATE_2: Point = { x: GATE_2.x + 200, y: GATE_2.y + 25 };
const BATTERY_2: Point = { x: GATE_2.x + 360, y: GATE_2.y + 25 };
const ROW_CONNECT = 789;                      // "Connect Supply" on a selected plate or battery
const CHOICE_POWER_IN: Point = { x: 133, y: 736 }; // the single text choice "Supply → PowerIn"
const CLOSED_MAX_PY = 3.2; // blade rest height is 3.0 m
const OPEN_MIN_PY = 4.2;   // fully open blade reaches 4.55 m
// GpuBodyId of the gate blade for instance id n: (1 << 32) + 256 + n * 256 + 2. The gates are placed first (instances 1 and 2).
const bladeId = (instance: number) => String((1n << 32n) + 256n + BigInt(instance) * 256n + 2n);
const BLADES = [bladeId(1), bladeId(2)];
// The committed dynamic bodies: both blades and the bowling ball.
const BENCH = { bodies: 3 };

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

/** The saved Supply → PowerIn links as `source→target` instance ids, and every plate's saved minimum_mass. */
function savedCircuits(bytes: Buffer): { links: string[]; minimums: number[] } {
    const construction = decodeSavedConstruction(bytes).construction()!;
    const links: string[] = [];
    for (let i = 0; i < construction.connectionsLength(); i++) {
        const edge = construction.connections(i)!;
        assert.equal(edge.domain(), ConnectionDomain.Electrical, 'every saved link is electrical');
        assert.equal(edge.fromSocket(), Socket.Supply, 'every saved link leaves Supply');
        assert.equal(edge.toSocket(), Socket.PowerIn, 'every saved link enters PowerIn');
        links.push(`${edge.source()}→${edge.target()}`);
    }
    const minimums: number[] = [];
    for (let i = 0; i < construction.instancesLength(); i++) {
        const instance = construction.instances(i)!;
        if (instance.partId() === 'pressure_plate') minimums.push(parameter(instance, 'minimum_mass'));
    }
    return { links: links.sort(), minimums };
}

describe('CAT-052: two Pressure Plates switch only their own gates (Story 9R.4)', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

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

    async function wire(source: Point, target: Point) {
        await driver.selectPartAt(source.x, source.y);
        await driver.clickAt(130, ROW_CONNECT);
        await driver.clickAt(target.x, target.y);
        await driver.clickAt(CHOICE_POWER_IN.x, CHOICE_POWER_IN.y);
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);
    }

    // G1, G2, P1, P2, B1, B2 (instances 1-6), wired B1 → P1 → G1 and B2 → P2 → G2, then a bowling ball placed onto `loaded` (it rests on the face, F-1).
    async function buildTwoPlates(loaded: Point): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        for (const [tool, at] of [['powered_gate', GATE_1], ['powered_gate', GATE_2], ['pressure_plate', PLATE_1],
            ['pressure_plate', PLATE_2], ['battery', BATTERY_1], ['battery', BATTERY_2]] as const) {
            await driver.selectTool(tool);
            await driver.placeOnCanvas(at.x, at.y);
        }
        await wire(PLATE_1, GATE_1);
        await wire(BATTERY_1, PLATE_1);
        await wire(PLATE_2, GATE_2);
        await wire(BATTERY_2, PLATE_2);
        // F.6 F-1 (C-Plate): the bowling ball placed from the drawer onto the flat plate rests on its face; no gizmo lift.
        await driver.selectTool('bowling');
        await driver.placeOnCanvas(loaded.x, loaded.y);
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);
        return driver.captureSnapshot(BENCH);
    }

    // Run, then sample both blades' poses over the window; the Pause acknowledgement proves the cycle advanced.
    async function runAndSample(s0: RunResetSnapshot, windowMs: number): Promise<Map<string, WorkshopBodyPose[]>> {
        await driver.run();
        const started = Date.now();
        const blades = new Map<string, WorkshopBodyPose[]>(BLADES.map(id => [id, []]));
        let previousSeq = -1n;
        while (Date.now() - started < windowMs) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence && BigInt(slot.sequence) !== previousSeq) {
                previousSeq = BigInt(slot.sequence);
                for (const id of BLADES) {
                    const blade = slot.bodies.find(b => b.id === id);
                    if (blade) blades.get(id)!.push(blade);
                }
            }
            await driver.page.waitForTimeout(25);
        }
        await driver.expectRunAdvanced(s0);
        for (const id of BLADES) assert.ok(blades.get(id)!.length > 20, `Expected >20 poses of blade ${id}, got ${blades.get(id)!.length}`);
        return blades;
    }

    function assertOnlyOpens(blades: Map<string, WorkshopBodyPose[]>, open: string, label: string) {
        for (const id of BLADES) {
            const heights = blades.get(id)!.map(b => b.py);
            if (id === open) {
                const highest = Math.max(...heights);
                assert.ok(highest > OPEN_MIN_PY, `${label}: the loaded plate's gate (blade ${id}) opens, max py=${highest}`);
            } else {
                const highest = Math.max(...heights);
                assert.ok(highest < CLOSED_MAX_PY, `${label}: the other gate (blade ${id}) stays closed throughout, max py=${highest}`);
            }
        }
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    test('1. A ball on P1 opens only G1; Reset matches the snapshot (both plates Empty, both gates closed)', { timeout: 240000 }, async () => {
        const s0 = await buildTwoPlates(PLATE_1);
        const circuits = savedCircuits(await saved(driver));
        assert.deepEqual(circuits.links, ['3→1', '4→2', '5→3', '6→4'].sort(), 'B1 → P1 → G1 and B2 → P2 → G2');
        assert.deepEqual(circuits.minimums, [0.5, 0.5]);
        for (const cycle of ['first run', 'second run']) {
            assertOnlyOpens(await runAndSample(s0, 3500), BLADES[0], `P1 loaded, ${cycle}`);
            await resetMatches(s0);
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Mirror: a ball on P2 opens only G2; Reset matches', { timeout: 240000 }, async () => {
        const s0 = await buildTwoPlates(PLATE_2);
        assertOnlyOpens(await runAndSample(s0, 3500), BLADES[1], 'P2 loaded');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    // Owner decision, 20 Oct 2026: no activation-slot limit. Seven more plates and five each of counters, clocks and latches (24
    // activation parts in all; Build used to refuse the ninth) are placed through the palette on the left of the bench.
    test('4. Twenty-four activation parts: Build admits them, only G1 opens, Reset and Save/Load are exact', { timeout: 600000 }, async () => {
        await buildTwoPlates(PLATE_1);
        const extras = [...Array(7).fill('pressure_plate'), ...Array(5).fill('counter'), ...Array(5).fill('clock'), ...Array(5).fill('latch')] as
            ('pressure_plate' | 'counter' | 'clock' | 'latch')[];
        for (const [i, tool] of extras.entries()) {
            const at = screen(-6 + (i % 6), -3 + Math.floor(i / 6) * 1.5);
            await driver.selectTool(tool);
            await driver.placeOnCanvas(at.x, at.y);
        }
        const s0 = await driver.captureSnapshot(BENCH);
        const bytes = await saved(driver);
        const construction = decodeSavedConstruction(bytes).construction()!;
        const kinds = Array.from({ length: construction.instancesLength() }, (_, i) => construction.instances(i)!.partId());
        assert.equal(kinds.length, 29, `all 29 parts are on the bench, got ${kinds.length}`);
        assert.equal(kinds.filter(k => k === 'pressure_plate').length, 9, '9 plates');
        for (const kind of ['counter', 'clock', 'latch']) assert.equal(kinds.filter(k => k === kind).length, 5, `5 ${kind}s`);
        assertOnlyOpens(await runAndSample(s0, 3500), BLADES[0], '24 activation parts, P1 loaded');
        await resetMatches(s0);
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        assert.deepEqual(authoredState(await saved(driver)), authoredState(bytes), 'Authored state must roundtrip bit-identically');
        assertOnlyOpens(await runAndSample(loaded, 3500), BLADES[0], 'loaded 24-part bench, P1 loaded');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Save/Load preserves both circuits and minimum_mass; the loaded bench opens only G1', { timeout: 240000 }, async () => {
        const s0 = await buildTwoPlates(PLATE_1);
        const initialSaved = await saved(driver);
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        const loadedSaved = await saved(driver);
        assert.deepEqual(authoredState(loadedSaved), authoredState(initialSaved), 'Authored state must roundtrip bit-identically');
        assert.deepEqual(savedCircuits(loadedSaved), savedCircuits(initialSaved));
        assertOnlyOpens(await runAndSample(loaded, 3500), BLADES[0], 'loaded bench, P1 loaded');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
