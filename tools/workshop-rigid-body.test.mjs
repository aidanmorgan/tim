// Supplemental control of the shipped simulation worker's rigid-body kernels: real admission records (body, collider,
// material tables as PhysicsGpuAbi lays them out) are stepped through the module's own stage/commit path in Node.
// No gameplay outcome is fabricated; the facts below are the physical behaviours the Chrome suites then observe.
import assert from 'node:assert/strict';
import test from 'node:test';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort } from 'node:worker_threads';

const source = await readFile(process.env.WORKER_SOURCE ?? 'CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');

const STATE_BYTES = 150832;        // PhysicsGpuAbi.ByteLength
const BODIES = 128, COLLIDERS = 4352, MATERIALS = 10496;
const ORIENTATION_SENSORS = 19744; // PhysicsGpuAbi.OrientationSensorsOffset (64-byte records, count at header byte 116)
const BENCH_Y = -0.46;
const TICKS_PER_SECOND = 120;      // cadence 2 => 4 substeps of 1/480 s per tick
// Declared ball data as BallMaterial.For compiles it (parts/catalog/ball.tres, bowling.tres): linear drag 0.04 1/s on the body
// record and the rolling-resistance coefficient on the material record.
const BALL_DRAG = .04, BASKETBALL_ROLLING = .035, BOWLING_ROLLING = .03;
const BASKETBALL_R = .34, BOWLING_R = .28;   // BallMaterial.For radii: the Bowling ball is smaller and heavier (owner decision 9 Oct 2026)

async function loadWorker() {
    let imports;
    const host = {
        CommandAbi: () => [72, 5768], ResponseAbi: () => [23952, 40, 56], OperationAbi: () => [0, 1],
        StateBytes: () => STATE_BYTES, ScheduleRoles: () => [1, 2], CaptureMode: () => 1
    };
    const runtime = {
        setModuleImports: (_, methods) => { imports = methods; },
        getAssemblyExports: async () => ({ Program: host }),
        getConfig: () => ({ mainAssemblyName: 'RigidBodyControl' })
    };
    const self = { onmessage: undefined, postMessage: () => {}, close: () => {} };
    const context = vm.createContext({ self, Uint8Array, Float32Array, Uint32Array, BigInt64Array, Float64Array, ArrayBuffer,
        SharedArrayBuffer, DataView, Atomics, Map, Math, Number, Object, MessagePort, console, setTimeout, clearTimeout, Promise, Error });
    const dotnet = new vm.SyntheticModule(['dotnet'], function () { this.setExport('dotnet', { create: async () => runtime }); }, { context });
    const clock = new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
        this.setExport('admitNativeClock', async () => {});
        this.setExport('nativeNow', () => 1);
        this.setExport('nativeClockEvidence', () => [2, 1, 1, 1, .005, 1, 1]);
    }, { context });
    const module = new vm.SourceTextModule(source, { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return dotnet;
        if (name === '../native-clock.js') return clock;
        throw Error('Unexpected worker import');
    });
    await module.evaluate();
    await imports.initialize(new Uint8Array(0));
    return imports;
}

// Round to nearest even like .NET (Half) so the admission records carry the same bits the compiler would write.
function halfBits(value) {
    if (Number.isNaN(value)) return 0x7e00;
    const sign = (value < 0 || Object.is(value, -0)) ? 0x8000 : 0;
    const magnitude = Math.abs(value);
    if (magnitude === 0) return sign;
    if (!Number.isFinite(magnitude)) return sign | 0x7c00;
    const nearestEven = scaled => { const f = Math.floor(scaled); const d = scaled - f; return d > 0.5 ? f + 1 : d < 0.5 ? f : (f % 2 === 0 ? f : f + 1); };
    if (magnitude < Math.pow(2, -14)) return sign | nearestEven(magnitude / Math.pow(2, -24));
    let exponent = Math.floor(Math.log2(magnitude));
    if (magnitude / Math.pow(2, exponent) >= 2) exponent++;
    if (magnitude / Math.pow(2, exponent) < 1) exponent--;
    let q = nearestEven(magnitude / Math.pow(2, exponent - 10));
    if (q >= 2048) { q = 1024; exponent++; }
    if (exponent > 15) return sign | 0x7c00;
    return sign | ((exponent + 15) << 10) | (q - 1024);
}
function halfValue(u16) {
    const sign = (u16 & 0x8000) ? -1 : 1; const exp = (u16 >> 10) & 0x1f; const frac = u16 & 0x3ff;
    if (exp === 0) return sign * (frac / 1024) * 0.00006103515625;
    if (exp === 31) return frac === 0 ? sign * Infinity : NaN;
    return sign * (1 + frac / 1024) * Math.pow(2, exp - 15);
}
const H = (view, offset, value) => view.setUint16(offset, halfBits(value), true);
const RH = (view, offset) => halfValue(view.getUint16(offset, true));
function position(metres) {
    const cells = metres * 16; let cell = Math.floor(cells + 0.5); let local = cells - cell;
    if (local === 0.5) { cell++; local = -0.5; }
    return [cell, local];
}
// Mantissa in [0.5, 1) times 2^exponent, as RigidMassProperties compiles principal moments (zero stays zero).
function principal(view, offset, value) {
    if (!(value > 0)) { H(view, offset, 0); view.setInt32(offset + 4, 0, true); return; }
    let exponent = 0; let mantissa = value;
    while (mantissa < 0.5) { mantissa *= 2; exponent--; }
    while (mantissa >= 1) { mantissa *= 0.5; exponent++; }
    H(view, offset, mantissa); view.setInt32(offset + 4, exponent, true);
}

