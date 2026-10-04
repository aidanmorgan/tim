import { dotnet } from './_framework/dotnet.js';
import { admitNativeClock, nativeNow, nativeClockEvidence } from '../native-clock.js';

// Review-only bounded startup attribution; stage IDs are external observation metadata.
const StartupStage = Object.freeze({ Initialize:1, Adapter:2, Device:3, Fetch:4, Text:5, Module:6,
    CompilationInfo:7, AdmitPipeline:8, AdvancePipeline:9, Buffers:10, ErrorScope:11,
    FirstAdmission:12, FirstMap:13, Publish:14, Acknowledge:15, Bootstrap:16 });
const startupStages = [];
let startupObservationOpen = true, firstAdmissionObserved = false;
function observeStartup(stage, completed) {
    if (startupObservationOpen && startupStages.length < 96) {
        const row = [stage, completed, Date.now()];
        startupStages.push(row);
        console.info('CCGUIDE_STARTUP_STAGE', JSON.stringify(row));
    }
}
Object.defineProperty(globalThis, '__p054StartupStages', { get: () => startupStages.map(row => row.slice()) });

await admitNativeClock();
const runtime = await dotnet.create();
let host, device, pipelines, bindings, buffers = [], readback, input, stateBytes, current = 0;
let readyCandidate = false, resultBytes, operations;
let activeRequests = 0;
let observationPending = false;
let animationPort, roles;
let lifetime = 0, disposed = false;
function disposeOwned() {
    disposed = true;
    lifetime++;
    if (animationPort) { animationPort.onmessage = null; animationPort.close(); animationPort = undefined; }
    const ownedDevice = device;
    device = undefined;
    readyCandidate = false;
    resultBytes = undefined;
    buffers.forEach(buffer => buffer.destroy());
    input?.destroy(); readback?.destroy(); ownedDevice?.destroy();
    buffers = [];
}
function clockNow() {
    try { return nativeNow(); }
    catch (error) {
        disposeOwned();
        try { self.postMessage({ failure: true, detail: String(error) }); }
        finally { self.close(); }
        throw error;
    }
}
function requireLifetime(owner) {
    if (disposed || owner !== lifetime) throw new Error('GPU qualification belongs to a retired lifetime.');
}
let dispatchReadbackMs = 0;
let latestRead, pendingReadIdentity;
function flushRead() {
    if (pendingReadIdentity || !latestRead) return;
    const bytes = latestRead;
    latestRead = undefined;
    pendingReadIdentity = new Uint8Array(24);
    pendingReadIdentity.set(bytes.subarray(64, 80));
    pendingReadIdentity.set(bytes.subarray(104, 112), 16);
    self.postMessage({ read: bytes, dispatchReadbackMs }, [bytes.buffer]);
}
function flushObservation() {
    if (!observationPending || activeRequests !== 0 || pendingReadIdentity || latestRead) return;
    observationPending = false;
    try { host.FlushObservation(); }
    catch { /* Diagnostic output failure cannot reject an admitted read; missing trace remains Incomplete. */ }
}
function acknowledgeRead(receipt) {
    if (!pendingReadIdentity || receipt.length !== 24 ||
        !receipt.every((value, index) => value === pendingReadIdentity[index])) return;
    pendingReadIdentity = undefined;
    // A queued terminal must become the next pending identity before any diagnostic output.
    flushRead();
    flushObservation();
}
runtime.setModuleImports('workshopGpu', {
    now() { return clockNow(); },
    scheduleControl(bytes, recipient) {
        const value = new Uint8Array(bytes);
        if (recipient === roles[0]) self.postMessage({ scheduleControl: value }, [value.buffer]);
        else if (recipient === roles[1] && animationPort)
            animationPort.postMessage({ scheduleControl: value }, [value.buffer]);
        else throw new Error('Unknown or retired schedule recipient.');
    },
    async initialize(preamble) {
        observeStartup(StartupStage.Initialize, false);
        const owner = lifetime;
        requireLifetime(owner);
        buffers.forEach(buffer => buffer.destroy()); input?.destroy(); readback?.destroy();
        buffers = []; readyCandidate = false; resultBytes = undefined;
        if (!navigator.gpu) throw new Error('WebGPU is required for this Workshop.');
        observeStartup(StartupStage.Adapter, false);
        const adapter = await navigator.gpu.requestAdapter();
        observeStartup(StartupStage.Adapter, true);
        requireLifetime(owner);
        if (!adapter) throw new Error('No WebGPU adapter is available.');
        if (!adapter.features.has('shader-f16')) throw new Error('Required shader-f16 is unavailable.');
        observeStartup(StartupStage.Device, false);
        const ownedDevice = await adapter.requestDevice({ requiredFeatures: ['shader-f16'] });
        observeStartup(StartupStage.Device, true);
        if (disposed || owner !== lifetime) { ownedDevice.destroy(); requireLifetime(owner); }
        device = ownedDevice;
        device.lost.then(info => {
            if (device !== ownedDevice) return;
            device = undefined;
            host.DeviceLost();
            console.error('CCGPU_DEVICE_LOST', info.message);
        });
        try {
            observeStartup(StartupStage.Fetch, false);
            const response = await fetch('./physics.wgsl');
            observeStartup(StartupStage.Fetch, true);
            requireLifetime(owner);
            if (!response.ok) throw new Error('Workshop physics shader could not be loaded.');
            observeStartup(StartupStage.Text, false);
            const source = await response.text();
            observeStartup(StartupStage.Text, true);
            requireLifetime(owner);
            observeStartup(StartupStage.Module, false);
            const module = ownedDevice.createShaderModule({ code: preamble + source });
            observeStartup(StartupStage.Module, true);
            observeStartup(StartupStage.CompilationInfo, false);
            const compilation = await module.getCompilationInfo();
            observeStartup(StartupStage.CompilationInfo, true);
            requireLifetime(owner);
            if (compilation.messages.some(item => item.type === 'error'))
                throw new Error(compilation.messages.map(item => item.message).join('\n'));
            device.pushErrorScope('validation');
            pipelines = await Promise.all(['admit', 'advance'].map((entryPoint, index) => {
                const stage = index === 0 ? StartupStage.AdmitPipeline : StartupStage.AdvancePipeline;
                observeStartup(stage, false);
                return device.createComputePipelineAsync({ layout: 'auto', compute: { module, entryPoint } })
                    .then(pipeline => { observeStartup(stage, true); return pipeline; });
            }));
            requireLifetime(owner);
            observeStartup(StartupStage.Buffers, false);
            const storageUsage = GPUBufferUsage.STORAGE | GPUBufferUsage.COPY_SRC | GPUBufferUsage.COPY_DST;
            buffers = [0, 1].map(() => device.createBuffer({ size: stateBytes, usage: storageUsage }));
            input = device.createBuffer({ size: stateBytes, usage: storageUsage });
            readback = device.createBuffer({ size: stateBytes, usage: GPUBufferUsage.COPY_DST | GPUBufferUsage.MAP_READ });
            bindings = pipelines.map((pipeline, operationIndex) => buffers.map((_, committedIndex) =>
                device.createBindGroup({ layout: pipeline.getBindGroupLayout(0), entries: [
                    { binding: 0, resource: { buffer: operationIndex === 0 ? input : buffers[committedIndex] } },
                    { binding: 1, resource: { buffer: buffers[1 - committedIndex] } }
                ] })));
            observeStartup(StartupStage.Buffers, true);
            observeStartup(StartupStage.ErrorScope, false);
            const error = await device.popErrorScope();
            observeStartup(StartupStage.ErrorScope, true);
            requireLifetime(owner);
            if (error) throw new Error(error.message);
            observeStartup(StartupStage.Initialize, true);
            console.info('CCGPU_READY', JSON.stringify({ shaderF16: true, stateBytes, ownedGpuBytes: stateBytes * 4 }));
        } catch (error) {
            buffers.forEach(buffer => buffer.destroy());
            input?.destroy(); readback?.destroy();
            device = undefined;
            ownedDevice.destroy();
            throw error;
        }
    },
    async stage(bytes, operation) {
        const owner = lifetime;
        requireLifetime(owner);
        if (!device) throw new Error('GPU device is unavailable.');
        const operationIndex = operations.indexOf(operation);
        if (operationIndex < 0 || readyCandidate) throw new Error('Invalid GPU candidate operation.');
        if ((operationIndex === 0 && bytes.length !== stateBytes) || (operationIndex === 1 && bytes.length !== 0))
            throw new Error('Invalid GPU candidate input length.');
        const observingAdmission = operationIndex === 0 && !firstAdmissionObserved;
        if (observingAdmission) { firstAdmissionObserved = true; observeStartup(StartupStage.FirstAdmission, false); }
        const started = clockNow();
        if (operationIndex === 0) device.queue.writeBuffer(input, 0, bytes);
        const encoder = device.createCommandEncoder();
        const pass = encoder.beginComputePass();
        pass.setPipeline(pipelines[operationIndex]);
        pass.setBindGroup(0, bindings[operationIndex][current]);
        pass.dispatchWorkgroups(1);
        pass.end();
        encoder.copyBufferToBuffer(buffers[1 - current], 0, readback, 0, stateBytes);
        device.queue.submit([encoder.finish()]);
        let timeoutEmitted = false;
        const timeout = setTimeout(() => {
            if (!disposed && owner === lifetime) {
                timeoutEmitted = true;
                self.postMessage({ candidateTimedOut: true });
            }
        }, 2000);
        try {
            if (observingAdmission) observeStartup(StartupStage.FirstMap, false);
            await readback.mapAsync(GPUMapMode.READ);
            if (observingAdmission) observeStartup(StartupStage.FirstMap, true);
            try { requireLifetime(owner); resultBytes = new Uint8Array(readback.getMappedRange()).slice(); }
            finally { readback.unmap(); }
        } finally {
            clearTimeout(timeout);
            if (timeoutEmitted && !disposed && owner === lifetime) self.postMessage({ candidateTimedOut: false });
        }
        dispatchReadbackMs = clockNow() - started;
        readyCandidate = true;
        if (observingAdmission) observeStartup(StartupStage.FirstAdmission, true);
    },
    deviceReady() { return device !== undefined; },
    read() {
        if (!readyCandidate || !resultBytes) throw new Error('Candidate readback has not completed.');
        const result = resultBytes;
        resultBytes = undefined;
        return result;
    },
    commit() {
        if (!device || !readyCandidate) throw new Error('Candidate is not ready to commit.');
        current = 1 - current;
        readyCandidate = false;
    },
    discard() { readyCandidate = false; resultBytes = undefined; },
    acknowledge(bytes, pending) {
        observeStartup(StartupStage.Acknowledge, true); startupObservationOpen = false;
        observationPending = pending;
        const result = new Uint8Array(bytes);
        self.postMessage({ acknowledgement: result, dispatchReadbackMs }, [result.buffer]);
    },
    retire(detail) {
        try { self.postMessage({ failure: true, detail }); }
        finally { self.close(); }
    },
    publish(bytes, pending) {
        observeStartup(StartupStage.Publish, true);
        observationPending = pending;
        const result = new Uint8Array(bytes);
        latestRead = result;
        flushRead();
    },
    dispose: disposeOwned
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.Program;
roles = Array.from(host.ScheduleRoles());
operations = Array.from(host.OperationAbi());
stateBytes = host.StateBytes();
self.onmessage = async event => {
    let received;
    try { received = clockNow(); } catch { return; }
    const bootstrap = event.data?.bootstrap;
    if (bootstrap instanceof Uint8Array) {
        try {
            const captureMode = event.data.captureMode;
            if ((captureMode !== 1 && captureMode !== 2) || captureMode !== host.CaptureMode())
                throw new Error('Browser and simulation capture modes differ.');
            observeStartup(StartupStage.Bootstrap, false);
            const masterPeer = new Uint8Array(host.Bootstrap(bootstrap));
            observeStartup(StartupStage.Bootstrap, true);
            const nativeClock = new Float64Array(nativeClockEvidence());
            self.postMessage({ ready: true, nativeClock, captureMode, masterPeer }, [nativeClock.buffer, masterPeer.buffer]);
        }
        catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
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
                activeRequests === 0 && !latestRead && !pendingReadIdentity && !readyCandidate));
            if (clockNow() - received > 1000) {
                console.error('CCGPU_MEMORY_LATE', Array.from(result).join(','));
                throw new Error('Stopped worker collection exceeded 1000ms; reload to recover.');
            }
            self.postMessage({ memoryResult: result }, [result.buffer]);
        } catch (error) { self.postMessage({ failure: true, detail: String(error) }); }
        return;
    }
    const bytes = event.data?.bytes;
    if (!(bytes instanceof Uint8Array) || bytes.length < 72 || bytes.length > 1416 || activeRequests >= 2) {
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
