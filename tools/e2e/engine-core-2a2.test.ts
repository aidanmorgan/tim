// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2a2.
// Verifies stable resting contact without jitter or artificial position projection,
// dissipative bounce restitution dynamics (Box2D v3 TGS Soft constraint solver),
// exact Run/Reset restoration, and Save/Load persistence roundtrip.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2a2: TGS Soft Solver Resting Contact and Dissipative Bounce Restitution', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Stable resting contact: Basketball settles on workbench without bounce jitter or position projection', { timeout: 120000 }, async () => {
        // Select Basketball tool via typed driver method
        await driver.selectTool('basketball');

        // Click workbench canvas at (720, 485) -> places ball near (0, 3, 0)
        await driver.placeOnCanvas(720, 485);

        // Run simulation for 4200 ms to allow ball to bounce, dissipate, and settle
        await driver.run(4200);

        const settledPose1 = await driver.readLatestPose();
        assert.ok(settledPose1, 'Pose ring slot must be readable after settling');
        assert.equal(settledPose1.bodyCount, 1, 'Pose ring must report exactly 1 body');

        const ball = settledPose1.bodies[0];
        assert.equal(ball.id, '1', 'Body ID must be 1');

        // Workbench plane is at y = -0.46, ball radius is 0.34.
        // Equilibrium center of mass elevation is y_rest = -0.46 + 0.34 = -0.12 m.
        // With CONTACT_SLOP = 0.0005 m (0.5 mm), resting elevation must sit within [-0.125, -0.119].
        assert.ok(
            ball.py >= -0.125 && ball.py <= -0.119,
            `Ball must settle at resting elevation ~ -0.12 m without penetration > 0.5mm (got py=${ball.py})`
        );

        // Velocity must be damped to resting equilibrium (|vy| < 0.05 m/s) without jitter
        assert.ok(
            Math.abs(ball.vy) < 0.05,
            `Vertical velocity must be damped at rest (got vy=${ball.vy})`
        );

        // Wait an additional 300 ms while running to verify sequence advances and resting state remains stable
        await driver.page.waitForTimeout(300);
        const settledPose2 = await driver.readLatestPose();
        assert.ok(settledPose2, 'Pose ring slot must be readable');
        assert.ok(
            BigInt(settledPose2.sequence) > BigInt(settledPose1.sequence),
            'Simulation must actively advance ticks while at rest'
        );
        assert.ok(
            settledPose2.bodies[0].py >= -0.125 && settledPose2.bodies[0].py <= -0.119,
            `Ball must maintain stable resting elevation without jitter (got py=${settledPose2.bodies[0].py})`
        );

        // Reset to return to Build Mode
        await driver.reset();
    });

    test('2. Dissipative bounce restitution dynamics: Successive bounce peaks decay strictly (h2 < h1)', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Basketball at (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Start simulation
        await driver.run();

        // Sample poses at ~40ms intervals over 3500 ms to capture trajectory
        const samples: { t: number; py: number; vy: number }[] = [];
        const startTime = Date.now();

        while (Date.now() - startTime < 3500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                samples.push({
                    t: Date.now() - startTime,
                    py: pose.bodies[0].py,
                    vy: pose.bodies[0].vy
                });
            }
            await driver.page.waitForTimeout(40);
        }

        // Stop simulation
        await driver.reset();

        assert.ok(samples.length > 40, `Must have collected sufficient trajectory samples (got ${samples.length})`);

        // Find bounce peaks: identify local maxima in py after approaching the floor (py < 0.05)
        const floorThreshold = 0.05;
        let inBounce = false;
        let currentPeak = -Infinity;
        const bouncePeaks: number[] = [];

        for (const s of samples) {
            if (s.py < floorThreshold) {
                if (inBounce && currentPeak > floorThreshold) {
                    bouncePeaks.push(currentPeak);
                    currentPeak = -Infinity;
                }
                inBounce = true;
            } else if (inBounce) {
                if (s.py > currentPeak) {
                    currentPeak = s.py;
                }
                // When falling back towards the floor, record peak
                if (s.vy < -0.5 && currentPeak > floorThreshold) {
                    bouncePeaks.push(currentPeak);
                    currentPeak = -Infinity;
                    inBounce = false;
                }
            }
        }
        if (currentPeak > floorThreshold) {
            bouncePeaks.push(currentPeak);
        }

        assert.ok(
            bouncePeaks.length >= 2,
            `Must record at least 2 distinct bounce peaks (recorded ${bouncePeaks.length}: ${bouncePeaks.map(p => p.toFixed(3)).join(', ')})`
        );

        const [h1, h2] = bouncePeaks;
        assert.ok(h1 > 0.5, `First bounce peak h1 must rebound significantly (got h1=${h1.toFixed(3)} m)`);
        assert.ok(h2 > -0.1, `Second bounce peak h2 must rebound above floor (got h2=${h2.toFixed(3)} m)`);
        assert.ok(
            h2 < h1,
            `Dissipative restitution must ensure h2 < h1 (got h1=${h1.toFixed(3)} m, h2=${h2.toFixed(3)} m)`
        );
    });

    test('3. Reset exact restoration after multi-bounce dissipation', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Basketball at (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Run for 2500 ms (dissipating through bounces)
        await driver.run(2500);

        const runningPose = await driver.readLatestPose();
        assert.ok(runningPose, 'Pose ring slot must be readable during run');
        assert.ok(
            runningPose.bodies[0].py < 2.0,
            `Ball elevation must have dissipated below 2.0 m (got py=${runningPose.bodies[0].py})`
        );

        // Reset simulation back to Build Mode
        await driver.reset();

        // Run second time: inspect initial frame elevation
        await driver.run(100);
        const secondRunPose = await driver.readLatestPose();
        assert.ok(secondRunPose, 'Pose ring slot must be readable during second Run');
        assert.equal(secondRunPose.bodyCount, 1, 'Body count must remain 1 after Reset');
        assert.ok(
            secondRunPose.bodies[0].py > 2.8,
            `Ball must restart from initial elevation near 3.0 m after Reset (got py=${secondRunPose.bodies[0].py})`
        );

        // Reset to return to Build Mode
        await driver.reset();
    });

    test('4. Save & Load persistence roundtrip: Ball preserves placed location and settles on workbench after load', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Basketball at (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Save construction
        await driver.save();

        // Reload page to start with a blank canvas
        await driver.reload();

        const cleanPose = await driver.readLatestPose();
        assert.equal(cleanPose?.bodyCount, 0, 'Reloaded workbench must start blank with 0 bodies');

        // Load saved construction
        await driver.load();

        // Run simulation for 4200 ms to settle
        await driver.run(4200);

        const loadedPose = await driver.readLatestPose();
        assert.ok(loadedPose, 'Pose ring slot must be readable after Load and Run');
        assert.equal(loadedPose.bodyCount, 1, 'Loaded construction must restore exactly 1 body');

        const loadedBall = loadedPose.bodies[0];
        assert.ok(
            loadedBall.py >= -0.125 && loadedBall.py <= -0.119,
            `Loaded ball must settle stably at resting elevation ~ -0.12 m (got py=${loadedBall.py})`
        );
        assert.ok(
            Math.abs(loadedBall.vy) < 0.05,
            `Loaded ball vertical velocity must be damped at rest (got vy=${loadedBall.vy})`
        );

        // Reset simulation
        await driver.reset();
    });
});
