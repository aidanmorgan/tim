// Storage boundary controls for the actual exported client functions.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../CuriousContraptions.web/wwwroot/workshop-client.js', import.meta.url), 'utf8');
const executable = source.replace(/^import .*;\n/gm, '').replace(/^export /gm, '') +
    '\n;({saveConstruction, loadConstruction});';

function harness(initial) {
    let durable = initial, current, closed = 0;
    const database = {
        close() { closed++; },
        transaction(_store, mode) {
            let pending, request;
            current = {
                objectStore() { return {
                    put(value) { pending = new Uint8Array(value); return {}; },
                    get() { request = { result: durable === undefined ? undefined : new Uint8Array(durable) }; return request; }
                }; },
                requestSuccess() { request?.onsuccess?.(); },
                commit() { if (mode === 'readwrite') durable = pending; this.oncomplete(); },
                abort() { this.error = new Error('controlled transaction abort'); this.onabort(); }
            };
            return current;
        }
    };
    const api = vm.runInNewContext(executable, {
        Uint8Array, Map, Set, Array, Promise, Error, btoa: value => Buffer.from(value, 'binary').toString('base64'),
        indexedDB: { open() { const request = { result: database }; queueMicrotask(() => request.onsuccess()); return request; } }
    });
    return { api, get current() { return current; }, get durable() { return durable; }, get closed() { return closed; } };
}
const openTransaction = async () => { await Promise.resolve(); await Promise.resolve(); await Promise.resolve(); };

test('request success cannot complete Save; transaction abort preserves prior exact bytes', async () => {
    const h = harness(new Uint8Array([1, 2, 3]));
    let settled = false;
    const pending = h.api.saveConstruction(new Uint8Array([7, 8, 9]));
    pending.then(() => { settled = true; }, () => { settled = true; });
    await openTransaction();
    h.current.requestSuccess();
    await Promise.resolve();
    assert.equal(settled, false);
    assert.deepEqual(h.durable, new Uint8Array([1, 2, 3]));
    h.current.abort();
    await assert.rejects(pending, /transaction abort/);
    assert.deepEqual(h.durable, new Uint8Array([1, 2, 3]));
    assert.equal(h.closed, 1);
});

test('completed Save owns input bytes and Load returns an exact independent roundtrip', async () => {
    const h = harness();
    const input = new Uint8Array([0, 128, 255]);
    const pending = h.api.saveConstruction(input);
    input.fill(4);
    await openTransaction(); h.current.commit(); await pending;
    assert.deepEqual(h.durable, new Uint8Array([0, 128, 255]));
    const loaded = h.api.loadConstruction(3);
    await openTransaction(); h.current.commit();
    const result = Uint8Array.from(Buffer.from(await loaded, 'base64'));
    assert.deepEqual(result, new Uint8Array([0, 128, 255]));
    result.fill(5);
    assert.deepEqual(h.durable, new Uint8Array([0, 128, 255]));
    assert.equal(h.closed, 2);
});

test('missing save is empty and incompatible width rejects without replacement', async () => {
    const h = harness();
    const missing = h.api.loadConstruction(3);
    await openTransaction(); h.current.commit(); assert.equal(await missing, '');
    const stored = harness(new Uint8Array([1, 2]));
    const wrongWidth = stored.api.loadConstruction(3);
    await openTransaction(); stored.current.commit();
    await assert.rejects(wrongWidth, /Unsupported construction save/);
    assert.deepEqual(stored.durable, new Uint8Array([1, 2]));
    assert.equal(stored.closed, 1);
});
