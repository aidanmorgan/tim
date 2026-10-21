// Supplemental control of the actual shipped worker module; no gameplay outcomes are fabricated.
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort, MessageChannel, Worker as NodeWorker } from 'node:worker_threads';
import { pathToFileURL } from 'node:url';
import * as clockRing from '../CuriousContraptions.web/wwwroot/clock-ring.js';

const source = await readFile('CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');
const workerUrl = pathToFileURL('CuriousContraptions.Simulation/wwwroot/worker.js').href;
const ringModuleUrl = new URL('../CuriousContraptions.web/wwwroot/clock-ring.js', import.meta.url).href;
// A stand-in for the Simulation-owned clock master context: a real blocking thread on the shared clock ring that
// answers in the stub native time (1 ms) the worker module reads here, so its live qualification can pass.
const masterScript = `
const { parentPort, workerData } = require('node:worker_threads');
import(workerData.ringModule).then(ring => {
    parentPort.once('message', bootstrap => {
        const clock = ring.attachClockRing(bootstrap.ring);
        const peers = { [ring.ClockRole.Browser]: bootstrap.browserPeer, [ring.ClockRole.Animation]: bootstrap.animationPeer };
        parentPort.postMessage({ ready: true });
        for (;;) ring.awaitRequest(clock, ring.serveRequests(clock, peers, () => 1));
    });
    parentPort.postMessage({ awaitingBootstrap: true });
});`;
class StandInClockMaster {
    constructor() {
        this.node = new NodeWorker(masterScript, { eval: true, workerData: { ringModule: ringModuleUrl } });
        this.node.on('message', data => this.onmessage?.({ data }));
        this.node.on('error', error => this.onerror?.({ message: String(error) }));
        this.node.unref();
    }
    postMessage(message) { this.node.postMessage(message); }
    terminate() { this.node.terminate(); }
}
async function until(predicate, label) {
    for (let attempt = 0; attempt < 2000 && !predicate(); attempt++) await new Promise(resolve => setTimeout(resolve, 1));
    assert.ok(predicate(), label);
}
// The worker's managed derivation check (WorkshopClockDerivation), recorded: the stand-in master and the stub native
// clock both read 1 ms, so every exchange is narrow and consistent and no correction is ever installed.
function derivationHost() {
    const record = { qualified: 0, startupMissed: 0, checks: 0, missed: 0, adopted: 0, narrowQueries: 0, retried: 0 };
    return {
        record,
        // [exchange timeout ms, check interval ms, exchanges per check] (WorkshopClockDerivation).
        ClockDerivationProfile: () => [2, 100, 4],
        ClockDerivationQualified: () => record.qualified === 16,
        QualifyClockDerivation: (before, master, after, epoch) => {
            assert.ok(before === 1 && master === 1 && after === 1 && epoch === 0, 'a consistent startup exchange');
            record.qualified++;
        },
        ClockDerivationQualificationMissed: () => { record.startupMissed++; },
        // The stand-in master and the stub native clock agree exactly, so the derived startup offset is zero.
        ClockDerivationStartupCorrection: () => 0,
        // An empty installation: the check was consistent, so nothing is written to the ring.
        CheckClockDerivation: () => { record.checks++; return []; },
        AdoptClockDerivation: () => { record.adopted++; },
        ClockDerivationMissed: () => { record.missed++; },
        // The first exchange of every check reads as wide, so each check retries exactly once before its narrow one.
        ClockDerivationExchangeNarrow: () => (record.narrowQueries++ % 2) === 1,
        ClockDerivationRetried: () => { record.retried++; }
    };
}
// The worker's run-loop yield channel, unreferenced so the harness process can exit once its checks complete.
class HarnessMessageChannel {
    constructor() {
        const channel = new MessageChannel();
        channel.port2.unref();
        this.port2 = channel.port2;
        this.port1 = { set onmessage(handler) { channel.port1.onmessage = handler; channel.port1.unref(); },
            get onmessage() { return channel.port1.onmessage; }, close: () => channel.port1.close() };
    }
}
async function loadWorker(host, additionalGlobals = {}) {
    let imports;
    const messages = [];
    const runtime = {
        setModuleImports: (_, methods) => { imports = methods; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
        getConfig: () => ({ mainAssemblyName: 'BoundaryControl' })
    };
    const self = {
        onmessage: undefined,
        postMessage: (message, transfer = []) => messages.push(structuredClone(message, { transfer })),
        close: () => {}
    };
    const context = vm.createContext({ self, Uint8Array, Float64Array, ArrayBuffer, DataView,
        MessagePort, MessageChannel: HarnessMessageChannel, console, setTimeout, clearTimeout, Promise, Error, URL, SharedArrayBuffer, Atomics, performance,
        // The derivation check interval, unreferenced so the harness process can exit.
        setInterval: (callback, milliseconds) => setInterval(callback, milliseconds).unref(), clearInterval,
        Worker: StandInClockMaster, ...additionalGlobals });
    const dotnet = new vm.SyntheticModule(['dotnet'], function () {
        this.setExport('dotnet', { create: async () => runtime });
    }, { context });
    const ring = new vm.SyntheticModule(['attachClockRing', 'deriveExchange', 'qualifyDerivation', 'writeDerivation', 'beat'], function () {
        this.setExport('attachClockRing', clockRing.attachClockRing);
        this.setExport('deriveExchange', clockRing.deriveExchange);
        this.setExport('qualifyDerivation', clockRing.qualifyDerivation);
        this.setExport('writeDerivation', clockRing.writeDerivation);
        this.setExport('beat', clockRing.beat);
    }, { context });
    const clock = new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
        this.setExport('admitNativeClock', async () => {});
        this.setExport('nativeNow', () => 1);
        this.setExport('nativeClockEvidence', () => [2, 1, 1, 1, .005, 1, 1]);
    }, { context });
    const module = new vm.SourceTextModule(source, { context, initializeImportMeta: meta => { meta.url = workerUrl; } });
    await module.link(name => {
        if (name === './_framework/dotnet.js') return dotnet;
        if (name === '../native-clock.js') return clock;
        if (name === '../clock-ring.js') return ring;
        throw Error('Unexpected worker import');
    });
    await module.evaluate();
    return { self, messages, get imports() { return imports; } };
}

