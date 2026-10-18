// CAT-015a preloaded-contact preparation. Recharge/Battery and campaign admission remain separate prerequisites.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeWorkshopResponse, authoredState, decodeSavedConstruction, type BodyRead, type WorkshopResponse, type InstanceRecord } from './workshop-wire.ts';

const BASE_URL = process.env.BUMPER_PREVIEW_URL ?? 'http://127.0.0.1:8060/';

function bodyState(b: BodyRead) {
    return {
        id: b.id(),
        cell: { x: b.cell()!.x(), y: b.cell()!.y(), z: b.cell()!.z() },
        local: { x: b.local()!.x(), y: b.local()!.y(), z: b.local()!.z() },
        rotation: { x: b.rotation()!.x(), y: b.rotation()!.y(), z: b.rotation()!.z(), w: b.rotation()!.w() },
        velocity: { x: b.velocity()!.x(), y: b.velocity()!.y(), z: b.velocity()!.z() },
        angularVelocity: { x: b.angularVelocity()!.x(), y: b.angularVelocity()!.y(), z: b.angularVelocity()!.z() },
    };
}
async function savedBytes(driver: WorkshopDriver): Promise<Buffer> {
    const values = await driver.page.evaluate(() => new Promise<number[]>((resolve, reject) => {
        const request = indexedDB.open('curious-contraptions-workshop');
        request.onupgradeneeded = () => request.transaction?.abort();
        request.onerror = () => reject(new Error('Existing save database unavailable'));
        request.onsuccess = () => {
            const database = request.result;
            const transaction = database.transaction('construction', 'readonly');
            const read = transaction.objectStore('construction').get(1);
            transaction.oncomplete = () => { database.close(); resolve(Array.from(read.result as Uint8Array)); };
            transaction.onabort = () => { database.close(); reject(new Error('Save observation aborted')); };
        };
    }));
    return Buffer.from(values);
}