// Scene builder mirroring PhysicsGpuAbi.Admission for bodies, colliders, materials and orientation sensors only. Declared linear
// drag sits at body record +66 and rolling resistance at material record +14; both default to zero like a static declaration.
function scene(spec, sensors = [], cadence = 2) {
    const bytes = new Uint8Array(STATE_BYTES); const view = new DataView(bytes.buffer);
    view.setUint32(0, 8, true);
    view.setBigUint64(32, 1n, true); view.setUint32(48, cadence, true); view.setUint32(52, 1, true);
    view.setBigUint64(56, 1n, true); view.setBigUint64(64, 1n, true); view.setBigUint64(72, 2n, true); view.setBigUint64(80, 4096n, true);
    const bodies = [{ id: 1, motion: 0, position: [0, BENCH_Y, 0], shape: 2, material: { restitution: 1, threshold: .1, friction: .3 } }, ...spec];
    view.setUint32(12, bodies.length, true); view.setUint32(16, bodies.length, true); view.setUint32(20, bodies.length, true);
    let dynamicCount = 0;
    bodies.forEach((b, slot) => {
        const r = BODIES + slot * 128;
        view.setBigUint64(r, BigInt(b.id), true); view.setUint32(r + 8, b.motion, true);
        const [cx, lx] = position(b.position[0]); const [cy, ly] = position(b.position[1]); const [cz, lz] = position(b.position[2]);
        view.setInt32(r + 16, cx, true); view.setInt32(r + 20, cy, true); view.setInt32(r + 24, cz, true);
        H(view, r + 32, lx); H(view, r + 34, ly); H(view, r + 36, lz);
        const q = b.rotation ?? [0, 0, 0, 1];
        H(view, r + 40, q[0]); H(view, r + 42, q[1]); H(view, r + 44, q[2]); H(view, r + 46, q[3]);
        const c = COLLIDERS + slot * 96;
        view.setBigUint64(c, BigInt(100 + slot), true); view.setUint32(c + 8, slot, true); view.setUint32(c + 12, slot, true);
        view.setUint32(c + 16, b.shape, true); H(view, c + 38, 1);
        if (b.shape === 0) H(view, c + 40, b.radius);
        if (b.shape === 1) { H(view, c + 42, b.half[0]); H(view, c + 44, b.half[1]); H(view, c + 46, b.half[2]); }
        const m = MATERIALS + slot * 32;
        view.setBigUint64(m, BigInt(200 + slot), true);
        H(view, m + 8, b.material.restitution); H(view, m + 10, b.material.threshold); H(view, m + 12, b.material.friction);
        H(view, m + 14, b.material.rolling ?? 0);
        if (b.motion !== 1) return;
        dynamicCount++;
        const v = b.velocity ?? [0, 0, 0]; const w = b.angular ?? [0, 0, 0];
        H(view, r + 48, v[0] / 32); H(view, r + 50, v[1] / 32); H(view, r + 52, v[2] / 32);
        H(view, r + 56, w[0]); H(view, r + 58, w[1]); H(view, r + 60, w[2]);
        H(view, r + 64, b.mass); H(view, r + 66, b.drag ?? 0); H(view, r + 70, b.gravity ?? -9.81);
        H(view, r + 94, 1);
        let moments;
        if (b.moments) moments = b.moments;
        else if (b.shape === 0) { const i = .4 * b.mass * b.radius * b.radius; moments = [i, i, i]; }
        else {
            const [a, h, d] = b.half;
            moments = [b.mass * (h * h + d * d) / 3, b.mass * (a * a + d * d) / 3, b.mass * (a * a + h * h) / 3];
        }
        principal(view, r + 96, moments[0]); principal(view, r + 104, moments[1]); principal(view, r + 112, moments[2]);
        view.setUint32(r + 120, slot, true);
    });
    view.setUint32(28, dynamicCount, true);
    // Declared orientation sensors: identity, sensed body slot, admitted initial rotation and the cosine of half the threshold angle.
    sensors.forEach((sensor, i) => {
        const o = ORIENTATION_SENSORS + i * 64;
        view.setBigUint64(o, BigInt(sensor.id), true);
        view.setUint32(o + 8, bodies.findIndex(b => b.id === sensor.body), true);
        const q = sensor.initial ?? [0, 0, 0, 1];
        H(view, o + 16, q[0]); H(view, o + 18, q[1]); H(view, o + 20, q[2]); H(view, o + 22, q[3]);
        H(view, o + 24, Math.cos((sensor.degrees ?? 45) * Math.PI / 360));
    });
    view.setUint32(116, sensors.length, true);
    return bytes;
}

function readSensor(bytes, slot) {
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength); const o = ORIENTATION_SENSORS + slot * 64;
    const declaration = Array.from(new Uint8Array(bytes.buffer, bytes.byteOffset + o, 32));
    return { declaration, fired: view.getUint32(o + 32, true), ordinal: view.getUint32(o + 36, true), phase: RH(view, o + 40),
        padding: new Uint8Array(bytes.buffer, bytes.byteOffset + o + 42, 22).every(b => b === 0) };
}

function readBody(bytes, slot) {
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength); const r = BODIES + slot * 128;
    const p = [(view.getInt32(r + 16, true) + RH(view, r + 32)) / 16, (view.getInt32(r + 20, true) + RH(view, r + 34)) / 16, (view.getInt32(r + 24, true) + RH(view, r + 36)) / 16];
    const q = [RH(view, r + 40), RH(view, r + 42), RH(view, r + 44), RH(view, r + 46)];
    const v = [RH(view, r + 48) * 32, RH(view, r + 50) * 32, RH(view, r + 52) * 32];
    const w = [RH(view, r + 56), RH(view, r + 58), RH(view, r + 60)];
    // Tilt of the body's local +Y axis from world up, in degrees.
    const upY = 1 - 2 * (q[0] * q[0] + q[2] * q[2]);
    return { p, q, v, w, tilt: Math.acos(Math.max(-1, Math.min(1, upY))) * 180 / Math.PI, speed: Math.hypot(...v), spin: Math.hypot(...w) };
}

// Every committed cell remainder must be a canonical Half in [-0.5, 0.5); the host rejects a remainder that rounds to 0.5.
function assertCanonicalRemainders(bytes, tick) {
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const bodyCount = view.getUint32(12, true);
    for (let slot = 0; slot < bodyCount; slot++) {
        const r = BODIES + slot * 128;
        if (view.getUint32(r + 8, true) !== 1) continue;
        for (const offset of [32, 34, 36]) {
            const local = RH(view, r + offset);
            assert.ok(local >= -0.5 && local < 0.5, `tick ${tick} body slot ${slot} remainder ${local} at +${offset} is not canonical`);
        }
    }
}

const tickTimes = [];
async function run(bytes, ticks, observe) {
    const imports = await loadWorker();
    await imports.stage(bytes, 0); imports.commit();
    let latest = null;
    tickTimes.length = 0;
    for (let tick = 1; tick <= ticks; tick++) {
        const started = performance.now();
        await imports.stage(new Uint8Array(0), 1);
        tickTimes.push(performance.now() - started);
        latest = imports.read(); imports.commit();
        assertCanonicalRemainders(latest, tick);
        if (observe) observe(tick, latest);
    }
    return latest;
}

const domino = (position, extra = {}) => ({ id: 2, motion: 1, position, shape: 1, half: [.125, .55, .325], mass: .4,
    material: { restitution: .05, threshold: .1, friction: .6 }, ...extra });
const basketball = (position, extra = {}) => ({ id: 3, motion: 1, position, shape: 0, radius: .34, mass: 1,
    drag: BALL_DRAG, material: { restitution: .55, threshold: .1, friction: .3, rolling: BASKETBALL_ROLLING }, ...extra });
// The Bowling ball (CAT-014) is the same sphere kernel reading another declared material: 4 kg, 0.28 m, bounce .14.
const bowlingBall = (position, extra = {}) => ({ id: 4, motion: 1, position, shape: 0, radius: BOWLING_R, mass: 4,
    drag: BALL_DRAG, material: { restitution: .14, threshold: .1, friction: .3, rolling: BOWLING_ROLLING }, ...extra });
// The same ball with zero declared drag and rolling resistance: the control for facts about ideal contact (rolling without slipping,
// declared moments, momentum transfer at a known impact speed) that deceleration would otherwise blur.
const resistanceFree = ball => ({ ...ball, drag: 0, material: { ...ball.material, rolling: 0 } });
// Pure rolling on the bench along +X: omega_z = -v / r.
const rollingAt = (ball, speed) => ({ ...ball, velocity: [speed, 0, 0], angular: [0, 0, -speed / ball.radius] });
const TICKS_AT = { 1: 60, 2: 120, 3: 240 };   // header cadence -> committed ticks per second (8, 4, 2 substeps of 1/480 s)

