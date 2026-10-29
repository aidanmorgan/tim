import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';
import { setImmediate } from 'node:timers/promises';
import * as poseRing from '../../CuriousContraptions.web/wwwroot/pose-ring.js';
import * as clockRing from '../../CuriousContraptions.web/wwwroot/clock-ring.js';
import * as eventRing from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
type Api = typeof import('../../CuriousContraptions.web/wwwroot/workshop-client.js');
class StorageRequest extends EventTarget {
    onsuccess: ((event: Event) => void) | null = null;
    constructor(readonly result?: Uint8Array) { super(); }
    succeed() { const event = new Event('success'); this.dispatchEvent(event); this.onsuccess?.(event); }
}
interface Transaction { readonly request: StorageRequest; oncomplete?: () => void; onabort?: () => void; error?: Error; requestSuccess(): void; commit(): void; abort(): void; }
async function harness(initial?: Uint8Array) {
    let durable = initial, current!: Transaction, closed = 0;
    let opened!: () => void;
    let transactionOpened = new Promise<void>(resolve => { opened = resolve; });
    const database = {
        close() { closed++; },
        transaction(_store: string, mode: string) {
            let pending: Uint8Array | undefined, request: StorageRequest | undefined;
            const transaction = {
                oncomplete: undefined as (() => void) | undefined, onabort: undefined as (() => void) | undefined, error: undefined as Error | undefined,
                objectStore() { return {
                    put(value: Uint8Array) { pending = new Uint8Array(value); request = new StorageRequest(); return request; },
                    get() { request = new StorageRequest(durable === undefined ? undefined : new Uint8Array(durable)); return request; }
                }; },
                get request() { assert.ok(request); return request; },
                requestSuccess() { this.request.succeed(); },
                commit() { if (mode === 'readwrite') durable = pending; this.oncomplete?.(); },
                abort() { this.error = new Error('controlled transaction abort'); this.onabort?.(); }
            };
            current = transaction; opened(); return transaction;
        }
    };
    const context = vm.createContext({
        Uint8Array, Map, Set, Array, Promise, Error, btoa: (value: string) => Buffer.from(value, 'binary').toString('base64'),
        indexedDB: { open() { const request = { result: database, onsuccess: undefined as (() => void) | undefined }; queueMicrotask(() => request.onsuccess?.()); return request; } }
    });
    const module = new vm.SourceTextModule(readFileSync('CuriousContraptions.web/wwwroot/workshop-client.js', 'utf8'), { context });
    await module.link(name => {
        const values: Record<string, unknown> = name.includes('pose-ring') ? poseRing : name.includes('clock-ring') ? clockRing : name.includes('event-ring') ? eventRing
            : name.includes('native-clock') ? { admitNativeClock: async () => {}, nativeNow: () => 0, nativeProfile: 1, nativeClockEvidence: () => [] }
            : { requireIsolation: () => {}, watchIsolation: () => () => {}, isolationQualified: () => {}, isolationEvidence: () => ({}), stopIsolation: () => {} };
        return new vm.SyntheticModule(Object.keys(values), function () { for (const [key, value] of Object.entries(values)) this.setExport(key, value); }, { context });
    });
    await module.evaluate();
    return { api: module.namespace as unknown as Api, get current() { return current; }, get durable() { return durable; }, get closed() { return closed; },
        async opened() { await transactionOpened; transactionOpened = new Promise<void>(resolve => { opened = resolve; }); } };
}
// The production save codec belongs to C#. This opaque-storage fixture gives its consumer
// meaningful fields without pretending to encode the game's save format. Reserved bytes
// are intentionally unobserved: storage tests compare the decoded values, not byte identity.
interface StoredFixture { revision: number; massKg: number; bodyId: bigint; }
const FixtureBytes = 24;
function fixture(value: StoredFixture): Uint8Array {
    const bytes = new Uint8Array(FixtureBytes), data = new DataView(bytes.buffer);
    data.setUint32(0, value.revision, true);
    data.setFloat32(4, value.massKg, true);
    data.setBigUint64(8, value.bodyId, true);
    return bytes;
}
function decodeFixture(bytes: Uint8Array | undefined): StoredFixture {
    assert.ok(bytes);
    const data = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    return { revision: data.getUint32(0, true), massKg: data.getFloat32(4, true), bodyId: data.getBigUint64(8, true) };
}
test('request success cannot complete Save; transaction abort preserves prior values', async () => {
    const previous = { revision: 7, massKg: 2.5, bodyId: 4294967297n };
    const h = await harness(fixture(previous));
    let settled = false, requestSucceeded = false;
    const pending = h.api.saveConstruction(fixture({ revision: 8, massKg: 4, bodyId: 81n }));
    pending.then(() => { settled = true; }, () => { settled = true; });
    await h.opened();
    const putRequest = h.current.request;
    putRequest.addEventListener('success', event => { requestSucceeded = event.target === putRequest; });
    h.current.requestSuccess();
    // A request-success handler may resolve the client's nested promises. Drain their
    // microtasks before the next event-loop turn, without using a wall-clock delay.
    await setImmediate();
    assert.equal(requestSucceeded, true, 'the put request dispatched its success event');
    assert.equal(settled, false, 'request success does not commit the transaction');
    assert.deepEqual(decodeFixture(h.durable), previous);
    h.current.abort(); await assert.rejects(pending, /transaction abort/);
    assert.deepEqual(decodeFixture(h.durable), previous); assert.equal(h.closed, 1);
});
test('committed Save owns caller input and Load returns independent values', async () => {
    const saved = { revision: 42, massKg: 0.25, bodyId: 18446744073709551615n };
    const h = await harness(), input = fixture(saved);
    const pending = h.api.saveConstruction(input); input.fill(4);
    await h.opened(); h.current.commit(); await pending;
    assert.deepEqual(decodeFixture(h.durable), saved);
    const loaded = h.api.loadConstruction(FixtureBytes); await h.opened(); h.current.commit();
    const result = Uint8Array.from(Buffer.from(await loaded, 'base64'));
    assert.deepEqual(decodeFixture(result), saved);
    result.fill(5);
    assert.deepEqual(decodeFixture(h.durable), saved); assert.equal(h.closed, 2);
});
test('missing save is empty and incompatible width refuses without replacement', async () => {
    const h = await harness(), missing = h.api.loadConstruction(FixtureBytes);
    await h.opened(); h.current.commit(); assert.equal(await missing, '');
    const previous = { revision: 3, massKg: 8, bodyId: 17n };
    const stored = await harness(fixture(previous)), wrongWidth = stored.api.loadConstruction(FixtureBytes + 1);
    await stored.opened(); stored.current.commit(); await assert.rejects(wrongWidth, /Unsupported construction save/);
    assert.deepEqual(decodeFixture(stored.durable), previous); assert.equal(stored.closed, 1);
});
