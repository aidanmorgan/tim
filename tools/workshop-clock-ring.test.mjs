// Story 9.4b R5/P10 and Murdoch-2 H1/M1/M2: the session clock ring, its master-side reply checks (the JS port
// of WorkshopClockWire.EncodeReply), the Simulation heartbeat and the Simulation's derivation exchange, slew and
// re-derivation. The peer, probe and reply bytes follow WorkshopClockWire exactly (bytes 0..3 of a peer are zero padding); the
// managed decoder (DecodeReply) consumes these replies in every Chrome suite, and WorkshopWireTests pins the same layout
// on the C# side. WorkshopClockDerivationTests decide drift natively; this file proves the ring mechanics they rely on.
import assert from 'node:assert/strict';
import test from 'node:test';
import { Worker } from 'node:worker_threads';
import * as clockRing from '../CuriousContraptions.web/wwwroot/clock-ring.js';

const { ClockRole, ProbeBytes, ReplyBytes } = clockRing;

function peer(role, origin = 1_000_000n) {
    const bytes = new Uint8Array(64), view = new DataView(bytes.buffer);
    view.setUint32(4, 2, true);                                   // bytes 0..3: zero padding
    for (let i = 0; i < 16; i++) bytes[8 + i] = i + 1;            // session
    view.setBigUint64(24, 1n, true); view.setBigInt64(32, origin, true);
    view.setBigUint64(40, 1n, true); view.setBigUint64(48, 100_000n, true);
    view.setUint32(56, role, true); view.setUint32(60, ClockRole.Simulation, true);
    return bytes;
}
function probe(owner, sequence = 7n, sent = 2_000_000n) {
    const bytes = new Uint8Array(ProbeBytes), view = new DataView(bytes.buffer);
    bytes.set(owner, 0); view.setBigUint64(64, sequence, true); view.setBigInt64(72, sent, true);
    return bytes;
}
const stamps = reply => {
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
    assert.equal(reply.length, ReplyBytes);
    assert.equal(ReplyBytes, 104);
    assert.deepEqual(reply.subarray(0, ProbeBytes), asked);
    assert.deepEqual(stamps(reply), { received: 3_000_000n, sent: 3_000_500n, epoch: 5n });
});

test('the master rejects every probe, stamp or epoch WorkshopClockWire.EncodeReply rejects', () => {
    const owner = peer(ClockRole.Browser);
    const cases = {
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

const masterScript = `
const { parentPort, workerData } = require('node:worker_threads');
import(workerData.ringModule).then(ring => {
    const clock = ring.attachClockRing(workerData.buffer);
    let now = 1.5;
    parentPort.postMessage('ready');
    for (;;) ring.awaitRequest(clock, ring.serveRequests(clock, workerData.peers, () => (now += 0.001)));
});`;
function startMaster(buffer, peers) {
    const worker = new Worker(masterScript, { eval: true, workerData: { buffer, peers,
        ringModule: new URL('../CuriousContraptions.web/wwwroot/clock-ring.js', import.meta.url).href } });
    worker.unref();
    return new Promise(resolve => worker.once('message', () => resolve(worker)));
}

test('a blocking master thread answers each requester through its own slot; a foreign probe is rejected explicitly', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser), animation = peer(ClockRole.Animation);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser, [ClockRole.Animation]: animation });
    try {
        const [a, b] = await Promise.all([
            clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)),
            clockRing.requestReply(clock, ClockRole.Animation, probe(animation, 1n))]);
        assert.deepEqual(a.subarray(0, ProbeBytes), probe(browser, 1n));
        assert.deepEqual(b.subarray(0, ProbeBytes), probe(animation, 1n));
        assert.ok(stamps(a).sent >= stamps(a).received, 'master stamps are ordered');
        assert.equal(stamps(a).epoch, 0n, 'stamped under the initial derivation');
        await assert.rejects(clockRing.requestReply(clock, ClockRole.Browser, probe(animation, 2n)), /rejected/);
        assert.throws(() => clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 3n)), /rejected/,
            'the rejection stays visible to the next probe');
        const exchange = clockRing.deriveExchange(clock, () => 1.5, 50);
        assert.ok(exchange.before === 1.5 && exchange.after === 1.5 && exchange.master > 1.5 && exchange.epoch === 0,
            'the derivation exchange returns the master reading, its bracket and its epoch');
    } finally { await master.terminate(); }
});

