import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, activeRequests = 0, observationPending = false;
let animationPort, roles, lifetime = 0, disposed = false;
let poseRing, poseSeqView, poseDataView, poseFloatView, poseSlotIndex = 0, posePublicationSequence = 0n;
let eventRing;
let latestRead, pendingReadIdentity, occurrenceReadReserved = false;
const occurrenceReads = [], pendingScheduleControls = [], occurrenceReadCapacity = 64;

// Byte layouts owned by the C# ABI (WorkerAbiTests verifies these against C# constants).
const PRISMATIC_BYTES = 160;
const ELECTRICAL_SOURCE_BYTES = 64;
const ELECTRICAL_BINDING_BYTES = 16;
const BODY_BYTES = 160;
const BODY_VELOCITY = 64;
const BODY_ANGULAR_VELOCITY = 76;
const BODY_MASS = 88;
const BODY_DRAG = 90;
const BODY_GRAVITY = 92;
const BODY_CENTRE_OF_MASS = 98;
const MOTION_HEADER_BYTES = 16;
const MOTION_PIECE_BYTES = 144;
const MOTION_DRAG_RATE = 60;
const MOTION_VELOCITY = 80;
const MOTION_ANGULAR_VELOCITY = 92;
const MOTION_GRAVITY = 104;
const READ_BODIES_OFFSET = 128;
const BODY_WIRE_BYTES = 80;
const BODY_WIRE_VELOCITY = 56;

let COLLIDERS_OFFSET = 5408;
let MATERIALS_OFFSET = 11552;
let TRIGGERS_OFFSET = 15680;
let CONTACT_WORKS_OFFSET = 16192;

