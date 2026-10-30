import * as poseRing from '../../CuriousContraptions.web/wwwroot/pose-ring.js';
import type { ManagedHost, ManagedImports, ClockRing } from './worker-contracts.js';
// Supplemental control of the actual shipped worker module; no gameplay outcomes are fabricated.
import assert from 'node:assert/strict';
import test from 'node:test';
import vm from 'node:vm';
import { readFile } from 'node:fs/promises';
import { MessagePort, MessageChannel } from 'node:worker_threads';
import { pathToFileURL } from 'node:url';
const clockRing = await import(pathToFileURL('CuriousContraptions.web/wwwroot/clock-ring.js').href) as typeof import('../../CuriousContraptions.web/wwwroot/clock-ring.js');
import { EventRingMemory } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';

interface Posted {
    ready?: boolean; read?: Uint8Array; readIdentity?: Uint8Array; acknowledgement?: Uint8Array; sequence?: Uint8Array;
    failure?: boolean; rejected?: boolean; detail?: string; clockReply?: Uint8Array; scheduleControl?: Uint8Array;
}

const source = await readFile('CuriousContraptions.Simulation/wwwroot/worker.js', 'utf8');
const workerUrl = pathToFileURL('CuriousContraptions.Simulation/wwwroot/worker.js').href;
const ringSource = await readFile('CuriousContraptions.web/wwwroot/clock-ring.js', 'utf8');
// Production's WorkshopClockDerivation profile: one check every 100 ms, of at most 4 exchanges that together share one 2 ms
// budget (the exchanges of a check never block the worker loop for longer than that in total).
const ExchangeTimeoutMilliseconds = 2, CheckIntervalMilliseconds = 100, CheckExchanges = 4;
// The master reading the stand-in master stamps into every derivation reply (native milliseconds, before correction).
const MasterReading = 1;
// How far an advancing stub native clock moves per read (exact in binary, so derived budgets compare exactly).
const ClockStepMilliseconds = 0.25;
// The Simulation-owned clock master context, made deterministic: no thread races the worker's blocking exchange. The worker
// reads the shipped clock-ring.js, evaluated in its own context, whose Atomics.wait first lets this master serve every
// request already posted in the ring, as a master scheduled at once would; the shipped wait then finds the reply and returns
// without blocking. A slow master (`answering` returns false) serves nothing, so the shipped wait runs its whole timeout and
// the exchange is missed. Every ring wait's timeout is recorded, so a test can derive the budget each exchange was given.
function clockMaster() {
    let ring: ClockRing | undefined;
    const master = {
        answering: () => true, waits: [] as number[],
        /** The master serves every request now posted in the ring (a slow master's late answer). */
        serve() { clockRing.serveRequests(ring!, {}, () => BigInt(MasterReading * 1_000_000)); },
        atomics: Object.create(Atomics, { wait: { value(words: Int32Array<SharedArrayBuffer>, index: number, value: number, timeout: number) {
            if (ring && words.buffer === ring.buffer) { master.waits.push(timeout); if (master.answering()) master.serve(); }
            return Atomics.wait(words, index, value, timeout);
        } } }),
        // The context the worker starts; its bootstrap handshake as clock-master.js answers it.
        Worker: class {
            onmessage?: (event: { data: unknown }) => void;
            onerror?: (event: unknown) => void;
            constructor() { queueMicrotask(() => this.onmessage?.({ data: { awaitingBootstrap: true } })); }
            postMessage(bootstrap: { ring: SharedArrayBuffer }) {
                ring = clockRing.attachClockRing(bootstrap.ring);
                queueMicrotask(() => this.onmessage?.({ data: { ready: true } }));
            }
            terminate() { ring = undefined; }
        }
    };
    return master;
}
// The worker's managed derivation check (WorkshopClockDerivation), recorded. Every check's installation is empty, so no
// correction beyond the startup one is ever adopted.
function derivationHost() {
    const record = { qualified: 0, startupMissed: 0, checks: 0, missed: 0, adopted: 0, narrowQueries: 0, retried: 0 };
    return {
        record,
        // [exchange timeout ms, check interval ms, exchanges per check]: production's profile.
        ClockDerivationProfile: () => [ExchangeTimeoutMilliseconds, CheckIntervalMilliseconds, CheckExchanges],
        ClockDerivationQualified: () => record.qualified === 16,
        QualifyClockDerivation: (exchange: Uint8Array) => {
            const view = new DataView(exchange.buffer, exchange.byteOffset, exchange.byteLength);
            const before = view.getBigInt64(0, true), master = view.getBigInt64(8, true), after = view.getBigInt64(16, true), epoch = view.getUint32(24, true);
            assert.ok(after >= before && master === BigInt(MasterReading * 1_000_000) && epoch === 0, 'a consistent startup exchange');
            record.qualified++;
        },
        ClockDerivationQualificationMissed: () => { record.startupMissed++; },
        // The derived startup offset is zero.
        ClockDerivationStartupCorrection: () => new Uint8Array(16),
        // An empty installation: the check was consistent, so nothing is written to the ring.
        CheckClockDerivation: () => { record.checks++; return new Uint8Array(); },
        AdoptClockDerivation: () => { record.adopted++; },
        ClockDerivationMissed: () => { record.missed++; },
        // The first exchange of every check reads as wide, so each check retries exactly once before its narrow one.
        ClockDerivationExchangeNarrow: () => (record.narrowQueries++ % 2) === 1,
        ClockDerivationRetried: () => { record.retried++; }
    };
}
// The worker's run-loop yield channel, unreferenced so the harness process can exit once its checks complete.
class HarnessMessageChannel {
    port1: { onmessage: ((event: { data: unknown }) => void) | null; close(): void };
    port2: MessagePort;
    constructor() {
        const channel = new MessageChannel();
        channel.port2.unref();
        this.port2 = channel.port2;
        let handler: ((event: { data: unknown }) => void) | null = null;
        channel.port1.on('message', (data: unknown) => handler?.({ data }));
        channel.port1.unref();
        this.port1 = { set onmessage(value) { handler = value; },
            get onmessage() { return handler; }, close: () => channel.port1.close() };
    }
}
// `nativeNow` is the stub native clock the worker reads (a fixed 1 ms unless a test advances it).
async function loadWorker(host: Partial<ManagedHost>, additionalGlobals: Record<string, unknown> = {}, { nativeNow = () => 1 }: { nativeNow?: () => number } = {}) {
    let imports: ManagedImports | undefined;
    const master = clockMaster();
    const messages: Posted[] = [];
    let signalReady!: () => void;
    const ready = new Promise<void>(resolve => { signalReady = resolve; });
    const intervals = new Map<number, () => void>(); let nextInterval = 0;
    const runtime = {
        setModuleImports: (_: string, methods: ManagedImports) => { imports = methods; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: host } } }),
        getConfig: () => ({ mainAssemblyName: 'BoundaryControl' })
    };
    const self: { onmessage: (event: { data: unknown }) => Promise<void>; postMessage(message: Posted, transfer?: Transferable[]): void; close(): void } = {
        onmessage: async () => { throw new Error('Worker has not installed its message handler.'); },
        postMessage: (message, transfer = []) => { messages.push(structuredClone(message, { transfer })); if (message.ready) signalReady(); },
        close: () => {}
    };
    const context = vm.createContext({ self, Uint8Array, Float64Array, ArrayBuffer, DataView,
        MessagePort, MessageChannel: HarnessMessageChannel, console, setTimeout, clearTimeout, Promise, Error, URL, SharedArrayBuffer, Atomics: master.atomics, performance,
        // The derivation check interval, unreferenced so the harness process can exit.
        setInterval: (callback: () => void) => { const id = ++nextInterval; intervals.set(id, callback); return id; }, clearInterval: (id: number) => intervals.delete(id),
        Worker: master.Worker, ...additionalGlobals });
    const dotnet = new vm.SyntheticModule(['dotnet'], function () {
        this.setExport('dotnet', { create: async () => runtime });
    }, { context });
    // The shipped clock ring, evaluated in the worker's context so its blocking waits go through the stand-in master.
    const ring = new vm.SourceTextModule(ringSource, { context });
    const clock = new vm.SyntheticModule(['admitNativeClock', 'nativeNow', 'nativeClockEvidence'], function () {
        this.setExport('admitNativeClock', async () => {});
        this.setExport('nativeNow', nativeNow);
        this.setExport('nativeClockEvidence', () => [2, 1, 1, 1, .005, 1, 1]);
    }, { context });
    const eventRing = new vm.SyntheticModule(['EventRingMemory'], function () { this.setExport('EventRingMemory', EventRingMemory); }, { context });
    const module = new vm.SourceTextModule(source, { context, initializeImportMeta: meta => { meta.url = workerUrl; } });
    await module.link(name => {
        if (name === '../pose-ring.js') return new vm.SyntheticModule(Object.keys(poseRing), function () { for (const [key, value] of Object.entries(poseRing)) this.setExport(key, value); }, { context });
        if (name === './_framework/dotnet.js') return dotnet;
        if (name === '../native-clock.js') return clock;
        if (name === '../clock-ring.js') return ring;
        if (name === '../event-ring-cursor.js') return eventRing;
        throw Error('Unexpected worker import');
    });
    await module.evaluate();
    return { self, messages, master, ready, tick: () => { for (const callback of intervals.values()) callback(); }, get imports() { assert.ok(imports); return imports; } };
}

