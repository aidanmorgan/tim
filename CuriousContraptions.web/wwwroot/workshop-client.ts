import { PoseReader, createPoseRing } from './pose-ring.js';
import type { ClockRing } from '../../tools/runtime/worker-contracts.js';
interface Client {
    id: number; worker: Worker; animation?: Worker; animationPort?: MessagePort;
    pending: Map<bigint, { resolve(): void; reject(error: unknown): void; started: number }>;
    acknowledgements: Map<bigint, { bytes: Uint8Array; started: number; taken: boolean }>;
    failure: unknown; states: { ready: number; backpressure: number; timedOut: number; indeterminate: number; recoveryBlocked: number }; state: number;
    serviceCallback(): void; clockReplyCallback(): void;
    observationCallback(): void; retiredCallback(detail: string): void; pendingObservations: Uint8Array[];
    service?: ReturnType<typeof setInterval>; startup?: ReturnType<typeof setTimeout>; retirement?: ReturnType<typeof setTimeout>;
    clockRing?: SharedArrayBuffer; clock?: ClockRing; identity?: Uint8Array;
    clockAbi: { clockProfile: number; peerBytes: number; probeBytes: number; replyBytes: number; browserRole: number; masterRole: number };
    captureMode: number; events?: EventRingCursor;
    poses?: PoseReader;
    eventObservers: Map<number, EventRingCursor>; nextEventObserver: number;
    displayRate?: [number, number]; unwatchIsolation?: () => void;
    bootstrapReject?: ((error: unknown) => void) | null; animationReady?: boolean; animationQualified?: boolean;
    hintKinds: number[]; hintPending?: boolean;
    workerProfile?: Float64Array; liveness?: { beats: number; since: number; serviced: number };
    eventBytes?: Uint8Array; eventReceived?: number;
}
function envelope(value: unknown): Record<string, unknown> {
    if (value === null || typeof value !== 'object') throw new Error('Worker envelope must be an object.');
    return value as Record<string, unknown>;
}

import { admitNativeClock, nativeNow, nativeProfile } from './native-clock.js';
import { requireIsolation, watchIsolation, isolationQualified, stopIsolation } from './workshop-isolation.js';
import { createClockRing, attachClockRing, requestReply, heartbeat, ClockRole } from './clock-ring.js';
import { EventRingCursor, eventDelivery } from './event-ring-cursor.js';

// The Animation output record fields this client reads, owned by WorkshopAnimationWire (WorkerAbiTests pins each one).
const ANIMATION_OUTPUT_BYTES = 144;            // WorkshopAnimationWire.OutputBytes
const ANIMATION_OUTPUT_KIND_OFFSET = 60;       // WorkshopAnimationWire.OutputKindOffset

