import * as poseRing from '../../CuriousContraptions.web/wwwroot/pose-ring.js';
import { pathToFileURL } from 'node:url';
import type { TestContext } from 'node:test';
import type { ClockRing } from './worker-contracts.js';
type ClientApi = typeof import('../../CuriousContraptions.web/wwwroot/workshop-client.js');
interface Timer { callback(): void; delay: number; }
interface Posted { clockRing?: SharedArrayBuffer; animationControl?: Uint8Array; [key: string]: unknown; }
interface HarnessWorker { terminated: boolean; onmessage(event: { data: unknown }): void; onerror(event: { message: string }): void; onmessageerror(event: unknown): void; }
type Harness = Awaited<ReturnType<typeof clientHarness>>;
// Deterministic controls for the actual JS client at its native-clock, Worker,
// timer and managed-delegate boundaries. No browser/GPU qualification is claimed.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import nodeTest from 'node:test';
// Every case is bounded: a harness that can never settle (a creation that fails before its workers exist, a leaked port or timer)
// fails here within the bound instead of hanging the run.
const TestBoundMilliseconds = 10_000;
const test = (name: string, body: (context: TestContext) => void | Promise<void>) => nodeTest(name, { timeout: TestBoundMilliseconds }, body);
import vm from 'node:vm';
import * as clockRing from '../../CuriousContraptions.web/wwwroot/clock-ring.js';
import { EventRingCursor, eventDelivery } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

const path = pathToFileURL('CuriousContraptions.web/wwwroot/workshop-client.js');
const source = readFileSync(path, 'utf8');
// These integers are the C# WorkshopTransportState values at the JS import boundary.
const state = Object.freeze({ Ready: 0, Backpressure: 1, TimedOut: 2, Indeterminate: 3, RecoveryBlocked: 4 });

