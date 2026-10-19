// Diagnosis probe (Story 9.4b, not a deliverable): repeats the cat-023b wired-chain Run/Reset cycle and records every
// non-read console line with a timestamp, so a chain that fails to solve can be attributed (debt fault, stall, clock).
import { test } from 'node:test';
import { appendFileSync } from 'node:fs';
import { WorkshopDriver } from '/Users/aidan/dev/personal/tim/tools/e2e/workshop-driver.ts';

const OUT = process.env.PROBE_OUT ?? '/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/probe-023b.log';
const tileScreenX = (x: number) => Math.round(720 + 65.2 * x);
const log = (line: string) => appendFileSync(OUT, `${new Date().toISOString()} ${line}\n`);

test('probe wired chain Run/Reset cycles', { timeout: 600000 }, async () => {
    const driver = await WorkshopDriver.launch();
    const t0 = Date.now();
    driver.page.on('console', message => {
        const text = message.text();
        if (text.startsWith('CCGPU ') || text.startsWith('CCGPU_RETAINED ')) return;
        log(`+${Date.now() - t0}ms [${message.type()}] ${text.slice(0, 2000)}`);
    });
    driver.page.on('pageerror', error => log(`+${Date.now() - t0}ms [pageerror] ${error.message}`));
    try {
        await driver.selectLevel('domino_effect');
        for (const x of [-4, -3, -2, -1]) { await driver.selectTool('domino'); await driver.placeOnCanvas(tileScreenX(x), 676); }
        await driver.connectActivation({ x: tileScreenX(0.1), y: 676, panel: 'locked' }, { x: 915, y: 612 });
        await driver.save();
        for (let cycle = 1; cycle <= Number(process.env.PROBE_CYCLES ?? 8); cycle++) {
            if (process.env.PROBE_RELOAD === '1') { await driver.reload(); await driver.selectLevel('domino_effect'); await driver.load(); log(`cycle ${cycle}: reloaded`); }
            for (const leg of ['first', 'after-reset']) {
                const before = await driver.readCaptured();
                const started = Date.now();
                await driver.run();
                let solved = false;
                while (Date.now() - started < 12000) {
                    if ((await driver.readCaptured()) > before) { solved = true; break; }
                    await driver.page.waitForTimeout(50);
                }
                const pose = await driver.readLatestPose();
                log(`cycle ${cycle} ${leg}: solved=${solved} after ${Date.now() - started}ms; pose seq=${pose?.sequence} bodies=${pose?.bodies.map(b => `${b.id}:${b.px.toFixed(2)},${b.py.toFixed(2)}`).join(' ')}`);
                await driver.reset(800);
            }
        }
    } finally { await driver.close(); }
});
