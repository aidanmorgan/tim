// Console boundary for CAT-023b: binary trace payloads are not diagnostic prose.
export function isWorkshopConsoleFailure(type, text) {
    if (type === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE') ||
        text.includes('CCGPU_STARTUP_EXCEPTION') || text.includes('Unhandled exception')) return true;
    if (!text.includes('NaN')) return false;
    // WorkshopTrace.FlushSealed emits exactly five unsigned fields and one base64 payload.
    // Only normal log messages with the complete envelope may suppress a payload substring.
    const fields = text.split(' ');
    const isTraceChunk = type === 'log' && !/[\r\n]/.test(text) && fields.length === 7 &&
        fields[0] === 'CCGPU_TRACE_CHUNK' &&
        fields.slice(1, 6).every(field => /^[0-9]+$/.test(field)) &&
        fields[6].length % 4 === 0 && /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(fields[6]);
    return !isTraceChunk;
}
