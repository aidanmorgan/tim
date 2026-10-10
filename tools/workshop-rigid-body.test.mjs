// Supplemental control of the shipped simulation worker's rigid-body kernels: real admission records (body, collider,
// material tables as PhysicsGpuAbi lays them out) are stepped through the module's own stage/commit path in Node.
// No gameplay outcome is fabricated; the facts below are the physical behaviours the Chrome suites then observe.
import assert from 'node:assert/strict';
import test from 'node:test';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort } from 'node:worker_threads';

const source = await readFile(process.env.WORKER_SOURCE ?? 'CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');

const STATE_BYTES = 154048;        // PhysicsGpuAbi.ByteLength
const BODIES = 128, COLLIDERS = 4352, MATERIALS = 10496;
const ORIENTATION_SENSORS = 19744; // PhysicsGpuAbi.OrientationSensorsOffset (64-byte records, count at header byte 116)
const BENCH_Y = -0.46;
const TICKS_PER_SECOND = 120;      // cadence 2 => 4 substeps of 1/480 s per tick
// Declared ball data as BallMaterial.For compiles it (parts/catalog/ball.tres, bowling.tres): linear drag 0.04 1/s on the body
// record and the rolling-resistance coefficient on the material record.
const BALL_DRAG = .04, BASKETBALL_ROLLING = .035, BOWLING_ROLLING = .03;
const BASKETBALL_R = .34, BOWLING_R = .28;   // BallMaterial.For radii: the Bowling ball is smaller and heavier (owner decision 9 Oct 2026)

