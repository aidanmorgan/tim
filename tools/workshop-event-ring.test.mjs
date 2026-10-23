import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs/promises';
import vm from 'node:vm';

// Story 9A.22: the shipped Simulation worker's event ring writer, evaluated from worker.js itself. C# composes each publication (the
// 64-byte header the ring holds after it, then 64-byte records each led by its sequence); the worker only copies it. The C# side of the
// same round trip is EventRingProducerTests.TheShippedWorkerWritesTheSameRingAsTheReferenceForARealRunAndAnOverflow.
const workerSource = await fs.readFile(new URL('../CuriousContraptions.Simulation/wwwroot/worker.js', import.meta.url), 'utf8');
const posted = [];
class HarnessMessageChannel {
    constructor() {
        const channel = new MessageChannel();
        channel.port2.unref();
        this.port2 = channel.port2;
        this.port1 = { set onmessage(handler) { channel.port1.onmessage = handler; channel.port1.unref(); },
            get onmessage() { return channel.port1.onmessage; }, close() { channel.port1.close(); } };
    }
}
const context = vm.createContext({ self: { postMessage(message) { if (message?.eventRing) posted.push(message.eventRing); } }, console,
    Uint8Array, Uint32Array, Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, BigUint64Array, DataView, Atomics,
    MessageChannel: HarnessMessageChannel });
const host = { ScheduleRoles: () => [1, 2] };
const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
    getConfig: () => ({ mainAssemblyName: 'EventRingWriterControl' }) };
const worker = new vm.SourceTextModule(workerSource + '\nexport { writeEventRing, disposeOwned };', { context });
await worker.link(async name => {
    const names = name.includes('dotnet') ? ['dotnet'] : name === '../clock-ring.js'
        ? ['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'] : ['admitNativeClock', 'nativeNow', 'nativeClockEvidence'];
    return new vm.SyntheticModule(names, function () {
        for (const name of names) this.setExport(name, name === 'dotnet' ? { create: async () => runtime }
            : name === 'admitNativeClock' ? async () => {} : () => { throw new Error('The event ring writer control never uses this.'); });
    }, { context });
});
await worker.evaluate();
const { writeEventRing, disposeOwned } = worker.namespace;

// A publication: header (capacity 0, slot bytes 4, epoch 8, write sequence 16, lapped 24), then one record per sequence whose kind word
// (offset 16) carries a marker.
function publication(epoch, capacity, sequences, lapped = 0n) {
    const bytes = new Uint8Array(64 + sequences.length * 64), view = new DataView(bytes.buffer);
    view.setUint32(0, capacity, true); view.setUint32(4, 64, true); view.setBigUint64(8, epoch, true);
    view.setBigUint64(16, sequences.length ? sequences.at(-1) : 0n, true); view.setBigUint64(24, lapped, true);
    sequences.forEach((sequence, index) => {
        view.setBigUint64(64 + index * 64, sequence, true);
        view.setUint32(64 + index * 64 + 16, Number(sequence) * 10, true);
    });
    return bytes;
}
const header = ring => { const view = new DataView(ring); return [view.getUint32(0, true), view.getBigUint64(8, true),
    view.getBigUint64(16, true), view.getBigUint64(24, true)]; };
const slot = (ring, index) => { const view = new DataView(ring, 64 + index * 64); return [view.getBigUint64(0, true), view.getUint32(16, true)]; };

test('the first publication of an epoch allocates and posts its ring, and writes each record at its sequence slot', () => {
    posted.length = 0; disposeOwned();
    writeEventRing(publication(7n, 4, [1n, 2n, 3n]));
    assert.equal(posted.length, 1);
    const [ring] = posted;
    assert.equal(ring.byteLength, 64 + 4 * 64);
    assert.deepEqual(header(ring), [4, 7n, 3n, 0n]);
    assert.deepEqual([slot(ring, 0), slot(ring, 1), slot(ring, 2), slot(ring, 3)], [[1n, 10], [2n, 20], [3n, 30], [0n, 0]]);
    // The same epoch and capacity keep the ring: sequences wrap modulo the capacity.
    writeEventRing(publication(7n, 4, [4n, 5n]));
    assert.equal(posted.length, 1);
    assert.deepEqual(header(ring), [4, 7n, 5n, 0n]);
    assert.deepEqual([slot(ring, 3), slot(ring, 0)], [[4n, 40], [5n, 50]]);
});

test('a new epoch or capacity replaces the ring; an empty publication (no admitted world) changes nothing', () => {
    posted.length = 0; disposeOwned();
    writeEventRing(publication(7n, 4, [1n]));
    writeEventRing(new Uint8Array(0));
    writeEventRing(publication(7n, 4, []));
    assert.equal(posted.length, 1);
    writeEventRing(publication(8n, 4, [1n]));
    writeEventRing(publication(8n, 8, []));
    assert.equal(posted.length, 3);
    assert.deepEqual(header(posted[1]), [4, 8n, 1n, 0n]);
    assert.deepEqual(header(posted[2]), [8, 8n, 0n, 0n]);
    assert.deepEqual(slot(posted[0], 0), [1n, 10]); // the old ring is left as it was
});

test('the header carries the producer\'s lapped count, and a publication of more records than its ring is refused', () => {
    posted.length = 0; disposeOwned();
    writeEventRing(publication(9n, 4, [3n, 4n, 5n, 6n], 2n));
    assert.deepEqual(header(posted[0]), [4, 9n, 6n, 2n]);
    assert.deepEqual([slot(posted[0], 2), slot(posted[0], 1)], [[3n, 30], [6n, 60]]);
    assert.throws(() => writeEventRing(publication(9n, 2, [1n, 2n, 3n])), /not whole records within its ring/);
    assert.throws(() => writeEventRing(publication(9n, 4, [1n]).subarray(0, 100)), /not whole records within its ring/);
});

test('disposal drops the ring, so the next session allocates a fresh one', () => {
    posted.length = 0; disposeOwned();
    writeEventRing(publication(7n, 4, [1n]));
    disposeOwned();
    writeEventRing(publication(7n, 4, [1n]));
    assert.equal(posted.length, 2);
    assert.notEqual(posted[0], posted[1]);
});
