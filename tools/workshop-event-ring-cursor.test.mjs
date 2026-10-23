import assert from 'node:assert/strict';
import test from 'node:test';
import { Worker } from 'node:worker_threads';
import { EventRingCursor, eventDelivery } from '../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

// Story 9A.23: the shared event ring reader against the shipped Simulation worker's writer (writeEventRing, evaluated from worker.js
// itself in a writer thread) and concurrent readers in their own threads. Every record's 64 bytes are a function of its sequence, so a
// torn copy (bytes of two sequences) is detected even where it would pass record validation. The C# side, the JS output equal to
// EventRingConsumer over the same bytes, is EventRingConsumerTests.

const cursorUrl = new URL('../CuriousContraptions.web/wwwroot/event-ring-cursor.js', import.meta.url).href;
const workerUrl = new URL('../CuriousContraptions.Simulation/wwwroot/worker.js', import.meta.url).href;

// The payload byte k of sequence s (bytes 0..7 are the sequence word).
const expectedByte = (sequence, k) => Number((sequence * 131n + BigInt(k) * 7n) & 0xffn);

// Loads the shipped writer exactly as tools/workshop-event-ring.test.mjs does: worker.js in a vm module, its imports stubbed.
const loadWriter = `
const vm = require('node:vm'); const fs = require('node:fs/promises');
async function loadWriter(url, onRing) {
    const source = await fs.readFile(new URL(url), 'utf8');
    class Channel { constructor() { this.port1 = { close() {} }; this.port2 = { postMessage() {}, close() {} }; } }
    const context = vm.createContext({ self: { postMessage(message) { if (message?.eventRing) onRing(message.eventRing); } }, console,
        Uint8Array, Uint32Array, Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, BigUint64Array, DataView, Atomics,
        MessageChannel: Channel });
    const host = { ScheduleRoles: () => [1, 2] };
    const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
        getConfig: () => ({ mainAssemblyName: 'EventRingCursorStress' }) };
    const worker = new vm.SourceTextModule(source + '\\nexport { writeEventRing, disposeOwned };', { context });
    await worker.link(async name => {
        const names = name.includes('dotnet') ? ['dotnet'] : name === '../clock-ring.js'
            ? ['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'] : ['admitNativeClock', 'nativeNow', 'nativeClockEvidence'];
        return new vm.SyntheticModule(names, function () {
            for (const name of names) this.setExport(name, name === 'dotnet' ? { create: async () => runtime }
                : name === 'admitNativeClock' ? async () => {} : () => { throw new Error('unused'); });
        }, { context });
    });
    await worker.evaluate();
    return worker.namespace;
}
`;

function record(sequence) {
    const bytes = new Uint8Array(64);
    new DataView(bytes.buffer).setBigUint64(0, sequence, true);
    for (let k = 8; k < 64; k++) bytes[k] = Number((sequence * 131n + BigInt(k) * 7n) & 0xffn);
    return bytes;
}
// One publication of the records (from, to]: the header the ring holds after it, then the newest records that fit; the rest is lapped.
function publication(epoch, capacity, from, to, lapped) {
    const count = Number(to - from), kept = Math.min(count, capacity), bytes = new Uint8Array(64 + kept * 64), view = new DataView(bytes.buffer);
    view.setUint32(0, capacity, true); view.setUint32(4, 64, true); view.setBigUint64(8, epoch, true);
    view.setBigUint64(16, to, true); view.setBigUint64(24, lapped + BigInt(count - kept), true);
    for (let i = 0; i < kept; i++) bytes.set(record(to - BigInt(kept) + 1n + BigInt(i)), 64 + i * 64);
    return { bytes, lapped: lapped + BigInt(count - kept) };
}
// The writer thread loads the same two functions as its own module source (their text), so both sides compose identical publications.
const publicationHelpers = '\n' + record.toString() + '\n' + publication.toString() + '\n';

