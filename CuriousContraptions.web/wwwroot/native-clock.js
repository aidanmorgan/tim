// External native boundary. Qualification is the live precision witness under cross-origin
// isolation: no browser identity pin. UA-CH identity, where available, is logged as evidence only.
// Conservative error 84.1255us; declaration stays 100us. A small increment alone never qualifies.
export const nativeProfile = 2;
const Phase = Object.freeze({ fresh: 0, admitting: 1, ready: 2, retired: 3 });
let phase = Phase.fresh, last = 0, qualifiedAt = 0, witnessDelta = 0, renewals = 0;
function reject(message) {
    phase = Phase.retired;
    throw new Error('Unsupported native clock: ' + message);
}
function read() {
    if (phase === Phase.retired) throw new Error('Native clock generation is retired.');
    if (!globalThis.isSecureContext || !globalThis.crossOriginIsolated) reject('secure isolation is required.');
    const value = performance.now();
    if (!Number.isFinite(value) || value < 0 || value < last) reject('invalid or reversed reading.');
    last = value;
    return value;
}
function qualify(start) {
    let previous = start;
    // Including the caller's anchor, at most 4096 native reads. Scheduling gaps do not
    // prove coarse precision; only a fresh adjacent pair can qualify after any gap.
    for (let i = 1; i < 4096; i++) {
        const current = read();
        const delta = current - previous;
        if (delta > 0 && delta <= 0.007) {
            qualifiedAt = current;
            witnessDelta = delta;
            renewals++;
            return current;
        }
        previous = current;
    }
    reject('no witness within the finite sampling limit.');
}
async function describeSource() {
    // Evidence only; a browser without UA-CH (Safari, Firefox) or one that refuses the lookup proceeds straight to the witness.
    try {
        if (!navigator.userAgentData?.getHighEntropyValues) return 'identity unavailable';
        const identity = await navigator.userAgentData.getHighEntropyValues(['fullVersionList', 'platform']);
        const brands = (identity.fullVersionList ?? []).map(value => value.brand + ' ' + value.version).join(', ');
        return (identity.platform ?? 'unknown platform') + ': ' + (brands || 'no brands');
    } catch { return 'identity unavailable'; }
}
export async function admitNativeClock() {
    if (phase !== Phase.fresh) throw new Error('Native clock admission is not repeatable.');
    phase = Phase.admitting; // Reserve this generation before the asynchronous evidence lookup.
    try {
        console.info('Native clock source: ' + await describeSource());
        qualify(read());
        phase = Phase.ready;
    } catch (error) { phase = Phase.retired; throw error; }
}
export function nativeNow() {
    if (phase !== Phase.ready) throw new Error('Native clock has not qualified.');
    const current = read();
    return current - qualifiedAt > 250 ? qualify(current) : current;
}
export function nativeClockEvidence() {
    return [nativeProfile, globalThis.isSecureContext ? 1 : 0, globalThis.crossOriginIsolated ? 1 : 0,
        qualifiedAt, witnessDelta, renewals, last];
}
