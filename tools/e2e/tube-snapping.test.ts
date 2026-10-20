// Story 9R.6: typed tube-mouth snapping through actual Workshop UI in Chrome (reopened Stories 6.9 and 6.10; Story 6.11 repair).
// A tube's mouth (position, outward axis, bore) is scene data. Placing a tube with a mouth within 0.45 m and 20° of a free mouth of
// equal bore and opposing axis snaps it: the hover preview shows a gold joint ring and issues no command; the click (or the release
// of a moved tube) commits the snapped pose, which bypasses the 0.1 grid. One Undo removes only the new part.
// Every committed pose is read back from the saved construction of a Run/Reset snapshot (tools/e2e/tube-mouths.ts).
// 1. Snap: a pipe offered 0.12 m off and turned 5°: gold preview, no command while hovering; mouths coincide; one Undo removes it.
// 2. Far and boundary: 0.72 m and 0.52 m keep the grid pose; 0.42 m snaps.
// 3. Angled: 40° keeps the grid pose; 15° snaps.
// 4. Occupied: a third pipe at the joined mouth keeps its grid pose.
// 5. Wrong bore: the Funnel's 1.3 m inlet keeps the grid pose; its outlet snaps.
// 6. Locked mover: dragging a level's locked fixture issues no command and moves nothing.
// 7. Run/Pause: no placement while Running or Paused; snapping returns after Reset.
// 8. Moved tube (D4): dragging a placed pipe near a free mouth snaps it on release; one Undo returns it.
// 9. Reset and Save/Load: the joined placement survives the exact Reset snapshot and a reload + Load.
// 10-13. A ball crosses straight→45°, 45°→45°, straight→90° and Funnel outlet→pipe joints continuously; Reset restores each bench.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { isWorkshopConsoleFailure } from '../workshop-console.mjs';
import { WorkshopDriver, type RunResetSnapshot } from './workshop-driver.ts';
import { assertLoadRestores, snapshotEntries } from './run-reset-snapshot.ts';
import { assertAt, assertJoined, assertNotJoined, dot, hasInstance, instanceCount, length, mouth, screen, sub, type Vec3 } from './tube-mouths.ts';

// Every bench carries one basketball, so each snapshot's committed read has one (dynamic) body; tubes are static.
const BENCH = { bodies: 1 } as const;
const BALL = 1;

