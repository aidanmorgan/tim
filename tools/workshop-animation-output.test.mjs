// Executes the shipped Animation transport with a managed export boundary, never a game solver.
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort, MessageChannel } from 'node:worker_threads';

const source = await readFile('CuriousContraptions.Animation.Worker/wwwroot/worker.js', 'utf8');
let imports, closed = false, controls = 0;
const messages = [];
const host = {
    OutputKinds: () => [1, 2, 3], TargetCapacity: () => 10,
    Bootstrap: () => {}, AnimationControl: () => { controls++; }
};
const runtime = {
    setModuleImports: (_, value) => { imports = value; },
    getAssemblyExports: async () => ({ Program: host }),
    getConfig: () => ({ mainAssemblyName: 'AnimationBoundaryControl' })
};
const self = {
    postMessage: (value, transfer = []) => messages.push(structuredClone(value, { transfer })),
    close: () => { closed = true; }
};
const context = vm.createContext({ self, Uint8Array, Float64Array, DataView, ArrayBuffer, MessagePort,
    Number, Error, Map, console, setInterval: () => 1, clearInterval: () => {} });
const dotnet = new vm.SyntheticModule(['dotnet'], function () {
    this.setExport('dotnet', { create: async () => runtime });
}, { context });
const clock = new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
    this.setExport('admitNativeClock', async () => {});
    this.setExport('nativeNow', () => 1);
    this.setExport('nativeClockEvidence', () => [2, 1, 1, 1, .005, 1, 1]);
}, { context });
const module = new vm.SourceTextModule(source, { context });
await module.link(name => {
    if (name === './_framework/dotnet.js') return dotnet;
    if (name === '../native-clock.js') return clock;
    throw Error('Unexpected import');
});
await module.evaluate();
const channel = new MessageChannel();
self.onmessage({ data: { bootstrap: new Uint8Array(64), clockPort: channel.port1 } });
self.onmessage({ data: { animationControl: new Uint8Array(96) } });
assert.equal(controls, 1, 'current renamed export is callable');
function sample(target, pulse) {
    const bytes = new Uint8Array(96), view = new DataView(bytes.buffer);
    view.setUint32(60, 2, true); view.setBigUint64(64, BigInt(target), true);
    view.setBigUint64(40, BigInt(pulse), true); return bytes;
}
imports.output(sample(1, 1)); // Hold its receipt while every admitted target publishes.
for (let target = 1; target <= host.TargetCapacity(); target++) imports.output(sample(target, 2));
for (let target = 1; target <= host.TargetCapacity(); target++) imports.output(sample(target, 3));
assert.equal(messages.filter(value => value.animationOutput).length, 1);
assert.throws(() => imports.output(sample(11, 3)), /capacity exceeded/);
const first = messages.at(-1).animationOutput;
self.onmessage({ data: { animationAcknowledged: first } });
for (let target = 1; target <= host.TargetCapacity(); target++) {
    const current = messages.at(-1).animationOutput;
    const view = new DataView(current.buffer, current.byteOffset, current.byteLength);
    assert.equal(view.getBigUint64(64, true), BigInt(target));
    assert.equal(view.getBigUint64(40, true), 3n, 'only latest per target survives');
    self.onmessage({ data: { animationAcknowledged: current } });
}
assert.equal(messages.filter(value => value.animationOutput).length, 11);
imports.output(sample(1, 4)); imports.output(sample(2, 4));
self.onmessage({ data: { dispose: true } });
const afterDispose = messages.length;
imports.output(sample(3, 5)); imports.qualified();
self.onmessage({ data: { animationAcknowledged: messages.at(-1).animationOutput } });
assert.equal(messages.length, afterDispose); assert.equal(closed, true);
channel.port2.close();
console.log(JSON.stringify({ passed: true, targets: host.TargetCapacity(), controls:
    ['declaredCapacity', 'heldReceipt', 'latestReplacement', 'orderedDrain', 'overflow', 'renamedControl', 'disposedLateOutput'] }));
