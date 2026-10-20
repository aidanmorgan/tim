// Session clock ring: the typed ClockProbe/ClockReply query (WorkshopClockWire) between each requester and the
// Simulation-owned clock master context, through one SharedArrayBuffer with one request/reply slot per role.
// The master context never runs solver work, so a probe is stamped when it arrives, not when the Simulation's event
// loop next becomes free (Story 9.4b R5). The reply is the receipt: it carries the probe's own session, generation,
// role and sequence, plus the master derivation epoch its stamps were taken under. Each slot owns at most one
// outstanding exchange; a reply is consumed exactly once.
//
// Header (32 bytes): doorbell, zero padding, derivation seqlock word (odd while the Simulation writes), derivation
// epoch, Simulation heartbeat (advanced by the Simulation's own event loop between tick turns), reserved, and the f64
// derivation correction (milliseconds, as raw bits) the master adds to every stamp.
export const ClockRingBytes = 1024;
export const PeerBytes = 64, ProbeBytes = 80, ReplyBytes = 104;
// A requester waits for its reply at most WorkshopClockMapping.ProbeLifetime (250 ms): the mapping expires the probe
// then, so the wait is released instead of lingering after a failure or disposal (pinned by WorkerAbiTests).
export const ReplyWaitMilliseconds = 250;
export const SlotState = Object.freeze({ Idle: 0, Requested: 1, Replied: 2, Rejected: 3 });
// WorkshopRuntimeRole: Browser and Animation request master time; the Simulation slot carries its derivation check.
export const ClockRole = Object.freeze({ Browser: 1, Simulation: 2, Animation: 3 });

// Int32 word indices; BigInt64 index 3 = bytes 24..31.
const Doorbell = 0, DerivationSequence = 2, DerivationEpoch = 3, Heartbeat = 4, CorrectionBits = 3;
const HeaderBytes = 32, SlotBytes = 256, ProbeOffset = 8, ReplyOffset = 96;
const SlotOrder = Object.freeze({ [ClockRole.Browser]: 0, [ClockRole.Animation]: 1, [ClockRole.Simulation]: 2 });

function slotBase(role) {
    const index = SlotOrder[role];
    if (index === undefined) throw new Error('Unknown clock ring role.');
    return HeaderBytes + index * SlotBytes;
}

export function createClockRing() {
    if (typeof SharedArrayBuffer === 'undefined') throw new Error('The clock ring requires cross-origin isolation.');
    return new SharedArrayBuffer(ClockRingBytes);
}

export function attachClockRing(buffer) {
    if (!(buffer instanceof SharedArrayBuffer) || buffer.byteLength !== ClockRingBytes)
        throw new Error('Unsupported clock ring layout.');
    const words = new Int32Array(buffer), bytes = new Uint8Array(buffer);
    return { buffer, words, bytes, wide: new BigInt64Array(buffer), view: new DataView(buffer) };
}

function stateIndex(role) { return slotBase(role) / 4; }

function ring(bell) {
    Atomics.add(bell.words, Doorbell, 1);
    Atomics.notify(bell.words, Doorbell);
}

function sameProbe(clock, base, probe) {
    for (let i = 0; i < ProbeBytes; i++) if (clock.bytes[base + ProbeOffset + i] !== probe[i]) return false;
    return true;
}

const bitsView = new DataView(new ArrayBuffer(8));
function toBits(value) { bitsView.setFloat64(0, value, true); return bitsView.getBigInt64(0, true); }
function fromBits(bits) { bitsView.setBigInt64(0, bits, true); return bitsView.getFloat64(0, true); }

/** Master: the current derivation, read under the seqlock so a correction and its epoch are never torn apart. */
export function readDerivation(clock) {
    for (;;) {
        const begin = Atomics.load(clock.words, DerivationSequence);
        if ((begin & 1) !== 0) continue;
        const epoch = Atomics.load(clock.words, DerivationEpoch);
        const correction = fromBits(Atomics.load(clock.wide, CorrectionBits));
        if (Atomics.load(clock.words, DerivationSequence) === begin) return { epoch, correction };
    }
}

/**
 * Simulation (the single writer): installs a derivation correction and returns the epoch it is stamped under. A slew
 * keeps the epoch (requester mappings absorb it within their drift budget); a re-derivation advances it, so every
 * requester recalibrates on its next reply.
 */
export function writeDerivation(clock, correction, rederive) {
    if (!Number.isFinite(correction)) throw new Error('Invalid clock derivation correction.');
    if (typeof rederive !== 'boolean') throw new Error('A clock derivation write must say whether it re-derives.');
    const begin = Atomics.load(clock.words, DerivationSequence);
    if ((begin & 1) !== 0) throw new Error('A clock derivation write is already in progress.');
    const epoch = Atomics.load(clock.words, DerivationEpoch);
    if (begin >= 0x7ffffffd || (rederive && epoch >= 0x7fffffff)) throw new Error('Clock derivation sequence exhausted.');
    Atomics.store(clock.words, DerivationSequence, begin + 1);
    Atomics.store(clock.wide, CorrectionBits, toBits(correction));
    if (rederive) Atomics.store(clock.words, DerivationEpoch, epoch + 1);
    Atomics.store(clock.words, DerivationSequence, begin + 2);
    return rederive ? epoch + 1 : epoch;
}