async function clientHarness(loadRuntimes = true, failAnimationConstructor = false, captureMode = 1) {
    const intervals = new Map<number, Timer>(), timers = new Map<number, Timer>(), workers: HarnessWorker[] = [], errors: unknown[][] = [], probes: number[] = [], messages: Posted[] = [];
    let timerId = 0, milliseconds = 0, publishedId = 0, calls = 0, outputCalls = 0, clockReplies = 0, simulationRuns = true;
    let outputCallback: (() => void) | undefined, resolveWorker!: () => void;
    // Every clock reply wait the client starts (the real ring's Atomics.waitAsync). A test awaits these waits themselves, not a
    // wall-clock pause: the client's own continuation is attached first, so once a wait has settled its reply was delivered
    // or dropped.
    const replyWaits: Promise<Uint8Array | null>[] = [];
    const workerCreated = new Promise<void>(resolve => { resolveWorker = resolve; });
    const profile = new Float64Array([2, 1, 1, 0, 0.001, 1, 1]);
    let isolationFailure: Error | undefined;
    const context = {
        Uint8Array, Float64Array, DataView, URL, Map, Error, Array, Promise, Math,
        SharedArrayBuffer, BigInt64Array, Float32Array, Atomics,
        createClockRing: clockRing.createClockRing, attachClockRing: clockRing.attachClockRing, ClockRole: clockRing.ClockRole,
        requestReply: (...request: Parameters<typeof clockRing.requestReply>) => { const wait = clockRing.requestReply(...request); replyWaits.push(wait); return wait; },
        heartbeat: clockRing.heartbeat, EventRingCursor, eventDelivery,
        document: { baseURI: 'http://127.0.0.1:8060/', hidden: false },
        nativeProfile: 2, nativeNow: () => milliseconds,
        nativeClockEvidence: () => Array.from(profile), admitNativeClock: async () => {},
        requireIsolation: () => { if (isolationFailure) throw isolationFailure; }, watchIsolation: () => () => {}, isolationQualified: () => {},
        isolationEvidence: () => ({}), stopIsolation: () => {},
        console: { error: (...args: unknown[]) => errors.push(args) },
        setInterval: (callback: () => void, delay: number) => {
            const id = ++timerId; intervals.set(id, { callback, delay }); return id;
        },
        clearInterval: (id: number) => intervals.delete(id),
        setTimeout: (callback: () => void, delay: number) => {
            const id = ++timerId; timers.set(id, { callback, delay }); return id;
        },
        clearTimeout: (id: number) => timers.delete(id),
        MessageChannel: class { port1: { close(): void }; port2: { close(): void }; constructor() { this.port1 = { close() {} }; this.port2 = { close() {} }; } },
        Worker: class implements HarnessWorker {
            terminated = false;
            onmessage: HarnessWorker['onmessage'] = () => {};
            onerror: HarnessWorker['onerror'] = () => {};
            onmessageerror: HarnessWorker['onmessageerror'] = () => {};
            constructor() { if (failAnimationConstructor && workers.length === 1) throw new Error('Animation constructor failed'); workers.push(this); resolveWorker(); }
            postMessage(message: Posted) { messages.push(message); }
            terminate() { this.terminated = true; }
        }
    };
    const realm = vm.createContext(context);
    const module = new vm.SourceTextModule(source, { context: realm });
    await module.link(name => {
        const values: Record<string, unknown> = name.includes('pose-ring') ? poseRing
            : name.includes('clock-ring') ? { ...clockRing, requestReply: context.requestReply }
            : name.includes('event-ring') ? { EventRingCursor, eventDelivery }
            : name.includes('native-clock') ? { admitNativeClock: context.admitNativeClock, nativeNow: context.nativeNow, nativeProfile: context.nativeProfile, nativeClockEvidence: context.nativeClockEvidence }
            : { requireIsolation: context.requireIsolation, watchIsolation: context.watchIsolation, isolationQualified: context.isolationQualified, isolationEvidence: context.isolationEvidence, stopIsolation: context.stopIsolation };
        return new vm.SyntheticModule(Object.keys(values), function () { for (const [key, value] of Object.entries(values)) this.setExport(key, value); }, { context: realm });
    });
    await module.evaluate();
    const api = module.namespace as unknown as ClientApi;
    const session = new Uint8Array(16); session[0] = 1;
    const peer = new Uint8Array(64), view = new DataView(peer.buffer);
    view.setUint32(4, 2, true); peer.set(session, 8); // bytes 0..3: zero padding
    view.setBigUint64(24, 1n, true); view.setBigUint64(40, 1n, true); view.setBigUint64(48, 100000n, true);
    view.setUint32(56, 1, true); view.setUint32(60, 2, true);
    // The managed service sends one owned probe per tick and a stand-in clock master answers it through the ring, as the
    // Simulation-owned master context does; the reply is delivered to the managed callback when the wait resolves.
    let probeSequence = 0n;
    const service = () => {
        calls++; probes.push(publishedId);
        const probe = new Uint8Array(clockRing.ProbeBytes), probeView = new DataView(probe.buffer);
        probe.set(peer, 0); probeView.setBigUint64(64, ++probeSequence, true); probeView.setBigInt64(72, 0n, true);
        api.probe(publishedId, probe);
        const ringBuffer = messages.find(message => message.clockRing)?.clockRing;
        if (!ringBuffer) return;
        const clock = clockRing.attachClockRing(ringBuffer);
        clockRing.serveRequests(clock, { [clockRing.ClockRole.Browser]: peer }, () => clockRing.nanoseconds(milliseconds));
        // A running Simulation beats the ring between its tick turns (worker.js); a hung one stops.
        if (simulationRuns) clockRing.beat(clock);
    };
    const creation = api.create(Object.values(state), session, [2, 64, 72, 96, 1, 2], captureMode, false,
        () => { clockReplies++; }, () => {}, () => {}, () => { outputCalls++; outputCallback?.(); }, service, () => {}, detail => errors.push(['retired', detail]));
    creation.catch(() => {});
    // A creation that rejects before constructing its workers (e.g. a missing module binding) fails the harness with its own error;
    // waiting only for the first Worker left the test pending forever with an idle event loop.
    await Promise.race([workerCreated, creation.then(() => { throw new Error('Creation settled before its workers existed.'); })]);
    await Promise.resolve();
    assert.equal(intervals.size, 0, 'creation must not schedule managed service');
    if (!failAnimationConstructor) assert.equal(workers.length, 2, 'both runtime loads start concurrently');
    assert.equal(timers.size, 0, 'asset loading is outside the qualification timer');
    const runtimeReady = (index: number) => workers[index].onmessage({ data: { awaitingBootstrap: true } });
    if (loadRuntimes && !failAnimationConstructor) { runtimeReady(0); runtimeReady(1); }
    const startup = () => [...timers.values()].find(timer => timer.delay === 5000);
    return {
        api, creation, intervals, timers, workers, errors, probes, messages, runtimeReady,
        failIsolation: (error: Error) => { isolationFailure = error; },
        get startup() { return startup(); },
        onOutput: (callback: () => void) => { outputCallback = callback; },
        calls: () => calls, outputCalls: () => outputCalls, clockReplies: () => clockReplies, publish: (id: number) => { publishedId = id; },
        /** The outcomes of every reply wait, including any started while settling, once each has settled and the client has
         * handled it. */
        replyWaitsSettled: async () => {
            const outcomes = [];
            while (replyWaits.length) outcomes.push(...await Promise.allSettled(replyWaits.splice(0)));
            return outcomes;
        },
        peer,
        /** The stand-in master answers whatever is requested in the ring now (a reply that lands late). */
        serveRing: () => {
            const ringBuffer = messages.find(message => message.clockRing)?.clockRing;
            clockRing.serveRequests(clockRing.attachClockRing(ringBuffer!), { [clockRing.ClockRole.Browser]: peer }, () => clockRing.nanoseconds(milliseconds));
        },
        clock: (value: number) => { milliseconds = value; },
        hangSimulation: () => { simulationRuns = false; },
        ready: async () => {
            workers[0].onmessage({ data: { ready: true, captureMode, nativeClock: profile, masterPeer: peer } });
            return await creation;
        },
        tick: () => {
            assert.equal(intervals.size, 1);
            const interval = [...intervals.values()][0];
            assert.equal(interval.delay, 10);
            interval.callback();
        }
    };
}

