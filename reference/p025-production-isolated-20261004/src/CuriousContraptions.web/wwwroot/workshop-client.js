import { admitNativeClock, nativeNow, nativeProfile, nativeClockEvidence } from './native-clock.js';
import { requireIsolation, watchIsolation, isolationQualified, isolationEvidence, stopIsolation } from './workshop-isolation.js';

const clients = new Map();
let nextClient = 1, retiredSession = false, creating = false;
function owner(id) {
    const client = clients.get(id);
    if (!client) throw new Error('Workshop worker client is disposed.');
    return client;
}
function sequence(bytes) {
    if (!(bytes instanceof Uint8Array) || bytes.length < 8) throw new Error('Invalid command identity.');
    return new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getBigUint64(0, true);
}
function fail(client, error, retiring = false) {
    if (client.failure) return;
    client.failure = error;
    client.state = client.states.indeterminate;
    if (!retiring) console.error('CCGPU_TRANSPORT_FAILURE', client.state, error);
    retiredSession = true;
    client.unwatchIsolation?.(); client.unwatchIsolation = undefined;
    clearInterval(client.service);
    clearTimeout(client.startup);
    client.worker.onmessage = null;
    client.worker.onerror = null;
    client.worker.terminate();
    if (client.animation) { client.animation.onmessage = client.animation.onerror = null; client.animation.terminate(); }
    client.animationPort?.close(); client.animationPort = undefined;
    for (const pending of client.pending.values()) pending.reject(error);
    client.pending.clear();
    settleMemory(client, error);
    client.bootstrapReject?.(error);
    client.bootstrapReject = null;
    if (!retiring) stopIsolation(error);
    client.retirement = setTimeout(() => { client.state = client.states.recoveryBlocked; }, 1000);
}
function deliver(client, bytes, received, callback, clockLease = false) {
    if (client.eventBytes) throw new Error('Reentrant worker delivery is unsupported.');
    client.eventBytes = bytes; client.eventReceived = received;
    client.eventClock = clockLease; client.clockAccepted = false;
    try { callback(); }
    finally { client.eventBytes = undefined; client.eventReceived = undefined; client.eventClock = false; client.clockAccepted = false; }
}
async function admitDisplayRate() {
    return await new Promise((resolve, reject) => {
        const intervals = [];
        let previous, frame;
        const timeout = setTimeout(() => { cancelAnimationFrame(frame); reject(new Error('Display cadence admission timed out.')); }, 2000);
        const sample = timestamp => {
            if (previous !== undefined) intervals.push(timestamp - previous);
            previous = timestamp;
            if (intervals.length < 20) { frame = requestAnimationFrame(sample); return; }
            clearTimeout(timeout);
            intervals.sort((a, b) => a - b);
            const milliseconds = (intervals[9] + intervals[10]) / 2;
            let numerator = Math.round(1000000 / milliseconds), denominator = 1000;
            if (!Number.isFinite(milliseconds) || milliseconds <= 0 || numerator < 30000 || numerator > 240000) {
                reject(new Error('Unsupported display cadence.')); return;
            }
            let a = numerator, b = denominator;
            while (b) { const remainder = a % b; a = b; b = remainder; }
            resolve([numerator / a, denominator / a]);
        };
        frame = requestAnimationFrame(sample);
    });
}
export function displayRate(id) { return owner(id).displayRate; }
export async function create(states, bootstrapBytes, clockAbi, captureMode, admitDisplay, clockReply, physicalRead, scheduleControl, animationOutput, service, beginMemory, receiveMemory, releaseMemory) {
    if (clients.size !== 0 || retiredSession || creating) throw new Error('RecoveryBlocked: reload the page before creating another worker.');
    creating = true;
    let id;
    try {
        if (captureMode !== 1 && captureMode !== 2) throw new Error('Unknown Workshop capture mode.');
        requireIsolation();
        await admitNativeClock();
        requireIsolation();
        if (typeof admitDisplay !== 'boolean') throw new Error('Invalid display admission selection.');
        const displayRate = admitDisplay ? await admitDisplayRate() : undefined;
        const session = new Uint8Array(bootstrapBytes);
        if (session.length !== 16 || !session.some(value => value !== 0) || clockAbi.length !== 8)
            throw new Error('Unsupported session bootstrap.');
        const [clockVersion, clockProfile, peerBytes, probeBytes, replyBytes, diagnosticBytes, browserRole, masterRole] = clockAbi;
        if (clockProfile !== nativeProfile) throw new Error('Unsupported native clock profile.');
        const [ready, backpressure, timedOut, indeterminate, recoveryBlocked] = states;
        id = nextClient++;
        const client = { displayRate, worker: undefined, pending: new Map(), acknowledgements: new Map(),
            failure: null, states: { ready, backpressure, timedOut, indeterminate, recoveryBlocked },
            serviceCallback: service, service: undefined,
            state: ready, lastReply: now(), stalled: false, candidateTimedOut: false,
            identity: undefined, clockAbi: { clockVersion, clockProfile, peerBytes, probeBytes, replyBytes, diagnosticBytes, browserRole, masterRole }, captureMode, beginMemory, receiveMemory, releaseMemory,
            memory: undefined,
            observations: captureMode === 2 ? new Uint8Array(128 * diagnosticBytes) : undefined, observationNext: 0, observationCount: 0, observationSequence: 0n,
            presentations: captureMode === 2 ? new Uint8Array(32 * 352) : undefined, presentationSequence: 0n };
        const worker = new Worker(new URL('./simulation/worker.js', document.baseURI), { type: 'module' });
        client.worker = worker;
        clients.set(id, client);
        client.unwatchIsolation = watchIsolation(error => fail(client, error));
        await new Promise((resolve, reject) => {
            client.bootstrapReject = reject;
            client.startup = setTimeout(() => fail(client, new Error('StartupFailed: usable GPU and clock qualification exceeded five seconds.')), 5000);
            let readyReceived = false;
            worker.onmessage = event => {
                if (client.failure || !clients.has(id)) return;
                try {
                    const received = now(), message = event.data;
                    if (message.awaitingBootstrap === true) {
                        const bootstrap = new Uint8Array(bootstrapBytes);
                        worker.postMessage({ bootstrap, captureMode }, [bootstrap.buffer]);
                    } else if (message.ready === true) {
                        if (readyReceived) throw new Error('Duplicate worker bootstrap.');
                        if (message.captureMode !== captureMode) throw new Error('Worker capture mode differs from the browser build.');
                        const profile = message.nativeClock;
                        if (!(profile instanceof Float64Array) || profile.length !== 7 || !profile.every(Number.isFinite) ||
                            profile[0] !== nativeProfile || profile[1] !== 1 || profile[2] !== 1 ||
                            profile[3] < 0 || profile[4] <= 0 || profile[4] > 0.007 ||
                            !Number.isSafeInteger(profile[5]) || profile[5] <= 0 || profile[6] < profile[3])
                            throw new Error('Worker native clock profile is unproven.');
                        const masterPeer = message.masterPeer;
                        if (!(masterPeer instanceof Uint8Array) || masterPeer.length !== peerBytes)
                            throw new Error('Missing authoritative master bootstrap.');
                        const declared = new DataView(masterPeer.buffer, masterPeer.byteOffset, masterPeer.byteLength);
                        if (declared.getUint32(0, true) !== clockVersion || declared.getUint32(4, true) !== clockProfile ||
                            !session.every((value, index) => value === masterPeer[index + 8]) ||
                            declared.getBigUint64(24, true) !== 1n || declared.getBigInt64(32, true) < 0n ||
                            declared.getBigUint64(40, true) !== 1n || declared.getBigUint64(48, true) !== 100000n ||
                            declared.getUint32(56, true) !== browserRole || declared.getUint32(60, true) !== masterRole)
                            throw new Error('Invalid authoritative master bootstrap.');
                        client.identity = masterPeer;
                        const channel = new MessageChannel();
                        client.animationPort = channel.port2;
                        worker.postMessage({ animationPort: channel.port1 }, [channel.port1]);
                        client.workerProfile = profile;
                        client.lastReply = received; // Liveness begins when this worker can answer its first probe.
                        readyReceived = true;
                        client.bootstrapReject = null;
                        resolve();
                    } else if (message.animationPeer instanceof Uint8Array) {
                        if (client.animation || !client.animationPort) throw new Error('Duplicate Animation ownership.');
                        const animation = new Worker(new URL('./animation/worker.js', document.baseURI), { type: 'module' });
                        client.animation = animation;
                        animation.onerror = error => fail(client, new Error(error.message || 'Animation worker failed.'));
                        animation.onmessage = incoming => {
                            if (client.failure) return;
                            try {
                                const item = incoming.data;
                                if (item.awaitingBootstrap === true) {
                                    const bootstrap = message.animationPeer;
                                    const clockPort = client.animationPort;
                                    if (!clockPort) throw new Error('Animation port was already transferred.');
                                    client.animationPort = undefined;
                                    animation.postMessage({ bootstrap, clockPort }, [bootstrap.buffer, clockPort]);
                                } else if (item.ready === true) {
                                    if (client.animationReady || !(item.nativeClock instanceof Float64Array) ||
                                        item.nativeClock.length !== 7 || item.nativeClock[0] !== nativeProfile ||
                                        item.nativeClock[1] !== 1 || item.nativeClock[2] !== 1)
                                        throw new Error('Invalid Animation native-clock admission.');
                                    client.animationReady = true;
                                } else if (item.qualified === true) {
                                    if (!client.animationReady || client.animationQualified) throw new Error('Duplicate Animation qualification.');
                                    client.animationQualified = true;
                                } else if (item.animationOutput instanceof Uint8Array) {
                                    if (item.animationOutput.length !== 96) throw new Error('Wrong Animation output length.');
                                    deliver(client, item.animationOutput, now(), animationOutput);
                                    const kind = new DataView(item.animationOutput.buffer, item.animationOutput.byteOffset, 96).getUint32(60, true);
                                    if (kind === client.hintKinds[1]) {
                                        const animationAcknowledged = item.animationOutput.slice();
                                        animation.postMessage({ animationAcknowledged }, [animationAcknowledged.buffer]);
                                    } else if (kind === client.hintKinds[0] || kind === client.hintKinds[2]) {
                                        if (!client.hintPending) throw new Error('Unsolicited hint acknowledgement.');
                                        client.hintPending = false;
                                    } else throw new Error('Unknown Animation output.');
                                } else throw new Error(item.detail || 'Unknown Animation envelope.');
                            } catch (error) { fail(client, error); }
                        };
                    } else if (message.scheduleControl instanceof Uint8Array) {
                        deliver(client, message.scheduleControl, received, scheduleControl);
                    } else if (message.clockReply instanceof Uint8Array) {
                        deliver(client, message.clockReply, received, clockReply, true);
                    } else if (typeof message.candidateTimedOut === 'boolean') {
                        client.candidateTimedOut = message.candidateTimedOut;
                    } else if (message.acknowledgement instanceof Uint8Array) {
                        const key = sequence(message.acknowledgement);
                        const pending = client.pending.get(key);
                        if (!pending) throw new Error('Unexpected worker acknowledgement.');
                        client.pending.delete(key);
                        client.acknowledgements.set(key, message.acknowledgement);
                        pending.resolve();
                    } else if (message.memoryResult instanceof Uint8Array) {
                        receiveMemoryResult(client, message.memoryResult, received);
                    } else if (message.read instanceof Uint8Array) {
                        // Complete C# validation/admission precedes return; JS retains no pose history.
                        deliver(client, message.read, received, physicalRead);
                        const receipt = new Uint8Array(24);
                        receipt.set(message.read.subarray(64, 80));
                        receipt.set(message.read.subarray(104, 112), 16);
                        worker.postMessage({ readAcknowledged: receipt }, [receipt.buffer]);
                    } else if (message.failure || message.rejected) {
                        throw new Error(message.detail ?? 'Worker rejected its transport envelope.');
                    } else throw new Error('Unknown worker transport envelope.');
                } catch (error) { fail(client, error); }
            };
            worker.onerror = event => fail(client, new Error(
                typeof event.message === 'string' && event.message.trim().length > 0
                    ? event.message : 'Workshop simulation worker failed to load or execute. Reload the page to try again.'));
        });
        return id;
    } catch (error) {
        retiredSession = true;
        if (id !== undefined) dispose(id);
        stopIsolation(error);
        throw error;
    } finally { creating = false; }
}
export function animationQualified(id) { return owner(id).animationQualified === true; }
export function animationKinds(id, kinds) {
    if (kinds.length !== 3 || new Set(kinds).size !== 3) throw new Error('Invalid Animation output ABI.');
    owner(id).hintKinds = Array.from(kinds);
}
export function scheduleResult(id, bytes) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    const scheduleControl = new Uint8Array(bytes);
    client.worker.postMessage({ scheduleControl }, [scheduleControl.buffer]);
}
export function hint(id, bytes) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    if (!client.animationQualified || client.hintPending) throw new Error('Animation control is unavailable or pending.');
    const hintControl = new Uint8Array(bytes);
    if (hintControl.length !== 96) throw new Error('Invalid hint command.');
    client.hintPending = true;
    client.animation.postMessage({ hintControl }, [hintControl.buffer]);
}
export function bootstrap(id) {
    const client = owner(id);
    if (!client.identity) throw new Error('Master bootstrap is not available.');
    return client.identity;
}
export function activate(id) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    if (!client.workerProfile || client.service !== undefined)
        throw new Error('Workshop service activation requires one ready owner.');
    client.service = setInterval(() => {
        if (client.failure) return;
        try {
            const current = now();
            client.serviceCallback();
            if (!document.hidden && current - client.lastReply > 1000)
                throw new Error('Worker fault: matching clock reply missing for 1000ms.');
            let oldest = 0;
            for (const pending of client.pending.values()) oldest = Math.max(oldest, current - pending.started);
            client.state = oldest > 2000 || client.candidateTimedOut ? client.states.timedOut
                : oldest > 50 ? client.states.backpressure : client.states.ready;
            if (oldest > 500 && !client.stalled) { client.stalled = true; client.worker.postMessage({ reliableStalled: true }); }
            if (oldest === 0) client.stalled = false;
        } catch (error) { fail(client, error); }
    }, 10);
}
export function eventBytes(id) {
    const bytes = owner(id).eventBytes;
    if (!bytes) throw new Error('No synchronous worker delivery is active.');
    return bytes;
}
export function eventMilliseconds(id) {
    const received = owner(id).eventReceived;
    if (received === undefined) throw new Error('No synchronous worker delivery is active.');
    return received;
}
export function now() {
    try { requireIsolation(); return nativeNow(); }
    catch (error) {
        for (const client of clients.values()) fail(client, error);
        stopIsolation(error);
        throw error;
    }
}
export function probe(id, bytes) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    const clockProbe = new Uint8Array(bytes);
    client.worker.postMessage({ clockProbe }, [clockProbe.buffer]);
}
export function acceptedClockReply(id) {
    const client = owner(id);
    if (!client.eventClock || client.clockAccepted || client.eventReceived === undefined)
        throw new Error('Accepted heartbeat requires one active clock-reply lease.');
    client.clockAccepted = true;
    client.lastReply = client.eventReceived;
}
export function clockObservation(id, bytes) {
    const client = owner(id);
    if (!client.observations) throw new Error('Clock capture is unavailable in Production.');
    if (bytes.length !== client.clockAbi.diagnosticBytes) throw new Error('Invalid fixed clock diagnostic record.');
    if (client.observationSequence === 0xffffffffffffffffn) throw new Error('Clock evidence sequence exhausted.');
    client.observations.set(bytes, client.observationNext * client.clockAbi.diagnosticBytes);
    client.observationSequence++;
    client.observationNext = (client.observationNext + 1) % 128;
    client.observationCount = Math.min(128, client.observationCount + 1);

}
// Both fixed rings total29,696 retained payload bytes. Extraction visits at most8 records and does
// not copy the ring; its largest Number payload is8*352*8=22,528 scratch bytes.
function evidence(client, ring, total, recordBytes, capacity, afterSequence) {
    if (typeof afterSequence !== 'string' || !/^(0|[1-9][0-9]{0,19})$/.test(afterSequence))
        throw new Error('Evidence cursor must be an exact unsigned decimal sequence.');
    if (!ring) throw new Error('Evidence capture is unavailable in Production.');
    if (client.memory) throw new Error('A stopped observation owns the evidence reservation.');
    const after = BigInt(afterSequence);
    if (after > total) throw new Error('Evidence cursor is ahead of this session.');
    const oldest = total >= BigInt(capacity) ? total - BigInt(capacity) + 1n : 1n;
    const first = after + 1n < oldest ? oldest : after + 1n;
    const count = Number(total < first ? 0n : total - first + 1n > 8n ? 8n : total - first + 1n);
    const bytes = new Array(count * recordBytes);
    for (let i = 0; i < count; i++) {
        const source = Number((first + BigInt(i) - 1n) % BigInt(capacity)) * recordBytes;
        for (let j = 0; j < recordBytes; j++) bytes[i * recordBytes + j] = ring[source + j];
    }
    return { identity: Array.from(client.identity), first: first.toString(), count,
        total: total.toString(), overwritten: (oldest - 1n).toString(),
        missed: (first - after - 1n).toString(), recordBytes, bytes };
}
export function clockEvidence(id, afterSequence = '0') {
    const client = owner(id);
    return evidence(client, client.observations, client.observationSequence, client.clockAbi.diagnosticBytes, 128, afterSequence);
}
export function presentationObservation(id, bytes) {
    const client = owner(id);
    if (!client.presentations) throw new Error('Presentation capture is unavailable in Production.');
    if (!(bytes instanceof Uint8Array) || bytes.length !== 352)
        throw new Error('Invalid fixed presentation diagnostic record.');
    if (client.presentationSequence === 0xffffffffffffffffn) throw new Error('Presentation evidence sequence exhausted.');
    client.presentations.set(bytes, Number(client.presentationSequence % 32n) * 352);
    client.presentationSequence++;
}
export function presentationEvidence(id, afterSequence = '0') {
    const client = owner(id);
    return evidence(client, client.presentations, client.presentationSequence, 352, 32, afterSequence);
}
export function qualified(id) {
    const client = owner(id);
    isolationQualified();
    clearTimeout(client.startup);
}
export function clockProfileEvidence(id) {
    const client = owner(id);
    return { browser: nativeClockEvidence(), worker: Array.from(client.workerProfile ?? []), serving: isolationEvidence() };
}
export function status(id) { return owner(id).state; }
export function captureMode(id) { return owner(id).captureMode; }