test('rigid-body: an upright box set 1 cm above the bench settles on its face and stands within the rest tolerances', async () => {
    const start = [0, BENCH_Y + .55 + .01, 0];
    const final = readBody(await run(scene([domino(start)]), 3 * TICKS_PER_SECOND), 1);
    assert.ok(final.tilt < 2, `tilt ${final.tilt.toFixed(3)} deg`);
    assert.ok(Math.hypot(final.p[0], final.p[2]) < .005, `drift ${Math.hypot(final.p[0], final.p[2]).toFixed(4)} m`);
    assert.ok(final.speed < .05, `speed ${final.speed.toFixed(4)} m/s`);
    assert.ok(Math.abs(final.p[1] - (BENCH_Y + .55)) < .0005, `height ${final.p[1].toFixed(5)} vs ${(BENCH_Y + .55).toFixed(5)}`);
    assert.ok(final.spin < .05, `spin ${final.spin.toFixed(4)} rad/s`);
});

test('rigid-body: a box tipped past its support topples through 60 deg and settles flat without jitter', async () => {
    const start = [0, BENCH_Y + .55 + .0005, 0];
    const tilts = [];
    const samples = [];
    const final = readBody(await run(scene([domino(start, { angular: [0, 0, -4] })]), 4 * TICKS_PER_SECOND,
        (tick, bytes) => { const b = readBody(bytes, 1); tilts.push(b.tilt); if (tick > 3 * TICKS_PER_SECOND) samples.push(b); }), 1);
    const passed = tilts.findIndex(t => t > 60);
    assert.ok(passed >= 0 && passed < 2 * TICKS_PER_SECOND, `passed 60 deg at tick ${passed}`);
    assert.ok(Math.abs(final.tilt - 90) < 3, `final tilt ${final.tilt.toFixed(2)} deg`);
    assert.ok(final.speed < .05 && final.spin < .1, `rest speed ${final.speed.toFixed(4)} spin ${final.spin.toFixed(4)}`);
    assert.ok(Math.abs(final.p[1] - (BENCH_Y + .125)) < .002, `lying height ${final.p[1].toFixed(4)}`);
    let maxDq = 0;
    for (let i = 1; i < samples.length; i++)
        maxDq = Math.max(maxDq, Math.hypot(...samples[i].q.map((c, k) => c - samples[i - 1].q[k])));
    assert.ok(maxDq < .002, `frame-to-frame quaternion change ${maxDq.toExponential(2)} over the final second`);
});

test('rigid-body: a sphere dropped onto the bench bounces dissipatively and comes to rest at its radius', async () => {
    let rebounded = false, peakAfterBounce = 0, landed = false;
    const final = readBody(await run(scene([basketball([0, 3, 0])]), 6 * TICKS_PER_SECOND, (tick, bytes) => {
        const b = readBody(bytes, 1);
        if (b.v[1] < -1) landed = true;
        if (landed && b.v[1] > .5) rebounded = true;
        if (rebounded) peakAfterBounce = Math.max(peakAfterBounce, b.p[1]);
    }), 1);
    assert.ok(rebounded, 'the ball rebounds');
    assert.ok(peakAfterBounce < 1.5, `first rebound apex ${peakAfterBounce.toFixed(3)} m stays below the drop height`);
    assert.ok(Math.abs(final.p[1] - (BENCH_Y + .34)) < .001, `rest height ${final.p[1].toFixed(4)}`);
    assert.ok(final.speed < .02, `rest speed ${final.speed.toFixed(4)}`);
});

test('rigid-body: friction on the bench turns a sliding sphere into a rolling one through the declared inertia', async () => {
    const final = readBody(await run(scene([resistanceFree(basketball([0, BENCH_Y + .34, 0], { velocity: [3, 0, 0] }))]), TICKS_PER_SECOND), 1);
    assert.ok(final.v[0] > 1.5 && final.v[0] < 3, `forward speed ${final.v[0].toFixed(3)} m/s`);
    // Rolling without slipping on +X about -Z: omega_z = -v/r.
    assert.ok(Math.abs(final.w[2] + final.v[0] / .34) < .3, `omega ${final.w[2].toFixed(3)} vs ${(-final.v[0] / .34).toFixed(3)}`);
});

test('rigid-body: a box resting on a static box face holds through the box-box manifold without sinking or launching', async () => {
    const wall = { id: 4, motion: 0, position: [0, BENCH_Y + 1, 0], shape: 1, half: [1.5, 1, .125], material: { restitution: 1, threshold: .1, friction: .3 } };
    const top = BENCH_Y + 2;
    let maxY = -Infinity;
    const final = readBody(await run(scene([domino([0, top + .55 + .005, 0]), wall]), 2 * TICKS_PER_SECOND,
        (tick, bytes) => { maxY = Math.max(maxY, readBody(bytes, 1).p[1]); }), 1);
    assert.ok(final.tilt < 2, `tilt ${final.tilt.toFixed(3)} deg`);
    assert.ok(Math.abs(final.p[1] - (top + .55)) < .001, `height ${final.p[1].toFixed(4)} vs ${(top + .55).toFixed(4)}`);
    assert.ok(maxY < top + .55 + .02, `never launched (max y ${maxY.toFixed(4)})`);
    assert.ok(final.speed < .05, `speed ${final.speed.toFixed(4)}`);
});

test('rigid-body: a box resting across the edge of a tilted static box balances on the genuine edge-edge crossing', async () => {
    // Bottom edge (along Z) of the dynamic box crosses the top edge (along X) of a static box tilted 45 deg about X. The contact point
    // is the crossing under the upper box's centre, so it balances; a vertex-midpoint support contact spins it off and sinks it.
    const rotX = deg => [Math.sin(deg * Math.PI / 360), 0, 0, Math.cos(deg * Math.PI / 360)];
    const rotZ = deg => [0, 0, Math.sin(deg * Math.PI / 360), Math.cos(deg * Math.PI / 360)];
    const hs = .2, hb = .1, topEdge = BENCH_Y + 1 + hs * Math.SQRT2, rest = topEdge + hb * Math.SQRT2;
    const beam = { id: 4, motion: 0, position: [0, BENCH_Y + 1, 0], rotation: rotX(45), shape: 1, half: [1.5, hs, hs], material: { restitution: 1, threshold: .1, friction: .3 } };
    const upper = { id: 2, motion: 1, position: [0, rest + .002, 0], rotation: rotZ(45), shape: 1, half: [hb, hb, .8], mass: .4,
        material: { restitution: .05, threshold: .1, friction: .6 } };
    let maxSpin = 0;
    const final = readBody(await run(scene([upper, beam]), TICKS_PER_SECOND / 2, (tick, bytes) => { maxSpin = Math.max(maxSpin, readBody(bytes, 1).spin); }), 1);
    assert.ok(maxSpin < .05, `the balanced box never spins up (max ${maxSpin.toFixed(3)} rad/s)`);
    assert.ok(Math.hypot(final.p[0], final.p[2]) < .002, `drift ${Math.hypot(final.p[0], final.p[2]).toFixed(4)} m`);
    assert.ok(Math.abs(final.p[1] - rest) < .003, `rests on the crossing (y ${final.p[1].toFixed(4)} vs ${rest.toFixed(4)})`);
});

