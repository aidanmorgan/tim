// Pure TypeScript Playwright-driven E2E acceptance suite for Story 9R.5: a Pressure Plate's level ActivationOut lights a Signal lamp,
// held 0.1 s (12 ticks at 120 Hz) after the last qualifying contact. Every bench is built with the actual Workshop UI (palette, canvas
// clicks, the move-gizmo lift, Fine rotate, the contextual Connect rows and the target socket choices). Observation is read-only: the
// committed read of each Pause / Step acknowledgement (per tick, exact), the pose ring, the Animation-worker telemetry, rendered
// pixels, the CCGPU_UI_REJECTION console channel behind the status line, and the IndexedDB save slot.
// Layout (instance ids follow placement order): 1 Powered Gate (720,485, tilted -15°), 2 Basketball upstream, 3 Pressure Plate
// (920,510), 4 Battery (1080,510), 5 Signal lamp (1000,610), then the load. Wires, all through the drawer: Plate ActivationOut → Lamp
// ActivationIn, Plate Supply → Gate PowerIn, Battery Supply → Plate PowerIn.
// 1. Rest and both domains: a Basketball resting on the plate lights the lamp (committed Latched, Animation blend 1, a lit bulb) while
//    the same plate's supplied contact opens the gate and the upstream ball rolls through. Reset matches the snapshot (lamp Slate).
// 2. Leave: a bowling ball lands on the plate tilted 30° and rolls off. Stepped tick by tick, the plate level and the lamp are one value
//    on every tick: lit through n = 11 after the last qualifying tick, dark from n = 12. Reset matches.
// 3. Underweight control: a 0.4 kg Domino leaves the plate Underweight and the lamp dark. Reset matches.
// 4. Bounce: a Basketball dropped about 2 m rebounds off the face (a non-qualifying tick after first contact) yet the lamp stays lit on
//    every tick from first contact. Reset matches.
// 5. Refusals: the plate's ActivationOut to a Counter, a Delay, and the latch's SetIn and ResetIn is refused with "A level output can only
//    light a Signal lamp."; the latch's own ActivationOut to the Counter gives the identical refusal; no link is stored. Control: the
//    plate's ActivationOut to the lamp is admitted.
// 6. Save / reload / Load preserves the activation_out wire and both supply links bit-identically; the loaded bench replays (lamp lit).
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot, type WorkshopBodyPose } from './workshop-driver.ts';
import { decodeSavedConstruction, authoredState } from './workshop-wire.ts';
import { MemoryKind, activationMemoryRows, assertLoadRestores, snapshotValue } from './run-reset-snapshot.ts';

const requireMcp = createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const { PNG } = requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');

const ConnectionDomain = { Activation: 1, Electrical: 2 } as const;
const Socket = { ActivationOut: 1, SetIn: 2, ResetIn: 3, ActivationIn: 4, Supply: 7, PowerIn: 8 } as const;
const ActivationPhase = { Clear: '0', Latched: '1' } as const;
const HOLD_TICKS = 12n; // ticks(0.1 s, 120 Hz)
const LEVEL_REFUSAL = 'A level output can only light a Signal lamp.';

type Point = { x: number; y: number };
const screen = (x: number, z: number): Point => ({
    x: Math.round(720 + 53.8 * x - 36.8 * z),
    y: Math.round(485 + 17.0 * x + 24.8 * z)
});
const GATE: Point = { x: 720, y: 485 };
const PLATE: Point = { x: 920, y: 510 };
const BATTERY: Point = { x: 1080, y: 510 };
const LAMP: Point = { x: 1000, y: 610 };
// The refusal bench's other targets, clear of the plate and the lamp.
const COUNTER: Point = { x: 450, y: 485 };
const DELAY: Point = { x: 600, y: 420 };
const LATCH: Point = { x: 1100, y: 520 };
// Contextual rows (ui/WorkshopConnections.cs) are bottom-anchored 45 px apart; text target choices stack upward from 735.
const ROW_LAST = 789;
const ROW_SECOND_LAST = 744;
const TEXT_CHOICE: Point = { x: 133, y: 735 };
// Icon-only State Latch socket choices (DESIGN.md Set/Reset latch): raised bar = Set, hollow ring = Reset.
const CHOICE_SET_BAR: Point = { x: 134, y: 673 };
const CHOICE_RESET_RING: Point = { x: 134, y: 730 };
const SIGNAL_BULB_PIXEL: Point = { x: 1000, y: 596 }; // the Signal lamp's bulb on screen (the cat-037 lamp position)
const BLADE_BODY_ID = '4294967810'; // SliderBody(1, 0): (1 << 32) + 256 + 1 * 256 + 2
const UPSTREAM_BALL = '2';
const PLATE_NODE = '3';
const LAMP_NODE = '5';
const LAMP_TARGET = '7'; // the lamp's Memory cosmetic: ActivationTarget(lamp node 5) = 5 + 2
// F.6 F-1 (C-Plate): a part placed from the drawer onto the flat plate rests on its face, so the resting Basketball and the Domino
// need no lift. Move-gizmo lifts (about 56 px/m): the bounce ball is lifted about 2 m above its rested pose
// (evidence-9r-5/probe-bounce-native.txt: a 7-tick rebound gap); on the plate tilted 30° placement does not rest the bowling ball, so
// it is lifted from the 3 m placement plane.
const BOUNCE_LIFT_PX = 112;
const BOWLING_LIFT_PX = 12;

