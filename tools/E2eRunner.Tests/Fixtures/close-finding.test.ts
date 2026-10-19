// Browserless runner fixture: every test passes, and a session close finding is raised from the suite's after hook through the real
// close-findings module (as WorkshopDriver.closeAfterSuite does), so the file exits 1.
import assert from 'node:assert/strict';
import { after, describe, test } from 'node:test';
import { raiseCloseFindings } from '../../workshop-close-findings.mjs';

describe('a suite whose session closes with a finding', () => {
    after(() => raiseCloseFindings([new Error('The session recalibrated a clock mapping 1 time(s); first: fixture')], 'after the suite'));
    test('passes', () => assert.equal(1, 1));
});