const clients = new Map<number, Client>();
let nextClient = 1, retiredSession = false, creating = false;
function owner(id: number) {
    const client = clients.get(id);
    if (!client) throw new Error('Workshop worker client is disposed.');
    return client;
}
// A command's receipt key: the 8-byte little-endian sequence C# hands over with the command and the worker returns with its
// acknowledgement (Story 9A.2: no script parses a FlatBuffers buffer).
function sequence(bytes: unknown) {
    if (!(bytes instanceof Uint8Array) || bytes.length !== 8) throw new Error('Invalid command identity.');
    return new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getBigUint64(0, true);
}
// The age of the oldest command still pending or unconsumed: the player's Backpressure and TimedOut state only. The browser posts
// nothing about it; the Simulation worker detects a reliable-read stall in its own receipt accounting (Story 9A.3).
function oldestCommandAge(client: Client, current: number) {
    let oldest = 0;
    for (const pending of client.pending.values()) oldest = Math.max(oldest, current - pending.started);
    for (const receipt of client.acknowledgements.values()) oldest = Math.max(oldest, current - receipt.started);
    return oldest;
}
function failureDetail(error: unknown, seen = new Set<unknown>()): string {
    if (seen.has(error)) return '[circular cause]';
    if (!(error instanceof Error)) return String(error);
    seen.add(error);
    const heading = error.name + ': ' + error.message;
    const detail = error.stack ? heading + '\\n' + error.stack : heading;
    return error.cause === undefined ? detail : detail + '\\nCaused by: ' + failureDetail(error.cause, seen);
}
function fail(client: Client, error: unknown, retiring = false) {
    if (client.failure) return;
    client.failure = error;
    client.state = client.states.indeterminate;
    if (!retiring) client.retiredCallback(failureDetail(error));
    retiredSession = true;
    client.unwatchIsolation?.(); client.unwatchIsolation = undefined;
    clearInterval(client.service);
    clearTimeout(client.startup);
    client.worker.onmessage = null;
    client.worker.onerror = null;
    client.worker.terminate();
    if (client.animation) { client.animation.onmessage = client.animation.onerror = null; client.animation.terminate(); }
    client.animationPort?.close(); client.animationPort = undefined;
    // The session clock ring is detached with its workers (dispose goes through here): nothing probes or reads it again.
    client.clock = client.clockRing = undefined;
    client.pendingObservations.length = 0;
    client.events?.close(); client.events = undefined;
    client.eventObservers.clear();
    client.poses?.dispose(); client.poses = undefined;
    for (const pending of client.pending.values()) pending.reject(error);
    client.pending.clear();
    client.bootstrapReject?.(error);
    client.bootstrapReject = null;
    if (!retiring) stopIsolation(error);
    client.retirement = setTimeout(() => { client.state = client.states.recoveryBlocked; }, 1000);
}
function deliver(client: Client, bytes: Uint8Array, received: number, callback: () => void) {
    if (client.eventBytes) throw new Error('Reentrant worker delivery is unsupported.');
    client.eventBytes = bytes; client.eventReceived = received;
    try { callback(); }
    finally { client.eventBytes = undefined; client.eventReceived = undefined; }
}
async function admitDisplayRate() {
    return await new Promise<[number, number]>((resolve, reject) => {
        const intervals: number[] = [];
        let previous: number | undefined, frame = 0;
        const timeout = setTimeout(() => { cancelAnimationFrame(frame); reject(new Error('Display cadence admission timed out.')); }, 2000);
        const sample = (timestamp: number) => {
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
export function displayRate(id: number) { return owner(id).displayRate; }
export async function create(states: number[], bootstrapBytes: Uint8Array, clockAbi: number[], captureMode: number, admitDisplay: boolean, clockReply: () => void, physicalRead: () => void, scheduleControl: () => void, animationOutput: () => void, service: () => void, observation: () => void, retired: (detail: string) => void) {
    if (clients.size !== 0 || retiredSession || creating) throw new Error('RecoveryBlocked: reload the page before creating another worker.');
    creating = true;
    let id: number | undefined;
    try {
        if (captureMode !== 1 && captureMode !== 2) throw new Error('Unknown Workshop capture mode.');
        requireIsolation();
        await admitNativeClock();
        requireIsolation();
        if (typeof admitDisplay !== 'boolean') throw new Error('Invalid display admission selection.');
        const displayRate = admitDisplay ? await admitDisplayRate() : undefined;
        const session = new Uint8Array(bootstrapBytes);
        if (session.length !== 16 || !session.some(value => value !== 0) || clockAbi.length !== 6)
            throw new Error('Unsupported session bootstrap.');
        const [clockProfile, peerBytes, probeBytes, replyBytes, browserRole, masterRole] = clockAbi;
        if (clockProfile !== nativeProfile) throw new Error('Unsupported native clock profile.');
        const [ready, backpressure, timedOut, indeterminate, recoveryBlocked] = states;
        id = nextClient++;
        // Cross-origin isolation is required before any client exists (requireIsolation), so the shared rings always exist.
        const poseRing = createPoseRing();
        const clockRing = createClockRing();
        const worker = new Worker(new URL('./simulation/worker.js', document.baseURI), { type: 'module' });
        const client: Client = { id, hintKinds: [], displayRate, worker, pending: new Map(), acknowledgements: new Map(),
            failure: null, states: { ready, backpressure, timedOut, indeterminate, recoveryBlocked },
            serviceCallback: service, service: undefined, clockReplyCallback: clockReply,
            observationCallback: observation, retiredCallback: retired, pendingObservations: [],
            clockRing, clock: attachClockRing(clockRing),
            state: ready,
            identity: undefined, clockAbi: { clockProfile, peerBytes, probeBytes, replyBytes, browserRole, masterRole }, captureMode,
            events: new EventRingCursor(), eventObservers: new Map(), nextEventObserver: 0 };
        client.poses = new PoseReader(poseRing);
        client.worker = worker;
        clients.set(id, client);
        client.unwatchIsolation = watchIsolation(error => fail(client, error));
        await new Promise<void>((resolve, reject) => {
            client.bootstrapReject = reject;
            let readyReceived = false, simulationAwaiting = false, animationAwaiting = false;
            let bootstrapStarted = false, animationBootstrapped = false;
            const animation = new Worker(new URL('./animation/worker.js', document.baseURI), { type: 'module' });
            client.animation = animation;
            function beginBootstrap() {
                if (client.failure || !clients.has(client.id) || bootstrapStarted || !simulationAwaiting || !animationAwaiting) return;
                bootstrapStarted = true;
                // Download/runtime loading precedes qualification; the same bounded handshake starts once both can participate.
                client.startup = setTimeout(() => fail(client, new Error('StartupFailed: usable GPU and clock qualification exceeded five seconds.')), 5000);
                const bootstrap = new Uint8Array(bootstrapBytes);
                const postData = { bootstrap, captureMode, clockRing: client.clockRing, poseRing };
                worker.postMessage(postData, [bootstrap.buffer]);
            }
            animation.onerror = error => fail(client, new Error(error.message || 'Animation worker failed.'));
            animation.onmessage = incoming => {
                if (client.failure || !clients.has(client.id)) return;
                try {
                    const item = envelope(incoming.data);
                    if (item.awaitingBootstrap === true) {
                        if (animationAwaiting) throw new Error('Duplicate Animation runtime readiness.');
                        animationAwaiting = true;
                        beginBootstrap();
                    } else if (item.ready === true) {
                        if (!animationBootstrapped || client.animationReady || !(item.nativeClock instanceof Float64Array) ||
                            item.nativeClock.length !== 7 || item.nativeClock[0] !== nativeProfile ||
                            item.nativeClock[1] !== 1 || item.nativeClock[2] !== 1)
                            throw new Error('Invalid Animation native-clock admission.');
                        client.animationReady = true;
                    } else if (item.qualified === true) {
                        if (!client.animationReady || client.animationQualified) throw new Error('Duplicate Animation qualification.');
                        client.animationQualified = true;
                    } else if (item.observation instanceof Uint8Array) {
                        receiveObservation(client, item.observation);
                    } else if (item.animationOutput instanceof Uint8Array) {
                        if (!animationBootstrapped || !client.animationReady) throw new Error('Animation output precedes admission.');
                        if (item.animationOutput.length !== ANIMATION_OUTPUT_BYTES) throw new Error('Wrong Animation output length.');
                        deliver(client, item.animationOutput, now(), animationOutput);
                        const output = new DataView(item.animationOutput.buffer, item.animationOutput.byteOffset, ANIMATION_OUTPUT_BYTES);
                        const kind = output.getUint32(ANIMATION_OUTPUT_KIND_OFFSET, true);
                        if (kind === client.hintKinds[1]) {
                            const animationAcknowledged = item.animationOutput.slice();
                            animation.postMessage({ animationAcknowledged }, [animationAcknowledged.buffer]);
                        } else if (kind === client.hintKinds[0] || kind === client.hintKinds[2]) {
                            if (!client.hintPending) throw new Error('Unsolicited hint acknowledgement.');
                            client.hintPending = false;
                        } else throw new Error('Unknown Animation output.');
                    } else throw new Error(typeof item.detail === 'string' ? item.detail : 'Unknown Animation envelope.');
                } catch (error) { fail(client, error); }
            };
            worker.onmessage = event => {
                if (client.failure || !clients.has(client.id)) return;
                try {
                    const received = now(), message = envelope(event.data);
                    if (message.awaitingBootstrap === true) {
                        if (simulationAwaiting) throw new Error('Duplicate Simulation runtime readiness.');
                        simulationAwaiting = true;
                        beginBootstrap();
                    } else if (message.ready === true) {
                        if (!bootstrapStarted || readyReceived) throw new Error('Invalid worker bootstrap.');
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
                        if (declared.getUint32(0, true) !== 0 || declared.getUint32(4, true) !== clockProfile || // bytes 0..3: zero padding
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
                        readyReceived = true;
                        client.bootstrapReject = null;
                        resolve();
                    } else if (message.animationPeer instanceof Uint8Array) {
                        if (!readyReceived || animationBootstrapped || !animationAwaiting || !client.animationPort)
                            throw new Error('Invalid Animation ownership.');
                        const bootstrap = message.animationPeer;
                        const masterPort = client.animationPort;
                        client.animationPort = undefined;
                        animationBootstrapped = true;
                        animation.postMessage({ bootstrap, masterPort, clockRing: client.clockRing }, [bootstrap.buffer, masterPort]);
                    } else if (message.poseRing instanceof SharedArrayBuffer) {
                        // The worker committed the logical population within reserved storage for the newly admitted epoch.
                        if (!readyReceived) throw new Error('A pose ring arrived before the session was ready.');
                        const reader = new PoseReader(message.poseRing);
                        client.poses?.dispose(); client.poses = reader;
                    } else if (message.observation instanceof Uint8Array) {
                        receiveObservation(client, message.observation);
                    } else if (message.eventRing instanceof SharedArrayBuffer) {
                        // Story 9A.23: the session's event ring for an admitted epoch, read by the main thread's own cursor.
                        if (!readyReceived) throw new Error('An event ring arrived before the session was ready.');
                        client.events!.attach(message.eventRing);
                    } else if (message.scheduleControl instanceof Uint8Array) {
                        deliver(client, message.scheduleControl, received, scheduleControl);
                    } else if (message.acknowledgement instanceof Uint8Array) {
                        const key = sequence(message.sequence);
                        const pending = client.pending.get(key);
                        if (!pending) throw new Error('Unexpected worker acknowledgement.');
                        client.pending.delete(key);
                        client.acknowledgements.set(key, { bytes: message.acknowledgement, started: pending.started, taken: false });
                        pending.resolve();
                    } else if (message.read instanceof Uint8Array) {
                        // Complete C# validation/admission precedes return; JS retains no pose history.
                        deliver(client, message.read, received, physicalRead);
                        // The receipt is the identity C# published with the read (its session and publication), echoed unread.
                        if (!(message.readIdentity instanceof Uint8Array) || message.readIdentity.length !== 24)
                            throw new Error('A physical read arrived without its receipt identity.');
                        worker.postMessage({ readAcknowledged: message.readIdentity }, [message.readIdentity.buffer]);
                    } else if (message.failure || message.rejected) {
                        throw new Error(typeof message.detail === 'string' ? message.detail : 'Worker rejected its transport envelope.');
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
export function animationQualified(id: number) { return owner(id).animationQualified === true; }
export function animationKinds(id: number, kinds: number[]) {
    if (kinds.length !== 3 || new Set(kinds).size !== 3) throw new Error('Invalid Animation output ABI.');
    owner(id).hintKinds = Array.from(kinds);
}
export function scheduleResult(id: number, bytes: Uint8Array) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    const scheduleControl = new Uint8Array(bytes);
    client.worker.postMessage({ scheduleControl }, [scheduleControl.buffer]);
}
export function animationControl(id: number, bytes: Uint8Array) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    if (!client.animationQualified || client.hintPending) throw new Error('Animation control is unavailable or pending.');
    const animationControl = new Uint8Array(bytes);
    if (animationControl.length !== 144) throw new Error('Invalid animation command.');
    client.hintPending = true;
    client.animation!.postMessage({ animationControl }, [animationControl.buffer]);
}
export function bootstrap(id: number) {
    const client = owner(id);
    if (!client.identity) throw new Error('Master bootstrap is not available.');
    return client.identity;
}
// A Simulation whose own event loop has not run for this long is hung, not lapsed: its solver spins or its context died.
// The dedicated clock master keeps answering in that case, so only the Simulation's ring heartbeat (advanced between its
// tick turns, worker.js) proves it is alive. The window restarts whenever this service itself was not running (a
// blocked or throttled Browser), so only the Simulation's silence can expire it.
const SimulationLivenessMilliseconds = 1000, BrowserServiceGapMilliseconds = 250;
function checkSimulationLiveness(client: Client, current: number) {
    const beats = heartbeat(client.clock!);
    if (beats !== client.liveness!.beats || current - client.liveness!.serviced > BrowserServiceGapMilliseconds) {
        client.liveness!.beats = beats; client.liveness!.since = current;
    }
    client.liveness!.serviced = current;
    if (!document.hidden && current - client.liveness!.since > SimulationLivenessMilliseconds) {
        const error = new Error(`The Simulation worker's event loop has not run for ${SimulationLivenessMilliseconds} ms.`);
        error.name = 'SimulationUnresponsive';
        throw error;
    }
}
function receiveObservation(client: Client, bytes: Uint8Array) {
    if (client.service === undefined) { client.pendingObservations.push(bytes); return; }
    try { deliver(client, bytes, now(), client.observationCallback); }
    catch (error) { client.retiredCallback('Observation unavailable: ' + String(error)); }
}
export function activate(id: number) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    if (!client.workerProfile || client.service !== undefined)
        throw new Error('Workshop service activation requires one ready owner.');
    const started = now();
    client.liveness = { beats: heartbeat(client.clock!), since: started, serviced: started };
    client.service = setInterval(() => {
        if (client.failure) return;
        try {
            const current = now();
            // A missing clock reply never faults the session: the C# mapping recalibrates on its lifetime and recovery
            // bounds as counted telemetry (Story 9.4b, game-grade envelope).
            client.serviceCallback();
            checkSimulationLiveness(client, current);
            const oldest = oldestCommandAge(client, current);
            client.state = oldest > 2000 ? client.states.timedOut
                : oldest > 50 ? client.states.backpressure : client.states.ready;
        } catch (error) { fail(client, error); }
    }, 10);
    for (const bytes of client.pendingObservations) receiveObservation(client, bytes);
    client.pendingObservations.length = 0;
}
export function eventBytes(id: number) {
    const bytes = owner(id).eventBytes;
    if (!bytes) throw new Error('No synchronous worker delivery is active.');
    return bytes;
}
export function eventMilliseconds(id: number) {
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
export function probe(id: number, bytes: Uint8Array) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    // The Simulation-owned clock master answers through the clock ring, never the Simulation's busy event loop.
    // The reply is received when the wait resolves and is delivered under the same clock lease as before.
    requestReply(client.clock!, ClockRole.Browser, new Uint8Array(bytes)).then(reply => {
        if (!reply || client.failure || !clients.has(client.id)) return;
        try { deliver(client, reply, now(), client.clockReplyCallback); }
        catch (error) { fail(client, error); }
    }, error => {
        if (client.failure || !clients.has(client.id)) return;
        fail(client, error);
    });
}
export function qualified(id: number) {
    const client = owner(id);
    isolationQualified();
    clearTimeout(client.startup);
}
export function status(id: number) { return owner(id).state; }
export function captureMode(id: number) { return owner(id).captureMode; }

export function send(id: number, bytes: Uint8Array, sequenceBytes: Uint8Array) {
    const client = owner(id);
    if (client.failure) return Promise.reject(client.failure);
    const key = sequence(new Uint8Array(sequenceBytes));
    if (client.pending.size + client.acknowledgements.size >= 2 || client.pending.has(key) || client.acknowledgements.has(key))
        return Promise.reject(new Error('Workshop transport capacity or identity violation.'));
    return new Promise<void>((resolve, reject) => {
        client.pending.set(key, { resolve, reject, started: now() });
        const owned = new Uint8Array(bytes);
        client.worker.postMessage({ bytes: owned }, [owned.buffer]);
    });
}
export function acknowledgement(id: number, identity: Uint8Array) {
    const client = owner(id), key = sequence(identity);
    if (client.failure) throw client.failure;
    const receipt = client.acknowledgements.get(key);
    if (!receipt || receipt.taken) throw new Error('The command acknowledgement is not available.');
    receipt.taken = true;
    return receipt.bytes;
}
export function completeAcknowledgement(id: number, identity: Uint8Array) {
    const client = owner(id), key = sequence(identity);
    if (client.failure) throw client.failure;
    const receipt = client.acknowledgements.get(key);
    if (!receipt?.taken) throw new Error('The command acknowledgement is not leased.');
    // C# has validated the complete response and its command/session identity.
    client.acknowledgements.delete(key);
}
export function dispose(id: number) {
    const client = clients.get(id);
    if (!client) return;
    fail(client, new Error('Worker session retired; unacknowledged operations are Indeterminate.'), true);
    clearTimeout(client.retirement);
    client.state = client.states.recoveryBlocked;
    clients.delete(id);
}


// One construction slot. Completion is the transaction boundary, not request success.
const constructionDatabase = 'curious-contraptions-workshop';
const constructionStore = 'construction';
const constructionSlot = 1;
function openConstructionStorage() {
    return new Promise<IDBDatabase>((resolve, reject) => {
        const request = indexedDB.open(constructionDatabase, 1);
        let refused = false;
        request.onupgradeneeded = () => request.result.createObjectStore(constructionStore);
        request.onblocked = () => { refused = true; reject(new Error('Construction storage is blocked.')); };
        request.onerror = () => reject(request.error ?? new Error('Construction storage is unavailable.'));
        request.onsuccess = () => { if (refused) request.result.close(); else resolve(request.result); };
    });
}
export async function saveConstruction(bytes: Uint8Array) {
    const value = new Uint8Array(bytes);
    const database = await openConstructionStorage();
    try {
        await new Promise<void>((resolve, reject) => {
            const transaction = database.transaction(constructionStore, 'readwrite');
            transaction.objectStore(constructionStore).put(value, constructionSlot);
            transaction.oncomplete = () => resolve();
            transaction.onabort = () => reject(transaction.error ?? new Error('Construction was not stored.'));
        });
    } finally { database.close(); }
}
export async function loadConstruction(expectedBytes: number) {
    const database = await openConstructionStorage();
    try {
        return await new Promise<string>((resolve, reject) => {
            const transaction = database.transaction(constructionStore, 'readonly');
            const request = transaction.objectStore(constructionStore).get(constructionSlot);
            transaction.onabort = () => reject(transaction.error ?? new Error('Construction could not be loaded.'));
            transaction.oncomplete = () => {
                const bytes: unknown = request.result;
                if (bytes === undefined) { resolve(''); return; }
                if (!(bytes instanceof Uint8Array) || (expectedBytes > 0 && bytes.length !== expectedBytes)) {
                    reject(new Error('Unsupported construction save.')); return;
                }
                resolve(btoa(Array.from(bytes, value => String.fromCharCode(value)).join('')));
            };
        });
    } finally { database.close(); }
}


// Story 9A.23 D8: the main thread's event ring read, for the epoch C# has installed (the reader holds a newer ring and ignores an
// older one). C# decodes the delivery (EventRingDelivery); this script reads no record field.
export function readEvents(id: number, epoch: string) {
    const events = owner(id).events;
    if (!events) throw new Error('The event ring reader is retired.');
    return eventDelivery(events.read(BigInt(epoch)));
}


export function openObservationEvents(id: number) {
    const client = owner(id);
    if (!client.events) throw new Error('The event reader is retired.');
    const cursor = ++client.nextEventObserver;
    client.eventObservers.set(cursor, client.events.fork());
    return cursor;
}
export function readObservationEvents(id: number, cursor: number, epoch: string) {
    const observer = owner(id).eventObservers.get(cursor);
    if (!observer) throw new Error('The observation cursor is closed.');
    return eventDelivery(observer.read(BigInt(epoch)));
}
export function closeObservationEvents(id: number, cursor: number) {
    const client = clients.get(id);
    if (!client) return;
    const observer = client.eventObservers.get(cursor);
    if (!observer) return;
    client.events?.release(observer);
    client.eventObservers.delete(cursor);
}