type PlateLoad = 'basketball' | 'bounce' | 'domino' | 'rolloff';
interface TickRead { tick: bigint; plate: boolean; lamp: boolean; qualifying: boolean }

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

/** The saved links as `domain:source.fromSocket→target.toSocket` (instance ids), sorted. */
function savedLinks(bytes: Buffer): string[] {
    const construction = decodeSavedConstruction(bytes).construction()!;
    const links: string[] = [];
    for (let i = 0; i < construction.connectionsLength(); i++) {
        const edge = construction.connections(i)!;
        links.push(`${edge.domain()}:${edge.source()}.${edge.fromSocket()}→${edge.target()}.${edge.toSocket()}`);
    }
    return links.sort();
}
const ACTIVATION_OUT_WIRE = `${ConnectionDomain.Activation}:3.${Socket.ActivationOut}→5.${Socket.ActivationIn}`;
const BENCH_LINKS = [ACTIVATION_OUT_WIRE, `${ConnectionDomain.Electrical}:3.${Socket.Supply}→1.${Socket.PowerIn}`,
    `${ConnectionDomain.Electrical}:4.${Socket.Supply}→3.${Socket.PowerIn}`].sort();

/** The committed activation phase of one node in a snapshot's read. */
function phase(snapshot: RunResetSnapshot, node: string): string {
    const length = Number(snapshotValue(snapshot, 'read.activations.length'));
    for (let i = 0; i < length; i++)
        if (snapshotValue(snapshot, `read.activations[${i}].node`) === node) return snapshotValue(snapshot, `read.activations[${i}].phase`);
    throw new Error(`no committed activation for node ${node}`);
}
/** The plate's committed release hold: the boundary its held level is due to clear (0 when released). */
function holdDue(snapshot: RunResetSnapshot): bigint {
    const rows = activationMemoryRows(snapshot).filter(row => row.node === PLATE_NODE);
    assert.equal(rows.length, 1, 'the plate commits one memory row');
    assert.equal(rows[0].kind, MemoryKind.ContactLoadSource, 'the plate row is its contact-load hold');
    return BigInt(rows[0].due_tick);
}
function tickRead(snapshot: RunResetSnapshot): TickRead {
    const plate = phase(snapshot, PLATE_NODE) === ActivationPhase.Latched;
    return { tick: snapshot.tick, plate, lamp: phase(snapshot, LAMP_NODE) === ActivationPhase.Latched,
        // A qualifying (Loaded) tick commits the level high with its release due exactly 12 ticks later.
        qualifying: plate && holdDue(snapshot) === snapshot.tick + HOLD_TICKS };
}

interface Rgb { r: number; g: number; b: number }
async function bulbColour(driver: WorkshopDriver, tag: string): Promise<Rgb> {
    await driver.page.screenshot({ path: `.anvil/cat-052-lamp-${tag}.png`, clip: { x: LAMP.x - 60, y: LAMP.y - 70, width: 120, height: 110 } });
    const png = PNG.sync.read(await driver.page.screenshot({ clip: { x: SIGNAL_BULB_PIXEL.x - 2, y: SIGNAL_BULB_PIXEL.y - 2, width: 5, height: 5 } }));
    let r = 0, g = 0, b = 0;
    for (let i = 0; i < png.data.length; i += 4) { r += png.data[i]; g += png.data[i + 1]; b += png.data[i + 2]; }
    const n = png.data.length / 4;
    const colour = { r: Math.round(r / n), g: Math.round(g / n), b: Math.round(b / n) };
    console.log(`[CAT-052-LAMP bulb ${tag}] ${JSON.stringify(colour)}`);
    return colour;
}
const isBulbLit = (c: Rgb) => c.r > 230 && c.g > 210;   // #fff0a5 with #e9b24c emission
const isBulbDark = (c: Rgb) => c.r < 200 && c.g < 200;  // slate #556573 bulb

