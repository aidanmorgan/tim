// Supplemental control of the actual shipped worker module; no gameplay outcomes are fabricated.
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort } from 'node:worker_threads';

const source = await readFile('CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');
async function loadWorker(host, additionalGlobals = {}) {
    let imports;
    const messages = [];
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
        MessagePort, console, setTimeout, clearTimeout, Promise, Error, ...additionalGlobals });
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
    return { self, messages, get imports() { return imports; } };
}

for (const captureMode of [1, 2]) {
    let flushes = 0, release, releasePreparation, preparations = 0, throwOutput = false;
    const stallStates = [];
    const host = {
        CommandAbi: () => [72, 5768], ResponseAbi: () => [24336, 40, 56], OperationAbi: () => [0, 1], StateBytes: () => 154048, PrismaticRowKinds: () => [0, 1, 2, 3, 4, 5, 6, 7], ScheduleRoles: () => [1, 2],
        CaptureMode: () => captureMode, Bootstrap: () => new Uint8Array(16),
        PrepareGpu: () => { preparations++; return new Promise(resolve => { releasePreparation = resolve; }); },
        ClockReply: () => new Uint8Array(144),
        Dispatch: async () => { await new Promise(resolve => { release = resolve; }); },
        FlushObservation: () => { flushes++; if (throwOutput) throw Error('Output unavailable'); },
        SetReliableStall: value => stallStates.push(value)
    };
    const boundary = await loadWorker(host);
    const { self, messages, imports } = boundary;
    const preparing = self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode } });
    assert.equal(messages.at(-1).ready, true);
    assert.equal(preparations, 1);
    await self.onmessage({ data: { clockProbe: new Uint8Array(96) } });
    assert.ok(messages.at(-1).clockReply instanceof Uint8Array, 'clock work proceeds while preparation is held');
    assert.equal(messages.filter(item => item.read || item.acknowledgement).length, 0, 'preparation cannot publish a world');
    releasePreparation();
    await preparing;
    function publication(identity) {
        const bytes = new Uint8Array(24336);
        bytes.fill(7, 40, 56);
        new DataView(bytes.buffer).setBigUint64(56, identity, true);
        return bytes;
    }
    function receipt(bytes) {
        const value = new Uint8Array(24);
        value.set(bytes.subarray(40, 56));
        value.set(bytes.subarray(56, 64), 16);
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
    imports.acknowledge(new Uint8Array(24336), true);
    assert.equal(flushes, 2);
    release();
    await reset;
    assert.equal(flushes, 3, 'acknowledgement-only Reset announces sealed observation');
    await self.onmessage({ data: { reliableStalled: true } });
    await self.onmessage({ data: { reliableStalled: false } });
    assert.deepEqual(stallStates, [true, false], 'actual worker forwards current boolean state');
    const held = publication(100n), eventOne = publication(102n), eventTwo = publication(104n), latest = publication(105n);
    imports.publish(held, false);
    imports.publish(publication(101n), false);
    assert.equal(imports.reserveOccurrenceRead(), true);
    imports.publish(eventOne, false, true);
    imports.publish(publication(103n), false);
    assert.equal(imports.reserveOccurrenceRead(), true);
    imports.publish(eventTwo, false, true);
    imports.publish(latest, false);
    const controlsBefore = messages.filter(item => item.scheduleControl).length;
    imports.scheduleControl(new Uint8Array([7]), 1);
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore);
    for (const expected of [eventOne, eventTwo, latest]) {
        const current = messages.filter(item => item.read).at(-1).read;
        await self.onmessage({ data: { readAcknowledged: receipt(current) } });
        assert.deepEqual(messages.filter(item => item.read).at(-1).read, expected);
        assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore,
            'installation cannot overtake a committed occurrence or final endpoint');
    }
    await self.onmessage({ data: { readAcknowledged: receipt(latest) } });
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore + 1);
    const capacityHeld = publication(200n);
    imports.publish(capacityHeld, false);
    for (let index = 0; index < 64; index++) {
        assert.equal(imports.reserveOccurrenceRead(), true);
        imports.publish(publication(BigInt(201 + index)), false, true);
    }
    assert.equal(imports.reserveOccurrenceRead(), false, 'backpressure precedes the next GPU commit');
    await self.onmessage({ data: { readAcknowledged: receipt(capacityHeld) } });
    assert.equal(imports.reserveOccurrenceRead(), true, 'exact ACK returns one reserved capacity slot');
    imports.releaseOccurrenceRead();
    for (let index = 0; index < 64; index++)
        await self.onmessage({ data: { readAcknowledged: receipt(publication(BigInt(201 + index))) } });
    imports.dispose();
    await self.onmessage({ data: { reliableStalled: true } });
    assert.deepEqual(stallStates, [true, false], 'disposed worker cannot update stall state');
    console.log(JSON.stringify({ captureMode, passed: true, controls: ['preparationClockOverlap', 'noEarlyWorld', 'pending', 'staleReceipt',
        'queuedTerminal', 'activeCommand', 'matchingReceipt', 'duplicateReceipt', 'outputFailure', 'ordinaryReadNoExport', 'acknowledgementOnlyReset', 'currentReliableStall', 'dispose'] }));
}