const writerCode = loadWriter + publicationHelpers + `
const { parentPort, workerData } = require('node:worker_threads');
(async () => {
    const { capacity, total, epoch, seed, control } = workerData;
    const { writeEventRing } = await loadWriter(workerData.workerUrl, ring => parentPort.postMessage({ ring }));
    const state = new Int32Array(control);
    let random = seed >>> 0; const next = () => (random = (Math.imul(random, 1664525) + 1013904223) >>> 0);
    writeEventRing(publication(BigInt(epoch), capacity, 0n, 0n, 0n).bytes);
    while (Atomics.load(state, 0) < workerData.readers) Atomics.wait(state, 0, Atomics.load(state, 0), 5);
    let written = 0n, lapped = 0n;
    while (written < BigInt(total)) {
        // Random batch sizes, some larger than the ring (a burst the producer counts as lapped).
        const size = BigInt(1 + (next() % (next() % 8 === 0 ? capacity * 2 : Math.max(1, capacity >> 1))));
        const to = written + size > BigInt(total) ? BigInt(total) : written + size;
        const result = publication(BigInt(epoch), capacity, written, to, lapped);
        writeEventRing(result.bytes); written = to; lapped = result.lapped;
    }
    parentPort.postMessage({ done: written });
})().catch(error => { parentPort.postMessage({ error: String(error?.stack ?? error) }); });
`;

const readerCode = `
const { parentPort, workerData } = require('node:worker_threads');
(async () => {
    const { EventRingCursor } = await import(workerData.cursorUrl);
    const { ring, epoch, control, total } = workerData;
    const state = new Int32Array(control);
    const cursor = new EventRingCursor(); cursor.attach(ring);
    Atomics.add(state, 0, 1); Atomics.notify(state, 0);
    let delivered = 0n, skipped = 0n, last = 0n, torn = 0, reads = 0;
    const deadline = Date.now() + 30000;
    while (delivered + skipped < BigInt(total)) {
        if (Date.now() > deadline) throw new Error('the reader never accounted for every written record');
        const read = cursor.read(BigInt(epoch)); reads++;
        skipped += read.skipped;
        for (let offset = 0; offset < read.records.length; offset += 64) {
            const view = new DataView(read.records.buffer, read.records.byteOffset + offset, 64);
            const sequence = view.getBigUint64(0, true);
            if (sequence <= last) throw new Error('sequence ' + sequence + ' after ' + last);
            last = sequence;
            for (let k = 8; k < 64; k++)
                if (read.records[offset + k] !== Number((sequence * 131n + BigInt(k) * 7n) & 0xffn)) { torn++; break; }
            delivered++;
        }
    }
    parentPort.postMessage({ delivered, skipped, lapped: cursor.lapped, last, torn, reads });
})().catch(error => { parentPort.postMessage({ error: String(error?.stack ?? error) }); });
`;

function run(code, workerData) {
    const worker = new Worker(code, { eval: true, workerData, execArgv: ['--experimental-vm-modules', '--no-warnings'] });
    return worker;
}
function next(worker, key) {
    return new Promise((resolve, reject) => {
        const onMessage = message => {
            if (message.error) { worker.off('message', onMessage); reject(new Error(message.error)); }
            else if (key in message) { worker.off('message', onMessage); resolve(message[key] ?? message); }
        };
        worker.on('message', onMessage); worker.once('error', reject);
    });
}

async function stress(capacity, total, readers, seed) {
    const control = new SharedArrayBuffer(4);
    const writer = run(writerCode, { capacity, total, epoch: 5, seed, control, readers, workerUrl });
    try {
        const ring = await next(writer, 'ring');
        const done = next(writer, 'done');
        const results = await Promise.all(Array.from({ length: readers }, () => {
            const reader = run(readerCode, { cursorUrl, ring, epoch: 5, control, total });
            return new Promise((resolve, reject) => {
                reader.once('message', message => message.error ? reject(new Error(message.error)) : resolve(message));
                reader.once('error', reject);
            }).finally(() => reader.terminate());
        }));
        assert.equal(await done, BigInt(total));
        return results;
    } finally { await writer.terminate(); }
}

test('concurrent stress: over a million records through the shipped writer, no reader ever receives a torn record', { timeout: 120000 }, async () => {
    let records = 0;
    for (const [capacity, total, seed] of [[4, 260000, 1], [16, 260000, 2], [128, 260000, 3], [1024, 260000, 4]]) {
        for (const result of await stress(capacity, total, 2, seed)) {
            assert.equal(result.torn, 0, `capacity ${capacity}: a torn record was delivered`);
            // Every written record is either delivered once, in strictly increasing order, or counted as skipped.
            assert.equal(result.delivered + result.skipped, BigInt(total), `capacity ${capacity}: delivered + skipped = written`);
            assert.equal(result.lapped, result.skipped);
            assert.ok(result.delivered > 0n);
        }
        records += total;
    }
    assert.ok(records >= 1_000_000);
});

