import assert from 'node:assert/strict';
import test from 'node:test';
import { Worker } from 'node:worker_threads';
import { EventRingCursor, EventRingMemory, eventDelivery } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
import { eventWorker, HarnessPort } from './event-worker-harness.js';
import { publication, publish, sequences, hasExpectedFields, StressRole, StressMessage } from './event-ring-fixture.js';
import type { StressData, StressReply, StressResult } from './event-ring-fixture.js';

test('prepared rings are advertised to browser and Animation only after commit and publication', async () => {
    const worker = await eventWorker(), port = new HarnessPort(); await worker.seatAnimationPort(port);
    worker.imports.prepareEvents(publication(3n, 3, 0n, 2n));
    assert.equal(worker.posted.length, 0); assert.equal(port.messages.length, 0);
    worker.imports.commitEvents(); assert.equal(worker.posted.length, 0);
    worker.imports.publishEvents();
    assert.equal(worker.posted.length, 1);
    assert.deepEqual(port.messages, [{ eventRing: worker.posted[0] }]);
    const ring = EventRingMemory.attach(worker.posted[0]);
    assert.equal(ring.capacity, 3); assert.equal(ring.epoch, 3n); assert.equal(ring.loadWriteSequence(), 2n);
    assert.equal(ring.loadSequence(1n), 1n); assert.equal(ring.loadSequence(2n), 2n);
    publish(worker.imports, publication(3n, 3, 2n, 4n));
    assert.equal(worker.posted.length, 1); assert.equal(ring.loadSequence(4n), 4n);
    worker.imports.dispose();
});

test('growth copies history and preserves two different cursors across successive replacements', async () => {
    const worker = await eventWorker(), fast = new EventRingCursor(), slow = new EventRingCursor();
    publish(worker.imports, publication(5n, 2, 0n, 2n));
    fast.attach(worker.posted[0]); slow.attach(worker.posted[0]);
    assert.deepEqual(sequences(fast.read(5n)), [1n, 2n]);
    for (const [capacity, from, to] of [[4, 2n, 4n], [8, 4n, 6n]] as const) {
        publish(worker.imports, publication(5n, capacity, from, to));
        const replacement = worker.posted.at(-1)!; fast.attach(replacement); slow.attach(replacement);
        assert.deepEqual(sequences(fast.read(5n)), [from + 1n, to]);
    }
    assert.deepEqual(sequences(slow.read(5n)), [1n, 2n, 3n, 4n, 5n, 6n]);
    assert.equal(fast.lapped, 0n); assert.equal(slow.lapped, 0n);
    assert.throws(() => fast.attach(worker.posted[0]), /shrink or rewind/);
    worker.imports.dispose();
});

test('discard never overwrites unread live slots, both without growth and with growth', async () => {
    const worker = await eventWorker();
    publish(worker.imports, publication(3n, 3, 0n, 3n));
    for (const capacity of [3, 8]) {
        worker.imports.prepareEvents(publication(3n, capacity, 3n, 5n));
        const cursor = new EventRingCursor(); cursor.attach(worker.posted[0]);
        assert.deepEqual(sequences(cursor.read(3n)), [1n, 2n, 3n]);
        assert.equal(worker.posted.length, 1);
        worker.imports.discardEvents(); worker.imports.publishEvents();
        assert.equal(EventRingMemory.attach(worker.posted[0]).loadWriteSequence(), 3n);
    }
    publish(worker.imports, publication(3n, 8, 3n, 5n));
    const cursor = new EventRingCursor(); cursor.attach(worker.posted[1]);
    assert.deepEqual(sequences(cursor.read(3n)), [1n, 2n, 3n, 4n, 5n]);
    worker.imports.dispose();
});

test('malformed packets and removed producer-loss header fields refuse before publication', async () => {
    const worker = await eventWorker();
    publish(worker.imports, publication(9n, 4, 0n, 1n));
    const reserved = publication(9n, 4, 1n, 2n); new DataView(reserved.buffer).setBigUint64(24, 1n, true);
    for (const batch of [reserved, publication(9n, 2, 1n, 4n), publication(9n, 4, 2n, 3n),
        publication(9n, 4, 1n, 2n).subarray(0, 100), new Uint8Array(0)]) {
        assert.throws(() => worker.imports.prepareEvents(batch));
        assert.equal(worker.posted.length, 1);
        assert.equal(EventRingMemory.attach(worker.posted[0]).loadWriteSequence(), 1n);
    }
    publish(worker.imports, publication(9n, 4, 1n, 2n));
    assert.equal(EventRingMemory.attach(worker.posted[0]).loadWriteSequence(), 2n);
    worker.imports.dispose();
});

