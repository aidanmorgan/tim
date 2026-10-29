// Story 9R.7 (reopened Story 6.10): the joined_pipe lesson ("Meet in the middle") through actual Workshop UI in Chrome.
// The content locks the ball (-1.3081, 8.402188, 0), a 90° bend at (1, 3.5, 0) turned -45° about Z, and the Receiver (-0.2, 0.6, 0),
// and grants one straight pipe. The player shortens the pipe to 2 m with its square resize handle, tilts it downhill and drags its
// lower mouth onto the bend's upper mouth; the release snaps the joint (Story 9R.6). Every pose is read back from the saved
// construction of a Run/Reset snapshot (tools/e2e/tube-mouths.ts); ball motion is sampled from the pose ring.
// 1. The level: locked fixtures, knots and inventory equal content/puzzles.json; a second pipe placement issues no command.
// 2. Solve: resized to 2 m, tilted and snapped, the ball travels pipe -> bend -> Receiver; exactly one solve is reported.
// 3. Run/Reset and Save/Load: Reset restores the exact snapshot and a second Run solves; Save, reload and Load restore the pipe's
//    pose, length and joint byte for byte, and the loaded level solves.
// 4. Unsnapped (Precise): the same pipe released 0.72 m left of the joint keeps its pose; the ball lands on the outer shell, never
//    enters either bore, and the goal stays unsolved for 1,200 ticks.
// 5. Default length (Precise): a 3.6 m pipe snapped to the bend puts its upper mouth off the drop line; the ball misses the bore.
// 6. Boundaries: the handle commits exactly 1 m and 8 m at the limits; one Undo restores the prior length; Escape cancels a drag.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.ts';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { decodeSavedConstruction, type AssistanceKnotRecord } from './workshop-wire.ts';
import { assertLoadRestores, snapshotEntries, snapshotValue } from './run-reset-snapshot.ts';
import { assertJoined, assertNotJoined, dot, length, mouth, pipeLength, pose, rotate, sub, type Vec3 } from './tube-mouths.ts';

// The locked ball is the only dynamic body; the bend, the Receiver and the pipe are static.
const BENCH = { bodies: 1 } as const;
const BALL = 1, BEND = 2, RECEIVER = 3, PIPE = 4;
const BORE = 0.65;
const TICKS = 1200; // the negative rows' window at 120 Hz

// Authored lessons use the orthographic front view: camera size 13.8 m over the 900 px viewport, centred on (0, 3.6).
const PX_PER_M = 900 / 13.8;
const screen = (x: number, y: number): { x: number; y: number } =>
    ({ x: Math.round(720 + PX_PER_M * x), y: Math.round(450 - PX_PER_M * (y - 3.6)) });
const METRE_X = { x: PX_PER_M, y: 0 };
// Rotation-gizmo radius: 0.55 × the pipe artwork's bounds diagonal (length + 0.18 by two 1.48 m collar diameters), 0.85–2.5 m.
const handleRadius = (metres: number): number => Math.min(2.5, Math.max(0.85, 0.55 * Math.hypot(metres + 0.18, 1.48, 1.48)));

const PARK: readonly [number, number] = [-3.5, 6]; // where the granted pipe is first placed: level, far from every mouth
const SOLUTION: readonly [number, number] = [-0.5373, 6.031442]; // content solution pipe_1 (length 2, -45° about Z)
// A refused snap leaves the release on the 0.1 grid: (-1.3, 6) puts the lower mouth 0.76 m left of the bend's upper mouth.
const UNSNAPPED: readonly [number, number] = [-1.3, 6];
const DEFAULT_SNAP: readonly [number, number] = [-1.103, 6.597]; // a 3.6 m pipe's centre when its lower mouth meets the bend

