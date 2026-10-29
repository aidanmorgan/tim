import vm from 'node:vm';
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
// Story 9.4b R5/P10 and Murdoch-2 H1/M1/M2: the session clock ring, its master-side reply checks (the JS port
// of WorkshopClockWire.EncodeReply), the Simulation heartbeat and the Simulation's derivation exchange, slew and
// re-derivation. The peer, probe and reply bytes follow WorkshopClockWire exactly (bytes 0..3 of a peer are zero padding); the
// managed decoder (DecodeReply) consumes these replies in every Chrome suite, and WorkshopWireTests pins the same layout
// on the C# side. WorkshopClockDerivationTests decide drift natively; this file proves the ring mechanics they rely on.
import assert from 'node:assert/strict';
import test, { after } from 'node:test';
import { Worker } from 'node:worker_threads';
import * as clockRing from '../../CuriousContraptions.web/wwwroot/clock-ring.js';

const { ClockRole, ProbeBytes, ReplyBytes } = clockRing;
async function controlledClock(overrides: PropertyDescriptorMap) {
    const context = vm.createContext({ SharedArrayBuffer, ArrayBuffer, Uint8Array, Int32Array, BigInt64Array, DataView,
        Atomics: Object.create(Atomics, overrides) });
    const module = new vm.SourceTextModule(readFileSync('CuriousContraptions.web/wwwroot/clock-ring.js', 'utf8'), { context });
    await module.link(() => { throw Error('Unexpected clock import'); }); await module.evaluate();
    return module.namespace as unknown as typeof clockRing;
}

// A pending Atomics.waitAsync is not a handle that keeps a Node process alive, so a test awaiting a ring reply with no other
// pending work could end with the process instead of with the reply's timeout or answer. A page's event loop always runs the
// wait to its end; the harness holds the loop open the same way until every test has finished.
const keepAlive = setInterval(() => {}, 60_000);
after(() => clearInterval(keepAlive));

function peer(role: number, origin = 1_000_000n) {
    const bytes = new Uint8Array(64), view = new DataView(bytes.buffer);
    view.setUint32(4, 2, true);                                   // bytes 0..3: zero padding
    for (let i = 0; i < 16; i++) bytes[8 + i] = i + 1;            // session
    view.setBigUint64(24, 1n, true); view.setBigInt64(32, origin, true);
    view.setBigUint64(40, 1n, true); view.setBigUint64(48, 100_000n, true);
    view.setUint32(56, role, true); view.setUint32(60, ClockRole.Simulation, true);
    return bytes;
}
function probe(owner: Uint8Array, sequence = 7n, sent = 2_000_000n) {
    const bytes = new Uint8Array(ProbeBytes), view = new DataView(bytes.buffer);
    bytes.set(owner, 0); view.setBigUint64(64, sequence, true); view.setBigInt64(72, sent, true);
    return bytes;
}
const stamps = (reply: Uint8Array | null) => {
    assert.ok(reply);
    const view = new DataView(reply.buffer, reply.byteOffset, reply.byteLength);
    return { received: view.getBigInt64(80, true), sent: view.getBigInt64(88, true), epoch: view.getBigUint64(96, true) };
};

test('nanoseconds is the exact round-half-even port of WorkshopNativeClock.FromMilliseconds', () => {
    assert.equal(clockRing.nanoseconds(0), 0n);
    assert.equal(clockRing.nanoseconds(1), 1_000_000n);
    assert.equal(clockRing.nanoseconds(123.456789), 123_456_789n);
    assert.equal(clockRing.nanoseconds(0.5e-6), 0n, 'exactly half a nanosecond rounds to even (0)');
    assert.equal(clockRing.nanoseconds(1.5e-6), 2n, 'exactly one and a half nanoseconds rounds to even (2)');
    for (const invalid of [-1, NaN, Infinity]) assert.throws(() => clockRing.nanoseconds(invalid), RangeError);
});