for (const captureMode of [1, 2]) {
    let flushes = 0, release, releasePreparation, preparations = 0, throwOutput = false;
    const stallStates = [];
    const derivation = derivationHost();
    const host = {
        ...derivation,
        ScheduleRoles: () => [1, 2],
        CaptureMode: () => captureMode, Bootstrap: () => new Uint8Array(16),
        AnimationPeer: () => new Uint8Array(64),
        PrepareGpu: () => { preparations++; return new Promise(resolve => { releasePreparation = resolve; }); },
        Dispatch: async () => { await new Promise(resolve => { release = resolve; }); },
        FlushObservation: () => { flushes++; if (throwOutput) throw Error('Output unavailable'); },
        SetReliableStall: value => stallStates.push(value)
    };
    const boundary = await loadWorker(host);
    const { self, messages, imports } = boundary;
    const sessionClock = clockRing.createClockRing();
    const preparing = self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode, clockRing: sessionClock,
        poseRing: new SharedArrayBuffer(3120) } });
    await until(() => messages.some(message => message.ready === true), 'the worker is ready once its clock master qualified');
    assert.equal(derivation.record.qualified, 16, 'every startup exchange qualified the derivation before ready');
    await until(() => derivation.record.checks > 0, 'the running derivation check runs between turns of the worker loop');
    // One adoption: the startup correction installed under the initial epoch; no running check needed another.
    assert.deepEqual([derivation.record.missed, derivation.record.adopted], [0, 1]);
    assert.equal(derivation.record.retried, derivation.record.checks,
        'a wide first exchange is retried within its check, and the check rests on the narrow one');
    assert.ok(clockRing.heartbeat(clockRing.attachClockRing(sessionClock)) >= derivation.record.checks,
        'every check turn also beats the Simulation heartbeat the Browser watches');
    assert.equal(preparations, 1);
    // The Simulation worker no longer answers clock probes itself (Story 9.4b): the deleted raw probe envelope is
    // now just an invalid command transport, rejected without dispatch.
    await self.onmessage({ data: { clockProbe: new Uint8Array(80) } });
    assert.equal(messages.at(-1).rejected, true, 'a raw clock probe envelope has no handler in the Simulation worker');
    assert.equal(messages.filter(item => item.clockReply).length, 0, 'the Simulation worker never posts a clock reply');
    assert.equal(messages.filter(item => item.read || item.acknowledgement).length, 0, 'preparation cannot publish a world');
    releasePreparation();
    await preparing;
    // A published read as C# hands it to the worker (Story 9A.2): its bytes, its 24-byte receipt identity (session, then
    // publication) and its composed pose slot. The worker never parses the bytes.
    function publication(identity) {
        const bytes = new Uint8Array(64); new DataView(bytes.buffer).setBigUint64(0, identity, true);
        const receipt = new Uint8Array(24); receipt.fill(7, 0, 16);
        new DataView(receipt.buffer).setBigUint64(16, identity, true);
        return { bytes, identity: receipt };
    }
    const receipt = read => read.identity.slice();
    const publish = (read, pending, retain) => imports.publish(read.bytes, read.identity, new Uint8Array(16), pending, retain);
    const posted = () => messages.filter(item => item.read).at(-1);
    const first = publication(1n), final = publication(2n);
    for (let index = 0; index < 8; index++) {
        const running = publication(BigInt(index + 10));
        publish(running, false);
        await self.onmessage({ data: { readAcknowledged: receipt(running) } });
    }
    assert.equal(flushes, 0, 'ordinary Running reads never call the observation export');
    publish(first, false);
    publish(final, true);
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
    publish(last, true);
    const failuresBefore = messages.filter(item => item.failure).length;
    await self.onmessage({ data: { readAcknowledged: receipt(last) } });
    assert.equal(flushes, 2);
    assert.equal(messages.filter(item => item.failure).length, failuresBefore, 'observation failure is contained');
    throwOutput = false;
    const reset = self.onmessage({ data: { bytes: new Uint8Array(72) } });
    await Promise.resolve();
    imports.acknowledge(new Uint8Array(64), new Uint8Array(8), true);
    assert.equal(messages.at(-1).sequence.length, 8, 'an acknowledgement carries the 8-byte sequence C# handed over');
    assert.equal(flushes, 2);
    release();
    await reset;
    assert.equal(flushes, 3, 'acknowledgement-only Reset announces sealed observation');
    await self.onmessage({ data: { reliableStalled: true } });
    await self.onmessage({ data: { reliableStalled: false } });
    assert.deepEqual(stallStates, [true, false], 'actual worker forwards current boolean state');
    const held = publication(100n), eventOne = publication(102n), eventTwo = publication(104n), latest = publication(105n);
    publish(held, false);
    publish(publication(101n), false);
    assert.equal(imports.reserveOccurrenceRead(), true);
    publish(eventOne, false, true);
    publish(publication(103n), false);
    assert.equal(imports.reserveOccurrenceRead(), true);
    publish(eventTwo, false, true);
    publish(latest, false);
    const controlsBefore = messages.filter(item => item.scheduleControl).length;
    imports.scheduleControl(new Uint8Array([7]), 1);
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore);
    for (const expected of [eventOne, eventTwo, latest]) {
        assert.equal(posted().readIdentity.length, 24, 'the worker posts the C# receipt identity with its read');
        await self.onmessage({ data: { readAcknowledged: posted().readIdentity } });
        assert.deepEqual(posted().read, expected.bytes);
        assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore,
            'installation cannot overtake a committed occurrence or final endpoint');
    }
    await self.onmessage({ data: { readAcknowledged: receipt(latest) } });
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore + 1);
    const capacityHeld = publication(200n);
    publish(capacityHeld, false);
    for (let index = 0; index < 64; index++) {
        assert.equal(imports.reserveOccurrenceRead(), true);
        publish(publication(BigInt(201 + index)), false, true);
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

// The worker owns no WebGPU device since the pure C# WASM engine (Story 9.2b), so startup has only preparation
// rejection to control; the former device-loss control was deleted with that device path (Story 9.4b).
const StartupControl = Object.freeze({ Reject: 1, DisposeThenReject: 2 });
for (const control of Object.values(StartupControl)) {
    let rejectPreparation;
    const host = {
        ...derivationHost(),
        ScheduleRoles: () => [1, 2],
        CaptureMode: () => 1, Bootstrap: () => new Uint8Array(16),
        AnimationPeer: () => new Uint8Array(64),
        PrepareGpu: () => new Promise((_, reject) => { rejectPreparation = reject; })
    };
    const boundary = await loadWorker(host);
    const pending = boundary.self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode: 1, clockRing: clockRing.createClockRing(),
        poseRing: new SharedArrayBuffer(3120) } });
    await until(() => boundary.messages.some(item => item.ready), 'ready after clock qualification');
    assert.equal(boundary.messages.filter(item => item.ready).length, 1);
    if (control === StartupControl.DisposeThenReject) boundary.imports.dispose();
    rejectPreparation(Error('fixture preparation rejection'));
    await pending;
    assert.equal(boundary.messages.filter(item => item.failure).length,
        control === StartupControl.DisposeThenReject ? 0 : 1, 'late failures cannot resurrect disposed ownership');
    assert.equal(boundary.messages.filter(item => item.read || item.acknowledgement).length, 0);
    console.log(JSON.stringify({ startupControl: control, passed: true }));
}

