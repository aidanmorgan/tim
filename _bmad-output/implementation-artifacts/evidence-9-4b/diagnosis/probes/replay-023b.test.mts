// Diagnosis wrapper (Story 9.4b, not a deliverable): runs the unmodified cat-023b suite while recording every
// non-read console line (debt/tick faults, goal announcements, clock faults) with timestamps.
import { appendFileSync } from 'node:fs';
import { WorkshopDriver } from '/Users/aidan/dev/personal/tim/tools/e2e/workshop-driver.ts';

const OUT = process.env.REPLAY_OUT!;
const launch = WorkshopDriver.launch.bind(WorkshopDriver);
WorkshopDriver.launch = (async (options?: Parameters<typeof WorkshopDriver.launch>[0]) => {
    const driver = await launch(options);
    const t0 = Date.now();
    driver.page.on('console', message => {
        const text = message.text();
        if (text.startsWith('CCGPU ') || text.startsWith('CCGPU_RETAINED ') || text.startsWith('CCGPU_TRACE_')) return;
        appendFileSync(OUT, `+${Date.now() - t0}ms [${message.type()}] ${text.slice(0, 1500)}\n`);
    });
    return driver;
}) as typeof WorkshopDriver.launch;
await import('/Users/aidan/dev/personal/tim/tools/e2e/cat-023b.test.ts');
