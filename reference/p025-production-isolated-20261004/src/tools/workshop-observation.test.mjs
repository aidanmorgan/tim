// Supplemental control of the actual shipped worker module; no gameplay outcomes are fabricated.
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort } from 'node:worker_threads';

const source = await readFile('CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');
for (const captureMode of [1, 2]) {
    let imports, flushes = 0, release, throwOutput = false;
    const messages = [];
    const host = {
        OperationAbi: () => [0, 1], StateBytes: () => 18064, ScheduleRoles: () => [1, 2],
        CaptureMode: () => captureMode, Bootstrap: () => new Uint8Array(16),
        Dispatch: async () => { await new Promise(resolve => { release = resolve; }); },
        FlushObservation: () => { flushes++; if (throwOutput) throw Error('Output unavailable'); }
    };
    const runtime = {
        setModuleImports: (_, methods) => { imports = methods; },
        getAssemblyExports: async () => ({ Program: host }),
        getConfig: () => ({ mainAssemblyName: 'BoundaryControl' })
    };
    const self = {
        onmessage: undefined,
        postMessage: (message, transfer = []) => messages.push(structuredClone(message, { transfer })),
        close: () => {}
    };
    const context = vm.createContext({ self, Uint8Array, Float64Array, ArrayBuffer, DataView,
        MessagePort, console, setTimeout, clearTimeout, Promise, Error });
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
        throw Error('Unexpected worker import');
    });
    await module.evaluate();
    await self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode } });
    assert.equal(messages.at(-1).ready, true);
    function publication(identity) {
        const bytes = new Uint8Array(9744);
        bytes.fill(7, 64, 80);
        new DataView(bytes.buffer).setBigUint64(104, identity, true);
        return bytes;
    }
    function receipt(bytes) {
        const value = new Uint8Array(24);
        value.set(bytes.subarray(64, 80));
        value.set(bytes.subarray(104, 112), 16);
        return value;
    }
    const first = publication(1n), final = publication(2n);
    for (let index = 0; index < 8; index++) {
        const running = publication(BigInt(index + 10));
        imports.publish(running, false);
        await self.onmessage({ data: { readAcknowledged: receipt(running) } });
    }
    assert.equal(flushes, 0, 'ordinary Running reads never call the observation export');
    imports.publish(first, false);
    imports.publish(final, true);
    assert.equal(flushes, 0);
    const wrong = receipt(first); wrong[0] ^= 1;
    await self.onmessage({ data: { readAcknowledged: wrong } });
    assert.equal(flushes, 0);
    await self.onmessage({ data: { readAcknowledged: receipt(first) } });
    assert.equal(flushes, 0, 'queued terminal is pending before observation flush');
    const command = self.onmessage({ data: { bytes: new Uint8Array(72) } });
    await Promise.resolve();
    await self.onmessage({ data: { readAcknowledged: receipt(final) } });
    assert.equal(flushes, 0, 'active command suppresses observation flush');
    release();
    await command;
    assert.equal(flushes, 1);
    await self.onmessage({ data: { readAcknowledged: receipt(final) } });
    assert.equal(flushes, 1, 'duplicate receipt does not flush again');
    throwOutput = true;
    const last = publication(3n);
    imports.publish(last, true);
    const failuresBefore = messages.filter(item => item.failure).length;
    await self.onmessage({ data: { readAcknowledged: receipt(last) } });
    assert.equal(flushes, 2);
    assert.equal(messages.filter(item => item.failure).length, failuresBefore, 'observation failure is contained');
    throwOutput = false;
    const reset = self.onmessage({ data: { bytes: new Uint8Array(72) } });
    await Promise.resolve();
    imports.acknowledge(new Uint8Array(9744), true);
    assert.equal(flushes, 2);
    release();
    await reset;
    assert.equal(flushes, 3, 'acknowledgement-only Reset announces sealed observation');
    imports.dispose();
    console.log(JSON.stringify({ captureMode, passed: true, controls: ['pending', 'staleReceipt',
        'queuedTerminal', 'activeCommand', 'matchingReceipt', 'duplicateReceipt', 'outputFailure', 'ordinaryReadNoExport', 'acknowledgementOnlyReset', 'dispose'] }));
}
