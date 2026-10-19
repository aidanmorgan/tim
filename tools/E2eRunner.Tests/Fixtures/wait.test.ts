// Browserless runner fixture: waits (polling, never spinning) until the test creates a `release` file in the run's output
// directory, or 60 s pass, so a test can change the bundle or the suite sources while files are running.
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { test } from 'node:test';
import { setTimeout as delay } from 'node:timers/promises';

test('waits for release', { timeout: 120000 }, async () => {
    const release = join(dirname(process.env.CLOCK_DERIVATION_LOG ?? '.'), 'release');
    for (let polls = 0; polls < 600 && !existsSync(release); polls++) await delay(100);
    assert.ok(existsSync(release), 'released');
});