test('normal publication activates exactly one service and retains startup qualification', async () => {
    const h = await clientHarness(), id = await h.ready();
    assert.equal(id, 1); h.publish(id); h.api.activate(id); h.tick();
    assert.deepEqual(h.probes, [id]); assert.equal(h.api.status(id), state.Ready);
    assert.equal(h.errors.length, 0);
    assert.equal(h.messages.length, 2, 'the probe travels through the clock ring, never as a Simulation worker message');
    assert.ok([...h.timers.values()].includes(h.startup!));
    h.api.qualified(id);
    assert.ok(![...h.timers.values()].includes(h.startup!));
    h.api.dispose(id);
    assert.equal(h.intervals.size, 0);
});

// Story 9A.1 (F.10): bytes 0..3 of the master peer are zero padding where the clock wire version stood. The retired version 4,
// or any non-zero byte there, is not the authoritative bootstrap and the session is refused; the test above is the zero control.
for (const [label, offset, value] of [['the retired clock wire version 4', 0, 4], ['a non-zero high padding byte', 3, 1]] as const) {
    test('a master peer whose leading padding holds ' + label + ' is refused', async () => {
        const h = await clientHarness();
        h.peer[offset] = value;
        await assert.rejects(h.ready(), /Invalid authoritative master bootstrap/);
    });
}

test('delayed managed publication has no callback until explicit activation', async () => {
    const h = await clientHarness(), id = await h.ready();
    await Promise.resolve(); await Promise.resolve();
    assert.equal(h.intervals.size, 0);
    assert.equal(h.calls(), 0); assert.deepEqual(h.probes, []);
    h.publish(id); h.api.activate(id); h.tick();
    assert.deepEqual(h.probes, [id]); assert.equal(h.errors.length, 0);
    h.api.dispose(id);
});

test('default and missing IDs reject without scheduling or changing the ready owner', async () => {
    const h = await clientHarness(), id = await h.ready();
    for (const invalid of [0, 2]) assert.throws(() => h.api.activate(invalid), /disposed/);
    assert.equal(h.intervals.size, 0); assert.equal(h.api.status(id), state.Ready);
    h.publish(id); h.api.activate(id); h.tick();
    assert.deepEqual(h.probes, [id]); h.api.dispose(id);
});

