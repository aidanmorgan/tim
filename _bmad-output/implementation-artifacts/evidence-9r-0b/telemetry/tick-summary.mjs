// Story 9R.0b telemetry only: summarises tick-capture.mjs output. Usage: node tick-summary.mjs <ticks.jsonl>...
// Each CCGPU_TICK_TIMES record (BEGIN, CHUNKs, END of one capture identity) is decoded from little-endian binary64 milliseconds.
import { readFileSync } from 'node:fs';

const quantile = (sorted, q) => sorted[Math.min(sorted.length - 1, Math.ceil(q * sorted.length) - 1)];
for (const file of process.argv.slice(2)) {
    const captures = new Map();
    for (const line of readFileSync(file, 'utf8').split('\n').filter(Boolean)) {
        const entry = JSON.parse(line);
        if (entry.openToReadyMs !== undefined) { console.log(`${entry.suite} openToReadyMs ${entry.openToReadyMs.toFixed(0)}`); continue; }
        const fields = entry.text.split(' ');
        const key = `${entry.suite}#${fields[1]}`;
        if (fields[0] === 'CCGPU_TICK_TIMES_BEGIN')
            captures.set(key, { suite: entry.suite, id: fields[1], status: fields[11], constructionBytes: Buffer.from(fields[12], 'base64').length, ms: [] });
        else if (fields[0] === 'CCGPU_TICK_TIMES_CHUNK') {
            const bytes = Buffer.from(fields[6], 'base64');
            for (let offset = 0; offset < bytes.length; offset += 8) captures.get(key).ms.push(bytes.readDoubleLE(offset));
        }
    }
    for (const capture of captures.values()) {
        if (capture.ms.length === 0) continue;
        const sorted = [...capture.ms].sort((a, b) => a - b);
        console.log(`${capture.suite} capture ${capture.id} constructionBytes ${capture.constructionBytes} ticks ${sorted.length} status ${capture.status}` +
            ` p50 ${quantile(sorted, 0.5).toFixed(3)} p95 ${quantile(sorted, 0.95).toFixed(3)} max ${sorted.at(-1).toFixed(3)} ms`);
    }
}