test('rigid-body: a box resting across an off-centre point of a tilted beam edge still balances on the crossing', async () => {
    // The crossing lies 0.7 m along the beam edge: an endpoint-midpoint support point would sit 0.7 m from the real support.
    const rotX = deg => [Math.sin(deg * Math.PI / 360), 0, 0, Math.cos(deg * Math.PI / 360)];
    const rotZ = deg => [0, 0, Math.sin(deg * Math.PI / 360), Math.cos(deg * Math.PI / 360)];
    const hs = .2, hb = .1, topEdge = BENCH_Y + 1 + hs * Math.SQRT2, rest = topEdge + hb * Math.SQRT2, X = .7;
    const beam = { id: 4, motion: 0, position: [0, BENCH_Y + 1, 0], rotation: rotX(45), shape: 1, half: [1.5, hs, hs], material: { restitution: 1, threshold: .1, friction: .3 } };
    const upper = { id: 2, motion: 1, position: [X, rest + .002, 0], rotation: rotZ(45), shape: 1, half: [hb, hb, .8], mass: .4,
        material: { restitution: .05, threshold: .1, friction: .6 } };
    let maxSpin = 0;
    const final = readBody(await run(scene([upper, beam]), TICKS_PER_SECOND / 2, (tick, bytes) => { maxSpin = Math.max(maxSpin, readBody(bytes, 1).spin); }), 1);
    assert.ok(maxSpin < .05, `the balanced box never spins up (max ${maxSpin.toFixed(3)} rad/s)`);
    assert.ok(Math.hypot(final.p[0] - X, final.p[2]) < .002, `drift ${Math.hypot(final.p[0] - X, final.p[2]).toFixed(4)} m`);
    assert.ok(Math.abs(final.p[1] - rest) < .003, `rests on the crossing (y ${final.p[1].toFixed(4)} vs ${rest.toFixed(4)})`);
});

test('rigid-body: a sphere dropped onto a standing box topples it past 60 deg and every tick stays inside the real-time budget', async () => {
    const start = [0, BENCH_Y + .55 + .0005, 0];
    const tilts = [];
    const final = readBody(await run(scene([domino(start), basketball([.4, 3, 0])]), 3 * TICKS_PER_SECOND,
        (tick, bytes) => { tilts.push(readBody(bytes, 1).tilt); }), 1);
    const passed = tilts.findIndex(t => t > 60);
    assert.ok(passed >= 0 && passed < 2 * TICKS_PER_SECOND, `passed 60 deg at tick ${passed}`);
    assert.ok(Math.abs(final.tilt - 90) < 5, `final tilt ${final.tilt.toFixed(2)} deg`);
    const slowest = Math.max(...tickTimes);
    assert.ok(slowest < 8, `slowest tick ${slowest.toFixed(2)} ms must fit the 120 Hz budget`);
});

test('rigid-body: speeds beyond the game-grade envelope clamp to 64 m/s and 128 rad/s and the tick continues', async () => {
    // |v| = 72.1 m/s and |omega| = 141.4 rad/s start outside both clamps.
    const b = readBody(await run(scene([domino([0, 20, 0], { velocity: [0, -60, 40], angular: [100, 100, 0] })]), 1), 1);
    assert.ok(b.speed <= 64.01 && b.speed > 60, `speed ${b.speed.toFixed(2)} clamped to the envelope`);
    assert.ok(b.spin <= 128.01 && b.spin > 120, `spin ${b.spin.toFixed(2)} clamped to the envelope`);
    assert.ok([...b.p, ...b.q, ...b.v, ...b.w].every(Number.isFinite), 'finite state');
});

test('rigid-body: zero restitution never rebounds and zero friction never spins up (declared values, no fallback)', async () => {
    const dead = basketball([0, 2, 0], { material: { restitution: 0, threshold: .1, friction: .3 } });
    let landed = false, rebound = 0;
    await run(scene([dead]), 2 * TICKS_PER_SECOND, (tick, bytes) => {
        const b = readBody(bytes, 1);
        if (b.v[1] < -1) landed = true;
        if (landed) rebound = Math.max(rebound, b.v[1]);
    });
    assert.ok(landed, 'the ball reaches the bench');
    assert.ok(rebound < 0.05, `restitution 0 gives no rebound (max upward ${rebound.toFixed(3)} m/s)`);
    const slick = resistanceFree(basketball([0, BENCH_Y + .34, 0], { velocity: [3, 0, 0], material: { restitution: .55, threshold: .1, friction: 0 } }));
    const final = readBody(await run(scene([slick]), TICKS_PER_SECOND), 1);
    assert.ok(final.spin < 0.05, `friction 0 (geometric mean with the bench) never spins the ball (${final.spin.toFixed(3)} rad/s)`);
    assert.ok(final.v[0] > 2.9, `and never slows it (${final.v[0].toFixed(3)} m/s)`);
});

test('rigid-body: rolling speed follows the declared principal moments, not a shape formula (solid vs hollow sphere)', async () => {
    // Sliding at 3 m/s and settling into pure rolling: v = v0 / (1 + I / (m r^2)) -> 2.14 m/s solid (I = 2/5), 1.80 m/s hollow (I = 2/3).
    const hollow = (2 / 3) * 1 * .34 * .34;
    const solid = readBody(await run(scene([resistanceFree(basketball([0, BENCH_Y + .34, 0], { velocity: [3, 0, 0] }))]), TICKS_PER_SECOND), 1);
    const shell = readBody(await run(scene([resistanceFree(basketball([0, BENCH_Y + .34, 0], { velocity: [3, 0, 0], moments: [hollow, hollow, hollow] }))]), TICKS_PER_SECOND), 1);
    assert.ok(Math.abs(solid.v[0] - 2.143) < 0.08, `solid sphere rolls at ${solid.v[0].toFixed(3)} m/s`);
    assert.ok(Math.abs(shell.v[0] - 1.8) < 0.08, `hollow sphere (record moments) rolls at ${shell.v[0].toFixed(3)} m/s`);
});

test('rigid-body: a remainder that rounds to Half 0.5 carries into the next cell instead of committing 0.5', async () => {
    // From rest at cell 49 / local -2020/4096 under g = -9.875 one tick moves -0.0068576 cells: the double remainder after the
    // integer carry is 0.4999784, which rounds to Half 0.5 unless the carry uses the rounded value.
    const y0 = (49 - 2020 / 4096) / 16;
    const bytes = await run(scene([resistanceFree(basketball([0, y0, 0], { gravity: -9.875 }))]), 1);
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength); const r = BODIES + 128;
    const cell = view.getInt32(r + 20, true), local = RH(view, r + 34);
    assert.ok(local >= -0.5 && local < 0.5, `committed remainder ${local} is canonical`);
    const expected = y0 - 9.875 * (1 / 480) * (1 / 480) * 10;
    assert.ok(Math.abs((cell + local) / 16 - expected) < 1e-4, `height ${(cell + local) / 16} matches the fall to ${expected.toFixed(6)}`);
});

