import { dotnet } from './_framework/dotnet.js';
const runtime = await dotnet.create();
let device, pipeline, input, output, readback, binding;
let latency = [], resultBytes;
let stateBytes;
runtime.setModuleImports('gpu', {
    async initialize() {
        if (!navigator.gpu) throw new Error('WebGPU is unsupported.');
        const adapter = await navigator.gpu.requestAdapter();
        if (!adapter || !adapter.features.has('shader-f16')) throw new Error('Required shader-f16 is unsupported.');
        device = await adapter.requestDevice({ requiredFeatures: ['shader-f16'] });
        const source = 'enable f16;\n' + host.ShaderAbi() + '\n' + await (await fetch('./body-integration.wgsl')).text();
        device.pushErrorScope('validation');
        const module = device.createShaderModule({ code: source });
        const compilation = await module.getCompilationInfo();
        if (compilation.messages.some(item => item.type === 'error'))
            throw new Error(compilation.messages.map(item => item.message).join('\n'));
        pipeline = await device.createComputePipelineAsync({
            layout: 'auto', compute: { module, entryPoint: 'integrate' }
        });
        input = device.createBuffer({ size: stateBytes, usage: GPUBufferUsage.STORAGE | GPUBufferUsage.COPY_DST });
        output = device.createBuffer({ size: stateBytes, usage: GPUBufferUsage.STORAGE | GPUBufferUsage.COPY_SRC });
        readback = device.createBuffer({ size: stateBytes, usage: GPUBufferUsage.COPY_DST | GPUBufferUsage.MAP_READ });
        binding = device.createBindGroup({ layout: pipeline.getBindGroupLayout(0),
            entries: [{ binding: 0, resource: { buffer: input } }, { binding: 1, resource: { buffer: output } }] });
        const error = await device.popErrorScope();
        if (error) throw new Error(error.message);
        device.lost.then(info => { device = undefined; self.postMessage({ fault: info.message }); });
    },
    async advance(bytes) {
        if (!device) throw new Error('GPU device unavailable.');
        const started = performance.now();
        device.queue.writeBuffer(input, 0, bytes);
        const encoder = device.createCommandEncoder();
        const pass = encoder.beginComputePass();
        pass.setPipeline(pipeline); pass.setBindGroup(0, binding); pass.dispatchWorkgroups(1); pass.end();
        encoder.copyBufferToBuffer(output, 0, readback, 0, stateBytes);
        device.queue.submit([encoder.finish()]);
        await readback.mapAsync(GPUMapMode.READ);
        const result = new Uint8Array(readback.getMappedRange()).slice();
        readback.unmap();
        latency.push(performance.now() - started);
        resultBytes = result;
    },
    readResult() { if (!resultBytes) throw new Error('Readback is not complete.'); const result=resultBytes; resultBytes=undefined; return result; },
    publish(bytes, status, detail) {
        self.postMessage({ bytes: Array.from(bytes), status, detail,
            dispatchReadbackMs: latency.at(-1) ?? 0, bufferBytes: stateBytes * 3 });
    },
    dispose() { input?.destroy(); output?.destroy(); readback?.destroy(); device?.destroy(); device = undefined; }
});
const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
const host = exports.Program;
stateBytes = host.RecordByteLength();
const [Construct, Run, Reset, Dispose] = host.CommandAbi();
self.postMessage({ recordBytes: stateBytes, commands: { Construct, Run, Reset, Dispose }, states: Array.from(host.StateAbi()) });
self.onmessage = async event => {
    const { command, origin, speed, ticks } = event.data;
    // Validated numeric command discriminants are an external fixture boundary.
    switch (command) {
        case Construct: host.Construct(origin, speed); break;
        case Run: await host.Run(ticks); break;
        case Reset: host.Reset(); break;
        case Dispose: host.Dispose(); break;
        default: throw new Error('Unknown fixture command.');
    }
};
await host.Initialize();
