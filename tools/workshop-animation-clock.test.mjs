// Murdoch-2 R2-F2 (Story 9.4b): the Animation worker's clock-ring cleanup, run against the shipped worker.js. A disposed
// worker holds no clock ring (it refuses to probe it), and a reply the master writes after disposal (the probe was already
// waiting) never reaches the managed host.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';
import { MessageChannel, MessagePort } from 'node:worker_threads';
import * as clockRing from '../CuriousContraptions.web/wwwroot/clock-ring.js';
import * as eventRingCursor from '../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

const source = await readFile(new URL('../CuriousContraptions.Animation.Worker/wwwroot/worker.js', import.meta.url), 'utf8');
const settle = () => new Promise(resolve => setTimeout(resolve, 20));

// The Animation peer as the master expects it: requester role Animation (3) at 56, master role Simulation (2) at 60.
const peer = new Uint8Array(clockRing.PeerBytes);
new DataView(peer.buffer).setUint32(56, clockRing.ClockRole.Animation, true);
new DataView(peer.buffer).setUint32(60, clockRing.ClockRole.Simulation, true);
function probe(sequence) {
    const bytes = new Uint8Array(clockRing.ProbeBytes), view = new DataView(bytes.buffer);
    bytes.set(peer, 0); view.setBigUint64(64, BigInt(sequence), true); view.setBigInt64(72, 0n, true);
    return bytes;
}

async function animationWorker() {
    let imports, service;
    const replies = [], controls = [], reads = [], messages = [], turns = [], channel = new MessageChannel(), ring = clockRing.createClockRing();
    // Story 9A.23: the installed epoch the reader may consume, and each turn's managed calls in order. EventRecords refuses a delivery of
    // another epoch exactly as WorkshopDeclaredPresentation.AcceptEvents does ("Foreign event ring epoch."), so a leak faults the worker.
    const installed = { epoch: 0 };
    const host = {
        OutputKinds: () => [1, 2, 3], Bootstrap: () => {}, AnimationControl: () => {}, Service: () => turns.push('Service'),
        Reply: bytes => replies.push(bytes),
        ScheduleControl: bytes => controls.push(bytes), CommittedRead: bytes => reads.push(bytes),
        EventEpoch: () => installed.epoch, EventRecords: bytes => {
            if (new DataView(bytes.buffer, bytes.byteOffset, 8).getBigUint64(0, true) !== BigInt(installed.epoch))
                throw new Error('Foreign event ring epoch.');
            turns.push(bytes);
        }
    };
    const runtime = {
        setModuleImports: (_, value) => { imports = value; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Animation: { Worker: { Program: host } } } }),
        getConfig: () => ({ mainAssemblyName: 'AnimationClockControl' })
    };
    const self = { postMessage: (value, transfer = []) => messages.push(structuredClone(value, { transfer })), close: () => {} };
    const context = vm.createContext({ self, Uint8Array, Float64Array, DataView, ArrayBuffer, MessagePort, SharedArrayBuffer,
        Number, Error, Map, BigInt, console, setInterval: callback => { service = callback; return 1; }, clearInterval: () => {} });
    const module = new vm.SourceTextModule(source, { context });
    const synthetic = (names, values) => new vm.SyntheticModule(names, function () { for (const name of names) this.setExport(name, values[name]); }, { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return synthetic(['dotnet'], { dotnet: { create: async () => runtime } });
        if (name === '../native-clock.js') return synthetic(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'],
            { admitNativeClock: async () => {}, nativeNow: () => 1, nativeClockEvidence: () => [2, 1, 1, 1, .005, 1, 1] });
        if (name === '../clock-ring.js') return synthetic(['attachClockRing', 'requestReply', 'ClockRole'], clockRing);
        if (name === '../event-ring-cursor.js') return synthetic(['EventRingCursor', 'eventDelivery'], eventRingCursor);
        throw Error('Unexpected import ' + name);
    });
    await module.evaluate();
    self.onmessage({ data: { bootstrap: new Uint8Array(64), masterPort: channel.port1, clockRing: ring } });
    const master = clockRing.attachClockRing(ring);
    return {
        imports, replies, controls, reads, messages, self, turns, installed, turn: () => service(),
        send: message => channel.port2.postMessage(message),
        serve: () => clockRing.serveRequests(master, { [clockRing.ClockRole.Animation]: peer }, () => 1),
        close: () => channel.port2.close()
    };
}

test('a served Animation probe reaches the host (control)', async () => {
    const worker = await animationWorker();
    worker.imports.probe(probe(1));
    worker.serve(); await settle();
    assert.equal(worker.replies.length, 1);
    worker.close();
});

