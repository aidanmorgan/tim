import { readFileSync } from 'node:fs';
import vm from 'node:vm';
import { PoseProducer, PoseReader, createPoseRing } from '../../CuriousContraptions.web/wwwroot/pose-ring.js';
import assert from 'node:assert/strict';
import test from 'node:test';
import { eventWorker } from './event-worker-harness.js';
import { poseClient } from './pose-client-harness.js';
import { PosePublicationPreparation } from './worker-contracts.js';

function identity(owner: bigint, epoch: bigint) {
    const bytes = new Uint8Array(16), view = new DataView(bytes.buffer);
    view.setBigUint64(0, owner, true); view.setBigUint64(8, epoch, true); return bytes;
}
function slot(ids: bigint[], publication: number) {
    const bytes = new Uint8Array(16 + ids.length * 64), view = new DataView(bytes.buffer);
    view.setBigInt64(8, BigInt(publication * 100), true);
    ids.forEach((id, index) => {
        const offset = 16 + index * 64;
        view.setFloat32(offset, publication, true);
        view.setFloat32(offset + 4, -2.5, true); view.setFloat32(offset + 8, 3.25, true);
        view.setFloat32(offset + 16, 0.125, true); view.setFloat32(offset + 20, -0.25, true);
        view.setFloat32(offset + 24, 0.5, true); view.setFloat32(offset + 28, 0.75, true);
        view.setFloat32(offset + 32, -4.5, true); view.setFloat32(offset + 36, 5.75, true); view.setFloat32(offset + 40, -6.25, true);
        view.setUint32(offset + 44, 1, true); view.setBigUint64(offset + 48, id, true);
    });
    return bytes;
}
test('common bootstrap and reserved committed epochs preserve full-width identities across wraps and shrink', async () => {
    const client = await poseClient(), worker = await eventWorker();
    try {
        assert.equal(client.bootstrapRing.byteLength, 3120);
        let reader = new PoseReader(client.bootstrapRing);
        assert.equal(reader.latest()!.bodies.length, 0);
        let publication = 0;
        for (const [epoch, ids, capacity] of [[1n, [1n, 4294967296n, 18446744073709551615n], 16],
            [2n, Array.from({ length: 40 }, (_, i) => BigInt(i + 1)), 54], [3n, [7n], 16], [4n, [], 16]] as const) {
            const owned = identity(epoch, epoch);
            assert.equal(worker.imports.preparePose(owned, ids.length, capacity), PosePublicationPreparation.Ready);
            assert.equal(worker.posePosted.length, Number(epoch) - 1);
            worker.imports.commitPose(owned);
            const ring = worker.posePosted.at(-1)!;
            assert.equal(ring.byteLength, 3 * (16 + capacity * 64));
            client.deliverRing(ring);
            reader.dispose(); reader = new PoseReader(ring);
            for (let tick = 0; tick < 5; tick++) {
                worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([...ids], ++publication), false);
                const observed = reader.latest()!;
                assert.equal(observed.timestamp, BigInt(publication * 100));
                assert.deepEqual(Array.from(observed.bodies, body => body.id), ids.map(String));
                for (const body of observed.bodies) {
                    assert.equal(body.px, publication);
                    assert.deepEqual([body.py, body.pz], [-2.5, 3.25]);
                    assert.deepEqual([body.qx, body.qy, body.qz, body.qw], [0.125, -0.25, 0.5, 0.75]);
                    assert.deepEqual([body.vx, body.vy, body.vz], [-4.5, 5.75, -6.25]);
                }
            }
        }
    } finally { worker.imports.dispose(); client.dispose(); }
});
test('prepare and stale discard preserve advertised storage; same epoch reuses it and population changes reject', async () => {
    const worker = await eventWorker(), client = await poseClient();
    try {
        const initial = identity(1n, 1n);
        worker.imports.preparePose(initial, 2, 16); worker.imports.commitPose(initial);
        const ring = worker.posePosted[0]; client.deliverRing(ring);
        const reader = new PoseReader(ring);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([1n, 2n], 1), false);
        const next = identity(2n, 2n);
        worker.imports.preparePose(next, 40, 54);
        worker.imports.discardPose(initial);
        assert.throws(() => worker.imports.commitPose(initial), /does not own/);
        assert.equal(worker.posePosted.length, 1);
        assert.deepEqual(Array.from(reader.latest()!.bodies, body => body.id), ['1', '2']);
        worker.imports.discardPose(next);
        assert.throws(() => worker.imports.commitPose(next), /does not own/);
        assert.throws(() => worker.imports.preparePose(identity(3n, 1n), 1, 16), /cannot change/);
        const same = identity(4n, 1n);
        worker.imports.preparePose(same, 2, 16); worker.imports.commitPose(same);
        assert.equal(worker.posePosted.length, 1);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([1n, 2n], 2), false);
        assert.equal(reader.latest()!.timestamp, 200n);
        worker.imports.preparePose(identity(5n, 2n), 1, 16); worker.imports.dispose();
        assert.throws(() => worker.imports.commitPose(identity(5n, 2n)), /does not own/);
    } finally { worker.imports.dispose(); client.dispose(); }
});
test('actual reader ignores in-progress slots and rejects invalid slot indices', async () => {
    const client = await poseClient(), worker = await eventWorker();
    try {
        const owned = identity(1n, 1n); worker.imports.preparePose(owned, 1, 16); worker.imports.commitPose(owned);
        const ring = worker.posePosted[0]; client.deliverRing(ring);
        const reader = new PoseReader(ring);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([4294967296n], 1), false);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([4294967296n], 2), false);
        const words = new BigInt64Array(ring);
        Atomics.store(words, 2 * 1040 / 8, 5n);
        assert.equal(reader.read(2), null);
        assert.equal(reader.latest()!.bodies[0].px, 1);
        assert.equal(reader.read(-1), null);
        assert.equal(reader.read(3), null);
    } finally { worker.imports.dispose(); client.dispose(); }
});
test('malformed ownership and population refuse before publishing; valid retry succeeds', async () => {
    const worker = await eventWorker();
    try {
        for (const [owned, count] of [[new Uint8Array(0), 1], [identity(0n, 1n), 1],
            [identity(1n, 0n), 1], [identity(1n, 1n), -1], [identity(1n, 1n), 1.5]] as const)
            assert.throws(() => worker.imports.preparePose(owned, count, 16));
        assert.throws(() => worker.imports.preparePose(identity(1n, 1n), 17, 16));
        assert.throws(() => worker.imports.preparePose(identity(1n, 1n), 1, 15));
        assert.throws(() => worker.imports.preparePose(identity(1n, 1n), 1, 16.5));
        assert.equal(worker.posePosted.length, 0);
        const owned = identity(1n, 1n); worker.imports.preparePose(owned, 0, 16); worker.imports.commitPose(owned);
        assert.equal(worker.posePosted[0].byteLength, 3120);
        assert.throws(() => worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([1n], 1), false), /match its admitted/);
    } finally { worker.imports.dispose(); }
});
test('malformed shared pose layout retires the public client', async () => {
    const client = await poseClient();
    try {
        client.deliverRing(new SharedArrayBuffer(51));
        assert.equal(client.api.status(client.id), 4);
    } finally { client.dispose(); }
});