// Story 9A.3: the worker refuses every envelope no handler owns through the managed typed rejection, which names its keys
// (WorkerEnvelopeRejectedException); these stand-ins return the same detail the managed one does.
const envelopeRejections = (record: (string[] | string)[]) => ({
    RejectUnknownEnvelope: (keys: string[]) => { record.push(Array.from(keys)); return 'Unsupported worker envelope: ' + (keys.length ? Array.from(keys).join(', ') : '(no keys)'); },
    RejectSaturatedCommand: () => { record.push('saturated'); return 'The Workshop command transport already holds its two outstanding commands.'; }
});
const receiptIdentity = (bytes: Uint8Array) => {
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    return { sessionHigh: view.getBigUint64(0, true), sessionLow: view.getBigUint64(8, true), publication: view.getBigUint64(16, true) };
};

for (const captureMode of [1, 2]) test(`capture mode ${captureMode}: preparation, receipts, queued terminal, output failure, retired envelopes and disposal`,
    async () => {
    let flushes = 0, preparations = 0, throwOutput = false;
    let release!: () => void, releasePreparation!: () => void;
    const receipts: ReturnType<typeof receiptIdentity>[] = [], rejectedEnvelopes: (string[] | string)[] = [], logged: string[] = [];
    const derivation = derivationHost();
    const host = {
        ...derivation,
        ...envelopeRejections(rejectedEnvelopes),
        ScheduleRoles: () => [1, 2],
        CaptureMode: () => captureMode, Bootstrap: () => new Uint8Array(16),
        AnimationPeer: () => new Uint8Array(64),
        PrepareGpu: () => { preparations++; return new Promise<void>(resolve => { releasePreparation = resolve; }); },
        Dispatch: async () => { await new Promise<void>(resolve => { release = resolve; }); },
        FlushObservation: () => { flushes++; if (throwOutput) throw Error('Output unavailable'); },
        ReportObservationFailure: (detail: string) => logged.push(detail),
        // The managed reliable-read ledger: every receipt reaches it, unparsed (it accepts all here; refusals are below).
        AcknowledgeRead: (receipt: Uint8Array) => { receipts.push(receiptIdentity(receipt)); }
    };
    const recordingConsole = { ...console, error: (...items: unknown[]) => { logged.push(items.map(String).join(' ')); } };
    // The stub native clock advances a quarter millisecond per read, so a check's retry is given only what is left of the
    // check's shared budget.
    let nativeReading = 0;
    const boundary = await loadWorker(host, { console: recordingConsole }, { nativeNow: () => (nativeReading += ClockStepMilliseconds) });
    const { self, messages, imports } = boundary;
    const sessionClock = clockRing.createClockRing();
    const preparing = self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode, clockRing: sessionClock,
        poseRing: new SharedArrayBuffer(3 * (16 + 16 * 64)) } });
    await boundary.ready;
    assert.equal(derivation.record.qualified, 16, 'every startup exchange qualified the derivation before ready');
    boundary.tick(); assert.ok(derivation.record.checks > 0);
    // One adoption: the startup correction installed under the initial epoch; no running check needed another.
    assert.deepEqual([derivation.record.missed, derivation.record.adopted], [0, 1]);
    assert.equal(derivation.record.retried, derivation.record.checks,
        'a wide first exchange is retried within its check, and the check rests on the narrow one');
    // The shared budget: each of the 16 answered startup exchanges waits with the whole budget; a running check's first
    // exchange too, and its retry with what is left after the three clock reads since the check began (its start, then the
    // first exchange's before and after).
    assert.deepEqual(boundary.master.waits.slice(0, 18), [...Array(17).fill(ExchangeTimeoutMilliseconds),
        ExchangeTimeoutMilliseconds - 3 * ClockStepMilliseconds]);
    assert.ok(clockRing.heartbeat(clockRing.attachClockRing(sessionClock)) >= derivation.record.checks,
        'every check turn also beats the Simulation heartbeat the Browser watches');
    assert.equal(preparations, 1);
    // The Simulation worker no longer answers clock probes itself (Story 9.4b): the deleted raw probe envelope is
    // now an unknown envelope, refused by name without dispatch.
    await self.onmessage({ data: { clockProbe: new Uint8Array(80) } });
    assert.equal(messages.at(-1)!.rejected, true, 'a raw clock probe envelope has no handler in the Simulation worker');
    assert.equal(messages.at(-1)!.detail, 'Unsupported worker envelope: clockProbe');
    assert.equal(messages.filter(item => item.clockReply).length, 0, 'the Simulation worker never posts a clock reply');
    assert.equal(messages.filter(item => item.read || item.acknowledgement).length, 0, 'preparation cannot publish a world');
    releasePreparation();
    await preparing;
    // A published read as C# hands it to the worker (Story 9A.2): its bytes, its 24-byte receipt identity (session, then
    // publication) and its composed pose slot. The worker never parses the bytes.
    function publication(identity: bigint) {
        const bytes = new Uint8Array(64); new DataView(bytes.buffer).setBigUint64(0, identity, true);
        const receipt = new Uint8Array(24); receipt.fill(7, 0, 16);
        new DataView(receipt.buffer).setBigUint64(16, identity, true);
        return { bytes, identity: receipt };
    }
    const receipt = (read: { identity: Uint8Array }) => read.identity.slice();
    const publish = (read: { bytes: Uint8Array; identity: Uint8Array }, pending: boolean) => imports.publish(read.bytes, read.identity, new Uint8Array(16), pending);
    const posted = () => messages.filter(item => item.read).at(-1)!;
    const first = publication(1n), final = publication(2n);
    for (let index = 0; index < 8; index++) {
        const running = publication(BigInt(index + 10));
        publish(running, false);
        await self.onmessage({ data: { readAcknowledged: receipt(running) } });
    }
    assert.equal(flushes, 0, 'ordinary Running reads never call the observation export');
    assert.deepEqual(receipts, Array.from({ length: 8 }, (_, index) => receiptIdentity(publication(BigInt(index + 10)).identity)),
        'every receipt reaches the managed ledger unparsed, in order');
    publish(first, false);
    publish(final, true);
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
    throwOutput = true;
    const last = publication(3n);
    publish(last, true);
    const failuresBefore = messages.filter(item => item.failure).length;
    await self.onmessage({ data: { readAcknowledged: receipt(last) } });
    assert.equal(flushes, 2);
    assert.equal(messages.filter(item => item.failure).length, failuresBefore, 'observation failure is contained');
    assert.deepEqual(logged, ['Error: Output unavailable'], 'and logged, never swallowed');
    throwOutput = false;
    const beforeReset = publication(4n), afterReset = publication(5n);
    publish(beforeReset, false);
    const reset = self.onmessage({ data: { bytes: new Uint8Array(72) } });
    await Promise.resolve();
    imports.acknowledge(new Uint8Array(64), new Uint8Array(8), true);
    assert.equal(messages.at(-1)!.sequence!.length, 8, 'an acknowledgement carries the 8-byte sequence C# handed over');
    assert.equal(flushes, 2);
    release();
    await reset;
    assert.equal(flushes, 2, 'Reset cannot flush while its earlier read remains in flight');
    publish(afterReset, true);
    assert.deepEqual(receiptIdentity(posted().readIdentity!), receiptIdentity(beforeReset.identity), 'Reset keeps the in-flight publication');
    await self.onmessage({ data: { readAcknowledged: receipt(beforeReset) } });
    assert.deepEqual(receipts.at(-1), receiptIdentity(beforeReset.identity), 'the delayed pre-Reset receipt reaches managed accounting');
    assert.deepEqual(receiptIdentity(posted().readIdentity!), receiptIdentity(afterReset.identity), 'the post-Reset publication drains afterwards');
    await self.onmessage({ data: { readAcknowledged: receipt(afterReset) } });
    assert.equal(flushes, 3, 'Reset observation flushes once both reads retire');
    const nextRun = self.onmessage({ data: { bytes: new Uint8Array(72) } });
    await Promise.resolve();
    release();
    await nextRun;
    assert.equal(messages.filter(item => item.failure).length, failuresBefore, 'the recovered transport accepts the next Run');

    // Story 9A.3 negative control: the deleted raw stall flag and raw memory channel are unknown envelopes, refused by name
    // through the typed rejection; nothing reaches the ledger, the dispatcher or a stall setter.
    const receiptsBefore = receipts.length;
    for (const [data, detail] of [[{ reliableStalled: true }, 'reliableStalled'], [{ reliableStalled: false }, 'reliableStalled'],
        [{ memoryRequest: new Uint8Array(64) }, 'memoryRequest'], [{ memoryResult: new Uint8Array(128) }, 'memoryResult']]) {
        await self.onmessage({ data });
        assert.equal(messages.at(-1)!.rejected, true);
        assert.equal(messages.at(-1)!.detail, 'Unsupported worker envelope: ' + detail);
    }
    assert.equal(receipts.length, receiptsBefore);
    assert.equal('SetReliableStall' in host, false, 'no stall setter exists to forward to');
    // Story 9A.23 D9: no read is reliable or retained (every one-shot occurrence is an event ring record), so the occurrence-read
    // reservation is gone and the reads behind the one in flight coalesce to the latest; an installation still waits behind it.
    assert.equal('reserveOccurrenceRead' in imports, false, 'the occurrence-read reservation is deleted');
    assert.equal('releaseOccurrenceRead' in imports, false, 'the occurrence-read release is deleted');
    const held = publication(100n), latest = publication(105n);
    publish(held, false);
    for (const skipped of [101n, 102n, 103n, 104n]) publish(publication(skipped), false);
    publish(latest, false);
    const controlsBefore = messages.filter(item => item.scheduleControl).length;
    imports.scheduleControl(new Uint8Array([7]), 1);
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore);
    assert.equal(posted().readIdentity!.length, 24, 'the worker posts the C# receipt identity with its read');
    await self.onmessage({ data: { readAcknowledged: posted().readIdentity } });
    assert.equal(new DataView(posted().read!.buffer).getBigUint64(0, true), 105n, 'the reads behind the one in flight coalesce to the latest');
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore,
        'installation cannot overtake the final endpoint');
    await self.onmessage({ data: { readAcknowledged: receipt(latest) } });
    assert.equal(messages.filter(item => item.scheduleControl).length, controlsBefore + 1);
    imports.dispose();
    const receiptsAtDisposal = receipts.length;
    await self.onmessage({ data: { readAcknowledged: receipt(publication(300n)) } });
    assert.equal(receipts.length, receiptsAtDisposal, 'a disposed worker forwards no receipt');
    assert.equal(rejectedEnvelopes.filter(item => item !== 'saturated').length, 5, 'one typed rejection per unknown envelope');
    console.log(JSON.stringify({ captureMode, passed: true, controls: ['preparationClockOverlap', 'noEarlyWorld', 'pending', 'receiptsToLedger',
        'queuedTerminal', 'activeCommand', 'matchingReceipt', 'outputFailureLogged', 'ordinaryReadNoExport', 'acknowledgementOnlyReset',
        'retiredEnvelopesRejected', 'dispose'] }));
});

