import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';
type Isolation = typeof import('../../CuriousContraptions.web/wwwroot/workshop-isolation.js');
const isolationSource = readFileSync('CuriousContraptions.web/wwwroot/workshop-isolation.js', 'utf8');
const bootstrapSource = readFileSync('CuriousContraptions.web/wwwroot/workshop-bootstrap.js', 'utf8');
enum Scenario { Ready, Waiting, WrongController, EngineFailure, HttpFailure, NetworkFailure, InvalidJson, InvalidManifest }
async function boot(scenario: Scenario) {
    let resolveDone!: () => void, started = 0, quitted = 0, loaded = 0, failure: unknown, receivedSizes: unknown;
    const done = new Promise<void>(resolve => { resolveDone = resolve; });
    const overlay = { isConnected: true, remove() { this.isConnected = false; resolveDone(); } };
    const progress = { style: { display: '' }, value: 0, max: 0, removeAttribute() {} };
    const notice = { style: { display: '' }, value: '', set textContent(value: string) { this.value = value; resolveDone(); } };
    const controller = { scriptURL: scenario === Scenario.WrongController ? 'https://game.test/wrong.js' : 'https://game.test/workshop-isolation-worker.js',
        postMessage(_request: unknown, ports: { peer: { onmessage?: (event: { data: unknown }) => void } }[]) {
            queueMicrotask(() => ports[0].peer.onmessage?.({ data: { policyReady: true } }));
        } };
    class Port { onmessage?: (event: { data: unknown }) => void; close() {} }
    const context = vm.createContext({
        URL, URLSearchParams, Error, Promise, performance: { timeOrigin: 1000, now: () => 1, mark() {}, measure: () => ({ duration: 1 }), getEntriesByType: () => [] },
        document: { baseURI: 'https://game.test/', getElementById: (id: string) => id === 'status' ? overlay : id === 'status-progress' ? progress : notice,
            body: { appendChild() { overlay.isConnected = true; } } },
        location: { search: '', reload() { throw Error('unexpected reload'); } },
        sessionStorage: { getItem: () => null, setItem() {}, removeItem() {} },
        isSecureContext: true, crossOriginIsolated: true,
        navigator: { serviceWorker: { controller, addEventListener() {}, removeEventListener() {}, register: async () => ({
            scope: 'https://game.test/', update: async () => {}, installing: null, waiting: scenario === Scenario.Waiting ? {} : null }) } },
        MessageChannel: class { port1 = new Port(); port2 = { peer: this.port1, close() {} }; },
        setTimeout: () => 1, clearTimeout() {},
        console: { error(value: unknown) { failure = value; }, log() {} },
        fetch: async () => {
            if (scenario === Scenario.NetworkFailure) throw Error('network unavailable');
            return { ok: scenario !== Scenario.HttpFailure, json: async () => {
                if (scenario === Scenario.InvalidJson) throw new SyntaxError('invalid JSON');
                return scenario === Scenario.InvalidManifest ? { 'godot.wasm': -1 } : { 'godot.wasm': 100, 'godot.pck': 200 };
            } };
        },
        Engine: class {
            static getMissingFeatures() { return []; } static unload() { loaded--; }
            constructor(config: { fileSizes: unknown; onProgress(current: number, total: number): void }) { receivedSizes = config.fileSizes; loaded++; config.onProgress(5, 10); }
            requestQuit() { quitted++; }
            async init() { if (scenario === Scenario.EngineFailure) throw Error('engine initialization failed'); }
            async preloadFile() {}
            async start() { started++; api.isolationQualified(); }
        }
    });
    const isolation = new vm.SourceTextModule(isolationSource, { context });
    await isolation.link(() => { throw Error('unexpected dependency'); }); await isolation.evaluate();
    const api = isolation.namespace as unknown as Isolation;
    const bootstrap = new vm.SourceTextModule(bootstrapSource, { context });
    await bootstrap.link(() => isolation); await bootstrap.evaluate(); await done;
    return { overlay, progress, notice, started, quitted, loaded, failure, receivedSizes,
        bootstrap: bootstrap.namespace as unknown as typeof import('../../CuriousContraptions.web/wwwroot/workshop-bootstrap.js') };
}
test('actual bootstrap isolates before start, renders progress and hides after qualification', async () => {
    const h = await boot(Scenario.Ready); assert.equal(h.started, 1); assert.equal(h.overlay.isConnected, false);
    assert.deepEqual(JSON.parse(JSON.stringify(h.receivedSizes)), { 'godot.wasm': 100, 'godot.pck': 200 });
    assert.equal(h.progress.value, 5); assert.equal(h.progress.max, 10); assert.equal(h.failure, undefined);
});
for (const scenario of [Scenario.Waiting, Scenario.WrongController]) test('actual bootstrap displays isolation refusal ' + scenario, async () => {
    const h = await boot(scenario); assert.equal(h.started, 0); assert.equal(h.loaded, -1); assert.equal(h.overlay.isConnected, true);
    assert.match(h.notice.value, scenario === Scenario.Waiting ? /waiting.*including this one/ : /unexpected service worker/);
});
test('engine failure retires startup and displays the cause', async () => {
    const h = await boot(Scenario.EngineFailure); assert.equal(h.started, 0); assert.equal(h.quitted, 1);
    assert.match(h.notice.value, /engine initialization failed/); assert.equal(h.overlay.isConnected, true);
});

for (const scenario of [Scenario.HttpFailure, Scenario.NetworkFailure, Scenario.InvalidJson, Scenario.InvalidManifest])
    test('manifest refusal stops startup with typed visible cause ' + scenario, async () => {
        const h = await boot(scenario);
        assert.equal(h.started, 0); assert.equal(h.receivedSizes, undefined);
        assert.ok(h.failure instanceof h.bootstrap.BootstrapFailure);
        assert.equal(h.failure.reason, scenario === Scenario.HttpFailure || scenario === Scenario.NetworkFailure
            ? h.bootstrap.BootstrapFailureReason.ManifestUnavailable : h.bootstrap.BootstrapFailureReason.ManifestInvalid);
        assert.match(h.notice.value, /StartupFailed: download manifest is (unavailable|invalid)/);
        assert.equal(h.overlay.isConnected, true);
    });
