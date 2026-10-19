// Murdoch-2 (Story 9.4b adversarial review) shared probe helpers. Read-only observation of the page and its workers;
// every construction and lifecycle action goes through WorkshopDriver's actual UI controls. Copy this directory into
// tools/e2e/ beside workshop-driver.ts before running (imports are relative to tools/e2e).
import assert from 'node:assert/strict';
import type { ConsoleMessage, Worker as PlaywrightWorker } from 'playwright';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeWorkshopResponse, type BodyRead, type WorkshopResponse } from './workshop-wire.ts';

export const PREVIEW_URL = process.env.MURDOCH_PREVIEW_URL ?? 'http://127.0.0.1:8060/';

// Session termination: any of these ends the session or marks a transport/clock fault. A recalibration is NOT here; it is
// counted separately as typed telemetry (CCGPU_CLOCK_RECALIBRATED).
const TERMINATION = [
    /CCGPU_TRANSPORT_FAILURE/, /CCGPU_STARTUP_EXCEPTION/, /Unhandled exception/, /Worker fault/, /Clock master/,
    /master reply missing/, /Clock mapping recovery exhausted/, /CCGPU_CLOCK_RECOVERY_FAULT/, /Clock slot left/,
    /left a probe unanswered/, /rejected a probe/, /Failed to resize vector/, /SimulationUnresponsive/,
    /CCGPU_CLOCK_REPLY_REJECTED/, /derivation epoch reversed/,
];
// Only the game's own contexts count as session workers (review fix, Story 9.4b terminal review: the probe's own blob
// hog workers were counted as closed game workers, as Amelia reported).
const GAME_WORKER = /\/simulation\/worker\.js|\/animation\/worker\.js|\/simulation\/clock-master\.js/;

export interface Recalibration { at: number; role: number | null; cause: number | null; count: number | null; raw: string }
export interface DebtFault { at: number; committedTick: number; reliableStalled: boolean }

/** Console/page/worker observation for one page. Times are ms since `mark()` (or launch). */
export class SessionWatch {
    readonly terminations: { at: number; text: string }[] = [];
    readonly errors: { at: number; text: string }[] = [];
    readonly recalibrations: Recalibration[] = [];
    readonly legacyClockFaults: string[] = [];
    readonly debtFaults: DebtFault[] = [];
    readonly tickFailures: string[] = [];
    readonly solved: { at: number; text: string }[] = [];
    readonly reads: WorkshopResponse[] = [];
    readonly retained: WorkshopResponse[] = [];
    readonly workerUrls: string[] = [];
    readonly closedWorkers: { at: number; url: string }[] = [];
    private origin = Date.now();

    constructor(readonly driver: WorkshopDriver) {
        const page = driver.page;
        page.on('console', (message: ConsoleMessage) => this.observe(message.type(), message.text()));
        page.on('pageerror', error => { const t = this.now(); this.errors.push({ at: t, text: error.message }); this.terminations.push({ at: t, text: 'pageerror: ' + error.message }); });
        for (const worker of page.workers()) this.track(worker);
        page.on('worker', worker => this.track(worker));
    }

    now(): number { return Date.now() - this.origin; }
    mark(): void { this.origin = Date.now(); }

    private track(worker: PlaywrightWorker): void {
        this.workerUrls.push(worker.url());
        worker.on('close', () => { if (GAME_WORKER.test(worker.url())) this.closedWorkers.push({ at: this.now(), url: worker.url() }); });
    }

    private observe(type: string, text: string): void {
        const at = this.now();
        if (text.startsWith('CCGPU ')) {
            try { this.reads.push(decodeWorkshopResponse(Buffer.from(text.slice(6), 'base64'))); } catch { this.errors.push({ at, text: 'undecodable CCGPU line' }); }
        }
        if (text.startsWith('CCGPU_RETAINED ')) {
            try { this.retained.push(decodeWorkshopResponse(Buffer.from(text.slice(15), 'base64'))); } catch { this.errors.push({ at, text: 'undecodable CCGPU_RETAINED line' }); }
        }
        if (text.includes('CCGOAL_SOLVED')) this.solved.push({ at, text });
        if (text.startsWith('CCGPU_CLOCK_RECALIBRATED')) {
            let role: number | null = null, cause: number | null = null, count: number | null = null;
            try {
                const json = JSON.parse(text.slice('CCGPU_CLOCK_RECALIBRATED'.length).trim());
                role = typeof json.role === 'number' ? json.role : null;
                cause = typeof json.cause === 'number' ? json.cause : null;
                count = typeof json.count === 'number' ? json.count : (typeof json.count === 'string' ? Number(json.count) : null);
            } catch { /* the raw line is retained; an unparsable record is itself reported by the summary */ }
            this.recalibrations.push({ at, role, cause, count, raw: text.slice(0, 400) });
        }
        if (text.includes('CCGPU_CLOCK_RECOVERY_FAULT')) this.legacyClockFaults.push(text.slice(0, 400));
        const debt = /CCGPU_DEBT_FAULT: committedTick=(\d+) reliableStalled=(True|False)/.exec(text);
        if (debt) this.debtFaults.push({ at, committedTick: Number(debt[1]), reliableStalled: debt[2] === 'True' });
        if (text.includes('CCGPU_TICK_FAILED')) this.tickFailures.push(text);
        if (type === 'error') this.errors.push({ at, text: text.slice(0, 400) });
        if (TERMINATION.some(pattern => pattern.test(text))) this.terminations.push({ at, text: text.slice(0, 400) });
    }