test('pre-ready activation rejects atomically and subsequent legitimate activation succeeds', async () => {
    const h = await clientHarness();
    assert.throws(() => h.api.activate(1), /one ready owner/);
    assert.equal(h.intervals.size, 0); assert.equal(h.calls(), 0);
    const id = await h.ready(); h.publish(id); h.api.activate(id); h.tick();
    assert.deepEqual(h.probes, [id]); h.api.dispose(id);
});

test('duplicate activation preserves the one original timer and owner state', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    const timer = [...h.intervals.values()][0];
    assert.throws(() => h.api.activate(id), /one ready owner/);
    assert.equal(h.intervals.size, 1); assert.equal([...h.intervals.values()][0], timer);
    h.tick(); assert.deepEqual(h.probes, [id]); assert.equal(h.api.status(id), state.Ready);
    h.api.dispose(id);
});

test('startup timeout before worker readiness rejects creation and activation', async () => {
    const h = await clientHarness();
    h.startup!.callback();
    await assert.rejects(h.creation, /StartupFailed/);
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.equal(h.workers[0].terminated, true);
    assert.throws(() => h.api.activate(1), /disposed/);
});

test('startup timeout after JS return remains anchored to both runtime readiness', async () => {
    const h = await clientHarness(), id = await h.ready();
    assert.ok([...h.timers.values()].includes(h.startup!));
    h.startup!.callback();
    assert.equal(h.calls(), 0); assert.equal(h.api.status(id), state.Indeterminate);
    assert.throws(() => h.api.activate(id), /StartupFailed/);
    assert.equal(h.intervals.size, 0); h.api.dispose(id);
});

test('worker failure rejects activation without invoking managed service', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.workers[0].onerror({ message: 'controlled worker failure' });
    assert.throws(() => h.api.activate(id), /controlled worker failure/);
    assert.equal(h.intervals.size, 0); assert.equal(h.calls(), 0);
    h.api.dispose(id);
});

test('disposal before managed publication prevents later activation', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.api.dispose(id); h.publish(id);
    assert.throws(() => h.api.activate(id), /disposed/);
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.equal(h.workers[0].terminated, true);
});

test('disposal after activation makes a captured late callback inert', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    const late = [...h.intervals.values()][0].callback;
    h.api.dispose(id); late();
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.throws(() => h.api.activate(id), /disposed/);
    assert.equal(h.errors.length, 0);
});

test('failure after activation makes a captured late callback inert', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    const late = [...h.intervals.values()][0].callback;
    h.startup!.callback(); late();
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.throws(() => h.api.activate(id), /StartupFailed/); h.api.dispose(id);
});

// Murdoch-2 R2-F2 (Story 9.4b): the BH10 cleanup, proved. A failed client holds no clock ring, and a clock reply that the
// master writes after the failure (the probe was already waiting) is never delivered to the retired managed client.
function ownedProbe(h: Harness, sequence: number) {
    const probe = new Uint8Array(clockRing.ProbeBytes), view = new DataView(probe.buffer);
    probe.set(h.peer, 0); view.setBigUint64(64, BigInt(sequence), true); view.setBigInt64(72, 0n, true);
    return probe;
}
test('a failed client detaches its clock ring', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    assert.doesNotThrow(() => h.api.probe(id, ownedProbe(h, 1)));
    h.workers[0].onerror({ message: 'controlled worker failure' });
    assert.throws(() => h.api.probe(id, ownedProbe(h, 2)), /clock ring|failure|retired/);
    h.api.dispose(id);
});
/** Exactly one reply wait was pending, and it settled with the master's answer (not the 250 ms release, which is null). */
function assertOneAnsweredWait(outcomes: PromiseSettledResult<Uint8Array | null>[], label: string) {
    assert.equal(outcomes.length, 1, `${label}: exactly one reply wait was pending`);
    assert.equal(outcomes[0].status, 'fulfilled', `${label}: the wait settled without error`);
    assert.notEqual(outcomes[0].value, null, `${label}: the master answered it`);
}
test('a clock reply landing after the client failed is never delivered', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    h.tick();
    assertOneAnsweredWait(await h.replyWaitsSettled(), 'control');
    assert.equal(h.clockReplies(), 1, 'control: an answered probe is delivered');
    h.api.probe(id, ownedProbe(h, 100)); // the master has not answered it yet
    h.workers[0].onerror({ message: 'controlled worker failure' });
    h.serveRing(); // the master answers the waiting probe after the failure
    assertOneAnsweredWait(await h.replyWaitsSettled(), 'after the failure');
    assert.equal(h.clockReplies(), 1, 'the failed client dropped the answered reply');
    assert.deepEqual(h.errors.filter(([kind]) => kind !== 'retired'), []);
    h.api.dispose(id);
});
test('a clock reply landing after disposal is never delivered', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.publish(id); h.api.activate(id);
    h.api.probe(id, ownedProbe(h, 100));
    h.api.dispose(id);
    h.serveRing();
    assertOneAnsweredWait(await h.replyWaitsSettled(), 'after disposal');
    assert.equal(h.clockReplies(), 0, 'the disposed client dropped the answered reply');
});

