// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2a1.
// Drives the browser via Playwright and verifies Dual-Sphere Simulation, Multi-body Triple Pose Ring,
// UI operations via typed TypeScript driver, and Save/Load persistence.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized', { timeout: 120000 }, async () => {
        const title: string = await driver.page.title();
        const crossOriginIsolated: boolean = await driver.page.evaluate(() => globalThis.crossOriginIsolated);
        const hasPoseRing: boolean = await driver.page.evaluate(() => globalThis.WorkshopPoseRing?.hasPoseRing(1) === true);

        assert.equal(title, 'Curious Contraptions');
        assert.equal(crossOriginIsolated, true, 'Page must be cross-origin isolated');
        assert.equal(hasPoseRing, true, 'WorkshopPoseRing must be initialized for client 1');
    });

    test('2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode', { timeout: 120000 }, async () => {
        const initialPose = await driver.readLatestPose();
        assert.ok(initialPose, 'Pose ring slot must be readable');
        assert.equal(initialPose.bodyCount, 0, 'Clean Free Workshop must start with 0 bodies');

        // Start simulation
        await driver.run(1000);

        const runningPose = await driver.readLatestPose();
        assert.ok(runningPose, 'Pose ring slot must be readable while running');
        assert.equal(runningPose.bodyCount, 0, 'Running empty construction must have 0 bodies');
        assert.ok(BigInt(runningPose.sequence) > BigInt(initialPose.sequence), 'Sequence must advance during Run');

        // Reset simulation
        await driver.reset();

        const resetPose = await driver.readLatestPose();
        assert.ok(resetPose, 'Pose ring slot must be readable after Reset');
        assert.equal(resetPose.bodyCount, 0, 'Reset empty construction must have 0 bodies');
    });

    test('3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation', { timeout: 120000 }, async () => {
        await driver.reload();

        // Select Basketball tool via typed driver method
        await driver.selectTool('basketball');

        // Click workbench canvas at (720, 485) -> places ball near (0, 3, 0)
        await driver.placeOnCanvas(720, 485);

        // Start simulation
        await driver.run(1000);

        const runningPose = await driver.readLatestPose();
        assert.ok(runningPose, 'Pose ring slot must be readable during single-ball Run');
        assert.equal(runningPose.bodyCount, 1, 'Pose ring must report exactly 1 body');
        assert.equal(runningPose.bodies[0].id, 1, 'Body ID must be 1');
        assert.ok(runningPose.bodies[0].py < 3.0, 'Ball must have fallen under gravity (py < 3.0)');

        // Reset simulation
        await driver.reset();

        // Run second time: verify exact Reset restoration (ball restarts from elevation py ~ 3.0)
        await driver.run(100);
        const secondRunPose = await driver.readLatestPose();
        assert.ok(secondRunPose, 'Pose ring slot must be readable during second Run');
        assert.equal(secondRunPose.bodyCount, 1, 'Body count must remain 1 after Reset');
        assert.ok(secondRunPose.bodies[0].py > 1.8, 'Ball must restart from initial elevation after Reset (py > 1.8)');

        // Reset to return to Build Mode
        await driver.reset();
    });

    test('4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Basketball 1 at left [666, 468] (~ x = -1, y = 3, z = 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(666, 468);

        // Place Basketball 2 at right [774, 502] (~ x = 1, y = 3, z = 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(774, 502);

        // Start simulation
        await driver.run(1000);

        const runningPose = await driver.readLatestPose();
        assert.ok(runningPose, 'Pose ring slot must be readable during dual-sphere Run');
        assert.equal(runningPose.bodyCount, 2, 'Pose ring must report exactly 2 bodies');

        const [b1, b2] = runningPose.bodies;
        assert.notEqual(b1.id, b2.id, 'Bodies must have distinct IDs');
        assert.ok(b1.px < 0, 'Body 1 must be on the left (px < 0)');
        assert.ok(b2.px > 0, 'Body 2 must be on the right (px > 0)');
        assert.ok(b1.py < 3.0, 'Body 1 must have fallen under gravity (py < 3.0)');
        assert.ok(b2.py < 3.0, 'Body 2 must have fallen under gravity (py < 3.0)');
        assert.ok((BigInt(runningPose.sequence) & 1n) === 0n, 'Pose ring sequence must be even (committed)');

        // Reset simulation
        await driver.reset();

        // Run second time: verify both bodies restored to initial arrangement
        await driver.run(100);
        const secondRunPose = await driver.readLatestPose();
        assert.ok(secondRunPose, 'Pose ring slot must be readable during second Run');
        assert.equal(secondRunPose.bodyCount, 2, 'Pose ring must report both bodies after Reset');
        assert.ok(secondRunPose.bodies[0].py > 1.8, 'Body 1 restarted from initial elevation (py > 1.8)');
        assert.ok(secondRunPose.bodies[1].py > 1.8, 'Body 2 restarted from initial elevation (py > 1.8)');

        // Reset to return to Build Mode
        await driver.reset();
    });

    test('5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Basketball 1 at left [666, 468]
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(666, 468);

        // Place Basketball 2 at right [774, 502]
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(774, 502);

        // Save via driver
        await driver.save();

        // Reload page to start with a fresh blank workbench
        await driver.reload();

        const cleanPose = await driver.readLatestPose();
        assert.equal(cleanPose?.bodyCount, 0, 'Reloaded workbench must start blank with 0 bodies');

        // Load via driver
        await driver.load();

        // Start simulation
        await driver.run(1000);

        const restoredPose = await driver.readLatestPose();
        assert.ok(restoredPose, 'Pose ring slot must be readable after Load and Run');
        assert.equal(restoredPose.bodyCount, 2, 'Loaded construction must restore both 2 bodies in pose ring');

        const [r1, r2] = restoredPose.bodies;
        assert.notEqual(r1.id, r2.id, 'Restored bodies must have distinct IDs');
        assert.ok(r1.px < 0 && r2.px > 0, 'Restored bodies must be at original left and right coordinates');

        // Reset simulation
        await driver.reset();
    });
});