// A slow clock master: a running check it does not answer within the shared budget is counted as missed, never a failure. The
// session keeps beating and checking, and once the master serves the stale request, the next check consumes that late answer
// unread and completes on its own exchange.
test('a slow clock master costs counted missed checks, never a failure, and the session continues', async () => {
    const derivation = derivationHost();
    const host = { ...derivation, ScheduleRoles: () => [1, 2], CaptureMode: () => 1, Bootstrap: () => new Uint8Array(16),
        AnimationPeer: () => new Uint8Array(64), PrepareGpu: () => new Promise<void>(() => {}) };
    const boundary = await loadWorker(host);
    const { self, messages, master } = boundary;
    // The master answers every startup exchange, then no running one.
    master.answering = () => derivation.record.qualified < 16;
    const sessionClock = clockRing.createClockRing(), beats = () => clockRing.heartbeat(clockRing.attachClockRing(sessionClock));
    self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode: 1, clockRing: sessionClock, poseRing: new SharedArrayBuffer(3 * (16 + 16 * 64)) } });
    await boundary.ready;
    boundary.tick(); boundary.tick(); assert.equal(derivation.record.missed, 2);
    assert.ok(derivation.record.missed > 0);
    assert.equal(derivation.record.checks, 0, 'no check completed without its master');
    assert.equal(messages.filter(message => message.failure).length, 0, 'a missed check is never a session failure');
    const beatsWhileSlow = beats();
    assert.ok(beatsWhileSlow >= derivation.record.missed, 'every missed check turn still beats the heartbeat');
    master.answering = () => true;
    master.serve();
    boundary.tick(); assert.ok(derivation.record.checks > 0);
    assert.ok(beats() > beatsWhileSlow, 'the worker loop kept running its check turns');
    assert.equal(messages.filter(message => message.failure).length, 0);
    boundary.imports.dispose();
    console.log(JSON.stringify({ slowMasterMissesAreCounted: true }));
});