test('a valid probe is answered with itself followed by the two master stamps and their derivation epoch', () => {
    const owner = peer(ClockRole.Browser), asked = probe(owner);
    const reply = clockRing.encodeReply(asked, owner, 3_000_000n, 3_000_500n, 5);
    assert.ok(reply);
    assert.equal(reply.length, ReplyBytes);
    assert.equal(ReplyBytes, 104);
    assert.deepEqual(reply.subarray(0, ProbeBytes), asked);
    assert.deepEqual(stamps(reply), { received: 3_000_000n, sent: 3_000_500n, epoch: 5n });
});

test('the master rejects every probe, stamp or epoch WorkshopClockWire.EncodeReply rejects', () => {
    const owner = peer(ClockRole.Browser);
    const cases: Record<string, [Uint8Array, bigint, bigint, number]> = {
        'another session': (() => { const p = probe(owner); p[8] ^= 1; return [p, 3_000_000n, 3_000_000n, 0]; })(),
        'another generation': (() => { const p = probe(owner); p[24] ^= 1; return [p, 3_000_000n, 3_000_000n, 0]; })(),
        'non-zero leading padding (the retired wire version 4)': (() => { const p = probe(owner); p[0] = 4; return [p, 3_000_000n, 3_000_000n, 0]; })(),
        'another requester role': [probe(peer(ClockRole.Animation)), 3_000_000n, 3_000_000n, 0],
        'another origin': [probe(peer(ClockRole.Browser, 5n)), 3_000_000n, 3_000_000n, 0],
        'a zero sequence': [probe(owner, 0n), 3_000_000n, 3_000_000n, 0],
        'a negative requester stamp': [probe(owner, 7n, -1n), 3_000_000n, 3_000_000n, 0],
        'a receipt before the master origin': [probe(owner), 999_999n, 1_000_000n, 0],
        'a reply that reverses time': [probe(owner), 3_000_000n, 2_999_999n, 0],
        'a short probe': [probe(owner).subarray(0, 79), 3_000_000n, 3_000_000n, 0],
        'a negative epoch': [probe(owner), 3_000_000n, 3_000_000n, -1],
        'a fractional epoch': [probe(owner), 3_000_000n, 3_000_000n, 1.5],
        'a NaN epoch': [probe(owner), 3_000_000n, 3_000_000n, NaN]
    };
    for (const [label, [asked, received, sent, epoch]] of Object.entries(cases))
        assert.equal(clockRing.encodeReply(asked, owner, received, sent, epoch), null, label);
    assert.ok(clockRing.encodeReply(probe(owner), owner, 1_000_000n, 1_000_000n, 0), 'control: the origin itself is admitted');
});

function startMaster(buffer: SharedArrayBuffer, peers: Record<number, Uint8Array>) {
    const worker = new Worker(new URL('./clock-master-thread.js', import.meta.url), { workerData: { buffer, peers } });
    const failed = watch(worker);
    return Promise.race([new Promise<Worker>(resolve => worker.once('message', () => resolve(worker))), failed]);
}
// A worker that fails (its module import or its serving loop throws, or a message cannot be deserialized) fails the test that
// owns it: a start still waiting for the worker rejects at once, and stop() rethrows the failure after terminating the worker.
const failures = new WeakMap<Worker, unknown>();
function watch(worker: Worker) {
    worker.unref();
    const failed = new Promise<never>((_, reject) => {
        const fail = (error: unknown) => { if (!failures.has(worker)) failures.set(worker, error); reject(error); };
        worker.on('error', fail);
        worker.on('messageerror', fail);
    });
    failed.catch(() => {});
    return failed;
}
async function stop(worker: Worker) {
    await worker.terminate();
    if (failures.has(worker)) throw failures.get(worker);
}

