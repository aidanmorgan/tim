// Scratch probe (not a deliverable): do ordinary page loads log console, page, worker or web errors?
import { test } from 'node:test';
import type { ConsoleMessage, Worker } from 'playwright';
import { WorkshopDriver } from '/Users/aidan/dev/personal/tim/.claude/worktrees/agent-a67f1ccf03b1c80b5/tools/e2e/workshop-driver.ts';

test('boot console probe', { timeout: 300000 }, async () => {
    const errors: string[] = [];
    const watch = (source: string) => (message: ConsoleMessage): void => {
        if (message.type() === 'error') errors.push(`${source}: ${message.text().slice(0, 300)}`);
    };
    const driver = await WorkshopDriver.launch();
    try {
        driver.page.on('console', watch('page'));
        driver.page.on('pageerror', (error: Error) => errors.push(`pageerror: ${String(error).slice(0, 300)}`));
        driver.page.on('worker', (worker: Worker) => worker.on('console', watch(`worker ${worker.url()}`)));
        driver.context.on('weberror', webError => errors.push(`weberror: ${String(webError.error()).slice(0, 300)}`));
        for (let i = 0; i < 3; i++) await driver.reload();
        console.log(`BOOT_CONSOLE_ERRORS ${errors.length}`);
        for (const line of errors) console.log(`BOOT_ERROR ${line}`);
    } finally {
        await driver.close();
    }
});
