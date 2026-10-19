// Murdoch-2 Story 9.4b: Save -> reload -> Load -> Run x3 cycling on the cat-023b wired domino chain.
// One saved construction is reloaded three times; each load is run three times with Reset between runs (9 cascades,
// three times cat-023b's heaviest load). Per run: the chain admits upright, every tile and the end Domino pass 45 deg in
// chain order, CCGOAL_SOLVED prints exactly once, the lamp reaches full glow with a non-restarting ramp, the pose ring
// stays readable, and the console slice of that run has zero errors and zero clock recalibrations. Per load: the tick-0
// read equals the first load's bodies byte-for-byte, Reset restores it byte-for-byte, and a re-save is byte-identical in
// authored state to the original save. after(): suite totals are zero (catches anything logged between runs).
import assert from 'node:assert/strict';
import { after, before, describe, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';
import { authoredState } from './workshop-wire.ts';
import { PREVIEW_URL, SessionWatch, CHAIN, buildWiredChain, readSavedBytes, allBodies, waitUntil, sleep } from './murdoch-common.ts';
import type { WorkshopBodyPose } from './workshop-driver.ts';

const LOADS = Number(process.env.MURDOCH_CYCLE_LOADS ?? 3);
const RUNS_PER_LOAD = Number(process.env.MURDOCH_CYCLE_RUNS ?? 3);

function tilt(b: WorkshopBodyPose): number {
    const upY = 1 - 2 * (b.qx * b.qx + b.qz * b.qz);
    return Math.acos(Math.max(-1, Math.min(1, upY))) * 180 / Math.PI;
}

describe('Murdoch cat-023b Save/Load/Run cycling', () => {
    let driver: WorkshopDriver;
    let watch: SessionWatch;
    let original: Buffer;
    let loadedBodies: ReturnType<typeof allBodies> | undefined;
    const perRun: object[] = [];

    before(async () => {
        driver = await WorkshopDriver.launch({ baseUrl: PREVIEW_URL });
        watch = new SessionWatch(driver);
        await buildWiredChain(driver);
        await driver.save();
        original = await readSavedBytes(driver);
    });

    after(async () => {
        try {
            console.log('MURDOCH_CYCLES ' + JSON.stringify({ perRun, total: watch.summary() }));
            assert.deepEqual(watch.terminations.map(t => t.text.slice(0, 200)), [], 'Zero terminations across the cycling suite');
            assert.deepEqual(watch.errors.map(t => t.text.slice(0, 200)), [], 'Zero console errors across the cycling suite (including between runs)');
            assert.equal(watch.recalibrations.length, 0, 'Zero clock recalibrations across the cycling suite');
            assert.equal(watch.legacyClockFaults.length, 0, 'No legacy clock fault line');
        } finally { if (driver) await driver.close(); }
    });

    for (let load = 1; load <= LOADS; load++) test(`load ${load}: reload, Load, ${RUNS_PER_LOAD} Runs with Reset`, { timeout: 300000 }, async () => {
        await driver.reload();
        await driver.selectLevel('domino_effect');
        await driver.load();
        const tickZero = watch.reads.at(-1);
        assert.ok(tickZero, 'Load produced a committed read');
        assert.equal(tickZero.tick(), 0n, 'Load read is at tick 0');
        const bodies = allBodies(tickZero);
        if (loadedBodies) assert.deepEqual(bodies, loadedBodies, `load ${load}: loaded bodies equal the first load byte-for-byte`);
        else loadedBodies = bodies;
        await driver.save();
        assert.deepEqual(authoredState(await readSavedBytes(driver)), authoredState(original), `load ${load}: re-save after Load is authored-identical`);

        for (let run = 1; run <= RUNS_PER_LOAD; run++) {
            const errorsAt = watch.errors.length, recalAt = watch.recalibrations.length, termAt = watch.terminations.length;
            const solvedAt = watch.solved.length;
            const lampBefore = (await driver.readAnimationSamplesForTarget(CHAIN.LAMP_TARGET)).length;
            await driver.run();
            const first = await driver.readLatestPose();
            assert.ok(first, `load ${load} run ${run}: pose readable at Run`);
            const ids = first.bodies.map(b => b.id).sort();
            assert.deepEqual(ids, [CHAIN.BALL_ID, CHAIN.END_ID, ...CHAIN.TILE_IDS].sort(), `load ${load} run ${run}: ball, end and four tiles`);
            for (const id of [...CHAIN.TILE_IDS, CHAIN.END_ID]) assert.ok(tilt(first.bodies.find(b => b.id === id)!) < 0.05, `load ${load} run ${run}: ${id} admitted upright`);

            const passed = new Map<string, bigint>();
            const start = Date.now();
            while (Date.now() - start < 9000 && passed.size < 5) {
                const pose = await driver.readLatestPose();
                assert.ok(pose, `load ${load} run ${run}: pose slot readable during the cascade`);
                for (const id of [...CHAIN.TILE_IDS, CHAIN.END_ID]) {
                    const b = pose.bodies.find(body => body.id === id);
                    if (b && !passed.has(id) && tilt(b) > 45) passed.set(id, BigInt(pose.sequence));
                }
                await sleep(driver, 40);
            }
            assert.equal(passed.size, 5, `load ${load} run ${run}: every tile and the end Domino pass 45 deg (${[...passed.keys()]})`);
            const order = [...passed.entries()].sort((a, b) => (a[1] < b[1] ? -1 : a[1] > b[1] ? 1 : 0)).map(([id]) => id);
            assert.deepEqual(order, [...CHAIN.TILE_IDS, CHAIN.END_ID], `load ${load} run ${run}: chain order`);

            assert.ok(await waitUntil(() => watch.solved.length === solvedAt + 1, 10000), `load ${load} run ${run}: CCGOAL_SOLVED prints`);
            assert.ok(await waitUntil(async () => (await driver.readLastAnimationSample(CHAIN.LAMP_TARGET))?.valBits === CHAIN.HALF_ONE, 3000),
                `load ${load} run ${run}: lamp reaches full glow`);
            await sleep(driver, 2000);
            assert.equal(watch.solved.length, solvedAt + 1, `load ${load} run ${run}: CCGOAL_SOLVED exactly once`);
            const history = (await driver.readAnimationSamplesForTarget(CHAIN.LAMP_TARGET)).map(s => s.value);
            assert.ok(history.length >= lampBefore + 5, `load ${load} run ${run}: lamp glow ramp recorded (${history.length} after ${lampBefore})`);
            const own = history.slice(lampBefore);
            for (let i = 1; i < own.length; i++) assert.ok(own[i] >= own[i - 1], `load ${load} run ${run}: the lamp ramp never restarts`);

            await driver.reset(800);
            const reset = watch.reads.at(-1)!;
            assert.equal(reset.tick(), 0n, `load ${load} run ${run}: Reset read at tick 0`);
            assert.deepEqual(allBodies(reset), loadedBodies, `load ${load} run ${run}: Reset restores the loaded bodies byte-for-byte`);

            const slice = { load, run, errors: watch.errors.slice(errorsAt).map(e => e.text.slice(0, 160)),
                recalibrations: watch.recalibrations.slice(recalAt).map(r => r.raw.slice(0, 160)),
                terminations: watch.terminations.slice(termAt).map(t => t.text.slice(0, 160)), order };
            perRun.push(slice);
            assert.deepEqual(slice.terminations, [], `load ${load} run ${run}: zero terminations in this run`);
            assert.deepEqual(slice.errors, [], `load ${load} run ${run}: zero console errors in this run`);
            assert.deepEqual(slice.recalibrations, [], `load ${load} run ${run}: zero recalibrations in this run`);
        }
    });
});
