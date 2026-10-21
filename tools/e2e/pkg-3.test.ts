// Story PKG-3 (9A.7 and 9A.13): the Delay Box and Impact Switch are declared by their elements/ packages, and their scenes moved
// from parts/scenes into those packages. Through actual UI controls only (palette, canvas clicks, the move-gizmo lift and the
// contextual Connect rows):
// - both parts place from the published bundle with their catalogue identities, and their typed sockets wire
//   Impact switch ActivationOut → Delay ActivationIn and Delay ActivationOut → Signal lamp ActivationIn;
// - a Run drops the Basketball on the switch, the switch fires and the Delay lights the lamp after its 1 s duration; every Reset
//   restores the exact Story 9R.1 snapshot (driver.captureSnapshot / expectResetMatches);
// - control: without the switch → Delay wire the switch still fires but the lamp stays dark;
// - Save, reload and Load restore that snapshot bit for bit (assertLoadRestores) and replay the same behaviour.
// Layout (instance ids follow placement order): 1 Basketball lifted 150 px above 2 Impact switch (450,485), 3 Delay (600,420),
// 4 Signal lamp (1000,610), the cat-037 placements.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';
import { assertLoadRestores, snapshotValue } from './run-reset-snapshot.ts';

type Point = { x: number; y: number };
const SWITCH: Point = { x: 450, y: 485 };
const DELAY: Point = { x: 600, y: 420 };
const LAMP: Point = { x: 1000, y: 610 };
const BALL_LIFT_PX = 150; // the cat-037 drop: a lifted Basketball strikes the switch placed under it
const SWITCH_NODE = '2';
const LAMP_NODE = '4';
const LATCHED = '1';
// Ball drop (about 0.8 s) plus the Delay's default 1 s duration, with margin.
const RUN_WINDOW_MS = 3500;
const Activation = 1;
const Socket = { ActivationOut: 1, ActivationIn: 4 } as const;
// Contextual rows (ui/WorkshopConnections.cs) are bottom-anchored 45 px apart; text target choices stack upward from 735.
const ROW_LAST = 789;
const TEXT_CHOICE: Point = { x: 133, y: 735 };
const BENCH_BODIES = { bodies: 1 } as const; // the Basketball; the switch, Delay and lamp are static

describe('PKG-3: Delay Box and Impact Switch element packages E2E', () => {
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

    /** The source's only remaining Connect row (its last row) to the target's single text socket choice. */
    async function connect(source: Point, target: Point): Promise<void> {
        await driver.selectPartAt(source.x, source.y);
        await driver.clickAt(133, ROW_LAST);
        await driver.clickAt(target.x, target.y);
        await driver.clickAt(TEXT_CHOICE.x, TEXT_CHOICE.y);
        await driver.page.keyboard.press('Escape');
        await driver.page.waitForTimeout(150);
    }

    /** The bench, the Delay → lamp wire and, unless this is the control, the switch → Delay wire; then the S0 snapshot. */
    async function build(switchWired: boolean): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(SWITCH.x, SWITCH.y);
        await driver.selectPartAt(SWITCH.x, SWITCH.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(BALL_LIFT_PX, SWITCH.x, SWITCH.y);
        await driver.selectTool('switch');
        await driver.placeOnCanvas(SWITCH.x, SWITCH.y);
        await driver.selectTool('delay');
        await driver.placeOnCanvas(DELAY.x, DELAY.y);
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(LAMP.x, LAMP.y);
        // Delay rows: [Delay: 1 s, Connect ActivationOut] → the lamp's ActivationIn.
        await connect(DELAY, LAMP);
        // Switch rows: [Connect ActivationOut] → the Delay's ActivationIn.
        if (switchWired) await connect(SWITCH, DELAY);
        const s0 = await driver.captureSnapshot(BENCH_BODIES);
        assertSaved((await driver.save()).slot, switchWired);
        return s0;
    }

    function assertSaved(slot: Uint8Array, switchWired: boolean): void {
        const construction = decodeSavedConstruction(slot).construction();
        assert.ok(construction, 'the saved slot holds a construction');
        const parts = Array.from({ length: construction.instancesLength() }, (_, i) =>
            [construction.instances(i)!.id(), construction.instances(i)!.partId() ?? ''] as const);
        assert.deepEqual(parts, [[1n, 'ball'], [2n, 'switch'], [3n, 'delay'], [4n, 'lamp']], 'the packaged parts save with their identities');
        const links = Array.from({ length: construction.connectionsLength() }, (_, i) => {
            const link = construction.connections(i)!;
            return [link.source(), link.fromSocket(), link.target(), link.toSocket(), link.domain()] as const;
        });
        const expected: (readonly [bigint, number, bigint, number, number])[] = [[3n, Socket.ActivationOut, 4n, Socket.ActivationIn, Activation]];
        if (switchWired) expected.push([2n, Socket.ActivationOut, 3n, Socket.ActivationIn, Activation]);
        assert.deepEqual([...links].sort((a, b) => Number(a[0] - b[0])), [...expected].sort((a, b) => Number(a[0] - b[0])),
            'exactly the authored typed activation links are saved');
    }

    /** The committed activation phase of every node in a read. */
    function phases(read: RunResetSnapshot): Map<string, string> {
        const result = new Map<string, string>();
        const length = Number(snapshotValue(read, 'read.activations.length'));
        for (let i = 0; i < length; i++)
            result.set(snapshotValue(read, `read.activations[${i}].node`), snapshotValue(read, `read.activations[${i}].phase`));
        return result;
    }

    /** Runs past the drop and the delay, pauses, checks the switch fired and returns the lamp's committed phase; then Resets exactly. */
    async function runResetCycle(s0: RunResetSnapshot, label: string): Promise<string | undefined> {
        await driver.run(RUN_WINDOW_MS);
        const committed = phases(await driver.expectRunAdvanced(s0));
        assert.equal(committed.get(SWITCH_NODE), LATCHED, `${label}: the Impact switch (2) fired on the ball's impact`);
        await driver.reset();
        await driver.expectResetMatches(s0);
        assert.deepEqual(errors, [], `${label}: no console or worker error: ${errors.join('; ')}`);
        return committed.get(LAMP_NODE);
    }

    test('1. The switch fires, the Delay lights the lamp, and each of two Resets restores the exact snapshot', { timeout: 300000 }, async () => {
        const s0 = await build(true);
        for (const cycle of ['first run', 'second run'])
            assert.equal(await runResetCycle(s0, cycle), LATCHED, `${cycle}: the lamp (4) is lit through the Delay`);
    });

    test('2. Control: without the switch → Delay wire the switch fires but the lamp stays dark', { timeout: 300000 }, async () => {
        const s0 = await build(false);
        assert.notEqual(await runResetCycle(s0, 'unwired run'), LATCHED, 'unwired run: the lamp stays dark');
    });

    test('3. Save, reload and Load restore the packaged parts and their wires bit for bit, and replay the delayed lamp', { timeout: 300000 }, async () => {
        const s0 = await build(true);
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH_BODIES);
        assertLoadRestores(s0, loaded);
        assertSaved((await driver.save()).slot, true);
        assert.equal(await runResetCycle(loaded, 'loaded run'), LATCHED, 'loaded run: the lamp is lit through the Delay');
    });
});
