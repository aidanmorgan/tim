// Controls exercise the shipped isolation boundary; no browser qualification claim.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../../CuriousContraptions.web/wwwroot/workshop-isolation.js', import.meta.url), 'utf8');
type IsolationApi = typeof import('../../CuriousContraptions.web/wwwroot/workshop-isolation.js');
interface Port { close(): void; onmessage?: ((event: { data: unknown }) => void) | null; }
interface Options { isolated?: boolean; heldRegistration?: boolean; storage?: Map<string, string>; origin?: number; reply?: unknown; waiting?: unknown; wrongController?: boolean; waitingOnReply?: boolean; initiallyControlled?: boolean; }

async function harness({ isolated = true, heldRegistration = false, storage = new Map(), origin = 100000,
    reply = { policyReady: true }, waiting = undefined, wrongController = false, waitingOnReply = false, initiallyControlled = true }: Options = {}) {
    let elapsed = 0, nextTimer = 0, reloads = 0;
    const registration = { scope: 'https://example.test/tim/', update: async () => {}, installing: undefined, waiting };
    const timers = new Map<number, () => void>(), listeners = new Map<string, Set<() => void>>(), failures: Error[] = [];
    const controller = {
        scriptURL: wrongController ? 'https://example.test/foreign.js' : 'https://example.test/tim/workshop-isolation-worker.js',
        postMessage(message: { policyRequest?: boolean }, ports: { peer: Port }[]) { if (message?.policyRequest === true) queueMicrotask(() => { if (waitingOnReply) registration.waiting = {}; ports[0].peer.onmessage?.({ data: reply }); }); }
    };
    const serviceWorker = {
        controller: (initiallyControlled ? controller : null) as object | null,
        addEventListener(name: string, callback: () => void) { const callbacks = listeners.get(name) ?? new Set<() => void>(); callbacks.add(callback); listeners.set(name, callbacks); },
        removeEventListener(name: string, callback: () => void) { listeners.get(name)?.delete(callback); },
        register: async () => {
            if (heldRegistration) return await new Promise<never>(() => {});
            if (!serviceWorker.controller) queueMicrotask(() => { serviceWorker.controller = controller; for (const callback of listeners.get('controllerchange') ?? []) callback(); });
            return registration;
        }
    };
    const context = vm.createContext({
        URL, Map, Promise, Error, Array, Number, Math, JSON,
        document: { baseURI: 'https://example.test/tim/' }, navigator: { serviceWorker },
        isSecureContext: true, crossOriginIsolated: isolated,
        performance: { timeOrigin: origin, now: () => elapsed },
        sessionStorage: {
            getItem: (key: string) => storage.get(key) ?? null,
            setItem: (key: string, value: string) => storage.set(key, value),
            removeItem: (key: string) => storage.delete(key)
        },
        location: { reload() { reloads++; } },
        MessageChannel: class {
            port1: Port; port2: Port & { peer: Port };
            constructor() {
                this.port1 = { close() {} }; this.port2 = { close() {}, peer: this.port1 };
            }
        },
        setTimeout: (callback: () => void) => { const id = ++nextTimer; timers.set(id, callback); return id; },
        clearTimeout: (id: number) => timers.delete(id)
    });
    const module = new vm.SourceTextModule(source, { context });
    await module.link(() => { throw new Error('Unexpected isolation dependency.'); });
    await module.evaluate();
    const api = module.namespace as unknown as IsolationApi;
    return {
        api, timers, failures, storage, get reloads() { return reloads; },
        prepare: () => api.prepareIsolation(error => failures.push(error instanceof Error ? error : new Error(String(error)))),
        advance: (value: number) => { elapsed = value; },
        expire: () => { for (const callback of [...timers.values()]) callback(); },
        changeController: () => { serviceWorker.controller = {}; for (const callback of listeners.get('controllerchange') ?? []) callback(); }
    };
}

test('established isolation permits delayed runtime loading and records actual usable time', async () => {
    const h = await harness();
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
    const h = await harness({ heldRegistration: true });
    const preparing = h.prepare();
    h.advance(5001); h.expire();
    assert.equal(await preparing, false);
    assert.equal(h.failures.length, 1);
    assert.match(h.failures[0].message, /isolation handshake exceeded five seconds/);
    assert.throws(() => h.api.requireIsolation(), /unavailable or changed/);
});