/** Simulation: one liveness beat from its own event loop, between tick turns. */
export function beat(clock) { Atomics.add(clock.words, Heartbeat, 1); }

/** Browser: the Simulation's liveness count; it stops advancing only if the Simulation's event loop stops running. */
export function heartbeat(clock) { return Atomics.load(clock.words, Heartbeat); }

/**
 * Requester: posts one probe and resolves with its reply bytes, consuming the slot, or with null when the probe was not
 * answered as sent. Null covers two cases. A newer probe of the same requester superseded it (its mapping expired the
 * old probe; the stale answer is discarded unread). Or the master has not yet served the previous probe, so this one
 * is not written at all: an unserved request is never overwritten (the master may be reading it), and the requester's
 * mapping times the unsent probe out, then recovers or recalibrates as counted telemetry (game-grade envelope). A
 * rejected probe does not own this session clock: that is a protocol violation, never a lapse, and fails explicitly.
 */
export function requestReply(clock, role, probe) {
    if (role === ClockRole.Simulation) throw new Error('The Simulation slot carries no probe.');
    if (!(probe instanceof Uint8Array) || probe.length !== ProbeBytes) throw new Error('Invalid clock probe width.');
    const state = stateIndex(role), base = slotBase(role);
    const previous = Atomics.load(clock.words, state);
    if (previous === SlotState.Rejected) throw new Error('Clock master rejected a probe that does not own this session clock.');
    if (previous === SlotState.Requested) return Promise.resolve(null);
    clock.bytes.set(probe, base + ProbeOffset);
    Atomics.store(clock.words, state, SlotState.Requested);
    ring(clock);
    const owned = probe.slice();
    const settle = () => {
        if (!sameProbe(clock, base, owned)) return null;
        const outcome = Atomics.load(clock.words, state);
        if (outcome === SlotState.Replied) {
            const reply = clock.bytes.slice(base + ReplyOffset, base + ReplyOffset + ReplyBytes);
            Atomics.store(clock.words, state, SlotState.Idle);
            return reply;
        }
        throw new Error(outcome === SlotState.Rejected ? 'Clock master rejected a probe that does not own this session clock.'
            : 'Clock slot left its owned exchange.');
    };
    const wait = Atomics.waitAsync(clock.words, state, SlotState.Requested, ReplyWaitMilliseconds);
    return wait.async ? wait.value.then(result => result === 'timed-out' && Atomics.load(clock.words, state) === SlotState.Requested
        ? null : settle()) : Promise.resolve().then(settle);
}

/** Exact port of WorkshopNativeClock.FromMilliseconds: round-half-even integer nanoseconds of an IEEE double. */
export function nanoseconds(milliseconds) {
    if (!Number.isFinite(milliseconds) || milliseconds < 0) throw new RangeError('Invalid native milliseconds.');
    const view = new DataView(new ArrayBuffer(8));
    view.setFloat64(0, milliseconds, true);
    const bits = view.getBigUint64(0, true);
    const exponent = Number((bits >> 52n) & 0x7ffn);
    let significand = bits & 0xfffffffffffffn;
    if (exponent !== 0) significand |= 1n << 52n;
    if (significand === 0n) return 0n;
    const power = exponent === 0 ? -1074 : exponent - 1023 - 52;
    if (power >= 0) throw new RangeError('Native milliseconds exceed the nanosecond range.');
    const shift = BigInt(-power);
    if (shift >= 128n) return 0n;
    const numerator = significand * 1000000n, denominator = 1n << shift;
    let quotient = numerator >> shift;
    const twice = (numerator & (denominator - 1n)) * 2n;
    if (twice > denominator || (twice === denominator && (quotient & 1n) !== 0n)) quotient++;
    if (quotient > 0x7fffffffffffffffn) throw new RangeError('Native milliseconds exceed the nanosecond range.');
    return quotient;
}

/** Master: the checks of WorkshopClockWire.DecodeProbe against the expected peer of that slot's role. */
export function probeOwnsPeer(probe, peer) {
    if (probe.length !== ProbeBytes || peer.length !== PeerBytes) return false;
    for (let i = 0; i < PeerBytes; i++) if (probe[i] !== peer[i]) return false;
    const view = new DataView(probe.buffer, probe.byteOffset, probe.byteLength);
    return view.getBigUint64(64, true) !== 0n && view.getBigInt64(72, true) >= 0n;
}

