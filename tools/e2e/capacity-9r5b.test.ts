// Story 9R.5b: every physics table is sized at Run, so Build sets no part limit (owner decision, 20 Oct 2026: "the physics body table
// needs to be infinite"; an unlimited Free Workshop). The bench is built with the actual Workshop UI (palette, canvas clicks, the
// move-gizmo lift and the contextual Connect row): forty Impact Switches (Build refused the ninth trigger and the 33rd part), a
// Basketball lifted above the last switch, and a Signal lamp wired from that switch's ActivationOut. 42 parts, 40 triggers.
// Layout (instance ids follow placement order): switches 1..39 in an eight-by-five grid, 40 the Basketball, 41 the last switch (under
// the lifted ball, the grid's fortieth cell), 42 the Signal lamp. Observation is read-only: the committed read of each Pause and Save
// acknowledgement (the Story 9R.1 snapshot), the pose ring and the IndexedDB save slot.
// 1. The last switch fires its own lamp: after Run the committed read shows switch 41 and lamp 42 Latched and every other switch
//    Clear; Reset restores the Build snapshot exactly (twice); a reload and Load restores the 42-part bench and replays it.
// 2. Control: the same bench without the wire leaves the lamp Clear although switch 41 fires.
// 3. Twenty Basketballs (Build refused the seventeenth moving body): every one publishes through the pose ring the worker grows at
//    Run, with its own identity; Reset is exact.
// Every leg fails on a worker or console error, and WorkshopDriver.close() fails it on any session fault.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores, snapshotValue } from './run-reset-snapshot.ts';
import { decodeSavedConstruction } from './workshop-wire.ts';

type Point = { x: number; y: number };
// Free-workshop camera (workshop-driver.ts FREE_VIEW_PX_PER_M): a point (x, 3, z) on the placement plane projects to the screen
// offset (53.8 x - 36.8 z, 17.0 x + 24.8 z) from (720, 485).
const screenOf = (x: number, z: number): Point =>
    ({ x: Math.round(720 + 53.8 * x - 36.8 * z), y: Math.round(485 + 17.0 * x + 24.8 * z) });

const SWITCHES = 40;
// Eight columns by five rows 1.5 m apart (x -6..4.5 m, z -3..3 m, the capacity-9r bench's proven placement area): no switch
// (1.1 m by 1.0 m) overlaps a neighbour.
const GRID: Point[] = Array.from({ length: SWITCHES }, (_, i) => screenOf(-6 + i % 8 * 1.5, -3 + Math.floor(i / 8) * 1.5));
const LAST = GRID[SWITCHES - 1];
const LAMP = screenOf(6.5, 0);
const BALL_LIFT_PX = 150; // the cat-037 drop: a lifted Basketball strikes the switch placed under it
const LAST_SWITCH_NODE = '41';
const LAMP_NODE = '42';
const LATCHED = '1';
// Contextual rows (ui/WorkshopConnections.cs) are bottom-anchored 45 px apart; text target choices stack upward from 735.
const ROW_LAST = 789;
const TEXT_CHOICE: Point = { x: 133, y: 735 };
const PART_COUNT = SWITCHES + 2;
const MOVING = 20; // past the retired 16-row moving-body, read-set and pose-ring tables

