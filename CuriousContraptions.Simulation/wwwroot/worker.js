import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';
import { attachClockRing, deriveExchange, qualifyDerivation, writeDerivation, beat } from '../clock-ring.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, activeRequests = 0, observationPending = false;
let animationPort, clockMaster, derivationCheck, roles, lifetime = 0, disposed = false;
// Scheduler turns (Program.NextTurn): declared before anything that can dispose the session.
const turnChannel = new MessageChannel(), queuedTurns = [], turnTimers = new Set();
let poseRing, poseSeqView, poseCapacity = 0, poseSlotIndex = 0, posePublicationSequence = 0n;
let latestRead, readInFlight = false, occurrenceReadReserved = false; // a read is { bytes, identity }
const occurrenceReads = [], pendingScheduleControls = [], occurrenceReadCapacity = 64;

// The pose ring layout owned by WorkshopPoseRing; WorkerAbiTests pins every value against its C# owner. Story 9A.2: this worker
// reads no read layout: C# composes each committed read's pose slot (WorkshopPoseRing.ComposeSlot) and the worker copies it.
const POSE_SLOT_COUNT = 3;             // WorkshopPoseRing.SlotCount
const POSE_HEADER_BYTES = 16;          // WorkshopPoseRing.HeaderBytes
const POSE_BODY_BYTES = 64;            // WorkshopPoseRing.BodyBytes