function settleMemory(client, error, bytes) {
    const observation = client.memory;
    if (!observation) return;
    client.memory = undefined;
    clearTimeout(observation.timeout);
    observation.request = undefined;
    observation.simulation = undefined;
    try { client.releaseMemory(); }
    catch (releaseError) { error ??= releaseError; }
    if (error) observation.reject(error);
    else observation.resolve(bytes);
}
export function observeMemory(id) {
    const client = owner(id);
    if (client.failure) return Promise.reject(client.failure);
    if (client.memory || client.pending.size || client.acknowledgements.size || client.eventBytes ||
        client.state !== client.states.ready)
        return Promise.reject(new Error('Memory observation requires an idle Ready transport.'));
    const started = now();
    return new Promise((resolve, reject) => {
        const observation = { resolve, reject, started, request: undefined, simulation: undefined, timeout: undefined };
        client.memory = observation;
        observation.timeout = setTimeout(() => {
            if (client.memory === observation)
                fail(client, new Error('Stopped memory observation exceeded 1000ms; reload to recover.'));
        }, 1000);
        try {
            client.beginMemory();
            if (!observation.request) throw new Error('Memory observation did not produce its owned request.');
        } catch (error) { settleMemory(client, error); }
    });
}
export function sendMemoryRequest(id, bytes) {
    const client = owner(id), observation = client.memory;
    if (!observation || observation.request || !(bytes instanceof Uint8Array) || bytes.length !== 64)
        throw new Error('No unique stopped memory request is owned.');
    observation.request = new Uint8Array(bytes);
    const memoryRequest = new Uint8Array(bytes);
    client.worker.postMessage({ memoryRequest }, [memoryRequest.buffer]);
}
function receiveMemoryResult(client, bytes, received) {
    const observation = client.memory;
    if (!observation?.request || observation.simulation || bytes.length !== 128)
        throw new Error('Unexpected, malformed or duplicate stopped memory response.');
    if (received - observation.started > 1000) {
        console.error('CCGPU_MEMORY_LATE', Array.from(bytes).join(','));
        throw new Error('Stopped memory response exceeded 1000ms; reload to recover.');
    }
    observation.simulation = bytes;
    deliver(client, bytes, received, client.receiveMemory);
    if (client.memory === observation)
        settleMemory(client, new Error('Worker rejected the stopped collector observation.'));
}
export function completeMemoryObservation(id, bytes) {
    const client = owner(id), observation = client.memory;
    if (!observation?.simulation || client.eventBytes !== observation.simulation ||
        !(bytes instanceof Uint8Array) || bytes.length !== 128)
        throw new Error('Stopped browser result does not own the response lease.');
    const combined = new Uint8Array(256);
    combined.set(bytes);
    combined.set(observation.simulation, 128);
    const elapsed = now() - observation.started;
    if (elapsed > 1000) {
        console.error('CCGPU_MEMORY_LATE', Array.from(combined).join(','));
        throw new Error('Stopped collection exceeded 1000ms; reload to recover.');
    }
    if (new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getUint32(12, true) !== 1)
        settleMemory(client, new Error('Browser collector observation is unsupported.'));
    else settleMemory(client, undefined, combined);
}
export function send(id, bytes) {
    const client = owner(id);
    if (client.failure) return Promise.reject(client.failure);
    if (client.memory) return Promise.reject(new Error('A stopped memory observation owns the transport.'));
    const key = sequence(bytes);
    if (client.pending.size + client.acknowledgements.size >= 2 || client.pending.has(key) || client.acknowledgements.has(key))
        return Promise.reject(new Error('Workshop transport capacity or identity violation.'));
    return new Promise((resolve, reject) => {
        client.pending.set(key, { resolve, reject, started: now() });
        const owned = new Uint8Array(bytes);
        client.worker.postMessage({ bytes: owned }, [owned.buffer]);
    });
}
export function acknowledgement(id, identity) {
    const client = owner(id), key = sequence(identity);
    const bytes = client.acknowledgements.get(key);
    if (!bytes) throw new Error('The command acknowledgement is not available.');
    client.acknowledgements.delete(key);
    return bytes;
}
export function dispose(id) {
    const client = clients.get(id);
    if (!client) return;
    fail(client, new Error('Worker session retired; unacknowledged operations are Indeterminate.'), true);
    clearTimeout(client.retirement);
    client.state = client.states.recoveryBlocked;
    clients.delete(id);
}
