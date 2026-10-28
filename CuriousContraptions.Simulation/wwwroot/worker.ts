import { PosePublicationPreparation, EventPublicationPreparation, type ManagedHost } from '../../tools/runtime/worker-contracts.js';
declare const self: { postMessage(message: unknown, transfer?: Transferable[]): void; close(): void; onmessage: ((event: MessageEvent<unknown>) => void) | null; };
import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';
import { attachClockRing, deriveExchange, qualifyDerivation, writeDerivation, beat } from '../clock-ring.js';
import { EventRingMemory } from '../event-ring-cursor.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host: ManagedHost;
let activeRequests = 0, observationPending = false;
let animationPort: MessagePort | undefined, clockMaster: Worker | undefined;
let derivationCheck: ReturnType<typeof setInterval> | undefined;
let roles: number[], lifetime = 0, disposed = false;
// Scheduler turns (Program.NextTurn): declared before anything that can dispose the session.
const turnChannel = new MessageChannel(), queuedTurns: number[] = [], turnTimers = new Set<ReturnType<typeof setTimeout>>();
interface PoseStorage {
    buffer: SharedArrayBuffer;
    sequences: BigInt64Array<SharedArrayBuffer>;
    slots: Uint8Array<SharedArrayBuffer>[];
    capacity: number;
    message: { poseRing: SharedArrayBuffer };
}
interface PreparedPose { owner: bigint; epoch: bigint; bodies: number; storage: PoseStorage; }
let poseStorage: PoseStorage | undefined, candidatePose: PreparedPose | undefined;
let poseBodies = 0;
let poseEpoch = 0n, poseSlotIndex = 0, posePublicationSequence = 0n;
// One read in flight and the latest behind it: reads coalesce, since every one-shot occurrence is an event ring record (Story 9A.23).
let latestRead: { bytes: Uint8Array<ArrayBuffer>; identity: Uint8Array<ArrayBuffer> } | undefined;
let readInFlight = false; // a read is { bytes, identity }
const pendingScheduleControls: { bytes: Uint8Array<ArrayBuffer>; recipient: number }[] = [];

// The pose ring layout owned by WorkshopPoseRing; WorkerAbiTests pins every value against its C# owner. Story 9A.2: this worker
// reads no read layout: C# composes each committed read's pose slot (WorkshopPoseRing.ComposeSlot) and the worker copies it.
const POSE_DEFAULT_BODY_CAPACITY = 16; // WorkshopPoseRing.DefaultBodyCapacity
const POSE_SLOT_COUNT = 3;             // WorkshopPoseRing.SlotCount
const POSE_HEADER_BYTES = 16;          // WorkshopPoseRing.HeaderBytes
const POSE_BODY_BYTES = 64;            // WorkshopPoseRing.BodyBytes

