import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';
import { attachClockRing, deriveExchange, writeDerivation, beat } from '../clock-ring.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, activeRequests = 0, observationPending = false;
let animationPort, clockMaster, derivationCheck, roles, lifetime = 0, disposed = false;
let poseRing, poseSeqView, poseDataView, poseFloatView, poseSlotIndex = 0, posePublicationSequence = 0n;
let latestRead, pendingReadIdentity, occurrenceReadReserved = false;
const occurrenceReads = [], pendingScheduleControls = [], occurrenceReadCapacity = 64;

// Byte layouts owned by the C# ABI; WorkerAbiTests pins every one against its C# owner (Story 9.4b deleted the
// declared-but-unused JS-kernel layouts and the never-written event ring, and named every offset this worker reads).
const READ_CAPTURE_TIME = 72;          // WorkshopWire.ResponseCaptureTimeOffset
const READ_BODY_COUNT = 104;           // WorkshopWire.ResponseBodyCountOffset
const READ_BODIES_OFFSET = 128;        // WorkshopWire.ReadBodiesOffset
const BODY_WIRE_BYTES = 80;            // PhysicsBodyWire.ByteLength
const BODY_WIRE_CELL = 8;              // PhysicsBodyWire.CellOffset
const BODY_WIRE_LOCAL = 20;            // PhysicsBodyWire.LocalOffset
const BODY_WIRE_ROTATION = 32;         // PhysicsBodyWire.RotationOffset
const BODY_WIRE_VELOCITY = 56;         // PhysicsBodyWire.VelocityOffset
const POSE_SLOT_COUNT = 3;             // WorkshopPoseRing.SlotCount
const POSE_BODY_CAPACITY = 16;         // WorkshopPoseRing.BodyCapacity
const POSE_HEADER_BYTES = 16;          // WorkshopPoseRing.HeaderBytes
const POSE_BODY_BYTES = 64;            // WorkshopPoseRing.BodyBytes
const POSE_SLOT_BYTES = 1040;          // WorkshopPoseRing.SlotBytes
const POSE_RING_BYTES = 3120;          // WorkshopPoseRing.TotalBytes