test('disposed pose APIs expose no stale sample and refuse new publication ownership', () => {
    let ring = createPoseRing();
    const producer = new PoseProducer(message => { ring = message.poseRing; });
    producer.attach(ring); const owned = identity(1n, 1n);
    producer.prepare(owned, 1, 16); producer.commit(owned); producer.publish(slot([42n], 3));
    const reader = new PoseReader(ring);
    assert.equal(reader.latest()!.bodies[0].id, '42');
    reader.dispose(); producer.dispose();
    assert.equal(reader.latest(), null); assert.equal(reader.read(1), null);
    assert.throws(() => producer.attach(createPoseRing()), /bootstrap/);
    assert.throws(() => producer.prepare(identity(2n, 2n), 0, 16), /preparation/);
    assert.throws(() => producer.publish(slot([42n], 4)), /Missing admitted/);
});

test('publication during sampling rejects the invalidated candidate and returns the stable pose', async () => {
    let ring = createPoseRing();
    const producer = new PoseProducer(message => { ring = message.poseRing; });
    producer.attach(ring); const owned = identity(1n, 1n);
    producer.prepare(owned, 1, 16); producer.commit(owned);
    producer.publish(slot([11n], 1)); producer.publish(slot([22n], 2));
    // External atomic boundary controls the interleaving. The ABI sequence word for
    // slot 2 is selected, then read once to begin sampling. Before its verification
    // read, a concurrent producer begins writing (odd publication sequence).
    const candidateWord = 2 * 1040 / 8;
    let candidateReads = 0, invalidated = false;
    const context = vm.createContext({ SharedArrayBuffer, Uint8Array, BigInt64Array, DataView, Float32Array,
        Atomics: Object.create(Atomics, { load: { value(words: BigInt64Array, index: number) {
            if (words.buffer === ring && index === candidateWord && ++candidateReads === 3) {
                Atomics.store(words, index, 5n); invalidated = true;
            }
            return Atomics.load(words, index);
        } } }) });
    const module = new vm.SourceTextModule(readFileSync('CuriousContraptions.web/wwwroot/pose-ring.js', 'utf8'), { context });
    await module.link(() => { throw Error('Unexpected pose dependency'); }); await module.evaluate();
    const api = module.namespace as unknown as typeof import('../../CuriousContraptions.web/wwwroot/pose-ring.js');
    const reader = new api.PoseReader(ring), observed = reader.latest();
    assert.equal(invalidated, true);
    assert.ok(observed); assert.equal(observed.timestamp, 100n);
    assert.equal(observed.bodies[0].id, '11'); assert.equal(observed.bodies[0].px, 1);
    assert.equal(reader.read(2), null, 'the invalidated publication cannot escape as a stable sample');
    reader.dispose(); producer.dispose();
});
