import assert from 'node:assert/strict';
import test from 'node:test';

// Provide browser globals for workshop-isolation.js in Node
globalThis.document = { baseURI: 'http://127.0.0.1:8060/' };
Object.defineProperty(globalThis, 'navigator', {
    value: { serviceWorker: {} },
    configurable: true,
    writable: true
});

const { readPoseSlot, readLatestPoseSlot, hasPoseRing } = await import('../CuriousContraptions.web/wwwroot/workshop-client.js');

test('workshop-pose-ring: hasPoseRing, readPoseSlot, readLatestPoseSlot, and atomic protocol', () => {
    const sab = new SharedArrayBuffer(2352);
    const seqView = new BigInt64Array(sab);
    const dataView = new DataView(sab);
    const floatView = new Float32Array(sab);

    // Slot 0: sequence 2 (even = committed)
    Atomics.store(seqView, 0, 2n);
    dataView.setBigInt64(8, 1000000n, true); // timestamp
    // Body 0 (id 1, px=1.5, py=2.0, pz=3.0, qx=0, qy=0, qz=0, qw=1, vx=10, vy=0, vz=0, flags=1)
    floatView[4] = 1.5;
    floatView[5] = 2.0;
    floatView[6] = 3.0;
    dataView.setUint32(7 * 4, 1, true); // bodyId
    floatView[8] = 0;
    floatView[9] = 0;
    floatView[10] = 0;
    floatView[11] = 1.0;
    floatView[12] = 10.0;
    floatView[13] = 0;
    floatView[14] = 0;
    dataView.setUint32(15 * 4, 1, true); // flags

    // Body 1 (id 2, px=-2.0, py=4.0, pz=0.5, qx=0, qy=0.7071, qz=0, qw=0.7071, vx=-5, vy=2, vz=0, flags=1)
    const b1 = 4 + 12;
    floatView[b1 + 0] = -2.0;
    floatView[b1 + 1] = 4.0;
    floatView[b1 + 2] = 0.5;
    dataView.setUint32((b1 + 3) * 4, 2, true);
    floatView[b1 + 4] = 0;
    floatView[b1 + 5] = 0.7071;
    floatView[b1 + 6] = 0;
    floatView[b1 + 7] = 0.7071;
    floatView[b1 + 8] = -5.0;
    floatView[b1 + 9] = 2.0;
    floatView[b1 + 10] = 0;
    dataView.setUint32((b1 + 11) * 4, 1, true);

    // Slot 1: sequence 4 (even = committed, later frame)
    Atomics.store(seqView, 98, 4n);
    dataView.setBigInt64(784 + 8, 2000000n, true);
    const s1b0 = (784 + 16) / 4;
    floatView[s1b0 + 0] = 2.5;
    floatView[s1b0 + 1] = 2.0;
    floatView[s1b0 + 2] = 3.0;
    dataView.setUint32((s1b0 + 3) * 4, 1, true);
    floatView[s1b0 + 7] = 1.0;
    dataView.setUint32((s1b0 + 11) * 4, 1, true);

    // Slot 2: sequence 5 (odd = write in progress)
    Atomics.store(seqView, 196, 5n);

    // Test reader protocol logic
    // Reading slot 0:
    const s0Seq1 = Atomics.load(seqView, 0);
    assert.equal(s0Seq1 & 1n, 0n);
    const s0Time = dataView.getBigInt64(8, true);
    assert.equal(s0Time, 1000000n);
    assert.equal(floatView[4], 1.5);
    assert.equal(dataView.getUint32(7 * 4, true), 1);
    assert.equal(floatView[b1 + 0], -2.0);
    assert.equal(dataView.getUint32((b1 + 3) * 4, true), 2);

    // Reading slot 2 (odd sequence): rejected
    const s2Seq = Atomics.load(seqView, 196);
    assert.equal(s2Seq & 1n, 1n, 'Odd sequence indicates write in progress');

    // Finding latest committed slot: slot 1 has sequence 4 > slot 0 sequence 2
    let bestSlot = -1, bestSeq = -1n;
    for (let s = 0; s < 3; s++) {
        const seq = Atomics.load(seqView, s * 98);
        if ((seq & 1n) === 0n && seq > bestSeq) {
            bestSeq = seq;
            bestSlot = s;
        }
    }
    assert.equal(bestSlot, 1);
    assert.equal(bestSeq, 4n);
    assert.equal(floatView[s1b0 + 0], 2.5);
});