// Story 9.4b (game-grade envelope): a clock reply missing for longer than the former 1000 ms liveness bound never faults
// the session. The C# mapping recalibrates as counted telemetry (WorkshopClockDerivationTests,
// ALapsedMappingLifetimeIsACountedRecalibration); the service keeps running and keeps probing.
test('a clock reply missing beyond 1000ms never faults the session and the service keeps probing', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.clock(1000); h.publish(id); h.api.activate(id); h.tick();
    assert.equal(h.api.status(id), state.Ready);
    const probes = h.probes.length;
    h.clock(1000.001); h.tick(); h.clock(5000); h.tick();
    assert.equal(h.api.status(id), state.Ready);
    assert.deepEqual(h.errors, []);
    assert.equal(h.intervals.size, 1, 'the service interval still runs');
    assert.ok(h.probes.length > probes, 'the C# service callback keeps probing');
    h.api.dispose(id);
});

// Murdoch-2 H1 (Story 9.4b): a hung Simulation (a spinning solver or a dead context) is not a clock lapse. Its dedicated
// clock master keeps answering, so only the ring heartbeat proves the Simulation's own event loop runs. Silence for more
// than 1000 ms of running Browser service faults the session explicitly with a typed SimulationUnresponsive failure.
function serviceFor(h: Harness, from: number, milliseconds: number) {
    for (let at = from; at <= from + milliseconds && h.intervals.size === 1; at += 10) { h.clock(at); h.tick(); }
}
test('a Simulation heartbeat silent for 1000ms of running service faults the session as SimulationUnresponsive', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.clock(1000); h.publish(id); h.api.activate(id); h.tick();
    serviceFor(h, 1010, 2000); // control: a beating Simulation never faults
    assert.equal(h.api.status(id), state.Ready);
    assert.deepEqual(h.errors, []);
    h.hangSimulation();
    serviceFor(h, 3020, 990);
    assert.equal(h.api.status(id), state.Ready, 'within the bound the session stays ready');
    serviceFor(h, 4020, 30);
    assert.equal(h.api.status(id), state.Indeterminate);
    assert.match(String(h.errors[0][1]), /SimulationUnresponsive/);
    const failure = h.errors[0][1]; assert.equal(typeof failure, 'string');
    assert.match(String(failure), /SimulationUnresponsive/);
    assert.equal(h.intervals.size, 0); h.api.dispose(id);
});

test('a Browser that was itself blocked never mistakes the Simulation for hung', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.clock(1000); h.publish(id); h.api.activate(id); h.tick();
    h.hangSimulation();
    h.clock(2500); h.tick(); // the Browser service did not run for 1.5 s: the liveness window restarts
    serviceFor(h, 2510, 900);
    assert.equal(h.api.status(id), state.Ready);
    assert.deepEqual(h.errors, []);
    h.api.dispose(id);
});