function writePoseRing(bytes) {
    if (!poseRing || !poseSeqView || !poseDataView || !poseFloatView || bytes.length < 128) return;
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const bodyCount = bytes[104];
    const timestamp = view.getBigInt64(72, true);
    poseSlotIndex = (poseSlotIndex + 1) % 3;
    const s = poseSlotIndex;
    const seqIndex = s * 130;
    const nextSeq = posePublicationSequence + 2n;
    posePublicationSequence = nextSeq;
    Atomics.store(poseSeqView, seqIndex, nextSeq - 1n);

    const slotByteOffset = s * 1040;
    poseDataView.setBigInt64(slotByteOffset + 8, timestamp, true);
    const bodyFloatOffset = (slotByteOffset + 16) / 4;

    for (let i = 0; i < 16; i++) {
        const b = bodyFloatOffset + i * 16;
        if (i < bodyCount && READ_BODIES_OFFSET + (i + 1) * BODY_WIRE_BYTES <= bytes.length) {
            const src = READ_BODIES_OFFSET + i * BODY_WIRE_BYTES;
            poseFloatView[b + 0] = (view.getInt32(src + 8, true) + view.getFloat32(src + 20, true)) / 16;
            poseFloatView[b + 1] = (view.getInt32(src + 12, true) + view.getFloat32(src + 24, true)) / 16;
            poseFloatView[b + 2] = (view.getInt32(src + 16, true) + view.getFloat32(src + 28, true)) / 16;
            poseDataView.setUint32((b + 3) * 4, 0, true);
            poseDataView.setBigUint64((b + 12) * 4, view.getBigUint64(src, true), true);
            poseFloatView[b + 4] = view.getFloat32(src + 32, true);
            poseFloatView[b + 5] = view.getFloat32(src + 36, true);
            poseFloatView[b + 6] = view.getFloat32(src + 40, true);
            poseFloatView[b + 7] = view.getFloat32(src + 44, true);
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

export function writeEventRing(ring, events) {
    if (!ring || !events || !events.length) return;
    const headerU32 = new Uint32Array(ring, 0, 16);
    const headerU64 = new BigUint64Array(ring, 0, 8);
    const capacity = headerU32[0] || 1024;
    const dv = new DataView(ring);
    const capMask = BigInt(capacity - 1);
    for (const ev of events) {
        const nextSeq = Atomics.add(headerU64, 1, 1n) + 1n;
        const slotIdx = Number((nextSeq - 1n) & capMask);
        const offset = 64 + slotIdx * 64;
        dv.setBigUint64(offset + 8, BigInt(ev.timestampNs || 0), true);
        dv.setUint32(offset + 16, ev.domain || 0, true);
        dv.setUint32(offset + 20, ev.eventKind || 0, true);
        dv.setBigUint64(offset + 24, BigInt(ev.sourceBodyId || 0), true);
        dv.setBigUint64(offset + 32, BigInt(ev.targetBodyId || 0), true);
        dv.setFloat32(offset + 40, ev.p0 ?? ev.impulse ?? 0, true);
        dv.setFloat32(offset + 44, ev.p1 ?? ev.contactX ?? 0, true);
        dv.setFloat32(offset + 48, ev.p2 ?? ev.contactY ?? 0, true);
        dv.setFloat32(offset + 52, ev.p3 ?? ev.contactZ ?? 0, true);
        dv.setFloat32(offset + 56, ev.p4 ?? ev.value ?? 0, true);
        dv.setFloat32(offset + 60, ev.p5 ?? 0, true);
        Atomics.store(new BigUint64Array(ring, offset, 1), 0, nextSeq);
    }
}

function clockNow() {
    try { return nativeNow(); }
    catch (e) { disposeOwned(); self.postMessage({ failure: true, detail: String(e) }); self.close(); throw e; }
}

function disposeOwned() {
    disposed = true; lifetime++;
    if (animationPort) { animationPort.onmessage = null; animationPort.close(); animationPort = undefined; }
    poseRing = poseSeqView = poseDataView = poseFloatView = undefined;
    eventRing = undefined;
    occurrenceReads.length = pendingScheduleControls.length = 0;
    latestRead = undefined; occurrenceReadReserved = false;
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

function extractReadIdentity(bytes, respAbi) {
    if (bytes.length >= 12) {
        const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
        const rootPos = view.getUint32(0, true);
        if (rootPos >= 4 && rootPos <= bytes.length - 4) {
            const vtableOffset = view.getInt32(rootPos, true);
            const vtablePos = rootPos - vtableOffset;
            if (vtableOffset > 0 && vtablePos >= 0 && vtablePos <= rootPos) {
                const vtableSize = view.getUint16(vtablePos, true);
                let publication = 0n, sessionBytes = null;
                if (vtableSize >= 24) {
                    const pubFieldOffset = view.getUint16(vtablePos + 22, true);
                    if (pubFieldOffset > 0 && rootPos + pubFieldOffset + 8 <= bytes.length)
                        publication = view.getBigUint64(rootPos + pubFieldOffset, true);
                }
                if (vtableSize >= 32) {
                    const sessFieldOffset = view.getUint16(vtablePos + 30, true);
                    if (sessFieldOffset > 0 && rootPos + sessFieldOffset + 4 <= bytes.length) {
                        const vecOffset = view.getInt32(rootPos + sessFieldOffset, true);
                        const vecPos = rootPos + sessFieldOffset + vecOffset;
                        if (vecPos >= 0 && vecPos + 4 <= bytes.length && view.getUint32(vecPos, true) === 16)
                            sessionBytes = bytes.subarray(vecPos + 4, vecPos + 20);
                    }
                }
                if (sessionBytes) {
                    const id = new Uint8Array(24);
                    id.set(sessionBytes, 0);
                    new DataView(id.buffer, id.byteOffset, 24).setBigUint64(16, publication, true);
                    return id;
                }
            }
        }
    }
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

runtime.setModuleImports('workshopGpu', {
    now: clockNow,
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
            if (event.data?.poseRing) {
                if (event.data.poseRing.byteLength !== 3120) throw new Error('Unsupported pose ring layout');
                poseRing = event.data.poseRing;
                poseSeqView = new BigInt64Array(poseRing);
                poseDataView = new DataView(poseRing);
                poseFloatView = new Float32Array(poseRing);
                poseSlotIndex = 0;
            }
            if (event.data?.eventRing) {
                if (event.data.eventRing.byteLength !== 65600) throw new Error('Unsupported event ring layout');
                eventRing = event.data.eventRing;
                const headerU32 = new Uint32Array(eventRing, 0, 16);
                Atomics.store(headerU32, 0, 1024);
                Atomics.store(headerU32, 1, 64);
            }
            if ((captureMode !== 1 && captureMode !== 2) || captureMode !== host.CaptureMode())
                throw new Error('Browser and simulation capture modes differ.');
            const masterPeer = new Uint8Array(host.Bootstrap(bootstrap));
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
                    if (incoming.data?.clockProbe instanceof Uint8Array) {
                        const value = new Uint8Array(host.AnimationClockReply(incoming.data.clockProbe, clockNow()));
                        animationPort.postMessage({ clockReply: value }, [value.buffer]);
                    } else if (incoming.data?.scheduleControl instanceof Uint8Array) {
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
    const probe = event.data?.clockProbe;
    if (probe instanceof Uint8Array) {
        try {
            const reply = new Uint8Array(host.ClockReply(probe, received));
            self.postMessage({ clockReply: reply }, [reply.buffer]);
        } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
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