    summary(): object {
        const byRole: Record<string, number> = {}, byCause: Record<string, number> = {};
        for (const r of this.recalibrations) {
            byRole[String(r.role)] = (byRole[String(r.role)] ?? 0) + 1;
            byCause[String(r.cause)] = (byCause[String(r.cause)] ?? 0) + 1;
        }
        return {
            terminations: this.terminations.map(t => `${t.at}ms ${t.text.slice(0, 160)}`),
            recalibrations: this.recalibrations.length, recalibrationByRole: byRole, recalibrationByCause: byCause,
            firstRecalibrationMs: this.recalibrations[0]?.at ?? null,
            unparsableRecalibrations: this.recalibrations.filter(r => r.role === null).length,
            legacyClockFaults: this.legacyClockFaults.length,
            debtFaults: this.debtFaults, tickFailures: this.tickFailures.length,
            errors: this.errors.length, solved: this.solved.length,
            workers: [...new Set(this.workerUrls)], closedWorkers: this.closedWorkers,
        };
    }
}

export type ContextTarget = 'simulation' | 'animation' | 'main';

export function workerFor(driver: WorkshopDriver, target: 'simulation' | 'animation'): PlaywrightWorker {
    const suffix = target === 'simulation' ? '/simulation/worker.js' : '/animation/worker.js';
    const worker = driver.page.workers().find(candidate => candidate.url().includes(suffix));
    assert.ok(worker, `The ${target} worker is running (workers: ${driver.page.workers().map(w => w.url()).join(', ')})`);
    return worker;
}

/** Whether the off-thread clock master context is visible to Playwright. It is never evaluated: it blocks in Atomics.wait. */
export function masterContextVisible(driver: WorkshopDriver): boolean {
    return driver.page.workers().some(candidate => candidate.url().includes('clock-master'));
}

type Evaluator = { evaluate<R, A>(fn: (arg: A) => R | Promise<R>, arg: A): Promise<R> };
function evaluator(driver: WorkshopDriver, target: ContextTarget): Evaluator {
    return (target === 'main' ? driver.page : workerFor(driver, target)) as unknown as Evaluator;
}

/**
 * Installs a synchronous duty cycle in the target context: `busyMs` of spinning, then a yield (MessageChannel when
 * idleMs is 0, so no timer clamp hides the load; otherwise setTimeout(idleMs)). Returns immediately.
 */
export async function startDutyCycle(driver: WorkshopDriver, target: ContextTarget, busyMs: number, idleMs = 0): Promise<void> {
    // A plain source string: tsx's keepNames would wrap named inner functions in __name(), which the page lacks.
    if (!Number.isFinite(busyMs) || !Number.isFinite(idleMs)) throw new Error('Invalid duty cycle.');
    const source = `(() => {
        const g = globalThis; if (g.__murdochStop) g.__murdochStop();
        const channel = new MessageChannel(); let stopped = false, slices = 0;
        channel.port1.onmessage = () => {
            if (stopped) return;
            const end = performance.now() + ${busyMs};
            while (performance.now() < end) { /* injected synchronous work */ }
            slices++;
            if (stopped) return;
            if (${idleMs} > 0) setTimeout(() => { if (!stopped) channel.port2.postMessage(0); }, ${idleMs});
            else channel.port2.postMessage(0);
        };
        g.__murdochStop = () => { stopped = true; channel.port1.close(); return slices; };
        channel.port2.postMessage(0);
        return true;
    })()`;
    await (evaluator(driver, target) as unknown as { evaluate(expression: string): Promise<unknown> }).evaluate(source);
}