test('rigid-body: a zero declared moment reads as infinite inertia and the body stays finite', async () => {
    const final = readBody(await run(scene([basketball([0, 1, 0], { velocity: [2, 0, 0], moments: [0, 0, 0] })]), 2 * TICKS_PER_SECOND), 1);
    assert.ok([...final.p, ...final.q, ...final.v, ...final.w].every(Number.isFinite), 'finite state');
    assert.equal(final.spin, 0, 'no rotation without a finite moment');
    assert.ok(Math.abs(final.p[1] - (BENCH_Y + .34)) < .002, `rests on the bench (py ${final.p[1].toFixed(4)})`);
});

test('rigid-body: the worker source names no element, hard-coded inertia or material fallback', () => {
    assert.doesNotMatch(source, /\b(domino|basketball|bowling|bumper|ramp|wall|switch|lamp|receiver)\b/i);
    // Ball constants (Bowling radius 0.28, bounce 0.14, rolling resistance 0.03 / 0.035) must arrive as declared data, never as literals in the worker.
    assert.doesNotMatch(source, /3\.5 \* |2\.5 \/ |\|\| 0\.34|\|\| 0\.75|\|\| 0\.1\b|\|\| 0\.3\b|\|\| 1\.0\b|\|\| 8\.0|(?<![\d.])0?\.(28|38|14|035|03)(?!\d)/);
});

test('rigid-body: the motion description is written at the offset the host declares after the orientation sensor table', async () => {
    const bytes = await run(scene([basketball([0, 3, 0])]), 1);
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    assert.equal(view.getUint32(20768 + 4, true), 4, 'four substeps recorded at PhysicsGpuAbi.MotionOffset');
    assert.equal(view.getUint32(20768 + 12, true), 4, 'next ordinal recorded at PhysicsGpuAbi.MotionOffset + 12');
    assert.equal(view.getUint32(20768, true), 4, 'one dynamic body contributes four motion pieces');
});

// Orientation-threshold sensors: declared (body, admitted pose, cos half-angle), evaluated at substep endpoints, sticky once per world.
const tiltedTile = degrees => domino([0, BENCH_Y + .55 * Math.cos(degrees * Math.PI / 180) + .125 * Math.sin(degrees * Math.PI / 180) + .005, 0],
    { rotation: [0, 0, Math.sin(degrees * Math.PI / 360), Math.cos(degrees * Math.PI / 360)] });
const sensorOn = (body, initial, degrees = 45) => ({ id: 500, body, initial, degrees });

test('orientation sensor: a tile placed 10 deg off upright rocks back and never fires', async () => {
    const tile = tiltedTile(10);
    const sensors = [sensorOn(2, tile.rotation)];
    let everFired = false;
    const final = await run(scene([tile], sensors), 2 * TICKS_PER_SECOND, (tick, bytes) => { if (readSensor(bytes, 0).fired) everFired = true; });
    const s = readSensor(final, 0);
    assert.equal(everFired, false, 'never fired');
    assert.deepEqual([s.fired, s.ordinal, s.phase, s.padding], [0, 0, 0, true], 'armed state holds no event');
    assert.deepEqual(s.declaration, readSensor(scene([tile], sensors), 0).declaration, 'declaration bytes untouched');
    assert.ok(readBody(final, 1).tilt < 2, `settles upright (tilt ${readBody(final, 1).tilt.toFixed(2)} deg)`);
});

test('orientation sensor: a tile admitted rotated 90 deg about Y stands for 2 s and never fires (initial pose is the admitted pose)', async () => {
    const turned = domino([0, BENCH_Y + .55 + .005, 0], { rotation: [0, Math.SQRT1_2, 0, Math.SQRT1_2] });
    let everFired = false;
    const final = await run(scene([turned], [sensorOn(2, turned.rotation)]), 2 * TICKS_PER_SECOND, (tick, bytes) => { if (readSensor(bytes, 0).fired) everFired = true; });
    assert.equal(everFired, false, 'never fired');
    const b = readBody(final, 1);
    assert.ok(b.tilt < 2 && Math.abs(b.q[1]) > .7, `still standing and still turned about Y (tilt ${b.tilt.toFixed(2)}, qy ${b.q[1].toFixed(3)})`);
});

test('orientation sensor: a toppling tile fires once when it passes 45 deg and stays fired at that ordinal', async () => {
    const tile = domino([0, BENCH_Y + .55 + .0005, 0], { angular: [0, 0, -4] });
    let firedAt = null, crossed = null;
    const final = await run(scene([tile], [sensorOn(2, [0, 0, 0, 1])]), 4 * TICKS_PER_SECOND, (tick, bytes) => {
        const b = readBody(bytes, 1), s = readSensor(bytes, 0);
        if (crossed === null && b.tilt >= 45) crossed = tick;
        if (firedAt === null && s.fired) firedAt = { tick, ordinal: s.ordinal };
    });
    assert.ok(firedAt && crossed !== null, 'the sensor fires');
    assert.equal(firedAt.tick, crossed, 'fires in the tick whose committed pose first shows the threshold');
    assert.ok(firedAt.ordinal > (firedAt.tick - 1) * 4 && firedAt.ordinal <= firedAt.tick * 4, `event ordinal ${firedAt.ordinal} lies inside tick ${firedAt.tick}`);
    const s = readSensor(final, 0);
    assert.deepEqual([s.fired, s.ordinal, s.phase, s.padding], [1, firedAt.ordinal, 0, true], 'sticky: the same single event after settling flat');
    assert.ok(Math.abs(readBody(final, 1).tilt - 90) < 3, 'the tile lies flat');
});

test('orientation sensor: a sensed body that turns back within the threshold keeps its single emission', async () => {
    // A rolling sphere turns through a full revolution: the sensor fires once at 45 deg and never rearms when the alignment returns.
    const history = [];
    await run(scene([basketball([0, BENCH_Y + .34, 0], { velocity: [3, 0, 0] })], [sensorOn(3, [0, 0, 0, 1])]), 2 * TICKS_PER_SECOND,
        (tick, bytes) => history.push({ ...readSensor(bytes, 0), alignment: Math.abs(readBody(bytes, 1).q[3]) }));
    const first = history.findIndex(s => s.fired);
    assert.ok(first >= 0 && first < TICKS_PER_SECOND / 2, `fires early at tick ${first + 1}`);
    assert.ok(history.slice(first).every(s => s.fired === 1 && s.ordinal === history[first].ordinal), 'fired state and ordinal never change');
    assert.ok(history.slice(first + 1).some(s => s.alignment > Math.cos(Math.PI / 8)), 'the body really returned within the threshold afterwards');
});

test('orientation sensor: the threshold is declared data, not a constant of the worker', async () => {
    const tile = domino([0, BENCH_Y + .55 + .0005, 0], { angular: [0, 0, -4] });
    let narrow = null, wide = null;
    await run(scene([tile], [sensorOn(2, [0, 0, 0, 1], 20)]), TICKS_PER_SECOND, (tick, bytes) => { if (narrow === null && readSensor(bytes, 0).fired) narrow = tick; });
    await run(scene([tile], [sensorOn(2, [0, 0, 0, 1], 80)]), TICKS_PER_SECOND, (tick, bytes) => { if (wide === null && readSensor(bytes, 0).fired) wide = tick; });
    assert.ok(narrow !== null && wide !== null && narrow < wide, `20 deg fires at tick ${narrow}, 80 deg at tick ${wide}`);
    assert.doesNotMatch(source, /0\.92388|cos\(Math\.PI \/ 8\)|45 \*/, 'no 45-degree constant in the worker');
});

