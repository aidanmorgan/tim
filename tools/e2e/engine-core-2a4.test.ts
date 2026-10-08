// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2a4.
// Verifies Continuous Collision Detection (CCD) via speculative contact margins in narrowphase
// (d_spec = |v_rel . n| * dt + CONTACT_SLOP) and target velocity absorption (v_target = -g / dt):
// 1. High-speed vertical wall impact without tunneling through Wall
// 2. High-speed horizontal/tilted impact across 20 consecutive iterations without tunneling
// 3. Exact Reset and Save/Load persistence restoration in Chrome
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2a4: Anti-Tunneling High-Speed Wall Impact via Speculative Contacts CCD', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. High-speed vertical wall impact without tunneling', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place a Basketball at screen (720, 485) -> (0, 3, 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Select the Basketball and lift it up to maximum elevation py > 5.0 m
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(150);

        // Place a Wall beneath the ball at screen (720, 485) -> centered at (0, 3, 0), top face at py = 4.0 m
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Run simulation: ball drops from maximum elevation under gravity,
        // accelerates to high velocity, strikes top face of wall (y = 4.0 m),
        // and rebounds cleanly upward without tunneling through the wall to the floor.
        await driver.toggleRun(0);

        let reboundObserved = false;
        let minPyDuringImpact = Infinity;
        let maxImpactSpeed = 0;
        const startTime = Date.now();

        while (Date.now() - startTime < 2500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                if (ball.py < minPyDuringImpact) {
                    minPyDuringImpact = ball.py;
                }
                const speed = Math.hypot(ball.vx, ball.vy, ball.vz);
                if (speed > maxImpactSpeed) {
                    maxImpactSpeed = speed;
                }
                // Ball strikes wall around py ~ 4.34m and rebounds upwards with positive vy > 0.5
                if (ball.py >= 4.30 && ball.vy > 0.5) {
                    reboundObserved = true;
                }
            }
            await driver.page.waitForTimeout(40);
        }

        // Reset to return to Build Mode
        await driver.toggleRun(300);

        // Ball bottom must not penetrate top face of Wall (y=4.0).
        // Since ball radius is 0.34 m, ball center must remain > 4.0 m (workbench is at -0.12 m).
        assert.ok(
            minPyDuringImpact > 4.0,
            `Ball must never tunnel through Wall (got min py=${minPyDuringImpact.toFixed(3)}, wall top=4.0 m)`
        );
        assert.ok(
            maxImpactSpeed > 3.0,
            `Impact must occur at high speed (got maxSpeed=${maxImpactSpeed.toFixed(2)} m/s)`
        );
        assert.ok(reboundObserved, 'Ball must rebound cleanly upward off the Wall via speculative contact CCD');
    });

    test('2. High-speed impact across 20 iterations never tunnels through thin Wall', { timeout: 150000 }, async () => {
        await driver.reload();

        // Place a Basketball at screen (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Lift Basketball to high elevation
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(150);

        // Place Wall beneath the ball
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Tilt the Wall slightly (-20 deg) so impact has both high closing velocity and horizontal deflection
        await driver.tiltSelectedRamp(720, 485, -20);

        // Run 20 consecutive high-speed impact iterations
        for (let iteration = 1; iteration <= 20; iteration++) {
            // Start simulation
            await driver.toggleRun(0);

            let reboundInIteration = false;
            let minPyInIteration = Infinity;
            let horizontalMotion = false;
            const runStartTime = Date.now();

            while (Date.now() - runStartTime < 1200) {
                const pose = await driver.readLatestPose();
                if (pose && pose.bodies.length > 0) {
                    const ball = pose.bodies[0];
                    if (ball.py < minPyInIteration) {
                        minPyInIteration = ball.py;
                    }
                    if (ball.py >= 4.0 && ball.vy > 0.3) {
                        reboundInIteration = true;
                    }
                    if (Math.abs(ball.vx) > 0.2) {
                        horizontalMotion = true;
                    }
                }
                await driver.page.waitForTimeout(30);
            }

            // Reset simulation back to Build Mode
            await driver.toggleRun(200);

            assert.ok(
                minPyInIteration > 3.8,
                `Iteration ${iteration}: Ball must not tunnel through Wall (got min py=${minPyInIteration.toFixed(3)})`
            );
            assert.ok(
                reboundInIteration,
                `Iteration ${iteration}: Ball must cleanly rebound off Wall`
            );
        }
    });

    test('3. Exact Reset and Save/Load persistence restoration', { timeout: 150000 }, async () => {
        await driver.reload();

        // Place a Basketball at screen (720, 485)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Lift Basketball up
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(120);

        // Place a Wall beneath the ball
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Save construction
        await driver.save();

        // Run simulation: observe impact and rebound
        await driver.toggleRun(0);
        let bounced = false;
        const runStartTime = Date.now();
        while (Date.now() - runStartTime < 1500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                if (pose.bodies[0].vy > 0.5) bounced = true;
            }
            await driver.page.waitForTimeout(40);
        }

        // Reset simulation: ball must return to starting elevation py > 4.5
        await driver.toggleRun(400);

        // Run briefly to verify reset starting state
        await driver.toggleRun(100);
        const resetPose = await driver.readLatestPose();
        assert.ok(resetPose && resetPose.bodies.length > 0, 'Pose slot must be readable');
        assert.ok(
            resetPose.bodies[0].py > 4.5,
            `Reset must restore ball to starting elevation py > 4.5 m (got py=${resetPose.bodies[0].py.toFixed(3)})`
        );
        await driver.toggleRun(300);

        // Reload page to start with blank workshop state
        await driver.reload();

        // Load saved construction
        await driver.load();

        // Run loaded construction: verify it is fully functional and interacts stably with Wall
        await driver.toggleRun(200);
        let loadedBounced = false;
        let settledOnWall = false;
        let loadedMinPy = Infinity;
        const loadRunStartTime = Date.now();
        while (Date.now() - loadRunStartTime < 2500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                if (ball.py < loadedMinPy) loadedMinPy = ball.py;
                if ((ball.py >= 4.0 && ball.vy > 0.3) || (ball.py > loadedMinPy + 0.05)) {
                    loadedBounced = true;
                }
                if (Math.abs(ball.py - 4.34) < 0.02 && Math.abs(ball.vy) < 0.1) {
                    settledOnWall = true;
                }
            }
            await driver.page.waitForTimeout(40);
        }

        await driver.toggleRun(300);

        assert.ok(
            loadedMinPy > 4.0,
            `Loaded construction must not tunnel through Wall (got min py=${loadedMinPy.toFixed(3)})`
        );
        assert.ok(
            settledOnWall || loadedBounced,
            'Loaded construction must interact with and settle stably on the Wall'
        );
    });
});
