// Pure TypeScript Playwright-driven E2E acceptance test suite for slice ENGINE-CORE-2b1.
// Verifies Host Validation & Error Lane Removal (Story 2.1):
// Direct SAB pose ring publishing without intermediate host shadow checking:
// 1. Real-time ball drop and Workbench interaction without host validation overhead or errors
// 2. Multi-body Ramp and Wall interaction (First principles 2-ramp solve) with direct pose ring publishing
// 3. Exact Reset and Save/Load persistence roundtrip in Chrome
// Story 9A.2: test 2 refuses a stored save that fails structural pre-validation and keeps the bench.
// Story 9A.15: test 3 refuses a stored save naming an element the loaded bundle does not declare, naming the id.
// Tests 2 and 3 are proven by the shared recipes engine-core-2a3.test.ts tests 4 and 5, which carry ENGINE-CORE-2b1 in their names
// (owner decision 9 Oct 2026, Story 6.1d); this suite keeps test 1.
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import type { ConsoleMessage } from 'playwright';
import { WorkshopDriver } from './workshop-driver.ts';
import { authoredState, decodeSavedConstruction } from './workshop-wire.ts';

describe('ENGINE-CORE-2b1: Host Validation & Error Lane Removal (Direct SAB Pose Publishing)', () => {
    let driver: WorkshopDriver;

    before(async () => {
        driver = await WorkshopDriver.launch();
    });

    after(async () => {
        if (driver) await driver.closeAfterSuite();
    });

    test('1. Real-time ball drop and Workbench interaction without host validation overhead or errors', { timeout: 120000 }, async () => {
        // Listen for any transport or validation failure logged in console
        const errors: string[] = [];
        const errorHandler = (msg: ConsoleMessage) => {
            const text = msg.text();
            if (text.includes('CCGPU_TRANSPORT_FAILURE') || text.includes('CCGPU_STARTUP_EXCEPTION') || text.includes('Unhandled exception')) {
                errors.push(text);
            }
        };
        driver.page.on('console', errorHandler);

        // Select Basketball and place on canvas at (720, 485) -> (px ~ 0, py ~ 3.0, pz ~ 0)
        await driver.selectTool('basketball');
        await driver.placeOnCanvas(720, 485);

        // Run simulation: ball drops from py ~ 3.0 m down to workbench floor
        await driver.run();

        let sequenceAdvanced = false;
        let initialPy = -1;
        let finalPy = -1;
        let initialSequence = 0n;
        let latestSequence = 0n;
        const startTime = Date.now();

        while (Date.now() - startTime < 3000) {
            const pose = await driver.readLatestPose();
            if (pose && pose.bodies.length > 0) {
                const seq = BigInt(pose.sequence);
                if (initialSequence === 0n && seq > 0n) {
                    initialSequence = seq;
                    initialPy = pose.bodies[0].py;
                }
                if (seq > initialSequence) {
                    sequenceAdvanced = true;
                    latestSequence = seq;
                    finalPy = pose.bodies[0].py;
                }
            }
            await driver.page.waitForTimeout(50);
        }

        // Reset simulation back to Build Mode
        await driver.reset(300);

        driver.page.off('console', errorHandler);

        assert.equal(errors.length, 0, `Zero host validation/transport errors expected, got: ${errors.join('; ')}`);
        assert.ok(sequenceAdvanced, `Pose ring sequence must advance steadily (from ${initialSequence} to ${latestSequence})`);
        assert.ok(initialPy > 2.0, `Ball must start near py ~ 3.0 m (got initial py=${initialPy.toFixed(3)})`);
        assert.ok(finalPy < 1.0, `Ball must fall towards workbench floor (got final py=${finalPy.toFixed(3)})`);
    });

    // Story 9A.2 AC4: a stored save that fails structural pre-validation is the player's typed refusal. The Load issues no
    // command, the refusal is reported (not a fault), and the bench is kept; the intact save still loads (positive control).
    test('2. A save that fails structural pre-validation is refused and the bench is kept', { timeout: 120000 }, async () => {
        const failures: string[] = [];
        const failureHandler = (msg: ConsoleMessage) => {
            const text = msg.text();
            if (msg.type() === 'error' || text.includes('_EXCEPTION') || text.includes('Unexpected fault') || text.includes('Unhandled exception'))
                failures.push(text);
        };
        driver.page.on('console', failureHandler);
        const intact = (await driver.save()).slot;
        const before = authoredState(intact);
        const identifier = intact.slice(); identifier[8] ^= 0x20; // another root's identifier
        const damaged: [string, Uint8Array][] = [
            ['truncated', intact.slice(0, intact.length - 1)],
            ['trailing byte', Uint8Array.from([...intact, 0])],
            ['unframed (the pre-9A.2 shape)', intact.slice(4)],
            ['foreign identifier', identifier],
        ];
        for (const [name, bytes] of damaged) {
            await driver.writeSavedSlot(bytes);
            const refusal = await driver.loadRefused();
            assert.match(refusal, /WorkshopSaveRejectedException/, `${name}: the refusal is the typed save rejection`);
            assert.match(refusal, /This save cannot be loaded/, `${name}: the player is told the save cannot be loaded`);
            assert.deepEqual(authoredState((await driver.save()).slot), before, `${name}: the bench is kept`);
        }
        await driver.load(); // control: the intact save the last Save stored loads
        assert.deepEqual(authoredState((await driver.save()).slot), before);
        driver.page.off('console', failureHandler);
        assert.deepEqual(failures, [], 'a refused save is never a fault');
    });

    // Story 9A.15 AC2 (F16-4): a save carries each part's bundle element id string, validated against the loaded bundle. A stored
    // save naming an element the bundle does not declare (the ball's id, swapped byte for byte to 'kite') is refused naming the id,
    // the Load issues no command and the bench is kept; the intact save still loads (positive control).
    test('3. A save naming an element the loaded bundle does not declare is refused naming it, and the bench is kept', { timeout: 120000 }, async () => {
        const failures: string[] = [];
        const failureHandler = (msg: ConsoleMessage) => {
            const text = msg.text();
            if (msg.type() === 'error' || text.includes('_EXCEPTION') || text.includes('Unexpected fault') || text.includes('Unhandled exception'))
                failures.push(text);
        };
        driver.page.on('console', failureHandler);
        const intact = (await driver.save()).slot;
        const before = authoredState(intact);
        const ids = decodeSavedConstruction(intact).construction()!;
        assert.deepEqual(Array.from({ length: ids.instancesLength() }, (_, i) => ids.instances(i)!.partId()), ['ball'],
            'the saved bench names its ball by the bundle element id');
        // The FlatBuffers string: a u32 length, the id's bytes and a terminating zero.
        const needle = [4, 0, 0, 0, ...Array.from('ball', c => c.charCodeAt(0)), 0];
        const at = Array.from({ length: intact.length - needle.length + 1 }, (_, i) => i)
            .filter(i => needle.every((byte, j) => intact[i + j] === byte));
        assert.equal(at.length, 1, 'the save holds the element id exactly once');
        const unknown = intact.slice();
        unknown.set(Array.from('kite', c => c.charCodeAt(0)), at[0]! + 4);
        await driver.writeSavedSlot(unknown);
        const refusal = await driver.loadRefused();
        assert.match(refusal, /WorkshopSaveRejectedException/, 'the refusal is the typed save rejection');
        assert.match(refusal, /This save cannot be loaded: Part \d+ names the element 'kite', which this game does not have\./,
            'the player is told which element the save names');
        assert.deepEqual(authoredState((await driver.save()).slot), before, 'the bench is kept');
        await driver.load(); // control: the intact save the last Save stored loads
        assert.deepEqual(authoredState((await driver.save()).slot), before);
        driver.page.off('console', failureHandler);
        assert.deepEqual(failures, [], 'a refused save is never a fault');
    });
});
