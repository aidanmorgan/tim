// Story PKG-1 (9A.5, 9A.6 and 9A.12): the Receiver, Bowling ball and Springboard are declared by their elements/ packages, and their
// scenes moved from parts/scenes into those packages. Through actual UI controls only:
// - the three parts place from the published bundle with their catalogue identities;
// - a Run moves the machine (the bowling ball drops onto the springboard plate), and every Reset restores the exact Story 9R.1
//   snapshot (driver.captureSnapshot / expectResetMatches);
// - Save, reload and Load restore that snapshot bit for bit (assertLoadRestores).
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';
import { assertLoadRestores } from './run-reset-snapshot.ts';

// The bench point every suite places on (ENGINE-CORE-2a1); the Receiver sits about 4.5 m to its left.
const BENCH = { x: 720, y: 485 } as const;
const RECEIVER = { x: 480, y: 485 } as const;
// The Springboard has no driver tool: its palette row is pressed directly, exactly as cat-062 selects it.
const SPRINGBOARD_ROW = { x: 130, y: 715 } as const;
// Move-gizmo lift (about 56 px/m): the bowling ball starts about 1.6 m above the springboard plate.
const BALL_LIFT_PX = 90;
// The committed dynamic bodies: the bowling ball and the springboard's plate (the base and the Receiver are static).
const BENCH_BODIES = { bodies: 2 } as const;

describe('PKG-1: Receiver, Bowling ball and Springboard element packages E2E', () => {
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

    // Bowling ball lifted above the bench point, the Springboard under it, and the Receiver to the left; then the S0 snapshot.
    async function buildBench(): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('bowling');
        await driver.placeOnCanvas(BENCH.x, BENCH.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(BALL_LIFT_PX, BENCH.x, BENCH.y);
        await driver.page.keyboard.press('Escape');
        await driver.clickAt(SPRINGBOARD_ROW.x, SPRINGBOARD_ROW.y);
        await driver.placeOnCanvas(BENCH.x, BENCH.y);
        await driver.selectTool('receiver');
        await driver.placeOnCanvas(RECEIVER.x, RECEIVER.y);
        return driver.captureSnapshot(BENCH_BODIES);
    }

    function savedPartIds(slot: Uint8Array): string[] {
        const construction = decodeSavedConstruction(slot).construction();
        assert.ok(construction, 'the saved slot holds a construction');
        return Array.from({ length: construction.instancesLength() }, (_, i) => construction.instances(i)!.partId() ?? '');
    }

    async function runResetCycle(s0: RunResetSnapshot, label: string): Promise<void> {
        await driver.run(1500);
        // The paused committed read is past tick 0 and differs from S0: the ball fell and the plate moved.
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        await driver.expectResetMatches(s0);
        assert.equal(errors.length, 0, `${label}: zero console or worker errors expected, got: ${errors.join('; ')}`);
    }

    test('1. The three packaged parts place through the palette and save with their catalogue identities', { timeout: 150000 }, async () => {
        await buildBench();
        const { slot } = await driver.save();
        assert.deepEqual(savedPartIds(slot), ['bowling', 'spring', 'basket']);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Run moves the bowling ball onto the springboard, and each of two Resets restores the exact snapshot', { timeout: 150000 }, async () => {
        const s0 = await buildBench();
        await runResetCycle(s0, 'first run');
        await runResetCycle(s0, 'second run');
    });

    test('3. Save, reload and Load restore the three packaged parts bit for bit, and the loaded bench still resets exactly', { timeout: 150000 }, async () => {
        const s0 = await buildBench();
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH_BODIES);
        assertLoadRestores(s0, loaded);
        await runResetCycle(loaded, 'loaded run');
    });
});
