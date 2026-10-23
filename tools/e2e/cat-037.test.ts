// Pure TypeScript Playwright-driven E2E acceptance suite for slice CAT-037 (Story 9.4): Set/Reset State Latch bistable memory.
// Every construction is built with the actual Workshop UI (palette, canvas clicks, Move lift, contextual Connect buttons and the
// typed SetIn (bar) / ResetIn (ring) socket choices). Observation is read-only: the pose ring, the Animation-worker telemetry, rendered
// screenshot pixels and the IndexedDB save slot.
// Layout (instance ids follow placement order): 1 Powered Gate (720,485), 2 State Latch (930,520), 3 Battery (1100,520),
// 4 Basketball lifted 150 px above 5 Impact switch (450,485) [, 6 Delay (600,420)], then a Signal lamp (1000,610) wired from the
// latch ActivationOut (a level: lit while memory is On). Set/Reset are chosen through the drawer's icon-only bar/ring choices.
// Supply route: Battery Supply → Latch PowerIn; Latch Supply → Gate PowerIn. The gate blade is the energised consumer.
// Latch presentation: its Memory cosmetic (Animation target 4 = latch node 2 + 2) blends rocker −0.25 → +0.25 rad and lamp Slate → Gold.
// 1. Negative control and typed wiring (AC1, AC2): with SetIn unwired the supplied gate stays closed and the latch Off for 6.5 s;
//    SetIn is then wired through the UI and the save records distinct SetIn / PowerIn / Supply sockets.
// 2. Set and indefinite hold (AC2): closed before the Set impact; opens after it and stays open > 2.5 s; rocker/lamp show On.
// 3. Supply interruption, Pause and Reset (AC4, AC5): Disable supply closes the gate while memory and lamp stay On; Enable supply
//    reopens it without a new Set; Pause freezes; workbench Reset while On restores the closed gate, rocker rest and Slate lamp.
// 4. Reset dominance (AC3): one switch wired to both SetIn and ResetIn; the gate never opens and the latch never shows On.
// 5. Reset pulse (AC3): Switch → Delay (1 s) → ResetIn; On (gate open, Gold) after the Set, then Off (closed, Slate) after the Reset.
// 6. Persistence (AC6): reload, Load the Set/Delay/Reset machine, verify distinct sockets and replay identical behaviour.
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';

const requireMcp = createRequire('/opt/homebrew/lib/node_modules/@playwright/mcp/package.json');
const { PNG } = requireMcp('./node_modules/playwright-core/lib/utilsBundle.js');

const ConnectionDomain = { Activation: 1, Electrical: 2 } as const;
const Socket = { ActivationOut: 1, SetIn: 2, ResetIn: 3, ActivationIn: 4, Supply: 7, PowerIn: 8 } as const;
const BLADE_BODY_ID = '4294967810'; // SliderBody(1, 0): (1 << 32) + 256 + 1 * 256 + 2
const LATCH_TARGET = '4';           // ActivationTarget(latch node 2) = 2 + 2
const CLOSED_MAX_PY = 3.2; // blade rest height is 3.0 m
const OPEN_MIN_PY = 4.2;   // fully open blade reaches 4.55 m
const ROCKER_OFF = -0.25, ROCKER_ON = 0.25; // rad, the Memory binding's neutral and active rocker angles

const GATE = { x: 720, y: 485 };
const LATCH = { x: 930, y: 520 };
const BATTERY = { x: 1100, y: 520 };
const SWITCH = { x: 450, y: 485 };
const DELAY = { x: 600, y: 420 };
const SIGNAL_LAMP = { x: 1000, y: 610 }; // placed last: id 6, or 7 when the Delay is present
const LAMP_PIXEL = { x: 915, y: 548 }; // the latch's own indicator lamp on screen
const SIGNAL_BULB_PIXEL = { x: 1000, y: 596 }; // the Signal lamp's bulb on screen
const SWITCH_TARGET = '7'; // Impact switch id 5 + 2: its own activation animation witnesses that the impact fired
// Contextual rows (ui/WorkshopConnections.cs) are bottom-anchored 45 px apart; text target choices stack upward from 735.
const ROW_LAST = 789;
const ROW_SECOND_LAST = 744;
const ROW_FOURTH_LAST = 654; // latch "Connect ActivationOut" once it has two supply links
const CHOICE_LAST = 735;
// Icon-only State Latch socket choices (DESIGN.md Set/Reset latch): raised bar = Set, hollow ring = Reset.
const CHOICE_SET_BAR = { x: 134, y: 673 };
const CHOICE_RESET_RING = { x: 134, y: 730 };
const BATTERY_RUN_TOGGLE = { x: 138, y: 822 }; // "Disable supply" / "Enable supply" while Running or Paused

