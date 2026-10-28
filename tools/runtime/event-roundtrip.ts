import assert from 'node:assert/strict';
import { EventPublicationPreparation } from './worker-contracts.js';
import { eventWorker } from './event-worker-harness.js';
import { EventRingCursor } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

let text = '';
for await (const chunk of process.stdin) text += String(chunk);
const request: unknown = JSON.parse(text);
assert.ok(request !== null && typeof request === 'object');
if ('batches' in request) {
    assert.ok(Array.isArray(request.batches));
    const worker = await eventWorker();
    for (const hex of request.batches) {
        assert.equal(typeof hex, 'string');
        assert.equal(worker.imports.prepareEvents(Uint8Array.from(Buffer.from(String(hex), 'hex'))), EventPublicationPreparation.Ready);
        worker.imports.commitEvents(); worker.imports.publishEvents();
    }
    const last = worker.posted.at(-1);
    assert.ok(last);
    process.stdout.write(JSON.stringify({ rings: worker.posted.length, ring: Buffer.from(new Uint8Array(last)).toString('hex') }));
    worker.imports.dispose();
} else {
    assert.ok('rings' in request && Array.isArray(request.rings));
    const results = [];
    for (const hex of request.rings) {
        assert.equal(typeof hex, 'string');
        const bytes = Buffer.from(String(hex), 'hex');
        const shared = new SharedArrayBuffer(bytes.length); new Uint8Array(shared).set(bytes);
        const cursor = new EventRingCursor(); cursor.attach(shared);
        const read = cursor.read(cursor.epoch);
        results.push({ records: Buffer.from(read.records).toString('hex'), skipped: read.skipped.toString(), epoch: read.epoch.toString() });
    }
    process.stdout.write(JSON.stringify(results));
}