/** Master: WorkshopClockWire.EncodeReply. The probe is copied unchanged, then the two master stamps and their epoch. */
export function encodeReply(probe, peer, received, sent, epoch) {
    const origin = new DataView(peer.buffer, peer.byteOffset, peer.byteLength).getBigInt64(32, true);
    if (!probeOwnsPeer(probe, peer) || received < origin || sent < received || !Number.isSafeInteger(epoch) || epoch < 0)
        return null;
    const reply = new Uint8Array(ReplyBytes);
    reply.set(probe, 0);
    const view = new DataView(reply.buffer);
    view.setBigInt64(80, received, true);
    view.setBigInt64(88, sent, true);
    view.setBigUint64(96, BigInt(epoch), true);
    return reply;
}

/**
 * Master: serves every requested slot once. `derivedNow` returns the master-domain reading before correction, in
 * native milliseconds; each served slot is stamped under one read of the derivation. `peers` maps each requester
 * role to its expected peer bytes. Returns the doorbell value the scan started from.
 */
export function serveRequests(clock, peers, derivedNow) {
    const seen = Atomics.load(clock.words, Doorbell);
    for (const role of [ClockRole.Browser, ClockRole.Animation, ClockRole.Simulation]) {
        const state = stateIndex(role), base = slotBase(role);
        if (Atomics.load(clock.words, state) !== SlotState.Requested) continue;
        const { epoch, correction } = readDerivation(clock);
        const masterNow = () => {
            const value = derivedNow() + correction;
            if (!Number.isFinite(value) || value < 0) throw new Error('Clock master reading precedes the Simulation origin.');
            return value;
        };
        if (role === ClockRole.Simulation) {
            clock.view.setFloat64(base + ReplyOffset, masterNow(), true);
            clock.view.setBigUint64(base + ReplyOffset + 8, BigInt(epoch), true);
            Atomics.store(clock.words, state, SlotState.Replied);
        } else {
            const received = nanoseconds(masterNow());
            const probe = clock.bytes.slice(base + ProbeOffset, base + ProbeOffset + ProbeBytes);
            const reply = encodeReply(probe, peers[role], received, nanoseconds(masterNow()), epoch);
            if (reply) clock.bytes.set(reply, base + ReplyOffset);
            Atomics.store(clock.words, state, reply ? SlotState.Replied : SlotState.Rejected);
        }
        Atomics.notify(clock.words, state);
    }
    return seen;
}

/** Master: blocks until a requester rings after `seen`. Only a dedicated clock context may block. */
export function awaitRequest(clock, seen) {
    Atomics.wait(clock.words, Doorbell, seen);
}

/**
 * Simulation startup: runs derivation exchanges until `host.qualified()` (WorkshopClockDerivation.Qualify through the
 * host). Every missed attempt costs a full timeout of real waiting: an exchange that timed out leaves its request in the
 * slot, and the next attempt waits for the master to serve that stale request (or times out again) instead of counting
 * a miss at once, so the attempt budget measures time (Story 9.4b final round, edge-case E1).
 */
export function qualifyDerivation(clock, now, timeoutMilliseconds, host) {
    const state = stateIndex(ClockRole.Simulation);
    while (!host.qualified()) {
        if (Atomics.load(clock.words, state) === SlotState.Requested) {
            if (Atomics.wait(clock.words, state, SlotState.Requested, timeoutMilliseconds) === 'timed-out') host.missed();
            continue; // served late: the next exchange consumes that answer unread and posts its own
        }
        const exchange = deriveExchange(clock, now, timeoutMilliseconds);
        if (exchange) host.qualify(exchange); else host.missed();
    }
}

/**
 * Simulation: one synchronous derivation exchange, bounded by `timeoutMilliseconds`. Returns the master reading, the
 * Simulation's own [before, after] bracket in native milliseconds and the derivation epoch the master stamped under,
 * or null when the master did not answer in time (its slot is then left to the master, never rewritten). An earlier
 * exchange still unanswered makes this one return null at once, so a check blocks the Simulation's event loop for at
 * most one timeout.
 */
export function deriveExchange(clock, now, timeoutMilliseconds) {
    const state = stateIndex(ClockRole.Simulation), base = slotBase(ClockRole.Simulation);
    if (Atomics.load(clock.words, state) === SlotState.Requested) return null;
    // A late answer to an exchange that timed out is consumed unread before a new exchange is posted.
    if (Atomics.load(clock.words, state) === SlotState.Replied) Atomics.store(clock.words, state, SlotState.Idle);
    if (Atomics.load(clock.words, state) !== SlotState.Idle) throw new Error('Clock derivation slot left its owned exchange.');
    const before = now();
    Atomics.store(clock.words, state, SlotState.Requested);
    ring(clock);
    if (Atomics.wait(clock.words, state, SlotState.Requested, timeoutMilliseconds) === 'timed-out') return null;
    const after = now();
    if (Atomics.load(clock.words, state) !== SlotState.Replied) throw new Error('Clock derivation slot left its owned exchange.');
    const master = clock.view.getFloat64(base + ReplyOffset, true);
    const epoch = Number(clock.view.getBigUint64(base + ReplyOffset + 8, true));
    Atomics.store(clock.words, state, SlotState.Idle);
    return { before, master, after, epoch };
}
