// Pure TypeScript Playwright-driven E2E acceptance test suite for slice CAT-014 (Story 6.1, rest restored by ENGINE-DRAG, Story 6.1b).
// Verifies the Bowling ball as a second dynamic sphere by material declaration, through the real Workshop UI:
// 1. Two lanes in one Run: identical stock Dominoes, a Basketball and a Bowling ball released identically (x = 0.3 m beside the tile,
//    both lowered by the same move-gizmo drag): the Bowling lane topples past 60 deg, the Basketball lane stays below 20 deg, and the
//    declared drag and rolling resistance bring both struck balls to rest (below 0.02 m/s from 6 s after Run, under 1 mm of movement
//    over the following second)
// 2. Rest/bounce: both balls dropped from the placement plane; the Bowling ball barely rebounds and rests at its 0.28 m radius;
//    Reset restores both worlds bit-for-bit: identity rotation and exact x/z, and the restored free fall reproduces the first Run's
//    committed py and velocity exactly
// 3. Receiver: a Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates
// 4. Self-contained Save / reload / Load: the two-lane construction is built and saved, the page reloads, Load restores both kinds
//    (behaviour, rest heights and each kind's declared rolling resistance: both balls rest again); after Reset the second Run topples the Bowling lane again
// The two-lane Chrome outcome uses the two declared materials as they ship, so mass and radius differ together (4 kg at the Basketball
// radius only rocks the tile in this drop geometry). The mass-only proof is the harness mass-isolation controls in
// tools/workshop-rigid-body.test.mjs (rolling strike: 4 kg at the Basketball radius topples, 1 kg at the Bowling radius does not).
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver, type AnimationSampleRecord, type WorkshopBodyPose } from './workshop-driver.ts';

const BENCH_Y = -0.46;
const HALF_HEIGHT = 0.55;
const BASKETBALL_R = 0.34, BOWLING_R = 0.28;   // the Bowling ball is smaller and heavier (owner decision 9 Oct 2026)
// Free-workshop camera (azimuth 0.6, elevation 0.48, orthographic 65.2 px/m): a point (x, 3, z) on the placement plane projects to
// screen (720 + 53.8 x - 36.8 z, 485 + 17.0 x + 24.8 z); vertical motion projects at 57.9 px/m.
const screen = (x: number, z: number) => ({ x: Math.round(720 + 53.8 * x - 36.8 * z), y: Math.round(485 + 17.0 * x + 24.8 * z) });
const LANE_Z = { basketball: -1.5, bowling: 1.5 };
const BALL_X = 0.3;                      // placement snaps to 0.1 m: the same UI offset for both balls (corner overlap 0.165 / 0.105 m)
const LOWER_TILE_PX = -162;              // 3 m plane -> tile just above the bench (as cat-023a)
const LOWER_BALL_PX = -113;              // 3 m plane -> ball centre ≈ 1.05 m, inside the harness band 0.8–1.4 m (6.1b sweep)
const BALL_CENTRE = { min: 0.98, max: 1.12 };  // release band; the Basketball's bottom then clears the tile top by ≈ 6 cm, the Bowling ball's by ≈ 12 cm
const GRAVITY = 9.81;
// With ≈ 6 cm of clearance the first strike lands about 110 ms after Run, possibly before the first pose-ring read, so first-read checks admit it.
const FREE_FALL_MS = 500;                // a ball dropped from the 3 m plane is in free fall for ≈ 0.79 s
const TILE_A = 1, TILE_B = 2, BASKETBALL = 3, BOWLING = 4;      // placement order
const RECEIVER_HALO_TARGET = '12884901890';                     // 3·2^32 + receiver body 2 (free play: ball 1, receiver 2)
const HALF_ONE = 15360;

function tiltDegrees(b: WorkshopBodyPose): number {
    const upY = 1 - 2 * (b.qx * b.qx + b.qz * b.qz);
    return Math.acos(Math.max(-1, Math.min(1, upY))) * 180 / Math.PI;
}
function speed(b: WorkshopBodyPose): number { return Math.hypot(b.vx, b.vy, b.vz); }