// Story 9A.3: a receipt the managed ledger refuses (a mismatched, duplicate or unpublished one) is a typed rejection reported
// through the failure envelope, never a silent return; the session is retired, so no later read or receipt is handled.
test('a receipt the managed ledger refuses is a failure and retires the session', async () => {
    const forwarded: ReturnType<typeof receiptIdentity>[] = [];
    const host = {
        ScheduleRoles: () => [1, 2], ...envelopeRejections([]),
        AcknowledgeRead: (receipt: Uint8Array) => {
            forwarded.push(receiptIdentity(receipt));
            if (forwarded.length === 2) throw new Error('ReadReceiptRejectedException: Publication 1 is receipted, but publication 1 is already retired.');
        }
    };
    const { self, messages, imports } = await loadWorker(host);
    const identity = new Uint8Array(24); identity.fill(7, 0, 16); identity[16] = 1;
    const poseOwner = new Uint8Array(16); new DataView(poseOwner.buffer).setBigUint64(0, 1n, true); new DataView(poseOwner.buffer).setBigUint64(8, 1n, true);
    imports.preparePose(poseOwner, 0, 16); imports.commitPose(poseOwner);
    imports.publish(new Uint8Array(8), identity, new Uint8Array(16), false);
    assert.equal(messages.filter(item => item.read).length, 1);
    await self.onmessage({ data: { readAcknowledged: identity.slice() } });
    assert.equal(messages.filter(item => item.failure).length, 0, 'an accepted receipt is no failure');
    await self.onmessage({ data: { readAcknowledged: identity.slice() } });
    const failure = messages.find(item => item.failure);
    assert.match(failure?.detail ?? '', /ReadReceiptRejectedException: Publication 1 is receipted, but publication 1 is already retired/);
    await self.onmessage({ data: { readAcknowledged: identity.slice() } });
    assert.equal(forwarded.length, 2, 'the retired session forwards no later receipt');
    console.log(JSON.stringify({ refusedReceiptIsAFailure: true }));
});

