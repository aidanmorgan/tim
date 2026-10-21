// Stories 9A.14 and 9A.18: the generated element bundle is the game's only catalogue, and each element's palette icon ships in its
// package. Through actual UI controls only (palette rows, canvas clicks, Run, Reset, Save and Load):
// - the Free palette shows exactly the 22 bundle elements, in the bundle's declared palette order, with the Tennis ball and Balloon
//   absent: row i (top unscrolled, or scrolled to the end) places exactly the bundle's i-th element;
// - the palette's titles and package icons are pixel-identical to the pre-cutover palette (baselines captured from the published
//   build of 8b646ffe, unscrolled and scrolled to the end; tools/e2e/baselines);
// - one bench holding every element runs, resets to the exact Story 9R.1 snapshot, and saves, reloads and loads bit for bit.
// Layout: the 22 elements on a spaced bench grid; the six tube parts (pipe, ball detector, the two bends, funnel and powered gate)
// sit at least two grid steps apart so no mouth snaps.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

type Point = { x: number; y: number };

// The generated bundle (tools/BundleElements writes it on every build; the runner publishes from the same tree).
interface BundleEntry { Metadata: { Id: string; PaletteOrder: number }; Footprint: { MovingBodies: number } }
const BUNDLE: BundleEntry[] = (JSON.parse(readFileSync(new URL('../../elements/elements.bundle.json', import.meta.url), 'utf8')) as
    { Elements: BundleEntry[] }).Elements;
// The typed undelivered set (engine/gpu/UndeliveredParts.cs): never offered until Story 12.1 ships their packages.
const UNDELIVERED = ['tennis', 'balloon'];
const ROWS = BUNDLE.filter(entry => !UNDELIVERED.includes(entry.Metadata.Id))
    .sort((a, b) => a.Metadata.PaletteOrder - b.Metadata.PaletteOrder).map(entry => entry.Metadata.Id);
const MOVING_BODIES = BUNDLE.filter(entry => ROWS.includes(entry.Metadata.Id))
    .reduce((sum, entry) => sum + entry.Footprint.MovingBodies, 0);

// Palette geometry in the 1440x900 viewport (palette-placement): 51 px rows, the first row centred at y 155 unscrolled and the last
// at y 841 scrolled to the end. Rows 0-12 are pressed unscrolled, rows 13-21 scrolled.
const ROW_X = 130;
const ROW_PITCH = 51;
const FIRST_ROW_Y = 155;
const LAST_ROW_Y_SCROLLED = 841;
const LAST_UNSCROLLED_ROW = 12;
const PALETTE_CLIP = { x: 24, y: 94, width: 220, height: 782 };
const BASELINE_TOP = readFileSync(new URL('./baselines/arch-9a-palette-top.png', import.meta.url));
const BASELINE_END = readFileSync(new URL('./baselines/arch-9a-palette-end.png', import.meta.url));
const BENCH_CENTRE: Point = { x: 720, y: 485 };

// Bench slots by element id, on the placement plane's admitted area (a click outside it places nothing), 110 px apart: tubes at
// least two slots apart, every other element on the grid between them.
const TUBE_SLOTS: Record<string, Point> = {
    pipe: { x: 360, y: 420 }, ball_detector: { x: 690, y: 420 }, pipe_bend_45: { x: 910, y: 420 },
    funnel: { x: 580, y: 560 }, powered_gate: { x: 1020, y: 560 }, pipe_bend_90: { x: 800, y: 630 },
};
const OTHER_SLOTS: Point[] = [
    { x: 470, y: 420 }, { x: 580, y: 420 }, { x: 800, y: 420 },
    { x: 360, y: 490 }, { x: 470, y: 490 }, { x: 580, y: 490 }, { x: 690, y: 490 }, { x: 800, y: 490 }, { x: 910, y: 490 },
    { x: 1020, y: 490 }, { x: 1130, y: 490 },
    { x: 690, y: 560 }, { x: 800, y: 560 }, { x: 910, y: 560 }, { x: 1130, y: 560 },
    { x: 910, y: 630 },
];
const RUN_WINDOW_MS = 1500;