async function bodies(driver: WorkshopDriver): Promise<Map<number, WorkshopBodyPose>> {
    const pose = await driver.readLatestPose();
    assert.ok(pose, 'Pose slot must be readable');
    return new Map(pose.bodies.map(b => [b.id, b]));
}
async function waitFor(fn: () => Promise<boolean>, timeoutMs: number, stepMs = 50): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (await fn()) return true;
        await new Promise(resolve => setTimeout(resolve, stepMs));
    }
    return fn();
}
// Every committed state each body publishes during the window, polled as fast as the page allows. The pose ring carries no tick
// identity, so exact Reset is proven by state equality: a restored world replays the original world's committed states bit-for-bit.
async function recordStates(driver: WorkshopDriver, windowMs: number): Promise<Map<number, WorkshopBodyPose[]>> {
    const states = new Map<number, WorkshopBodyPose[]>();
    const start = Date.now();
    while (Date.now() - start < windowMs) {
        const pose = await driver.readLatestPose(5, 1);
        for (const b of pose?.bodies ?? []) {
            if (!states.has(b.id)) states.set(b.id, []);
            states.get(b.id)!.push(b);
        }
    }
    return states;
}
// Free fall is strictly monotonic in py, so py identifies the committed tick; the restored state at that py must equal the original
// in position, rotation and every velocity component exactly.
function assertReplaysExactly(original: WorkshopBodyPose[], restored: WorkshopBodyPose[], label: string): void {
    const byHeight = new Map(original.map(b => [b.py, b]));
    const matched = new Set<number>();
    for (const b of restored) {
        const o = byHeight.get(b.py);
        if (!o) continue;
        assert.deepEqual([b.px, b.py, b.pz, b.vx, b.vy, b.vz, b.qx, b.qy, b.qz, b.qw], [o.px, o.py, o.pz, o.vx, o.vy, o.vz, o.qx, o.qy, o.qz, o.qw],
            `${label}: the restored committed state at py ${b.py} equals the original bit-for-bit`);
        matched.add(b.py);
    }
    assert.ok(matched.size >= 3, `${label}: at least three restored free-fall states coincide with the original Run (${matched.size} of ${restored.length} reads)`);
}

