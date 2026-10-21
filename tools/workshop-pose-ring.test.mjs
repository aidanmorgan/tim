import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs/promises';
import vm from 'node:vm';

// Expose the existing private client registry only in this isolated test module.
const source = await fs.readFile(new URL('../CuriousContraptions.web/wwwroot/workshop-client.js', import.meta.url), 'utf8');
const context = vm.createContext({ SharedArrayBuffer, BigInt64Array, DataView, Float32Array, Atomics });
const module = new vm.SourceTextModule(source + '\nexport { clients, attachPoseRing };', { context });
await module.link(async (specifier) => {
    const names = specifier.includes('native-clock') ?
        ['admitNativeClock', 'nativeNow', 'nativeProfile', 'nativeClockEvidence'] :
        specifier.includes('clock-ring') ? ['createClockRing', 'attachClockRing', 'requestReply', 'heartbeat', 'ClockRole'] :
        ['requireIsolation', 'watchIsolation', 'isolationQualified', 'isolationEvidence', 'stopIsolation'];
    return new vm.SyntheticModule(names, function () {
        for (const name of names) this.setExport(name, () => { throw new Error('Unexpected clock/isolation use'); });
    }, { context });
});
await module.evaluate();
const { clients, attachPoseRing, readPoseSlot, readLatestPoseSlot } = module.namespace;
// Story 9R.5b: a ring's body capacity is its own length; the bootstrap ring holds 16 bodies (3 x (16 + 16 x 64) bytes).
const ringBytes = capacity => 3 * (16 + capacity * 64);
function client(bytes = ringBytes(16)) {
    const value = {};
    attachPoseRing(value, new SharedArrayBuffer(bytes));
    clients.set(1, value); return value;
}
function body(c, slot, index, id, x) {
    const offset = slot * (16 + c.poseCapacity * 64) + 16 + index * 64;
    c.poseDataView.setFloat32(offset, x, true);
    c.poseDataView.setFloat32(offset + 28, 1, true);
    c.poseDataView.setUint32(offset + 44, 1, true);
    c.poseDataView.setBigUint64(offset + 48, id, true);
}

test('actual pose reader preserves first secondary and full-width IDs without owner alias', () => {
    const c = client();
    Atomics.store(c.poseSeqView, 0, 2n);
    body(c, 0, 0, 1n, 1.5);
    body(c, 0, 1, 4294967296n, -2);
    body(c, 0, 2, 18446744073709551615n, 3);
    const result = readPoseSlot(1, 0);
    assert.deepEqual(Array.from(result.bodies, b => b.id), ['1', '4294967296', '18446744073709551615']);
    assert.equal(result.bodies[1].px, -2);
});

test('actual latest reader ignores in-progress slot and uses new aligned stride', () => {
    const c = client();
    Atomics.store(c.poseSeqView, 0, 2n); body(c, 0, 0, 1n, 1);
    Atomics.store(c.poseSeqView, 130, 4n); body(c, 1, 0, 4294967296n, 2);
    Atomics.store(c.poseSeqView, 260, 5n);
    assert.equal(readPoseSlot(1, 2), null);
    assert.equal(readLatestPoseSlot(1).bodies[0].id, '4294967296');
    assert.equal(readLatestPoseSlot(1).bodies[0].px, 2);
    assert.equal(readPoseSlot(1, -1), null);
    assert.equal(readPoseSlot(1, 3), null);
});

test('a ring that is not a whole number of slots and body records is refused', () => {
    assert.throws(() => client(ringBytes(16) + 3), /not a whole number of slots/);
    assert.throws(() => client(ringBytes(16) + 48), /not a whole number of slots/);
});

test('a grown ring of any capacity reads every body (no fixed body capacity)', () => {
    const c = client(ringBytes(40));
    Atomics.store(c.poseSeqView, 0, 2n);
    for (let index = 0; index < 40; index++) body(c, 0, index, BigInt(index + 1), index);
    const result = readPoseSlot(1, 0);
    assert.equal(result.bodies.length, 40);
    assert.equal(result.bodies[39].id, '40');
});

