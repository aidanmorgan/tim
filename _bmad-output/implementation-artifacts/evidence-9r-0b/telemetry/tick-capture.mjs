// Story 9R.0b telemetry only (OD2: advisory, never a gate). Preloaded with `node --import <tsx> --import ./tick-capture.mjs --test <suite>`
// against a manually started tools/Preview; the E2E runner clears NODE_OPTIONS and never loads this file. It records every
// CCGPU_TICK_TIMES_* console line and each driver open-to-ready time to TICK_CAPTURE_OUT (JSON lines), leaving the suite unchanged.
import { appendFileSync } from 'node:fs';
import { basename } from 'node:path';
import { WorkshopDriver } from '../../../../tools/e2e/workshop-driver.ts';

const out = process.env.TICK_CAPTURE_OUT;
if (!out) throw new Error('TICK_CAPTURE_OUT is required');
const suite = basename(process.argv.at(-1) ?? 'unknown');
const record = entry => appendFileSync(out, JSON.stringify({ suite, ...entry }) + '\n');

const observe = WorkshopDriver.prototype.observeConsole;
WorkshopDriver.prototype.observeConsole = function (text) {
    if (text.startsWith('CCGPU_TICK_TIMES_')) record({ text });
    return observe.call(this, text);
};
const launch = WorkshopDriver.launch;
WorkshopDriver.launch = async function (...args) {
    const started = performance.now();
    const driver = await launch.apply(this, args);
    record({ openToReadyMs: performance.now() - started });
    return driver;
};
