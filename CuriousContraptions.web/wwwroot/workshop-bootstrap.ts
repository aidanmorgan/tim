interface EngineConfiguration { args: string[]; canvasResizePolicy: number; ensureCrossOriginIsolationHeaders: boolean; executable: string; experimentalVK: boolean; focusCanvas: boolean; gdextensionLibs: string[]; fileSizes?: Record<string, number>; onProgress?: (current: number, total: number) => void; }
interface GameEngine { requestQuit(): void; init(executable: string): Promise<void>; preloadFile(source: string, destination: string): Promise<void>; start(options: { args: string[] }): Promise<void>; }
declare const Engine: { new(config: EngineConfiguration): GameEngine; getMissingFeatures(options: { threads: boolean }): string[]; unload(): void; };
enum StatusMode { Initial, Hidden, Progress, Notice }
export enum BootstrapFailureReason { ManifestUnavailable = 1, ManifestInvalid }
export class BootstrapFailure extends Error {
    constructor(readonly reason: BootstrapFailureReason, message: string, cause?: unknown) {
        super(message, { cause }); this.name = 'BootstrapFailure';
    }
}
async function loadManifest(): Promise<Record<string, number>> {
    let response: Response;
    try { response = await fetch('twodog.sizes.json'); }
    catch (cause) { throw new BootstrapFailure(BootstrapFailureReason.ManifestUnavailable, 'StartupFailed: download manifest is unavailable.', cause); }
    if (!response.ok) throw new BootstrapFailure(BootstrapFailureReason.ManifestUnavailable, 'StartupFailed: download manifest is unavailable.');
    let value: unknown;
    try { value = await response.json(); }
    catch (cause) { throw new BootstrapFailure(BootstrapFailureReason.ManifestInvalid, 'StartupFailed: download manifest is invalid.', cause); }
    if (value === null || typeof value !== 'object' || Array.isArray(value) || Object.keys(value).length === 0)
        throw new BootstrapFailure(BootstrapFailureReason.ManifestInvalid, 'StartupFailed: download manifest is invalid.');
    const sizes: Record<string, number> = {};
    for (const [name, size] of Object.entries(value)) {
        if (name.length === 0 || typeof size !== 'number' || !Number.isSafeInteger(size) || size < 0)
            throw new BootstrapFailure(BootstrapFailureReason.ManifestInvalid, 'StartupFailed: download manifest is invalid.');
        sizes[name] = size;
    }
    return sizes;
}
import { prepareIsolation, waitForQualification, stopIsolation, requireIsolation } from './workshop-isolation.js';
const GODOT_CONFIG: EngineConfiguration = {
    'args': [],
    'canvasResizePolicy': 2,
    'ensureCrossOriginIsolationHeaders': false,
    'executable': 'godot',
    'experimentalVK': false,
    'focusCanvas': true,
    'gdextensionLibs': []
};
const GODOT_THREADS_ENABLED = false;
(async function () {
    performance.mark('2dog:boot');

    const statusOverlay = document.getElementById('status')!;
    const statusProgress = document.getElementById('status-progress') as HTMLProgressElement;
    const statusNotice = document.getElementById('status-notice')!;

    let initializing = true;
    let statusMode = StatusMode.Initial;

    function setStatusMode(mode: StatusMode) {
        if (statusMode === mode || !initializing) {
            return;
        }
        if (mode === StatusMode.Hidden) {
            statusOverlay.remove();
            initializing = false;
            return;
        }
        statusProgress.style.display = mode === StatusMode.Progress ? 'block' : 'none';
        statusNotice.style.display = mode === StatusMode.Notice ? 'block' : 'none';
        statusMode = mode;
    }

    function displayFailureNotice(err: unknown) {
        console.error(err);
        if (!statusOverlay.isConnected) document.body.appendChild(statusOverlay);
        initializing = true;
        statusMode = StatusMode.Initial;
        const description = err instanceof Error ? err.message : String(err);
        statusNotice.textContent = typeof description === 'string' && description.trim().length > 0
            ? description : 'Workshop stopped because an error did not include a description. Reload the page to try again.';
        setStatusMode(StatusMode.Notice);
        initializing = false;
    }

    let downloadsDone = false;

    function reportProgress(current: number, total: number) {
        if (!downloadsDone && total > 0 && current >= total) {
            downloadsDone = true;
            performance.mark('2dog:downloads-done');
        }
        // Determinate while the size-tracked downloads run, then back
        // to indeterminate for the tail (.NET runtime files and engine
        // startup have no known total).
        if (current > 0 && total > 0 && current < total) {
            statusProgress.value = current;
            statusProgress.max = total;
        } else {
            statusProgress.removeAttribute('value');
            statusProgress.removeAttribute('max');
        }
    }

    function reportTimings() {
        performance.mark('2dog:first-frame');
        const total = performance.measure('2dog:total', '2dog:boot', '2dog:first-frame').duration;
        let text = '2dog: first frame ' + Math.round(total) + ' ms after boot';
        if (downloadsDone) {
            const tail = performance.measure('2dog:tail', '2dog:downloads-done', '2dog:first-frame').duration;
            text += ' (' + Math.round(total - tail) + ' ms downloading, ' + Math.round(tail) + ' ms starting)';
        }
        console.log(text);
        // Detail table on demand (?2dog-timing). transferSize reads 0
        // for memory-cache hits - measure with caching disabled.
        if (!new URLSearchParams(location.search).has('2dog-timing')) {
            return;
        }
        const rows = (performance.getEntriesByType('resource') as PerformanceResourceTiming[])
            .filter((entry) => entry.transferSize > 100000)
            .sort((a, b) => b.transferSize - a.transferSize)
            .map((entry) => ({
                file: entry.name.replace(/.*\//, ''),
                'wire kB': Math.round(entry.transferSize / 1000),
                ms: Math.round(entry.duration),
            }));
        if (rows.length) console.table(rows);
    }

    const missing = Engine.getMissingFeatures({
        threads: GODOT_THREADS_ENABLED,
    });

    if (missing.length !== 0) {
        const missingMsg = 'Error\nThe following features required to run Godot projects on the Web are missing:\n';
        displayFailureNotice(missingMsg + missing.join('\n'));
        return;
    }

    let activeEngine: GameEngine | undefined;
    function failStartup(error: unknown) {
        const retiring = activeEngine;
        activeEngine = undefined;
        try { retiring?.requestQuit(); } catch (cleanupError) { console.error(cleanupError); }
        try { Engine.unload(); } catch (cleanupError) { console.error(cleanupError); }
        displayFailureNotice(error);
    }
    if (!await prepareIsolation(failStartup)) return;

    // Published uncompressed sizes define the one download-progress path.
    loadManifest()
        .then(async (fileSizes) => {
            requireIsolation();
            GODOT_CONFIG['fileSizes'] = fileSizes;
            GODOT_CONFIG['onProgress'] = reportProgress;
            const engine = new Engine(GODOT_CONFIG);
            activeEngine = engine;
            setStatusMode(StatusMode.Progress);
            // Use the public manual initialization boundary so timed-out downloads cannot callMain later.
            await Promise.all([
                engine.init(GODOT_CONFIG['executable']),
                engine.preloadFile('godot.pck', 'godot.pck')
            ]);
            requireIsolation();
            await engine.start({ 'args': ['--main-pack', 'godot.pck'].concat(GODOT_CONFIG['args']) });
            requireIsolation();
            await waitForQualification();
            reportTimings();
            setStatusMode(StatusMode.Hidden);
        })
        .catch(stopIsolation);
}());
