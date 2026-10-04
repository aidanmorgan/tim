const policyVersion = 1;
const Phase = Object.freeze({ waiting: 0, booting: 1, qualified: 2, failed: 3 });
const scope = new URL('./', document.baseURI);
const script = new URL('workshop-isolation-worker.js', scope);
const markerKey = 'workshop-isolation:' + scope.pathname;
let phase = Phase.waiting, controller, deadline = 0, lastNavigation = 0, timer, pendingCleanup, showFailure, retire;
let resolveQualified, rejectQualified, rejectFailure;
const qualification = new Promise((resolve, reject) => { resolveQualified = resolve; rejectQualified = reject; });
const failure = new Promise((_, reject) => { rejectFailure = reject; });
qualification.catch(() => {}); failure.catch(() => {});
function navigationTime() {
    const value = performance.timeOrigin + performance.now();
    if (!Number.isFinite(value) || value <= 0 || value < lastNavigation)
        throw new Error('Navigation deadline clock is unavailable or reversed.');
    lastNavigation = value;
    return value;
}
export function stopIsolation(error) {
    if (phase === Phase.failed) return;
    phase = Phase.failed;
    clearTimeout(timer);
    navigator.serviceWorker?.removeEventListener('controllerchange', changedController);
    const cleanup = pendingCleanup; pendingCleanup = undefined; cleanup?.(error);
    try { sessionStorage.removeItem(markerKey); } catch { /* Preserve the original terminal cause. */ }
    rejectFailure(error); rejectQualified(error);
    const callback = retire; retire = undefined; callback?.(error);
    showFailure?.(error);
}
function checkDeadline() {
    if (phase === Phase.failed) throw new Error('Workshop isolation startup is retired.');
    if (navigationTime() > deadline) throw new Error('StartupFailed: navigation-to-usable exceeded five seconds.');
}
function changedController() {
    if (phase !== Phase.waiting && navigator.serviceWorker.controller !== controller)
        stopIsolation(new Error('Workshop isolation controller changed; reload is required.'));
}
function bounded(operation) { return Promise.race([operation, failure]); }
function waitForController() {
    if (navigator.serviceWorker.controller) return Promise.resolve(navigator.serviceWorker.controller);
    return new Promise((resolve, reject) => {
        const finish = error => {
            navigator.serviceWorker.removeEventListener('controllerchange', changed);
            pendingCleanup = undefined;
            if (error) reject(error); else resolve(navigator.serviceWorker.controller);
        };
        const changed = () => { if (navigator.serviceWorker.controller) finish(); };
        pendingCleanup = finish;
        navigator.serviceWorker.addEventListener('controllerchange', changed);
        changed();
    });
}
function waitForInstall(worker) {
    if (!worker || worker.state === 'installed' || worker.state === 'activated') return Promise.resolve();
    return new Promise((resolve, reject) => {
        const finish = error => {
            worker.removeEventListener('statechange', changed);
            pendingCleanup = undefined;
            if (error) reject(error); else resolve();
        };
        const changed = () => {
            if (worker.state === 'redundant') finish(new Error('Workshop isolation worker installation failed.'));
            else if (worker.state === 'installed' || worker.state === 'activated') finish();
        };
        pendingCleanup = finish;
        worker.addEventListener('statechange', changed);
        changed();
    });
}
function handshake(expected) {
    return new Promise((resolve, reject) => {
        const channel = new MessageChannel();
        const finish = error => {
            channel.port1.onmessage = null;
            channel.port1.onmessageerror = null;
            channel.port1.close(); channel.port2.close();
            pendingCleanup = undefined;
            if (error) reject(error); else resolve();
        };
        pendingCleanup = finish;
        channel.port1.onmessage = event => finish(
            event.data?.policyVersion !== policyVersion || navigator.serviceWorker.controller !== expected
                ? new Error('Unsupported Workshop isolation policy.') : undefined);
        channel.port1.onmessageerror = () => finish(new Error('Invalid isolation policy response.'));
        try { expected.postMessage({ policyRequest: policyVersion }, [channel.port2]); }
        catch (error) { finish(error); }
    });
}
export async function prepareIsolation(displayFailure) {
    showFailure = displayFailure;
    try {
        if (!globalThis.isSecureContext || !navigator.serviceWorker)
            throw new Error('Unsupported: secure service-worker isolation is required.');
        const saved = sessionStorage.getItem(markerKey);
        if (saved !== null && saved.length > 96) throw new Error('Invalid isolation startup attempt size.');
        const attempt = saved === null ? [policyVersion, performance.timeOrigin, 0, performance.timeOrigin] : JSON.parse(saved);
        if (!Array.isArray(attempt) || attempt.length !== 4 || attempt[0] !== policyVersion ||
            !Number.isFinite(attempt[1]) || attempt[1] <= 0 || (attempt[2] !== 0 && attempt[2] !== 1) ||
            !Number.isFinite(attempt[3]) || attempt[3] < attempt[1]) throw new Error('Invalid isolation startup attempt.');
        lastNavigation = attempt[3];
        deadline = attempt[1] + 5000;
        checkDeadline();
        attempt[3] = lastNavigation;
        sessionStorage.setItem(markerKey, JSON.stringify(attempt));
        timer = setTimeout(() => stopIsolation(new Error('StartupFailed: navigation-to-usable exceeded five seconds.')),
            Math.max(0, deadline - navigationTime()));
        navigator.serviceWorker.addEventListener('controllerchange', changedController);
        const registration = await bounded(navigator.serviceWorker.register(script.href,
            { scope: scope.href, updateViaCache: 'none' }));
        if (registration.scope !== scope.href) throw new Error('Unsupported isolation registration scope.');
        if (navigator.serviceWorker.controller) {
            await bounded(registration.update());
            await bounded(waitForInstall(registration.installing));
        }
        controller = await bounded(waitForController());
        if (controller.scriptURL !== script.href || registration.waiting)
            throw new Error('A different or waiting isolation policy requires closing all game tabs and reopening.');
        await bounded(handshake(controller));
        checkDeadline();
        if (registration.waiting) throw new Error('A waiting isolation policy requires closing all game tabs and reopening.');
        if (!globalThis.crossOriginIsolated) {
            if (attempt[2] !== 0) throw new Error('Unsupported: the isolated reload did not establish isolation.');
            sessionStorage.setItem(markerKey, JSON.stringify([policyVersion, attempt[1], 1, lastNavigation]));
            location.reload();
            return false;
        }
        phase = Phase.booting;
        return true;
    } catch (error) { stopIsolation(error); return false; }
}
export function requireIsolation() {
    checkDeadlineUnlessQualified();
    if ((phase !== Phase.booting && phase !== Phase.qualified) || !globalThis.crossOriginIsolated ||
        navigator.serviceWorker.controller !== controller)
        throw new Error('Workshop isolation policy is unavailable or changed.');
}
function checkDeadlineUnlessQualified() { if (phase !== Phase.qualified) checkDeadline(); }
export function watchIsolation(callback) {
    requireIsolation();
    if (retire) throw new Error('Workshop isolation already has a transport owner.');
    retire = callback;
    return () => { if (retire === callback) retire = undefined; };
}
export function isolationQualified() {
    requireIsolation();
    sessionStorage.removeItem(markerKey);
    phase = Phase.qualified;
    clearTimeout(timer);
    resolveQualified();
}
export function waitForQualification() { return qualification; }
export function isolationEvidence() {
    return { policyVersion, phase, isolated: globalThis.crossOriginIsolated === true,
        controlled: navigator.serviceWorker.controller === controller, scope: scope.href };
}