// Story 9R.5b: a ring's body capacity is its own length (WorkshopPoseRing.BodyCapacity); there is no fixed capacity.
function poseRingCapacity(buffer: SharedArrayBuffer) {
    const slot = buffer.byteLength / POSE_SLOT_COUNT, capacity = (slot - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (!Number.isInteger(slot) || !Number.isInteger(capacity) || capacity < 0) throw new Error('The pose ring is not a whole number of slots.');
    return capacity;
}
function poseViews(buffer: SharedArrayBuffer): PoseStorage {
    const capacity = poseRingCapacity(buffer), width = buffer.byteLength / POSE_SLOT_COUNT;
    return { buffer, capacity, sequences: new BigInt64Array(buffer), message: { poseRing: buffer },
        slots: Array.from({ length: POSE_SLOT_COUNT }, (_, index) => new Uint8Array(buffer, index * width, width)) };
}
function attachPoseRing(buffer: SharedArrayBuffer) {
    if (poseStorage || poseRingCapacity(buffer) !== POSE_DEFAULT_BODY_CAPACITY) throw new Error('Pose bootstrap must have its common reserve and be unique.');
    poseStorage = poseViews(buffer);
}
function poseIdentity(identity: Uint8Array) {
    if (identity.length !== 16) throw new Error('Pose ownership requires command and epoch identities.');
    const view = new DataView(identity.buffer, identity.byteOffset, identity.byteLength);
    const owner = view.getBigUint64(0, true), epoch = view.getBigUint64(8, true);
    if (owner === 0n || epoch === 0n) throw new Error('Pose ownership identities must be nonzero.');
    return { owner, epoch };
}
function preparePose(identity: Uint8Array, bodies: number, capacity: number): PosePublicationPreparation {
    const { owner, epoch } = poseIdentity(identity);
    if (disposed || candidatePose || !Number.isSafeInteger(bodies) || bodies < 0 || !Number.isSafeInteger(capacity) || capacity < bodies || capacity < POSE_DEFAULT_BODY_CAPACITY || epoch < poseEpoch)
        throw new Error('Invalid pose preparation ownership or population.');
    const bytes = POSE_SLOT_COUNT * (POSE_HEADER_BYTES + capacity * POSE_BODY_BYTES);
    if (!Number.isSafeInteger(bytes)) return PosePublicationPreparation.StorageUnavailable;
    if (epoch === poseEpoch && (poseBodies !== bodies || poseStorage?.capacity !== capacity))
        throw new Error('An admitted epoch cannot change its body population.');
    let storage: PoseStorage;
    try {
        storage = epoch === poseEpoch ? poseStorage! : poseViews(new SharedArrayBuffer(bytes));
    } catch (error) {
        if (error instanceof RangeError) return PosePublicationPreparation.StorageUnavailable;
        throw error;
    }
    candidatePose = { owner, epoch, bodies, storage };
    return PosePublicationPreparation.Ready;
}
function commitPose(identity: Uint8Array) {
    const { owner, epoch } = poseIdentity(identity), candidate = candidatePose;
    if (disposed || !candidate || candidate.owner !== owner || candidate.epoch !== epoch)
        throw new Error('Pose commit does not own the prepared storage.');
    const replacement = poseStorage !== candidate.storage;
    poseStorage = candidate.storage; poseBodies = candidate.bodies; poseEpoch = epoch; candidatePose = undefined;
    if (replacement) { poseSlotIndex = 0; self.postMessage(poseStorage.message); }
}
function discardPose(identity: Uint8Array) {
    const { owner, epoch } = poseIdentity(identity);
    if (candidatePose?.owner === owner && candidatePose.epoch === epoch) candidatePose = undefined;
}

// Copies one C#-composed slot (its 16-byte header, then one record per committed body) into the next ring slot under the
// sequence lock: odd while writing, even when complete. The records past the read are cleared, so no stale body survives.
function writePoseRing(slot: Uint8Array) {
    if (!poseStorage) throw new Error('Missing admitted pose storage.');
    const bodyCount = (slot.length - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (bodyCount !== poseBodies) throw new Error('A pose slot must match its admitted population.');
    poseSlotIndex = (poseSlotIndex + 1) % POSE_SLOT_COUNT;
    const target = poseStorage.slots[poseSlotIndex], seqIndex = target.byteOffset / 8;
    const nextSeq = posePublicationSequence + 2n;
    posePublicationSequence = nextSeq;
    Atomics.store(poseStorage.sequences, seqIndex, nextSeq - 1n);
    target.set(slot.subarray(8), 8);
    target.fill(0, slot.length);
    Atomics.store(poseStorage.sequences, seqIndex, nextSeq);
}

// Prepare owns private storage until the managed candidate commits. Publication
// changes advertised memory only after Commit, through atomic whole-word accessors.
const EVENT_HEADER_BYTES = 64;
const EVENT_SLOT_BYTES = 64;
const EVENT_CAPACITY_OFFSET = 0;
const EVENT_EPOCH_OFFSET = 8;
const EVENT_WRITE_SEQUENCE_OFFSET = 16;
interface PreparedEvents {
    ring: EventRingMemory;
    words: BigUint64Array<ArrayBuffer>;
    sequence: bigint;
    replaces: boolean;
}
let eventRing: EventRingMemory | undefined;
let candidateEvents: PreparedEvents | undefined, committedEvents: PreparedEvents | undefined;

function prepareEvents(batch: Uint8Array): EventPublicationPreparation {
    if (disposed || candidateEvents) throw new Error('No available event candidate owner.');
    if (batch.length < EVENT_HEADER_BYTES || (batch.length - EVENT_HEADER_BYTES) % EVENT_SLOT_BYTES !== 0)
        throw new Error('An event publication requires a header and whole records.');
    const view = new DataView(batch.buffer, batch.byteOffset, batch.byteLength);
    const capacity = view.getUint32(EVENT_CAPACITY_OFFSET, true), epoch = view.getBigUint64(EVENT_EPOCH_OFFSET, true);
    const sequence = view.getBigUint64(EVENT_WRITE_SEQUENCE_OFFSET, true);
    const count = (batch.length - EVENT_HEADER_BYTES) / EVENT_SLOT_BYTES;
    if (capacity === 0 || view.getUint32(4, true) !== EVENT_SLOT_BYTES || epoch === 0n || count > capacity)
        throw new Error('Unsupported event publication layout.');
    for (let word = 3; word < EVENT_HEADER_BYTES / 8; word++)
        if (view.getBigUint64(word * 8, true) !== 0n) throw new Error('Event header reserved words must be zero.');
    const sameEpoch = eventRing !== undefined && eventRing.epoch === epoch;
    const prior = sameEpoch ? eventRing!.loadWriteSequence() : 0n;
    if (sameEpoch && capacity < eventRing!.capacity || sequence < prior || sequence - prior !== BigInt(count))
        throw new Error('An event publication cannot shrink, rewind or omit records.');
    for (let index = 0; index < count; index++)
        if (view.getBigUint64(EVENT_HEADER_BYTES + index * EVENT_SLOT_BYTES, true) !== prior + BigInt(index) + 1n)
            throw new Error('Event sequences must be contiguous.');
    const replaces = !sameEpoch || eventRing!.capacity !== capacity;
    let ring: EventRingMemory, words: BigUint64Array<ArrayBuffer>;
    // Packet validation is complete. Only allocation failures map to storage refusal.
    try {
        const copy = new Uint8Array(batch);
        words = new BigUint64Array(copy.buffer);
        ring = replaces ? EventRingMemory.allocate(capacity, epoch) : eventRing!;
    } catch (error) {
        if (error instanceof RangeError) return EventPublicationPreparation.StorageUnavailable;
        throw error;
    }
    if (sameEpoch && replaces) {
        const old = eventRing!;
        const first = prior > BigInt(old.capacity) ? prior - BigInt(old.capacity) + 1n : 1n;
        for (let record = first; record <= prior; record++) {
            if (old.loadSequence(record) !== record) throw new Error('Retained event history is incomplete.');
            for (let word = 1; word < EVENT_SLOT_BYTES / 8; word++)
                ring.storePayload(record, word, old.loadPayload(record, word));
            if (old.loadSequence(record) !== record) throw new Error('Event history changed during preparation.');
            ring.publishSequence(record, record);
        }
        ring.storeWriteSequence(prior);
    }
    candidateEvents = { ring, words, sequence, replaces };
    return EventPublicationPreparation.Ready;
}
function commitEvents() {
    if (!candidateEvents || disposed) throw new Error('No prepared event candidate.');
    committedEvents = candidateEvents; candidateEvents = undefined;
}
function discardEvents() { candidateEvents = undefined; }
function disposeEvents() { candidateEvents = committedEvents = undefined; eventRing = undefined; }
function publishEvents() {
    const ready = committedEvents;
    if (!ready || disposed) return;
    for (let offset = EVENT_HEADER_BYTES / 8; offset < ready.words.length; offset += EVENT_SLOT_BYTES / 8) {
        const sequence = ready.words[offset];
        ready.ring.publishSequence(sequence, 0n);
        for (let word = 1; word < EVENT_SLOT_BYTES / 8; word++)
            ready.ring.storePayload(sequence, word, ready.words[offset + word]);
        ready.ring.publishSequence(sequence, sequence);
    }
    ready.ring.storeWriteSequence(ready.sequence);
    eventRing = ready.ring; committedEvents = undefined;
    if (ready.replaces) {
        eventRing.postTo(self);
        if (animationPort) eventRing.postTo(animationPort);
    }
}

function clockNow() {
    try { return nativeNow(); }
    catch (e) { disposeOwned(); self.postMessage({ failure: true, detail: String(e) }); self.close(); throw e; }
}

function disposeOwned() {
    disposed = true; lifetime++;
    if (animationPort) { animationPort.onmessage = null; animationPort.close(); animationPort = undefined; }
    if (clockMaster) { clockMaster.onmessage = clockMaster.onerror = null; clockMaster.terminate(); clockMaster = undefined; }
    if (derivationCheck !== undefined) { clearInterval(derivationCheck); derivationCheck = undefined; }
    // Scheduler turns end with the session: no queued or timed turn resumes a disposed run loop.
    turnChannel.port1.onmessage = null; turnChannel.port1.close(); turnChannel.port2.close();
    for (const timer of turnTimers) clearTimeout(timer);
    turnTimers.clear(); queuedTurns.length = 0;
    poseStorage = undefined; candidatePose = undefined; poseEpoch = 0n;
    disposeEvents();
    pendingScheduleControls.length = 0;
    latestRead = undefined; readInFlight = false;
}

// The clock master context answers every requester's probe through the clock ring; this worker never does (Story 9.4b).
// Its stamps derive the Simulation's native time from the shared isolated time origin plus the derivation correction,
// so this worker runs bounded synchronous exchanges with it (WorkshopClockDerivation): all startup exchanges must hold
// before the session is admitted, then one exchange every check interval, always between tick turns of this event
// loop. A small measured drift is slewed under the same epoch; a large one installs a new correction under a new epoch
// (each requester mapping recalibrates as counted telemetry); an unanswered or inconclusive check is counted. Each
// check turn also beats the ring heartbeat the Browser's liveness check reads. Nothing falls back.
async function startClockMaster(ringBuffer: SharedArrayBuffer, browserPeer: Uint8Array<ArrayBuffer>, animationPeer: Uint8Array<ArrayBuffer>) {
    const clock = attachClockRing(ringBuffer);
    const [exchangeTimeout, checkInterval, checkExchanges] = Array.from(host.ClockDerivationProfile());
    const context = new Worker(new URL('./clock-master.js', import.meta.url), { type: 'module' });
    clockMaster = context;
    await new Promise<void>((resolve, reject) => {
        context.onerror = event => reject(new Error(event.message || 'The clock master context failed to load.'));
        context.onmessage = (message: MessageEvent<Record<string, unknown>>) => {
            if (message.data?.awaitingBootstrap === true)
                context.postMessage({ ring: ringBuffer, simulationOrigin: performance.timeOrigin, browserPeer, animationPeer });
            else if (message.data?.ready === true) resolve();
            else reject(new Error(String(message.data?.detail ?? 'The clock master context rejected its bootstrap.')));
        };
    });
    context.onmessage = (message: MessageEvent<Record<string, unknown>>) => {
        disposeOwned();
        self.postMessage({ failure: true, detail: 'Clock master failed: ' + (message.data?.detail ?? 'unknown envelope') });
    };
    context.onerror = event => {
        disposeOwned();
        self.postMessage({ failure: true, detail: 'Clock master failed: ' + (event.message || 'context error') });
    };
    // Qualification retries wide or unanswered exchanges within the managed attempt budget, which refuses the session
    // once it is exhausted; each attempt blocks this loop for at most one exchange timeout.
    qualifyDerivation(clock, clockNow, exchangeTimeout, {
        qualified: () => host.ClockDerivationQualified(),
        qualify: exchange => host.QualifyClockDerivation(exchange.before, exchange.master, exchange.after, exchange.epoch),
        missed: () => host.ClockDerivationQualificationMissed(),
    });
    // The derived offset between the contexts' time origins is installed under the initial epoch before any requester
    // qualifies its mapping (this worker reports ready only after this returns).
    host.AdoptClockDerivation(writeDerivation(clock, host.ClockDerivationStartupCorrection(), false));
    derivationCheck = setInterval(() => {
        if (disposed) return;
        try {
            // The heartbeat proves this event loop still runs between tick turns (the Browser faults the session when
            // it stops); the derivation check rides on the same turn.
            beat(clock);
            // The first exchange wakes a master context asleep since the last check and is often wide; a wide one is
            // retried back to back, so the check rests on a narrow exchange when the attempts allow. All exchanges of
            // one check share a single exchange timeout, so a check blocks this loop for at most that long; the check
            // uses the narrow exchange, or else the last one answered.
            const started = clockNow();
            let exchange = deriveExchange(clock, clockNow, exchangeTimeout);
            for (let attempt = 1; exchange && attempt < checkExchanges &&
                !host.ClockDerivationExchangeNarrow(exchange.before, exchange.master, exchange.after, exchange.epoch); attempt++) {
                const remaining = exchangeTimeout - (clockNow() - started);
                if (remaining <= 0) break;
                host.ClockDerivationRetried();
                const next = deriveExchange(clock, clockNow, remaining);
                if (!next) break;
                exchange = next;
            }
            if (!exchange) { host.ClockDerivationMissed(); return; }
            const installation = Array.from(host.CheckClockDerivation(exchange.before, exchange.master, exchange.after, exchange.epoch));
            if (installation.length === 2)
                host.AdoptClockDerivation(writeDerivation(clock, installation[0], installation[1] === 1));
        } catch (error) {
            disposeOwned();
            self.postMessage({ failure: true, detail: 'Clock derivation check failed: ' + String(error) });
        }
    }, checkInterval);
}

function forwardScheduleControl(bytes: Uint8Array<ArrayBuffer>, recipient: number) {
    if (recipient === roles[0]) self.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else if (recipient === roles[1] && animationPort) animationPort.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else throw new Error('Unknown or retired schedule recipient.');
}

function flushScheduleControls() {
    if (readInFlight || latestRead) return;
    while (pendingScheduleControls.length) {
        const item = pendingScheduleControls.shift()!;
        forwardScheduleControl(item.bytes, item.recipient);
    }
}

// One read in flight, posted in publication order: its receipt identity (session and publication) comes from C# with the read,
// and the browser echoes it unread.
function flushRead() {
    if (readInFlight || !latestRead) return;
    const read = latestRead;
    latestRead = undefined;
    readInFlight = true;
    self.postMessage({ read: read.bytes, readIdentity: read.identity, dispatchReadbackMs: 0 }, [read.bytes.buffer, read.identity.buffer]);
}

// The trace flush is diagnostic output: its failure never stops the session, but it is always logged, never swallowed.
function flushObservation() {
    if (!observationPending || activeRequests !== 0 || readInFlight || latestRead) return;
    observationPending = false;
    try { host.FlushObservation(); }
    catch (error) { console.error('CCGPU_TRACE_FLUSH_EXCEPTION', String(error)); }
}

// Story 9A.3: C# matches every receipt in its own reliable-read accounting (WorkshopReliableReadLedger), which is how the worker
// detects a reliable-read stall. A receipt it refuses is a typed rejection reported as a transport failure, never ignored.
function acknowledgeRead(receipt: Uint8Array) {
    if (disposed) return;
    try { host.AcknowledgeRead(receipt); }
    catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); return; }
    readInFlight = false;
    flushRead();
    flushScheduleControls();
    flushObservation();
}