describe('Story 9R.6: typed tube-mouth snapping', () => {
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

    async function place(kind: 'basketball' | 'pipe' | 'funnel', x: number, y: number, z: number): Promise<void> {
        const at = screen(x, y, z);
        await driver.selectTool(kind);
        await driver.placeOnCanvas(at.x, at.y);
        await driver.pressKey('Escape');
    }

    // A fresh free workshop with the basketball (id 1) parked at (-5, 3, 0) and a first part (id 2) at the centre (0, 3, 0).
    async function bench(first: 'pipe' | 'funnel'): Promise<void> {
        await driver.reload();
        errors.length = 0;
        await place('basketball', -5, 3, 0);
        await place(first, 0, 3, 0);
    }

    /** Gold (#f7cb52) joint-ring pixels in a 120 px square around a screen point. */
    async function goldPixels(at: { x: number; y: number }): Promise<number> {
        const png = await driver.page.screenshot({ clip: { x: at.x - 60, y: at.y - 60, width: 120, height: 120 } });
        return driver.page.evaluate(async (data: string) => {
            const bitmap = await createImageBitmap(await (await fetch(`data:image/png;base64,${data}`)).blob());
            const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
            const context = canvas.getContext('2d')!;
            context.drawImage(bitmap, 0, 0);
            const pixels = context.getImageData(0, 0, bitmap.width, bitmap.height).data;
            let count = 0;
            for (let i = 0; i < pixels.length; i += 4) {
                const [r, g, b] = [pixels[i], pixels[i + 1], pixels[i + 2]];
                if (r > 200 && g > 150 && g < 225 && b < 130 && r - b > 110) count++;
            }
            return count;
        }, png.toString('base64'));
    }

    /** Choose a tube, adjust its preview, hover it at a world point (asserting the gold ring is shown or not, with no command),
     *  then click to place it. Returns the snapshot of the committed construction. */
    async function offer(kind: 'pipe' | 'pipe_bend_45' | 'pipe_bend_90', at: Vec3, snaps: boolean,
        joint: Vec3, adjust: { tilt?: number; turn?: number; height?: number } = {}): Promise<RunResetSnapshot> {
        await driver.selectTool(kind);
        if (adjust.tilt || adjust.turn || adjust.height) await driver.adjustPreview(adjust);
        const point = screen(...at);
        await driver.hoverCanvas(point.x, point.y);
        const gold = await goldPixels(screen(...joint));        if (snaps) assert.ok(gold >= 40, `the snapping preview shows the gold joint ring (${gold} gold pixels)`);
        else assert.ok(gold < 10, `a refused snap shows no gold joint ring (${gold} gold pixels)`);
        await driver.placeOnCanvas(point.x, point.y);
        await driver.pressKey('Escape');
        return driver.captureSnapshot(BENCH);
    }

    const JOINT: Vec3 = [1.89, 3, 0];

    test('1. Snap: an offset, turned pipe previews the gold joint without a command, joins exactly, and one Undo removes only it', { timeout: 150000 }, async () => {
        await bench('pipe');
        const before = await driver.captureSnapshot(BENCH);
        // Far from any mouth the preview shows no ring; near the free end mouth (0.12 m off, turned 5°) it does.
        await driver.selectTool('pipe');
        await driver.adjustPreview({ tilt: 1 });
        const far = screen(-1, 3, 3.5);
        await driver.hoverCanvas(far.x, far.y);
        assert.ok(await goldPixels(screen(...JOINT)) < 10, 'no ring away from every mouth');
        await driver.pressKey('Escape');
        const s1 = await offer('pipe', [3.9, 3, 0], true, JOINT, { tilt: 1 });
        assertJoined(s1, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'snapped pipe');
        assertAt(s1, 3, [3.78, 3, 0], 'snapped pipe (the snap bypasses the 0.1 grid)');
        assert.ok(Math.abs(mouth(s1, 3, 'end').out[1]) < 1e-4, 'the 5° preview tilt is removed by the shortest arc');

        await driver.undo();
        const undone = await driver.captureSnapshot(BENCH);
        assert.equal(instanceCount(undone), 2);
        assert.ok(!hasInstance(undone, 3), 'Undo removed the new pipe');
        assert.deepEqual(snapshotEntries(undone, 'saved.construction.instances'), snapshotEntries(before, 'saved.construction.instances'),
            'Undo restored exactly the construction before the placement');
        noErrors();
    });

    test('2. Far and boundary: mouths 0.72 m and 0.52 m away keep the grid pose; 0.42 m snaps', { timeout: 150000 }, async () => {
        await bench('pipe');
        const far = await offer('pipe', [4.5, 3, 0], false, JOINT);
        assertAt(far, 3, [4.5, 3, 0], '0.72 m');
        assertNotJoined(far, { id: 2, end: 'end' }, { id: 3, end: 'start' }, '0.72 m');
        await driver.undo();
        const outside = await offer('pipe', [4.3, 3, 0], false, JOINT);
        assertAt(outside, 4, [4.3, 3, 0], '0.52 m');
        await driver.undo();
        const inside = await offer('pipe', [4.2, 3, 0], true, JOINT);
        assertJoined(inside, { id: 2, end: 'end' }, { id: 5, end: 'start' }, '0.42 m');
        noErrors();
    });

    test('3. Angled: a pipe turned 40° keeps the grid pose; turned 15° it snaps', { timeout: 150000 }, async () => {
        await bench('pipe');
        // Turning about Y keeps the mouths in the placement plane; each start mouth is offered at the free end mouth (1.89, 3, 0).
        const angled = await offer('pipe', [3.3, 3, -1.2], false, [1.6, 3, -0.6], { turn: 8 });
        assertAt(angled, 3, [3.3, 3, -1.2], '40°');
        await driver.undo();
        const near = await offer('pipe', [3.7, 3, -0.5], true, JOINT, { turn: 3 });
        assertJoined(near, { id: 2, end: 'end' }, { id: 4, end: 'start' }, '15°');
        noErrors();
    });

    test('4. Occupied: a third pipe offered at the joined mouth keeps its grid pose', { timeout: 150000 }, async () => {
        await bench('pipe');
        const joined = await offer('pipe', [3.9, 3, 0], true, JOINT);
        assertJoined(joined, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'first join');
        // Its start mouth is 0.32 m from the joined end mouth, inside the capture distance: only occupancy refuses it.
        const occupied = await offer('pipe', [3.9, 3, 0.3], false, [1.89, 3, 0.15]);
        assertAt(occupied, 4, [3.9, 3, 0.3], 'occupied mouth');
        noErrors();
    });

    test('5. Wrong bore: the Funnel wide inlet (bore 1.3) keeps the grid pose; its outlet snaps', { timeout: 150000 }, async () => {
        await bench('funnel');
        const inlet = await offer('pipe', [-3, 3, 0], false, [-0.99, 3, 0]);
        assertAt(inlet, 3, [-3, 3, 0], 'wide inlet');
        assertNotJoined(inlet, { id: 2, end: 'start' }, { id: 3, end: 'end' }, 'wide inlet');
        await driver.undo();
        const outlet = await offer('pipe', [3, 3, 0], true, [0.99, 3, 0]);
        assertJoined(outlet, { id: 2, end: 'end' }, { id: 4, end: 'start' }, 'outlet');
        assertAt(outlet, 4, [2.88, 3, 0], 'outlet');
        noErrors();
    });

    test('6. Locked mover: dragging the level\'s locked receiver issues no command and moves nothing', { timeout: 150000 }, async () => {
        await driver.reload();
        errors.length = 0;
        await driver.selectLevel('clear_pipe');
        const s0 = await driver.captureSnapshot(BENCH);
        const receiver = screen(2, 0.6, 0);
        await driver.dragPartRefused(receiver.x, receiver.y, receiver.x - 80, receiver.y);
        await driver.pressKey('Escape');
        const s1 = await driver.captureSnapshot(BENCH);
        assert.deepEqual(snapshotEntries(s1, 'saved.construction.instances'), snapshotEntries(s0, 'saved.construction.instances'),
            'the locked fixture did not move');
        noErrors();
    });

    test('7. Run/Pause: no placement while Running or Paused; snapping returns after Reset', { timeout: 150000 }, async () => {
        await bench('pipe');
        const s0 = await driver.captureSnapshot(BENCH);
        const near = screen(3.9, 3, 0);
        const attempt = async (phase: string) => {
            await driver.selectTool('pipe');
            await driver.expectNoCommand(`placement while ${phase}`, () => driver.clickAt(near.x, near.y));
            await driver.pressKey('Escape');
        };
        await driver.run();
        await attempt('Running');
        await driver.expectRunAdvanced(s0);
        await attempt('Paused');
        await driver.reset();
        await driver.expectResetMatches(s0);
        const after = await offer('pipe', [3.9, 3, 0], true, JOINT);
        assertJoined(after, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'after Reset');
        noErrors();
    });

    test('8. Moved tube (D4): releasing a dragged pipe near a free mouth snaps it; one Undo returns it', { timeout: 150000 }, async () => {
        await bench('pipe');
        const parked = await offer('pipe', [0, 3, 3], false, JOINT);
        assertAt(parked, 3, [0, 3, 3], 'parked pipe');
        const from = screen(0, 3, 3), to = screen(3.9, 3, 0);
        await driver.dragPart(from.x, from.y, to.x, to.y);
        await driver.pressKey('Escape');
        const moved = await driver.captureSnapshot(BENCH);
        assertJoined(moved, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'moved pipe');
        await driver.undo();
        const undone = await driver.captureSnapshot(BENCH);
        assertAt(undone, 3, [0, 3, 3], 'one Undo returns the moved pipe');
        noErrors();
    });

    test('9. Reset and Save/Load: the joined placement survives the exact Reset snapshot and a reload + Load', { timeout: 150000 }, async () => {
        await bench('pipe');
        const s0 = await offer('pipe', [3.9, 3, 0], true, JOINT);
        assertJoined(s0, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'placed');
        await driver.run();
        await driver.expectRunAdvanced(s0);
        await driver.reset();
        const reset = await driver.expectResetMatches(s0);
        assertJoined(reset, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'after Reset');

        await driver.reload();
        await driver.load();
        const loaded = await driver.captureSnapshot(BENCH);
        assertLoadRestores(s0, loaded);
        assertJoined(loaded, { id: 2, end: 'end' }, { id: 3, end: 'start' }, 'after Save/Load');
        noErrors();
    });

    // A route: the basketball (id 1) lifted above the first tube's upward start mouth; the first tube (id 2) tilted -90° and raised;
    // the second (id 3) offered below its end mouth with its preview tilted and its placement plane moved, and snapped.
    interface Route {
        readonly ballX: number; readonly ballLiftPx: number;
        readonly first: 'pipe' | 'pipe_bend_45' | 'funnel'; readonly firstRaise: number;
        readonly second: 'pipe' | 'pipe_bend_45' | 'pipe_bend_90'; readonly secondTilt: number; readonly secondHeight: number;
        readonly offerAt: Vec3;
    }

    async function crossesJoint(route: Route, label: string): Promise<void> {
        await driver.reload();
        errors.length = 0;
        const ball = screen(route.ballX, 3, 0);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(ball.x, ball.y);
        await driver.selectPartAt(ball.x, ball.y);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(route.ballLiftPx, ball.x, ball.y);
        await driver.pressKey('Escape');

        await driver.selectTool(route.first);
        await driver.placeOnCanvas(720, 485);
        await driver.selectPartAt(720, 485);
        await driver.tiltSelectedByFineRotate(-18);
        await driver.selectPartAt(720, 485);
        await driver.liftSelectedByHeight(route.firstRaise);

        await driver.selectTool(route.second);
        await driver.adjustPreview({ tilt: route.secondTilt, height: route.secondHeight });
        const at = screen(...route.offerAt);
        await driver.hoverCanvas(at.x, at.y);
        await driver.placeOnCanvas(at.x, at.y);
        await driver.pressKey('Escape');
        const s0 = await driver.captureSnapshot(BENCH);
        assertJoined(s0, { id: 2, end: 'end' }, { id: 3, end: 'start' }, label);

        const entry = mouth(s0, 2, 'start').at;
        const exit = mouth(s0, 3, 'end');
        const chordLength = length(sub(exit.at, entry));
        const chord: Vec3 = [(exit.at[0] - entry[0]) / chordLength, (exit.at[1] - entry[1]) / chordLength, (exit.at[2] - entry[2]) / chordLength];

        await driver.run();
        const samples: { p: Vec3; speed: number }[] = [];
        let previous = '';
        for (let i = 0; i < 160; i++) {
            const slot = await driver.readLatestPose();
            const body = slot?.bodies.find(b => b.id === String(BALL));
            if (slot && body && slot.sequence !== previous) {
                previous = slot.sequence;
                samples.push({ p: [body.px, body.py, body.pz], speed: Math.hypot(body.vx, body.vy, body.vz) });
            }
            await driver.page.waitForTimeout(20);
        }
        await driver.expectRunAdvanced(s0);

        const progress = (p: Vec3) => dot(sub(p, entry), chord);
        const entered = samples.findIndex(s => progress(s.p) > 0);
        const left = samples.findIndex(s => dot(sub(s.p, exit.at), exit.out) > 0.4);
        assert.ok(entered >= 0, `${label}: the ball never entered the first mouth (${samples.length} samples)`);
        assert.ok(left > entered, `${label}: the ball never left the far mouth; last at ${samples.at(-1)?.p.join(', ')}`);
        let furthest = progress(samples[entered].p);
        for (let i = entered; i <= left; i++) {
            assert.ok(samples[i].speed >= 0.05, `${label}: publication ${i} speed ${samples[i].speed} m/s at ${samples[i].p.join(', ')}`);
            const now = progress(samples[i].p);
            assert.ok(now >= furthest - 1e-3, `${label}: publication ${i} progress fell back from ${furthest} to ${now}`);
            furthest = Math.max(furthest, now);
        }
        console.log(`${label}: ${samples.length} publications; entered ${entered}, left ${left}; ` +
            `min speed ${Math.min(...samples.slice(entered, left + 1).map(s => s.speed)).toFixed(3)} m/s`);
        await driver.reset();
        await driver.expectResetMatches(s0);
        noErrors();
    }

    test('10. A ball crosses a snapped straight→45° joint continuously', { timeout: 150000 }, async () => {
        // Pipe vertical at y 5 (end mouth (0, 3.11) down); the bend (start up) at (-0.183, 2.102).
        await crossesJoint({ ballX: 0, ballLiftPx: 266, first: 'pipe', firstRaise: 4, second: 'pipe_bend_45', secondTilt: -18,
            secondHeight: -2, offerAt: [-0.2, 2, 0] }, 'straight→45°');
    });

    test('11. A ball crosses a snapped 45°→45° joint continuously', { timeout: 150000 }, async () => {
        // First bend (start up) at y 4, end mouth (-0.584, 3.158); the second bend turned -135° at (-1.426, 2.575).
        await crossesJoint({ ballX: 0.2, ballLiftPx: 174, first: 'pipe_bend_45', firstRaise: 2, second: 'pipe_bend_45', secondTilt: -27,
            secondHeight: -1, offerAt: [-1.4, 2.5, 0] }, '45°→45°');
    });

    test('12. A ball crosses a snapped straight→90° joint continuously', { timeout: 150000 }, async () => {
        // Pipe vertical at y 6 (end mouth (0, 4.11) down); the 90° bend (start up) at (-0.703, 2.323).
        await crossesJoint({ ballX: 0, ballLiftPx: 318, first: 'pipe', firstRaise: 6, second: 'pipe_bend_90', secondTilt: -18,
            secondHeight: -1, offerAt: [-0.7, 2.5, 0] }, 'straight→90°');
    });

    test('13. A ball crosses a snapped Funnel outlet→pipe joint continuously', { timeout: 150000 }, async () => {
        // Funnel inlet up at y 7 (outlet (0, 6.01) down); the pipe (start up) at (0, 4.12).
        await crossesJoint({ ballX: 0.3, ballLiftPx: 325, first: 'funnel', firstRaise: 8, second: 'pipe', secondTilt: -18,
            secondHeight: 2, offerAt: [0, 4, 0] }, 'Funnel outlet→pipe');
    });
});