// Story 9A.3: the memory query travels the command transport like every command (one typed buffer for the managed decoder, its
// acknowledgement keyed by its sequence); a third outstanding command is refused with its typed reason, never dispatched.
test('the memory query travels the command transport and a third outstanding command is refused', async () => {
    const dispatched: { length: number; first: number }[] = [], rejected: (string[] | string)[] = [], releases: (() => void)[] = [];
    const host = {
        ScheduleRoles: () => [1, 2], ...envelopeRejections(rejected),
        Dispatch: async (bytes: Uint8Array) => { dispatched.push({ length: bytes.length, first: bytes[0] }); await new Promise<void>(resolve => { releases.push(resolve); }); }
    };
    const { self, messages, imports } = await loadWorker(host);
    const query = new Uint8Array([0x51, 0x4d, 0x0c, 0x00]), other = new Uint8Array([1, 2, 3]);
    const first = self.onmessage({ data: { bytes: query } });
    const second = self.onmessage({ data: { bytes: other } });
    await self.onmessage({ data: { bytes: new Uint8Array([9]) } });
    assert.deepEqual(dispatched, [{ length: 4, first: 0x51 }, { length: 3, first: 1 }], 'the query buffer reaches the managed decoder unchanged');
    assert.equal(messages.at(-1)!.detail, 'The Workshop command transport already holds its two outstanding commands.');
    assert.deepEqual(rejected, ['saturated']);
    const sequence = new Uint8Array(8); sequence[0] = 1;
    imports.acknowledge(new Uint8Array(16), sequence, false);
    assert.equal(new DataView(messages.at(-1)!.sequence!.buffer).getBigUint64(0, true), 1n, 'its acknowledgement carries its sequence');
    for (const release of releases) release();
    await first; await second;
    console.log(JSON.stringify({ memoryQueryOnCommandTransport: true }));
});

// The worker owns no WebGPU device since the pure C# WASM engine (Story 9.2b), so startup has only preparation
// rejection to control; the former device-loss control was deleted with that device path (Story 9.4b).
enum StartupControl { Reject, DisposeThenReject }
for (const control of [StartupControl.Reject, StartupControl.DisposeThenReject]) test(`startup control ${control}: a rejected preparation`, async () => {
    let rejectPreparation!: (error: Error) => void;
    const host = {
        ...derivationHost(),
        ScheduleRoles: () => [1, 2],
        CaptureMode: () => 1, Bootstrap: () => new Uint8Array(16),
        AnimationPeer: () => new Uint8Array(64),
        PrepareGpu: () => new Promise<void>((_, reject) => { rejectPreparation = reject; })
    };
    const boundary = await loadWorker(host);
    const pending = boundary.self.onmessage({ data: { bootstrap: new Uint8Array(16), captureMode: 1, clockRing: clockRing.createClockRing(),
        poseRing: new SharedArrayBuffer(3 * (16 + 16 * 64)) } });
    await boundary.ready;
    assert.equal(boundary.messages.filter(item => item.ready).length, 1);
    if (control === StartupControl.DisposeThenReject) boundary.imports.dispose();
    rejectPreparation(Error('fixture preparation rejection'));
    await pending;
    assert.equal(boundary.messages.filter(item => item.failure).length,
        control === StartupControl.DisposeThenReject ? 0 : 1, 'late failures cannot resurrect disposed ownership');
    assert.equal(boundary.messages.filter(item => item.read || item.acknowledgement).length, 0);
    console.log(JSON.stringify({ startupControl: control, passed: true }));
});