for (const order of [[0, 1], [1, 0]]) {
    test(`both runtimes must be ready before the one qualification deadline (${order})`, async () => {
        const h = await clientHarness(false);
        h.clock(12000); h.runtimeReady(order[0]);
        assert.equal(h.startup, undefined); assert.equal(h.messages.length, 0);
        h.runtimeReady(order[1]);
        assert.equal(h.startup!.delay, 5000); assert.equal(h.messages.length, 1);
        const id = await h.ready();
        h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
        assert.ok(h.messages.at(-1)!.masterPort);
        assert.ok(h.messages.at(-1)!.clockRing instanceof SharedArrayBuffer, 'the Animation runtime receives the session clock ring');
        h.workers[1].onmessage({ data: { ready: true, nativeClock: new Float64Array([2, 1, 1, 0, .001, 1, 1]) } });
        h.workers[1].onmessage({ data: { qualified: true } });
        assert.equal(h.errors.length, 0); h.api.dispose(id);
        assert.ok(h.workers.every(worker => worker.terminated));
    });
}
for (const index of [0, 1]) {
    test(`duplicate runtime readiness rejects worker ${index}`, async () => {
        const h = await clientHarness(false);
        h.runtimeReady(index); h.runtimeReady(index);
        await assert.rejects(h.creation, /Duplicate/);
        assert.ok(h.workers.every(worker => worker.terminated));
        assert.equal(h.startup, undefined);
    });
    test(`early ready rejects worker ${index}`, async () => {
        const h = await clientHarness(false);
        h.workers[index].onmessage({ data: { ready: true } });
        await assert.rejects(h.creation, /Invalid/);
        assert.ok(h.workers.every(worker => worker.terminated));
    });
}
test('early Animation qualification rejects before bootstrap', async () => {
    const h = await clientHarness(false);
    h.workers[1].onmessage({ data: { qualified: true } });
    await assert.rejects(h.creation, /qualification/);
    assert.ok(h.workers.every(worker => worker.terminated));
});
test('duplicate authoritative Animation peer rejects without a second transfer', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
    const count = h.messages.length;
    h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
    assert.equal(h.api.status(id), state.Indeterminate); assert.equal(h.messages.length, count);
    assert.ok(h.workers.every(worker => worker.terminated)); h.api.dispose(id);
});
test('disposal during asset loading prevents late readiness from rearming', async () => {
    const h = await clientHarness(false);
    const late = h.workers.map(worker => worker.onmessage);
    h.api.dispose(1);
    await assert.rejects(h.creation);
    for (const callback of late) callback({ data: { awaitingBootstrap: true } });
    assert.equal(h.startup, undefined); assert.equal(h.messages.length, 0);
    assert.ok(h.workers.every(worker => worker.terminated));
});
test('Animation constructor failure closes the already owned Simulation worker', async () => {
    const h = await clientHarness(false, true);
    await assert.rejects(h.creation, /constructor failed/);
    assert.ok(h.workers.every(worker => worker.terminated));
});

test('premature Animation output fails without delivering to the managed consumer', async () => {
    const h = await clientHarness(false);
    h.workers[1].onmessage({ data: { animationOutput: new Uint8Array(144) } });
    await assert.rejects(h.creation, /precedes admission/);
    assert.equal(h.outputCalls(), 0); assert.ok(h.workers.every(worker => worker.terminated));
});

function commandIdentity(sequence: bigint) {
    // The 8-byte little-endian sequence C# hands the transport with a command (and the worker returns with its acknowledgement).
    const bytes = new Uint8Array(8);
    new DataView(bytes.buffer).setBigUint64(0, sequence, true);
    return bytes;
}
// Story 9A.3: the browser posts no stall flag. The Simulation worker detects a reliable-read stall in its own receipt accounting;
// the browser's command ages drive only the player's Backpressure and TimedOut state.
function stallFlags(h: Harness) { return h.messages.filter(item => Object.hasOwn(item, 'reliableStalled')).length; }

test('delayed valid ACK keeps Backpressure until managed validation completes, and posts no stall flag', async () => {
    const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
    const bytes = commandIdentity(1n), pending = h.api.send(id, bytes, bytes);
    h.clock(50); h.tick(); assert.equal(h.api.status(id), state.Ready, 'exactly 50 ms is not backpressure');
    h.clock(501); h.tick(); assert.equal(h.api.status(id), state.Backpressure);
    h.workers[0].onmessage({ data: { acknowledgement: bytes, sequence: bytes } }); await pending;
    h.tick(); assert.equal(h.api.status(id), state.Backpressure, 'arrival is not validated completion');
    const acknowledgement = h.api.acknowledgement(id, bytes);
    assert.equal(new DataView(acknowledgement.buffer, acknowledgement.byteOffset, acknowledgement.byteLength).getBigUint64(0, true), 1n);
    h.tick(); assert.equal(h.api.status(id), state.Backpressure, 'leased but unvalidated remains unresolved');
    // Represents return from the actual C# decode/cursor/admission boundary, including valid inapplicable receipts.
    h.api.completeAcknowledgement(id, bytes);
    h.tick(); assert.equal(h.api.status(id), state.Ready);
    assert.throws(() => h.api.completeAcknowledgement(id, bytes), /not leased/);
    assert.equal(stallFlags(h), 0, 'no stall flag is ever posted');
    h.api.dispose(id);
});

