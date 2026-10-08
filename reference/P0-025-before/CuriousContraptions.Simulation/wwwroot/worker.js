import { dotnet } from './_framework/dotnet.js';

const runtime = await dotnet.create();
let host, device, pipelines, bindings, buffers = [], readback, input, stateBytes, current = 0;
let readyCandidate = false, resultBytes, operations;
let activeRequests = 0;
let lifetime = 0, disposed = false;
function requireLifetime(owner) {
    if (disposed || owner !== lifetime) throw new Error('GPU qualification belongs to a retired lifetime.');
}
let dispatchReadbackMs = 0;
let latestRead, pendingReadIdentity;
function flushRead() {
    if (pendingReadIdentity || !latestRead) return;
    const bytes = latestRead;
    latestRead = undefined;
    pendingReadIdentity = bytes.slice(24, 40);
    self.postMessage({ read: bytes, dispatchReadbackMs }, [bytes.buffer]);
}
runtime.setModuleImports('workshopGpu', {
    async initialize(preamble) {
        const owner = lifetime;
        requireLifetime(owner);
        buffers.forEach(buffer => buffer.destroy()); input?.destroy(); readback?.destroy();
        buffers = []; readyCandidate = false; resultBytes = undefined;
        if (!navigator.gpu) throw new Error('WebGPU is required for this Workshop.');
        const adapter = await navigator.gpu.requestAdapter();
        requireLifetime(owner);
        if (!adapter) throw new Error('No WebGPU adapter is available.');
        if (!adapter.features.has('shader-f16')) throw new Error('Required shader-f16 is unavailable.');
        const ownedDevice = await adapter.requestDevice({ requiredFeatures: ['shader-f16'] });
        if (disposed || owner !== lifetime) { ownedDevice.destroy(); requireLifetime(owner); }
        device = ownedDevice;
        device.lost.then(info => {
            if (device !== ownedDevice) return;
            device = undefined;
            host.DeviceLost();
            console.error('CCGPU_DEVICE_LOST', info.message);
        });
        try {
            const response = await fetch('./basketball.wgsl');
            requireLifetime(owner);
            if (!response.ok) throw new Error('Workshop physics shader could not be loaded.');
            const source = await response.text();
            requireLifetime(owner);
            const module = ownedDevice.createShaderModule({ code: preamble + source });
            const compilation = await module.getCompilationInfo();
            requireLifetime(owner);
            if (compilation.messages.some(item => item.type === 'error'))
                throw new Error(compilation.messages.map(item => item.message).join('\n'));
            device.pushErrorScope('validation');
            pipelines = await Promise.all(['admit', 'advance'].map(entryPoint =>
                device.createComputePipelineAsync({ layout: 'auto', compute: { module, entryPoint } })));
            requireLifetime(owner);
            const storageUsage = GPUBufferUsage.STORAGE | GPUBufferUsage.COPY_SRC | GPUBufferUsage.COPY_DST;
            buffers = [0, 1].map(() => device.createBuffer({ size: stateBytes, usage: storageUsage }));
            input = device.createBuffer({ size: stateBytes, usage: storageUsage });
            readback = device.createBuffer({ size: stateBytes, usage: GPUBufferUsage.COPY_DST | GPUBufferUsage.MAP_READ });
            bindings = pipelines.map((pipeline, operationIndex) => buffers.map((_, committedIndex) =>
                device.createBindGroup({ layout: pipeline.getBindGroupLayout(0), entries: [
                    { binding: 0, resource: { buffer: operationIndex === 0 ? input : buffers[committedIndex] } },
                    { binding: 1, resource: { buffer: buffers[1 - committedIndex] } }
                ] })));
            const error = await device.popErrorScope();
            requireLifetime(owner);
            if (error) throw new Error(error.message);
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
        const started = performance.now();
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
            await readback.mapAsync(GPUMapMode.READ);
            try { requireLifetime(owner); resultBytes = new Uint8Array(readback.getMappedRange()).slice(); }
            finally { readback.unmap(); }
        } finally {
            clearTimeout(timeout);
            if (timeoutEmitted && !disposed && owner === lifetime) self.postMessage({ candidateTimedOut: false });
        }
        dispatchReadbackMs = performance.now() - started;
        readyCandidate = true;
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
    acknowledge(bytes) {
        const result = new Uint8Array(bytes);
        self.postMessage({ acknowledgement: result, dispatchReadbackMs }, [result.buffer]);
    },
    retire(detail) {
        try { self.postMessage({ failure: true, detail }); }
        finally { self.close(); }
    },
    publish(bytes) {
        const result = new Uint8Array(bytes);
        latestRead = result;
        flushRead();
    },
    dispose() {
        disposed = true;
        lifetime++;
        const ownedDevice = device;
        device = undefined;
        readyCandidate = false;
        resultBytes = undefined;
        buffers.forEach(buffer => buffer.destroy());
        input?.destroy(); readback?.destroy(); ownedDevice?.destroy();
        buffers = [];
    }
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
host = exports.Program;
operations = Array.from(host.OperationAbi());
stateBytes = host.StateBytes();
self.onmessage = async event => {
    const probe = event.data?.clockProbe;
    if (Number.isSafeInteger(probe) && probe > 0) {
        const now = performance.now();
        self.postMessage({ clockReply: probe, received: now, sent: performance.now() });
        return;
    }
    if (event.data?.reliableStalled === true) { host.ReliableStalled(); return; }
    const receipt = event.data?.readAcknowledged;
    if (receipt instanceof Uint8Array) {
        if (pendingReadIdentity && receipt.length === 16 && receipt.every((value, index) => value === pendingReadIdentity[index])) {
            pendingReadIdentity = undefined;
            flushRead();
        }
        return;
    }
    const bytes = event.data?.bytes;
    if (!(bytes instanceof Uint8Array) || bytes.length < 40 || bytes.length > 168 || activeRequests >= 2) {
        self.postMessage({ rejected: true, detail: 'Invalid or saturated Workshop command transport.' });
        return;
    }
    activeRequests++;
    try {
        await host.Dispatch(bytes);
    } catch (error) {
        self.postMessage({ failure: Array.from(bytes.slice(0, 8)), detail: String(error) });
    } finally { activeRequests--; }
};
self.postMessage({ ready: true });
