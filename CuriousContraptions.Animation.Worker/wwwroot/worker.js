import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';

await admitNativeClock();
const runtime = await dotnet.create();
let host, port, service, disposed = false, bootstrapped = false, qualified = false;
let lastReply = 0, outputKinds, targetCapacity, pendingSample;
const latestSamples = new Map();
function closeOwned() {
    if (disposed) return;
    disposed = true;
    latestSamples.clear(); pendingSample = undefined;
    clearInterval(service); service = undefined;
    if (port) { port.onmessage = null; port.close(); port = undefined; }
}
function fail(error) {
    closeOwned();
    try { self.postMessage({ failure: true, detail: String(error) }); }
    finally { self.close(); }
}
function requirePort() {
    if (disposed || !port) throw new Error('Animation clock port is retired.');
    return port;
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
        const value = new Uint8Array(bytes);
        requirePort().postMessage({ clockProbe: value }, [value.buffer]);
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
            if (!latestSamples.has(target) && latestSamples.size >= targetCapacity)
                throw new Error('Animation latest target capacity exceeded.');
            latestSamples.set(target, value); flushSample();
        }
        else if (kind === outputKinds[0] || kind === outputKinds[2]) self.postMessage({ animationOutput: value }, [value.buffer]);
        else throw new Error('Unknown Animation output kind.');
    },
    qualified() {
        if (disposed) return;
        if (!qualified) { qualified = true; self.postMessage({ qualified: true }); }
    },
    acceptedReply() { lastReply = nativeNow(); }
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.Program;
outputKinds = Array.from(host.OutputKinds());
targetCapacity = host.TargetCapacity();
if (!Number.isSafeInteger(targetCapacity) || targetCapacity <= 0) throw new Error('Invalid declared Animation target capacity.');
self.onmessage = event => {
    if (disposed) return;
    try {
        const message = event.data;
        if (message.bootstrap instanceof Uint8Array) {
            const incomingPort = message.clockPort;
            if (!(incomingPort instanceof MessagePort)) throw new Error('Animation bootstrap requires its owned clock port.');
            if (bootstrapped) { incomingPort.close(); throw new Error('Duplicate Animation bootstrap.'); }
            try { host.Bootstrap(message.bootstrap); }
            catch (error) { incomingPort.close(); throw error; }
            port = incomingPort;
            port.onmessage = incoming => {
                if (disposed) return;
                try {
                    if (incoming.data?.clockReply instanceof Uint8Array)
                        host.Reply(incoming.data.clockReply, nativeNow());
                    else if (incoming.data?.scheduleControl instanceof Uint8Array)
                        host.ScheduleControl(incoming.data.scheduleControl);
                    else throw new Error('Unsupported Animation master-port envelope.');
                } catch (error) { fail(error); }
            };
            port.start(); bootstrapped = true; lastReply = nativeNow();
            // Wakeups only service the absolute master-derived ordinal; they are not a clock authority.
            service = setInterval(() => {
                try {
                    if (nativeNow() - lastReply > 1000) throw new Error('Animation master reply missing for1000ms.');
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
