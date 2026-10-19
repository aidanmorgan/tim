// Story 9R.0c: every Free palette part is placeable on the bench through actual UI controls, with no console or worker error,
// and the undelivered Tennis ball and Balloon are absent from the palette.
//
// The Godot palette is drawn on the canvas, so the suite names its rows by position and proves each row's identity from the
// saved construction (the read-only IndexedDB slot the actual Save control writes). The native PalettePlacementTests derive the
// same list from WorkshopInventoryPolicy.Free; this list mirrors its row order, and the two row-identity probes below fail if a
// row is inserted between Springboard and Pipe, where the Tennis ball and Balloon rows used to sit.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';

type DriverTool = Parameters<WorkshopDriver['selectTool']>[0];

// Free palette rows in order (engine/gpu/WorkshopInventory.cs). `partId` is the saved-construction wire identity. Springboard has no
// driver tool, so its row is pressed directly at its palette row centre, exactly as cat-062 selects it.
const PALETTE = [
    { title: 'Basketball', tool: 'basketball', partId: 'ball' },
    { title: 'Receiver', tool: 'receiver', partId: 'basket' },
    { title: 'Impact switch', tool: 'switch', partId: 'switch' },
    { title: 'Signal lamp', tool: 'lamp', partId: 'lamp' },
    { title: 'Wall', tool: 'wall', partId: 'wall' },
    { title: 'Delay', tool: 'delay', partId: 'delay' },
    { title: 'Pinball bumper', tool: 'bumper', partId: 'bumper' },
    { title: 'Ramp', tool: 'ramp', partId: 'ramp' },
    { title: 'Domino', tool: 'domino', partId: 'domino' },
    { title: 'Bowling ball', tool: 'bowling', partId: 'bowling' },
    { title: 'Battery', tool: 'battery', partId: 'battery' },
    { title: 'Springboard', tool: null, partId: 'spring' },
    { title: 'Pipe', tool: 'pipe', partId: 'pipe' },
    { title: 'Ball detector', tool: 'ball_detector', partId: 'ball_detector' },
    { title: 'Pipe bend 45', tool: 'pipe_bend_45', partId: 'pipe_bend_45' },
    { title: 'Pipe bend 90', tool: 'pipe_bend_90', partId: 'pipe_bend_90' },
    { title: 'Funnel', tool: 'funnel', partId: 'funnel' },
    { title: 'Powered gate', tool: 'powered_gate', partId: 'powered_gate' },
    { title: 'Pressure plate', tool: 'pressure_plate', partId: 'pressure_plate' },
    { title: 'Counter', tool: 'counter', partId: 'counter' },
    { title: 'Clock', tool: 'clock', partId: 'clock' },
    { title: 'State latch', tool: 'latch', partId: 'latch' },
] as const satisfies readonly { title: string; tool: DriverTool | null; partId: string }[];

// Wire identities of the undelivered parts (engine/UndeliveredParts.cs): never offered, so never saved.
const UNDELIVERED = ['tennis', 'balloon'] as const;

// Palette geometry in the 1440x900 viewport: 51 px rows, the first row centred at y 155 with the palette at its top, and the last
// row (the State latch) centred at y 841 with the palette scrolled to its end (workshop-driver.ts selectTool).
const ROW_X = 130;
const ROW_PITCH = 51;
const FIRST_ROW_Y = 155;
const LAST_ROW_Y_SCROLLED = 841;
const SPRINGBOARD_ROW = PALETTE.findIndex(row => row.partId === 'spring');
const PIPE_ROW = PALETTE.findIndex(row => row.partId === 'pipe');
const unscrolledRowY = (row: number): number => FIRST_ROW_Y + row * ROW_PITCH;
const scrolledRowY = (row: number): number => LAST_ROW_Y_SCROLLED - (PALETTE.length - 1 - row) * ROW_PITCH;

// The bench point every suite places on (ENGINE-CORE-2a1).
const BENCH = { x: 720, y: 485 } as const;

