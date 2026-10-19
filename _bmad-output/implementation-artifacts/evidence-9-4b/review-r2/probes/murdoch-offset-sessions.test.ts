// Murdoch-2 round 2 (Story 9.4b candidate 3) reviewer probe: the D14 startup time-origin offset under host load.
// Launches MURDOCH_OFFSET_SESSIONS plain sessions in a row (the caller applies any CPU load) and records, for each, the
// typed Startup derivation record read from the page console, or the refusal. A refused startup never reaches
// WorkshopDriver.close(), so the implementer's CLOCK_DERIVATION_LOG distribution cannot contain it; this probe counts it.
import assert from 'node:assert/strict';
import { appendFileSync } from 'node:fs';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

const URL = process.env.MURDOCH_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
const SESSIONS = Number(process.env.MURDOCH_OFFSET_SESSIONS ?? 30);
const LABEL = process.env.MURDOCH_OFFSET_LABEL ?? 'load0';
const OUT = process.env.MURDOCH_OFFSET_OUT;

test(`Murdoch startup offset over ${SESSIONS} sessions (${LABEL})`, { timeout: SESSIONS * 60000 }, async () => {
    assert.ok(OUT, 'MURDOCH_OFFSET_OUT names the JSONL output');
    let refused = 0;
    for (let session = 1; session <= SESSIONS; session++) {
        const started = Date.now();
        let record: unknown = null, failure: string | null = null;
        try {
            const driver = await WorkshopDriver.launch({ baseUrl: URL });
            const line = driver.clockDerivations.find(text => text.includes('"reportName":"Startup"'));
            record = line ? JSON.parse(line.slice('CCGPU_CLOCK_DERIVATION '.length)) : null;
            await driver.close();
        } catch (error) {
            refused++;
            failure = (error instanceof Error ? error.message : String(error)).slice(0, 600);
        }
        appendFileSync(OUT!, JSON.stringify({ label: LABEL, session, ms: Date.now() - started, record, failure }) + '\n');
    }
    console.log(`MURDOCH_OFFSET ${LABEL} sessions=${SESSIONS} refused=${refused}`);
    assert.equal(refused, 0, `${refused} of ${SESSIONS} sessions failed to start (${LABEL})`);
});