test('a drifted master is re-derived: the next exchange and every reply carry the new correction and epoch', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser, 0n);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser });
    try {
        // The stand-in master reads 1.5 ms plus 1 us per stamp: against a Simulation clock of 0.5 ms it is 1 ms ahead.
        const drifted = clockRing.deriveExchange(clock, () => 0.5, 50);
        assert.ok(drifted.master - drifted.after > 0.9, 'the drift is outside the Simulation bracket');
        const before = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)));
        assert.equal(clockRing.writeDerivation(clock, 0.5 - drifted.master, true), 1);
        assert.deepEqual(clockRing.readDerivation(clock), { epoch: 1, correction: 0.5 - drifted.master });
        const corrected = clockRing.deriveExchange(clock, () => 0.5, 50);
        assert.equal(corrected.epoch, 1);
        assert.ok(Math.abs(corrected.master - 0.5) < 0.01, `the corrected reading ${corrected.master} matches the Simulation`);
        const after = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)));
        assert.equal(after.epoch, 1n, 'requesters see the new epoch and recalibrate (WorkshopClockDerivationTests)');
        assert.ok(after.received < before.received, 'stamps moved by the correction');
        assert.throws(() => clockRing.writeDerivation(clock, NaN), /Invalid clock derivation correction/);
        assert.throws(() => clockRing.writeDerivation(clock, 0), /must say whether it re-derives/);
        assert.throws(() => clockRing.writeDerivation(clock, 0, 1), /must say whether it re-derives/);
    } finally { await master.terminate(); }
});

test('a slew installs the new correction under the same epoch, so requester mappings keep their calibration', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const browser = peer(ClockRole.Browser, 0n);
    const master = await startMaster(buffer, { [ClockRole.Browser]: browser });
    try {
        const before = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)));
        assert.equal(clockRing.writeDerivation(clock, -0.005, false), 0, 'a slew keeps the installed epoch');
        assert.deepEqual(clockRing.readDerivation(clock), { epoch: 0, correction: -0.005 });
        const after = stamps(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)));
        assert.equal(after.epoch, 0n, 'replies after a slew carry the unchanged epoch');
        // The stand-in master advances 1 us per stamp; the slew pulls the next stamps 5 us back.
        assert.ok(after.received < before.received, 'stamps moved by the slewed correction');
        assert.equal(clockRing.writeDerivation(clock, 0, true), 1, 'a later re-derivation still advances the epoch');
    } finally { await master.terminate(); }
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
    clockRing.serveRequests(clock, owners, () => 2);            // answered, but not yet consumed by its requester
    const second = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n));
    clockRing.serveRequests(clock, owners, () => 3);
    assert.equal(await first, null, 'the superseded exchange is discarded unread');
    const reply = await second;
    assert.equal(new DataView(reply.buffer).getBigUint64(64, true), 2n);
});

test('a probe the master has not served is never overwritten: the next probe resolves null and the session continues', async () => {
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    const browser = peer(ClockRole.Browser), owners = { [ClockRole.Browser]: browser };
    const first = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n));
    assert.equal(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n)), null, 'not sent, no throw');
    clockRing.serveRequests(clock, owners, () => 2);            // the master finally serves the original request
    const reply = await first;
    assert.equal(new DataView(reply.buffer).getBigUint64(64, true), 1n, 'the unserved probe was never torn or replaced');
    const next = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 3n));
    clockRing.serveRequests(clock, owners, () => 3);
    assert.equal(new DataView((await next).buffer).getBigUint64(64, true), 3n, 'the slot is usable again');
});