test('a command older than two seconds is TimedOut for the player, still without a stall flag', async () => {
    const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
    const bytes = commandIdentity(1n), pending = h.api.send(id, bytes, bytes); pending.catch(() => {});
    h.clock(2000); h.tick(); assert.equal(h.api.status(id), state.Backpressure);
    h.clock(2001); h.tick(); assert.equal(h.api.status(id), state.TimedOut);
    assert.equal(stallFlags(h), 0); h.api.dispose(id);
});

test('completing an older ACK cannot clear another aged request or exceed two leases', async () => {
    const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
    const a = commandIdentity(1n), b = commandIdentity(2n), c = commandIdentity(3n);
    const pa = h.api.send(id, a, a), pb = h.api.send(id, b, b);
    h.clock(501); h.tick();
    h.workers[0].onmessage({ data: { acknowledgement: a, sequence: a } }); await pa;
    h.api.acknowledgement(id, a);
    await assert.rejects(h.api.send(id, c, c), /capacity/);
    h.api.completeAcknowledgement(id, a); h.tick(); assert.equal(h.api.status(id), state.Backpressure);
    h.workers[0].onmessage({ data: { acknowledgement: b, sequence: b } }); await pb;
    assert.throws(() => h.api.completeAcknowledgement(id, b), /not leased/);
    h.api.acknowledgement(id, b); h.api.completeAcknowledgement(id, b);
    h.tick(); assert.equal(h.api.status(id), state.Ready);
    assert.equal(stallFlags(h), 0); h.api.dispose(id);
});

test('taken but invalid ACK cannot release capacity or report healthy without completion', async () => {
    const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
    const a = commandIdentity(1n), b = commandIdentity(2n), c = commandIdentity(3n);
    const pa = h.api.send(id, a, a);
    h.clock(501); h.tick();
    h.workers[0].onmessage({ data: { acknowledgement: a, sequence: a } }); await pa;
    h.api.acknowledgement(id, a); // Managed rejection never calls completeAcknowledgement.
    assert.throws(() => h.api.acknowledgement(id, a), /not available/);
    assert.throws(() => h.api.completeAcknowledgement(id, c), /not leased/);
    const pb = h.api.send(id, b, b); pb.catch(() => {});
    await assert.rejects(h.api.send(id, c, c), /capacity/);
    h.clock(502); h.tick(); assert.equal(h.api.status(id), state.Backpressure);
    const handler = h.workers[0].onmessage;
    h.api.dispose(id);
    handler({ data: { acknowledgement: b, sequence: b } });
    assert.throws(() => h.api.completeAcknowledgement(id, a), /disposed/);
    assert.equal(stallFlags(h), 0);
});

for (const duplicate of [false, true]) test(`unmatched or duplicate ACK faults the session (${duplicate})`, async () => {
    const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
    const a = commandIdentity(1n), pending = h.api.send(id, a, a); pending.catch(() => {});
    h.clock(501); h.tick();
    if (duplicate) { h.workers[0].onmessage({ data: { acknowledgement: a, sequence: a } }); await pending; }
    h.workers[0].onmessage({ data: { acknowledgement: duplicate ? a : commandIdentity(2n), sequence: duplicate ? a : commandIdentity(2n) } });
    assert.equal(h.api.status(id), state.Indeterminate);
    assert.equal(h.workers[0].terminated, true);
    assert.equal(stallFlags(h), 0); h.api.dispose(id);
});

