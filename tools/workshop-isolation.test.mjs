// Controls exercise the shipped isolation boundary; no browser qualification claim.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../CuriousContraptions.web/wwwroot/workshop-isolation.js', import.meta.url), 'utf8');
const executable = source.replace(/^export /gm, '') +
    '\n;({prepareIsolation, requireIsolation, watchIsolation, isolationQualified, isolationEvidence});';

function harness({ isolated = true, heldRegistration = false, storage = new Map(), origin = 100000 } = {}) {
    let elapsed = 0, nextTimer = 0, reloads = 0;
    const timers = new Map(), listeners = new Map(), failures = [];
    const controller = {
        scriptURL: 'https://example.test/tim/workshop-isolation-worker.js',
        postMessage(_message, ports) { queueMicrotask(() => ports[0].peer.onmessage({ data: { policyVersion: 1 } })); }
    };
    const serviceWorker = {
        controller,
        addEventListener(name, callback) { listeners.set(name, callback); },
        removeEventListener(name, callback) { if (listeners.get(name) === callback) listeners.delete(name); },
        register: async () => heldRegistration ? await new Promise(() => {}) :
            { scope: 'https://example.test/tim/', update: async () => {}, installing: undefined }
    };
    const api = vm.runInNewContext(executable, {
        URL, Map, Promise, Error, Array, Number, Math, JSON,
        document: { baseURI: 'https://example.test/tim/' }, navigator: { serviceWorker },
        isSecureContext: true, crossOriginIsolated: isolated,
        performance: { timeOrigin: origin, now: () => elapsed },
        sessionStorage: {
            getItem: key => storage.get(key) ?? null,
            setItem: (key, value) => storage.set(key, value),
            removeItem: key => storage.delete(key)
        },
        location: { reload() { reloads++; } },
        MessageChannel: class {
            constructor() {
                this.port1 = { close() {} }; this.port2 = { close() {}, peer: this.port1 };
            }
        },
        setTimeout: callback => { const id = ++nextTimer; timers.set(id, callback); return id; },
        clearTimeout: id => timers.delete(id)
    });
    return {
        api, timers, failures, storage, get reloads() { return reloads; },
        prepare: () => api.prepareIsolation(error => failures.push(error)),
        advance: value => { elapsed = value; },
        expire: () => { for (const callback of [...timers.values()]) callback(); },
        changeController: () => { serviceWorker.controller = {}; listeners.get('controllerchange')?.(); }
    };
}

test('established isolation permits delayed runtime loading and records actual usable time', async () => {
    const h = harness();
    assert.equal(await h.prepare(), true);
    let retired = 0; h.api.watchIsolation(() => retired++);
    assert.equal(h.timers.size, 0, 'the completed isolation deadline has no live timer');
    h.advance(9000); h.expire();
    assert.doesNotThrow(() => h.api.requireIsolation());
    assert.equal(retired, 0); assert.equal(h.failures.length, 0);
    h.api.isolationQualified();
    assert.equal(h.api.isolationEvidence().navigationToUsableMilliseconds, 9000);
    assert.equal(h.api.isolationEvidence().phase, 2);
});

test('incomplete isolation handshake still fails at the existing deadline', async () => {
    const h = harness({ heldRegistration: true });
    const preparing = h.prepare();
    h.advance(5001); h.expire();
    assert.equal(await preparing, false);
    assert.equal(h.failures.length, 1);
    assert.match(h.failures[0].message, /isolation handshake exceeded five seconds/);
    assert.throws(() => h.api.requireIsolation(), /unavailable or changed/);
});

for (const qualified of [false, true]) {
    test('controller replacement retires ' + (qualified ? 'qualified' : 'booting') + ' ownership', async () => {
        const h = harness(); assert.equal(await h.prepare(), true);
        let retired = 0; h.api.watchIsolation(() => retired++);
        if (qualified) h.api.isolationQualified();
        h.advance(9000); h.changeController();
        assert.equal(retired, 1); assert.equal(h.failures.length, 1);
        assert.throws(() => h.api.requireIsolation(), /unavailable or changed/);
    });
}

test('at most one isolation reload remains permitted', async () => {
    const storage = new Map();
    const first = harness({ isolated: false, storage });
    assert.equal(await first.prepare(), false); assert.equal(first.reloads, 1);
    const second = harness({ isolated: false, storage, origin: 101000 });
    assert.equal(await second.prepare(), false); assert.equal(second.reloads, 0);
    assert.match(second.failures[0].message, /isolated reload did not establish isolation/);
});

test('manual navigation after slow runtime loading starts a fresh isolation attempt', async () => {
    const storage = new Map();
    const first = harness({ storage });
    assert.equal(await first.prepare(), true);
    first.advance(9000);
    assert.equal(storage.size, 0, 'completed handshake marker is retired before runtime loading');
    const next = harness({ storage, origin: 110000 });
    assert.equal(await next.prepare(), true);
    assert.equal(next.failures.length, 0);
    assert.equal(next.reloads, 0);
});
