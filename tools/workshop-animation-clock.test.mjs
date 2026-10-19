// Murdoch-2 R2-F2 (Story 9.4b): the Animation worker's clock-ring cleanup, run against the shipped worker.js. A disposed
// worker holds no clock ring (it refuses to probe it), and a reply the master writes after disposal (the probe was already
// waiting) never reaches the managed host.
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';
import { MessageChannel, MessagePort } from 'node:worker_threads';
import * as clockRing from '../CuriousContraptions.web/wwwroot/clock-ring.js';

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
    let imports;
    const replies = [], messages = [], channel = new MessageChannel(), ring = clockRing.createClockRing();
    const host = {
        OutputKinds: () => [1, 2, 3], TargetCapacity: () => 10, Bootstrap: () => {}, AnimationControl: () => {}, Service: () => {},
        Reply: bytes => replies.push(bytes)
    };
    const runtime = {
        setModuleImports: (_, value) => { imports = value; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Animation: { Worker: { Program: host } } } }),
        getConfig: () => ({ mainAssemblyName: 'AnimationClockControl' })
    };
    const self = { postMessage: (value, transfer = []) => messages.push(structuredClone(value, { transfer })), close: () => {} };
    const context = vm.createContext({ self, Uint8Array, Float64Array, DataView, ArrayBuffer, MessagePort, SharedArrayBuffer,
        Number, Error, Map, console, setInterval: () => 1, clearInterval: () => {} });
    const module = new vm.SourceTextModule(source, { context });
    const synthetic = (names, values) => new vm.SyntheticModule(names, function () { for (const name of names) this.setExport(name, values[name]); }, { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return synthetic(['dotnet'], { dotnet: { create: async () => runtime } });
        if (name === '../native-clock.js') return synthetic(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'],
            { admitNativeClock: async () => {}, nativeNow: () => 1, nativeClockEvidence: () => [2, 1, 1, 1, .005, 1, 1] });
        if (name === '../clock-ring.js') return synthetic(['attachClockRing', 'requestReply', 'ClockRole'], clockRing);
        throw Error('Unexpected import ' + name);
    });
    await module.evaluate();
    self.onmessage({ data: { bootstrap: new Uint8Array(64), masterPort: channel.port1, clockRing: ring } });
    const master = clockRing.attachClockRing(ring);
    return {
        imports, replies, messages, self,
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
