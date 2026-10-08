const clients = new Map();
let nextClient = 1, retiredSession = false;
const empty = new Uint8Array();
function owner(id) {
    const client = clients.get(id);
    if (!client) throw new Error('Workshop worker client is disposed.');
    return client;
}
function sequence(bytes) {
    if (!(bytes instanceof Uint8Array) || bytes.length < 8) throw new Error('Invalid command identity.');
    return new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getBigUint64(0, true);
}
function fail(client, error) {
    if (client.failure) return;
    client.failure = error;
    client.state = client.states.indeterminate;
    retiredSession = true;
    clearInterval(client.service);
    clearTimeout(client.startup);
    client.worker.onmessage = null;
    client.worker.onerror = null;
    client.worker.terminate();
    for (const pending of client.pending.values()) pending.reject(error);
    client.pending.clear();
    client.bootstrapReject?.(error);
    client.bootstrapReject = null;
    // Terminate has no browser-owned reclamation receipt. Never allocate a replacement pool.
    client.retirement = setTimeout(() => { client.state = client.states.recoveryBlocked; }, 1000);
}
function probe(client) {
    if (client.probe === Number.MAX_SAFE_INTEGER) { fail(client, new Error('Clock identity exhausted.')); return; }
    const identity = ++client.probe;
    client.probes.set(identity, performance.now());
    while (client.probes.size > 4) client.probes.delete(client.probes.keys().next().value);
    client.worker.postMessage({ clockProbe: identity });
}
export async function create(states) {
    if (clients.size !== 0 || retiredSession) throw new Error('RecoveryBlocked: reload the page before creating another worker.');
    const [ready, backpressure, timedOut, indeterminate, recoveryBlocked] = states;
    const id = nextClient++;
    const worker = new Worker('./simulation/worker.js', { type: 'module' });
    const client = { worker, pending: new Map(), acknowledgements: new Map(), latest: empty,
        failure: null, states: { ready, backpressure, timedOut, indeterminate, recoveryBlocked },
        state: ready, probes: new Map(), probe: 0, lastReply: performance.now(), lastProbe: 0, stalled: false, candidateTimedOut: false };
    clients.set(id, client);
    try {
        await new Promise((resolve, reject) => {
            client.bootstrapReject = reject;
            client.startup = setTimeout(() => {
                const error = new Error('StartupFailed: usable GPU qualification exceeded five seconds.');
                fail(client, error); reject(error);
            }, 5000);
            let bootstrap = true;
            worker.onmessage = event => {
                const message = event.data;
                if (message.ready === true) { probe(client); return; }
                if (Number.isSafeInteger(message.clockReply)) {
                    const sent = client.probes.get(message.clockReply);
                    if (sent === undefined || !Number.isFinite(message.received) || !Number.isFinite(message.sent) ||
                        message.received < 0 || message.sent < message.received) return;
                    client.probes.delete(message.clockReply);
                    client.lastReply = performance.now();
                    if (bootstrap) { bootstrap = false; client.bootstrapReject = null; resolve(); }
                    return;
                }
                if (typeof message.candidateTimedOut === 'boolean') {
                    client.candidateTimedOut = message.candidateTimedOut;
                    return;
                }
                if (message.acknowledgement instanceof Uint8Array) {
                    const key = sequence(message.acknowledgement);
                    const pending = client.pending.get(key);
                    if (!pending) { fail(client, new Error('Unexpected worker acknowledgement.')); return; }
                    client.pending.delete(key);
                    client.acknowledgements.set(key, message.acknowledgement);
                    pending.resolve();
                } else if (message.read instanceof Uint8Array) {
                    client.latest = message.read;
                    const receipt = message.read.slice(24, 40);
                    worker.postMessage({ readAcknowledged: receipt }, [receipt.buffer]);
                } else if (message.failure || message.rejected) {
                    fail(client, new Error(message.detail ?? 'Worker rejected its transport envelope.'));
                } else fail(client, new Error('Unknown worker transport envelope.'));
            };
            worker.onerror = event => { const error = new Error(event.message); fail(client, error); reject(error); };
            client.service = setInterval(() => {
                if (client.failure) return;
                const now = performance.now();
                if (!document.hidden) {
                    if (now - client.lastReply > 1000) { fail(client, new Error('Worker fault: matching clock reply missing for 1000ms.')); return; }
                    if (now - client.lastProbe >= 250) { client.lastProbe = now; probe(client); }
                }
                let oldest = 0;
                for (const pending of client.pending.values()) oldest = Math.max(oldest, now - pending.started);
                client.state = oldest > 2000 || client.candidateTimedOut ? timedOut : oldest > 50 ? backpressure : ready;
                if (oldest > 500 && !client.stalled) { client.stalled = true; worker.postMessage({ reliableStalled: true }); }
                if (oldest === 0) client.stalled = false;
            }, 25);
        });
        return id;
    } catch (error) { dispose(id); throw error; }
}
export function qualified(id) { clearTimeout(owner(id).startup); }
export function status(id) { return owner(id).state; }
export function send(id, bytes) {
    const client = owner(id);
    if (client.failure) return Promise.reject(client.failure);
    const key = sequence(bytes);
    if (client.pending.size + client.acknowledgements.size >= 2 || client.pending.has(key) || client.acknowledgements.has(key))
        return Promise.reject(new Error('Workshop transport capacity or identity violation.'));
    return new Promise((resolve, reject) => {
        client.pending.set(key, { resolve, reject, started: performance.now() });
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
export function read(id) {
    const client = owner(id);
    if (client.failure) throw client.failure;
    const result = client.latest;
    client.latest = empty;
    return result;
}
export function dispose(id) {
    const client = clients.get(id);
    if (!client) return;
    fail(client, new Error('Worker session retired; unacknowledged operations are Indeterminate.'));
    clearTimeout(client.retirement);
    client.state = client.states.recoveryBlocked;
    clients.delete(id);
}