// Review F1: a seqlock whose payload is copied with plain (non-atomic) accesses lets torn records through on arm64: the plain loads and
// stores may be reordered past the sequence-word Atomics. The race shows in bursts once a round's code is optimised, so this variant
// runs independent rounds (fresh writer and reader threads each) at capacity 4, the configuration that tore most often on the pre-fix
// code (evidence-9a-23-25b/stress-40/). EVENT_RING_STRESS_ROUNDS raises the round count for the repeated-run evidence.
test('amplified concurrent stress at capacity 4: rounds of fresh writer and reader threads, no torn record', { timeout: 120000 }, async () => {
    const rounds = Number(process.env.EVENT_RING_STRESS_ROUNDS ?? 8), total = 250000, readers = 3;
    let torn = 0;
    for (let round = 0; round < rounds; round++)
        for (const result of await stress(4, total, readers, 100 + round)) {
            torn += result.torn;
            assert.equal(result.delivered + result.skipped, BigInt(total));
            assert.ok(result.delivered > 0n);
        }
    assert.equal(torn, 0, `${torn} torn record(s) delivered at capacity 4`);
});

// ---------- deterministic single-thread checks over the same writer ----------

const { writeEventRing, disposeOwned, posted } = await (async () => {
    const vm = await import('node:vm'); const fs = await import('node:fs/promises');
    const posted = [];
    const source = await fs.readFile(new URL(workerUrl), 'utf8');
    class Channel { constructor() { this.port1 = { close() {} }; this.port2 = { postMessage() {}, close() {} }; } }
    const context = vm.createContext({ self: { postMessage(message) { if (message?.eventRing) posted.push(message.eventRing); } }, console,
        Uint8Array, Uint32Array, Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer, BigInt64Array, BigUint64Array, DataView, Atomics,
        MessageChannel: Channel });
    const host = { ScheduleRoles: () => [1, 2] };
    const runtime = { setModuleImports() {}, getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
        getConfig: () => ({ mainAssemblyName: 'EventRingCursorControl' }) };
    const worker = new vm.SourceTextModule(source + '\nexport { writeEventRing, disposeOwned };', { context });
    await worker.link(async name => {
        const names = name.includes('dotnet') ? ['dotnet'] : name === '../clock-ring.js'
            ? ['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'] : ['admitNativeClock', 'nativeNow', 'nativeClockEvidence'];
        return new vm.SyntheticModule(names, function () {
            for (const name of names) this.setExport(name, name === 'dotnet' ? { create: async () => runtime }
                : name === 'admitNativeClock' ? async () => {} : () => { throw new Error('unused'); });
        }, { context });
    });
    await worker.evaluate();
    return { ...worker.namespace, posted };
})();

const sequences = read => Array.from({ length: read.records.length / 64 },
    (_, i) => new DataView(read.records.buffer, read.records.byteOffset + i * 64, 64).getBigUint64(0, true));
const intact = read => sequences(read).every((sequence, i) =>
    Array.from({ length: 56 }, (_, k) => read.records[i * 64 + 8 + k] === expectedByte(sequence, 8 + k)).every(Boolean));

test('a reader that keeps pace receives every record exactly once and skips none', () => {
    posted.length = 0; disposeOwned();
    const cursor = new EventRingCursor();
    let written = 0n, lapped = 0n, delivered = [];
    writeEventRing(publication(3n, 8, 0n, 0n, 0n).bytes); cursor.attach(posted[0]);
    for (const size of [1, 3, 8, 2, 5, 7]) {
        const result = publication(3n, 8, written, written + BigInt(size), lapped);
        writeEventRing(result.bytes); written += BigInt(size); lapped = result.lapped;
        const read = cursor.read(3n);
        assert.equal(read.skipped, 0n); assert.equal(read.epoch, 3n); assert.ok(intact(read));
        delivered.push(...sequences(read));
    }
    assert.deepEqual(delivered, Array.from({ length: Number(written) }, (_, i) => BigInt(i + 1)));
    assert.equal(cursor.read(3n).records.length, 0); // nothing is delivered twice
    assert.equal(cursor.lapped, 0n);
});

