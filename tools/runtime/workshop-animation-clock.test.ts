interface AnimationImports { probe(bytes: Uint8Array): void; output(bytes: Uint8Array): void; qualified(): void; }
interface Posted { failure?: boolean; animationOutput: Uint8Array; }
interface WorkerSelf { postMessage(value: Posted, transfer?: ArrayBuffer[]): void; close(): void; onmessage(event: { data: unknown }): void; }
// Murdoch-2 R2-F2 (Story 9.4b): the Animation worker's clock-ring cleanup, run against the shipped worker.js. A disposed
// worker holds no clock ring (it refuses to probe it), and a reply the master writes after disposal (the probe was already
// waiting) never reaches the managed host.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';
import { MessageChannel, MessagePort } from 'node:worker_threads';
import * as clockRing from '../../CuriousContraptions.web/wwwroot/clock-ring.js';
import * as eventRingCursor from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

const source = await readFile('CuriousContraptions.Animation.Worker/wwwroot/worker.js', 'utf8');
class ControlledPort {
    onmessage: ((event: { data: unknown }) => void) | null = null;
    start() {} close() { this.onmessage = null; }
}


// The Animation peer as the master expects it: requester role Animation (3) at 56, master role Simulation (2) at 60.
const peer = new Uint8Array(clockRing.PeerBytes);
new DataView(peer.buffer).setUint32(56, clockRing.ClockRole.Animation, true);
new DataView(peer.buffer).setUint32(60, clockRing.ClockRole.Simulation, true);
function probe(sequence: number) {
    const bytes = new Uint8Array(clockRing.ProbeBytes), view = new DataView(bytes.buffer);
    bytes.set(peer, 0); view.setBigUint64(64, BigInt(sequence), true); view.setBigInt64(72, 0n, true);
    return bytes;
}

async function animationWorker() {
    let imports!: AnimationImports, service!: () => void;
    const replies: Uint8Array[] = [], controls: Uint8Array[] = [], reads: Uint8Array[] = [], messages: Posted[] = [], deliveries: Uint8Array[] = [], channel = { port1: new ControlledPort() }, ring = clockRing.createClockRing();
    // Story 9A.23: the installed epoch the reader may consume, and the output produced from consumed events. EventRecords refuses a delivery of
    // another epoch exactly as WorkshopDeclaredPresentation.AcceptEvents does ("Foreign event ring epoch."), so a leak faults the worker.
    const installed = { epoch: 0 };
    const waits: Promise<Uint8Array | null>[] = [];
    const host = {
        OutputKinds: () => [1, 2, 3], Bootstrap: () => {}, AnimationControl: () => {},
        Service: () => {
            // External host output carries its consumed-delivery count. Posted output proves
            // the worker services after delivery, including turns without a matching ring.
            const output = new Uint8Array(104), view = new DataView(output.buffer);
            view.setUint32(60, 1, true);
            view.setBigUint64(40, BigInt(deliveries.length), true);
            imports.output(output);
        },
        Reply: (bytes: Uint8Array) => replies.push(bytes),
        ScheduleControl: (bytes: Uint8Array) => controls.push(bytes), CommittedRead: (bytes: Uint8Array) => reads.push(bytes),
        EventEpoch: () => installed.epoch.toString(), EventRecords: (bytes: Uint8Array) => {
            if (new DataView(bytes.buffer, bytes.byteOffset, 8).getBigUint64(0, true) !== BigInt(installed.epoch))
                throw new Error('Foreign event ring epoch.');
            deliveries.push(bytes);
        }
    };
    const runtime = {
        setModuleImports: (_: string, value: AnimationImports) => { imports = value; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Animation: { Worker: { Program: host } } } }),
        getConfig: () => ({ mainAssemblyName: 'AnimationClockControl' })
    };
    const self: WorkerSelf = { onmessage: () => {}, postMessage: (value, transfer = []) => messages.push(structuredClone(value, { transfer })), close: () => {} };
    const context = vm.createContext({ self, Uint8Array, Float64Array, DataView, ArrayBuffer, MessagePort: ControlledPort, SharedArrayBuffer,
        Number, Error, Map, BigInt, console, setInterval: (callback: () => void) => { service = callback; return 1; }, clearInterval: () => {} });
    const module = new vm.SourceTextModule(source, { context });
    const synthetic = (names: string[], values: Record<string, unknown>) => new vm.SyntheticModule(names, function () { for (const name of names) this.setExport(name, values[name]); }, { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return synthetic(['dotnet'], { dotnet: { create: async () => runtime } });
        if (name === '../native-clock.js') return synthetic(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'],
            { admitNativeClock: async () => {}, nativeNow: () => 1, nativeClockEvidence: () => [2, 1, 1, 1, .005, 1, 1] });
        if (name === '../clock-ring.js') return synthetic(['attachClockRing', 'requestReply', 'ClockRole'], { ...clockRing, requestReply: (...args: Parameters<typeof clockRing.requestReply>) => { const pending = clockRing.requestReply(...args); waits.push(pending); return pending; } });
        if (name === '../event-ring-cursor.js') return synthetic(['EventRingCursor', 'eventDelivery'], eventRingCursor);
        throw Error('Unexpected import ' + name);
    });
    await module.evaluate();
    self.onmessage({ data: { bootstrap: new Uint8Array(64), masterPort: channel.port1, clockRing: ring } });
    const master = clockRing.attachClockRing(ring);
    return {
        imports, replies, controls, reads, messages, self, deliveries, installed, turn: () => service(),
        send: (message: unknown) => channel.port1.onmessage?.({ data: message }),
        settled: () => Promise.all(waits),
        serve: () => clockRing.serveRequests(master, { [clockRing.ClockRole.Animation]: peer }, () => 1_000_000n),
        close: () => channel.port1.close()
    };
}