test('an unanswered derivation exchange returns null, a check while it is pending never blocks, and its late answer is consumed', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    assert.equal(clockRing.deriveExchange(clock, () => 1.5, 1), null, 'no master: the bounded wait times out');
    // Murdoch-2 M2: the slot still holds the unserved exchange, so the next check returns at once instead of
    // blocking the Simulation event loop for another timeout.
    const started = performance.now();
    assert.equal(clockRing.deriveExchange(clock, () => 1.5, 500), null, 'a pending exchange is not re-posted');
    assert.ok(performance.now() - started < 100, 'the pending check did not wait for its timeout');
    const master = await startMaster(buffer, {});
    try {
        // The master serves the stale request once it starts; the following exchange consumes that late answer
        // unread and posts its own.
        let exchange = null;
        for (let attempt = 0; attempt < 200 && !exchange; attempt++) {
            exchange = clockRing.deriveExchange(clock, () => 1.5, 50);
            if (!exchange) await new Promise(resolve => setTimeout(resolve, 5));
        }
        assert.ok(exchange && exchange.master > 1.5 && exchange.epoch === 0, 'the next exchange is answered for itself');
    } finally { await master.terminate(); }
});

// Blind hunter 10 (Story 9.4b final round): a requester's wait for its reply is bounded by the mapping's probe lifetime,
// so a master that never answers releases the waiter (null: the mapping expires the probe) instead of leaking it.
test('a reply wait ends after the probe lifetime when the master never answers', async () => {
    const clock = clockRing.attachClockRing(clockRing.createClockRing());
    const browser = peer(ClockRole.Browser);
    assert.equal(clockRing.ReplyWaitMilliseconds, 250);
    const started = performance.now();
    assert.equal(await clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 1n)), null);
    const waited = performance.now() - started;
    assert.ok(waited >= 240 && waited < 1000, `released after the lifetime (${waited} ms)`);
    // The unserved request still owns the slot; once the master serves it the next probe proceeds.
    clockRing.serveRequests(clock, { [ClockRole.Browser]: browser }, () => 2);
    const next = clockRing.requestReply(clock, ClockRole.Browser, probe(browser, 2n));
    clockRing.serveRequests(clock, { [ClockRole.Browser]: browser }, () => 3);
    assert.equal(new DataView((await next).buffer).getBigUint64(64, true), 2n);
});

// Edge-case E1 (Story 9.4b final round): one startup exchange that times out leaves the slot Requested. The startup loop
// must wait for that stale request rather than count an attempt at once, so a master stalled for 40 ms costs about
// 40 ms / timeout attempts, never the whole 500-attempt budget in microseconds.
test('startup qualification waits out a stalled master instead of burning its attempt budget', async () => {
    const buffer = clockRing.createClockRing(), clock = clockRing.attachClockRing(buffer);
    const stalled = new Worker(`
const { workerData } = require('node:worker_threads');
import(workerData.ringModule).then(ring => {
    const clock = ring.attachClockRing(workerData.buffer);
    let now = 1.5;
    setTimeout(() => { for (;;) ring.awaitRequest(clock, ring.serveRequests(clock, {}, () => (now += 0.001))); }, 40);
});`, { eval: true, workerData: { buffer, ringModule: new URL('../CuriousContraptions.web/wwwroot/clock-ring.js', import.meta.url).href } });
    stalled.unref();
    try {
        let qualified = 0, missed = 0;
        const started = performance.now();
        clockRing.qualifyDerivation(clock, () => performance.now(), 2, {
            qualified: () => qualified >= 3,
            qualify: exchange => { assert.ok(exchange.master > 1.5); qualified++; },
            missed: () => { if (++missed > 500) throw new Error('the attempt budget burned'); },
        });
        const elapsed = performance.now() - started;
        assert.equal(qualified, 3);
        assert.ok(elapsed >= 30, `the stall was waited out (${elapsed} ms)`);
        assert.ok(missed <= elapsed / 2 + 2, `${missed} missed attempts in ${elapsed} ms: each cost a full timeout`);
    } finally { await stalled.terminate(); }
});
