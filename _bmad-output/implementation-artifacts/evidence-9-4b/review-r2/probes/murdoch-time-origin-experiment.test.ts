// Murdoch-2 round 2 reviewer experiment (not part of the deliverable): does Chrome's performance.timeOrigin of two
// cross-origin-isolated dedicated workers, created a variable gap apart, agree as one clock? clock-master.js relies on
// exactly this (master timeOrigin - Simulation timeOrigin + master now == Simulation now).
// Method: per round, A stamps a1 and wakes B; B stamps b and wakes A; A stamps a2. Each stamp is timeOrigin + now().
// offset = b - (a1 + a2) / 2, bounded by +-(a2 - a1) / 2; the narrowest of the rounds per pair is kept. The page also
// records Date.now() - (timeOrigin + now()) before and after, which drifts when the OS slews the wall clock.
import assert from 'node:assert/strict';
import { appendFileSync } from 'node:fs';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

const PREVIEW = process.env.MURDOCH_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
const OUT = process.env.MURDOCH_TIME_ORIGIN_OUT;
const GAPS = (process.env.MURDOCH_TIME_ORIGIN_GAPS ?? '0,50,500,2000').split(',').map(Number);
const PAIRS = Number(process.env.MURDOCH_TIME_ORIGIN_PAIRS ?? 8);
const ROUNDS = 300;

// Signal words: 0 go (main -> A), 1 A stamped a1 (A -> B), 2 B stamped (B -> A), 3 A done (A -> main). Value = round + 1.
const WORKER_SOURCE = `
const until = (w, i, v) => { for (;;) { const x = Atomics.load(w, i); if (x === v) return; Atomics.wait(w, i, x, 1000); } };
onmessage = event => {
  const { sab, role, rounds } = event.data;
  const w = new Int32Array(sab, 0, 4), s = new Float64Array(sab, 64);
  const stamp = () => performance.timeOrigin + performance.now();
  postMessage({ isolated: crossOriginIsolated, timeOrigin: performance.timeOrigin });
  for (let r = 1; r <= rounds; r++) {
    if (role === 'A') {
      until(w, 0, r); s[(r - 1) * 3] = stamp(); Atomics.store(w, 1, r); Atomics.notify(w, 1);
      until(w, 2, r); s[(r - 1) * 3 + 2] = stamp(); Atomics.store(w, 3, r); Atomics.notify(w, 3);
    } else {
      until(w, 1, r); s[(r - 1) * 3 + 1] = stamp(); Atomics.store(w, 2, r); Atomics.notify(w, 2);
    }
  }
};`;

test('Murdoch experiment: worker timeOrigin agreement as a function of the creation gap', { timeout: 1800000 }, async () => {
    assert.ok(OUT, 'MURDOCH_TIME_ORIGIN_OUT names the JSONL output');
    const driver = await WorkshopDriver.launch({ baseUrl: PREVIEW });
    try {
        for (const gap of GAPS) for (let pair = 0; pair < PAIRS; pair++) {
            // A plain source string: tsx keepNames would wrap named inner functions in __name(), which the page lacks.
            const pageSource = `(async () => {
                const source = ${JSON.stringify(WORKER_SOURCE)}, gap = ${gap}, rounds = ${ROUNDS};
                const url = URL.createObjectURL(new Blob([source], { type: 'text/javascript' }));
                const sab = new SharedArrayBuffer(64 + rounds * 3 * 8);
                const w = new Int32Array(sab, 0, 4), s = new Float64Array(sab, 64);
                const start = role => new Promise(resolve => {
                    const worker = new Worker(url);
                    worker.onmessage = event => resolve({ worker, info: event.data });
                    worker.postMessage({ sab, role, rounds });
                });
                const wallBefore = Date.now() - (performance.timeOrigin + performance.now());
                const a = await start('A');
                await new Promise(resolve => setTimeout(resolve, gap));
                const b = await start('B');
                let offset = NaN, half = Infinity, completed = 0;
                for (let r = 1; r <= rounds; r++) {
                    Atomics.store(w, 0, r); Atomics.notify(w, 0);
                    const deadline = performance.now() + 100;
                    while (Atomics.load(w, 3) !== r && performance.now() < deadline) { }
                    if (Atomics.load(w, 3) !== r) break;
                    completed++;
                    const a1 = s[(r - 1) * 3], bb = s[(r - 1) * 3 + 1], a2 = s[(r - 1) * 3 + 2];
                    if ((a2 - a1) / 2 < half) { half = (a2 - a1) / 2; offset = bb - (a1 + a2) / 2; }
                }
                const wallAfter = Date.now() - (performance.timeOrigin + performance.now());
                a.worker.terminate(); b.worker.terminate(); URL.revokeObjectURL(url);
                return { gap, completed, isolatedA: a.info.isolated, isolatedB: b.info.isolated,
                    originGapMs: b.info.timeOrigin - a.info.timeOrigin, offsetUs: offset * 1000, halfWidthUs: half * 1000,
                    wallMinusMonotonicDriftMs: wallAfter - wallBefore };
            })()`;
            const result = await (driver.page as unknown as { evaluate(expression: string): Promise<{ completed: number } & Record<string, unknown>> })
                .evaluate(pageSource);
            appendFileSync(OUT!, JSON.stringify({ pair, ...result }) + '\n');
            assert.equal(result.completed, ROUNDS, `pair ${pair} gap ${gap}: every ping-pong round completed`);
        }
    } finally { await driver.close(); }
});