// Place a Domino at the placement plane above lane z and lower it onto the bench with the real move gizmo (no setters, no numeric menus).
async function placeTileOnBench(driver: WorkshopDriver, z: number): Promise<void> {
    const at = screen(0, z);
    await driver.selectTool('domino');
    await driver.placeOnCanvas(at.x, at.y);
    await driver.selectPartAt(at.x, at.y);
    await driver.setPartMode('move', 1);          // the Domino's "Connect ActivationOut" row sits above the dock
    await driver.liftSelectedPart(LOWER_TILE_PX, at.x, at.y);
}
// Place a ball beside the tile and lower it by the same drag as the other lane, so both balls are released identically.
async function placeBallLowered(driver: WorkshopDriver, kind: 'basketball' | 'bowling', z: number): Promise<void> {
    const at = screen(BALL_X, z);
    await driver.selectTool(kind);
    await driver.placeOnCanvas(at.x, at.y);
    await driver.selectPartAt(at.x, at.y);
    await driver.setPartMode('move');
    await driver.liftSelectedPart(LOWER_BALL_PX, at.x, at.y);
}
async function buildTwoLanes(driver: WorkshopDriver): Promise<void> {
    await placeTileOnBench(driver, LANE_Z.basketball);
    await placeTileOnBench(driver, LANE_Z.bowling);
    await placeBallLowered(driver, 'basketball', LANE_Z.basketball);
    await placeBallLowered(driver, 'bowling', LANE_Z.bowling);
}
function assertTileInLane(b: WorkshopBodyPose, z: number, label: string): void {
    assert.ok(tiltDegrees(b) < 3, `${label}: standing (tilt ${tiltDegrees(b).toFixed(3)} deg; the first strike may already have landed)`);
    assert.ok(Math.abs(b.px) < 0.02 && Math.abs(b.pz - z) < 0.005, `${label}: at x=0, z=${z} (got ${b.px.toFixed(4)}, ${b.pz.toFixed(4)})`);
    assert.ok(b.py > BENCH_Y + HALF_HEIGHT - 0.01 && b.py < 0.3, `${label}: stands just above the bench (py=${b.py.toFixed(3)})`);
}
// A ball read in free fall reveals its release height through py + vy²/2g; one already in contact has shed energy, so that value is a lower bound.
function assertReleasedBall(b: WorkshopBodyPose, z: number, struck: boolean, label: string): void {
    assert.ok(Math.abs(b.px - BALL_X) < 0.05 && Math.abs(b.pz - z) < 0.005, `${label}: released at x=${BALL_X}, z=${z} (got ${b.px.toFixed(3)}, ${b.pz.toFixed(3)})`);
    assert.ok(b.py > BALL_CENTRE.min - 0.15 && b.py < BALL_CENTRE.max, `${label}: centre ${b.py.toFixed(3)} m at the first read`);
    const release = b.py + b.vy * b.vy / (2 * GRAVITY);
    assert.ok(release < BALL_CENTRE.max + 0.01, `${label}: release height ${release.toFixed(3)} m is below the band top`);
    if (!struck) assert.ok(release > BALL_CENTRE.min, `${label}: release height ${release.toFixed(3)} m is above the band bottom`);
}
const unstruck = (tile: WorkshopBodyPose) => tiltDegrees(tile) < 0.01;
// Watch both lanes for the strike window: the committed maximum tilt of each tile and the moment the Bowling lane passes 60 deg.
async function watchLanes(driver: WorkshopDriver, windowMs: number): Promise<{ maxA: number; maxB: number; toppledB: boolean }> {
    let maxA = 0, maxB = 0, toppledB = false;
    const start = Date.now();
    while (Date.now() - start < windowMs) {
        const all = await bodies(driver);
        maxA = Math.max(maxA, tiltDegrees(all.get(TILE_A)!));
        maxB = Math.max(maxB, tiltDegrees(all.get(TILE_B)!));
        if (maxB > 60) toppledB = true;
        await driver.page.waitForTimeout(40);
    }
    return { maxA, maxB, toppledB };
}
function assertLaneOutcome(result: { maxA: number; maxB: number; toppledB: boolean }, all: Map<number, WorkshopBodyPose>, label: string): void {
    assert.ok(result.toppledB, `${label}: the Bowling lane tile passes 60 deg (max ${result.maxB.toFixed(1)} deg)`);
    assert.ok(result.maxA < 20, `${label}: the Basketball lane tile stays below 20 deg (max ${result.maxA.toFixed(1)} deg)`);
    const tileA = all.get(TILE_A)!, tileB = all.get(TILE_B)!, basketball = all.get(BASKETBALL)!, bowling = all.get(BOWLING)!;
    assert.ok(tiltDegrees(tileA) < 2, `${label}: the Basketball lane tile stands again (${tiltDegrees(tileA).toFixed(2)} deg)`);
    assert.ok(Math.abs(tiltDegrees(tileB) - 90) < 5, `${label}: the Bowling lane tile lies flat (${tiltDegrees(tileB).toFixed(1)} deg)`);
    assert.ok(Math.abs(tileA.pz - LANE_Z.basketball) < 0.05 && Math.abs(tileB.pz - LANE_Z.bowling) < 0.05, `${label}: tiles stay in their lanes`);
    // The kinds tell themselves apart by their declared radius: each ball is on the bench at its own height with no vertical motion left.
    for (const [ball, radius, lane, name] of [[basketball, BASKETBALL_R, LANE_Z.basketball, 'Basketball'], [bowling, BOWLING_R, LANE_Z.bowling, 'Bowling ball']] as const) {
        assert.ok([ball.px, ball.py, ball.pz, ball.vx, ball.vy, ball.vz].every(Number.isFinite), `${label}: ${name} state is finite`);
        assert.ok(Math.abs(ball.py - (BENCH_Y + radius)) < 0.01, `${label}: ${name} is at its radius height (py ${ball.py.toFixed(4)} vs ${(BENCH_Y + radius).toFixed(4)})`);
        assert.ok(Math.abs(ball.vy) < 0.05, `${label}: ${name} has no vertical jitter (vy ${ball.vy.toFixed(4)})`);
        assert.ok(Math.abs(ball.pz - lane) < 0.05, `${label}: ${name} stays in its lane (pz ${ball.pz.toFixed(3)})`);
    }
}

