import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs/promises';
import vm from 'node:vm';

// Expose the existing private client registry only in this isolated test module.
const source = await fs.readFile(new URL('../CuriousContraptions.web/wwwroot/workshop-client.js', import.meta.url), 'utf8');
const context = vm.createContext({ SharedArrayBuffer, BigInt64Array, DataView, Float32Array, Atomics });
const module = new vm.SourceTextModule(source + '\nexport { clients };', { context });
await module.link(async (specifier) => {
    const names = specifier.includes('native-clock') ?
        ['admitNativeClock', 'nativeNow', 'nativeProfile', 'nativeClockEvidence'] :
        ['requireIsolation', 'watchIsolation', 'isolationQualified', 'isolationEvidence', 'stopIsolation'];
    return new vm.SyntheticModule(names, function () {
        for (const name of names) this.setExport(name, () => { throw new Error('Unexpected clock/isolation use'); });
    }, { context });
});
await module.evaluate();
const { clients, readPoseSlot, readLatestPoseSlot } = module.namespace;
function client(bytes = 3120) {
    const poseRing = new SharedArrayBuffer(bytes);
    const value = { poseRing, poseSeqView: new BigInt64Array(poseRing),
        poseDataView: new DataView(poseRing), poseFloatView: new Float32Array(poseRing) };
    clients.set(1, value); return value;
}
function body(c, slot, index, id, x) {
    const offset = slot * 1040 + 16 + index * 64;
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

test('old ring layout rejects rather than truncating secondary identity', () => {
    client(2352);
    assert.equal(readPoseSlot(1, 0), null);
});

test('actual worker writer publishes globally newest frames across wraps and shrinking populations', async () => {
    const workerSource = await fs.readFile(new URL('../CuriousContraptions.Simulation/wwwroot/worker.js', import.meta.url), 'utf8');
    const workerContext = vm.createContext({ self: { postMessage() {} }, console, Uint8Array, Uint32Array,
        Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, DataView, Atomics });
    const host = { CommandAbi: () => [72, 5768], ResponseAbi: () => [24336, 40, 56],
        OperationAbi: () => [0, 1], StateBytes: () => 154048, PrismaticRowKinds: () => [0, 1, 2, 3, 4, 5, 6, 7], ScheduleRoles: () => [1, 2], CaptureMode: () => 1 };
    const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ Program: host }),
        getConfig: () => ({ mainAssemblyName: 'PoseWriterControl' }) };
    const worker = new vm.SourceTextModule(workerSource + `
export function bindTestRing(ring) {
    poseRing = ring; poseSeqView = new BigInt64Array(ring);
    poseDataView = new DataView(ring); poseFloatView = new Float32Array(ring);
}
export { writePoseRing };
`, { context: workerContext });
    await worker.link(async name => {
        if (name.includes('dotnet')) return new vm.SyntheticModule(['dotnet'], function () {
            this.setExport('dotnet', { create: async () => runtime });
        }, { context: workerContext });
        return new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
            this.setExport('admitNativeClock', async () => {});
            this.setExport('nativeNow', () => 1); this.setExport('nativeClockEvidence', () => []);
        }, { context: workerContext });
    });
    await worker.evaluate();
    let c = client(); worker.namespace.bindTestRing(c.poseRing);
    for (let publication = 1; publication <= 12; publication++) {
        // Build mode does not publish; this exercises actual committed population changes and fresh ring ownership.
        if (publication === 9) { c = client(); worker.namespace.bindTestRing(c.poseRing); }
        const ids = publication === 5 ? [] : publication < 5 ? [1n, 4294967296n] : [18446744073709551615n];
        const bytes = new Uint8Array(128 + ids.length * 64), view = new DataView(bytes.buffer);
        bytes[104] = ids.length; view.setBigInt64(72, BigInt(publication * 100), true);
        ids.forEach((id, index) => {
            const offset = 128 + index * 64;
            view.setBigUint64(offset, id, true); view.setInt32(offset + 8, publication * 16, true);
            view.setUint16(offset + 32, 15360, true);
        });
        worker.namespace.writePoseRing(bytes);
        const observed = readLatestPoseSlot(1);
        assert.equal(observed.timestamp, BigInt(publication * 100), 'every publication must supersede all preceding slots');
        assert.deepEqual(Array.from(observed.bodies, b => b.id), ids.map(String), 'retired secondary IDs disappear');
    }
});
