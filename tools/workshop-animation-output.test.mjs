// Executes the shipped Animation transport with a managed export boundary, never a game solver.
import assert from 'node:assert/strict';
import test from 'node:test';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort, MessageChannel } from 'node:worker_threads';
import * as clockRing from '../CuriousContraptions.web/wwwroot/clock-ring.js';
import * as eventRingCursor from '../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

test('every admitted target publishes its latest sample in order, holding one receipt, and a disposed worker publishes nothing', async () => {
    const source = await readFile('CuriousContraptions.Animation.Worker/wwwroot/worker.js', 'utf8');
    let imports, closed = false, controls = 0;
    const messages = [];
    const host = {
        OutputKinds: () => [1, 2, 3],
        Bootstrap: () => {}, AnimationControl: () => { controls++; }
    };
    // Story 9R.5b: the latest-sample table has no target capacity; this many targets is only the harness's population.
    const targets = 40;
    const runtime = {
        setModuleImports: (_, value) => { imports = value; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Animation: { Worker: { Program: host } } } }),
        getConfig: () => ({ mainAssemblyName: 'AnimationBoundaryControl' })
    };
    const self = {
        postMessage: (value, transfer = []) => messages.push(structuredClone(value, { transfer })),
        close: () => { closed = true; }
    };
    const context = vm.createContext({ self, Uint8Array, Float64Array, DataView, ArrayBuffer, MessagePort, SharedArrayBuffer,
        Number, Error, Map, console, setInterval: () => 1, clearInterval: () => {} });
    const ring = new vm.SyntheticModule(['attachClockRing', 'requestReply', 'ClockRole'], function () {
        this.setExport('attachClockRing', clockRing.attachClockRing);
        this.setExport('requestReply', clockRing.requestReply);
        this.setExport('ClockRole', clockRing.ClockRole);
    }, { context });
    const dotnet = new vm.SyntheticModule(['dotnet'], function () {
        this.setExport('dotnet', { create: async () => runtime });
    }, { context });
    const clock = new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
        this.setExport('admitNativeClock', async () => {});
        this.setExport('nativeNow', () => 1);
        this.setExport('nativeClockEvidence', () => [2, 1, 1, 1, .005, 1, 1]);
    }, { context });
    const cursor = new vm.SyntheticModule(['EventRingCursor', 'eventDelivery'], function () {
        this.setExport('EventRingCursor', eventRingCursor.EventRingCursor);
        this.setExport('eventDelivery', eventRingCursor.eventDelivery);
    }, { context });
    const module = new vm.SourceTextModule(source, { context });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return dotnet;
        if (name === '../native-clock.js') return clock;
        if (name === '../clock-ring.js') return ring;
        if (name === '../event-ring-cursor.js') return cursor;
        throw Error('Unexpected import');
    });
    await module.evaluate();
    const channel = new MessageChannel();
    self.onmessage({ data: { bootstrap: new Uint8Array(64), masterPort: channel.port1, clockRing: clockRing.createClockRing() } });
    self.onmessage({ data: { animationControl: new Uint8Array(96) } });
    assert.equal(controls, 1, 'current renamed export is callable');
    function sample(target, pulse) {
        const bytes = new Uint8Array(96), view = new DataView(bytes.buffer);
        view.setUint32(60, 2, true); view.setBigUint64(64, BigInt(target), true);
        view.setBigUint64(40, BigInt(pulse), true); return bytes;
    }
    imports.output(sample(1, 1)); // Hold its receipt while every admitted target publishes.
    for (let target = 1; target <= targets; target++) imports.output(sample(target, 2));
    for (let target = 1; target <= targets; target++) imports.output(sample(target, 3));
    assert.equal(messages.filter(value => value.animationOutput).length, 1);
    const first = messages.at(-1).animationOutput;
    self.onmessage({ data: { animationAcknowledged: first } });
    for (let target = 1; target <= targets; target++) {
        const current = messages.at(-1).animationOutput;
        const view = new DataView(current.buffer, current.byteOffset, current.byteLength);
        assert.equal(view.getBigUint64(64, true), BigInt(target));
        assert.equal(view.getBigUint64(40, true), 3n, 'only latest per target survives');
        self.onmessage({ data: { animationAcknowledged: current } });
    }
    assert.equal(messages.filter(value => value.animationOutput).length, targets + 1);
    imports.output(sample(1, 4)); imports.output(sample(2, 4));
    self.onmessage({ data: { dispose: true } });
    const afterDispose = messages.length;
    imports.output(sample(3, 5)); imports.qualified();
    self.onmessage({ data: { animationAcknowledged: messages.at(-1).animationOutput } });
    assert.equal(messages.length, afterDispose); assert.equal(closed, true);
    channel.port2.close();
    console.log(JSON.stringify({ passed: true, targets, controls:
        ['unboundedTargets', 'heldReceipt', 'latestReplacement', 'orderedDrain', 'renamedControl', 'disposedLateOutput'] }));
});