for (const qualified of [false, true]) {
    test('controller replacement retires ' + (qualified ? 'qualified' : 'booting') + ' ownership', async () => {
        const h = await harness(); assert.equal(await h.prepare(), true);
        let retired = 0; h.api.watchIsolation(() => retired++);
        if (qualified) h.api.isolationQualified();
        h.advance(9000); h.changeController();
        assert.equal(retired, 1); assert.equal(h.failures.length, 1);
        assert.throws(() => h.api.requireIsolation(), /unavailable or changed/);
    });
}

test('at most one isolation reload remains permitted', async () => {
    const storage = new Map();
    const first = await harness({ isolated: false, storage });
    assert.equal(await first.prepare(), false); assert.equal(first.reloads, 1);
    const second = await harness({ isolated: false, storage, origin: 101000 });
    assert.equal(await second.prepare(), false); assert.equal(second.reloads, 0);
    assert.match(second.failures[0].message, /isolated reload did not establish isolation/);
});

test('manual navigation after slow runtime loading starts a fresh isolation attempt', async () => {
    const storage = new Map();
    const first = await harness({ storage });
    assert.equal(await first.prepare(), true);
    first.advance(9000);
    assert.equal(storage.size, 0, 'completed handshake marker is retired before runtime loading');
    const next = await harness({ storage, origin: 110000 });
    assert.equal(await next.prepare(), true);
    assert.equal(next.failures.length, 0);
    assert.equal(next.reloads, 0);
});

// Story 9A.1 (F.10): the handshake carries no policy number. Another deploy's worker is refused by identity and structure: the
// browser re-fetches the worker script on update (updateViaCache 'none') and installs any worker whose bytes differ, so a changed
// policy behind a live controller is a waiting worker, which is refused; a controller answering in another shape is refused.
test("another deploy's changed isolation worker waiting behind the controller is refused", async () => {
    const h = await harness({ waiting: { state: 'installed' } });
    assert.equal(await h.prepare(), false);
    assert.match(h.failures[0].message, /waiting isolation policy requires closing all game tabs/);
});

test('a controller answering the handshake in another shape (the retired numbered reply) is refused', async () => {
    const h = await harness({ reply: { policyVersion: 1 } });
    assert.equal(await h.prepare(), false);
    assert.match(h.failures[0].message, /Unsupported Workshop isolation policy/);
});

test('the isolation worker answers only the structural policy request from an eligible client', async () => {
    const worker = readFileSync(new URL('../../CuriousContraptions.web/wwwroot/workshop-isolation-worker.js', import.meta.url), 'utf8');
    const handlers = new Map<string, (event: unknown) => void>();
    const context = vm.createContext({ URL, self: { registration: { scope: 'https://example.test/tim/' },
        addEventListener: (name: string, callback: (event: unknown) => void) => handlers.set(name, callback), clients: { claim: () => {} } } });
    new vm.Script(worker).runInContext(context); // registered as a classic service worker
    const ask = (data: unknown, url = 'https://example.test/tim/index.html') => {
        const replies: unknown[] = [];
        handlers.get('message')!({ data, source: { url }, ports: [{ postMessage: (reply: unknown) => replies.push(reply), close() {} }] });
        return JSON.parse(JSON.stringify(replies)); // the replies were built in the worker's realm
    };
    assert.deepEqual(ask({ policyRequest: true }), [{ policyReady: true }]);
    assert.deepEqual(ask({ policyRequest: 1 }), [], 'the retired numbered request');
    assert.deepEqual(ask({ policyRequest: true }, 'https://other.test/tim/'), [], 'an ineligible client');
});

test('unexpected controller identifies ownership without claiming an update is waiting', async () => {
 const h = await harness({ wrongController: true }); assert.equal(await h.prepare(), false);
 assert.match(h.failures[0].message, /unexpected service worker/); assert.doesNotMatch(h.failures[0].message, /waiting/);
});

test('an update becoming waiting during handshake refuses startup', async () => {
    const h = await harness({ waitingOnReply: true }); assert.equal(await h.prepare(), false);
    assert.match(h.failures[0].message, /waiting.*including this one/); assert.equal(h.timers.size, 0);
});

test('first uncontrolled navigation claims the policy and spends one isolated reload', async () => {
    const h = await harness({ initiallyControlled: false, isolated: false });
    assert.equal(await h.prepare(), false); assert.equal(h.reloads, 1); assert.equal(h.failures.length, 0);
});