// Story 9A.2: the worker reads no command width or layout. Every byte buffer goes to the one managed decoder, which admits or
// refuses its exact framed shape; only a non-buffer envelope is the transport's own rejection.
{
    let dispatched = 0;
    const host = { ScheduleRoles: () => [1, 2], Dispatch: async () => { dispatched++; } };
    const { self, messages } = await loadWorker(host);
    for (const width of [0, 7, 72, 70000]) await self.onmessage({ data: { bytes: new Uint8Array(width) } });
    assert.equal(dispatched, 4);
    await self.onmessage({ data: { bytes: [1, 2, 3] } });
    assert.equal(dispatched, 4);
    assert.equal(messages.filter(message => message.rejected).length, 1);
    console.log(JSON.stringify({ commandsReachTheManagedDecoder: true, dispatched: [0, 7, 72, 70000], rejected: ['array'] }));
}

// Blind hunter 10 (Story 9.4b final round): a scheduler turn that cannot resume is reported through the failure envelope,
// never as an uncaught worker error; a disposed session resumes no queued or timed turn.
{
    const resumed = [];
    const host = {
        ScheduleRoles: () => [1, 2],
        ResumeTurn: turn => { resumed.push(turn); if (turn === 7) throw new Error('Scheduler turn 7 was never requested.'); }
    };
    const { messages, imports } = await loadWorker(host);
    imports.scheduleTurn(7, 0);
    await until(() => messages.some(message => message.failure), 'the throwing turn becomes a failure envelope');
    assert.match(messages.find(message => message.failure).detail, /Scheduler turn failed: Error: Scheduler turn 7/);
    imports.scheduleTurn(8, 0);
    await new Promise(resolve => setTimeout(resolve, 20));
    assert.deepEqual(resumed, [7], 'a failed session resumes no later turn');
    console.log(JSON.stringify({ schedulerTurnFailureEnvelope: true }));
}
{
    const resumed = [];
    const host = { ScheduleRoles: () => [1, 2], ResumeTurn: turn => resumed.push(turn) };
    // Instrumented timers and turn channel: disposal must cancel the timed turn and close the channel, not merely rely on
    // the disposed guard (reviewer mutation: delete the cleanup and only the guard remains).
    const turnTimers = [], cleared = new Set(), closed = [];
    class ClosingChannel extends HarnessMessageChannel {
        constructor() {
            super();
            const port1 = this.port1, port2 = this.port2, close1 = port1.close, close2 = port2.close.bind(port2);
            port1.close = () => { closed.push('port1'); close1(); };
            port2.close = () => { closed.push('port2'); close2(); };
        }
    }
    const { imports } = await loadWorker(host, { MessageChannel: ClosingChannel,
        setTimeout: (callback, milliseconds) => { const timer = setTimeout(callback, milliseconds); if (milliseconds === 5) turnTimers.push(timer); return timer; },
        clearTimeout: timer => { cleared.add(timer); clearTimeout(timer); } });
    imports.scheduleTurn(1, 0);
    await until(() => resumed.length === 1, 'control: an immediate turn resumes');
    imports.scheduleTurn(2, 5); imports.scheduleTurn(3, 0);
    imports.dispose();
    await new Promise(resolve => setTimeout(resolve, 30));
    assert.deepEqual(resumed, [1], 'disposal cancels the timed and the queued turn');
    assert.equal(turnTimers.length, 1);
    assert.ok(cleared.has(turnTimers[0]), 'disposal clears the timed turn instead of leaving it to fire');
    assert.deepEqual(closed.sort(), ['port1', 'port2'], 'disposal closes both ends of the turn channel');
    console.log(JSON.stringify({ disposalCancelsSchedulerTurns: true }));
}
