// Browserless runner fixture: the second of three tests fails one assertion.
import assert from 'node:assert/strict';
import { test } from 'node:test';

test('first passes', () => assert.equal(1, 1));
test('second fails', () => assert.equal(1, 2));
test('third passes', () => assert.equal(3, 3));
