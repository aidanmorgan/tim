// Browserless runner fixture: a test that never settles while a child `sleep` (standing in for Chrome) keeps running. The runner's
// wall limit must kill the whole process tree, the sleep included.
import { spawn } from 'node:child_process';
import { test } from 'node:test';

test('hangs with a child process', { timeout: 600000 }, async () => {
    const child = spawn('sleep', ['600'], { stdio: 'ignore' });
    console.log(`fixture child ${child.pid}`);
    await new Promise(() => {});
});