test('a parked reader lapped by a capacity and more reports exactly the skipped records, then reads the newest intact', () => {
    posted.length = 0; disposeOwned();
    const cursor = new EventRingCursor();
    writeEventRing(publication(4n, 4, 0n, 0n, 0n).bytes); cursor.attach(posted[0]);
    let written = 0n, lapped = 0n;
    for (let batch = 0; batch < 3; batch++) {
        const result = publication(4n, 4, written, written + 3n, lapped); writeEventRing(result.bytes); written += 3n; lapped = result.lapped;
    }
    const read = cursor.read(4n);
    assert.equal(read.skipped, 5n);           // 9 written, the ring holds the newest 4
    assert.deepEqual(sequences(read), [6n, 7n, 8n, 9n]);
    assert.ok(intact(read));
    assert.equal(cursor.lapped, 5n);
    // A burst larger than the ring in one publication: the producer's lapped records are skipped and counted here too.
    const burst = publication(4n, 4, written, written + 6n, lapped); writeEventRing(burst.bytes);
    const after = cursor.read(4n);
    assert.equal(after.skipped, 2n); assert.deepEqual(sequences(after), [12n, 13n, 14n, 15n]);
    assert.equal(cursor.lapped, 7n);
});

test('a slot reused while it is copied is skipped, never delivered', () => {
    posted.length = 0; disposeOwned();
    const cursor = new EventRingCursor();
    writeEventRing(publication(6n, 4, 0n, 2n, 0n).bytes); cursor.attach(posted[0]);
    // The writer has zeroed slot 2's sequence word (it is mid-write) although WriteSequence already covers it.
    const words = new BigUint64Array(posted[0]);
    Atomics.store(words, 2, 3n); // claim sequence 3 written
    const read = cursor.read(6n);
    assert.deepEqual(sequences(read), [1n, 2n]);
    assert.equal(read.skipped, 1n);
});

test('epochs: a newer ring is held unread until it is installed, an older one is ignored, and a new ring restarts at sequence 1', () => {
    posted.length = 0; disposeOwned();
    const cursor = new EventRingCursor();
    writeEventRing(publication(7n, 4, 0n, 2n, 0n).bytes); cursor.attach(posted[0]);
    assert.deepEqual(sequences(cursor.read(7n)), [1n, 2n]);
    writeEventRing(publication(8n, 4, 0n, 3n, 0n).bytes); cursor.attach(posted[1]);
    // Still installed: epoch 7. The epoch 8 ring is held: nothing is consumed.
    const held = cursor.read(7n);
    assert.equal(held.records.length, 0); assert.equal(held.skipped, 0n); assert.equal(held.epoch, 8n);
    assert.equal(cursor.cursor, 0n);
    // After the matching Commit the whole new run is read from sequence 1, with no record of the old ring.
    assert.deepEqual(sequences(cursor.read(8n)), [1n, 2n, 3n]);
    // An older installation never reads the newer ring.
    assert.equal(cursor.read(6n).records.length, 0);
    // The same epoch with a new capacity is a new ring, and its sequences restart too.
    writeEventRing(publication(8n, 8, 0n, 1n, 0n).bytes); cursor.attach(posted[2]);
    assert.deepEqual(sequences(cursor.read(8n)), [1n]);
    assert.throws(() => cursor.read(8), /installed epoch/);
    assert.throws(() => cursor.attach(new ArrayBuffer(64)), /must be shared/);
});

// Review F2 (D5): Commit(N+1) can reach a consumer before the N+1 ring does. The installed epoch is then newer than the attached ring,
// which is ignored: nothing is delivered (so C# never sees a foreign epoch) and its cursor does not move.
test('a ring older than the installed epoch is ignored: nothing is delivered and the cursor does not move', () => {
    posted.length = 0; disposeOwned();
    const cursor = new EventRingCursor();
    writeEventRing(publication(7n, 4, 0n, 3n, 0n).bytes); cursor.attach(posted[0]);
    const ignored = cursor.read(8n);
    assert.deepEqual([ignored.records.length, ignored.skipped, ignored.epoch, cursor.cursor, cursor.lapped], [0, 0n, 7n, 0n, 0n]);
    assert.equal(eventDelivery(ignored).length, 16); // a delivery with no records, so the consumer skips its C# call
    assert.deepEqual(sequences(cursor.read(7n)), [1n, 2n, 3n]); // the same ring still reads whole for its own installation
});

test('a delivery carries the epoch and skipped count before the records', () => {
    const records = new Uint8Array(64); records[0] = 9;
    const delivery = eventDelivery({ records, skipped: 2n, epoch: 11n });
    const view = new DataView(delivery.buffer);
    assert.deepEqual([view.getBigUint64(0, true), view.getBigUint64(8, true), delivery.length, delivery[16]], [11n, 2n, 80, 9]);
});