type ResetWiring = 'none' | 'delay' | 'same-source';
interface BladeSample { t: number; py: number }
interface Rgb { r: number; g: number; b: number }

type Point = { x: number; y: number };
const TEXT_CHOICE: Point = { x: 133, y: CHOICE_LAST };

async function connect(driver: WorkshopDriver, source: Point, row: number, target: Point, choice: Point, verify?: () => Promise<void>) {
    await driver.selectPartAt(source.x, source.y);
    await driver.clickAt(133, row);
    await driver.clickAt(target.x, target.y);
    if (verify) await verify();
    await driver.clickAt(choice.x, choice.y);
    await driver.page.keyboard.press('Escape');
    await driver.page.waitForTimeout(150);
}

async function pixel(driver: WorkshopDriver, at: Point): Promise<Rgb> {
    const png = PNG.sync.read(await driver.page.screenshot({ clip: { x: at.x, y: at.y, width: 1, height: 1 } }));
    return { r: png.data[0], g: png.data[1], b: png.data[2] };
}
const isNavy = (c: Rgb) => c.r < 90 && c.g < 100 && c.b < 130;            // #293954 icon stroke/fill
const isDrawer = (c: Rgb) => c.r > 220 && c.g > 220 && c.b > 200;         // cream drawer background

// F1: the ambiguous latch target offers icon-only choices — a filled navy bar (Set) and a hollow navy ring (Reset) — and no text.
async function assertIconOnlyChoices(driver: WorkshopDriver) {
    await driver.page.screenshot({ path: '.anvil/cat-037-socket-choice-drawer.png', clip: { x: 0, y: 560, width: 260, height: 340 } });
    assert.ok(isNavy(await pixel(driver, CHOICE_SET_BAR)), 'Set choice shows the filled raised-bar icon');
    assert.ok(isDrawer(await pixel(driver, CHOICE_RESET_RING)), 'Reset choice is a hollow ring (light centre)');
    assert.ok(isNavy(await pixel(driver, { x: CHOICE_RESET_RING.x, y: CHOICE_RESET_RING.y - 9 })), 'Reset choice shows the navy ring rim');
    for (const y of [CHOICE_SET_BAR.y, CHOICE_RESET_RING.y])
        for (const x of [50, 70, 90, 180, 200])
            assert.ok(isDrawer(await pixel(driver, { x, y })), `No text beside the icon at (${x}, ${y})`);
}

