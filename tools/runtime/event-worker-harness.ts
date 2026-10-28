import * as clockRing from '../../CuriousContraptions.web/wwwroot/clock-ring.js';
import fs from 'node:fs/promises';
import path from 'node:path';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import { EventRingMemory } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
import type { ManagedImports } from './worker-contracts.js';

export class HarnessPort {
    readonly messages: unknown[] = [];
    onmessage: ((event: MessageEvent<unknown>) => void) | null = null;
    postMessage(message: unknown) { this.messages.push(message); }
    start() {}
    close() { this.onmessage = null; }
}
class HarnessMessageChannel {
    port1 = new HarnessPort();
    port2 = new HarnessPort();
}
export async function eventWorker() {
    const posted: SharedArrayBuffer[] = [], posePosted: SharedArrayBuffer[] = [];
    let imports: ManagedImports | undefined;
    const scope: {
        onmessage: ((event: { data: unknown }) => Promise<void>) | null;
        postMessage(message: unknown): void;
        close(): void;
    } = {
        onmessage: null,
        postMessage(message) {
            if (message !== null && typeof message === 'object' && 'eventRing' in message && message.eventRing instanceof SharedArrayBuffer)
                posted.push(message.eventRing);
            if (message !== null && typeof message === 'object' && 'poseRing' in message && message.poseRing instanceof SharedArrayBuffer)
                posePosted.push(message.poseRing);
        },
        close() {}
    };
    const context = vm.createContext({ self: scope, console, performance,
        Uint8Array, Uint32Array, Float32Array, Float64Array, ArrayBuffer, SharedArrayBuffer,
        BigInt64Array, BigUint64Array, DataView, Atomics, MessageChannel: HarnessMessageChannel,
        MessagePort: HarnessPort, setTimeout, clearTimeout, setInterval, clearInterval });
    const runtime = {
        setModuleImports(_name: string, value: ManagedImports) { imports = value; },
        getAssemblyExports: async () => ({ CuriousContraptions: { Simulation: { Program: {
            ScheduleRoles: () => [1, 2], AnimationPeer: () => new Uint8Array(0)
        } } } }),
        getConfig: () => ({ mainAssemblyName: 'EventRingControl' })
    };
    const source = await fs.readFile(path.join(process.cwd(), 'CuriousContraptions.Simulation/wwwroot/worker.js'), 'utf8');
    const worker = new vm.SourceTextModule(source, { context });
    await worker.link(async name => {
        if (name === '../event-ring-cursor.js')
            return new vm.SyntheticModule(['EventRingMemory'], function () { this.setExport('EventRingMemory', EventRingMemory); }, { context });
        if (name === '../clock-ring.js') return new vm.SyntheticModule(Object.keys(clockRing), function () {
            for (const [key, value] of Object.entries(clockRing)) this.setExport(key, value);
        }, { context });
        const names = name.includes('dotnet') ? ['dotnet'] : ['admitNativeClock', 'nativeNow', 'nativeClockEvidence'];
        return new vm.SyntheticModule(names, function () {
            for (const name of names) this.setExport(name,
                name === 'dotnet' ? { create: async () => runtime } :
                name === 'admitNativeClock' ? async () => {} :
                name === 'nativeNow' ? () => 0 : () => { throw new Error('Unused event harness boundary.'); });
        }, { context });
    });
    await worker.evaluate();
    assert.ok(imports);
    return { imports, posted, posePosted, async seatAnimationPort(port: HarnessPort) {
        assert.ok(scope.onmessage);
        await scope.onmessage({ data: { animationPort: port } });
    } };
}