test('new epoch resets sequence; a replacement waits for matching installed epoch', async () => {
    const worker = await eventWorker(), cursor = new EventRingCursor();
    publish(worker.imports, publication(7n, 4, 0n, 2n)); cursor.attach(worker.posted[0]);
    assert.deepEqual(sequences(cursor.read(7n)), [1n, 2n]);
    publish(worker.imports, publication(8n, 1, 0n, 1n)); cursor.attach(worker.posted[1]);
    assert.equal(cursor.cursor, 0n); assert.equal(cursor.read(7n).records.length, 0);
    assert.deepEqual(sequences(cursor.read(8n)), [1n]);
    assert.equal(cursor.read(6n).records.length, 0);
    worker.imports.dispose();
});

test('dispose releases prepared storage and a fresh host starts its own epoch', async () => {
    const first = await eventWorker();
    publish(first.imports, publication(7n, 4, 0n, 1n));
    first.imports.prepareEvents(publication(7n, 8, 1n, 3n)); first.imports.dispose();
    first.imports.publishEvents(); assert.equal(first.posted.length, 1);
    assert.throws(() => first.imports.commitEvents()); assert.throws(() => first.imports.prepareEvents(publication(7n, 4, 1n, 2n)));
    const next = await eventWorker(); publish(next.imports, publication(7n, 1, 0n, 1n));
    assert.notEqual(first.posted[0], next.posted[0]); next.imports.dispose();
});

test('a reader keeping pace receives every record exactly once with no torn payload', async () => {
    const worker = await eventWorker(), cursor = new EventRingCursor(); let written = 0n;
    publish(worker.imports, publication(3n, 8, 0n, 0n)); cursor.attach(worker.posted[0]);
    const delivered: bigint[] = [];
    for (const size of [1, 3, 8, 2, 5, 7]) {
        publish(worker.imports, publication(3n, 8, written, written + BigInt(size))); written += BigInt(size);
        const read = cursor.read(3n); assert.equal(read.skipped, 0n); assert.ok(hasExpectedFields(read)); delivered.push(...sequences(read));
    }
    assert.deepEqual(delivered, Array.from({ length: Number(written) }, (_, i) => BigInt(i + 1)));
    assert.equal(cursor.read(3n).records.length, 0); assert.equal(cursor.lapped, 0n); worker.imports.dispose();
});

test('slow readers account for ordinary overwritten history without replay', async () => {
    const worker = await eventWorker(), cursor = new EventRingCursor();
    publish(worker.imports, publication(4n, 4, 0n, 0n)); cursor.attach(worker.posted[0]);
    for (let from = 0n; from < 9n; from += 3n) publish(worker.imports, publication(4n, 4, from, from + 3n));
    const read = cursor.read(4n);
    assert.equal(read.skipped, 5n); assert.deepEqual(sequences(read), [6n, 7n, 8n, 9n]); assert.ok(hasExpectedFields(read));
    // A burst now grows before publication; the already counted loss never repeats.
    publish(worker.imports, publication(4n, 16, 9n, 15n)); cursor.attach(worker.posted[1]);
    assert.deepEqual(sequences(cursor.read(4n)), [10n, 11n, 12n, 13n, 14n, 15n]); assert.equal(cursor.lapped, 5n);
    worker.imports.dispose();
});

test('uncommitted slots are skipped through the defined atomic accessor', async () => {
    const worker = await eventWorker(), cursor = new EventRingCursor();
    publish(worker.imports, publication(6n, 4, 0n, 3n)); cursor.attach(worker.posted[0]);
    EventRingMemory.attach(worker.posted[0]).publishSequence(2n, 0n);
    const read = cursor.read(6n); assert.deepEqual(sequences(read), [1n, 3n]); assert.equal(read.skipped, 1n);
    worker.imports.dispose();
});