interface Knots { readonly [field: string]: number }
const CONTENT = (JSON.parse(readFileSync(new URL('../../content/puzzles.json', import.meta.url), 'utf8')) as
    { id: string; inventory: Record<string, number>; parts: { id: string; kind: string; locked: boolean; position: number[];
        orientation: number[]; difficulty: Knots[] }[]; solution: { kind: string; difficulty: Knots[]; properties: { length: number } }[] }[])
    .find(level => level.id === 'joined_pipe')!;
const KNOT_FIELDS = ['precision', 'position_window', 'rotation_window', 'max_position_correction', 'max_rotation_correction',
    'blend_seconds', 'capture_margin', 'capture_speed', 'capture_dwell', 'guide_acceleration', 'trigger_threshold'];

/** The eleven f32 lanes of one saved assistance knot, in KNOT_FIELDS order. */
function knotLanes(knot: AssistanceKnotRecord): number[] {
    return [knot.precision(), knot.positionWindow(), knot.rotationWindowDegrees(), knot.maximumPositionCorrection(),
        knot.maximumRotationCorrectionDegrees(), knot.blend(), knot.captureMargin(), knot.captureSpeed(), knot.captureDwell(),
        knot.guideAcceleration(), knot.triggerThreshold()];
}

describe('Story 9R.7: joined_pipe lesson (Meet in the middle)', () => {
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

    function noErrors(): void {
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    }

    async function openLevel(): Promise<void> {
        errors.length = 0;
        await driver.reload();
        await driver.selectLevel('joined_pipe');
    }

    /** Place the granted pipe at PARK through the drawer and select it. */
    async function placePipe(): Promise<{ x: number; y: number }> {
        const at = screen(...PARK);
        await driver.selectTool('pipe');
        await driver.placeOnCanvas(at.x, at.y);
        await driver.selectPartAt(at.x, at.y);
        return at;
    }

    /** Resize the selected pipe from `from` metres by `delta` with its +X square handle. */
    async function resize(at: { x: number; y: number }, from: number, delta: number): Promise<void> {
        const handle = { x: Math.round(at.x + PX_PER_M * handleRadius(from)), y: at.y };
        await driver.resizeSelectedPart(handle, METRE_X, delta);
    }

    /** The granted pipe resized to `metres`, tilted downhill (-45°) and dragged so its centre is released at `to`. */
    async function build(metres: number, to: readonly [number, number]): Promise<RunResetSnapshot> {
        const at = await placePipe();
        if (metres !== 3.6) await resize(at, 3.6, metres - 3.6);
        await driver.selectPartAt(at.x, at.y);
        await driver.tiltSelectedByFineRotate(-9);
        // Grab the shell 0.6 m down its axis: in the front view the gizmo's Z handle projects onto the part's centre.
        const grab = (x: number, y: number) => screen(x + 0.6 * Math.SQRT1_2, y - 0.6 * Math.SQRT1_2);
        const from = grab(...PARK), target = grab(...to);
        await driver.dragPart(from.x, from.y, target.x, target.y);
        await driver.pressKey('Escape');
        const s0 = await driver.captureSnapshot(BENCH);
        assert.ok(Math.abs(pipeLength(s0, PIPE) - metres) < 1e-6, `the saved pipe is ${pipeLength(s0, PIPE)} m, not ${metres} m`);
        return s0;
    }

    /** Ball positions from every new pose-ring publication over `ms` of the running machine. */
    async function sample(ms: number): Promise<Vec3[]> {
        const samples: Vec3[] = [];
        let previous = '';
        const end = Date.now() + ms;
        while (Date.now() < end) {
            const slot = await driver.readLatestPose();
            const body = slot?.bodies.find(b => b.id === String(BALL));
            if (slot && body && slot.sequence !== previous) { previous = slot.sequence; samples.push([body.px, body.py, body.pz]); }
            await driver.page.waitForTimeout(15);
        }
        return samples;
    }

    /** Whether a ball centre lies inside the committed pipe's bore (axially between its shell ends, radially inside 0.65 m). */
    function inPipeBore(s0: RunResetSnapshot, p: Vec3): boolean {
        const { position, rotation } = pose(s0, PIPE);
        const axis = rotate(rotation, [1, 0, 0]);
        const offset = sub(p, position), along = dot(offset, axis);
        const radial = length(sub(offset, [axis[0] * along, axis[1] * along, axis[2] * along]));
        return Math.abs(along) <= pipeLength(s0, PIPE) / 2 && radial < BORE;
    }

    /** Whether a ball centre has passed the bend's upper mouth plane inside its bore (it entered the bend). */
    function inBendMouth(s0: RunResetSnapshot, p: Vec3): boolean {
        const { at, out } = mouth(s0, BEND, 'start');
        const offset = sub(p, at), depth = -dot(offset, out);
        const lateral = length(sub(offset, [out[0] * -depth, out[1] * -depth, out[2] * -depth]));
        return depth > 0 && depth < 1 && lateral < BORE;
    }

    async function runSolved(s0: RunResetSnapshot, label: string): Promise<void> {
        const solvedBefore = await driver.readCaptured();
        await driver.run();
        const samples = await sample(3500);
        assert.ok(samples.some(p => inPipeBore(s0, p)), `${label}: the ball travelled inside the pipe's bore (${samples.length} samples)`);
        assert.ok(samples.some(p => inBendMouth(s0, p)), `${label}: the ball entered the bend's upper mouth`);
        if ((await driver.readCaptured()) === solvedBefore) await driver.waitForGoalSolved(8000);
        await driver.page.waitForTimeout(1500); // a second solve would print here
        assert.equal((await driver.readCaptured()) - solvedBefore, 1, `${label}: exactly one solve is reported`);
        const ball = (await driver.readLatestPose())?.bodies.find(b => b.id === String(BALL));
        assert.ok(ball && Math.abs(ball.px - -0.2) < 0.7 && ball.py < 1.2, `${label}: the ball rests in the Receiver, at ${ball?.px}, ${ball?.py}`);
    }

    async function resetMatches(s0: RunResetSnapshot): Promise<void> {
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        await driver.expectResetMatches(s0);
    }

    /** A negative row at Precise: 1,200 ticks with no solve; the committed read proves the window elapsed. */
    async function runUnsolved(s0: RunResetSnapshot, label: string): Promise<Vec3[]> {
        await driver.run();
        const samples = await sample(TICKS / 120 * 1000 + 500);
        assert.equal(await driver.readCaptured(), 0, `${label}: the goal stays unsolved`);
        const paused = await driver.expectRunAdvanced(s0);
        assert.ok(paused.tick >= BigInt(TICKS), `${label}: the run covered ${paused.tick} ticks, fewer than ${TICKS}`);
        assert.equal(await driver.readCaptured(), 0, `${label}: still unsolved at tick ${paused.tick}`);
        await driver.reset();
        await driver.expectResetMatches(s0);
        return samples;
    }

    test('1. The level: fixtures, knots and inventory equal the content; a second pipe issues no command', { timeout: 150000 }, async () => {
        await openLevel();
        const s0 = await driver.captureSnapshot(BENCH);
        const parts = Object.fromEntries(CONTENT.parts.map(part => [part.id, part]));
        for (const [id, name, kind] of [[BALL, 'ball', 'ball'], [BEND, 'bend', 'pipe_bend_90'], [RECEIVER, 'receiver', 'basket']] as const) {
            const { position, rotation, part } = pose(s0, id);
            assert.equal(part, kind);
            assert.equal(parts[name].kind, kind);
            assert.equal(snapshotValue(s0, `saved.construction.instances[id=${id}].locked`), '1', `${name} is locked`);
            assert.ok(length(sub(position, parts[name].position as unknown as Vec3)) < 1e-4, `${name} at ${position.join(', ')}`);
            // The content orientation is column-major: its first column is the image of local +X.
            const [cx, cy, cz] = parts[name].orientation;
            assert.ok(length(sub(rotate(rotation, [1, 0, 0]), [cx, cy, cz])) < 1e-4, `${name} orientation`);
        }
        assert.equal(snapshotEntries(s0, 'saved.construction.instances').filter(([path]) => path.endsWith('.part_id')).length, 3);
        const { slot } = await driver.save();
        const puzzle = decodeSavedConstruction(slot).construction()!.puzzle()!;
        assert.deepEqual(CONTENT.inventory, { pipe: 1 });
        assert.equal(puzzle.inventoryPart(), 'pipe');
        assert.equal(puzzle.inventoryCount(), 1);
        // Three profiles (ball, receiver, pipe) of three f32 knots each, the saved assistance records (Story 9A.44).
        assert.equal(puzzle.assistanceLength(), 9);
        const profiles = [parts.ball.difficulty, parts.receiver.difficulty, CONTENT.solution[0].difficulty];
        assert.deepEqual(parts.bend.difficulty, parts.ball.difficulty, 'the bend repeats the ball knots');
        assert.equal(CONTENT.solution[0].properties.length, 2);
        profiles.forEach((profile, p) => profile.forEach((knot, k) => KNOT_FIELDS.forEach((field, f) => {
            const value = knotLanes(puzzle.assistance(p * 3 + k)!)[f];
            assert.equal(value, Math.fround(knot[field]), `profile ${p} knot ${k} ${field}: ${value} ≠ ${knot[field]}`);
        })));

        // The granted pipe is the only one: with it placed, the drawer row is spent and a canvas click issues nothing.
        await placePipe();
        await driver.pressKey('Escape');
        const placed = await driver.captureSnapshot(BENCH);
        const spot = screen(3, 6);
        await driver.expectNoCommand('second pipe', async () => {
            await driver.clickAt(130, 155);
            await driver.clickAt(spot.x, spot.y);
        });
        await driver.pressKey('Escape');
        const unchanged = await driver.captureSnapshot(BENCH);
        assert.deepEqual(snapshotEntries(unchanged, 'saved'), snapshotEntries(placed, 'saved'), 'the saved construction is unchanged');
        assert.equal(snapshotEntries(unchanged, 'saved.construction.instances').filter(([path]) => path.endsWith('.part_id')).length, 4);
        noErrors();
    });

    test('2. Solve: a 2 m pipe tilted downhill and snapped to the bend delivers the ball; exactly one solve', { timeout: 150000 }, async () => {
        await openLevel();
        const s0 = await build(2, SOLUTION);
        assertJoined(s0, { id: PIPE, end: 'end' }, { id: BEND, end: 'start' }, 'the pipe joins the locked bend');
        assert.ok(Math.abs(mouth(s0, PIPE, 'start').at[0] - -1.3081) < 0.02, 'the upper mouth is on the drop line');
        await runSolved(s0, 'solved run');
        await resetMatches(s0);
        noErrors();
    });

    test('3. Run/Reset and Save/Load: Reset is exact, a second Run solves; Load restores pose, length and joint', { timeout: 150000 }, async () => {
        await openLevel();
        const s0 = await build(2, SOLUTION);
        await runSolved(s0, 'first run');
        await resetMatches(s0);
        await runSolved(s0, 'second run');
        await resetMatches(s0);

        // The snapshot's Save stored the joined construction; a reload starts a new page.
        await driver.reload();
        await driver.selectLevel('joined_pipe');
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        assert.deepEqual(snapshotEntries(loaded, `saved.construction.instances[id=${PIPE}]`),
            snapshotEntries(s0, `saved.construction.instances[id=${PIPE}]`), 'the pipe pose and length are byte-identical');
        assertJoined(loaded, { id: PIPE, end: 'end' }, { id: BEND, end: 'start' }, 'the loaded joint');
        await runSolved(loaded, 'loaded run');
        await resetMatches(loaded);
        noErrors();
    });

    test('4. Unsnapped at Precise: released 0.72 m off the joint, the ball leaves the route and the goal stays unsolved', { timeout: 150000 }, async () => {
        await openLevel();
        await driver.setPrecision('precise');
        const s0 = await build(2, UNSNAPPED);
        assertNotJoined(s0, { id: PIPE, end: 'end' }, { id: BEND, end: 'start' }, 'the refused snap');
        const gap = length(sub(mouth(s0, PIPE, 'end').at, mouth(s0, BEND, 'start').at));
        assert.ok(gap >= 0.72, `the mouths are ${gap} m apart (pipe at ${pose(s0, PIPE).position.join(', ')}), not 0.72 m off`);
        const samples = await runUnsolved(s0, 'unsnapped');
        assert.ok(samples.length > 50, `${samples.length} pose samples`);
        assert.ok(!samples.some(p => inPipeBore(s0, p)), 'the ball never entered the pipe bore');
        assert.ok(!samples.some(p => inBendMouth(s0, p)), 'the ball never entered the bend');
        const last = samples.at(-1)!;
        assert.ok(!(Math.abs(last[0] - -0.2) < 0.7 && last[1] < 1.2), `the ball is not in the Receiver, at ${last.join(', ')}`);
        noErrors();
    });

    test('5. Default length at Precise: a 3.6 m pipe snapped to the bend misses the drop line', { timeout: 150000 }, async () => {
        await openLevel();
        await driver.setPrecision('precise');
        const s0 = await build(3.6, DEFAULT_SNAP);
        assertJoined(s0, { id: PIPE, end: 'end' }, { id: BEND, end: 'start' }, 'the 3.6 m joint');
        assert.ok(mouth(s0, PIPE, 'start').at[0] < -1.9, 'the 3.6 m upper mouth lies about 1 m left of the drop line');
        const samples = await runUnsolved(s0, 'default length');
        assert.ok(!samples.some(p => inPipeBore(s0, p)), 'the ball strikes the outer shell, never the bore');
        noErrors();
    });

    test('6. Boundaries: the handle commits exactly 1 m and 8 m; one Undo restores the length; Escape cancels', { timeout: 150000 }, async () => {
        await openLevel();
        const at = await placePipe();
        await resize(at, 3.6, -3); // drags past 1 m: the pointer boundary clamps
        await driver.pressKey('Escape');
        const short = await driver.captureSnapshot(BENCH);
        assert.equal(pipeLength(short, PIPE), 1, 'the shortest pipe is exactly 1 m');
        await driver.undo();
        const undone = await driver.captureSnapshot(BENCH);
        assert.equal(snapshotValue(undone, `saved.construction.instances[id=${PIPE}].length`), '0x40666666',
            'one Undo restores the prior length, exactly the f32 3.6');

        await driver.selectPartAt(at.x, at.y);
        await resize(at, 3.6, 5); // past 8 m
        await driver.pressKey('Escape');
        const long = await driver.captureSnapshot(BENCH);
        assert.equal(pipeLength(long, PIPE), 8, 'the longest pipe is exactly 8 m');

        await driver.selectPartAt(at.x, at.y);
        const handle = { x: Math.round(at.x + PX_PER_M * handleRadius(8)), y: at.y };
        await driver.resizeSelectedPartCancelled(handle, METRE_X, -3);
        await driver.pressKey('Escape');
        const cancelled = await driver.captureSnapshot(BENCH);
        assert.deepEqual(snapshotEntries(cancelled, 'saved'), snapshotEntries(long, 'saved'), 'Escape left the 8 m pipe unchanged');
        // Control: the same drag from the same handle point without Escape commits, so the cancelled gesture did grab the handle.
        await driver.selectPartAt(at.x, at.y);
        await driver.resizeSelectedPart(handle, METRE_X, -3);
        await driver.pressKey('Escape');
        const shortened = await driver.captureSnapshot(BENCH);
        assert.equal(pipeLength(shortened, PIPE), 5, 'the uncancelled drag commits 5 m');
        noErrors();
    });
});
