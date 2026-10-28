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
        view.setFloat32(offset, publication, true); view.setFloat32(offset + 28, 1, true);
        view.setUint32(offset + 44, 1, true); view.setBigUint64(offset + 48, id, true);
    });
    return bytes;
}
test('common bootstrap and reserved committed epochs preserve full-width identities across wraps and shrink', async () => {
    const client = await poseClient(), worker = await eventWorker();
    try {
        assert.equal(client.bootstrapRing.byteLength, 3120);
        assert.equal(client.api.hasPoseRing(client.id), true);
        assert.equal(client.api.readLatestPoseSlot(client.id)!.bodies.length, 0);
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
            for (let tick = 0; tick < 5; tick++) {
                worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([...ids], ++publication), false);
                const observed = client.api.readLatestPoseSlot(client.id)!;
                assert.equal(observed.timestamp, BigInt(publication * 100));
                assert.deepEqual(Array.from(observed.bodies, body => body.id), ids.map(String));
                for (const body of observed.bodies) assert.equal(body.px, publication);
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
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([1n, 2n], 1), false);
        const next = identity(2n, 2n);
        worker.imports.preparePose(next, 40, 54);
        worker.imports.discardPose(initial);
        assert.throws(() => worker.imports.commitPose(initial), /does not own/);
        assert.equal(worker.posePosted.length, 1);
        assert.deepEqual(Array.from(client.api.readLatestPoseSlot(client.id)!.bodies, body => body.id), ['1', '2']);
        worker.imports.discardPose(next);
        assert.throws(() => worker.imports.commitPose(next), /does not own/);
        assert.throws(() => worker.imports.preparePose(identity(3n, 1n), 1, 16), /cannot change/);
        const same = identity(4n, 1n);
        worker.imports.preparePose(same, 2, 16); worker.imports.commitPose(same);
        assert.equal(worker.posePosted.length, 1);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([1n, 2n], 2), false);
        assert.equal(client.api.readLatestPoseSlot(client.id)!.timestamp, 200n);
        worker.imports.preparePose(identity(5n, 2n), 1, 16); worker.imports.dispose();
        assert.throws(() => worker.imports.commitPose(identity(5n, 2n)), /does not own/);
    } finally { worker.imports.dispose(); client.dispose(); }
});
test('actual reader ignores in-progress slots and rejects invalid slot indices', async () => {
    const client = await poseClient(), worker = await eventWorker();
    try {
        const owned = identity(1n, 1n); worker.imports.preparePose(owned, 1, 16); worker.imports.commitPose(owned);
        const ring = worker.posePosted[0]; client.deliverRing(ring);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([4294967296n], 1), false);
        worker.imports.publish(new Uint8Array(0), new Uint8Array(24), slot([4294967296n], 2), false);
        const words = new BigInt64Array(ring);
        Atomics.store(words, 2 * 1040 / 8, 5n);
        assert.equal(client.api.readPoseSlot(client.id, 2), null);
        assert.equal(client.api.readLatestPoseSlot(client.id)!.bodies[0].px, 1);
        assert.equal(client.api.readPoseSlot(client.id, -1), null);
        assert.equal(client.api.readPoseSlot(client.id, 3), null);
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
test('malformed shared pose layout retires the public client without replacing its prior readable ring', async () => {
    const client = await poseClient();
    try {
        client.deliverRing(new SharedArrayBuffer(51));
        assert.equal(client.api.status(client.id), 4);
        assert.equal(client.api.readLatestPoseSlot(client.id)!.bodies.length, 0);
    } finally { client.dispose(); }
});
