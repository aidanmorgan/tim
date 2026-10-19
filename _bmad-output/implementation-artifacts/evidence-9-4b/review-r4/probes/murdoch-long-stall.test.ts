// Murdoch-2 round 4 reviewer probe (Story 9.4b candidate 5; not part of the deliverable). The renewal's consistency check
// widens the old mapping by 500 ppm of its age with no bound on the stall, so its tolerance grows ~1 ms per 2 s stalled.
// Attack: a 6 s Browser main-thread stall, then the Browser clock jumps 3 ms (1.5 x MaximumMappedWidth). Control: the
// same 3 ms jump after the 700 ms stall of the Chrome proof. Both outcomes are recorded, nothing is asserted about which
// is intended; the record is the evidence.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

const URL = process.env.MURDOCH_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
const AFTER_READY_MS = 500, JUMP_MS = 3;

async function stallThenJump(driver: WorkshopDriver, stallMs: number, jumpMs: number): Promise<void> {
    await driver.page.evaluate(`(() => {
        const end = performance.now() + ${stallMs};
        while (performance.now() < end) { }
        const original = performance.now.bind(performance);
        performance.now = () => original() + ${jumpMs};
    })()`);
}

async function until(condition: () => boolean, timeoutMs: number, driver: WorkshopDriver): Promise<void> {
    for (const deadline = Date.now() + timeoutMs; !condition() && Date.now() < deadline;) await driver.page.waitForTimeout(25);
}

for (const stallMs of [6000, 700]) {
    test(`Browser stall ${stallMs} ms then a ${JUMP_MS} ms clock jump`, { timeout: 120000 }, async () => {
        const driver = await WorkshopDriver.launch({ baseUrl: URL, allowClockRecalibrations: true });
        try {
            await driver.page.waitForTimeout(AFTER_READY_MS);
            await stallThenJump(driver, stallMs, JUMP_MS);
            await until(() => driver.clockLateRenewals.length + driver.clockRecalibrations.length > 0, 4000, driver);
            await driver.page.waitForTimeout(1000);
            const renewals = driver.clockLateRenewals.map(line => JSON.parse(line.slice(line.indexOf(' ') + 1)));
            const recalibrations = driver.clockRecalibrations.map(line => JSON.parse(line.slice(line.indexOf(' ') + 1)))
                .map(r => ({ role: r.role, causeName: r.causeName, stateBefore: r.stateBefore, recoverySends: r.recoverySends }));
            const tolerance = renewals.map(r => ({
                lapseAgeMs: Number(r.lapseAge) / 1e6, exchanges: r.exchanges,
                oldWidthMs: (Number(r.oldUpper) - Number(r.oldLower)) / 1e6, newWidthMs: (Number(r.newUpper) - Number(r.newLower)) / 1e6,
                centreShiftMs: ((Number(r.newUpper) + Number(r.newLower)) - (Number(r.oldUpper) + Number(r.oldLower))) / 2e6,
            }));
            console.log('MURDOCH_LONG_STALL ' + JSON.stringify({ stallMs, jumpMs: JUMP_MS, renewals: renewals.length, tolerance, recalibrations }));
            assert.ok(renewals.length + recalibrations.length > 0, 'the stall lapsed the Browser mapping');
        } finally { await driver.close(); }
    });
}