function writePoseRing(bytes) {
    if (!poseRing || !poseSeqView || !poseDataView || !poseFloatView || bytes.length < READ_BODIES_OFFSET) return;
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const bodyCount = bytes[READ_BODY_COUNT];
    const timestamp = view.getBigInt64(READ_CAPTURE_TIME, true);
    poseSlotIndex = (poseSlotIndex + 1) % POSE_SLOT_COUNT;
    const s = poseSlotIndex;
    const seqIndex = s * (POSE_SLOT_BYTES / 8);
    const nextSeq = posePublicationSequence + 2n;
    posePublicationSequence = nextSeq;
    Atomics.store(poseSeqView, seqIndex, nextSeq - 1n);

    const slotByteOffset = s * POSE_SLOT_BYTES;
    poseDataView.setBigInt64(slotByteOffset + 8, timestamp, true);
    const bodyFloatOffset = (slotByteOffset + POSE_HEADER_BYTES) / 4;

    for (let i = 0; i < POSE_BODY_CAPACITY; i++) {
        const b = bodyFloatOffset + i * (POSE_BODY_BYTES / 4);
        if (i < bodyCount && READ_BODIES_OFFSET + (i + 1) * BODY_WIRE_BYTES <= bytes.length) {
            const src = READ_BODIES_OFFSET + i * BODY_WIRE_BYTES;
            for (let axis = 0; axis < 3; axis++)
                poseFloatView[b + axis] = (view.getInt32(src + BODY_WIRE_CELL + axis * 4, true) +
                    view.getFloat32(src + BODY_WIRE_LOCAL + axis * 4, true)) / 16;
            poseDataView.setUint32((b + 3) * 4, 0, true);
            poseDataView.setBigUint64((b + 12) * 4, view.getBigUint64(src, true), true);
            for (let component = 0; component < 4; component++)
                poseFloatView[b + 4 + component] = view.getFloat32(src + BODY_WIRE_ROTATION + component * 4, true);
            poseFloatView[b + 8] = view.getFloat32(src + BODY_WIRE_VELOCITY, true);
            poseFloatView[b + 9] = view.getFloat32(src + BODY_WIRE_VELOCITY + 4, true);
            poseFloatView[b + 10] = view.getFloat32(src + BODY_WIRE_VELOCITY + 8, true);
            poseDataView.setUint32((b + 11) * 4, 1, true);
        } else {
            poseFloatView[b + 0] = poseFloatView[b + 1] = poseFloatView[b + 2] = 0;
            poseDataView.setUint32((b + 3) * 4, 0, true);
            poseFloatView[b + 4] = poseFloatView[b + 5] = poseFloatView[b + 6] = poseFloatView[b + 7] = 0;
            poseFloatView[b + 8] = poseFloatView[b + 9] = poseFloatView[b + 10] = 0;
            poseDataView.setUint32((b + 11) * 4, 0, true);
            poseDataView.setBigUint64((b + 12) * 4, 0n, true);
        }
    }
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
    poseRing = poseSeqView = poseDataView = poseFloatView = undefined;
    occurrenceReads.length = pendingScheduleControls.length = 0;
    latestRead = undefined; occurrenceReadReserved = false;
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
    while (!host.ClockDerivationQualified()) {
        const exchange = deriveExchange(clock, clockNow, exchangeTimeout);
        if (exchange) host.QualifyClockDerivation(exchange.before, exchange.master, exchange.after, exchange.epoch);
        else host.ClockDerivationQualificationMissed();
    }
    // The derived offset between the contexts' time origins is installed under the initial epoch before any requester
    // qualifies its mapping (this worker reports ready only after this returns).
    host.AdoptClockDerivation(writeDerivation(clock, host.ClockDerivationStartupCorrection(), false));
    derivationCheck = setInterval(() => {
        if (disposed) return;
        try {
            // The heartbeat proves this event loop still runs between tick turns (the Browser faults the session when
            // it stops); the derivation check rides on the same turn.
            /* REVIEW MUTANT (heartbeat disabled): beat(clock); */
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
    if (pendingReadIdentity || latestRead || occurrenceReads.length) return;
    while (pendingScheduleControls.length) {
        const item = pendingScheduleControls.shift();
        forwardScheduleControl(item.bytes, item.recipient);
    }
}

// Every physical read this worker publishes is the binary WorkshopWire response (Program.ReadOutput); its receipt identity
// is the session and publication at the offsets the managed ResponseAbi declares. (Story 9.4b deleted a FlatBuffers
// branch here that no read could reach: a read's leading sequence word is zero.)
function extractReadIdentity(bytes, respAbi) {
    if (bytes.length !== respAbi[0]) throw new Error('A physical read must be one complete binary response.');
    const id = new Uint8Array(24);
    id.set(bytes.subarray(respAbi[1], respAbi[1] + 16));
    id.set(bytes.subarray(respAbi[2], respAbi[2] + 8), 16);
    return id;
}

function flushRead() {
    if (pendingReadIdentity || (occurrenceReads.length === 0 && !latestRead)) return;
    const bytes = occurrenceReads.length ? occurrenceReads.shift() : latestRead;
    if (bytes === latestRead) latestRead = undefined;
    pendingReadIdentity = extractReadIdentity(bytes, responseAbi);
    self.postMessage({ read: bytes, dispatchReadbackMs: 0 }, [bytes.buffer]);
}

function flushObservation() {
    if (!observationPending || activeRequests !== 0 || pendingReadIdentity || latestRead || occurrenceReads.length) return;
    observationPending = false;
    try { host.FlushObservation(); } catch {}
}

function acknowledgeRead(receipt) {
    if (!pendingReadIdentity || receipt.length !== 24 || !receipt.every((v, i) => v === pendingReadIdentity[i])) return;
    pendingReadIdentity = undefined;
    flushRead();
    flushScheduleControls();
    flushObservation();
}

// Scheduler turns of the run loop (Program.NextTurn). Delay 0 is an immediate macrotask through a MessageChannel, so
// queued commands, receipts and schedule controls run between tick batches without the 4 ms clamp a chained timer would
// add to catch-up; a positive delay is the ordinary wake-up timer. Either task calls ResumeTurn itself, so the loop
// continues inside it: one macrotask per turn, never a second runtime hop behind other queued work.
const turnChannel = new MessageChannel(), queuedTurns = [];
turnChannel.port1.onmessage = () => host.ResumeTurn(queuedTurns.shift());

runtime.setModuleImports('workshopGpu', {
    now: clockNow,
    scheduleTurn(turn, delayMilliseconds) {
        if (delayMilliseconds === 0) { queuedTurns.push(turn); turnChannel.port2.postMessage(0); }
        else setTimeout(() => host.ResumeTurn(turn), delayMilliseconds);
    },
    scheduleControl(bytes, recipient) {
        const val = new Uint8Array(bytes);
        if (pendingReadIdentity || latestRead || occurrenceReads.length) {
            if (pendingScheduleControls.length >= 4) throw new Error('Schedule drain capacity exceeded.');
            pendingScheduleControls.push({ bytes: val, recipient });
        } else forwardScheduleControl(val, recipient);
    },
    acknowledge(bytes, pending) {
        observationPending = pending;
        const res = new Uint8Array(bytes);
        self.postMessage({ acknowledgement: res, dispatchReadbackMs: 0 }, [res.buffer]);
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
    publish(bytes, pending, retainOccurrence = false) {
        observationPending = pending;
        const res = new Uint8Array(bytes);
        writePoseRing(res);
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
host = exports.Program;
roles = Array.from(host.ScheduleRoles());
const commandAbi = Array.from(host.CommandAbi());
const responseAbi = Array.from(host.ResponseAbi());

self.onmessage = async event => {
    let received;
    try { received = clockNow(); } catch { return; }
    const bootstrap = event.data?.bootstrap;
    if (bootstrap instanceof Uint8Array) {
        try {
            const captureMode = event.data.captureMode;
            if (!(event.data?.poseRing instanceof SharedArrayBuffer) || event.data.poseRing.byteLength !== POSE_RING_BYTES)
                throw new Error('Bootstrap requires the session pose ring.');
            poseRing = event.data.poseRing;
            poseSeqView = new BigInt64Array(poseRing);
            poseDataView = new DataView(poseRing);
            poseFloatView = new Float32Array(poseRing);
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
    if (typeof event.data?.reliableStalled === 'boolean') { if (!disposed) host.SetReliableStall(event.data.reliableStalled); return; }
    const receipt = event.data?.readAcknowledged;
    if (receipt instanceof Uint8Array) {
        acknowledgeRead(receipt);
        return;
    }
    const memoryRequest = event.data?.memoryRequest;
    if (memoryRequest instanceof Uint8Array) {
        try {
            if (memoryRequest.length !== 64) throw new Error('Invalid stopped memory request width.');
            const result = new Uint8Array(host.ObserveMemory(memoryRequest,
                activeRequests === 0 && !latestRead && !pendingReadIdentity));
            if (clockNow() - received > 1000) {
                console.error('CCGPU_MEMORY_LATE', Array.from(result).join(','));
                throw new Error('Stopped worker collection exceeded 1000ms; reload to recover.');
            }
            self.postMessage({ memoryResult: result }, [result.buffer]);
        } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const bytes = event.data?.bytes;
    if (!(bytes instanceof Uint8Array) || bytes.length < commandAbi[0] || bytes.length > commandAbi[1] || activeRequests >= 2) {
        self.postMessage({ rejected: true, detail: 'Invalid or saturated Workshop command transport.' });
        return;
    }
    activeRequests++;
    try {
        await host.Dispatch(bytes);
    } catch (error) {
        self.postMessage({ failure: Array.from(bytes.slice(0, 8)), detail: String(error) });
    } finally { activeRequests--; flushObservation(); }
};
self.postMessage({ awaitingBootstrap: true });