// Story 9R.5b: a ring's body capacity is its own length (WorkshopPoseRing.BodyCapacity); there is no fixed capacity.
function poseRingCapacity(buffer) {
    const slot = buffer.byteLength / POSE_SLOT_COUNT, capacity = (slot - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (!Number.isInteger(slot) || !Number.isInteger(capacity) || capacity < 0) throw new Error('The pose ring is not a whole number of slots.');
    return capacity;
}
function attachPoseRing(buffer) {
    poseCapacity = poseRingCapacity(buffer);
    poseRing = buffer;
    poseSeqView = new BigInt64Array(buffer);
}
// A world with more moving bodies than the ring holds replaces it, at Run, with one at least twice as large. The new ring is
// posted before the read that needs it, and messages keep their order, so the browser reads it before that read arrives.
function growPoseRing(bodyCount) {
    const capacity = Math.max(bodyCount, poseCapacity * 2);
    const buffer = new SharedArrayBuffer(POSE_SLOT_COUNT * (POSE_HEADER_BYTES + capacity * POSE_BODY_BYTES));
    attachPoseRing(buffer);
    poseSlotIndex = 0;
    self.postMessage({ poseRing: buffer });
}

// Copies one C#-composed slot (its 16-byte header, then one record per committed body) into the next ring slot under the
// sequence lock: odd while writing, even when complete. The records past the read are cleared, so no stale body survives.
function writePoseRing(slot) {
    if (!poseRing || !poseSeqView) return;
    const bodyCount = (slot.length - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (!Number.isInteger(bodyCount) || bodyCount < 0) throw new Error('A composed pose slot is not whole body records.');
    if (bodyCount > poseCapacity) growPoseRing(bodyCount);
    const slotBytes = POSE_HEADER_BYTES + poseCapacity * POSE_BODY_BYTES;
    poseSlotIndex = (poseSlotIndex + 1) % POSE_SLOT_COUNT;
    const slotByteOffset = poseSlotIndex * slotBytes;
    const seqIndex = slotByteOffset / 8;
    const nextSeq = posePublicationSequence + 2n;
    posePublicationSequence = nextSeq;
    Atomics.store(poseSeqView, seqIndex, nextSeq - 1n);
    const target = new Uint8Array(poseRing, slotByteOffset, slotBytes);
    target.set(slot.subarray(8), 8);
    target.fill(0, slot.length);
    Atomics.store(poseSeqView, seqIndex, nextSeq);
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
    poseRing = poseSeqView = undefined; poseCapacity = 0;
    occurrenceReads.length = pendingScheduleControls.length = 0;
    latestRead = undefined; readInFlight = occurrenceReadReserved = false;
}

// The clock master context answers every requester's probe through the clock ring; this worker never does (Story 9.4b).
// Its stamps derive the Simulation's native time from the shared isolated time origin plus the derivation correction,
// so this worker runs bounded synchronous exchanges with it (WorkshopClockDerivation): all startup exchanges must hold
// before the session is admitted, then one exchange every check interval, always between tick turns of this event
// loop. A small measured drift is slewed under the same epoch; a large one installs a new correction under a new epoch
// (each requester mapping recalibrates as counted telemetry); an unanswered or inconclusive check is counted. Each
// check turn also beats the ring heartbeat the Browser's liveness check reads. Nothing falls back.
async function startClockMaster(ringBuffer, browserPeer, animationPeer) {
    const clock = attachClockRing(ringBuffer);
    const [exchangeTimeout, checkInterval, checkExchanges] = Array.from(host.ClockDerivationProfile());
    const context = new Worker(new URL('./clock-master.js', import.meta.url), { type: 'module' });
    clockMaster = context;
    await new Promise((resolve, reject) => {
        context.onerror = event => reject(new Error(event.message || 'The clock master context failed to load.'));
        context.onmessage = message => {
            if (message.data?.awaitingBootstrap === true)
                context.postMessage({ ring: ringBuffer, simulationOrigin: performance.timeOrigin, browserPeer, animationPeer });
            else if (message.data?.ready === true) resolve();
            else reject(new Error(message.data?.detail ?? 'The clock master context rejected its bootstrap.'));
        };
    });
    context.onmessage = message => {
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

function forwardScheduleControl(bytes, recipient) {
    if (recipient === roles[0]) self.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else if (recipient === roles[1] && animationPort) animationPort.postMessage({ scheduleControl: bytes }, [bytes.buffer]);
    else throw new Error('Unknown or retired schedule recipient.');
}

function flushScheduleControls() {
    if (readInFlight || latestRead || occurrenceReads.length) return;
    while (pendingScheduleControls.length) {
        const item = pendingScheduleControls.shift();
        forwardScheduleControl(item.bytes, item.recipient);
    }
}

// One read in flight, posted in publication order: its receipt identity (session and publication) comes from C# with the read,
// and the browser echoes it unread.
function flushRead() {
    if (readInFlight || (occurrenceReads.length === 0 && !latestRead)) return;
    const read = occurrenceReads.length ? occurrenceReads.shift() : latestRead;
    if (read === latestRead) latestRead = undefined;
    readInFlight = true;
    self.postMessage({ read: read.bytes, readIdentity: read.identity, dispatchReadbackMs: 0 }, [read.bytes.buffer, read.identity.buffer]);
}

// The trace flush is diagnostic output: its failure never stops the session, but it is always logged, never swallowed.
function flushObservation() {
    if (!observationPending || activeRequests !== 0 || readInFlight || latestRead || occurrenceReads.length) return;
    observationPending = false;
    try { host.FlushObservation(); }
    catch (error) { console.error('CCGPU_TRACE_FLUSH_EXCEPTION', String(error)); }
}

// Story 9A.3: C# matches every receipt in its own reliable-read accounting (WorkshopReliableReadLedger), which is how the worker
// detects a reliable-read stall. A receipt it refuses is a typed rejection reported as a transport failure, never ignored.
function acknowledgeRead(receipt) {
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
function resumeTurn(turn) {
    if (disposed) return;
    try { host.ResumeTurn(turn); }
    catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: 'Scheduler turn failed: ' + String(error) }); }
}
turnChannel.port1.onmessage = () => resumeTurn(queuedTurns.shift());

runtime.setModuleImports('workshopGpu', {
    now: clockNow,
    scheduleTurn(turn, delayMilliseconds) {
        if (disposed) return;
        if (delayMilliseconds === 0) { queuedTurns.push(turn); turnChannel.port2.postMessage(0); }
        else { const timer = setTimeout(() => { turnTimers.delete(timer); resumeTurn(turn); }, delayMilliseconds); turnTimers.add(timer); }
    },
    scheduleControl(bytes, recipient) {
        const val = new Uint8Array(bytes);
        if (readInFlight || latestRead || occurrenceReads.length) {
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
    reserveOccurrenceRead() {
        if (occurrenceReadReserved) throw new Error('A physical publication reservation is already owned.');
        if (occurrenceReads.length >= occurrenceReadCapacity) return false;
        occurrenceReadReserved = true;
        return true;
    },
    releaseOccurrenceRead() { occurrenceReadReserved = false; },
    publish(bytes, identity, poseSlot, pending, retainOccurrence = false) {
        observationPending = pending;
        const res = { bytes: new Uint8Array(bytes), identity: new Uint8Array(identity) };
        if (res.identity.length !== 24) throw new Error('A physical read requires its 24-byte receipt identity.');
        writePoseRing(new Uint8Array(poseSlot));
        if (animationPort) {
            const committed = res.bytes.slice();
            animationPort.postMessage({ committedRead: committed }, [committed.buffer]);
        }
        if (retainOccurrence) {
            if (!occurrenceReadReserved || occurrenceReads.length >= occurrenceReadCapacity)
                throw new Error('Committed occurrence lacks its reserved reliable read slot.');
            latestRead = undefined;
            occurrenceReads.push(res);
        } else latestRead = res;
        occurrenceReadReserved = false;
        flushRead();
    },
    dispose: disposeOwned
});

const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.CuriousContraptions.Simulation.Program;
roles = Array.from(host.ScheduleRoles());

self.onmessage = async event => {
    try { clockNow(); } catch { return; } // a failed native clock has already retired this session and reported it
    const data = event.data;
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
    const bootstrap = event.data?.bootstrap;
    if (bootstrap instanceof Uint8Array) {
        try {
            const captureMode = event.data.captureMode;
            if (!(event.data?.poseRing instanceof SharedArrayBuffer)) throw new Error('Bootstrap requires the session pose ring.');
            attachPoseRing(event.data.poseRing);
            poseSlotIndex = 0;
            if ((captureMode !== 1 && captureMode !== 2) || captureMode !== host.CaptureMode())
                throw new Error('Browser and simulation capture modes differ.');
            if (!(event.data?.clockRing instanceof SharedArrayBuffer)) throw new Error('Bootstrap requires the session clock ring.');
            const masterPeer = new Uint8Array(host.Bootstrap(bootstrap));
            await startClockMaster(event.data.clockRing, masterPeer.slice(), new Uint8Array(host.AnimationPeer()));
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
    const transferredPort = event.data?.animationPort;
    if (transferredPort instanceof MessagePort) {
        if (animationPort || disposed) { transferredPort.close(); self.postMessage({ failure: true, detail: 'Duplicate or retired Animation port.' }); return; }
        animationPort = transferredPort;
        try {
            const animationPeer = new Uint8Array(host.AnimationPeer());
            animationPort.onmessage = incoming => {
                try {
                    if (incoming.data?.scheduleControl instanceof Uint8Array) {
                        host.ScheduleResult(incoming.data.scheduleControl, roles[1]);
                    } else throw new Error('Unsupported Animation master-port message.');
                } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
            };
            animationPort.start();
            self.postMessage({ animationPeer }, [animationPeer.buffer]);
        } catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    if (event.data?.scheduleControl instanceof Uint8Array) {
        try { host.ScheduleResult(event.data.scheduleControl, roles[0]); }
        catch (error) { disposeOwned(); self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const receipt = event.data?.readAcknowledged;
    if (receipt instanceof Uint8Array) {
        acknowledgeRead(receipt);
        return;
    }
    // Every command and query (memory observation among them) is one typed buffer for the managed decoder. Any other envelope,
    // a retired channel name included, is refused by name with its typed reason and recorded in this worker's fault log.
    const bytes = event.data?.bytes;
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