test('a blocking master thread answers each requester through its own slot; a foreign probe is rejected explicitly', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser), animation = peer(ClockRole.Animation);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser, [ClockRole.Animation]: animation });
    try {
        const [a, b] = await Promise.all([
            clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)),
            clockRing.requestReply(clock, ClockRole.Animation, probe(animation, 1n))]);
        assert.ok(a); assert.ok(b);
        assert.deepEqual(a.subarray(0, ProbeBytes), probe(browser, 1n));
        assert.deepEqual(b.subarray(0, ProbeBytes), probe(animation, 1n));
        assert.ok(stamps(a).sent >= stamps(a).received, 'master stamps are ordered');
        assert.equal(stamps(a).epoch, 0n, 'stamped under the initial derivation');
        await assert.rejects(clockRing.requestReply(clock, ClockRole.Browser, probe(animation, 2n)), /rejected/);
        assert.throws(() => clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 3n)), /rejected/,
            'the rejection stays visible to the next probe');
        const exchange = clockRing.deriveExchange(clock, () => 1.5, 50);
        assert.ok(exchange);
        assert.ok(exchange.before === 1_500_000n && exchange.after === 1_500_000n && exchange.master > 1_500_000n && exchange.epoch === 0,
            'the derivation exchange returns the master reading, its bracket and its epoch');
    } finally { await stop(master); }
});

test('a drifted master is re-derived: the next exchange and every reply carry the new correction and epoch', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser, 0n);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser });
    try {
        // The stand-in master reads 1.5 ms plus 1 us per stamp: against a Simulation clock of 0.5 ms it is 1 ms ahead.
        const drifted = clockRing.deriveExchange(clock, () => 0.5, 50);
        assert.ok(drifted);
        assert.ok(drifted.master - drifted.after > 900_000n, 'the drift is outside the Simulation bracket');
        const before = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)));
        assert.equal(clockRing.writeDerivation(clock, 500_000n - drifted.master, true), 1);
        assert.deepEqual(clockRing.readDerivation(clock), { epoch: 1, correction: 500_000n - drifted.master });
        const corrected = clockRing.deriveExchange(clock, () => 0.5, 50);
        assert.ok(corrected);
        assert.equal(corrected.epoch, 1);
        assert.ok(corrected.master >= 490_000n && corrected.master <= 510_000n, `the corrected reading ${corrected.master} matches the Simulation`);
        const after = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)));
        assert.equal(after.epoch, 1n, 'requesters see the new epoch and recalibrate (WorkshopClockDerivationTests)');
        assert.ok(after.received < before.received, 'stamps moved by the correction');
        assert.throws(() => Reflect.apply(clockRing.writeDerivation, undefined, [clock, NaN, false]), /Invalid integral nanosecond value/);
        assert.throws(() => Reflect.apply(clockRing.writeDerivation, undefined, [clock, 0n]), /must say whether it re-derives/);
        assert.throws(() => Reflect.apply(clockRing.writeDerivation, undefined, [clock, 0n, 1]), /must say whether it re-derives/);
    } finally { await stop(master); }
});

test('a slew installs the new correction under the same epoch, so requester mappings keep their calibration', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser, 0n);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser });
    try {
        const before = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)));
        assert.equal(clockRing.writeDerivation(clock, -5_000n, false), 0, 'a slew keeps the installed epoch');
        assert.deepEqual(clockRing.readDerivation(clock), { epoch: 0, correction: -5_000n });
        const after = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)));
        assert.equal(after.epoch, 0n, 'replies after a slew carry the unchanged epoch');
        // The stand-in master advances 1 us per stamp; the slew pulls the next stamps 5 us back.
        assert.ok(after.received < before.received, 'stamps moved by the slewed correction');
        assert.equal(clockRing.writeDerivation(clock, 0n, true), 1, 'a later re-derivation still advances the epoch');
    } finally { await stop(master); }
});