test('a served Animation probe reaches the host (control)', async () => {
    const worker = await animationWorker();
    worker.imports.probe(probe(1));
    worker.serve(); await worker.settled();
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
    worker.serve(); await worker.settled();  // the master answers the waiting probe after disposal
    assert.equal(worker.replies.length, 0);
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'a late reply is discarded, not a failure');
    worker.close();
});

// Story 9A.23 D1/D5: the Simulation worker posts each event ring on the master port; every Service turn first takes the whole records
// committed since this worker's own cursor (the shared reader), only while the ring's epoch is the installed one, then services.
function eventRing(epoch: bigint, sequences: bigint[]) {
    const buffer = new SharedArrayBuffer(64 + 4 * 64), view = new DataView(buffer);
    view.setUint32(0, 4, true); view.setUint32(4, 64, true); view.setBigUint64(8, epoch, true);
    for (const sequence of sequences) view.setBigUint64(64 + Number((sequence - 1n) % 4n) * 64, sequence, true);
    view.setBigUint64(16, sequences.at(-1) ?? 0n, true);
    return buffer;
}
test('each Service turn first delivers the new records of the installed epoch, once', async () => {
    const worker = await animationWorker();
    worker.send({ eventRing: eventRing(5n, [1n, 2n]) }); await worker.settled();
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'the ring envelope is the master port\'s own');
    worker.turn();
    assert.equal(worker.deliveries.length, 0, 'no world installed (epoch 0): the ring is held unread');
    worker.installed.epoch = 5;
    worker.turn();
    assert.equal(worker.deliveries.length, 1);
    const records: unknown = worker.deliveries[0]; assert.ok(records instanceof Uint8Array);
    const delivery = new DataView(records.buffer, records.byteOffset, records.byteLength);
    assert.deepEqual([delivery.getBigUint64(0, true), delivery.getBigUint64(8, true), records.length,
        delivery.getBigUint64(16, true), delivery.getBigUint64(80, true)], [5n, 0n, 16 + 128, 1n, 2n]);
    worker.turn();
    assert.deepEqual(worker.messages.filter(message => message.animationOutput).map(message =>
        new DataView(message.animationOutput.buffer).getBigUint64(40, true)), [0n, 1n, 1n]);
    assert.equal(worker.deliveries.length, 1, 'nothing new: no record is delivered twice');
    worker.close();
});

// Review F2: a ring older than the installed epoch (its world was replaced, and the new ring is not yet posted) is ignored by the
// worker: nothing reaches EventRecords, so AcceptEvents never sees a foreign epoch, and every turn still services.
test('a ring older than the installed epoch is ignored: no delivery, no foreign-epoch fault, and the turn still services', async () => {
    const worker = await animationWorker();
    worker.send({ eventRing: eventRing(5n, [1n, 2n]) }); await worker.settled();
    worker.installed.epoch = 6;
    worker.turn(); worker.turn();
    assert.deepEqual(worker.messages.filter(message => message.animationOutput).map(message =>
        new DataView(message.animationOutput.buffer).getBigUint64(40, true)), [0n, 0n]);
    assert.equal(worker.deliveries.length, 0, 'no delivery of the older epoch');
    assert.equal(worker.messages.filter(message => message.failure).length, 0, 'the worker is not faulted');
    worker.close();
});

for (const name of ['scheduleControl', 'committedRead']) {
    test('owned master-port accepts exactly one ' + name + ' payload', async () => {
        const worker = await animationWorker();
        worker.send({ [name]: new Uint8Array([1, 2, 3]) }); await worker.settled();
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
        worker.send(envelope); await worker.settled();
        assert.equal(worker.controls.length, 0); assert.equal(worker.reads.length, 0);
        assert.equal(worker.messages.filter(message => message.failure).length, 1);
        worker.close();
    });
}