async function loadWorker(overrides = {}) {
    let imports;
    const host = {
        CommandAbi: () => [72, 5768], ResponseAbi: () => [24336, 40, 56], OperationAbi: () => [0, 1],
        StateBytes: () => STATE_BYTES, PrismaticRowKinds: () => [0, 1, 2, 3, 4, 5, 6, 7], ScheduleRoles: () => [1, 2], CaptureMode: () => 1, ...overrides
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
    const module = new vm.SourceTextModule(source + '\nexport { constraintRows, constraintMass, applyConstraintImpulse, updateDynamicFrame, sweepConstraints };', { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return dotnet;
        if (name === '../native-clock.js') return clock;
        throw Error('Unexpected worker import');
    });
    await module.evaluate();
    await imports.initialize(new Uint8Array(0));
    return { imports, kernels: module.namespace };
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
const F = (view, offset, value) => view.setFloat32(offset, value, true);
const RF = (view, offset) => view.getFloat32(offset, true);
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
// drag sits at body record +74 and rolling resistance at material record +14; both default to zero like a static declaration.
// Committed linear (m/s) and angular (rad/s) velocity are f32 at body record +48 and +60; mass +72, gravity +76, COM +82.
function scene(spec, sensors = [], cadence = 2) {
    const bytes = new Uint8Array(STATE_BYTES); const view = new DataView(bytes.buffer);
    view.setUint32(0, 11, true);
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
        F(view, r + 48, v[0]); F(view, r + 52, v[1]); F(view, r + 56, v[2]);
        F(view, r + 60, w[0]); F(view, r + 64, w[1]); F(view, r + 68, w[2]);
        H(view, r + 72, b.mass); H(view, r + 74, b.drag ?? 0); H(view, r + 78, b.gravity ?? -9.81);
        const com = b.com ?? [0, 0, 0];
        H(view, r + 82, com[0]); H(view, r + 84, com[1]); H(view, r + 86, com[2]);
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
    const v = [RF(view, r + 48), RF(view, r + 52), RF(view, r + 56)];
    const w = [RF(view, r + 60), RF(view, r + 64), RF(view, r + 68)];
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
    const { imports } = await loadWorker();
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

// Exact host bound: PhysicsDeclarationBounds.Magnitude sums the squares of the committed f32 components in double, left to right.
const squared = v => v[0] * v[0] + v[1] * v[1] + v[2] * v[2];
const sphereDirections = count => Array.from({ length: count }, (_, i) => {
    const z = 1 - (2 * i + 1) / count, r = Math.sqrt(1 - z * z), a = i * 2.399963229728653;
    return [r * Math.cos(a), z, r * Math.sin(a)];
});
test('rigid-body: speeds beyond the game-grade envelope clamp inside the exact host bound (64 m/s, 128 rad/s) in every direction and the tick continues', async () => {
    // |v| = 72 m/s and |omega| = 144 rad/s start outside both clamps along 16 directions; f32 rounding of a vector clamped to exactly
    // the bound commits a squared magnitude above 64² (and 128²) in 6 of these 16, so only the 2^-20 headroom keeps every commit admissible.
    const directions = sphereDirections(16);
    for (const [k, d] of directions.entries()) {
        const e = directions[(k + 5) % directions.length];
        const b = readBody(await run(scene([domino([0, 20, 0], { gravity: 0, velocity: d.map(c => 72 * c), angular: e.map(c => 144 * c) })]), 1), 1);
        assert.ok(squared(b.v) <= 64 * 64 && b.speed > 63.99, `direction ${k}: committed |v|² ${squared(b.v)} vs 4096 (speed ${b.speed})`);
        assert.ok(squared(b.w) <= 128 * 128 && b.spin > 127.99, `direction ${k}: committed |omega|² ${squared(b.w)} vs 16384 (spin ${b.spin})`);
        assert.ok([...b.p, ...b.q, ...b.v, ...b.w].every(Number.isFinite), `direction ${k}: finite state`);
    }
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
    // The tile rocks about its bottom edges with a slowly decaying ±3° swing for about 3 s, so the upright check samples at 4 s.
    const final = await run(scene([tile], sensors), 4 * TICKS_PER_SECOND, (tick, bytes) => { if (readSensor(bytes, 0).fired) everFired = true; });
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

// ENGINE-DRAG: declared linear drag (body record +74, applied per substep after gravity) and declared rolling resistance (material
// record +14, an angular impulse opposing rolling at a sphere's contacts) are the only decelerating data; nothing names a ball.
test('drag: a free-flying sphere slows as exp(-drag·t) at 60/120/240 Hz and its free-flight spin is never damped', async () => {
    for (const cadence of [1, 2, 3]) {
        const rate = TICKS_AT[cadence], expected = 2 * Math.exp(-.125 * 2);
        const flying = basketball([0, 1, 0], { gravity: 0, drag: .125, velocity: [2, 0, 0], angular: [0, 5, 0] });
        const final = readBody(await run(scene([flying], [], cadence), 2 * rate), 1);
        // f32 commits resolve about 2^-24 of the speed per tick, so the per-tick decay (0.05 % at 240 Hz) survives every commit.
        assert.ok(Math.abs(final.v[0] - expected) < .01 * expected, `${rate} Hz: ${final.v[0].toFixed(4)} m/s after 2 s vs ${expected.toFixed(4)}`);
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
        // The row targets zero relative rolling, so neither the speed nor the spin ever crosses zero (a bang-bang impulse would, by
        // about 3e-4 rad/s). f32 commits keep solver residue near 1e-24 at rest, so "crossing" means beyond 1e-6.
        assert.ok(backward >= -1e-6 && counterSpin <= 1e-6, `${label} never reverses (min v ${backward} m/s, max omega_z ${counterSpin} rad/s)`);
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
            // A decrement below half an ulp of the committed format would round back to the previous speed every tick (binary16 did above ~3 m/s).
            for (let i = 1; i < rest; i++) assert.ok(forward[i] < forward[i - 1], `${label} ${rate} Hz tick ${i + 1}: ${forward[i]} m/s is not below ${forward[i - 1]}`);
        }
        assert.ok(stops[120] < 5, `${label} stops within 5 s (${stops[120].toFixed(2)} s at 120 Hz)`);
        // The cadence changes only how often the committed f32 state is sampled; the stop time agrees within 10%.
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

// ENGINE-F32-VELOCITY (Story 6.1c): committed velocity and angular velocity are f32, so per-tick decrements far below a binary16 step survive.
test('f32 velocity: the balls\' declared drag 0.04 slows a free-flying ball as 2·exp(-0.04·t) on every committed tick at 60/120/240 Hz', async () => {
    for (const cadence of [1, 2, 3]) {
        const rate = TICKS_AT[cadence], forward = [], spins = [];
        const flying = basketball([0, 1, 0], { gravity: 0, velocity: [2, 0, 0], angular: [0, 5, 0] });
        await run(scene([flying], [], cadence), 2 * rate, (tick, bytes) => { const b = readBody(bytes, 1); forward.push(b.v[0]); spins.push(b.w[1]); });
        let previous = 2;
        forward.forEach((v, i) => {
            const expected = 2 * Math.exp(-BALL_DRAG * (i + 1) / rate);
            assert.ok(v < previous, `${rate} Hz tick ${i + 1}: ${v} m/s is not below ${previous}`);
            assert.ok(Math.abs(v - expected) < .01 * expected, `${rate} Hz tick ${i + 1}: ${v} m/s vs ${expected}`);
            previous = v;
        });
        assert.ok(spins.every(w => w === 5), `${rate} Hz: free-flight spin stays exactly 5 rad/s`);
    }
});

test('f32 velocity: a Basketball rolling at 5 m/s at 240 Hz decelerates on every committed tick at (5/7)·(Crr·g + drag·v) and comes to rest', async () => {
    const rate = TICKS_AT[3], states = [];
    await run(scene([rollingAt(basketball([-30, BENCH_Y + BASKETBALL_R, 0]), 5)], [], 3), 18 * rate, (tick, bytes) => states.push(readBody(bytes, 1)));
    const rest = states.findIndex(b => b.speed < .02);
    // Closed form for a = (5/7)·(Crr·g + drag·v) from 5 m/s: about 16.1 s.
    assert.ok(rest > 0 && rest < 17.5 * rate, `rests at ${((rest + 1) / rate).toFixed(2)} s`);
    // The spin shares every decelerating impulse with the speed, so it also falls on every committed tick until rest.
    let previous = 5, previousSpin = 5 / BASKETBALL_R;
    for (let i = 0; i < rest; i++) {
        assert.ok(states[i].v[0] < previous, `tick ${i + 1}: ${states[i].v[0]} m/s is not below ${previous}`);
        assert.ok(states[i].spin < previousSpin, `tick ${i + 1}: spin ${states[i].spin} rad/s is not below ${previousSpin}`);
        previous = states[i].v[0]; previousSpin = states[i].spin;
    }
    const early = states[rate - 1], later = states[3 * rate - 1];
    const measured = (early.v[0] - later.v[0]) / 2, predicted = (5 / 7) * (BASKETBALL_ROLLING * 9.81 + BALL_DRAG * (early.v[0] + later.v[0]) / 2);
    assert.ok(Math.abs(measured - predicted) < .1 * predicted, `decelerates ${measured.toFixed(4)} m/s² at ~4.5 m/s vs ${predicted.toFixed(4)}`);
    // Same tolerance as the 0.7 m/s fact: f32 commits keep solver residue near 1e-24 at rest, so "crossing" means beyond 1e-6.
    const backward = Math.min(...states.map(b => b.v[0])), counterSpin = Math.max(...states.map(b => b.w[2]));
    assert.ok(backward >= -1e-6 && counterSpin <= 1e-6, `never reverses (min v ${backward} m/s, max omega_z ${counterSpin} rad/s)`);
    const last = states.at(-1);
    assert.ok(Math.abs(last.p[1] - (BENCH_Y + BASKETBALL_R)) < .001 && last.p[0] < 30, `rests on the bench at (${last.p[0].toFixed(2)}, ${last.p[1].toFixed(4)})`);
});

// Story 6.1d (velocity hardening): boundary facts for the committed-velocity envelope and the motion-piece drag lane.
const MOTION = 20768, MOTION_HEADER = 16, MOTION_PIECE = 128;   // PhysicsGpuAbi.MotionOffset, PhysicsMotionRead.HeaderBytes / PieceBytes

test('f32 velocity: a body whose centre of mass sits off its origin (body record +82) spins about that centre in free flight', async () => {
    // Local COM 0.1 m along +X, spinning at 2 rad/s about Z with no gravity: the centre stays at (0.1, 1, 0) and the origin circles it at 0.1 m.
    const spinner = resistanceFree(basketball([0, 1, 0], { radius: .2, gravity: 0, com: [.1, 0, 0], angular: [0, 0, 2] }));
    let worst = 0, reach = 0;
    await run(scene([spinner]), 2 * TICKS_PER_SECOND, (tick, bytes) => {
        const b = readBody(bytes, 1);
        worst = Math.max(worst, Math.abs(Math.hypot(b.p[0] - .1, b.p[1] - 1, b.p[2]) - .1));
        reach = Math.max(reach, Math.hypot(b.p[0], b.p[1] - 1, b.p[2]));
    });
    // Committed binary16 rotations re-place the centre by up to about 1e-4 m per tick; reading the COM from the wrong lanes misses by > 0.04 m.
    assert.ok(worst < 3e-3, `the origin stays 0.1 m from the declared centre of mass (worst deviation ${worst.toExponential(2)} m)`);
    assert.ok(reach > .19, `the origin really circles the centre (farthest ${reach.toFixed(4)} m from its start)`);
});

test('f32 velocity: a heavy body striking a light one at the envelope cannot commit a struck speed beyond 64 m/s (the clamp after restitution)', async () => {
    // A 100 kg sphere at 64 m/s reaches a resting 1 kg sphere in the last substep of the first tick; restitution 1 would send the light
    // sphere off at about 127 m/s, so only the clamp after the relax and restitution passes keeps the committed speed admissible.
    const struck = { id: 5, motion: 1, position: [0, 5, 0], shape: 0, radius: .3, mass: 1, gravity: 0, material: { restitution: 1, threshold: .1, friction: 0 } };
    const striker = { ...struck, id: 6, position: [-1.07, 5, 0], mass: 100, velocity: [64, 0, 0] };
    const first = await run(scene([struck, striker]), 1);
    const light = readBody(first, 1), heavy = readBody(first, 2);
    assert.ok(squared(light.v) <= 64 * 64 && light.v[0] > 63.99, `struck sphere commits ${light.v[0]} m/s (|v|² ${squared(light.v)} vs 4096)`);
    assert.ok(squared(heavy.v) <= 64 * 64 && heavy.v[0] < 63.9, `striker commits ${heavy.v[0]} m/s after the impact`);
});

test('f32 velocity: the end-of-substep guard clamps a finite velocity of any size and drops only a non-finite one', async () => {
    const { kernels } = await loadWorker();
    const committed = b => ({ v: [b.vx, b.vy, b.vz].map(Math.fround), w: [b.wx, b.wy, b.wz].map(Math.fround) });
    // Components near the f64 maximum: their sum and the length of the spin overflow, yet the vectors are finite and must clamp, not zero.
    const huge = { vx: 1e308, vy: 1e308, vz: -1e308, wx: 1.5e308, wy: -1.5e308, wz: 1e308 };
    kernels.settleVelocity(huge);
    const h = committed(huge);
    assert.ok(squared(h.v) <= 64 * 64 && squared(h.v) > 63.99 ** 2, `huge velocity clamps to the envelope (|v|² ${squared(h.v)})`);
    assert.ok(squared(h.w) <= 128 * 128 && squared(h.w) > 127.99 ** 2, `huge spin clamps to the envelope (|omega|² ${squared(h.w)})`);
    assert.ok(huge.vx === huge.vy && huge.vx === -huge.vz && huge.wx === -huge.wy && huge.wz > 0, 'and keeps its direction');
    const fast = { vx: 0, vy: -100, vz: 0, wx: 0, wy: 0, wz: 200 };
    kernels.settleVelocity(fast);
    assert.ok(squared(committed(fast).v) <= 64 * 64 && fast.vy < -63.99 && fast.wz > 127.99, `an ordinary overshoot clamps (${fast.vy} m/s, ${fast.wz} rad/s)`);
    const inside = { vx: 3, vy: -4, vz: 12, wx: 1, wy: 2, wz: -3 };
    kernels.settleVelocity(inside);
    assert.deepEqual(inside, { vx: 3, vy: -4, vz: 12, wx: 1, wy: 2, wz: -3 }, 'an in-envelope velocity is untouched');
    for (const lane of ['vx', 'wz']) {
        const broken = { vx: 1, vy: 2, vz: 3, wx: 4, wy: 5, wz: 6, [lane]: NaN };
        kernels.settleVelocity(broken);
        assert.deepEqual(broken, { vx: 0, vy: 0, vz: 0, wx: 0, wy: 0, wz: 0 }, `a non-finite ${lane} drops the whole motion`);
    }
});

test('f32 velocity: every motion piece carries its body\'s declared drag rate (+54), so between-tick poses decay like the committed physics', async () => {
    const bytes = await run(scene([basketball([0, 1, 0], { gravity: 0, velocity: [2, 0, 0] }),
        bowlingBall([0, 1, 2], { gravity: 0, drag: .125, velocity: [2, 0, 0] }), resistanceFree(basketball([0, 1, -2], { id: 7, gravity: 0 }))]), 1);
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const declared = { 3: halfBits(BALL_DRAG), 4: halfBits(.125), 7: 0 };
    const count = view.getUint32(MOTION, true);
    assert.equal(count, 12, 'three dynamic bodies contribute four pieces each');
    for (let i = 0; i < count; i++) {
        const piece = MOTION + MOTION_HEADER + i * MOTION_PIECE;
        const id = Number(view.getBigUint64(piece + 24, true));
        assert.equal(view.getUint16(piece + 54, true), declared[id], `piece ${i} (body ${id}) drag rate bits`);
    }
});

test('f32 velocity: at the envelope boundary declared drag still slows a ball flying at 63.9 m/s on every committed tick at 240 Hz', async () => {
    const rate = TICKS_AT[3], speeds = [];
    await run(scene([basketball([-40, 1, 0], { gravity: 0, velocity: [63.9, 0, 0] })], [], 3), rate / 2, (tick, bytes) => speeds.push(readBody(bytes, 1).v[0]));
    let previous = 63.9;
    speeds.forEach((v, i) => {
        const expected = 63.9 * Math.exp(-BALL_DRAG * (i + 1) / rate);
        assert.ok(v < previous && Math.abs(v - expected) < 1e-3 * expected, `tick ${i + 1}: ${v} m/s vs ${expected} (previous ${previous})`);
        previous = v;
    });
});

test('f32 velocity: a Basketball rolling at 20 m/s at 240 Hz decelerates at (5/7)·(Crr·g + drag·v) over a second, its spin never rises and it never reverses', async () => {
    // Above about 5 m/s the contact rows act intermittently on a fast-spinning sphere (deferred-work, Story 6.1d): the ball skids for a few
    // ticks with only drag acting, then friction re-couples it. Precision is not the limit (an f32 step at 20 m/s is 2e-6 m/s); the
    // per-tick claim is pinned at 5 m/s above and the rate claim here.
    const rate = TICKS_AT[3], states = [];
    await run(scene([rollingAt(basketball([-60, BENCH_Y + BASKETBALL_R, 0]), 20)], [], 3), rate, (tick, bytes) => states.push(readBody(bytes, 1)));
    const risen = states.findIndex((b, i) => b.spin > (i === 0 ? 20 / BASKETBALL_R : states[i - 1].spin));
    assert.equal(risen, -1, `spin never rises (first rise at tick ${risen + 1})`);
    assert.ok(Math.min(...states.map(b => b.v[0])) > 0 && Math.max(...states.map(b => b.w[2])) < 0, 'never reverses');
    const last = states.at(-1), predicted = (5 / 7) * (BASKETBALL_ROLLING * 9.81 + BALL_DRAG * (20 + last.v[0]) / 2);
    assert.ok(Math.abs(20 - last.v[0] - predicted) < .1 * predicted, `decelerates ${(20 - last.v[0]).toFixed(4)} m/s² vs ${predicted.toFixed(4)}`);
    assert.ok(Math.abs(last.w[2] + last.v[0] / BASKETBALL_R) < .01 * last.v[0] / BASKETBALL_R && Math.abs(last.p[1] - (BENCH_Y + BASKETBALL_R)) < .002,
        `still rolling on the bench (omega_z ${last.w[2].toFixed(2)}, y ${last.p[1].toFixed(4)})`);
});

test('paid contact frame uses current anisotropic inertia and contact arms after pose integration', async () => {
    const { kernels } = await loadWorker();
    const angle = .2, sine = Math.sin(angle), cosine = Math.cos(angle);
    const izz = 4 * sine * sine + cosine * cosine;
    const iyz = 3 * sine * cosine;
    const dynamic = { motion: 1, invMass: 1, cmx: 0, cmy: 0, cmz: 0,
        vx: 0, vy: 4, vz: 0, wx: 96, wy: 0, wz: 0,
        invIWorld: [1, 0, 0, 0, 4 * cosine * cosine + sine * sine, iyz, 0, iyz, izz] };
    const fixed = { motion: 0, cmx: 0, cmy: 0, cmz: 0 };
    const contact = { a: dynamic, b: fixed, px: 1, py: 0, pz: 0,
        nx: 0, ny: 1, nz: 0, rAx: 9, rAy: 9, rAz: 9, normalMass: .5 };
    const frame = kernels.contactWorkFrame(contact);
    assert.ok(Math.abs(frame.inverseMass - (1 + izz)) < 1e-12);
    assert.ok(frame.inverseMass > 2.118 && frame.inverseMass < 2.119);
    assert.equal(frame.speed, 4);
    assert.equal(frame.contact.rAx, 1);
    assert.equal(frame.contact.rAy, 0);
    assert.equal(frame.contact.rAz, 0);
    assert.equal(contact.rAx, 9, 'contact preparation must not mutate the solver manifold');
    const correctImpulse = (8 - frame.speed) / frame.inverseMass;
    const work = frame.speed * correctImpulse + .5 * frame.inverseMass * correctImpulse ** 2;
    assert.ok(work < 12, 'current inertia must lower the impulse/work needed for this target');
    const staleWork = frame.speed * 2 + .5 * frame.inverseMass * 2 ** 2;
    assert.ok(staleWork > 12.23, 'the stale 2-unit impulse would spend undeclared work');
});

test('electrical phase routes exactly120 observations per physical second at every publication cadence', async () => {
    // Scheduling test only: the arithmetic host deliberately performs no transfer.
    for (const [cadence, rate] of [[1, 60], [2, 120], [3, 240]]) {
        let phases = 0, enabledObservations = 0;
        const { imports } = await loadWorker({
            ElectricalState: (low, high, previous) => { assert.equal(low, 1); assert.equal(high, 0); enabledObservations++; return previous; },
            AllocateElectrical: (enabled, balance, power, debit, balances, capacities, credits) => {
                assert.equal(enabled, 1); assert.equal(balance, 3600); assert.equal(power, 120);
                assert.equal(debit, 0); assert.equal(balances.length, 0); assert.equal(capacities.length, 0); assert.equal(credits.length, 0);
                phases++; return [balance, 0];
            }
        });
        const bytes = scene([], [], cadence), view = new DataView(bytes.buffer);
        view.setUint32(120, 1, true);
        const source = 150832;
        view.setBigUint64(source, 2n, true); view.setUint32(source + 12, 1, true);
        view.setFloat32(source + 16, 3600, true); view.setFloat32(source + 20, 120, true);
        view.setFloat32(source + 24, 3600, true); view.setFloat32(source + 32, 3600, true); view.setUint32(source + 36, 1, true);
        await imports.stage(bytes, 0); imports.commit();
        for (let tick = 0; tick < rate; tick++) { await imports.stage(new Uint8Array(0), 1); imports.commit(); }
        assert.equal(phases, 120); assert.equal(enabledObservations, 120);
    }
});


// These controls exercise real worker contact admission with a recording payment boundary.
// The actual C# work law is covered natively and in WASM/Chrome; this spy adds no impulse.
const CONTACT_WORK = 15136, WORK_OCCURRENCE = 15648;
function contactWorkScene(normal, velocity, {gap = -.001, previous = false, ordinal = 0, targets = 1} = {}) {
    const origin = [0, 4, 0];
    const ball = { id: 2, motion: 1, position: origin.map((v, i) => v + normal[i] * (.65 + .34 + gap)),
        shape: 0, radius: .34, mass: 1, gravity: 0, velocity,
        material: { restitution: 1, threshold: .1, friction: .3 } };
    const bumper = { id: 3, motion: 0, position: origin, shape: 0, radius: .65,
        material: { restitution: 1, threshold: .1, friction: .3 }, rotation: [.2, .3, .1, Math.sqrt(.86)] };
    const specs = [ball, bumper];
    if (targets === 2) specs.push({...ball, id: 4, position: [0, 4 - .989, 0], velocity: [0, 4, 0]});
    const bytes = scene(specs), view = new DataView(bytes.buffer);
    view.setUint32(88, ordinal, true); view.setUint32(104, 1, true); view.setUint32(108, targets, true);
    view.setBigUint64(CONTACT_WORK, 500n, true); view.setUint32(CONTACT_WORK + 8, 2, true);
    F(view, CONTACT_WORK + 16, 8); F(view, CONTACT_WORK + 20, .05);
    view.setUint32(CONTACT_WORK + 24, 72, true); view.setUint32(CONTACT_WORK + 28, 0xffffffff, true);
    F(view, CONTACT_WORK + 36, 32); F(view, CONTACT_WORK + 48, 32);
    view.setBigUint64(WORK_OCCURRENCE + 4, 2n, true);
    if (targets === 2) view.setBigUint64(WORK_OCCURRENCE + 32 + 4, 4n, true);
    if (previous) {
        view.setUint32(CONTACT_WORK + 32, 1, true);
        view.setUint32(WORK_OCCURRENCE + 12, 1, true);
        view.setUint32(WORK_OCCURRENCE + 16, 1, true);
    }
    return bytes;
}
async function observeContactAdmission(bytes) {
    const calls = [];
    const {imports} = await loadWorker({PaidContactImpulse: (speed, target, inverseMass, available, x, y, z) => {
        calls.push({speed, target, inverseMass, available, normal: [x, y, z]});
        return [available, 0, 0, 0, 0];
    }});
    await imports.stage(bytes, 0); imports.commit();
    await imports.stage(new Uint8Array(0), 1);
    return {calls, bytes: imports.read()};
}
test('contact work admission: all six radial directions qualify independent of owner rotation', async () => {
    for (const normal of [[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]]) {
        const result = await observeContactAdmission(contactWorkScene(normal, normal.map(v => -4*v)));
        assert.equal(result.calls.length, 1);
        assert.ok(result.calls[0].normal.every((v,i) => Math.abs(v-normal[i])<.001));
        assert.ok(result.calls[0].speed > 3.9);
    }
});
test('contact work admission: oblique contact keeps ordinary spin and tangent while depth miss never qualifies', async () => {
    const result = await observeContactAdmission(contactWorkScene([0,1,0], [2,-4,0]));
    assert.equal(result.calls.length, 1);
    const body = readBody(result.bytes, 1);
    assert.ok(body.spin > 1 && body.v[0] > 0 && body.v[0] < 2);
    // Solid sphere tangential momentum reconstructed at contact, before any paid radial response.
    assert.ok(Math.abs(body.v[0] - .4*.34*body.w[2] - 2) < .03);
    const missed = contactWorkScene([0,1,0], [0,-4,0]);
    new DataView(missed.buffer).setInt32(BODIES+128+24,32,true);
    assert.equal((await observeContactAdmission(missed)).calls.length,0);
});
test('contact work admission: resting separating below-threshold and grazing gap never call payment', async () => {
    for (const velocity of [[0,0,0],[0,2,0],[0,-.01,0]])
        assert.equal((await observeContactAdmission(contactWorkScene([0,1,0],velocity))).calls.length,0);
    assert.equal((await observeContactAdmission(contactWorkScene([0,1,0],[2,0,0],{gap:.02}))).calls.length,0);
});
test('contact work admission: cooldown eligibility is per body and opens at the exact 72-step boundary', async () => {
    const before = await observeContactAdmission(contactWorkScene([0,1,0],[0,-4,0],
        {previous:true,ordinal:68}));
    assert.equal(before.calls.length,0);
    assert.equal(new DataView(before.bytes.buffer,before.bytes.byteOffset).getUint32(WORK_OCCURRENCE+16,true),1);
    assert.equal(new DataView(before.bytes.buffer,before.bytes.byteOffset).getUint32(WORK_OCCURRENCE+12,true),1);
    assert.equal((await observeContactAdmission(contactWorkScene([0,1,0],[0,-4,0],
        {previous:true,ordinal:68,targets:2}))).calls.length,1);
    const eligible = await observeContactAdmission(contactWorkScene([0,1,0],[0,-4,0],
        {previous:true,ordinal:72}));
    assert.equal(eligible.calls.length,1);
    assert.equal(new DataView(eligible.bytes.buffer,eligible.bytes.byteOffset).getUint32(WORK_OCCURRENCE+16,true),73);
    assert.equal(new DataView(eligible.bytes.buffer,eligible.bytes.byteOffset).getUint32(WORK_OCCURRENCE+12,true),2);
});
test('prismatic relaxation refreshes both moving frames but retains physical-step spring error', async () => {
    const frames = [], solves = [];
    const { imports } = await loadWorker({
        SpringCoefficients: () => [.25, 20],
        PrismaticJacobians: data => {
            frames.push(Array.from(data));
            // Recording boundary, not a substitute for the independently tested C# Jacobian law.
            return Array.from({ length: 104 }, (_, i) => i % 13 === 12 ? frames.length : [0, 4, 6, 11].includes(i % 13) ? frames.length : 0);
        },
        ConstraintRows: data => { solves.push({ frame: frames.length, data: Array.from(data) }); return Array(8).fill(0); }
    });
    const bytes = scene([
        domino([0, 5, 0], { velocity: [1, 0, 0], angular: [2, 3, 4], moments: [1, 1, 1], gravity: 0 }),
        domino([2, 5, 0], { id: 3, velocity: [0, 1, 0], angular: [-3, 2, 1], moments: [1, 1, 1], gravity: 0 })
    ]);
    const view = new DataView(bytes.buffer), offset = 151488;
    view.setUint32(151472, 1, true);
    view.setBigUint64(offset, 900n, true);
    view.setUint32(offset + 8, 1, true); view.setUint32(offset + 12, 2, true);
    F(view, offset + 16, .3); F(view, offset + 44, -.2);
    F(view, offset + 40, 1); F(view, offset + 68, 1);
    F(view, offset + 72, -.25); F(view, offset + 80, 400); F(view, offset + 84, .2);
    await imports.stage(bytes, 0); imports.commit();
    await imports.stage(new Uint8Array(0), 1);
    assert.equal(frames.length, 8, 'four substeps each prepare and refresh current geometry');
    for (let step = 0; step < 4; step++) {
        const before = frames[step * 2], after = frames[step * 2 + 1];
        for (const start of [0, 10]) {
            assert.notDeepEqual(after.slice(start, start + 3), before.slice(start, start + 3), 'both origins move');
            assert.notDeepEqual(after.slice(start + 3, start + 7), before.slice(start + 3, start + 7), 'both frames rotate');
            assert.notDeepEqual(after.slice(start + 7, start + 10), before.slice(start + 7, start + 10), 'both COMs advance');
        }
        const relaxed = solves.filter(s => s.frame === step * 2 + 2);
        assert.ok(relaxed.length >= 8, 'relaxation uses refreshed geometry');
        for (let i = 0; i < relaxed.length; i++) {
            const tag = step * 2 + 2;
            assert.ok(Math.abs(relaxed[i].data[0] - (2 / halfValue(halfBits(.4)) + 2) * tag * tag) < .001, 'refreshed linear/angular Jacobians determine mass, including spring');
            assert.ok(Math.abs(relaxed[i].data[4] - 5 * tag) < .01, 'refreshed linear/angular Jacobians determine velocity, including spring');
            assert.equal(relaxed[i].data[8], i % 8 === 5 ? step * 2 + 1 : step * 2 + 2);
            if (i % 8 === 5) { assert.equal(relaxed[i].data[12], .25); assert.equal(relaxed[i].data[16], 20); }
        }
    }
});
test('real WASM prismatic rows keep zero-gravity rest and dissipate an uncharged release', { skip: process.env.SPRING_WASM !== '1' }, async () => {
    const { dotnet } = await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime = await dotnet.create();
    const { Program } = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    for (const [compression, initialSpeed] of [[0, 0], [-.1, 0], [-.24, 0], [-.2, -8]]) {
        const { imports } = await loadWorker(Program);
        const bytes = scene([
            { ...domino([0, 5, 0]), motion: 0 },
            domino([0, 5 + compression, 0], { id: 3, mass: .25, half: [.65, .075, .6], velocity: [0, initialSpeed, 0], gravity: 0 })
        ]);
        const view = new DataView(bytes.buffer), offset = 151488;
        view.setUint32(151472, 1, true); view.setBigUint64(offset, 900n, true);
        view.setUint32(offset + 8, 1, true); view.setUint32(offset + 12, 2, true);
        F(view, offset + 40, 1); F(view, offset + 68, 1);
        F(view, offset + 72, -.25); F(view, offset + 80, 400); F(view, offset + 84, .2);
        await imports.stage(bytes, 0); imports.commit();
        const initial = readBody(bytes, 2);
        const initialEnergy = .125 * initial.speed ** 2 + 200 * (initial.p[1] - 5) ** 2;
        let maximumEnergy = 0, minimumTravel = 0, maximumTravel = -Infinity, maximumLowerImpulse = 0;
        for (let tick = 0; tick < 180; tick++) {
            await imports.stage(new Uint8Array(0), 1);
            const output = imports.read();
            maximumLowerImpulse = Math.max(maximumLowerImpulse, new DataView(output.buffer, output.byteOffset, output.byteLength).getFloat32(offset + 120, true));
            const body = readBody(output, 2); imports.commit();
            const travel = body.p[1] - 5;
            const energy = .125 * body.speed ** 2 + 200 * travel ** 2;
            maximumEnergy = Math.max(maximumEnergy, energy);
            minimumTravel = Math.min(minimumTravel, travel); maximumTravel = Math.max(maximumTravel, travel);
            assert.ok(energy <= initialEnergy + .005, `finite passive energy ${energy} <= ${initialEnergy}`);
            assert.ok(travel >= -.251 && travel <= .001, `travel ${travel}`);
            assert.ok(Math.abs(body.p[0]) < 1e-5 && Math.abs(body.p[2]) < 1e-5 && body.spin < 1e-5);
            if (compression === 0) assert.equal(energy, 0, 'unloaded zero-gravity plate cannot start itself');
        }
        if (initialSpeed < 0) assert.ok(maximumLowerImpulse > 0, 'incoming motion activates the lower unilateral stop');
        console.log(JSON.stringify({ compression, initialSpeed, initialEnergy, maximumEnergy, minimumTravel, maximumTravel, maximumLowerImpulse }));
    }
});
test('real WASM moving tilted pair preserves reaction momentum and bounded total energy', { skip: process.env.SPRING_WASM !== '1' }, async () => {
    const { dotnet } = await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime = await dotnet.create();
    const { Program } = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const { imports } = await loadWorker(Program);
    const rotation = [0, 0, Math.sin(.2), Math.cos(.2)];
    const bytes = scene([
        domino([0, 5, 0], { mass: 1, rotation, velocity: [1, .3, 0], angular: [.2, .3, .5], moments: [1, 1, 1], gravity: 0 }),
        domino([0, 5, 0], { id: 3, mass: .25, rotation, velocity: [-.5, -.4, 0], angular: [-.2, .1, -.3], moments: [1, 1, 1], gravity: 0 })
    ]);
    const view = new DataView(bytes.buffer), offset = 151488;
    view.setUint32(151472, 1, true); view.setBigUint64(offset, 901n, true);
    view.setUint32(offset + 8, 1, true); view.setUint32(offset + 12, 2, true);
    for (const frame of [16, 44]) { F(view, offset + frame, .3); F(view, offset + frame + 8, .2); F(view, offset + frame + 24, 1); }
    F(view, offset + 72, -.25); F(view, offset + 80, 400); F(view, offset + 84, .2);
    const cross = (a, b) => [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]];
    const rotate = (q, v) => { const norm=Math.hypot(...q);const xyz=q.slice(0,3).map(x=>x/norm),w=q[3]/norm;const t=cross(xyz,v).map(x=>2*x),u=cross(xyz,t);return v.map((x,i)=>x+w*t[i]+u[i]); };
    const quantities = data => {
        const a=readBody(data,1), b=readBody(data,2);
        const axis=rotate(a.q,[0,1,0]), aa=rotate(a.q,[.3,0,.2]), ab=rotate(b.q,[.3,0,.2]);
        const displacement=b.p.map((x,i)=>x+ab[i]-a.p[i]-aa[i]);
        const q=axis.reduce((s,x,i)=>s+x*displacement[i],0);
        const momentum=a.v.map((x,i)=>x+.25*b.v[i]);
        const oa=cross(a.p,a.v),ob=cross(b.p,b.v.map(x=>.25*x));
        const angular=a.w.map((x,i)=>x+b.w[i]+oa[i]+ob[i]);
        return { energy:.5*a.speed**2+.125*b.speed**2+.5*(a.spin**2+b.spin**2)+200*q*q, momentum, angular, q };
    };
    await imports.stage(bytes,0);imports.commit();
    const initial=quantities(bytes);let maximumEnergy=initial.energy, maximumMomentumError=0,maximumAngularError=0;
    for(let tick=0;tick<120;tick++){
        await imports.stage(new Uint8Array(0),1);const current=quantities(imports.read());imports.commit();
        maximumEnergy=Math.max(maximumEnergy,current.energy);
        maximumMomentumError=Math.max(maximumMomentumError,...current.momentum.map((x,i)=>Math.abs(x-initial.momentum[i])));
        maximumAngularError=Math.max(maximumAngularError,...current.angular.map((x,i)=>Math.abs(x-initial.angular[i])));
        assert.ok(current.energy<=initial.energy+.01,`energy ${current.energy} initial ${initial.energy}`);
        assert.ok(current.q>=-.251 && current.q<=.001,`travel ${current.q}`);
    }
    assert.ok(maximumMomentumError<.001,`momentum ${maximumMomentumError}`);
    assert.ok(maximumAngularError<.01,`angular momentum ${maximumAngularError}`);
    console.log(JSON.stringify({initial,maximumEnergy,maximumMomentumError,maximumAngularError}));
});
test('real WASM isolated anisotropic row applies both current-frame reactions', { skip: process.env.SPRING_WASM !== '1' }, async () => {
    const { dotnet } = await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime = await dotnet.create();
    const { Program } = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    let firstInput, firstResult, calls=0;
    const { kernels } = await loadWorker({ ...Program, ConstraintRows: input => {
        if(calls++===0){firstInput=Array.from(input);firstResult=Array.from(Program.ConstraintRows(input));return firstResult;}
        return Array(8).fill(0); // Isolate one real solved row; subsequent row reactions are deliberately disabled in this harness.
    }});
    const makeBody=(slot,angle,mass,moments,p,w)=>({
        bodySlot:slot,motion:1,px:p[0],py:p[1],pz:p[2],
        qx:0,qy:Math.sin(angle/2),qz:0,qw:Math.cos(angle/2),
        principal:[0,0,0,1],comLocal:[.1,0,-.2],invMass:1/mass,
        invI:moments.map(x=>1/x),invIWorld:Array(9).fill(0),
        vx:.3,vy:-.2,vz:.1,wx:w[0],wy:w[1],wz:w[2]
    });
    // All masses/moments and local anchors here are exactly representable in the admitted shared scalar lanes.
    const a=makeBody(1,.4,1,[.5,1,2],[0,4,0],[.2,.3,-.4]);
    const b=makeBody(2,-.7,.5,[2,.25,1],[.2,4.1,-.1],[-.3,.5,.1]);
    kernels.updateDynamicFrame(a);kernels.updateDynamicFrame(b);
    const joint={a,b,pa:[.25,.125,-.25],qa:[0,0,0,1],pb:[-.125,.25,.125],qb:[0,0,0,1],
        lower:-.25,upper:0,soft:Array.from(Program.SpringCoefficients(400,.2)),impulses:Array(8).fill(0)};
    joint.rows=kernels.constraintRows(joint,1/480);
    const row=joint.rows[0],beforeA={...a},beforeB={...b};
    const dot=(x,y)=>x.reduce((sum,value,i)=>sum+value*y[i],0);
    const multiply=(angle,inverse,v)=>{
        const c=Math.cos(angle),s=Math.sin(angle);
        const local=[c*v[0]-s*v[2],v[1],s*v[0]+c*v[2]].map((x,i)=>x*inverse[i]);
        return [c*local[0]+s*local[2],local[1],-s*local[0]+c*local[2]];
    };
    const ia=multiply(.4,[2,1,.5],row.aa),ib=multiply(-.7,[.5,4,1],row.ab);
    const expectedMass=dot(row.la,row.la)+2*dot(row.lb,row.lb)+dot(row.aa,ia)+dot(row.ab,ib);
    kernels.sweepConstraints([joint],true);
    assert.ok(Math.abs(firstInput[0]-expectedMass)<2e-6,`effective mass ${firstInput[0]} vs ${expectedMass}`);
    const impulse=firstResult[4];assert.ok(Math.abs(impulse)>.001);
    for(const [body,before,linear,angular,invMass] of [[a,beforeA,row.la,ia,1],[b,beforeB,row.lb,ib,2]]){
        for(const [i,key] of ['vx','vy','vz'].entries())assert.ok(Math.abs(body[key]-before[key]-linear[i]*impulse*invMass)<2e-5,`linear reaction ${key}`);
        for(const [i,key] of ['wx','wy','wz'].entries())assert.ok(Math.abs(body[key]-before[key]-angular[i]*impulse)<2e-5,`angular reaction ${key}`);
    }
    console.log(JSON.stringify({expectedMass,actualMass:firstInput[0],impulse}));
});
test('real WASM gravity-fed ball and plate contact respects complete passive energy', { skip: process.env.SPRING_WASM !== '1' }, async () => {
    const {dotnet}=await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime=await dotnet.create();const {Program}=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const {imports}=await loadWorker(Program);
    const bytes=scene([
        {...domino([0,4.5,0]),motion:0,half:[.65,.06,.6]},
        domino([0,5,0],{id:3,mass:.25,half:[.65,.075,.6],material:{restitution:0,threshold:.05,friction:.1}}),
        basketball([0,6.5,0],{id:4})
    ]);
    const view=new DataView(bytes.buffer),offset=151488;
    view.setUint32(151472,1,true);view.setBigUint64(offset,902n,true);
    view.setUint32(offset+8,1,true);view.setUint32(offset+12,2,true);
    F(view,offset+20,.5);F(view,offset+40,1);F(view,offset+68,1);
    F(view,offset+72,-.25);F(view,offset+80,400);F(view,offset+84,.2);
    const moments=slot=>[96,104,112].map(at=>RH(view,BODIES+slot*128+at)*2**view.getInt32(BODIES+slot*128+at+4,true));
    const plateI=moments(2),ballI=moments(3),g=-RH(view,BODIES+2*128+78);
    const energy=data=>{
        const plate=readBody(data,2),ball=readBody(data,3);
        // Centred drop retains identity orientation. Include every rotational component explicitly.
        assert.ok(plate.tilt<.001&&ball.tilt<.001);
        const rotational=.5*plate.w.reduce((sum,w,i)=>sum+plateI[i]*w*w,0)+.5*ball.w.reduce((sum,w,i)=>sum+ballI[i]*w*w,0);
        return .125*plate.speed**2+.5*ball.speed**2+rotational+g*(.25*plate.p[1]+ball.p[1])+200*(plate.p[1]-5)**2;
    };
    await imports.stage(bytes,0);imports.commit();
    const initial=energy(bytes);let maximum=initial,minimumPlate=5,upward=0;
    for(let tick=0;tick<360;tick++){
        await imports.stage(new Uint8Array(0),1);const output=imports.read();imports.commit();
        const current=energy(output),plate=readBody(output,2),ball=readBody(output,3);
        maximum=Math.max(maximum,current);minimumPlate=Math.min(minimumPlate,plate.p[1]);upward=Math.max(upward,ball.v[1]);
        assert.ok(current<=initial+.01,`complete energy ${current} vs ${initial}`);
        assert.ok(plate.p[1]>=4.749&&plate.p[1]<=5.001);
    }
    assert.ok(minimumPlate<4.85&&upward>2,'actual contact compresses plate and returns finite spring work to ball');
    console.log(JSON.stringify({initial,maximum,minimumPlate,upward,g}));
});

test('real WASM preserved passive ball contacts along all three source orientations', { skip: process.env.SPRING_WASM !== '1' }, async () => {
    const {dotnet}=await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime=await dotnet.create(),{Program}=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
    const rotate=(q,v)=>{const length=Math.hypot(...q),xyz=q.slice(0,3).map(x=>x/length),w=q[3]/length,t=cross(xyz,v).map(x=>2*x),u=cross(xyz,t);return v.map((x,i)=>x+w*t[i]+u[i]);};
    const multiply=(a,b)=>{const c=cross(a,b);return [...[0,1,2].map(i=>a[3]*b[i]+b[3]*a[i]+c[i]),a[3]*b[3]-a.slice(0,3).reduce((s,x,i)=>s+x*b[i],0)];};
    for(const degrees of [[0,0,0],[30,50,70],[90,0,0]]){
        // Godot default Euler YXZ, matching baseline PartOrientation.FromEulerDegrees columns.
        const [x,y,z]=degrees.map(v=>v*Math.PI/360);
        const rotation=multiply(multiply([0,Math.sin(y),0,Math.cos(y)],[Math.sin(x),0,0,Math.cos(x)]),[0,0,Math.sin(z),Math.cos(z)]);
        const n=rotate(rotation,[0,1,0]),origin=[0,4,0],at=d=>origin.map((v,i)=>v+d*n[i]);
        const bytes=scene([
            {...domino(at(-.36)),motion:0,rotation,half:[.65,.06,.6]},
            domino(at(.14),{id:3,rotation,mass:.25,half:[.65,.075,.6],moments:[.25*(.15**2+1.2**2)/12,.25*(1.3**2+1.2**2)/12,.25*(1.3**2+.15**2)/12],gravity:0,material:{restitution:0,threshold:.05,friction:.1}}),
            basketball(at(1),{id:4,velocity:n.map(v=>-2*v),gravity:0,drag:0})
        ]);
        const view=new DataView(bytes.buffer),offset=151488;
        view.setUint32(151472,1,true);view.setBigUint64(offset,903n,true);
        view.setUint32(offset+8,1,true);view.setUint32(offset+12,2,true);
        F(view,offset+20,.5);F(view,offset+40,1);F(view,offset+68,1);
        F(view,offset+72,-.25);F(view,offset+80,400);
        const axis=rotate(readBody(bytes,1).q,[0,1,0]),base=readBody(bytes,1).p;
        const moments=slot=>[96,104,112].map(a=>RH(view,BODIES+slot*128+a)*2**view.getInt32(BODIES+slot*128+a+4,true));
        const inertia=[moments(2),moments(3)];
        const measure=data=>{
            const plate=readBody(data,2),ball=readBody(data,3);
            const displacement=plate.p.reduce((s,v,i)=>s+(v-base[i])*axis[i],0)-.5;
            const rotational=[plate,ball].reduce((s,b,index)=>{const local=rotate([-b.q[0],-b.q[1],-b.q[2],b.q[3]],b.w);return s+.5*local.reduce((sum,w,i)=>sum+inertia[index][i]*w*w,0);},0);
            return {energy:.125*plate.speed**2+.5*ball.speed**2+rotational+200*displacement**2,compression:-displacement,returned:ball.v.reduce((s,v,i)=>s+v*axis[i],0)};
        };
        const {imports}=await loadWorker(Program);await imports.stage(bytes,0);imports.commit();
        const initial=measure(bytes).energy;let maximum=initial,compression=0,returned=0;
        for(let tick=0;tick<120;tick++){
            await imports.stage(new Uint8Array(0),1);const m=measure(imports.read());imports.commit();
            maximum=Math.max(maximum,m.energy);compression=Math.max(compression,m.compression);returned=Math.max(returned,m.returned);
            assert.ok(m.energy>=0&&m.energy<=initial+1e-5,`orientation ${degrees}: energy ${m.energy} initial ${initial}`);
        }
        assert.ok(compression>=.01&&compression<=.250001,`compression ${compression}`);
        assert.ok(returned>.2,`return speed ${returned}`);
        console.log(JSON.stringify({degrees,initial,maximum,compression,returned}));
    }
});

test('prismatic row-kind boundary rejects changed missing or duplicate mappings', async () => {
    for (const mapping of [[0,1,2,3,4,5,6], [0,1,2,3,4,6,5,7], [0,1,2,3,4,5,6,6]])
        await assert.rejects(loadWorker({PrismaticRowKinds:()=>mapping}), /Invalid prismatic row kind mapping/);
});

test('real WASM five disjoint joints scatter full and partial SIMD batches and shared bodies sequence', {skip:process.env.SPRING_WASM!=='1'}, async()=>{
    const {dotnet}=await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime=await dotnet.create(),{Program}=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const {kernels}=await loadWorker(Program);
    const body=(slot,seed)=>{const b={bodySlot:slot,motion:1,px:seed*.1,py:4+seed*.03,pz:-seed*.02,
        qx:0,qy:Math.sin(seed*.03),qz:0,qw:Math.cos(seed*.03),comLocal:[0,0,0],principal:[0,0,0,1],
        invMass:1/(1+seed*.125),invI:[1+seed*.1,.5+seed*.05,.8+seed*.025],invIWorld:Array(9).fill(0),
        vx:seed*.07,vy:-seed*.03,vz:seed*.02,wx:seed*.01,wy:-seed*.015,wz:seed*.02};
        kernels.updateDynamicFrame(b);return b;};
    const joint=(a,b,seed)=>{const j={a,b,pa:[.03*seed,0,.01],qa:[0,0,0,1],pb:[-.01,0,.02*seed],qb:[0,0,0,1],
        lower:-.25,upper:0,soft:Array.from(Program.SpringCoefficients(120+seed*50,.2)),impulses:Array(8).fill(0)};
        j.rows=kernels.constraintRows(j,1/480);return j;};
    const make=()=>Array.from({length:5},(_,i)=>joint(body(i*2+1,i+1),body(i*2+2,i+2),i+1));
    const together=make(),separate=make();
    for(let sweep=0;sweep<3;sweep++){kernels.sweepConstraints(together,true);for(const j of separate)kernels.sweepConstraints([j],true);}
    const state=j=>({a:['vx','vy','vz','wx','wy','wz'].map(k=>j.a[k]),b:['vx','vy','vz','wx','wy','wz'].map(k=>j.b[k]),impulses:Array.from(j.impulses)});
    assert.deepEqual(together.map(state),separate.map(state));
    assert.equal(new Set(together.map(j=>JSON.stringify(j.impulses))).size,5,'Distinct lanes cannot be broadcast or dropped');
    const shared=()=>{const common=body(2,2);return[joint(body(1,1),common,1),joint(common,body(3,3),2)];};
    const actual=shared(),expected=shared();
    // Independent scalar row schedule: each later joint must consume the earlier shared-body reaction.
    for(let sweep=0;sweep<3;sweep++){
        kernels.sweepConstraints(actual,true);
        for(let row=0;row<8;row++)for(const j of expected){
            const r=j.rows[row],input=Array(32).fill(0);
            input[0]=Math.fround(kernels.constraintMass(j.a,r.la,r.aa)+kernels.constraintMass(j.b,r.lb,r.ab));
            const velocity=(b,l,a)=>Math.fround(Math.fround(Math.fround(Math.fround(l[0]*b.vx)+Math.fround(l[1]*b.vy))+Math.fround(l[2]*b.vz))+
                Math.fround(Math.fround(Math.fround(a[0]*b.wx)+Math.fround(a[1]*b.wy))+Math.fround(a[2]*b.wz)));
            input[4]=Math.fround(velocity(j.a,r.la,r.aa)+velocity(j.b,r.lb,r.ab));
            input[8]=r.error;input[12]=r.gamma;input[16]=r.bias;input[20]=j.impulses[row];input[24]=r.minimum;input[28]=3.4028234663852886e38;
            const result=Program.ConstraintRows(input);j.impulses[row]=result[0];kernels.applyConstraintImpulse(j,r,result[4]);
        }
    }
    assert.deepEqual(actual.map(state),expected.map(state));
});

test('real WASM connected collision policy filters only its declared pair', {skip:process.env.SPRING_WASM!=='1'},async()=>{
    const {dotnet}=await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime=await dotnet.create(),{Program}=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const outcomes=[];
    for(const enabled of [0,1]){
        const bytes=scene([{...domino([0,5,0]),motion:0,half:[.65,.05,.6]},
            domino([0,5,0],{id:3,mass:.25,half:[.65,.075,.6],gravity:0}),
            {...domino([3,5,0]),id:4,motion:0,half:[.5,.1,.5]},
            basketball([3,5.3,0],{id:5,velocity:[0,-2,0],gravity:0,drag:0})]);
        const v=new DataView(bytes.buffer),o=151488;v.setUint32(151472,1,true);v.setBigUint64(o,904n,true);
        v.setUint32(o+8,1,true);v.setUint32(o+12,2,true);F(v,o+40,1);F(v,o+68,1);
        F(v,o+72,-.25);F(v,o+80,400);v.setUint32(o+88,enabled,true);
        const {imports}=await loadWorker(Program);await imports.stage(bytes,0);imports.commit();
        await imports.stage(new Uint8Array(0),1);const output=imports.read();imports.commit();
        outcomes.push({plate:readBody(output,2),other:readBody(output,4)});
    }
    assert.equal(outcomes[0].plate.speed,0);
    assert.ok(outcomes[1].plate.speed>.001||Math.abs(outcomes[1].plate.p[1]-5)>.00001,'Enabled connected overlap has a physical response');
    assert.deepEqual(outcomes[0].other,outcomes[1].other,'Unrelated collision is retained for both policies');
    assert.ok(outcomes[0].other.v[1]>0,'Unrelated ball really collides rather than passing through');
    console.log(JSON.stringify(outcomes));
});

test('real WASM compound local frames restore across the shortest-arc boundary', {skip:process.env.SPRING_WASM!=='1'},async()=>{
    const {dotnet}=await import('../CuriousContraptions.web/AppBundle/simulation/_framework/dotnet.js');
    const runtime=await dotnet.create(),{Program}=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
    const mul=(a,b)=>{const c=cross(a,b);return [...[0,1,2].map(i=>a[3]*b[i]+b[3]*a[i]+c[i]),a[3]*b[3]-a.slice(0,3).reduce((s,x,i)=>s+x*b[i],0)];};
    const axis=(v,t)=>{const n=Math.hypot(...v);return[...v.map(x=>x/n*Math.sin(t/2)),Math.cos(t/2)];};
    const la=axis([1,2,0],.4),lb=axis([0,1,2],-.3),qa=axis([2,-1,1],.5),conjugate=q=>[-q[0],-q[1],-q[2],q[3]];
    for(const degrees of [179.5,180.5]){
        const results=[];
        for(const sign of [1,-1]){
            const qb=mul(mul(mul(axis([1,2,-3],degrees*Math.PI/180),qa),la),conjugate(lb)).map(x=>x*sign);
            const bytes=scene([{...domino([0,5,0]),motion:0,rotation:qa},
                domino([0,5,0],{id:3,rotation:qb,mass:.25,moments:[.5,.75,1],gravity:0})]);
            const v=new DataView(bytes.buffer),o=151488;v.setUint32(151472,1,true);v.setBigUint64(o,905n,true);
            v.setUint32(o+8,1,true);v.setUint32(o+12,2,true);
            la.forEach((x,i)=>F(v,o+28+i*4,x));lb.forEach((x,i)=>F(v,o+56+i*4,x));
            F(v,o+72,-.25);F(v,o+80,400);
            const {imports}=await loadWorker(Program);await imports.stage(bytes,0);imports.commit();let final,maxSpin=0;
            for(let tick=0;tick<120;tick++){
                await imports.stage(new Uint8Array(0),1);final=readBody(imports.read(),2);imports.commit();
                maxSpin=Math.max(maxSpin,final.spin);assert.ok(Number.isFinite(final.spin)&&final.spin<128);
            }
            const relative=mul(mul(final.q,lb),conjugate(mul(readBody(bytes,1).q,la)));
            const angle=2*Math.atan2(Math.hypot(...relative.slice(0,3)),Math.abs(relative[3]));
            assert.ok(angle<.01,`restoration ${degrees}/${sign}: ${angle}`);
            results.push({angle,maxSpin,position:final.p});
        }
        assert.ok(Math.abs(results[0].angle-results[1].angle)<1e-5);
        assert.ok(Math.abs(results[0].maxSpin-results[1].maxSpin)<1e-4);
        console.log(JSON.stringify({degrees,results}));
    }
});