describe('Story 9R.5b: a forty-switch bench past every retired table runs, resets and saves', () => {
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

    /** Thirty-nine switches, a Basketball lifted above the fortieth cell, the last switch under it, then the lamp; optionally the
     *  last switch's ActivationOut wired to the lamp through its contextual Connect row. */
    async function build(wired: boolean): Promise<RunResetSnapshot> {
        errors.length = 0;
        await driver.reload();
        for (const [index, point] of GRID.slice(0, SWITCHES - 1).entries()) {
            await driver.selectTool('switch');
            try { await driver.placeOnCanvas(point.x, point.y); }
            catch (error) {
                await driver.page.screenshot({ path: `.anvil/capacity-9r5b-placement-${index + 1}.png` });
                throw error;
            }
        }
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(LAST.x, LAST.y);
        await driver.selectPartAt(LAST.x, LAST.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(BALL_LIFT_PX, LAST.x, LAST.y);
        await driver.selectTool('switch');
        await driver.placeOnCanvas(LAST.x, LAST.y);
        await driver.selectTool('lamp');
        await driver.placeOnCanvas(LAMP.x, LAMP.y);
        if (wired) {
            // Switch rows: [Connect ActivationOut] → the lamp's ActivationIn (single text choice).
            await driver.selectPartAt(LAST.x, LAST.y);
            await driver.clickAt(133, ROW_LAST);
            await driver.clickAt(LAMP.x, LAMP.y);
            await driver.clickAt(TEXT_CHOICE.x, TEXT_CHOICE.y);
            await driver.page.keyboard.press('Escape');
            await driver.page.waitForTimeout(150);
        }
        const s0 = await driver.captureSnapshot({ bodies: 1 });
        const saved = decodeSavedConstruction((await driver.save()).slot).construction()!;
        const kinds = Array.from({ length: saved.instancesLength() }, (_, i) => saved.instances(i)!.partId() ?? '');
        assert.equal(kinds.length, PART_COUNT, `The bench holds ${PART_COUNT} parts (Build refused the 33rd before Story 9R.5b)`);
        assert.equal(kinds.filter(kind => kind === 'switch').length, SWITCHES, 'Forty Impact Switches (Build refused the ninth trigger)');
        assert.equal(saved.connectionsLength(), wired ? 1 : 0, wired ? 'The last switch is wired to the lamp' : 'No wire');
        if (wired) {
            const link = saved.connections(0)!;
            assert.deepEqual([link.source(), link.target()], [41n, 42n], 'The wire joins the last switch (41) to the lamp (42)');
        }
        return s0;
    }

    /** The committed activation phase of every node in a read. */
    function phases(read: RunResetSnapshot): Map<string, string> {
        const result = new Map<string, string>();
        const length = Number(snapshotValue(read, 'read.activations.length'));
        for (let i = 0; i < length; i++)
            result.set(snapshotValue(read, `read.activations[${i}].node`), snapshotValue(read, `read.activations[${i}].phase`));
        return result;
    }

    /** Runs until the ball has struck the last switch, pauses through the menu and returns the Pause acknowledgement's read. */
    async function runAndPause(s0: RunResetSnapshot, label: string): Promise<Map<string, string>> {
        await driver.run(2500);
        const pose = await driver.readLatestPose();
        assert.equal(pose?.bodies.length, 1, `${label}: the Basketball is the one committed body`);
        const read = await driver.expectRunAdvanced(s0);
        const committed = phases(read);
        assert.equal(committed.get(LAST_SWITCH_NODE), LATCHED, `${label}: the last switch (41) fired`);
        for (let node = 1; node < SWITCHES; node++)
            assert.notEqual(committed.get(String(node)), LATCHED, `${label}: switch ${node} was never struck`);
        return committed;
    }

    test('1. The last of forty switches lights its own lamp; Reset is exact and Save/Load replays it', { timeout: 900000 }, async () => {
        const s0 = await build(true);
        for (const cycle of ['first', 'second']) {
            const committed = await runAndPause(s0, `${cycle} run`);
            assert.equal(committed.get(LAMP_NODE), LATCHED, `${cycle} run: the lamp (42) is lit by the last switch`);
            await driver.reset();
            await driver.expectResetMatches(s0);
        }
        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot({ bodies: 1 });
        assertLoadRestores(s0, loaded);
        const committed = await runAndPause(loaded, 'loaded run');
        assert.equal(committed.get(LAMP_NODE), LATCHED, 'loaded run: the lamp is lit by the last switch');
        await driver.reset();
        await driver.expectResetMatches(loaded);
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });

    // The worker grows the bootstrap pose ring (16 moving bodies) at Run; nothing is truncated.
    test('3. Twenty Basketballs (past the retired 16 moving bodies) all publish through the grown pose ring; Reset is exact', { timeout: 900000 }, async () => {
        errors.length = 0;
        await driver.reload();
        const balls = GRID.slice(0, MOVING);
        for (const point of balls) {
            await driver.selectTool('basketball');
            await driver.placeOnCanvas(point.x, point.y);
        }
        const s0 = await driver.captureSnapshot({ bodies: MOVING });
        assert.equal(s0.bodies, MOVING, `The committed read holds all ${MOVING} moving bodies`);
        for (const cycle of ['first', 'second']) {
            await driver.run(1500);
            const pose = await driver.readLatestPose();
            assert.equal(pose?.bodies.length, MOVING, `${cycle} run: the pose ring publishes every moving body (${pose?.bodies.length})`);
            assert.deepEqual(pose!.bodies.map(body => body.id).sort((a, b) => Number(a) - Number(b)),
                Array.from({ length: MOVING }, (_, i) => String(i + 1)), `${cycle} run: each ball keeps its identity`);
            await driver.expectRunAdvanced(s0);
            await driver.reset();
            await driver.expectResetMatches(s0);
        }
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });

    test('2. Control: without the wire the lamp stays dark although the last switch fires', { timeout: 900000 }, async () => {
        const s0 = await build(false);
        const committed = await runAndPause(s0, 'unwired run');
        assert.notEqual(committed.get(LAMP_NODE), LATCHED, 'unwired run: the lamp stays dark');
        await driver.reset();
        await driver.expectResetMatches(s0);
        assert.deepEqual(errors, [], `No console or worker error: ${errors.join('; ')}`);
    });
});
