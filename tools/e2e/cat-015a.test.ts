// CAT-015a preloaded-contact preparation. Recharge/Battery and campaign admission remain separate prerequisites.
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';

// WorkshopWire.ReadContactWorkOffset; store record32, occurrence record32; bound to C# by the paid-record tests.
const WORK_READ = 3328, OCCURRENCE_READ = WORK_READ + 8 * 32;
const BASE_URL = process.env.BUMPER_PREVIEW_URL ?? 'http://127.0.0.1:8060/';
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

function authoredState(bytes: Buffer): Buffer {
    // Construction authority revision (save bytes24..31) is a new-session envelope, not authored part data.
    assert.ok(bytes.readBigUInt64LE(24) > 0n);
    return Buffer.concat([bytes.subarray(0, 24), bytes.subarray(32)]);
}

async function scenario(kind: 'basketball' | 'bowling', duration: number) {
    const driver = await WorkshopDriver.launch({ baseUrl: BASE_URL });
    const errors: string[] = [];
    const observations: Buffer[] = [];
    driver.page.on('console', message => {
        const text = message.text();
        if (/CCGPU_TRANSPORT_FAILURE|CCGPU_STARTUP_EXCEPTION|Unhandled exception|Impulse occurrence dropped/.test(text)) errors.push(text);
        if (text.startsWith('CCGPU ')) observations.push(Buffer.from(text.slice(6), 'base64'));
    });
    driver.page.on('pageerror', error => errors.push(error.message));
    try {
        await driver.selectTool(kind);
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('bumper', 150);
        const authored = observations[observations.length - 1].subarray(128, 192);
        await driver.save();
        const construction = await savedBytes(driver);
        const ballSlot = 24 + 320, bumperSlot = ballSlot + 160;
        assert.equal(construction.readFloatLE(bumperSlot + 104), 8, 'Placed bumper strength is8m/s');
        assert.equal(construction.readFloatLE(bumperSlot + 108), 1, 'Calibration reference mass is1kg');
        assert.equal(construction.readFloatLE(bumperSlot + 112), 32, 'Declared finite preload is32J');
        for (const offset of [48, 56, 72, 76])
            assert.equal(construction.readUInt16LE(ballSlot + offset), construction.readUInt16LE(bumperSlot + offset), 'Payload and bumper share the X/Z contact column');
        assert.equal(construction.readInt32LE(bumperSlot + 52), 48, 'Bumper is on the3m placement plane');
        assert.ok(construction.readInt32LE(ballSlot + 52) > 80, 'Payload is visibly lifted above the bumper');
        await driver.reload();
        await driver.load();
        await driver.save();
        assert.deepEqual(authoredState(await savedBytes(driver)), authoredState(construction), 'Save/Load preserves every authored bumper and payload declaration byte');
        const loaded = observations[observations.length - 1];
        assert.equal(loaded[104], 1, 'Loaded construction has one dynamic payload');
        const built = loaded.subarray(128, 192);
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
        assert.ok(paid.length > OCCURRENCE_READ + 32 && paid[108] === 1);
        const remaining = paid.readFloatLE(WORK_READ + 20);
        const debit = paid.readFloatLE(OCCURRENCE_READ + 26);
        const effect = paid[OCCURRENCE_READ + 30];
        const count = paid.readUInt32LE(WORK_READ + 16);
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
        assert.deepEqual(restored.subarray(128, 192), built, 'Reset restores exact placed body state after saved construction reload');
        assert.equal(restored.readFloatLE(WORK_READ + 20), 32);
        assert.equal(restored.readUInt32LE(WORK_READ + 16), 0);
        await driver.save();
        assert.deepEqual(authoredState(await savedBytes(driver)), authoredState(construction), 'Reset preserves the full authored construction, including bumper pose and strength');
        assert.equal(errors.length, 0, errors.join('\n'));
    } finally {
        await driver.close();
    }
}
test('CAT-015a paid default bumper conserves finite work and restores after Save/Load and Reset', {timeout:120000}, () => scenario('basketball', 1100));
test('CAT-015a heavy payload receives partial affordable boost, then ordinary unpaid bounce', {timeout:120000}, () => scenario('bowling', 3600));
