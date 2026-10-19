// Console boundary for the Chrome suites: typed CCGPU envelopes carry binary or JSON payloads, not diagnostic prose.
const Base64 = /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/;
const EnvelopeName = /^CCGPU(?:_[A-Z0-9]+)*$/;

// A whole single-line typed envelope: its name, any unsigned integer fields, then one canonical base64 payload
// (acknowledgements, reads, retained commits, trace and tick-time chunks), or its name and one JSON object (typed
// telemetry records). Only such a line may contain "NaN" inside its payload; JSON cannot carry a NaN number.
function isTypedEnvelope(text) {
    if (/[\r\n]/.test(text)) return false;
    const space = text.indexOf(' ');
    if (space <= 0 || !EnvelopeName.test(text.slice(0, space))) return false;
    const rest = text.slice(space + 1);
    if (rest.startsWith('{')) {
        try { const value = JSON.parse(rest); return value !== null && typeof value === 'object' && !Array.isArray(value); }
        catch (error) { if (error instanceof SyntaxError) return false; throw error; }
    }
    const fields = rest.split(' ');
    const payload = fields.at(-1);
    return fields.slice(0, -1).every(field => /^[0-9]+$/.test(field)) && payload.length > 0 && payload.length % 4 === 0 &&
        Base64.test(payload);
}

/** A "NaN" in diagnostic prose, never a substring of a typed envelope's payload (Murdoch-2 F1, Story 9.4b). */
export function reportsNaN(type, text) {
    return text.includes('NaN') && !(type === 'log' && isTypedEnvelope(text));
}

export function isWorkshopConsoleFailure(type, text) {
    if (type === 'error' || text.includes('CCGPU_TRANSPORT_FAILURE') ||
        text.includes('CCGPU_STARTUP_EXCEPTION') || text.includes('Unhandled exception')) return true;
    return reportsNaN(type, text);
}
