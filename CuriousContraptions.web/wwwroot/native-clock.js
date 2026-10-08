// External native boundary for the independently reviewed Chromium154.0.8037.98 Mac profile.
// Fixed rounded context origin is part of the unknown offset. Conservative error84.1255us;
// declaration stays100us. Neither the UA-CH identity nor a small increment alone qualifies.
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
    // Including the caller's anchor, at most4096 native reads. Scheduling gaps do not
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
export async function admitNativeClock() {
    if (phase !== Phase.fresh) throw new Error('Native clock admission is not repeatable.');
    phase = Phase.admitting; // Reserve this generation before the asynchronous external identity lookup.
    try {
        if (!navigator.userAgentData?.getHighEntropyValues) reject('source identity is unavailable.');
        const identity = await navigator.userAgentData.getHighEntropyValues(['fullVersionList', 'platform', 'architecture', 'bitness']);
        if (identity.platform !== 'macOS' || identity.bitness !== '64' ||
            (identity.architecture !== 'arm' && identity.architecture !== 'x86') ||
            !identity.fullVersionList?.some(value =>
                (value.brand === 'Google Chrome' || value.brand === 'Chromium') && value.version === '154.0.8037.98'))
            reject('this browser/native source profile is not yet proven.');
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