test('the Simulation heartbeat advances only when the Simulation beats, and a ring of another layout is refused', () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    assert.equal(clockRing.heartbeat(clock), 0);
    clockRing.beat(clock); clockRing.beat(clock);
    assert.equal(clockRing.heartbeat(clock), 2, 'the Browser reads exactly the beats of the Simulation event loop');
    assert.equal(clockRing.readDerivation(clock).epoch, 0, 'the heartbeat word is separate from the derivation epoch');
    // Story 9A.1 (F.10): the ring carries no layout version; a ring of another layout is refused by its structure.
    assert.equal('ClockRingVersion' in clockRing, false);
    assert.equal(new Int32Array(buffer)[1], 0, 'header word 1 is zero padding');
    assert.throws(() => clockRing.attachClockRing(new SharedArrayBuffer(clockRing.ClockRingBytes + 4)), /Unsupported clock ring layout/);
    assert.throws(() => clockRing.attachClockRing(new ArrayBuffer(clockRing.ClockRingBytes)), /Unsupported clock ring layout/);
});

test('a newer probe of the same requester supersedes an unconsumed answer, which resolves to null', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser);
    const owners = { [ClockRole.Browser]: browser };
    const first = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n));
    clockRing.serveRequests(clock, owners, () => 2_000_000n);            // answered, but not yet consumed by its requester
    const second = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n));
    clockRing.serveRequests(clock, owners, () => 3_000_000n);
    assert.equal(await first, null, 'the superseded exchange is discarded unread');
    const reply = await second;
    assert.equal(new DataView(reply!.buffer).getBigUint64(64, true), 2n);
});

test('a probe the master has not served is never overwritten: the next probe resolves null and the session continues', async () => {
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    const browser = peer(ClockRole.Browser), owners = { [ClockRole.Browser]: browser };
    const first = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n));
    assert.equal(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)), null, 'not sent, no throw');
    clockRing.serveRequests(clock, owners, () => 2_000_000n);            // the master finally serves the original request
    const reply = await first;
    assert.equal(new DataView(reply!.buffer).getBigUint64(64, true), 1n, 'the unserved probe was never torn or replaced');
    const next = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 3n));
    clockRing.serveRequests(clock, owners, () => 3_000_000n);
    assert.equal(new DataView((await next)!.buffer).getBigUint64(64, true), 3n, 'the slot is usable again');
});

test('an unanswered derivation exchange returns null, a check while it is pending never blocks, and its late answer is consumed', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    let waits = 0;
    const controlled = await controlledClock({ wait: { value() { waits++; return 'timed-out'; } } });
    assert.equal(controlled.deriveExchange(clock, () => 1.5, 1), null, 'no master: the bounded wait times out');
    // Murdoch-2 M2: the slot still holds the unserved exchange, so the next check returns at once instead of
    // blocking the Simulation event loop for another timeout.
    assert.equal(controlled.deriveExchange(clock, () => 1.5, 500), null, 'a pending exchange is not re-posted');
    assert.equal(waits, 1, 'the pending check does not enter a second blocking wait');
    clockRing.serveRequests(clock, {}, () => 1_501_000n); // answer the original, timed-out request
    const continuing = await controlledClock({ wait: { value() {
        clockRing.serveRequests(clock, {}, () => 2_501_000n); // answer only the newly posted request
        return 'ok';
    } } });
    const exchange = continuing.deriveExchange(clock, () => 1.5, 50);
    assert.ok(exchange);
    assert.equal(exchange.master, 2_501_000n, 'the stale 1,501,000 reply was discarded, not returned for the new request');
    assert.equal(exchange.epoch, 0);
});

// Blind hunter 10 (Story 9.4b final round): a requester's wait for its reply is bounded by the mapping's probe lifetime,
// so a master that never answers releases the waiter (null: the mapping expires the probe) instead of leaking it.
test('a reply wait ends after the probe lifetime when the master never answers', async () => {
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    const browser = peer(ClockRole.Browser);
    assert.equal(clockRing.ReplyWaitMilliseconds, 250);
    let timeout = 0;
    const controlled = await controlledClock({ waitAsync: { value(_words: unknown, _index: number, _expected: number, milliseconds: number) {
        timeout = milliseconds; return { async: true, value: Promise.resolve('timed-out') };
    } } });
    assert.equal(await controlled.requestReply(clock, ClockRole.Browser, probe(browser, 1n)), null);
    assert.equal(timeout, 250);
    // The unserved request still owns the slot; once the master serves it the next probe proceeds.
    clockRing.serveRequests(clock, { [ClockRole.Browser]: browser }, () => 2_000_000n);
    const next = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n));
    clockRing.serveRequests(clock, { [ClockRole.Browser]: browser }, () => 3_000_000n);
    assert.equal(new DataView((await next)!.buffer).getBigUint64(64, true), 2n);
});

