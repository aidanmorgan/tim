const worker = new Worker('./worker.js', { type: 'module' });
const elements = Object.fromEntries(['origin','speed','duration','construct','run','reset','view','status'].map(id => [id,document.getElementById(id)]));
const context = elements.view.getContext('2d');
let construction, samples = [], commands, states, recordBytes;
for (const button of [elements.construct,elements.run,elements.reset]) button.disabled = true;
elements.construct.onclick = () => { samples = []; worker.postMessage({command:commands.Construct,origin:Number(elements.origin.value),speed:Number(elements.speed.value)}); };
elements.run.onclick = () => worker.postMessage({command:commands.Run,ticks:Math.round(Number(elements.duration.value)*120)});
elements.reset.onclick = () => worker.postMessage({command:commands.Reset});
function half(bits) {
    const sign = (bits & 32768) ? -1 : 1;
    const exponent = (bits >>> 10) & 31, fraction = bits & 1023;
    return sign * (exponent === 0 ? fraction * 2 ** -24 : (1 + fraction / 1024) * 2 ** (exponent - 15));
}
worker.onmessage = event => {
    const message = event.data;
    if (message.commands) { commands=Object.freeze(message.commands); recordBytes=message.recordBytes; const [Ready,Running,Complete,Reset,Rejected,Unsupported,Fault]=message.states; states=Object.freeze({Ready,Running,Complete,Reset,Rejected,Unsupported,Fault}); return; }
    if (message.status === states.Ready) for (const button of [elements.construct,elements.run,elements.reset]) button.disabled = false;
    if (message.fault) { elements.status.textContent = message.fault; return; }
    if (message.bytes.length === recordBytes) {
        const bytes = new Uint8Array(message.bytes), view = new DataView(bytes.buffer);
        const wideTick = view.getBigUint64(16,true);
        if (wideTick > BigInt(Number.MAX_SAFE_INTEGER)) throw new Error('Fixture display tick exceeds exact JS number range.');
        const tick = Number(wideTick);
        const position = (view.getInt32(32,true)+half(view.getUint16(56,true)))/16;
        const speed = half(view.getUint16(64,true))*32;
        if (message.status === states.Ready) construction = { bytes:message.bytes, position, speed };
        if (tick > 0) samples.push({tick,position,latency:message.dispatchReadbackMs});
        const error = construction ? position-(construction.position+construction.speed*tick/120) : 0;
        const maximum = construction ? samples.reduce((value,sample)=>Math.max(value,Math.abs(sample.position-construction.position-construction.speed*sample.tick/120)),0) : 0;
        const exactReset = message.status === states.Reset && construction && message.bytes.every((value,index)=>value===construction.bytes[index]);
        elements.status.textContent = JSON.stringify({ ...message, position, canonicalSpeed:speed, trajectoryError:error, maximumError:maximum, exactReset, sampleCount:samples.length },null,2);
        context.clearRect(0,0,960,260); context.fillStyle='#468c8a';
        context.fillRect(460+(position-(construction?.position??0))*12,100,40,40);
        console.log('GPU_BODY', JSON.stringify({tick,position,speed,status:message.status,error,maximum,exactReset,latency:message.dispatchReadbackMs,bytes:message.bytes}));
    } else elements.status.textContent = message.detail;
};
worker.onerror = event => { elements.status.textContent = event.message; };
