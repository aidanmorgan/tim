// Browserless runner fixture: the test process dies by a signal in the middle of its second test.
import assert from 'node:assert/strict';
import { test } from 'node:test';

test('first passes', () => assert.equal(1, 1));
test('second crashes the process', () => { process.kill(process.pid, 'SIGKILL'); });