// ENGINE-DRAG: from 6 s after Run both struck balls stay below 0.02 m/s and move less than 1 mm over the following second.
const REST_AFTER_MS = 6000, REST_WINDOW_MS = 1000, REST_SPEED = 0.02, REST_DRIFT = 0.001;
async function assertBallsRest(driver: WorkshopDriver, runStarted: number, label: string): Promise<void> {
    await driver.page.waitForTimeout(Math.max(0, REST_AFTER_MS - (Date.now() - runStarted)));
    const states = await recordStates(driver, REST_WINDOW_MS);
    for (const [id, name] of [[BASKETBALL, 'Basketball'], [BOWLING, 'Bowling ball']] as const) {
        const seen = states.get(id) ?? [];
        assert.ok(seen.length >= 10, `${label}: ${name} is sampled across the rest window (${seen.length} reads)`);
        const fastest = Math.max(...seen.map(speed));
        const drift = Math.max(...seen.map(b => Math.hypot(b.px - seen[0].px, b.py - seen[0].py, b.pz - seen[0].pz)));
        assert.ok(fastest < REST_SPEED, `${label}: ${name} stays below ${REST_SPEED} m/s from 6 s after Run (max ${fastest.toFixed(4)})`);
        assert.ok(drift < REST_DRIFT, `${label}: ${name} moves ${(drift * 1000).toFixed(3)} mm over the following second (no jitter)`);
    }
}

