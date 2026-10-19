// Murdoch-2 round 3 reviewer probe (not part of the deliverable): the clock-master R2-F1 proof test's exact-replay check
// (copied verbatim below from tools/e2e/clock-master.test.ts) must be able to fail. Control: Reset then Run replays the
// first Run bit-for-bit (passes). Attack: the ball is lifted 20 px with the move gizmo between the Runs (a different
// world); the same check must reject it.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver, type WorkshopBodyPose } from './workshop-driver.ts';

const URL = process.env.MURDOCH_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
const FREE_FALL_MS = 500;
async function recordFall(driver: WorkshopDriver, label: string): Promise<WorkshopBodyPose[]> {
    const states: WorkshopBodyPose[] = [];
    for (const start = Date.now(); Date.now() - start < FREE_FALL_MS;) {
        const slot = await driver.readLatestPose(5, 1);
        assert.ok(slot, `${label}: the pose ring is readable`);
        assert.equal(slot.bodies.length, 1, `${label}: exactly the placed ball is published`);
        states.push(slot.bodies[0]);
    }
    return states;
}
function assertReplaysExactly(original: WorkshopBodyPose[], restored: WorkshopBodyPose[], label: string): void {
    const byHeight = new Map(original.map(b => [b.py, b]));
    const matched = new Set<number>();
    for (const b of restored) {
        const o = byHeight.get(b.py);
        if (!o) continue;
        assert.deepEqual([b.px, b.py, b.pz, b.vx, b.vy, b.vz, b.qx, b.qy, b.qz, b.qw], [o.px, o.py, o.pz, o.vx, o.vy, o.vz, o.qx, o.qy, o.qz, o.qw],
            `${label}: the restored committed state at py ${b.py} equals the original bit-for-bit`);
        matched.add(b.py);
    }
    assert.ok(matched.size >= 3, `${label}: at least three restored free-fall states coincide with the original Run (${matched.size} of ${restored.length} reads)`);
}

test('control: Reset then Run replays exactly; attack: a 20 px lifted ball is rejected by the same check', { timeout: 150000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: URL });
    try {
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);
        await driver.run(0);
        const original = await recordFall(driver, 'first Run');
        await driver.reset();
        await driver.run(0);
        assertReplaysExactly(original, await recordFall(driver, 'control'), 'control'); // must pass
        await driver.reset();
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(20, 720, 485);
        await driver.run(0);
        const moved = await recordFall(driver, 'moved');
        assert.throws(() => assertReplaysExactly(original, moved, 'moved'), /coincide|bit-for-bit/,
            'the replay check rejects a different world');
        await driver.reset();
    } finally { await driver.closeAfterSuite(); }
});