// Bowling ball (CAT-014): the shipped sphere kernel reads another declared material from the body and material records; nothing names the kind.
test('bowling ball: a rolling 0.7 m/s strike (zero declared resistance, so 0.7 m/s at impact) topples a stock Domino that the Basketball only rocks, and the declared mass alone decides it', async () => {
    let bowlingMax = 0, basketballMax = 0;
    const bowlingFinal = await run(scene([domino([0, BENCH_Y + .56, 0]), resistanceFree(bowlingBall([-1.5, BENCH_Y + BOWLING_R, 0], { velocity: [.7, 0, 0] }))]), 4 * TICKS_PER_SECOND,
        (tick, bytes) => { bowlingMax = Math.max(bowlingMax, readBody(bytes, 1).tilt); });
    const basketballFinal = await run(scene([domino([0, BENCH_Y + .56, 0]), resistanceFree(basketball([-1.5, BENCH_Y + .34, 0], { velocity: [.7, 0, 0] }))]), 4 * TICKS_PER_SECOND,
        (tick, bytes) => { basketballMax = Math.max(basketballMax, readBody(bytes, 1).tilt); });
    assert.ok(bowlingMax > 60 && Math.abs(readBody(bowlingFinal, 1).tilt - 90) < 5, `the bowling lane tile passes 60 deg and lies flat (max ${bowlingMax.toFixed(1)} deg)`);
    assert.ok(basketballMax < 20 && readBody(basketballFinal, 1).tilt < 2, `the basketball lane tile only rocks (max ${basketballMax.toFixed(1)} deg) and stands again`);
    // Mass-isolation controls: the same strike with only the mass swapped (inertia follows the declared mass). 4 kg at the Basketball
    // radius and bounce topples the tile; 1 kg at the Bowling radius and bounce only rocks it.
    let heavyMax = 0, lightMax = 0;
    await run(scene([domino([0, BENCH_Y + .56, 0]), resistanceFree(basketball([-1.5, BENCH_Y + .34, 0], { mass: 4, velocity: [.7, 0, 0] }))]), 4 * TICKS_PER_SECOND,
        (tick, bytes) => { heavyMax = Math.max(heavyMax, readBody(bytes, 1).tilt); });
    await run(scene([domino([0, BENCH_Y + .56, 0]), resistanceFree(bowlingBall([-1.5, BENCH_Y + BOWLING_R, 0], { mass: 1, velocity: [.7, 0, 0] }))]), 4 * TICKS_PER_SECOND,
        (tick, bytes) => { lightMax = Math.max(lightMax, readBody(bytes, 1).tilt); });
    assert.ok(heavyMax > 60, `4 kg at the Basketball radius topples the tile (max ${heavyMax.toFixed(1)} deg)`);
    assert.ok(lightMax < 20, `1 kg at the Bowling radius only rocks the tile (max ${lightMax.toFixed(1)} deg)`);
});

test('bowling ball: the two-lane e2e setup (both balls at x = 0.3 and centre 1.05 m, lanes z = -1.5 / +1.5) separates the kinds with a clear margin and both struck balls come to rest', async () => {
    const peak = { basketball: 0, bowling: 0 };
    const settled = { 3: [], 4: [] };   // ball states over the second after 6 s
    const final = await run(scene([domino([0, BENCH_Y + .56, -1.5]), domino([0, BENCH_Y + .56, 1.5], { id: 5 }),
        basketball([.3, 1.05, -1.5]), bowlingBall([.3, 1.05, 1.5])]), 7 * TICKS_PER_SECOND, (tick, bytes) => {
        peak.basketball = Math.max(peak.basketball, readBody(bytes, 1).tilt); peak.bowling = Math.max(peak.bowling, readBody(bytes, 2).tilt);
        if (tick >= 6 * TICKS_PER_SECOND) for (const slot of [3, 4]) settled[slot].push(readBody(bytes, slot));
    });
    assert.ok(peak.basketball < 20, `Basketball lane tilt ${peak.basketball.toFixed(1)} deg stays below 20`);
    assert.ok(peak.bowling > 60, `Bowling lane tilt ${peak.bowling.toFixed(1)} deg passes 60`);
    const [tileA, tileB, ballA, ballB] = [1, 2, 3, 4].map(slot => readBody(final, slot));
    assert.ok(tileA.tilt < 2 && Math.abs(tileB.tilt - 90) < 5, `tiles end upright (${tileA.tilt.toFixed(1)} deg) and flat (${tileB.tilt.toFixed(1)} deg)`);
    assert.ok([tileA, ballA].every(b => Math.abs(b.p[2] + 1.5) < .01) && [tileB, ballB].every(b => Math.abs(b.p[2] - 1.5) < .01), 'the lanes never interact');
    assert.ok(Math.abs(ballA.p[1] - (BENCH_Y + BASKETBALL_R)) < .005 && Math.abs(ballB.p[1] - (BENCH_Y + BOWLING_R)) < .005, `bench heights ${ballA.p[1].toFixed(3)} / ${ballB.p[1].toFixed(3)}`);
    // ENGINE-DRAG: the declared drag and rolling resistance bring both struck balls to rest: below 0.02 m/s from 6 s on and no
    // position change of 1 mm over the following second (no jitter).
    for (const [slot, label] of [[3, 'Basketball'], [4, 'Bowling ball']]) {
        const states = settled[slot];
        const fastest = Math.max(...states.map(b => b.speed));
        const drift = Math.max(...states.map(b => Math.hypot(...b.p.map((c, k) => c - states[0].p[k]))));
        assert.ok(fastest < .02, `${label} speed stays below 0.02 m/s from 6 s (max ${fastest.toFixed(4)})`);
        assert.ok(drift < .001, `${label} moves ${(drift * 1000).toFixed(3)} mm over the following second`);
    }
});

test('bowling ball: dropped from 3 m each ball rebounds to e²·h of its declared bounce (Bowling low, Basketball high) and the Bowling ball rests at its 0.28 m radius', async () => {
    const drop = async (ball, radius) => {
        let landed = false, rebounded = false, peak = -Infinity;
        const final = readBody(await run(scene([ball]), 6 * TICKS_PER_SECOND, (tick, bytes) => {
            const b = readBody(bytes, 1);
            if (b.v[1] < -1) landed = true;
            if (landed && b.v[1] > .05) rebounded = true;
            if (rebounded) peak = Math.max(peak, b.p[1]);
        }), 1);
        return { rebounded, apex: peak - (BENCH_Y + radius), final };
    };
    const bowling = await drop(bowlingBall([0, 3, 0]), BOWLING_R);
    const orange = await drop(basketball([0, 3, 0]), .34);
    // Restitution e scales the rebound height to e²·h, where h is the fall of the centre from 3 m to rest height.
    const expected = (e, radius) => e * e * (3 - (BENCH_Y + radius));
    for (const [label, ball, e, radius] of [['bowling', bowling, .14, BOWLING_R], ['basketball', orange, .55, BASKETBALL_R]])
        assert.ok(ball.rebounded && Math.abs(ball.apex - expected(e, radius)) <= .15 * expected(e, radius),
            `${label} rebound apex ${ball.apex.toFixed(3)} m is within 15% of e²·h = ${expected(e, radius).toFixed(3)} m`);
    assert.ok(Math.abs(bowling.final.p[1] - (BENCH_Y + BOWLING_R)) < .001 && bowling.final.speed < .02, `rests at radius height ${bowling.final.p[1].toFixed(4)} m, speed ${bowling.final.speed.toFixed(4)}`);
});