// Story 9A.2: the worker reads no command width or layout. Every byte buffer goes to the one managed decoder, which admits or
// refuses its exact framed shape; only a non-buffer envelope is the transport's own rejection.
test('every byte buffer reaches the one managed decoder', async () => {
    let dispatched = 0;
    const host = { ScheduleRoles: () => [1, 2], ...envelopeRejections([]), Dispatch: async () => { dispatched++; } };
    const { self, messages } = await loadWorker(host);
    for (const width of [0, 7, 72, 70000]) await self.onmessage({ data: { bytes: new Uint8Array(width) } });
    assert.equal(dispatched, 4);
    await self.onmessage({ data: { bytes: [1, 2, 3] } });
    assert.equal(dispatched, 4);
    assert.equal(messages.filter(message => message.rejected).length, 1);
    console.log(JSON.stringify({ commandsReachTheManagedDecoder: true, dispatched: [0, 7, 72, 70000], rejected: ['array'] }));
});

// Deterministic scheduler turns for the absence controls: the worker's turn channel delivers only when the test pumps it, and its
// turn timers fire only when the test runs them, on a virtual clock. "No turn resumed" is asserted at a quiescence point, after
// every posted turn message was delivered (or dropped by a closed port, as a MessagePort drops it) and every live timer has
// fired, never after a wall-clock pause. A run that never quiesces (a delivery or timer that keeps scheduling more) fails at
// QuiescenceLimit steps instead of hanging.
const QuiescenceLimit = 1000;
function manualTurns() {
    const deliveries: number[] = [], timers = new Map<number, { callback(): void; due: number }>(), cleared = new Set<number>(), closed: string[] = [];
    let handler: ((event: { data: number }) => void) | null = null;
    let receiving = true, sending = true, nextTimer = 0, now = 0;
    class ManualTurnChannel {
        port1: { onmessage: ((event: { data: number }) => void) | null; close(): void };
        port2: { postMessage(data: number): void; close(): void };
        constructor() {
            this.port1 = { set onmessage(value) { handler = value; }, get onmessage() { return handler; },
                close: () => { receiving = false; closed.push('port1'); } };
            this.port2 = { postMessage: (data: number) => { if (sending) deliveries.push(data); }, close: () => { sending = false; closed.push('port2'); } };
        }
    }
    return {
        cleared, closed, timers,
        globals: {
            MessageChannel: ManualTurnChannel,
            setTimeout: (callback: () => void, milliseconds: number) => { const id = ++nextTimer; timers.set(id, { callback, due: now + milliseconds }); return id; },
            clearTimeout: (id: number) => { cleared.add(id); timers.delete(id); }
        },
        /** Every turn message posted so far is delivered in order, including any a delivery posts. */
        pump() {
            for (let delivered = 0; deliveries.length; delivered++) {
                assert.ok(delivered < QuiescenceLimit, 'the turn channel quiesces: deliveries keep posting turns');
                const data = deliveries.shift()!;
                if (receiving && handler) handler({ data });
            }
        },
        /** The quiescence point: every posted turn message is delivered, then every live timer fires in due-time order (the
         * virtual clock advancing to each), pumping after each. */
        quiesce() {
            this.pump();
            for (let fired = 0; timers.size; fired++) {
                assert.ok(fired < QuiescenceLimit, 'the turn timers quiesce: firing keeps arming timers');
                const [id, timer] = [...timers].sort((a, b) => a[1].due - b[1].due || a[0] - b[0])[0];
                timers.delete(id); now = timer.due; timer.callback(); this.pump();
            }
        }
    };
}

// Blind hunter 10 (Story 9.4b final round): a scheduler turn that cannot resume is reported through the failure envelope,
// never as an uncaught worker error; a disposed session resumes no queued or timed turn.
test('a scheduler turn that cannot resume is a failure envelope', async () => {
    const resumed: number[] = [];
    const host = {
        ScheduleRoles: () => [1, 2],
        ResumeTurn: (turn: number) => { resumed.push(turn); if (turn === 7) throw new Error('Scheduler turn 7 was never requested.'); }
    };
    const turns = manualTurns();
    const { messages, imports } = await loadWorker(host, turns.globals);
    imports.scheduleTurn(7, 0);
    turns.pump();
    assert.match(messages.find(message => message.failure)?.detail ?? 'no failure envelope', /Scheduler turn failed: Error: Scheduler turn 7/);
    imports.scheduleTurn(8, 0);
    turns.quiesce();
    assert.deepEqual(resumed, [7], 'a failed session resumes no later turn');
    console.log(JSON.stringify({ schedulerTurnFailureEnvelope: true }));
});
test('disposal cancels the timed and the queued scheduler turn', async () => {
    const resumed: number[] = [];
    const host = { ScheduleRoles: () => [1, 2], ResumeTurn: (turn: number) => resumed.push(turn) };
    // Manual timers and turn channel: disposal must cancel the timed turn and close the channel, not merely rely on the
    // disposed guard (reviewer mutation: delete the cleanup and only the guard remains).
    const turns = manualTurns();
    const { imports } = await loadWorker(host, turns.globals);
    imports.scheduleTurn(1, 0);
    turns.pump();
    assert.deepEqual(resumed, [1], 'control: an immediate turn resumes');
    imports.scheduleTurn(2, 5); imports.scheduleTurn(3, 0);
    const timed = [...turns.timers.keys()];
    imports.dispose();
    turns.quiesce();
    assert.deepEqual(resumed, [1], 'disposal cancels the timed and the queued turn');
    assert.equal(timed.length, 1);
    assert.ok(turns.cleared.has(timed[0]), 'disposal clears the timed turn instead of leaving it to fire');
    assert.deepEqual(turns.closed.sort(), ['port1', 'port2'], 'disposal closes both ends of the turn channel');
    console.log(JSON.stringify({ disposalCancelsSchedulerTurns: true }));
});