async function savedPartIds(driver: WorkshopDriver): Promise<string[]> {
    const bytes = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error(`Construction database unavailable: ${request.error?.message ?? 'no detail'}`));
        request.onsuccess = () => {
            const connection = request.result;
            const transaction = connection.transaction('construction', 'readonly');
            const read = transaction.objectStore('construction').get(1);
            transaction.oncomplete = () => {
                connection.close();
                if (!(read.result instanceof Uint8Array)) { reject(new Error('The construction slot is empty.')); return; }
                resolve(Array.from(read.result));
            };
            transaction.onabort = () => { connection.close(); reject(new Error('The construction slot read aborted.')); };
        };
    }));
    const construction = decodeSavedConstruction(new Uint8Array(bytes)).construction();
    assert.ok(construction, 'The saved slot must hold a construction');
    const ids: string[] = [];
    for (let i = 0; i < construction.instancesLength(); i++) ids.push(construction.instances(i)!.partId() ?? '');
    return ids;
}

describe('Story 9R.0c: every Free palette part places on the bench; undelivered parts are absent', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    const watchConsole = (source: string) => (message: ConsoleMessage): void => {
        if (message.type() === 'error') errors.push(`${source} console error: ${message.text().slice(0, 400)}`);
    };

    before(async () => {
        driver = await WorkshopDriver.launch();
        // Registered before the first reload, so every later boot, its workers and every placement are observed.
        driver.page.on('console', watchConsole('page'));
        driver.page.on('pageerror', (error: Error) => errors.push(`page error: ${String(error).slice(0, 400)}`));
        driver.page.on('worker', (worker: Worker) => worker.on('console', watchConsole(`worker ${worker.url()}`)));
        driver.context.on('weberror', webError => errors.push(`web error: ${String(webError.error()).slice(0, 400)}`));
    });

    after(async () => {
        if (driver) await driver.closeAfterSuite();
    });

    // Place the selected palette row on the bench (one Applied Construct), Save through the menu, and return the saved part ids.
    async function placeAndSave(): Promise<string[]> {
        await driver.placeOnCanvas(BENCH.x, BENCH.y);
        await driver.save();
        return savedPartIds(driver);
    }

    for (const row of PALETTE) {
        test(`${row.title} places on the bench with no console or worker error`, { timeout: 120000 }, async () => {
            await driver.reload();
            const errorsBefore = errors.length;
            if (row.tool === null) await driver.clickAt(ROW_X, unscrolledRowY(SPRINGBOARD_ROW));
            else await driver.selectTool(row.tool);
            const saved = await placeAndSave();
            assert.deepEqual(saved, [row.partId], `The bench must hold exactly the placed ${row.title}`);
            assert.deepEqual(errors.slice(errorsBefore), [], `Placing ${row.title} must log no console or worker error`);
        });
    }

    test('Tennis ball and Balloon are absent: the row after Springboard is Pipe', { timeout: 120000 }, async () => {
        await driver.reload();
        const errorsBefore = errors.length;
        // With the palette at its top, the row after Springboard was the Tennis ball before Story 9R.0c.
        await driver.clickAt(ROW_X, unscrolledRowY(SPRINGBOARD_ROW + 1));
        const saved = await placeAndSave();
        assert.deepEqual(saved, ['pipe'], 'The row after Springboard must be Pipe');
        for (const undelivered of UNDELIVERED) assert.ok(!saved.includes(undelivered), `${undelivered} must not be placeable`);
        assert.deepEqual(errors.slice(errorsBefore), [], 'The probe must log no console or worker error');
    });

    test('Tennis ball and Balloon are absent: the row before Pipe is Springboard', { timeout: 120000 }, async () => {
        await driver.reload();
        const errorsBefore = errors.length;
        // selectTool scrolls the palette to its end; the row above Pipe was the Balloon before Story 9R.0c.
        await driver.selectTool('pipe');
        await driver.clickAt(ROW_X, scrolledRowY(PIPE_ROW - 1));
        const saved = await placeAndSave();
        assert.deepEqual(saved, ['spring'], 'The row before Pipe must be Springboard');
        for (const undelivered of UNDELIVERED) assert.ok(!saved.includes(undelivered), `${undelivered} must not be placeable`);
        assert.deepEqual(errors.slice(errorsBefore), [], 'The probe must log no console or worker error');
    });

    test('the whole run logged no console or worker error', () => {
        assert.deepEqual(errors, []);
    });
});