// ENGINE-DRAG: declared linear drag (body record +66, applied per substep after gravity) and declared rolling resistance (material
// record +14, an angular impulse opposing rolling at a sphere's contacts) are the only decelerating data; nothing names a ball.
test('drag: a free-flying sphere slows as exp(-drag·t) at 60/120/240 Hz and its free-flight spin is never damped', async () => {
    for (const cadence of [1, 2, 3]) {
        const rate = TICKS_AT[cadence], expected = 2 * Math.exp(-.125 * 2);
        const flying = basketball([0, 1, 0], { gravity: 0, drag: .125, velocity: [2, 0, 0], angular: [0, 5, 0] });
        const final = readBody(await run(scene([flying], [], cadence), 2 * rate), 1);
        // Half commits resolve about 2^-11 of the speed per tick, so the per-tick decay (0.05 % at 240 Hz) is kept within a few percent.
        assert.ok(Math.abs(final.v[0] - expected) < .03 * expected, `${rate} Hz: ${final.v[0].toFixed(4)} m/s after 2 s vs ${expected.toFixed(4)}`);
        assert.equal(final.w[1], 5, `${rate} Hz: free-flight spin is untouched (rolling resistance acts only at contacts)`);
        const control = readBody(await run(scene([resistanceFree(flying)], [], cadence), 2 * rate), 1);
        assert.equal(control.v[0], 2, `${rate} Hz: zero declared drag keeps the speed exactly`);
    }
});

test('rolling resistance: each ball rolling at 0.7 m/s decelerates at (5/7)·(Crr·g + drag·v), rests within 5 s at its radius height and never reverses', async () => {
    for (const [label, ball, crr, radius] of [['Basketball', basketball([0, BENCH_Y + BASKETBALL_R, 0]), BASKETBALL_ROLLING, BASKETBALL_R], ['Bowling ball', bowlingBall([0, BENCH_Y + BOWLING_R, 0]), BOWLING_ROLLING, BOWLING_R]]) {
        const states = [];
        await run(scene([rollingAt(ball, .7)]), 6 * TICKS_PER_SECOND, (tick, bytes) => states.push(readBody(bytes, 1)));
        // Rolling without slipping shares every decelerating impulse with the spin: a = (5/7)·(Crr·g + drag·v) for a solid sphere.
        const early = states[TICKS_PER_SECOND / 4 - 1], later = states[5 * TICKS_PER_SECOND / 4 - 1];
        const measured = early.v[0] - later.v[0], predicted = (5 / 7) * (crr * 9.81 + BALL_DRAG * (early.v[0] + later.v[0]) / 2);
        assert.ok(Math.abs(measured - predicted) < .1 * predicted, `${label} decelerates ${measured.toFixed(4)} m/s² vs ${predicted.toFixed(4)}`);
        const rest = states.findIndex(b => b.speed < .02);
        assert.ok(rest >= 0 && rest < 5 * TICKS_PER_SECOND, `${label} rests at ${((rest + 1) / TICKS_PER_SECOND).toFixed(2)} s`);
        const backward = Math.min(...states.map(b => b.v[0])), counterSpin = Math.max(...states.map(b => b.w[2]));
        // The row targets zero relative rolling, so neither the speed nor the spin ever crosses zero (a bang-bang impulse would).
        assert.ok(backward >= 0 && counterSpin <= 0, `${label} never reverses (min v ${backward} m/s, max omega_z ${counterSpin} rad/s)`);
        const tail = states.slice(-TICKS_PER_SECOND);
        const drift = Math.max(...tail.map(b => Math.hypot(...b.p.map((c, k) => c - tail[0].p[k]))));
        assert.ok(drift < .001 && Math.max(...tail.map(b => b.speed)) < .02, `${label} holds still over the final second (${(drift * 1000).toFixed(3)} mm)`);
        assert.ok(Math.abs(tail.at(-1).p[1] - (BENCH_Y + radius)) < .001, `${label} rests at its radius height (${tail.at(-1).p[1].toFixed(4)})`);
    }
});

test('rolling resistance: with zero declared drag and coefficient a rolling ball keeps its speed (control)', async () => {
    const final = readBody(await run(scene([resistanceFree(rollingAt(basketball([0, BENCH_Y + .34, 0]), .7))]), 3 * TICKS_PER_SECOND), 1);
    assert.ok(Math.abs(final.v[0] - .7) < .005, `still ${final.v[0].toFixed(4)} m/s after 3 s`);
    assert.ok(Math.abs(final.w[2] + final.v[0] / .34) < .02, `still rolling (omega_z ${final.w[2].toFixed(3)})`);
});

test('rolling resistance: at 60/120/240 Hz a ball of either kind struck to 1 m/s stops at the same time and no committed tick erases its decrement', async () => {
    for (const [label, make, radius] of [['Basketball', basketball, BASKETBALL_R], ['Bowling ball', bowlingBall, BOWLING_R]]) {
        const stops = {};
        for (const cadence of [1, 2, 3]) {
            const rate = TICKS_AT[cadence], forward = [];
            await run(scene([rollingAt(make([0, BENCH_Y + radius, 0]), 1)], [], cadence), 6 * rate, (tick, bytes) => forward.push(readBody(bytes, 1).v[0]));
            const rest = forward.findIndex(v => v < .02);
            assert.ok(rest > 0, `${label} ${rate} Hz: the ball comes to rest`);
            stops[rate] = (rest + 1) / rate;
            // A decrement below half a Half ulp would round back to the previous committed speed every tick and the ball would roll forever.
            for (let i = 1; i < rest; i++) assert.ok(forward[i] < forward[i - 1], `${label} ${rate} Hz tick ${i + 1}: ${forward[i]} m/s is not below ${forward[i - 1]}`);
        }
        assert.ok(stops[120] < 5, `${label} stops within 5 s (${stops[120].toFixed(2)} s at 120 Hz)`);
        // Per-tick Half rounding of v and omega shifts the stop by a few percent between cadences; it never removes the deceleration.
        for (const rate of [60, 240]) assert.ok(Math.abs(stops[rate] - stops[120]) < .1 * stops[120], `${label} ${rate} Hz stops at ${stops[rate].toFixed(2)} s vs ${stops[120].toFixed(2)} s`);
    }
});

// The rolling row covers both tangent axes, static and dynamic supports of any shape, and no spin about the contact normal.
const settles = async (bodies, slot, seconds = 6) => {
    const states = [];
    await run(scene(bodies), seconds * TICKS_PER_SECOND, (tick, bytes) => states.push(readBody(bytes, slot)));
    return { states, rest: states.findIndex(b => b.speed < .02), tail: states.slice(-TICKS_PER_SECOND) };
};
const spread = tail => Math.max(...tail.map(b => Math.hypot(...b.p.map((c, k) => c - tail[0].p[k]))));