export async function stopDutyCycle(driver: WorkshopDriver, target: ContextTarget): Promise<number | null> {
    try {
        return await evaluator(driver, target).evaluate(() => {
            const g = globalThis as unknown as { __murdochStop?: () => number };
            const slices = g.__murdochStop?.() ?? null; g.__murdochStop = undefined; return slices;
        }, undefined);
    } catch { return null; } // a terminated context has nothing to stop; the watch reports that termination
}

/** One synchronous block of `ms` in the target context, scheduled as its own task; resolves once it has run. */
export async function blockOnce(driver: WorkshopDriver, target: ContextTarget, ms: number): Promise<void> {
    await evaluator(driver, target).evaluate((duration: number) => new Promise<void>(resolve => {
        setTimeout(() => { const end = performance.now() + duration; while (performance.now() < end) { /* block */ } resolve(); }, 0);
    }), ms);
}

/** CPU oversubscription: `count` spinning blob workers owned by the page (never the game's workers). */
export async function startHogs(driver: WorkshopDriver, count: number): Promise<void> {
    await driver.page.evaluate((n: number) => {
        const g = globalThis as unknown as { __murdochHogs?: Worker[] };
        const url = URL.createObjectURL(new Blob(['for(;;){}'], { type: 'text/javascript' }));
        g.__murdochHogs = Array.from({ length: n }, () => new Worker(url));
    }, count);
}
export async function stopHogs(driver: WorkshopDriver): Promise<void> {
    await driver.page.evaluate(() => {
        const g = globalThis as unknown as { __murdochHogs?: Worker[] };
        for (const w of g.__murdochHogs ?? []) w.terminate();
        g.__murdochHogs = undefined;
    });
}
export async function hardwareConcurrency(driver: WorkshopDriver): Promise<number> {
    return await driver.page.evaluate(() => navigator.hardwareConcurrency || 4);
}

export async function readSavedBytes(driver: WorkshopDriver): Promise<Buffer> {
    const values = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error('Existing save database unavailable'));
        request.onsuccess = () => {
            const database = request.result;
            const transaction = database.transaction('construction', 'readonly');
            const read = transaction.objectStore('construction').get(1);
            transaction.oncomplete = () => { database.close(); resolve(Array.from(read.result as Uint8Array)); };
            transaction.onabort = () => { database.close(); reject(new Error('Save observation aborted')); };
        };
    }));
    return Buffer.from(values);
}

export function bodyState(b: BodyRead) {
    return {
        id: b.id(),
        cell: { x: b.cell()!.x(), y: b.cell()!.y(), z: b.cell()!.z() },
        local: { x: b.local()!.x(), y: b.local()!.y(), z: b.local()!.z() },
        rotation: { x: b.rotation()!.x(), y: b.rotation()!.y(), z: b.rotation()!.z(), w: b.rotation()!.w() },
        com: { x: b.com()!.x(), y: b.com()!.y(), z: b.com()!.z() },
        velocity: { x: b.velocity()!.x(), y: b.velocity()!.y(), z: b.velocity()!.z() },
        angularVelocity: { x: b.angularVelocity()!.x(), y: b.angularVelocity()!.y(), z: b.angularVelocity()!.z() },
    };
}
export function allBodies(read: WorkshopResponse) {
    const out: ReturnType<typeof bodyState>[] = [];
    for (let i = 0; i < read.bodiesLength(); i++) out.push(bodyState(read.bodies(i)!));
    return out;
}

export async function sleep(driver: WorkshopDriver, ms: number): Promise<void> { await driver.page.waitForTimeout(ms); }

// domino_effect recipe, identical to tools/e2e/cat-023b.test.ts test 1 (front view; tiles on row 676; end Domino locked).
export const CHAIN = {
    BALL_ID: '1', END_ID: '2', TILE_IDS: ['4', '5', '6', '7'], LAMP_TARGET: '5', HALF_ONE: 15360, TILE_Y: 676,
    CHAIN_X: [-4, -3, -2, -1], LAMP_SCREEN: { x: 915, y: 612 },
};
export const tileScreenX = (x: number) => Math.round(720 + 65.2 * x);
export async function buildWiredChain(driver: WorkshopDriver): Promise<void> {
    await driver.selectLevel('domino_effect');
    for (const x of CHAIN.CHAIN_X) {
        await driver.selectTool('domino');
        await driver.placeOnCanvas(tileScreenX(x), CHAIN.TILE_Y);
    }
    await driver.connectActivation({ x: tileScreenX(0.1), y: CHAIN.TILE_Y, panel: 'locked' }, CHAIN.LAMP_SCREEN);
}

export async function waitUntil(fn: () => Promise<boolean> | boolean, timeoutMs: number, stepMs = 50): Promise<boolean> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        if (await fn()) return true;
        await new Promise(resolve => setTimeout(resolve, stepMs));
    }
    return await fn();
}
