// Browserless runner fixture: two passing tests, and one line for each driver signal the runner counts.
import assert from 'node:assert/strict';
import { test } from 'node:test';

test('first passes', () => {
    for (const signal of ['DebtFault', 'ClockRecalibrated', 'ClockReplyRejected', 'ClockRenewedLate', 'ClockOffsetWide', 'SimulationUnresponsive'])
        console.log(`E2E_OBSERVED ${signal} fixture line`);
    console.log('Session clock late renewals (reported, not a failure): 1; first: fixture');
    assert.equal(1, 1);
});
test('second passes', () => assert.equal(2, 2));