// The worker's run-loop yield channel, unreferenced so the harness process can exit once its checks complete.
class HarnessMessageChannel {
    constructor() {
        const channel = new MessageChannel();
        channel.port2.unref();
        this.port2 = channel.port2;
        this.port1 = { set onmessage(handler) { channel.port1.onmessage = handler; channel.port1.unref(); },
            get onmessage() { return channel.port1.onmessage; } };
    }
}
test('actual worker writer publishes globally newest frames across wraps and shrinking populations', async () => {
    const workerSource = await fs.readFile(new URL('../CuriousContraptions.Simulation/wwwroot/worker.js', import.meta.url), 'utf8');
    const posted = [];
    const workerContext = vm.createContext({ self: { postMessage(message) { if (message?.poseRing) posted.push(message); } }, console, Uint8Array, Uint32Array,
        Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, DataView, Atomics, MessageChannel: HarnessMessageChannel });
    const host = { ScheduleRoles: () => [1, 2], CaptureMode: () => 1 };
    const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
        getConfig: () => ({ mainAssemblyName: 'PoseWriterControl' }) };
    const worker = new vm.SourceTextModule(workerSource + `
export function bindTestRing(ring) { attachPoseRing(ring); }
export { writePoseRing };
`, { context: workerContext });
    await worker.link(async name => {
        if (name.includes('dotnet')) return new vm.SyntheticModule(['dotnet'], function () {
            this.setExport('dotnet', { create: async () => runtime });
        }, { context: workerContext });
        if (name === '../clock-ring.js') return new vm.SyntheticModule(['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'], function () {
            for (const name of ['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'])
                this.setExport(name, () => { throw new Error('The pose writer control never bootstraps a clock.'); });
        }, { context: workerContext });
        return new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
            this.setExport('admitNativeClock', async () => {});
            this.setExport('nativeNow', () => 1); this.setExport('nativeClockEvidence', () => []);
        }, { context: workerContext });
    });
    await worker.evaluate();
    let c = client(); worker.namespace.bindTestRing(c.poseRing);
    // The C#-composed slot (WorkshopPoseRing.ComposeSlot): a 16-byte header with the capture time at 8, then one 64-byte record
    // per committed body (position at 0, rotation w at 28, the active flag at 44, the full identity at 48).
    const read = (ids, publication) => {
        const bytes = new Uint8Array(16 + ids.length * 64), view = new DataView(bytes.buffer);
        view.setBigInt64(8, BigInt(publication * 100), true);
        ids.forEach((id, index) => {
            const offset = 16 + index * 64;
            view.setFloat32(offset, publication, true); view.setFloat32(offset + 28, 1, true);
            view.setUint32(offset + 44, 1, true); view.setBigUint64(offset + 48, id, true);
        });
        return bytes;
    };
    for (let publication = 1; publication <= 12; publication++) {
        // Build mode does not publish; this exercises actual committed population changes and fresh ring ownership.
        if (publication === 9) { c = client(); worker.namespace.bindTestRing(c.poseRing); }
        const ids = publication === 5 ? [] : publication < 5 ? [1n, 4294967296n] : [18446744073709551615n];
        worker.namespace.writePoseRing(read(ids, publication));
        const observed = readLatestPoseSlot(1);
        assert.equal(observed.timestamp, BigInt(publication * 100), 'every publication must supersede all preceding slots');
        assert.deepEqual(Array.from(observed.bodies, b => b.id), ids.map(String), 'retired secondary IDs disappear');
    }
    assert.equal(posted.length, 0, 'a world within the ring never replaces it');
    // Story 9R.5b: seventeen moving bodies (one past the bootstrap ring) grow the ring; the worker posts the larger ring before
    // the read that needs it, and every body is published (nothing is truncated).
    const seventeen = Array.from({ length: 17 }, (_, index) => BigInt(index + 1));
    worker.namespace.writePoseRing(read(seventeen, 13));
    assert.equal(posted.length, 1, 'the worker posts exactly one grown ring');
    assert.ok(posted[0].poseRing instanceof SharedArrayBuffer && posted[0].poseRing.byteLength >= ringBytes(17));
    attachPoseRing(c, posted[0].poseRing);
    const grown = readLatestPoseSlot(1);
    assert.equal(grown.timestamp, 1300n);
    assert.deepEqual(Array.from(grown.bodies, b => b.id), seventeen.map(String));
    // A smaller world keeps the grown ring.
    worker.namespace.writePoseRing(read([7n], 14));
    assert.equal(posted.length, 1);
    attachPoseRing(c, c.poseRing);
    assert.deepEqual(Array.from(readLatestPoseSlot(1).bodies, b => b.id), ['7']);
});
