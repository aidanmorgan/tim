import * as poseRing from '../../CuriousContraptions.web/wwwroot/pose-ring.js';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import vm from 'node:vm';
import { EventRingCursor, eventDelivery } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
import { HarnessPort } from './event-worker-harness.js';
import type * as ClientModule from '../../CuriousContraptions.web/wwwroot/workshop-client.js';

type ClientApi = Pick<typeof ClientModule, 'create' | 'dispose' | 'status' | 'readEvents' | 'openObservationEvents' | 'readObservationEvents' | 'closeObservationEvents'>;
class Channel { port1 = new HarnessPort(); port2 = new HarnessPort(); }
export async function poseClient() {
    const workers: PlatformWorker[] = [];
    let bootstrapRing: SharedArrayBuffer | undefined;
    class PlatformWorker {
        onmessage: ((event: { data: unknown }) => void) | null = null;
        onerror: ((event: { message: string }) => void) | null = null;
        readonly animation: boolean;
        constructor(url: URL) {
            this.animation = url.pathname.includes('/animation/');
            workers.push(this);
            queueMicrotask(() => this.emit({ awaitingBootstrap: true }));
        }
        emit(data: unknown) { this.onmessage?.({ data }); }
        postMessage(data: unknown) {
            assert.ok(data !== null && typeof data === 'object');
            if ('bootstrap' in data) {
                if (this.animation) {
                    queueMicrotask(() => this.emit({ ready: true, nativeClock: new Float64Array([1, 1, 1, 0, .001, 1, 0]) }));
                } else {
                    assert.ok(data.bootstrap instanceof Uint8Array);
                    assert.ok('poseRing' in data && data.poseRing instanceof SharedArrayBuffer);
                    bootstrapRing = data.poseRing;
                    const peer = new Uint8Array(64), view = new DataView(peer.buffer);
                    view.setUint32(4, 1, true); peer.set(data.bootstrap, 8);
                    view.setBigUint64(24, 1n, true); view.setBigUint64(40, 1n, true);
                    view.setBigUint64(48, 100000n, true); view.setUint32(56, 1, true);
                    queueMicrotask(() => this.emit({ ready: true, captureMode: 1, masterPeer: peer,
                        nativeClock: new Float64Array([1, 1, 1, 0, .001, 1, 0]) }));
                }
            } else if ('animationPort' in data)
                queueMicrotask(() => this.emit({ animationPeer: new Uint8Array(64) }));
        }
        terminate() {}
    }
    const context = vm.createContext({ console, Uint8Array, Float32Array, Float64Array, SharedArrayBuffer,
        BigInt64Array, DataView, Atomics, URL, MessageChannel: Channel, Worker: PlatformWorker,
        document: { baseURI: 'https://example.test/', hidden: false }, setTimeout, clearTimeout, setInterval, clearInterval });
    const source = await fs.readFile('CuriousContraptions.web/wwwroot/workshop-client.js', 'utf8');
    const module = new vm.SourceTextModule(source, { context });
    await module.link(async specifier => {
        if (specifier.includes('pose-ring')) return new vm.SyntheticModule(Object.keys(poseRing), function () { for (const [key, value] of Object.entries(poseRing)) this.setExport(key, value); }, { context });
        const exports: Record<string, unknown> = specifier.includes('event-ring-cursor')
            ? { EventRingCursor, eventDelivery }
            : specifier.includes('native-clock')
                ? { admitNativeClock: async () => {}, nativeNow: () => 0, nativeProfile: 1, nativeClockEvidence: () => [] }
                : specifier.includes('clock-ring')
                    ? { createClockRing: () => new SharedArrayBuffer(64), attachClockRing: () => ({}),
                        requestReply: () => { throw new Error('Unused pose clock request.'); }, heartbeat: () => 0, ClockRole: { Browser: 1 } }
                    : { requireIsolation: () => {}, watchIsolation: () => () => {}, isolationQualified: () => {},
                        isolationEvidence: () => ({}), stopIsolation: () => {} };
        return new vm.SyntheticModule(Object.keys(exports), function () {
            for (const [key, value] of Object.entries(exports)) this.setExport(key, value);
        }, { context });
    });
    await module.evaluate();
    const api = module.namespace as unknown as ClientApi;
    const id = await api.create([1, 2, 3, 4, 5], new Uint8Array(16).fill(1),
        [1, 64, 64, 64, 1, 0], 1, false, () => {}, () => {}, () => {}, () => {}, () => {}, () => {}, () => {});
    assert.ok(bootstrapRing);
    const simulation = workers.find(worker => !worker.animation)!;
    return { api, id, bootstrapRing, deliverRing(buffer: SharedArrayBuffer) { simulation.emit({ poseRing: buffer }); },
        deliverEvents(buffer: SharedArrayBuffer) { simulation.emit({ eventRing: buffer }); },
        dispose() { api.dispose(id); } };
}