describe('CAT-014: Bowling ball dynamic sphere by material declaration', () => {
    let driver: WorkshopDriver;
    const errors: string[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch();
        driver.page.on('console', (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') ||
                text.includes('Unhandled exception') || text.includes('NaN')) errors.push(text);
        });
        driver.page.on('pageerror', (error: Error) => errors.push(error.message));
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('free_workshop');
        await buildTwoLanes(driver);

        await driver.toggleRun(0);
        const runStarted = Date.now();
        const first = await bodies(driver);
        assert.deepEqual([...first.keys()].sort((a, b) => a - b), [TILE_A, TILE_B, BASKETBALL, BOWLING], 'Two tiles and two balls are published');
        assertTileInLane(first.get(TILE_A)!, LANE_Z.basketball, 'Basketball lane tile');
        assertTileInLane(first.get(TILE_B)!, LANE_Z.bowling, 'Bowling lane tile');
        assertReleasedBall(first.get(BASKETBALL)!, LANE_Z.basketball, !unstruck(first.get(TILE_A)!), 'Basketball');
        assertReleasedBall(first.get(BOWLING)!, LANE_Z.bowling, !unstruck(first.get(TILE_B)!), 'Bowling ball');
        assert.ok(Math.abs(first.get(BASKETBALL)!.py - first.get(BOWLING)!.py) < 0.06, 'Both balls are released from the same height');

        const outcome = await watchLanes(driver, 4000);
        assertLaneOutcome(outcome, await bodies(driver), 'first Run');
        await assertBallsRest(driver, runStarted, 'first Run');
        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('free_workshop');
        const a = screen(0, LANE_Z.basketball), b = screen(0, LANE_Z.bowling);
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(a.x, a.y);
        await driver.selectTool('bowling');
        await driver.placeOnCanvas(b.x, b.y);

        await driver.toggleRun(0);
        const first = await bodies(driver);
        assert.deepEqual([...first.keys()].sort((a, b) => a - b), [1, 2], 'Both balls are published');
        assert.ok(first.get(1)!.py > 2 && first.get(2)!.py > 2, 'Both balls start high above the bench');
        const originalFall = await recordStates(driver, FREE_FALL_MS);
        const track = { 1: { landed: false, rebounded: false, apex: -Infinity }, 2: { landed: false, rebounded: false, apex: -Infinity } } as Record<number, { landed: boolean; rebounded: boolean; apex: number }>;
        const start = Date.now();
        while (Date.now() - start < 5000) {
            const all = await bodies(driver);
            for (const id of [1, 2]) {
                const ball = all.get(id)!, t = track[id];
                if (ball.vy < -1) t.landed = true;
                if (t.landed && ball.vy > 0.05) t.rebounded = true;
                if (t.rebounded) t.apex = Math.max(t.apex, ball.py);
            }
            await driver.page.waitForTimeout(30);
        }
        const final = await bodies(driver);
        const bowlingApex = track[2].apex - (BENCH_Y + BOWLING_R), basketballApex = track[1].apex - (BENCH_Y + BASKETBALL_R);
        assert.ok(track[2].landed, 'The Bowling ball reaches the bench');
        assert.ok(!track[2].rebounded || bowlingApex < 0.15, `The Bowling ball rebounds low (apex ${bowlingApex.toFixed(3)} m above rest)`);
        assert.ok(track[1].rebounded && basketballApex > 0.5, `The Basketball rebounds high (apex ${basketballApex.toFixed(3)} m above rest)`);
        const bowling = final.get(2)!, basketball = final.get(1)!;
        assert.ok(Math.abs(bowling.py - (BENCH_Y + BOWLING_R)) < 0.005, `The Bowling ball rests at its radius height (py ${bowling.py.toFixed(4)})`);
        assert.ok(speed(bowling) < 0.02, `The Bowling ball is at rest (speed ${speed(bowling).toFixed(4)})`);
        assert.ok(Math.abs(bowling.px) < 0.01 && Math.abs(bowling.pz - LANE_Z.bowling) < 0.01, 'The Bowling ball rests where it fell');
        assert.ok(Math.abs(basketball.py - (BENCH_Y + BASKETBALL_R)) < 0.01, `The Basketball ends on the bench at its own radius (py ${basketball.py.toFixed(4)})`);

        // Exact Reset for both kinds: identity rotation and admitted x/z, and the restored free fall reproduces the original committed
        // py and velocity bit-for-bit.
        await driver.toggleRun(800);                                                     // Reset
        await driver.toggleRun(0);                                                       // Run the restored construction
        const restored = await bodies(driver);
        const restoredFall = await recordStates(driver, FREE_FALL_MS);
        for (const [id, label] of [[1, 'reset Basketball'], [2, 'reset Bowling ball']] as const) {
            const ball = restored.get(id)!, original = first.get(id)!;
            assert.deepEqual([ball.qx, ball.qy, ball.qz, ball.qw], [0, 0, 0, 1], `${label}: identity orientation bit-for-bit`);
            assert.deepEqual([ball.px, ball.pz], [original.px, original.pz], `${label}: placed x/z exactly as the first Run`);
            assert.ok(ball.py > 2.5 && ball.vy <= 0, `${label}: falls again from the placement plane (py=${ball.py.toFixed(3)})`);
            assertReplaysExactly(originalFall.get(id) ?? [], restoredFall.get(id) ?? [], label);
        }
        assert.ok(await waitFor(async () => Math.abs((await bodies(driver)).get(2)!.py - (BENCH_Y + BOWLING_R)) < 0.005, 6000), 'The restored Bowling ball lands and rests again');
        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('free_workshop');
        await driver.selectTool('bowling');
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('receiver', 150);
        assert.equal(await driver.readLastAnimationSample(RECEIVER_HALO_TARGET), null, 'No capture sample before Run');

        await driver.toggleRun(0);
        const captured = await waitFor(async () => (await driver.readLastAnimationSample(RECEIVER_HALO_TARGET))?.valBits === HALF_ONE, 12000);
        assert.ok(captured, 'The Receiver capture target ramps to Half 1.0 once the Bowling ball settles in it');
        const ball = (await bodies(driver)).get(1)!;
        assert.ok(speed(ball) <= 1.5, `Captured below the 1.5 m/s capture speed (speed ${speed(ball).toFixed(3)})`);
        assert.ok(Math.abs(ball.px) < 0.7 && Math.abs(ball.pz) < 0.7 && ball.py > 2.5 && ball.py < 4, `The ball sits inside the Receiver (${ball.px.toFixed(2)}, ${ball.py.toFixed(2)}, ${ball.pz.toFixed(2)})`);
        const halo = await driver.readAnimationSamplesForTarget(RECEIVER_HALO_TARGET);
        for (let i = 1; i < halo.length; i++) assert.ok(halo[i].value >= halo[i - 1].value, 'The capture halo ramps once');
        assert.equal(await driver.readCaptured(), 0, 'Free workshop announces no goal');
        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });

    test('4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing', { timeout: 150000 }, async () => {
        // Self-contained: build and save the two-lane construction, then reload the page and Load it.
        await driver.reload();
        await driver.selectLevel('free_workshop');
        await buildTwoLanes(driver);
        await driver.save();
        await driver.reload();
        await driver.selectLevel('free_workshop');
        await driver.load();
        await driver.toggleRun(0);
        const loadedRunStarted = Date.now();
        const loaded = await bodies(driver);
        assert.deepEqual([...loaded.keys()].sort((a, b) => a - b), [TILE_A, TILE_B, BASKETBALL, BOWLING], 'Load restores two tiles and two balls');
        assertTileInLane(loaded.get(TILE_A)!, LANE_Z.basketball, 'loaded Basketball lane tile');
        assertTileInLane(loaded.get(TILE_B)!, LANE_Z.bowling, 'loaded Bowling lane tile');
        assertReleasedBall(loaded.get(BASKETBALL)!, LANE_Z.basketball, !unstruck(loaded.get(TILE_A)!), 'loaded Basketball');
        assertReleasedBall(loaded.get(BOWLING)!, LANE_Z.bowling, !unstruck(loaded.get(TILE_B)!), 'loaded Bowling ball');
        assertLaneOutcome(await watchLanes(driver, 4000), await bodies(driver), 'loaded Run');   // the kinds restored: rest heights 0.34 / 0.28
        await assertBallsRest(driver, loadedRunStarted, 'loaded Run');                     // the loaded kinds carry their declared rolling resistance

        await driver.toggleRun(800);                                                     // Reset
        await driver.toggleRun(0);                                                       // Run the restored construction
        const restored = await bodies(driver);
        // The toppled tile stands again and both balls are back beside their tiles (the strike may already have begun by this read, as above).
        assertTileInLane(restored.get(TILE_A)!, LANE_Z.basketball, 'reset Basketball lane tile');
        assertTileInLane(restored.get(TILE_B)!, LANE_Z.bowling, 'reset Bowling lane tile');
        assertReleasedBall(restored.get(BASKETBALL)!, LANE_Z.basketball, !unstruck(restored.get(TILE_A)!), 'reset Basketball');
        assertReleasedBall(restored.get(BOWLING)!, LANE_Z.bowling, !unstruck(restored.get(TILE_B)!), 'reset Bowling ball');
        assertLaneOutcome(await watchLanes(driver, 4000), await bodies(driver), 'second Run');
        await driver.toggleRun(500);
        assert.equal(errors.length, 0, `Zero errors expected, got: ${errors.join('; ')}`);
    });
});