test('a disposed Animation worker holds no clock ring and refuses to probe it', async () => {
    const worker = await animationWorker();
    worker.self.onmessage({ data: { dispose: true } });
    assert.throws(() => worker.imports.probe(probe(1)), /clock ring is retired/);
    worker.close();
});

test('a clock reply landing after the Animation worker was disposed never reaches the host', async () => {
    const worker = await animationWorker();
    worker.imports.probe(probe(1)); // the master has not answered it yet
    worker.self.onmessage({ data: { dispose: true } });
    worker.serve(); await settle();  // the master answers the waiting probe after disposal
    assert.equal(worker.replies.length, 0);
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'a late reply is discarded, not a failure');
    worker.close();
});

// Story 9A.23 D1/D5: the Simulation worker posts each event ring on the master port; every Service turn first takes the whole records
// committed since this worker's own cursor (the shared reader), only while the ring's epoch is the installed one, then services.
function eventRing(epoch, sequences) {
    const buffer = new SharedArrayBuffer(64 + 4 * 64), view = new DataView(buffer);
    view.setUint32(0, 4, true); view.setUint32(4, 64, true); view.setBigUint64(8, epoch, true);
    for (const sequence of sequences) view.setBigUint64(64 + Number((sequence - 1n) % 4n) * 64, sequence, true);
    view.setBigUint64(16, sequences.at(-1) ?? 0n, true);
    return buffer;
}
test('each Service turn first delivers the new records of the installed epoch, once', async () => {
    const worker = await animationWorker();
    worker.send({ eventRing: eventRing(5n, [1n, 2n]) }); await settle();
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'the ring envelope is the master port\'s own');
    worker.turn();
    assert.deepEqual(worker.turns, ['Service'], 'no world installed (epoch 0): the ring is held unread');
    worker.installed.epoch = 5; worker.turns.length = 0;
    worker.turn();
    assert.equal(worker.turns.length, 2); assert.equal(worker.turns[1], 'Service', 'the records precede the turn\'s evaluation');
    const delivery = new DataView(worker.turns[0].buffer, worker.turns[0].byteOffset, worker.turns[0].byteLength);
    assert.deepEqual([delivery.getBigUint64(0, true), delivery.getBigUint64(8, true), worker.turns[0].length,
        delivery.getBigUint64(16, true), delivery.getBigUint64(80, true)], [5n, 0n, 16 + 128, 1n, 2n]);
    worker.turns.length = 0; worker.turn();
    assert.deepEqual(worker.turns, ['Service'], 'nothing new: no delivery, and no record twice');
    worker.close();
});

// Review F2: a ring older than the installed epoch (its world was replaced, and the new ring is not yet posted) is ignored by the
// worker: nothing reaches EventRecords, so AcceptEvents never sees a foreign epoch, and every turn still services.
test('a ring older than the installed epoch is ignored: no delivery, no foreign-epoch fault, and the turn still services', async () => {
    const worker = await animationWorker();
    worker.send({ eventRing: eventRing(5n, [1n, 2n]) }); await settle();
    worker.installed.epoch = 6;
    worker.turn(); worker.turn();
    assert.deepEqual(worker.turns, ['Service', 'Service'], 'no delivery of the older epoch, and the turn still services');
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'the worker is not faulted');
    worker.close();
});

for (const name of ['scheduleControl', 'committedRead']) {
    test('owned master-port accepts exactly one ' + name + ' payload', async () => {
        const worker = await animationWorker();
        worker.send({ [name]: new Uint8Array([1, 2, 3]) }); await settle();
        assert.equal(worker.controls.length, name === 'scheduleControl' ? 1 : 0);
        assert.equal(worker.reads.length, name === 'committedRead' ? 1 : 0);
        assert.equal(worker.messages.filter(message => message.failure).length, 0);
        worker.close();
    });
}
for (const envelope of [
    { scheduleControl: new Uint8Array(1), committedRead: new Uint8Array(1) },
    { committedRead: new Uint8Array(1), unexpected: true },
    { scheduleControl: new Uint8Array(1), unexpected: true },
    { unexpected: new Uint8Array(1) }
]) {
    test('mixed or unknown master-port envelope calls neither managed entry', async () => {
        const worker = await animationWorker();
        worker.send(envelope); await settle();
        assert.equal(worker.controls.length, 0); assert.equal(worker.reads.length, 0);
        assert.equal(worker.messages.filter(message => message.failure).length, 1);
        worker.close();
    });
}