describe('CAT-052: a Pressure Plate ActivationOut lights a Signal lamp, held 0.1 s (Story 9R.5)', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    const rejections: string[] = [];
    // The committed (dynamic) bodies: the gate blade, the upstream basketball and the load.
    const BENCH = { bodies: 3 };

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            console.log(`[E2E-CONSOLE ${msg.type()}] ${msg.text()}`);
            if (msg.text().includes('CCGPU_UI_REJECTION')) rejections.push(msg.text());
            if (isWorkshopConsoleFailure(msg.type(), msg.text())) errors.push(msg.text());
        });
        driver.page.on('pageerror', (err: Error) => errors.push(err.message));
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    async function connect(source: Point, row: number, target: Point, choice: Point, beforeEscape?: () => Promise<void>) {
        await driver.selectPartAt(source.x, source.y);
        await driver.clickAt(133, row);
        await driver.clickAt(target.x, target.y);
        await driver.clickAt(choice.x, choice.y);
        if (beforeEscape) await beforeEscape();
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(150);
    }

    /** Places a part from the drawer on the plate and, when `lift` is non-zero, lifts it with the move gizmo. */
    async function placeNewPart(tool: 'basketball' | 'bowling' | 'domino', lift: number) {
        await driver.selectTool(tool);
        await driver.placeOnCanvas(PLATE.x, PLATE.y);
        if (lift !== 0) {
            await driver.setPartMode('move');
            await driver.liftSelectedPart(lift, PLATE.x, PLATE.y);
        }
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(200);
    }

    async function buildLampBench(load: PlateLoad): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('powered_gate');
        await driver.placeOnCanvas(GATE.x, GATE.y);
        await driver.selectPartAt(GATE.x, GATE.y);
        await driver.tiltSelectedByFineRotate(-3);
        const ball = screen(-0.08, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ball.x, ball.y);
        await driver.selectTool('pressure_plate');
        await driver.placeOnCanvas(PLATE.x, PLATE.y);
        await driver.selectTool('battery');
        await driver.placeOnCanvas(BATTERY.x, BATTERY.y);
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(LAMP.x, LAMP.y);
        // Plate rows: [Connect ActivationOut, Connect Supply] → ActivationOut to ActivationIn on the lamp (single text choice).
        await connect(PLATE, ROW_SECOND_LAST, LAMP, TEXT_CHOICE);
        // Plate rows: [Connect ActivationOut, Connect Supply, Disconnect signal] → Supply to PowerIn on the gate.
        await connect(PLATE, ROW_SECOND_LAST, GATE, TEXT_CHOICE);
        // Battery rows: [Battery settings, Connect Supply] → Supply to PowerIn on the plate.
        await connect(BATTERY, ROW_LAST, PLATE, TEXT_CHOICE);
        if (load === 'rolloff') {
            await driver.selectPartAt(PLATE.x, PLATE.y);
            await driver.tiltSelectedByFineRotate(6);
        }
        if (load === 'basketball') await placeNewPart('basketball', 0);
        else if (load === 'bounce') await placeNewPart('basketball', BOUNCE_LIFT_PX);
        else if (load === 'domino') await placeNewPart('domino', 0);
        else await placeNewPart('bowling', BOWLING_LIFT_PX);
        const s0 = await driver.captureSnapshot(BENCH);
        assert.deepEqual(savedLinks(await saved(driver)), BENCH_LINKS, 'the drawer stored the three wires');
        assert.deepEqual([phase(s0, PLATE_NODE), phase(s0, LAMP_NODE), holdDue(s0)], [ActivationPhase.Clear, ActivationPhase.Clear, 0n],
            'Build: the plate is released and the lamp Slate');
        return s0;
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.reset();
        const s1 = await driver.expectResetMatches(s0);
        assert.equal(phase(s1, LAMP_NODE), ActivationPhase.Clear, 'Reset: the lamp is Slate');
    }

    /** Run, Pause at once, then Step one tick at a time through the Workshop menu, reading each committed tick until `done`. */
    async function stepTrace(s0: RunResetSnapshot, maxSteps: number, done: (trace: TickRead[]) => boolean): Promise<TickRead[]> {
        await driver.run();
        const trace = [tickRead(await driver.expectRunAdvanced(s0))];
        while (!done(trace)) {
            assert.ok(trace.length <= maxSteps, `the trace ended within ${maxSteps} steps: ${JSON.stringify(trace.slice(-5), (_, v) => typeof v === 'bigint' ? Number(v) : v)}`);
            const next = tickRead(await driver.stepSimulation());
            assert.equal(next.tick, trace[trace.length - 1].tick + 1n, 'each Step commits exactly the next tick');
            trace.push(next);
        }
        console.log(`[CAT-052-LAMP trace] ${trace.map(t => `${t.tick}:${t.qualifying ? 'Q' : t.plate ? 'H' : '.'}${t.lamp ? '*' : ' '}`).join(' ')}`);
        return trace;
    }

    test('1. Rest and both domains: a resting Basketball lights the lamp while the same plate opens the gate', { timeout: 150000 }, async () => {
        const s0 = await buildLampBench('basketball');
        assert.ok(isBulbDark(await bulbColour(driver, 'build')), 'Build: the bulb is Slate');
        await driver.run();
        const started = Date.now();
        const samples: { ball: WorkshopBodyPose; blade?: WorkshopBodyPose }[] = [];
        let previous = '';
        while (Date.now() - started < 3500) {
            const slot = await driver.readLatestPose();
            if (slot && slot.sequence !== previous) {
                previous = slot.sequence;
                const ball = slot.bodies.find(b => b.id === UPSTREAM_BALL);
                if (ball) samples.push({ ball, blade: slot.bodies.find(b => b.id === BLADE_BODY_ID) });
            }
            await driver.page.waitForTimeout(25);
        }
        const paused = await driver.expectRunAdvanced(s0);
        const read = tickRead(paused);
        assert.ok(read.plate && read.lamp && read.qualifying, `the resting ball keeps the plate Loaded and the lamp lit: ${JSON.stringify(read, (_, v) => typeof v === 'bigint' ? Number(v) : v)}`);
        const blend = await driver.readLastAnimationSample(LAMP_TARGET);
        assert.ok(blend && blend.value > 0.99, `the lamp's Memory cosmetic reached lit, got ${blend?.value}`);
        assert.ok(isBulbLit(await bulbColour(driver, 'rest')), 'the bulb renders lit');
        // Both domains: the same plate's supplied contact energised the gate and the upstream ball rolled through it.
        const blades = samples.map(s => s.blade).filter((b): b is WorkshopBodyPose => b !== undefined);
        assert.ok(blades.length > 20 && Math.max(...blades.map(b => b.py)) > 4.2, 'the gate blade retracts open');
        assert.ok(samples[samples.length - 1].ball.px < -0.6, `the upstream ball passes the gate, px=${samples[samples.length - 1].ball.px}`);
        await resetMatches(s0);
        await driver.page.waitForTimeout(400); // the cosmetic eases back over 0.16 s
        assert.ok(isBulbDark(await bulbColour(driver, 'reset')), 'Reset: the bulb is Slate again');
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Leave: a ball rolling off a 30° plate darkens the lamp on the 12th tick after its last qualifying tick', { timeout: 600000 }, async () => {
        const s0 = await buildLampBench('rolloff');
        const lastQualifying = (trace: TickRead[]) => [...trace].reverse().find(t => t.qualifying)?.tick;
        const trace = await stepTrace(s0, 1200, t => {
            const last = lastQualifying(t);
            return last !== undefined && t[t.length - 1].tick >= last + HOLD_TICKS + 2n;
        });
        const last = lastQualifying(trace)!;
        let held = 0;
        for (const t of trace) {
            assert.equal(t.lamp, t.plate, `tick ${t.tick}: the lamp follows the plate's one level`);
            const before = [...trace].reverse().find(q => q.qualifying && q.tick <= t.tick)?.tick;
            if (before === undefined) { assert.equal(t.plate, false, `tick ${t.tick}: dark before the first contact`); continue; }
            const n = t.tick - before;
            assert.equal(t.plate, n <= 11n, `tick ${t.tick}: n = ${n} after the last qualifying tick`);
            if (n > 0n && n <= 11n) held++;
        }
        const at = (n: bigint) => trace.find(t => t.tick === last + n)!;
        assert.deepEqual([at(11n).lamp, at(12n).lamp, at(13n).lamp], [true, false, false], 'boundary: lit at n = 11, dark at 12 and 13');
        assert.ok(held >= 11, `the level was held after the ball left (${held} ticks)`);
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Underweight control: a 0.4 kg Domino leaves the plate Underweight and the lamp dark', { timeout: 150000 }, async () => {
        const s0 = await buildLampBench('domino');
        await driver.run(1800);
        const read = tickRead(await driver.expectRunAdvanced(s0));
        assert.deepEqual([read.plate, read.lamp], [false, false], 'the domino never loads the plate');
        assert.equal(holdDue(await driver.stepSimulation()), 0n, 'no release hold is pending');
        const samples = await driver.readAnimationSamplesForTarget(LAMP_TARGET);
        assert.ok(samples.every(s => s.value < 0.01), `the lamp's cosmetic never lights: ${samples.map(s => s.value).join(',')}`);
        assert.ok(isBulbDark(await bulbColour(driver, 'domino')), 'the bulb stays Slate');
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Bounce: a Basketball dropped about 2 m rebounds yet keeps the lamp steadily lit from first contact', { timeout: 600000 }, async () => {
        const s0 = await buildLampBench('bounce');
        const first = (trace: TickRead[]) => trace.find(t => t.qualifying)?.tick;
        const trace = await stepTrace(s0, 1200, t => { const f = first(t); return f !== undefined && t[t.length - 1].tick >= f + 60n; });
        const landed = trace.filter(t => t.tick >= first(trace)!);
        assert.ok(landed.some(t => !t.qualifying), 'control: the ball leaves the face between contacts (it bounces)');
        for (const t of landed) assert.ok(t.lamp && t.plate, `tick ${t.tick}: lit through the bounce`);
        await resetMatches(s0);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Refusals: the plate (and the latch) ActivationOut can only light a Signal lamp', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();
        for (const [tool, at] of [['pressure_plate', PLATE], ['counter', COUNTER], ['delay', DELAY], ['latch', LATCH], ['lamp', LAMP]] as const) {
            await driver.selectTool(tool);
            await driver.placeOnCanvas(at.x, at.y);
        }
        const attempts: [string, Point, Point, Point][] = [
            ['plate → Counter ActivationIn', PLATE, COUNTER, TEXT_CHOICE],
            ['plate → Delay ActivationIn', PLATE, DELAY, TEXT_CHOICE],
            ['plate → latch SetIn', PLATE, LATCH, CHOICE_SET_BAR],
            ['plate → latch ResetIn', PLATE, LATCH, CHOICE_RESET_RING],
            // Regression: the latch's own level output gives the identical refusal.
            ['latch → Counter ActivationIn', LATCH, COUNTER, TEXT_CHOICE],
        ];
        for (const [label, source, target, choice] of attempts) {
            const before = rejections.length;
            // Plate and latch rows (no links yet): [Connect ActivationOut, Connect Supply].
            // The status line (canvas text) shows the refusal until Escape; keep it as a screenshot.
            await connect(source, ROW_SECOND_LAST, target, choice,
                async () => { await driver.page.screenshot({ path: `.anvil/cat-052-lamp-refusal-${attempts.findIndex(a => a[0] === label) + 1}.png` }); });
            assert.equal(rejections.length, before + 1, `${label}: one refusal reaches the status line`);
            assert.ok(rejections[rejections.length - 1].includes(`WorkshopEditRejectedException: ${LEVEL_REFUSAL}`),
                `${label}: refused with the level message, got ${rejections[rejections.length - 1].slice(0, 200)}`);
        }
        await driver.save();
        assert.deepEqual(savedLinks(await saved(driver)), [], 'no refused link was stored');
        // Control: the plate's ActivationOut to the lamp is admitted and stored.
        await connect(PLATE, ROW_SECOND_LAST, LAMP, TEXT_CHOICE);
        await driver.save();
        const construction = decodeSavedConstruction(await saved(driver)).construction()!;
        const ids = new Map<string, bigint>();
        for (let i = 0; i < construction.instancesLength(); i++) ids.set(construction.instances(i)!.partId()!, construction.instances(i)!.id());
        assert.deepEqual(savedLinks(await saved(driver)),
            [`${ConnectionDomain.Activation}:${ids.get('pressure_plate')}.${Socket.ActivationOut}→${ids.get('lamp')}.${Socket.ActivationIn}`]);
        assert.equal(rejections.length, attempts.length, 'the admitted link raised no refusal');
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('6. Save / reload / Load preserves the activation_out wire and replays the lit lamp', { timeout: 150000 }, async () => {
        const s0 = await buildLampBench('basketball');
        const initial = await saved(driver);
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        const reloaded = await saved(driver);
        assert.deepEqual(authoredState(reloaded), authoredState(initial), 'the authored state round-trips bit-identically');
        assert.deepEqual(savedLinks(reloaded), BENCH_LINKS, 'the activation_out wire and both supply links are preserved');
        await driver.run(1500);
        const read = tickRead(await driver.expectRunAdvanced(loaded));
        assert.ok(read.plate && read.lamp, 'the loaded bench lights the lamp');
        await resetMatches(loaded);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
