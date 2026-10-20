import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';
import { attachClockRing, requestReply, ClockRole } from '../clock-ring.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, port, clock, service, disposed = false, bootstrapped = false, qualified = false;
// The latest sample per target: one entry per animated target of the machine, with no fixed target capacity (Story 9R.5b).
let outputKinds, pendingSample;
const latestSamples = new Map();
function closeOwned() {
    if (disposed) return;
    disposed = true;
    latestSamples.clear(); pendingSample = undefined;
    clearInterval(service); service = undefined;
    if (port) { port.onmessage = null; port.close(); port = undefined; }
    clock = undefined;
}
function fail(error) {
    closeOwned();
    try { self.postMessage({ failure: true, detail: String(error) }); }
    finally { self.close(); }
}
function requirePort() {
    if (disposed || !port) throw new Error('Animation master port is retired.');
    return port;
}
// The ring is the worker's only clock authority; closeOwned detaches it, so a retired worker can never probe it again.
function requireClock() {
    if (!clock) throw new Error('Animation clock ring is retired.');
    return clock;
}
function flushSample() {
    if (pendingSample || latestSamples.size === 0) return;
    const [target, value] = latestSamples.entries().next().value;
    latestSamples.delete(target);
    pendingSample = value.slice();
    self.postMessage({ animationOutput: value }, [value.buffer]);
}
runtime.setModuleImports('workshopAnimation', {
    now: nativeNow,
    probe(bytes) {
        // The Simulation-owned clock master answers through the clock ring; the reply is received when the wait resolves.
        requestReply(requireClock(), ClockRole.Animation, new Uint8Array(bytes)).then(reply => {
            if (!reply || disposed) return;
            try { host.Reply(reply, nativeNow()); } catch (error) { fail(error); }
        }, error => { if (!disposed) fail(error); });
    },
    control(bytes) {
        const value = new Uint8Array(bytes);
        requirePort().postMessage({ scheduleControl: value }, [value.buffer]);
    },
    output(bytes) {
        if (disposed) return;
        const value = new Uint8Array(bytes);
        const kind = new DataView(value.buffer).getUint32(60, true);
        if (kind === outputKinds[1]) {
            const target = new DataView(value.buffer).getBigUint64(64, true);
            latestSamples.set(target, value); flushSample();
        }
        else if (kind === outputKinds[0] || kind === outputKinds[2]) self.postMessage({ animationOutput: value }, [value.buffer]);
        else throw new Error('Unknown Animation output kind.');
    },
    qualified() {
        if (disposed) return;
        if (!qualified) { qualified = true; self.postMessage({ qualified: true }); }
    }
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.CuriousContraptions.Animation.Worker.Program;
outputKinds = Array.from(host.OutputKinds());
self.onmessage = event => {
    if (disposed) return;
    try {
        const message = event.data;
        if (message.bootstrap instanceof Uint8Array) {
            const incomingPort = message.masterPort;
            if (!(incomingPort instanceof MessagePort)) throw new Error('Animation bootstrap requires its owned master port.');
            if (bootstrapped) { incomingPort.close(); throw new Error('Duplicate Animation bootstrap.'); }
            let ownedClock;
            try { ownedClock = attachClockRing(message.clockRing); host.Bootstrap(message.bootstrap); }
            catch (error) { incomingPort.close(); throw error; }
            port = incomingPort; clock = ownedClock;
            port.onmessage = incoming => {
                if (disposed) return;
                try {
                    if (incoming.data?.scheduleControl instanceof Uint8Array)
                        host.ScheduleControl(incoming.data.scheduleControl);
                    else throw new Error('Unsupported Animation master-port envelope.');
                } catch (error) { fail(error); }
            };
            port.start(); bootstrapped = true;
            // Wakeups only service the absolute master-derived ordinal; they are not a clock authority. A missing master
            // reply never faults the session: the mapping recalibrates as counted telemetry (Story 9.4b).
            service = setInterval(() => {
                try {
                    host.Service();
                } catch (error) { fail(error); }
            }, 1);
            const nativeClock = new Float64Array(nativeClockEvidence());
            self.postMessage({ ready: true, nativeClock }, [nativeClock.buffer]);
        } else if (message.animationAcknowledged instanceof Uint8Array) {
            const receipt = message.animationAcknowledged;
            if (!pendingSample || receipt.length !== pendingSample.length ||
                !receipt.every((value, index) => value === pendingSample[index]))
                throw new Error('Foreign Animation publication receipt.');
            pendingSample = undefined; flushSample();
        } else if (message.animationControl instanceof Uint8Array && bootstrapped) {
            host.AnimationControl(message.animationControl);
        } else if (message.dispose === true) {
            closeOwned(); self.close();
        } else throw new Error('Unsupported Animation browser envelope.');
    } catch (error) { fail(error); }
};
self.postMessage({ awaitingBootstrap: true });