// A recognized transport field never makes an unknown companion field legal. Rejection is atomic.
test('a mixed envelope is rejected atomically', async () => {
    let dispatches = 0, receipts = 0, schedules = 0;
    const host = {
        ScheduleRoles: () => [1, 2], ...envelopeRejections([]),
        Dispatch: async () => { dispatches++; },
        AcknowledgeRead: () => { receipts++; },
        ScheduleResult: () => { schedules++; }
    };
    const { self, messages } = await loadWorker(host);
    for (const data of [
        { bytes: new Uint8Array(8), memoryRequest: true },
        { readAcknowledged: new Uint8Array(24), reliableStalled: true },
        { scheduleControl: new Uint8Array(8), unexpected: true }
    ]) {
        await self.onmessage({ data });
        assert.equal(messages.at(-1)!.rejected, true);
        assert.match(messages.at(-1)!.detail!, /Unsupported worker envelope: (memoryRequest|reliableStalled|unexpected)/);
    }
    assert.deepEqual([dispatches, receipts, schedules], [0, 0, 0], 'mixed envelopes mutate no managed state');
    console.log(JSON.stringify({ mixedEnvelopesRejectedAtomically: true }));
});

test('the observation facade freezes every nested result and exposes no command capability', async () => {
    const { registerObservation, discoverObservation, ObservationRefusal, DiagnosticCategory, WorkshopSimulationPhase, WorkshopCommandOutcome, WorkshopRejection, WorkshopCommandKind, WorkshopRuntimeRole, ClockProbeOutcome } =
        await import('../../CuriousContraptions.web/wwwroot/workshop-observation.js');
    let tick = '4';
    registerObservation('101:206',
        () => ({ refusal: ObservationRefusal.None, value: {
            epoch: '2', tick, revision: '3', phase: WorkshopSimulationPhase.Running, cadence: '1', projection: '2', captures: [], electrical: [], sequence: '1', publication: '2', masterGeneration: '1', sourcePulse: '4',
            outcome: WorkshopCommandOutcome.Applied, reason: WorkshopRejection.None, activations: [], timers: [], contactWorks: [], memory: [], actuators: [], loads: [], logic: [],
            bodies: [{ id: '7', cell: { x: 0, y: 0, z: 0 }, local: { x: 1, y: 2, z: 3 },
                velocity: { x: 0, y: 0, z: 0 }, angularVelocity: { x: 0, y: 0, z: 0 }, localCentreOfMass: { x: 0, y: 0, z: 0 }, rotation: { x: 0, y: 0, z: 0, w: 1 } }] } }),
        () => ({ refusal: ObservationRefusal.None, value: [{ ordinal: '1', sequence: '3', kind: WorkshopCommandKind.Run, outcome: WorkshopCommandOutcome.Applied, reason: WorkshopRejection.None, appliedToPresentation: true }] }),
        () => ({ refusal: ObservationRefusal.None, value: [{ ordinal: '9007199254740993',
            record: { category: DiagnosticCategory.ClockSample, probe: { sessionLow: '101', sessionHigh: '206',
                generation: '1', requesterGeneration: '1', role: WorkshopRuntimeRole.Browser, sequence: '9007199254740995', sent: '9007199254740997' },
                outcome: ClockProbeOutcome.Accepted, offset: { lower: '-18446744073709551617', upper: '18446744073709551617' }, received: '9007199254740999' } }] }),
        () => ({ refusal: ObservationRefusal.None, value: '1' }),
        () => ({ refusal: ObservationRefusal.None, value: { epoch: '2', skipped: '0', records: [] } }),
        () => ({ refusal: ObservationRefusal.None }),
        () => ({ refusal: ObservationRefusal.None }));
    const port = discoverObservation()!;
    assert.equal(Object.isFrozen(port), true);
    assert.equal('execute' in port, false);
    const before = port.query();
    const value = before.value!;
    assert.throws(() => { Object.defineProperty(value.bodies[0].local, 'x', { value: 99 }); }, TypeError);
    assert.throws(() => { Object.defineProperty(value.bodies, '0', { value: {} }); }, TypeError);
    tick = '5';
    assert.equal(value.tick, '4');
    assert.equal(port.query().value!.tick, '5');
    assert.equal(port.query().value!.bodies[0].local.x, 1);
    assert.equal(Object.isFrozen(port.receipts().value), true);
    const diagnostic = port.diagnostics().value![0];
    assert.equal(diagnostic.ordinal, '9007199254740993');
    assert.equal(diagnostic.record.category, DiagnosticCategory.ClockSample);
    if (diagnostic.record.category !== DiagnosticCategory.ClockSample) throw new Error('Expected a typed clock sample.');
    assert.equal(diagnostic.record.probe.sequence, '9007199254740995');
    assert.equal(diagnostic.record.offset.lower, '-18446744073709551617');
    const offset = diagnostic.record.offset;
    assert.throws(() => { Object.defineProperty(offset, 'lower', { value: '0' }); }, TypeError);
});