async function scenario(kind: 'basketball' | 'bowling', duration: number) {
    const driver = await WorkshopDriver.launch({ baseUrl: BASE_URL });
    const errors: string[] = [];
    const observations: WorkshopResponse[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (/CCGPU_TRANSPORT_FAILURE|CCGPU_STARTUP_EXCEPTION|Unhandled exception|Impulse occurrence dropped/.test(text)) errors.push(text);
        if (text.startsWith('CCGPU ')) observations.push(decodeWorkshopResponse(Buffer.from(text.slice(6), 'base64')));
    });
    driver.page.on('pageerror', error => errors.push(error.message));
    try {
        await driver.selectTool(kind);
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('bumper', 150);
        const authored = bodyState(observations[observations.length - 1].bodies(0)!);
        await driver.save();
        const construction = await savedBytes(driver);
        const fbSave = decodeSavedConstruction(construction);
        const c = fbSave.construction()!;
        const payloadPartId = kind === 'basketball' ? 'ball' : kind;
        let ballInst: InstanceRecord | null = null;
        let bumperInst: InstanceRecord | null = null;
        for (let i = 0; i < c.instancesLength(); i++) {
            const inst = c.instances(i)!;
            if (inst.partId() === payloadPartId) ballInst = inst;
            if (inst.partId() === 'bumper') bumperInst = inst;
        }
        assert.ok(ballInst && bumperInst, 'Construction contains payload and bumper instances');
        assert.equal(bumperInst.strength(), 8, 'Placed bumper strength is 8m/s');
        assert.equal(bumperInst.referenceMass(), 1, 'Calibration reference mass is 1kg');
        assert.equal(bumperInst.preload(), 32, 'Declared finite preload is 32J');
        assert.equal(ballInst.cell()!.x(), bumperInst.cell()!.x(), 'Payload and bumper share the complete f32 X contact cell');
        assert.equal(ballInst.cell()!.z(), bumperInst.cell()!.z(), 'Payload and bumper share the complete f32 Z contact cell');
        assert.equal(ballInst.local()!.x(), bumperInst.local()!.x(), 'Payload and bumper share the complete f32 X contact local');
        assert.equal(ballInst.local()!.z(), bumperInst.local()!.z(), 'Payload and bumper share the complete f32 Z contact local');
        assert.equal(bumperInst.cell()!.y(), 48, 'Bumper is on the 3m placement plane');
        assert.ok(ballInst.cell()!.y() > 80, 'Payload is visibly lifted above the bumper');
        await driver.reload();
        await driver.load();
        await driver.save();
        assert.deepEqual(authoredState(await savedBytes(driver)), authoredState(construction), 'Save/Load preserves every authored bumper and payload declaration byte');
        const loaded = observations[observations.length - 1];
        assert.equal(loaded.bodiesLength(), 1, 'Loaded construction has one dynamic payload');
        const built = bodyState(loaded.bodies(0)!);
        assert.deepEqual(built, authored, 'Save/Load restores the exact placed payload');
        await driver.run();
        const samples: { time: number; y: number; vy: number }[] = [];
        const started = Date.now();
        while (Date.now() - started < duration) {
            const pose = await driver.readLatestPose();
            if (pose?.bodies[0]) samples.push({ time: Date.now() - started, y: pose.bodies[0].py, vy: pose.bodies[0].vy });
            await driver.page.waitForTimeout(20);
        }
        await driver.pauseSimulation();
        const paid = observations[observations.length - 1];
        assert.ok(paid.contactWorksLength() >= 1 && paid.occurrencesLength() >= 1);
        const remaining = paid.contactWorks(0)!.remainingEnergy();
        const debit = paid.occurrences(0)!.debit();
        const effect = paid.occurrences(0)!.effect();
        const count = paid.contactWorks(0)!.occurrenceCount();
        const history = await driver.readAnimationSamplesForTarget(String((2n << 32n) + 2n));
        assert.ok(history.some(value => value.value > .5), 'A paid impact must produce its gold-ring pulse');
        assert.equal(history[history.length - 1].value, 0, 'Paid pulse must return to neutral');
        const maximumUpwardSpeed = Math.max(...samples.map(value => value.vy));
        console.log(JSON.stringify({ kind, remaining, debit, effect, count, maximumUpwardSpeed, samples }));
        if (kind === 'basketball') {
            assert.ok(maximumUpwardSpeed > 7.2 && maximumUpwardSpeed <= 8.01, String(maximumUpwardSpeed));
            assert.ok(remaining > 0 && remaining < 32);
            assert.ok(debit > 0 && effect === 1 && count === 1);
            assert.ok(Math.abs(remaining + debit - 32) < 1e-5);
        } else {
            assert.ok(maximumUpwardSpeed > 3.5 && maximumUpwardSpeed < 6, '4kg payload must get only the affordable partial boost');
            assert.equal(remaining, 0, 'Partial first payout exhausts the finite store');
            assert.ok(count >= 2, 'The ball must return to the empty bumper');
            assert.equal(debit, 0, 'Later empty contact must not spend energy');
            assert.equal(effect, 0, 'Later empty contact is passive');
            const firstBoost = samples.findIndex(value => value.vy > 3.5);
            const firstFall = samples.findIndex((value, index) => index > firstBoost && value.vy < -.1);
            assert.ok(firstBoost >= 0 && firstFall > firstBoost, 'Paid launch must rise and then fall before the empty return');
            const returns = samples.slice(firstFall).filter(value => value.vy > .05);
            assert.ok(returns.length > 0 && Math.max(...returns.map(value => value.vy)) < 2, 'Empty contact retains a small ordinary bounce');
            const pulses = history.reduce((total, value, index) => total + (value.value > .5 && (index === 0 || history[index - 1].value <= .5) ? 1 : 0), 0);
            assert.equal(pulses, 1, 'Empty return must not create a second powered ring');
        }
        await driver.reset();
        const restored = observations[observations.length - 1];
        assert.deepEqual(bodyState(restored.bodies(0)!), built, 'Reset restores exact placed body state after saved construction reload');
        assert.equal(restored.contactWorks(0)!.remainingEnergy(), 32);
        assert.equal(restored.contactWorks(0)!.occurrenceCount(), 0);
        await driver.save();
        assert.deepEqual(authoredState(await savedBytes(driver)), authoredState(construction), 'Reset preserves the full authored construction, including bumper pose and strength');
        assert.equal(errors.length, 0, errors.join('\n'));
    } finally {
        await driver.close();
    }
}
test('CAT-015a paid default bumper conserves finite work and restores after Save/Load and Reset', {timeout:120000}, () => scenario('basketball', 1100));
test('CAT-015a heavy payload receives partial affordable boost, then ordinary unpaid bounce', {timeout:120000}, () => scenario('bowling', 3600));