const StartupControl = Object.freeze({ Reject: 1, DisposeThenReject: 2, EarlyLoss: 3 });
for (const control of Object.values(StartupControl)) {
    let rejectPreparation, loseDevice, boundary;
    let deviceDestroyed = false;
    const gpu = {
        lost: new Promise(resolve => { loseDevice = resolve; }),
        destroy: () => { deviceDestroyed = true; }
    };
    const host = {
        CommandAbi: () => [72, 5768], ResponseAbi: () => [24336, 40, 56], OperationAbi: () => [0, 1], StateBytes: () => 154048, PrismaticRowKinds: () => [0, 1, 2, 3, 4, 5, 6, 7], ScheduleRoles: () => [1, 2],
        CaptureMode: () => 1, Bootstrap: () => new Uint8Array(16),
        PrepareGpu: () => control === StartupControl.EarlyLoss
            ? boundary.imports.initialize('fixture preamble')
            : new Promise((_, reject) => { rejectPreparation = reject; }),
        DeviceLost: () => { throw Error('No Simulation exists during startup preparation'); }
    };
    boundary = await loadWorker(host, {
        navigator: { gpu: { requestAdapter: async () => ({ features: new Set(), requestDevice: async () => gpu }) } }
    });
    const pending = boundary.self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode: 1 } });
    assert.equal(boundary.messages.filter(item => item.ready).length, 1);
    if (control === StartupControl.EarlyLoss) {
        await pending;
        loseDevice({ message: 'fixture loss' });
        for (let i = 0; i < 10; i++) await Promise.resolve();
        assert.equal(boundary.messages.filter(item => item.failure).length, 1);
        assert.equal(deviceDestroyed, true, 'late preparation cannot retain its retired device');
    } else {
        if (control === StartupControl.DisposeThenReject) boundary.imports.dispose();
        rejectPreparation(Error('fixture preparation rejection'));
        await pending;
    }
    assert.equal(boundary.messages.filter(item => item.failure).length,
        control === StartupControl.DisposeThenReject ? 0 : 1, 'late failures cannot resurrect disposed ownership');
    assert.equal(boundary.messages.filter(item => item.read || item.acknowledgement).length, 0);
    console.log(JSON.stringify({ startupControl: control, passed: true }));
}

// The transport width comes from the managed canonical ABI, including new declaration lanes.
{
    let dispatched = 0;
    const host = {
        CommandAbi: () => [72, 5768], ResponseAbi: () => [24336, 40, 56], OperationAbi: () => [0, 1], StateBytes: () => 154048,
        PrismaticRowKinds: () => [0, 1, 2, 3, 4, 5, 6, 7], ScheduleRoles: () => [1, 2], Dispatch: async () => { dispatched++; }
    };
    const { self, messages } = await loadWorker(host);
    for (const width of [72, 1672, 5768]) await self.onmessage({ data: { bytes: new Uint8Array(width) } });
    assert.equal(dispatched, 3);
    for (const width of [71, 5769]) await self.onmessage({ data: { bytes: new Uint8Array(width) } });
    assert.equal(dispatched, 3);
    assert.equal(messages.filter(message => message.rejected).length, 2);
    console.log(JSON.stringify({ canonicalCommandWidth: true, accepted: [72, 1672, 5768], rejected: [71, 5769] }));
}