async function build(driver: WorkshopDriver, reset: ResetWiring, setWired = true) {
    await driver.reload();
    await driver.selectTool('powered_gate'); await driver.placeOnCanvas(GATE.x, GATE.y);
    await driver.selectTool('latch'); await driver.placeOnCanvas(LATCH.x, LATCH.y);
    await driver.selectTool('battery'); await driver.placeOnCanvas(BATTERY.x, BATTERY.y);
    await driver.selectTool('basketball'); await driver.placeOnCanvas(SWITCH.x, SWITCH.y);
    await driver.selectPartAt(SWITCH.x, SWITCH.y); await driver.setPartMode('move'); await driver.liftSelectedPart(150, SWITCH.x, SWITCH.y);
    await driver.selectTool('switch'); await driver.placeOnCanvas(SWITCH.x, SWITCH.y);
    if (reset === 'delay') { await driver.selectTool('delay'); await driver.placeOnCanvas(DELAY.x, DELAY.y); }
    await driver.selectTool('lamp'); await driver.placeOnCanvas(SIGNAL_LAMP.x, SIGNAL_LAMP.y);

    // Battery rows: [Battery settings, Connect Supply] → Supply to PowerIn on the latch (single text choice).
    await connect(driver, BATTERY, ROW_LAST, LATCH, TEXT_CHOICE);
    // Latch rows: [Connect ActivationOut, Connect Supply, Disconnect supply] → Supply to PowerIn on the gate.
    await connect(driver, LATCH, ROW_SECOND_LAST, GATE, TEXT_CHOICE);
    // Latch rows: [Connect ActivationOut, Connect Supply, Disconnect supply ×2] → ActivationOut to ActivationIn on the lamp.
    await connect(driver, LATCH, ROW_FOURTH_LAST, SIGNAL_LAMP, TEXT_CHOICE);
    if (!setWired) return;
    // Switch rows: [Connect ActivationOut] → the latch offers icon-only bar (Set) and ring (Reset); choose the bar.
    await connect(driver, SWITCH, ROW_LAST, LATCH, CHOICE_SET_BAR, () => assertIconOnlyChoices(driver));
    if (reset === 'delay') {
        // Switch rows: [Connect ActivationOut, Disconnect signal] → Delay ActivationIn (single text choice).
        await connect(driver, SWITCH, ROW_SECOND_LAST, DELAY, TEXT_CHOICE);
        // Delay rows: [Delay: 1 s, Connect ActivationOut, Disconnect signal] → choose the ring (Reset) on the latch.
        await connect(driver, DELAY, ROW_SECOND_LAST, LATCH, CHOICE_RESET_RING);
    } else if (reset === 'same-source') {
        // The same switch also drives the ring (Reset): one impact requests Set and Reset on the same tick.
        await connect(driver, SWITCH, ROW_SECOND_LAST, LATCH, CHOICE_RESET_RING);
    }
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

function assertWiring(bytes: Buffer, reset: ResetWiring) {
    const construction = decodeSavedConstruction(bytes).construction()!;
    const ids = new Map<string, bigint>();
    for (let i = 0; i < construction.instancesLength(); i++) {
        const instance = construction.instances(i)!;
        ids.set(instance.partId()!, instance.id());
    }
    assert.equal(ids.get('powered_gate'), 1n);
    assert.equal(ids.get('latch'), 2n, 'State Latch instance is persisted as part id "latch"');
    const expected: [string, string, number, number, number][] = [
        ['battery', 'latch', ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn],
        ['latch', 'powered_gate', ConnectionDomain.Electrical, Socket.Supply, Socket.PowerIn],
        ['latch', 'lamp', ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn],
        ['switch', 'latch', ConnectionDomain.Activation, Socket.ActivationOut, Socket.SetIn],
    ];
    assert.equal(ids.get('lamp'), reset === 'delay' ? 7n : 6n, 'Signal lamp is the last placed part');
    if (reset === 'delay') expected.push(['switch', 'delay', ConnectionDomain.Activation, Socket.ActivationOut, Socket.ActivationIn],
        ['delay', 'latch', ConnectionDomain.Activation, Socket.ActivationOut, Socket.ResetIn]);
    if (reset === 'same-source') expected.push(['switch', 'latch', ConnectionDomain.Activation, Socket.ActivationOut, Socket.ResetIn]);
    assert.equal(construction.connectionsLength(), expected.length, 'Exactly the authored links are saved');
    for (const [from, to, domain, out, input] of expected) {
        let found = false;
        for (let i = 0; i < construction.connectionsLength(); i++) {
            const edge = construction.connections(i)!;
            if (edge.source() === ids.get(from) && edge.target() === ids.get(to) && edge.domain() === domain &&
                edge.fromSocket() === out && edge.toSocket() === input) found = true;
        }
        assert.ok(found, `Missing ${from} → ${to} link (domain ${domain}, socket ${out} → ${input})`);
    }
}

async function sampleBlade(driver: WorkshopDriver, ms: number, tag: string): Promise<BladeSample[]> {
    const samples: BladeSample[] = [];
    const started = Date.now();
    let previous = '';
    while (Date.now() - started < ms) {
        const slot = await driver.readLatestPose();
        if (slot && slot.sequence !== previous) {
            previous = slot.sequence;
            const blade = slot.bodies.find(body => body.id === BLADE_BODY_ID);
            if (blade) samples.push({ t: Date.now() - started, py: blade.py });
        }
        await driver.page.waitForTimeout(40);
    }
    console.log(`[CAT-037 ${tag}] ${JSON.stringify(samples.map(s => [s.t, Number(s.py.toFixed(3))]))}`);
    return samples;
}

const max = (samples: BladeSample[]) => Math.max(...samples.map(s => s.py));
const min = (samples: BladeSample[]) => Math.min(...samples.map(s => s.py));
const after_ = (samples: BladeSample[], t: number) => samples.filter(s => s.t >= t);

// The rendered lamp colour (5×5 average) and a retained crop of the latch for evidence.
async function lampColour(driver: WorkshopDriver, tag: string): Promise<Rgb> {
    await driver.page.screenshot({ path: `.anvil/cat-037-latch-${tag}.png`, clip: { x: LATCH.x - 100, y: LATCH.y - 90, width: 200, height: 170 } });
    const png = PNG.sync.read(await driver.page.screenshot({ clip: { x: LAMP_PIXEL.x - 2, y: LAMP_PIXEL.y - 2, width: 5, height: 5 } }));
    let r = 0, g = 0, b = 0;
    for (let i = 0; i < png.data.length; i += 4) { r += png.data[i]; g += png.data[i + 1]; b += png.data[i + 2]; }
    const n = png.data.length / 4;
    const colour = { r: Math.round(r / n), g: Math.round(g / n), b: Math.round(b / n) };
    console.log(`[CAT-037 lamp ${tag}] ${JSON.stringify(colour)}`);
    return colour;
}
const isGold = (c: Rgb) => c.r - c.b > 80 && c.g - c.b > 60;  // #f7cb52 lit: measured (255, 255, 116)
const isSlate = (c: Rgb) => Math.abs(c.r - c.b) < 40 && c.b > 120; // #556573 lit: measured (144, 161, 163)

async function latchBlend(driver: WorkshopDriver): Promise<number | null> {
    const sample = await driver.readLastAnimationSample(LATCH_TARGET);
    return sample ? sample.value : null;
}
const rockerAngle = (blend: number) => ROCKER_OFF + (ROCKER_ON - ROCKER_OFF) * blend;
async function latchSampleCount(driver: WorkshopDriver): Promise<number> {
    return (await driver.readAnimationSamplesForTarget(LATCH_TARGET)).length;
}

// B: the latch ActivationOut is a level; the Signal lamp it drives (Memory cosmetic, target = lamp id + 2) lights and darkens with it.
const signalLampTarget = (reset: ResetWiring) => reset === 'delay' ? '9' : '8';
async function signalLampBlend(driver: WorkshopDriver, reset: ResetWiring): Promise<number | null> {
    const sample = await driver.readLastAnimationSample(signalLampTarget(reset));
    return sample ? sample.value : null;
}
async function signalLampSampleCount(driver: WorkshopDriver, reset: ResetWiring): Promise<number> {
    return (await driver.readAnimationSamplesForTarget(signalLampTarget(reset))).length;
}
async function bulbColour(driver: WorkshopDriver, tag: string): Promise<Rgb> {
    await driver.page.screenshot({ path: `.anvil/cat-037-signal-lamp-${tag}.png`, clip: { x: SIGNAL_LAMP.x - 60, y: SIGNAL_LAMP.y - 70, width: 120, height: 110 } });
    const png = PNG.sync.read(await driver.page.screenshot({ clip: { x: SIGNAL_BULB_PIXEL.x - 2, y: SIGNAL_BULB_PIXEL.y - 2, width: 5, height: 5 } }));
    let r = 0, g = 0, b = 0;
    for (let i = 0; i < png.data.length; i += 4) { r += png.data[i]; g += png.data[i + 1]; b += png.data[i + 2]; }
    const n = png.data.length / 4;
    const colour = { r: Math.round(r / n), g: Math.round(g / n), b: Math.round(b / n) };
    console.log(`[CAT-037 bulb ${tag}] ${JSON.stringify(colour)}`);
    return colour;
}
const isBulbLit = (c: Rgb) => c.r > 230 && c.g > 210;   // #fff0a5 with #e9b24c emission
const isBulbDark = (c: Rgb) => c.r < 200 && c.g < 200;  // slate #556573 bulb

async function toggleBattery(driver: WorkshopDriver, expectedLabel: 'Disable' | 'Enable') {
    await driver.selectPartAt(BATTERY.x, BATTERY.y);
    await driver.page.screenshot({ path: `.anvil/cat-037-battery-${expectedLabel.toLowerCase()}.png` });
    await driver.clickAt(BATTERY_RUN_TOGGLE.x, BATTERY_RUN_TOGGLE.y);
    await driver.page.keyboard.press('Escape');
}

describe('CAT-037: State Latch bistable memory E2E', () => {
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

    test('1. Negative control (SetIn unwired: supplied gate closed 6.5 s, latch Off), then typed SetIn wiring through the UI', { timeout: 150000 }, async () => {
        errors.length = 0;
        await build(driver, 'none', false);
        const before = await latchSampleCount(driver);
        const lampBefore = await signalLampSampleCount(driver, 'none');
        await driver.run();
        try {
            const samples = await sampleBlade(driver, 6500, 'unset');
            assert.ok(samples.length >= 40, `Expected >= 40 distinct publications, got ${samples.length}`);
            assert.ok(max(samples) < CLOSED_MAX_PY, `Battery → Latch → Gate stays closed while the latch was never Set, max py=${max(samples)}`);
            assert.equal(await latchSampleCount(driver), before, 'An Off latch drives no Memory animation');
            assert.ok(isSlate(await lampColour(driver, 'unset')), 'Never-Set latch lamp stays Slate');
            assert.equal(await signalLampSampleCount(driver, 'none'), lampBefore, 'Latch ActivationOut stays low: the Signal lamp never animates');
            assert.ok(isBulbDark(await bulbColour(driver, 'unset')), 'Signal lamp stays dark while the latch was never Set');
        } finally {
            await driver.reset();
        }
        // Wire the Set through the contextual UI: Switch [Connect ActivationOut] → choose SetIn (bar) on the latch.
        await connect(driver, SWITCH, ROW_LAST, LATCH, CHOICE_SET_BAR, () => assertIconOnlyChoices(driver));
        await driver.save();
        assertWiring(await saved(driver), 'none');
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Set impact latches On, energises the gate and holds it open; rocker and lamp show On', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.run();
        try {
            const samples = await sampleBlade(driver, 4500, 'set-hold');
            assert.ok(samples.length >= 20, `Expected >= 20 distinct publications, got ${samples.length}`);
            const early = samples.filter(s => s.t < 400);
            assert.ok(early.length > 0 && max(early) < CLOSED_MAX_PY, `Gate closed before the Set impact, max py=${max(early)}`);
            const late = after_(samples, 2000);
            assert.ok(late.length >= 10, 'Hold window observed');
            assert.ok(min(late) > OPEN_MIN_PY, `Latched contact holds the gate open for > 2.5 s, min py=${min(late)}`);
            const blend = await latchBlend(driver);
            assert.ok(blend !== null && blend > 0.99, `Memory blend reaches On, got ${blend}`);
            assert.ok(Math.abs(rockerAngle(blend!) - ROCKER_ON) < 0.01, `Rocker at +0.25 rad, got ${rockerAngle(blend!)}`);
            assert.ok(isGold(await lampColour(driver, 'on')), 'Latch lamp is Gold while On');
            const signal = await signalLampBlend(driver, 'none');
            assert.ok(signal !== null && signal > 0.99, `Latch ActivationOut high: the Signal lamp lights, blend ${signal}`);
            assert.ok(isBulbLit(await bulbColour(driver, 'on')), 'Signal lamp bulb is lit while the latch is On');
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. Supply interruption keeps memory, re-energises without a new Set; Pause freezes; workbench Reset while On restores rest', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.run();
        try {
            const opened = await sampleBlade(driver, 2500, 'interrupt-open');
            assert.ok(after_(opened, 2000).every(s => s.py > OPEN_MIN_PY), 'Gate open after the Set');

            await toggleBattery(driver, 'Disable');
            const interrupted = await sampleBlade(driver, 3000, 'interrupt-off');
            assert.ok(max(after_(interrupted, 2200)) < CLOSED_MAX_PY, `Disabled supply de-energises the gate, max py=${max(after_(interrupted, 2200))}`);
            assert.ok(isGold(await lampColour(driver, 'interrupted')), 'Memory stays On (Gold) while its supply is interrupted');

            // The impact switch is single-shot per Run and its ball is at rest on it: no new Set can occur.
            await toggleBattery(driver, 'Enable');
            const restored = await sampleBlade(driver, 3000, 'interrupt-restored');
            assert.ok(min(after_(restored, 2200)) > OPEN_MIN_PY, `Remembered On state re-energises the gate, min py=${min(after_(restored, 2200))}`);

            await driver.pauseSimulation();
            const paused = await driver.readLatestPose();
            const pausedBlade = paused?.bodies.find(body => body.id === BLADE_BODY_ID);
            assert.ok(pausedBlade && pausedBlade.py > OPEN_MIN_PY, 'Paused with the gate open');
            await driver.page.waitForTimeout(800);
            const stillPaused = (await driver.readLatestPose())?.bodies.find(body => body.id === BLADE_BODY_ID);
            assert.ok(stillPaused && Math.abs(stillPaused.py - pausedBlade.py) < 1e-6, 'No motion while paused');
            await driver.resumeSimulation();
            const resumed = await sampleBlade(driver, 1000, 'resumed');
            assert.ok(min(resumed) > OPEN_MIN_PY, 'Latch memory preserved across Pause/Resume');
            assert.ok(isGold(await lampColour(driver, 'before-workbench-reset')), 'Latch On immediately before the workbench Reset');
        } finally {
            await driver.reset();
        }
        await driver.page.waitForTimeout(500);
        assert.ok(isSlate(await lampColour(driver, 'after-workbench-reset')), 'Workbench Reset while On returns the lamp to Slate');
        assert.ok(isBulbDark(await bulbColour(driver, 'after-workbench-reset')), 'Workbench Reset restores the Signal lamp dark');
        // Build mode publishes no poses, so the restored world is observed on the next Run: closed gate and an Off latch.
        const before = await latchSampleCount(driver);
        await driver.run();
        try {
            const replay = await sampleBlade(driver, 400, 'after-reset');
            assert.ok(replay.length > 0 && max(replay) < CLOSED_MAX_PY, `Reset restored the closed gate, max py=${max(replay)}`);
            assert.equal(await latchSampleCount(driver), before, 'Reset world starts Off: no Memory animation before the Set');
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Simultaneous Set and Reset resolve Off (Reset dominance): the gate never opens and the latch never shows On', { timeout: 150000 }, async () => {
        errors.length = 0;
        await build(driver, 'same-source');
        await driver.save();
        assertWiring(await saved(driver), 'same-source');
        const before = await latchSampleCount(driver);
        const switchBefore = (await driver.readAnimationSamplesForTarget(SWITCH_TARGET)).length;
        const lampBefore = await signalLampSampleCount(driver, 'same-source');
        await driver.run();
        try {
            const samples = await sampleBlade(driver, 4000, 'dominance');
            assert.ok(samples.length >= 20, `Expected >= 20 distinct publications, got ${samples.length}`);
            // Witness: the impact switch really fired (its own activation animation ran), so "never opens" is not a missed impact.
            assert.ok((await driver.readAnimationSamplesForTarget(SWITCH_TARGET)).length > switchBefore, 'The impact switch fired');
            assert.ok(max(samples) < CLOSED_MAX_PY, `Reset dominates the coincident Set, max py=${max(samples)}`);
            assert.equal(await latchSampleCount(driver), before, 'No On transition was ever committed');
            assert.ok(isSlate(await lampColour(driver, 'dominance')), 'Lamp stays Slate');
            assert.equal(await signalLampSampleCount(driver, 'same-source'), lampBefore, 'The Signal lamp never lights');
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('5. Reset pulse after the Set: On (open, Gold, +0.25 rad), then Off (closed, Slate, −0.25 rad) and stays Off', { timeout: 150000 }, async () => {
        errors.length = 0;
        await build(driver, 'delay');
        await driver.save();
        assertWiring(await saved(driver), 'delay');
        await driver.run();
        try {
            const first = await sampleBlade(driver, 1300, 'set-reset-on');
            const onBlend = await latchBlend(driver);
            assert.ok(onBlend !== null && onBlend > 0.99, `Latch On between Set and Reset, blend ${onBlend}`);
            assert.ok(isGold(await lampColour(driver, 'pulse-on')), 'Lamp Gold between Set and Reset');
            const signalOn = await signalLampBlend(driver, 'delay');
            assert.ok(signalOn !== null && signalOn > 0.99, `Signal lamp lit between Set and Reset, blend ${signalOn}`);
            assert.ok(isBulbLit(await bulbColour(driver, 'pulse-on')), 'Signal lamp bulb lit between Set and Reset');
            const rest = await sampleBlade(driver, 3700, 'set-reset-off');
            const samples = [...first, ...rest.map(s => ({ t: s.t + 1300, py: s.py }))];
            assert.ok(max(samples.filter(s => s.t < 2200)) > 3.8, `Gate energised between Set and Reset, max py=${max(samples)}`);
            assert.ok(max(after_(samples, 4000)) < CLOSED_MAX_PY, `Gate closed after the Reset, max py=${max(after_(samples, 4000))}`);
            const offBlend = await latchBlend(driver);
            assert.ok(offBlend !== null && offBlend < 0.01, `Latch Off after the Reset pulse, blend ${offBlend}`);
            assert.ok(Math.abs(rockerAngle(offBlend!) - ROCKER_OFF) < 0.01, `Rocker back at −0.25 rad, got ${rockerAngle(offBlend!)}`);
            assert.ok(isSlate(await lampColour(driver, 'pulse-off')), 'Lamp Slate after the Reset pulse');
            const signalOff = await signalLampBlend(driver, 'delay');
            assert.ok(signalOff !== null && signalOff < 0.01, `Signal lamp dark after the Reset pulse, blend ${signalOff}`);
            assert.ok(isBulbDark(await bulbColour(driver, 'pulse-off')), 'Signal lamp bulb dark after the Reset pulse');
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('6. Save / reload / Load preserves the latch and its distinct sockets and replays Set then Reset', { timeout: 150000 }, async () => {
        errors.length = 0;
        await driver.reload();
        await driver.load();
        // F3: decode the loaded workbench itself (re-saved from it), not the slot written before the reload; an empty load would fail.
        await driver.save();
        assertWiring(await saved(driver), 'delay');
        await driver.run();
        try {
            const samples = await sampleBlade(driver, 5000, 'replay');
            assert.ok(max(samples.filter(s => s.t < 2200)) > 3.8, `Replay energises the gate after the Set, max py=${max(samples)}`);
            assert.ok(max(after_(samples, 4000)) < CLOSED_MAX_PY, 'Replay closes the gate after the Reset');
            const blends = (await driver.readAnimationSamplesForTarget(LATCH_TARGET)).map(s => s.value);
            assert.ok(blends.some(v => v > 0.99) && blends[blends.length - 1] < 0.01, 'Replay drives the latch On then Off');
            const lamp = (await driver.readAnimationSamplesForTarget(signalLampTarget('delay'))).map(s => s.value);
            assert.ok(lamp.some(v => v > 0.99) && lamp[lamp.length - 1] < 0.01, 'Replay lights the Signal lamp, then darkens it');
        } finally {
            await driver.reset();
        }
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
