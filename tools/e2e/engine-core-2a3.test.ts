// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2a3.
// Verifies multi-body Ramp and Wall contact on generic physics core:
// Dynamic AABB BVH with velocity fattening, branchless SAT narrowphase for Box-Sphere
// and Box-Box, local-axis Wall resizing, first_principles 2-ramp solve in Chrome,
// and exact Reset / Save/Load restoration.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

describe('ENGINE-CORE-2a3: Multi-body Ramp and Wall Contact on Generic Physics Core', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.close();
    });

    test('1. Ramp contact & rolling: Ball contacts inclined ramp and rolls along surface', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Ramp 1 target: [-3.4, 4.3, 0] -> screen canvas: (498, 404)
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // In first_principles, the ball is fixed at [-4, 6.5, 0].
        // Run simulation: ball falls under gravity, strikes the inclined ramp surface,
        // and rolls down the ramp towards the right (px increases, py decreases along incline).
        await driver.toggleRun(0);

        // Sample trajectory over 2500 ms
        let contactedRamp = false;
        let rolledRight = false;
        const startTime = Date.now();
        while (Date.now() - startTime < 2500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                // Initial ball elevation is ~6.5. Ramp 1 top surface is near y ~ 4.3.
                if (ball.py <= 4.8 && ball.py >= 3.8) {
                    contactedRamp = true;
                }
                // As it rolls along the ramp inclined down towards +X, px increases from -4 towards -2
                if (ball.px > -3.2 && ball.py < 4.5) {
                    rolledRight = true;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        // Reset to return to Build Mode
        await driver.toggleRun(300);

        assert.ok(contactedRamp, 'Ball must contact the ramp surface at elevation ~4.3 m');
        assert.ok(rolledRight, 'Ball must roll down along the inclined ramp towards the right');
    });

    test('2. Wall collision & deflection: Ball strikes Wall, rebounds via TGS Soft restitution', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place a Basketball at screen (720, 485) -> (0, 3, 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Select the Basketball and lift it up to elevation py ~ 4.7 m
        await driver.selectPartAt(720, 485);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(100);

        // Place a Wall beneath the ball at screen (720, 485) -> centered at (0, 3, 0), top face at py = 4.0 m
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Start simulation: ball drops from py ~ 4.7 m, strikes top face of wall (y ~ 4.0 m) and rebounds via TGS Soft
        await driver.toggleRun(0);

        let reboundObserved = false;
        let minPyDuringImpact = Infinity;
        let settledOnWall = false;
        const startTime = Date.now();
        while (Date.now() - startTime < 2500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                if (ball.py < minPyDuringImpact) {
                    minPyDuringImpact = ball.py;
                }
                // Ball strikes wall around py ~ 4.34m and rebounds upwards with positive vy
                if (ball.py >= 4.30 && ball.vy > 0.5) {
                    reboundObserved = true;
                }
                // Settles resting on wall surface (py ~ 4.34)
                if (Math.abs(ball.py - 4.34) < 0.02 && Math.abs(ball.vy) < 0.1) {
                    settledOnWall = true;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        // Reset to return to Build Mode
        await driver.toggleRun(300);

        assert.ok(
            minPyDuringImpact > 3.0,
            `Ball must collide on top of Wall at elevation > 3.0 m instead of floor (got min py=${minPyDuringImpact.toFixed(3)})`
        );
        assert.ok(reboundObserved, 'Ball must rebound upwards off the Wall via TGS Soft restitution');
        assert.ok(settledOnWall, 'Ball must settle stably on the top of the Wall');
    });

    test('3. Local-axis Wall resizing: Resized wall deflects ball at updated collision extents', { timeout: 120000 }, async () => {
        await driver.reload();

        // Place Wall at (720, 485) (unresized width 3.0 m, spans x in [-1.5, 1.5])
        await driver.selectTool('wall');
        await driver.placeOnCanvas(720, 485);

        // Place Basketball at (840, 521) -> (px = 2.2, pz = 0), outside unresized wall bounds [x <= 1.5]
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(840, 521);

        // Select and lift the ball up
        await driver.selectPartAt(840, 521);
        await driver.setPartMode('move');
        await driver.liftSelectedPart(100, 840, 521);

        // Run control for 2000 ms: ball falls past unresized wall down to the floor
        await driver.toggleRun(2000);
        const controlPose = await driver.readLatestPose();
        assert.ok(controlPose && controlPose.bodies.length > 0, 'Pose must be readable');
        assert.ok(
            controlPose.bodies[0].py < 1.0,
            `Control ball outside unresized wall must fall past wall towards floor (got py=${controlPose.bodies[0].py.toFixed(3)})`
        );

        // Reset to Build Mode
        await driver.toggleRun(500);

        // Select the Wall at (720, 485)
        await driver.selectPartAt(720, 485);

        // Switch to Resize mode via driver
        await driver.setPartMode('resize');

        // Drag right resize handle (+X) by 70px to expand wall width to span outside bounds (hx expands from 1.5 to 2.57)
        await driver.resizeSelectedWall(70, 720, 485);

        // Run second time: with widened wall bounds (span extends to x = 2.57), ball collides with the resized wall
        await driver.toggleRun(0);

        let resizedCollision = false;
        let minPyResized = Infinity;
        const testStartTime = Date.now();
        while (Date.now() - testStartTime < 2500) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const ball = pose.bodies[0];
                if (ball.py < minPyResized) {
                    minPyResized = ball.py;
                }
                // Ball strikes resized wall around py ~ 4.34m and rebounds upwards with positive vy
                if (ball.py >= 4.30 && ball.vy > 0.5) {
                    resizedCollision = true;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        // Reset to Build Mode
        await driver.toggleRun(300);

        assert.ok(
            minPyResized > 3.0,
            `Ball must collide on top of resized wall at elevation > 3.0 m instead of floor (got min py=${minPyResized.toFixed(3)})`
        );
        assert.ok(resizedCollision, 'Ball must collide with and rebound off the resized wall at extended bounds');
    });

    test('4. First principles 2-ramp solve: Ball rolls down both ramps into Receiver and achieves captured', { timeout: 120000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Place Ramp 1 at (498, 404) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2 at (674, 509) and tilt -20 deg
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -20);

        // Start simulation
        await driver.toggleRun(0);

        // Monitor for goal capture event
        let captured = 0;
        const solveStartTime = Date.now();
        while (Date.now() - solveStartTime < 8000) {
            captured = await driver.readCaptured();
            if (captured > 0) break;
            await driver.page.waitForTimeout(300);
        }

        const solvedPose = await driver.readLatestPose();
        assert.ok(solvedPose && solvedPose.bodies.length > 0, 'Pose must be readable');

        // Reset simulation
        await driver.toggleRun(300);

        assert.ok(captured > 0, `Receiver must capture ball and solve level (captured count=${captured})`);
        // Ball must have reached the receiver near [2.5, 0.9, 0]
        assert.ok(
            solvedPose.bodies[0].px > 1.8 && solvedPose.bodies[0].px < 3.2,
            `Ball must end inside Receiver horizontal range [1.8, 3.2] (got px=${solvedPose.bodies[0].px.toFixed(3)})`
        );
        assert.ok(
            solvedPose.bodies[0].py < 2.0,
            `Ball must settle inside Receiver basket (got py=${solvedPose.bodies[0].py.toFixed(3)})`
        );
    });

    test('5. Exact Reset and Save/Load restoration', { timeout: 150000 }, async () => {
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Place Ramp 1
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(498, 404);
        await driver.tiltSelectedRamp(498, 404, -20);

        // Place Ramp 2
        await driver.selectTool('ramp');
        await driver.placeOnCanvas(674, 509);
        await driver.tiltSelectedRamp(674, 509, -20);

        // Save construction
        await driver.save();

        // Run simulation until solved
        await driver.toggleRun(0);
        const runStartTime = Date.now();
        while (Date.now() - runStartTime < 6000) {
            if (await driver.readCaptured() > 0) break;
            await driver.page.waitForTimeout(300);
        }

        // Reset simulation: ball must return to starting elevation py ~ 6.5
        await driver.toggleRun(500);

        // Run briefly to verify reset starting state
        await driver.toggleRun(100);
        const resetPose = await driver.readLatestPose();
        assert.ok(resetPose && resetPose.bodies.length > 0, 'Pose slot must be readable');
        assert.ok(
            resetPose.bodies[0].py > 5.5,
            `Reset must restore ball to starting elevation py > 5.5 m (got py=${resetPose.bodies[0].py.toFixed(3)})`
        );
        await driver.toggleRun(300);

        // Reload page to start with blank First Principles state
        await driver.reload();
        await driver.selectLevel('first_principles');

        // Load saved construction
        await driver.load();

        // Run loaded construction: verify it is fully functional and solves the level
        await driver.toggleRun(0);
        let loadedCaptured = 0;
        const loadRunStartTime = Date.now();
        while (Date.now() - loadRunStartTime < 8000) {
            loadedCaptured = await driver.readCaptured();
            if (loadedCaptured > 0) break;
            await driver.page.waitForTimeout(300);
        }

        await driver.toggleRun(300);

        assert.ok(
            loadedCaptured > 0,
            `Loaded construction must solve level and capture ball in receiver (got captured=${loadedCaptured})`
        );
    });
});