// Scheduler turns of the run loop (Program.NextTurn). Delay 0 is an immediate macrotask through a MessageChannel, so
// queued commands, receipts and schedule controls run between tick batches without the 4 ms clamp a chained timer would
// add to catch-up; a positive delay is the ordinary wake-up timer. Either task calls ResumeTurn itself, so the loop
// continues inside it: one macrotask per turn, never a second runtime hop behind other queued work.
// A turn that fails to resume is a worker fault reported through the failure envelope, never an uncaught error.
function resumeTurn(turn: number) {
    if (disposed) return;
    try { host.ResumeTurn(turn); }
    catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: 'Scheduler turn failed: ' + String(error) }); }
}
turnChannel.port1.onmessage = () => { const turn = queuedTurns.shift(); if (turn !== undefined) resumeTurn(turn); };

runtime.setModuleImports('workshopGpu', {
    now: clockNow,
    scheduleTurn(turn, delayMilliseconds) {
        if (disposed) return;
        if (delayMilliseconds === 0) { queuedTurns.push(turn); turnChannel.port2.postMessage(0); }
        else { const timer = setTimeout(() => { turnTimers.delete(timer); resumeTurn(turn); }, delayMilliseconds); turnTimers.add(timer); }
    },
    scheduleControl(bytes, recipient) {
        const val = new Uint8Array(bytes);
        if (readInFlight || latestRead) {
            if (pendingScheduleControls.length >= 4) throw new Error('Schedule drain capacity exceeded.');
            pendingScheduleControls.push({ bytes: val, recipient });
        } else forwardScheduleControl(val, recipient);
    },
    acknowledge(bytes, sequenceBytes, pending) {
        observationPending = pending;
        const res = new Uint8Array(bytes), sequence = new Uint8Array(sequenceBytes);
        self.postMessage({ acknowledgement: res, sequence, dispatchReadbackMs: 0 }, [res.buffer, sequence.buffer]);
    },
    retire(detail) {
        try { self.postMessage({ failure: true, detail }); }
        finally { self.close(); }
    },
    preparePose, commitPose, discardPose,
    prepareEvents, commitEvents, discardEvents, publishEvents, disposeEvents,
    publish(bytes, identity, poseSlot, pending) {
        observationPending = pending;
        const res = { bytes: new Uint8Array(bytes), identity: new Uint8Array(identity) };
        if (res.identity.length !== 24) throw new Error('A physical read requires its 24-byte receipt identity.');
        writePoseRing(new Uint8Array(poseSlot));
        if (animationPort) {
            const committed = res.bytes.slice();
            animationPort.postMessage({ committedRead: committed }, [committed.buffer]);
        }
        latestRead = res;
        flushRead();
    },
    dispose: disposeOwned
});

