// Browserless runner fixture: the file cannot load (a missing module), so no test runs.
import { test } from 'node:test';
import { missing } from './does-not-exist.ts';

test('never runs', () => missing());
