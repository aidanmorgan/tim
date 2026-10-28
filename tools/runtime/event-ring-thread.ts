import { parentPort, workerData } from 'node:worker_threads';
import assert from 'node:assert/strict';
import { EventRingCursor } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
import { eventWorker } from './event-worker-harness.js';
import { StressRole, StressMessage, publication, publish, hasExpectedFields, sequences } from './event-ring-fixture.js';
import type { StressData, StressReply } from './event-ring-fixture.js';
assert.ok(parentPort);
const port = parentPort;
const data = workerData as StressData;
function send(reply: StressReply) { port.postMessage(reply); }
try {
    const state = new Int32Array(data.control);
    if (data.role === StressRole.Writer) {
        const worker = await eventWorker();
        publish(worker.imports, publication(BigInt(data.epoch), data.capacity, 0n, 0n));
        send({ kind: StressMessage.Ring, ring: worker.posted[0] });
        while (Atomics.load(state, 0) < data.readers) Atomics.wait(state, 0, Atomics.load(state, 0), 5);
        let random = data.seed >>> 0, written = 0n;
        const next = () => random = (Math.imul(random, 1664525) + 1013904223) >>> 0;
        while (written < BigInt(data.total)) {
            const size = BigInt(1 + next() % data.capacity);
            const to = written + size > BigInt(data.total) ? BigInt(data.total) : written + size;
            publish(worker.imports, publication(BigInt(data.epoch), data.capacity, written, to)); written = to;
        }
        send({ kind: StressMessage.Done, written }); worker.imports.dispose();
    } else if (data.role === StressRole.Reader) {
        assert.ok(data.ring);
        const cursor = new EventRingCursor(); cursor.attach(data.ring);
        Atomics.add(state, 0, 1); Atomics.notify(state, 0);
        let delivered = 0n, skipped = 0n, last = 0n, torn = 0;
        const deadline = Date.now() + 30000;
        while (delivered + skipped < BigInt(data.total)) {
            assert.ok(Date.now() <= deadline, 'Reader must account for every written record.');
            const read = cursor.read(BigInt(data.epoch)); skipped += read.skipped;
            if (!hasExpectedFields(read)) torn++;
            for (const sequence of sequences(read)) { assert.ok(sequence > last); last = sequence; delivered++; }
        }
        send({ kind: StressMessage.Result, result: { delivered, skipped, lapped: cursor.lapped, torn } });
    } else throw new Error('Undefined stress role.');
} catch (error) { send({ kind: StressMessage.Error, error: String(error instanceof Error ? error.stack : error) }); }