test('client observation event exports keep two observers independent from UI through growth and Reset', async () => {
    const { poseClient } = await import('./pose-client-harness.js');
    const { eventWorker } = await import('./event-worker-harness.js');
    const { publication, publish } = await import('./event-ring-fixture.js');
    const client = await poseClient(), worker = await eventWorker();
    const delivery = (bytes: Uint8Array) => {
        const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
        return { epoch: view.getBigUint64(0, true), skipped: view.getBigUint64(8, true),
            sequences: Array.from({ length: (bytes.length - 16) / 64 }, (_, i) => view.getBigUint64(16 + i * 64, true)) };
    };
    try {
        const fast = client.api.openObservationEvents(client.id), slow = client.api.openObservationEvents(client.id);
        publish(worker.imports, publication(5n, 2, 0n, 2n)); client.deliverEvents(worker.posted.at(-1)!);
        assert.deepEqual(delivery(client.api.readObservationEvents(client.id, fast, '5')).sequences, [1n, 2n]);
        publish(worker.imports, publication(5n, 4, 2n, 4n)); client.deliverEvents(worker.posted.at(-1)!);
        assert.deepEqual(delivery(client.api.readObservationEvents(client.id, fast, '5')).sequences, [3n, 4n]);
        assert.deepEqual(delivery(client.api.readEvents(client.id, '5')).sequences, [1n, 2n, 3n, 4n]);
        assert.deepEqual(delivery(client.api.readObservationEvents(client.id, slow, '5')).sequences, [1n, 2n, 3n, 4n]);
        publish(worker.imports, publication(6n, 2, 0n, 2n)); client.deliverEvents(worker.posted.at(-1)!);
        assert.deepEqual(delivery(client.api.readObservationEvents(client.id, fast, '6')).sequences, [1n, 2n]);
        assert.deepEqual(delivery(client.api.readEvents(client.id, '6')).sequences, [1n, 2n]);
        assert.deepEqual(delivery(client.api.readObservationEvents(client.id, slow, '6')).sequences, [1n, 2n]);
        client.api.closeObservationEvents(client.id, fast);
        assert.throws(() => client.api.readObservationEvents(client.id, fast, '6'), /closed/);
        client.dispose();
        assert.throws(() => client.api.readObservationEvents(client.id, slow, '6'), /retired|client/i);
    } finally { client.dispose(); worker.imports.dispose(); }
});

test('discovery retains the same startup port and exposes its terminal failure before a replacement session', async () => {
    const { registerObservation, discoverObservation, ObservationRefusal } = await import('../../CuriousContraptions.web/wwwroot/workshop-observation.js');
    let detail: string | undefined;
    registerObservation('startup', () => ({ refusal: detail ? ObservationRefusal.Retired : ObservationRefusal.NotCommitted }),
        () => ({ refusal: ObservationRefusal.None, value: [] }), () => ({ refusal: ObservationRefusal.None, value: [] }),
        () => ({ refusal: ObservationRefusal.NotCommitted }), () => ({ refusal: ObservationRefusal.NotCommitted }),
        () => ({ refusal: ObservationRefusal.ClosedCursor }), () => ({ refusal: ObservationRefusal.None, value: detail }));
    const before = discoverObservation()!;
    assert.equal(before.query().refusal, ObservationRefusal.NotCommitted);
    detail = 'bundle resource unavailable';
    assert.equal(discoverObservation(), before);
    assert.equal(before.query().refusal, ObservationRefusal.Retired);
    assert.equal(before.terminal().value, detail);
});

test('observation enums preserve canonical external command, refusal, state and diagnostic values', async () => {
    const value = await import('../../CuriousContraptions.web/wwwroot/workshop-observation.js');
    assert.equal(value.WorkshopCommandKind.Initialize, 0);
    assert.equal(value.WorkshopCommandKind.Run, 2);
    assert.equal(value.WorkshopCommandKind.ObserveMemory, 12);
    assert.equal(value.WorkshopCommandKind.CutRope, 13);
    assert.equal(value.WorkshopRejection.UnknownRopeSpan, 15);
    assert.equal(value.WorkshopRejection.RopeAlreadyCut, 16);
    assert.equal(value.WorkshopConnectionDomain.Rope, 3);
    assert.equal(value.WorkshopSocket.Tie, 9);
    assert.equal(value.WorkshopRejection.InvalidRead, 7);
    assert.equal(value.WorkshopRejection[6], undefined);
    assert.equal(value.WorkshopSimulationPhase.Running, 5);
    assert.equal(value.WorkshopConnectionDomain.Activation, 1);
    assert.equal(value.WorkshopConnectionDomain.Electrical, 2);
    assert.equal(value.TraceEnd.Fault, 4);
    assert.equal(value.TraceEnd[3], undefined);
    assert.equal(value.WorkshopRuntimeRole.Animation, 3);
});