describe('Stories 9A.14 and 9A.18: the bundle is the sole catalogue and icons ship in their packages', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];
    const watch = (source: string) => (message: ConsoleMessage): void => {
        if (isWorkshopConsoleFailure(message.type(), message.text())) errors.push(`${source}: ${message.text().slice(0, 400)}`);
    };

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', watch('page'));
        driver.page.on('pageerror', (error: Error) => errors.push(`page error: ${String(error).slice(0, 400)}`));
        driver.page.on('worker', (worker: Worker) => worker.on('console', watch(`worker ${worker.url()}`)));
    });

    after(async () => {
        await driver?.closeAfterSuite();
    });

    async function settle(): Promise<void> {
        await driver.page.evaluate(async () => {
            for (let i = 0; i < 12; i++) await new Promise(resolve => requestAnimationFrame(resolve));
        });
    }

    /** Scrolls the palette to its top or its end, then moves the pointer off it so no row is hovered. */
    async function scrollPalette(toEnd: boolean): Promise<void> {
        await driver.page.mouse.move(100, 500);
        for (let i = 0; i < 4; i++) {
            await driver.page.mouse.wheel(0, toEnd ? 2000 : -2000);
            await settle();
        }
        await driver.page.mouse.move(BENCH_CENTRE.x, BENCH_CENTRE.y);
        await settle();
    }

    /** Chooses palette row `row` through the actual palette. */
    async function chooseRow(row: number): Promise<void> {
        await driver.page.keyboard.press('Escape');
        const scrolled = row > LAST_UNSCROLLED_ROW;
        await scrollPalette(scrolled);
        await driver.clickAt(ROW_X, scrolled ? LAST_ROW_Y_SCROLLED - (ROWS.length - 1 - row) * ROW_PITCH : FIRST_ROW_Y + row * ROW_PITCH);
    }

    function savedParts(slot: Uint8Array): string[] {
        const construction = decodeSavedConstruction(slot).construction();
        assert.ok(construction, 'the saved slot holds a construction');
        return Array.from({ length: construction.instancesLength() }, (_, i) => construction.instances(i)!.partId() ?? '');
    }

    function slotOf(id: string, others: Point[]): Point {
        const slot = TUBE_SLOTS[id] ?? others.shift();
        assert.ok(slot, `no bench slot is left for ${id}`);
        return slot;
    }

    test('the bundle declares exactly 22 deliverable elements and neither undelivered part', () => {
        assert.equal(ROWS.length, 22);
        for (const id of UNDELIVERED) assert.ok(!ROWS.includes(id), `${id} is not a palette row`);
        assert.equal(new Set(BUNDLE.map(entry => entry.Metadata.PaletteOrder)).size, BUNDLE.length, 'palette orders are distinct');
    });

    test('the palette titles and package icons are pixel-identical to the pre-cutover palette', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.page.mouse.move(BENCH_CENTRE.x, BENCH_CENTRE.y);
        await settle();
        const top = await driver.page.screenshot({ clip: PALETTE_CLIP });
        await scrollPalette(true);
        const end = await driver.page.screenshot({ clip: PALETTE_CLIP });
        assert.ok(top.equals(BASELINE_TOP), `the unscrolled palette differs from its baseline (${top.length} vs ${BASELINE_TOP.length} bytes)`);
        assert.ok(end.equals(BASELINE_END), `the scrolled palette differs from its baseline (${end.length} vs ${BASELINE_END.length} bytes)`);
        assert.deepEqual(errors, [], `no console or worker error: ${errors.join('; ')}`);
    });

    for (const [row, id] of ROWS.entries()) {
        test(`palette row ${row} places the bundle's element ${id}`, { timeout: 120000 }, async () => {
            errors.length = 0;
            await driver.reload();
            await chooseRow(row);
            await driver.placeOnCanvas(BENCH_CENTRE.x, BENCH_CENTRE.y);
            assert.deepEqual(savedParts((await driver.save()).slot), [id], `row ${row} must place exactly ${id}`);
            assert.deepEqual(errors, [], `placing ${id} logs no console or worker error: ${errors.join('; ')}`);
        });
    }

    test('one bench with every bundle element runs, resets exactly, and saves and loads bit for bit', { timeout: 900000 }, async () => {
        errors.length = 0;
        await driver.reload();
        const others = [...OTHER_SLOTS];
        for (const [row, id] of ROWS.entries()) {
            await chooseRow(row);
            const slot = slotOf(id, others);
            await driver.placeOnCanvas(slot.x, slot.y);
        }
        await driver.page.keyboard.press('Escape');
        const bench = { bodies: MOVING_BODIES } as const;
        const s0: RunResetSnapshot = await driver.captureSnapshot(bench);
        assert.deepEqual(savedParts((await driver.save()).slot), ROWS, 'the bench saves every element once, in placement order');
        for (const cycle of ['first run', 'second run']) {
            await driver.run(RUN_WINDOW_MS);
            await driver.expectRunAdvanced(s0);
            await driver.reset();
            await driver.expectResetMatches(s0);
            assert.deepEqual(errors, [], `${cycle}: no console or worker error: ${errors.join('; ')}`);
        }
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(bench);
        assertLoadRestores(s0, loaded);
        assert.deepEqual(savedParts((await driver.save()).slot), ROWS, 'the loaded bench saves the same elements');
        await driver.run(RUN_WINDOW_MS);
        await driver.expectRunAdvanced(loaded);
        await driver.reset();
        await driver.expectResetMatches(loaded);
        assert.deepEqual(errors, [], `loaded run: no console or worker error: ${errors.join('; ')}`);
    });
});