// Edge-case E1 (Story 9.4b final round): one startup exchange that times out leaves the slot Requested. The startup loop
// must wait for that stale request rather than count an attempt at once, so a master stalled for 40 ms costs about
// 40 ms / timeout attempts, never the whole 500-attempt budget in microseconds.
test('startup qualification waits out a stalled master instead of burning its attempt budget', async () => {
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    let stalledOutcomes = 0, qualified = 0, missed = 0;
    const controlled = await controlledClock({ wait: { value(_words: unknown, _index: number, _expected: number, timeout: number) {
        assert.equal(timeout, 2);
        if (stalledOutcomes < 20) { stalledOutcomes++; return 'timed-out'; }
        clockRing.serveRequests(clock, {}, () => 1_501_000n);
        return 'ok';
    } } });
    controlled.qualifyDerivation(clock, () => 1.5, 2, {
        qualified: () => qualified >= 3,
        qualify: exchange => { assert.equal(exchange.master, 1_501_000n); qualified++; },
        missed: () => { if (++missed > stalledOutcomes) throw Error('the attempt budget burned without a stalled wait outcome'); }
    });
    assert.equal(missed, stalledOutcomes, 'only controlled timeouts consume the missed-attempt budget');
    assert.equal(qualified, 3, 'qualification eventually completes once the master serves requests');
});

test('signed nanosecond envelopes and ring corrections preserve adjacent large integers atomically', () => {
    const limit = (1n << 63n) - 1n;
    const exchange = clockRing.encodeDerivationExchange({ before: limit - 2n, master: limit - 1n, after: limit, epoch: 0xffffffff });
    const view = new DataView(exchange.buffer);
    assert.equal(view.getBigInt64(0, true), limit - 2n);
    assert.equal(view.getBigInt64(8, true), limit - 1n);
    assert.equal(view.getBigInt64(16, true), limit);
    assert.equal(view.getUint32(24, true), 0xffffffff);
    assert.equal(view.getUint32(28, true), 0);
    const correction = new Uint8Array(16), data = new DataView(correction.buffer);
    data.setBigInt64(0, -limit, true);
    data.setUint32(8, clockRing.ClockDerivationOutcome.Rederived, true);
    assert.deepEqual(clockRing.decodeDerivationCorrection(correction), { correction: -limit, outcome: clockRing.ClockDerivationOutcome.Rederived });
    data.setUint32(12, 1, true);
    assert.throws(() => clockRing.decodeDerivationCorrection(correction));
    data.setUint32(12, 0, true); data.setUint32(8, 99, true);
    assert.throws(() => clockRing.decodeDerivationCorrection(correction));
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    clockRing.writeDerivation(clock, -limit, false);
    for (const invalid of [limit + 1n, -limit - 2n]) {
        assert.throws(() => clockRing.writeDerivation(clock, invalid, true), RangeError);
        assert.deepEqual(clockRing.readDerivation(clock), { epoch: 0, correction: -limit });
        assert.throws(() => clockRing.encodeDerivationExchange({ before: 0n, master: invalid, after: 1n, epoch: 0 }));
    }
    const owner = peer(clockRing.ClockRole.Browser, 0n);
    assert.equal(clockRing.encodeReply(probe(owner), owner, limit + 1n, limit + 1n, 0), null);
    // A rejected external sample cannot publish a request; a valid retry still reaches the ordinary timeout.
    assert.throws(() => clockRing.deriveExchange(clock, () => NaN, 1));
    assert.equal(clockRing.deriveExchange(clock, () => 1, 1), null);
});