test('an older attached ring is ignored without moving its cursor', async () => {
    const worker = await eventWorker(), cursor = new EventRingCursor();
    publish(worker.imports, publication(7n, 4, 0n, 3n)); cursor.attach(worker.posted[0]);
    const ignored = cursor.read(8n);
    assert.deepEqual([ignored.records.length, ignored.skipped, ignored.epoch, cursor.cursor, cursor.lapped], [0, 0n, 7n, 0n, 0n]);
    assert.equal(eventDelivery(ignored).length, 16); assert.deepEqual(sequences(cursor.read(7n)), [1n, 2n, 3n]);
    worker.imports.dispose();
});

test('memory exposes only atomic accessors and its posted adoption message', () => {
    const memory = EventRingMemory.allocate(3, 3n), messages: { eventRing: SharedArrayBuffer }[] = [];
    assert.equal('buffer' in memory, false); assert.deepEqual(Object.keys(memory), []);
    memory.postTo({ postMessage: message => messages.push(message) });
    assert.deepEqual(Object.keys(messages[0]), ['eventRing']);
    const cursor = new EventRingCursor(); cursor.attach(messages[0].eventRing);
    assert.equal('ring' in cursor, false); assert.deepEqual(Object.keys(cursor).sort(), ['cursor', 'epoch', 'lapped']);
    assert.equal(cursor.epoch, 3n);
    assert.throws(() => Reflect.apply(cursor.read, cursor, [8]), /installed epoch/);
    assert.throws(() => Reflect.apply(cursor.attach, cursor, [new ArrayBuffer(64)]), /must be shared/);
});

test('delivery carries its epoch and skipped count before the records', () => {
    const records = new Uint8Array(64); records[0] = 9;
    const delivery = eventDelivery({ records, skipped: 2n, epoch: 11n }); const view = new DataView(delivery.buffer);
    assert.deepEqual([view.getBigUint64(0, true), view.getBigUint64(8, true), delivery.length, delivery[16]], [11n, 2n, 80, 9]);
});

function start(data: StressData) {
    return new Worker(new URL('./event-ring-thread.js', import.meta.url), { workerData: data,
        execArgv: ['--experimental-vm-modules', '--no-warnings'] });
}
function next(worker: Worker, kind: StressMessage): Promise<StressReply> {
    return new Promise((resolve, reject) => {
        const receive = (message: StressReply) => {
            if (message.kind === StressMessage.Error) { worker.off('message', receive); reject(new Error(message.error)); }
            else if (message.kind === kind) { worker.off('message', receive); resolve(message); }
        };
        worker.on('message', receive); worker.once('error', reject);
    });
}
async function stress(capacity: number, total: number, seed: number): Promise<StressResult[]> {
    const data: StressData = { role: StressRole.Writer, capacity, total, seed, epoch: 5, control: new SharedArrayBuffer(4), readers: 2 };
    const writer = start(data);
    try {
        const initial = await next(writer, StressMessage.Ring); assert.equal(initial.kind, StressMessage.Ring);
        const done = next(writer, StressMessage.Done);
        const results = await Promise.all(Array.from({ length: data.readers }, async () => {
            const reader = start({ ...data, role: StressRole.Reader, ring: initial.ring });
            try { const result = await next(reader, StressMessage.Result); assert.equal(result.kind, StressMessage.Result); return result.result; }
            finally { await reader.terminate(); }
        }));
        const complete = await done; assert.equal(complete.kind, StressMessage.Done); assert.equal(complete.written, BigInt(total));
        return results;
    } finally { await writer.terminate(); }
}
test('concurrent smoke: over a million records through shipped writer never deliver a torn payload', { timeout: 120000 }, async () => {
    let records = 0;
    for (const [capacity, total, seed] of [[4, 260000, 1], [16, 260000, 2], [128, 260000, 3], [1024, 260000, 4]]) {
        for (const result of await stress(capacity, total, seed)) {
            assert.equal(result.torn, 0); assert.equal(result.delivered + result.skipped, BigInt(total));
            assert.equal(result.lapped, result.skipped); assert.ok(result.delivered > 0n);
        }
        records += total;
    }
    assert.ok(records >= 1_000_000);
});