const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.CuriousContraptions.Simulation.Program;
roles = Array.from(host.ScheduleRoles());

self.onmessage = async event => {
    try { clockNow(); } catch { return; } // a failed native clock has already retired this session and reported it
    const data: Record<string, unknown> = event.data !== null && typeof event.data === 'object' ? event.data as Record<string, unknown> : {};
    const keys = data !== null && typeof data === 'object' ? Object.keys(data) : [];
    const allowed = data?.bootstrap instanceof Uint8Array ? ['bootstrap', 'captureMode', 'clockRing', 'poseRing']
        : data?.animationPort instanceof MessagePort ? ['animationPort']
        : data?.scheduleControl instanceof Uint8Array ? ['scheduleControl']
        : data?.readAcknowledged instanceof Uint8Array ? ['readAcknowledged']
        : data?.bytes instanceof Uint8Array ? ['bytes'] : [];
    const unsupported = keys.filter(key => !allowed.includes(key));
    if (unsupported.length) {
        self.postMessage({ rejected: true, detail: host.RejectUnknownEnvelope(unsupported) });
        return;
    }
    const bootstrap = data.bootstrap;
    if (bootstrap instanceof Uint8Array) {
        try {
            const captureMode = data.captureMode;
            if (!(data.poseRing instanceof SharedArrayBuffer)) throw new Error('Bootstrap requires the session pose ring.');
            attachPoseRing(data.poseRing);
            poseSlotIndex = 0;
            if ((captureMode !== 1 && captureMode !== 2) || captureMode !== host.CaptureMode())
                throw new Error('Browser and simulation capture modes differ.');
            if (!(data.clockRing instanceof SharedArrayBuffer)) throw new Error('Bootstrap requires the session clock ring.');
            const masterPeer = new Uint8Array(host.Bootstrap(bootstrap));
            await startClockMaster(data.clockRing, masterPeer.slice(), new Uint8Array(host.AnimationPeer()));
            const nativeClock = new Float64Array(nativeClockEvidence());
            self.postMessage({ ready: true, nativeClock, captureMode, masterPeer }, [nativeClock.buffer, masterPeer.buffer]);
            await host.PrepareGpu();
        } catch (error) {
            if (disposed) return;
            disposeOwned();
            self.postMessage({ failure: true, detail: String(error) });
        }
        return;
    }
    const transferredPort = data.animationPort;
    if (transferredPort instanceof MessagePort) {
        if (animationPort || disposed) { transferredPort.close(); self.postMessage({ failure: true, detail: 'Duplicate or retired Animation port.' }); return; }
        animationPort = transferredPort;
        try {
            const animationPeer = new Uint8Array(host.AnimationPeer());
            animationPort.onmessage = (incoming: MessageEvent<Record<string, unknown>>) => {
                try {
                    if (incoming.data?.scheduleControl instanceof Uint8Array) {
                        host.ScheduleResult(incoming.data.scheduleControl, roles[1]);
                    } else throw new Error('Unsupported Animation master-port message.');
                } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
            };
            animationPort.start();
            // A ring allocated before the port arrived reaches the Animation worker's reader too.
            if (eventRing) eventRing.postTo(animationPort);
            self.postMessage({ animationPeer }, [animationPeer.buffer]);
        } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    if (data.scheduleControl instanceof Uint8Array) {
        try { host.ScheduleResult(data.scheduleControl, roles[0]); }
        catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const receipt = data.readAcknowledged;
    if (receipt instanceof Uint8Array) {
        acknowledgeRead(receipt);
        return;
    }
    // Every command and query (memory observation among them) is one typed buffer for the managed decoder. Any other envelope,
    // a retired channel name included, is refused by name with its typed reason and recorded in this worker's fault log.
    const bytes = data.bytes;
    if (!(bytes instanceof Uint8Array)) {
        const keys = event.data !== null && typeof event.data === 'object' ? Object.keys(event.data) : [];
        self.postMessage({ rejected: true, detail: host.RejectUnknownEnvelope(keys) });
        return;
    }
    if (activeRequests >= 2) {
        self.postMessage({ rejected: true, detail: host.RejectSaturatedCommand() });
        return;
    }
    activeRequests++;
    try {
        await host.Dispatch(bytes);
    } catch (error) {
        self.postMessage({ failure: true, detail: String(error) });
    } finally { activeRequests--; flushObservation(); }
};
self.postMessage({ awaitingBootstrap: true });