for (const retired of ['memoryResult', 'candidateTimedOut'])
    test(`the deleted worker channel ${retired} is an unknown envelope that faults the session`, async () => {
        const h = await clientHarness(), id = await h.ready(); h.publish(id); h.api.activate(id);
        h.workers[0].onmessage({ data: { [retired]: retired === 'memoryResult' ? new Uint8Array(128) : true } });
        assert.equal(h.api.status(id), state.Indeterminate);
        assert.match(String(h.errors.find(entry => entry[0] === 'retired')?.[1]), /Unknown worker transport envelope/);
        h.api.dispose(id);
    });


test('two animation owners send sequential controls only after managed ACK delivery returns', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
    h.workers[1].onmessage({ data: { ready: true, nativeClock: new Float64Array([2, 1, 1, 0, .001, 1, 1]) } });
    h.workers[1].onmessage({ data: { qualified: true } });
    h.api.animationKinds(id, [1, 2, 3]);
    const first = new Uint8Array(144), second = new Uint8Array(144);
    new DataView(first.buffer).setBigUint64(56, 4n, true);
    new DataView(second.buffer).setBigUint64(56, 5n, true);
    h.api.animationControl(id, first);
    h.onOutput(() => assert.throws(() => h.api.animationControl(id, second), /pending/));
    const ack = new Uint8Array(144); new DataView(ack.buffer).setUint32(60, 1, true);
    h.workers[1].onmessage({ data: { animationOutput: ack } });
    assert.equal(h.errors.length, 0);
    h.onOutput(() => {}); h.api.animationControl(id, second);
    h.workers[1].onmessage({ data: { animationOutput: ack } });
    assert.equal(h.messages.filter(message => message.animationControl).length, 2);
    assert.equal(h.errors.length, 0); h.api.dispose(id);
});

// Story 9A.44 (D7): the Animation worker writes the channel value as f32 at 56 and the property as u16 at 136; the client records
// exactly those, so a value no half-precision lane holds (0.34) and the declared property are read back unchanged.
test('an Animation sample records the f32 channel value at 56 and the u16 property at 136', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
    h.workers[1].onmessage({ data: { ready: true, nativeClock: new Float64Array([2, 1, 1, 0, .001, 1, 1]) } });
    h.workers[1].onmessage({ data: { qualified: true } });
    h.api.animationKinds(id, [1, 2, 3]);
    const sample = new Uint8Array(144), view = new DataView(sample.buffer);
    view.setUint32(60, 2, true);            // the sample kind (the second declared output kind)
    view.setBigUint64(64, 7n, true);        // its target
    view.setBigUint64(40, 9n, true);        // its ordinal
    view.setFloat32(56, 0.34, true);        // the f32 channel value
    view.setUint16(136, 5, true);           // the animated property
    h.onOutput(() => {
        const recorded = h.api.eventBytes(id);
        const value = new DataView(recorded.buffer, recorded.byteOffset, recorded.byteLength);
        assert.equal(value.getFloat32(56, true), Math.fround(0.34), 'f32 reaches managed admission');
        assert.equal(value.getUint16(136, true), 5, 'property reaches managed admission');
        assert.equal(value.getBigUint64(40, true), 9n);
    });
    h.workers[1].onmessage({ data: { animationOutput: sample } });
    assert.equal(h.outputCalls(), 1);
    assert.equal(h.messages.filter(message => message.animationAcknowledged).length, 1);
    assert.equal(h.errors.length, 0); h.api.dispose(id);
});

test('transport retirement retains error name message stack and nested cause', async () => {
    const h = await clientHarness(), id = await h.ready();
    const cause = new Error('underlying transport cause'); cause.name = 'TransportCause'; cause.stack = 'cause-stack-marker';
    const error = new Error('outer transport failure', { cause }); error.name = 'TransportFailure'; error.stack = 'outer-stack-marker';
    h.failIsolation(error);
    assert.throws(() => h.api.now(), /outer transport failure/);
    const detail = h.errors.find(entry => entry[0] === 'retired')?.[1];
    assert.equal(typeof detail, 'string');
    for (const fragment of ['TransportFailure', 'outer transport failure', 'outer-stack-marker',
        'TransportCause', 'underlying transport cause', 'cause-stack-marker'])
        assert.ok(String(detail).includes(fragment), fragment);
    h.api.dispose(id);
});