test('rolling resistance: a ball rolling along +Z (spin about X, the second tangent axis) also comes to rest', async () => {
    const ball = basketball([0, BENCH_Y + BASKETBALL_R, 0], { velocity: [0, 0, .7], angular: [.7 / BASKETBALL_R, 0, 0] });
    const { states, rest, tail } = await settles([ball], 1);
    assert.ok(rest >= 0 && rest < 5 * TICKS_PER_SECOND, `rests at ${((rest + 1) / TICKS_PER_SECOND).toFixed(2)} s`);
    assert.ok(Math.min(...states.map(b => b.v[2])) >= 0 && spread(tail) < .001, `never reverses and holds still (${(spread(tail) * 1000).toFixed(3)} mm)`);
});

test('rolling resistance: a ball rolling on a static box slab (not the bench plane) comes to rest within 5 s', async () => {
    const top = BENCH_Y + 1.1;
    const slab = { id: 6, motion: 0, position: [0, BENCH_Y + 1, 0], shape: 1, half: [2.5, .1, 1], material: { restitution: 1, threshold: .1, friction: .3 } };
    const ball = rollingAt(basketball([-1, top + BASKETBALL_R, 0]), .7);
    const { rest, tail } = await settles([ball, slab], 1);
    assert.ok(rest >= 0 && rest < 5 * TICKS_PER_SECOND, `rests on the slab at ${((rest + 1) / TICKS_PER_SECOND).toFixed(2)} s`);
    const last = tail.at(-1);
    assert.ok(Math.abs(last.p[1] - (top + BASKETBALL_R)) < .002 && Math.abs(last.p[0]) < 2.5 && spread(tail) < .001, `still on the slab at (${last.p[0].toFixed(3)}, ${last.p[1].toFixed(4)})`);
});

test('rolling resistance: a ball rolling on a dynamic box slab also comes to rest (every contact, reaction on the partner)', async () => {
    // Owner decision 9 Oct 2026: the row acts at every sphere contact, so a heavy dynamic slab resting on the bench is a support too.
    const top = BENCH_Y + .2;
    const slab = { id: 6, motion: 1, position: [0, BENCH_Y + .1 + .0005, 0], shape: 1, half: [2.5, .1, 1], mass: 20, material: { restitution: .05, threshold: .1, friction: .6 } };
    const ball = rollingAt(basketball([-1, top + BASKETBALL_R, 0]), .7);
    const { rest, tail } = await settles([ball, slab], 1);
    assert.ok(rest >= 0 && rest < 5 * TICKS_PER_SECOND, `rests on the dynamic slab at ${((rest + 1) / TICKS_PER_SECOND).toFixed(2)} s`);
    const last = tail.at(-1);
    assert.ok(Math.abs(last.p[1] - (top + BASKETBALL_R)) < .003 && spread(tail) < .001, `still on the slab at (${last.p[0].toFixed(3)}, ${last.p[1].toFixed(4)})`);
});

test('drag: a free-flying sphere moving diagonally slows along both X and Z as exp(-drag·t)', async () => {
    const final = readBody(await run(scene([basketball([0, 1, 0], { gravity: 0, drag: .125, velocity: [2, 0, 2] })]), 2 * TICKS_PER_SECOND), 1);
    const expected = 2 * Math.exp(-.125 * 2);
    for (const k of [0, 2]) assert.ok(Math.abs(final.v[k] - expected) < .03 * expected, `axis ${k}: ${final.v[k].toFixed(4)} m/s vs ${expected.toFixed(4)}`);
});

test('rolling resistance: a ball resting on the bench spinning about the contact normal keeps its spin (two tangent axes only)', async () => {
    const final = readBody(await run(scene([basketball([0, BENCH_Y + BASKETBALL_R, 0], { angular: [0, 5, 0] })]), 2 * TICKS_PER_SECOND), 1);
    assert.equal(final.w[1], 5, `omega_y ${final.w[1]} rad/s after 2 s`);
});

test('rolling resistance: a free partner ball receives the equal and opposite angular impulse (zero gravity, tangential spin at impact)', async () => {
    // A Basketball spinning about Z strikes a resting free Basketball along X, so the spin is tangential at the contact. Each run is
    // compared with the same strike at zero declared resistance, which isolates the rolling row from friction.
    const strike = async spin => {
        const A = basketball([-1, 1, 0], { gravity: 0, velocity: [2, 0, 0], angular: [0, 0, spin] });
        const B = basketball([0, 1, 0], { id: 5, gravity: 0 });
        const [f, g] = [await run(scene([A, B]), TICKS_PER_SECOND), await run(scene([resistanceFree(A), resistanceFree(B)]), TICKS_PER_SECOND)];
        return { a: readBody(f, 1), b: readBody(f, 2), a0: readBody(g, 1), b0: readBody(g, 2) };
    };
    // 0.3 rad/s lies within the row's budget: using both inertias it zeroes the pair's relative rolling and spins the partner up.
    const small = await strike(.3);
    assert.ok(Math.abs(small.a.w[2] - small.b.w[2]) < .005, `relative rolling zeroed (${small.a.w[2].toFixed(4)} vs ${small.b.w[2].toFixed(4)} rad/s)`);
    assert.ok(small.b.w[2] - small.b0.w[2] > .1, `the partner gains spin from the row (${(small.b.w[2] - small.b0.w[2]).toFixed(4)} rad/s)`);
    // 10 rad/s saturates the budget: what the striker loses to the row, the equal partner gains.
    const large = await strike(10);
    const dA = large.a.w[2] - large.a0.w[2], dB = large.b.w[2] - large.b0.w[2];
    assert.ok(dB > .15 && Math.abs(dA + dB) < .05, `equal and opposite (striker ${dA.toFixed(4)}, partner ${dB.toFixed(4)} rad/s)`);
});

test('rolling resistance: a rolling Bowling ball pushing a resting Basketball (the larger radius as the second collider) brings both to rest together', async () => {
    // Slot order makes the Bowling ball collider A and the 0.34 m Basketball collider B; their contact rolls with unequal radii.
    const states = [];
    await run(scene([rollingAt(bowlingBall([-1, BENCH_Y + BOWLING_R, 0]), .7), basketball([0, BENCH_Y + BASKETBALL_R, 0])]), 6 * TICKS_PER_SECOND,
        (tick, bytes) => states.push([readBody(bytes, 1), readBody(bytes, 2)]));
    const pushed = Math.max(...states.map(([, b]) => b.v[0]));
    assert.ok(pushed > .3, `the Basketball is driven forward (peak ${pushed.toFixed(3)} m/s)`);
    const rest = Math.max(...[0, 1].map(k => states.findLastIndex(s => s[k].speed >= .02) + 1));
    assert.ok(rest < 5 * TICKS_PER_SECOND, `both rest by ${(rest / TICKS_PER_SECOND).toFixed(2)} s`);
    assert.ok(states.every(([w, b]) => w.v[0] >= -.005 && b.v[0] >= -.005 && Math.abs(w.p[2]) < .005 && Math.abs(b.p[2]) < .005), 'neither reverses nor leaves the line');
    const [w, b] = states.at(-1);
    assert.ok(Math.abs(b.p[0] - w.p[0] - (BOWLING_R + BASKETBALL_R)) < .005, `they rest in contact, Basketball ahead (gap ${(b.p[0] - w.p[0]).toFixed(4)} m)`);
});
