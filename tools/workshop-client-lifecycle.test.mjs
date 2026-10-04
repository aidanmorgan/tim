// Deterministic controls for the actual JS client at its native-clock, Worker,
// timer and managed-delegate boundaries. No browser/GPU qualification is claimed.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const path = new URL('../CuriousContraptions.web/wwwroot/workshop-client.js', import.meta.url);
const source = readFileSync(path, 'utf8');
const executable = source.replace(/^import .*;\n/gm, '').replace(/^export /gm, '') +
    '\n;({create, activate, probe, status, dispose, qualified});';
// These integers are the C# WorkshopTransportState values at the JS import boundary.
const state = Object.freeze({ Ready: 0, Backpressure: 1, TimedOut: 2, Indeterminate: 3, RecoveryBlocked: 4 });

async function clientHarness(loadRuntimes = true, failAnimationConstructor = false) {
    const intervals = new Map(), timers = new Map(), workers = [], errors = [], probes = [], messages = [];
    let timerId = 0, milliseconds = 0, publishedId = 0, calls = 0, outputCalls = 0, resolveWorker;
    const workerCreated = new Promise(resolve => { resolveWorker = resolve; });
    const profile = new Float64Array([2, 1, 1, 0, 0.001, 1, 1]);
    const context = {
        Uint8Array, Float64Array, DataView, URL, Map, Error, Array, Promise, Math,
        document: { baseURI: 'http://127.0.0.1:8060/', hidden: false },
        nativeProfile: 2, nativeNow: () => milliseconds,
        nativeClockEvidence: () => Array.from(profile), admitNativeClock: async () => {},
        requireIsolation: () => {}, watchIsolation: () => () => {}, isolationQualified: () => {},
        isolationEvidence: () => ({}), stopIsolation: () => {},
        console: { error: (...args) => errors.push(args) },
        setInterval: (callback, delay) => {
            const id = ++timerId; intervals.set(id, { callback, delay }); return id;
        },
        clearInterval: id => intervals.delete(id),
        setTimeout: (callback, delay) => {
            const id = ++timerId; timers.set(id, { callback, delay }); return id;
        },
        clearTimeout: id => timers.delete(id),
        MessageChannel: class { constructor() { this.port1 = { close() {} }; this.port2 = { close() {} }; } },
        Worker: class {
            constructor() { if (failAnimationConstructor && workers.length === 1) throw new Error('Animation constructor failed'); workers.push(this); resolveWorker(); }
            postMessage(message) { messages.push(message); }
            terminate() { this.terminated = true; }
        }
    };
    const api = vm.runInNewContext(executable, context, { filename: path.pathname });
    const session = new Uint8Array(16); session[0] = 1;
    const peer = new Uint8Array(64), view = new DataView(peer.buffer);
    view.setUint32(0, 2, true); view.setUint32(4, 2, true); peer.set(session, 8);
    view.setBigUint64(24, 1n, true); view.setBigUint64(40, 1n, true); view.setBigUint64(48, 100000n, true);
    view.setUint32(56, 1, true); view.setUint32(60, 2, true);
    const service = () => { calls++; probes.push(publishedId); api.probe(publishedId, new Uint8Array([1])); };
    const creation = api.create(Object.values(state), session, [2, 2, 64, 72, 96, 160, 1, 2], 1, false,
        () => {}, () => {}, () => {}, () => { outputCalls++; }, service, () => {}, () => {}, () => {});
    creation.catch(() => {});
    await workerCreated;
    await Promise.resolve();
    assert.equal(intervals.size, 0, 'creation must not schedule managed service');
    if (failAnimationConstructor) return { creation, workers };
    assert.equal(workers.length, 2, 'both runtime loads start concurrently');
    assert.equal(timers.size, 0, 'asset loading is outside the qualification timer');
    const runtimeReady = index => workers[index].onmessage({ data: { awaitingBootstrap: true } });
    if (loadRuntimes) { runtimeReady(0); runtimeReady(1); }
    const startup = () => [...timers.values()].find(timer => timer.delay === 5000);
    return {
        api, creation, intervals, timers, workers, errors, probes, messages, runtimeReady,
        get startup() { return startup(); },
        calls: () => calls, outputCalls: () => outputCalls, publish: id => { publishedId = id; },
        clock: value => { milliseconds = value; },
        ready: async () => {
            workers[0].onmessage({ data: { ready: true, captureMode: 1, nativeClock: profile, masterPeer: peer } });
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
    assert.equal(h.errors.length, 0); assert.equal(h.messages.length, 3);
    assert.ok([...h.timers.values()].includes(h.startup));
    h.api.qualified(id);
    assert.ok(![...h.timers.values()].includes(h.startup));
    h.api.dispose(id);
    assert.equal(h.intervals.size, 0);
});

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
    h.startup.callback();
    await assert.rejects(h.creation, /StartupFailed/);
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.equal(h.workers[0].terminated, true);
    assert.throws(() => h.api.activate(1), /disposed/);
});

test('startup timeout after JS return remains anchored to both runtime readiness', async () => {
    const h = await clientHarness(), id = await h.ready();
    assert.ok([...h.timers.values()].includes(h.startup));
    h.startup.callback();
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
    h.startup.callback(); late();
    assert.equal(h.calls(), 0); assert.equal(h.intervals.size, 0);
    assert.throws(() => h.api.activate(id), /StartupFailed/); h.api.dispose(id);
});

test('activation does not reset the 1000ms worker liveness boundary', async () => {
    const h = await clientHarness(), id = await h.ready();
    h.clock(1000); h.publish(id); h.api.activate(id); h.tick();
    assert.equal(h.api.status(id), state.Ready);
    h.clock(1000.001); h.tick();
    assert.equal(h.api.status(id), state.Indeterminate);
    assert.match(h.errors[0][2].message, /1000ms/);
    assert.equal(h.intervals.size, 0); h.api.dispose(id);
});


for (const order of [[0, 1], [1, 0]]) {
    test(`both runtimes must be ready before the one qualification deadline (${order})`, async () => {
        const h = await clientHarness(false);
        h.clock(12000); h.runtimeReady(order[0]);
        assert.equal(h.startup, undefined); assert.equal(h.messages.length, 0);
        h.runtimeReady(order[1]);
        assert.equal(h.startup.delay, 5000); assert.equal(h.messages.length, 1);
        const id = await h.ready();
        h.workers[0].onmessage({ data: { animationPeer: new Uint8Array(64) } });
        assert.ok(h.messages.at(-1).clockPort);
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
    h.workers[1].onmessage({ data: { animationOutput: new Uint8Array(96) } });
    await assert.rejects(h.creation, /precedes admission/);
    assert.equal(h.outputCalls(), 0); assert.ok(h.workers.every(worker => worker.terminated));
});
