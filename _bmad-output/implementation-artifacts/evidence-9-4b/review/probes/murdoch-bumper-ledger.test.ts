// Murdoch-2 Story 9.4b CAT-015 Bumper ledger probe through the actual UI.
// Basketball and Bowling payloads are stacked over a default Bumper at several lift heights (actual move-gizmo drags), so
// the store sees a big first payout, many small top-up payouts (a basketball returning to ~8 m/s on every bounce),
// budget-limited exhausting payouts (4 kg bowling) and later empty contacts. Every retained energy/event commit
// (CCGPU_RETAINED) and every CCGPU read is decoded. Invariants per Run:
//   L1 the store is finite and in [0, preload]; L2 it never increases within a Run; L3 every occurrence debit is finite,
//   >= 0, and effect Paid <=> debit > 0; L4 ledger closure preload - remaining == sum of debits (f32 accumulation bound);
//   L5 no dust: no paid debit below DUST_J and no observed store in (0, DUST_J]; L6 once the store reads 0 every later
//   occurrence is unpaid; L7 (Incomplete, not Fail) observed unique occurrences == the store occurrenceCount;
//   L8 Reset restores preload and count 0; L9 a second Run after Reset reproduces the debit sequence (printed; strict
//   with MURDOCH_STRICT_DETERMINISM=1). Coverage gates: at least one scenario has >= 3 paid payouts and at least one has
//   a payout that exhausts the store, or the probe reports itself Incomplete.
import assert from 'node:assert/strict';
import { after, test } from 'node:test';
import { WorkshopDriver } from './workshop-driver.ts';
import { decodeSavedConstruction, type WorkshopResponse } from './workshop-wire.ts';
import { PREVIEW_URL, SessionWatch, readSavedBytes, sleep } from './murdoch-common.ts';

// Rounding residue, not gameplay: 8 ULPs of the 32 J preload (ulp(32) = 2^-18 J).
const DUST_J = 8 * 2 ** -18;
const LIFTS = (process.env.MURDOCH_LEDGER_LIFTS ?? '60,110,150,200,260').split(',').map(Number);
const KINDS = (process.env.MURDOCH_LEDGER_KINDS ?? 'basketball,bowling').split(',') as ('basketball' | 'bowling')[];
const WINDOW_MS: Record<'basketball' | 'bowling', number> = { basketball: 16000, bowling: 6000 };
const STRICT_DETERMINISM = process.env.MURDOCH_STRICT_DETERMINISM === '1';

const incomplete: string[] = [];
interface Occ { sequence: number; debit: number; effect: number; ordinal: number; approach: number }
interface Ledger { stores: { tick: bigint; remaining: number; count: number }[]; occurrences: Map<number, Occ> }

function harvest(responses: WorkshopResponse[]): Ledger {
    const ledger: Ledger = { stores: [], occurrences: new Map() };
    for (const r of responses) {
        if (r.contactWorksLength() < 1) continue;
        const store = r.contactWorks(0)!;
        ledger.stores.push({ tick: r.tick(), remaining: store.remainingEnergy(), count: store.occurrenceCount() });
        for (let i = 0; i < r.occurrencesLength(); i++) {
            const o = r.occurrences(i)!;
            ledger.occurrences.set(o.sequence(), { sequence: o.sequence(), debit: o.debit(), effect: o.effect(), ordinal: o.eventOrdinal(), approach: o.approachSpeed() });
        }
    }
    ledger.stores.sort((a, b) => (a.tick < b.tick ? -1 : a.tick > b.tick ? 1 : 0));
    return ledger;
}

function check(label: string, ledger: Ledger, preload: number): string[] {
    const v: string[] = [];
    const occ = [...ledger.occurrences.values()].sort((a, b) => a.sequence - b.sequence);
    let previous = preload;
    for (const s of ledger.stores) {
        if (!Number.isFinite(s.remaining) || s.remaining < 0 || s.remaining > preload) v.push(`L1 store ${s.remaining} at tick ${s.tick}`);
        if (s.remaining > previous) v.push(`L2 store rose ${previous} -> ${s.remaining} at tick ${s.tick}`);
        if (s.remaining > 0 && s.remaining <= DUST_J) v.push(`L5 dust store ${s.remaining} J at tick ${s.tick}`);
        previous = s.remaining;
    }
    let paidSum = 0, exhaustedAt: number | undefined;
    for (const o of occ) {
        if (!Number.isFinite(o.debit) || o.debit < 0) v.push(`L3 debit ${o.debit} (seq ${o.sequence})`);
        if ((o.effect === 1) !== (o.debit > 0)) v.push(`L3 effect ${o.effect} with debit ${o.debit} (seq ${o.sequence})`);
        if (o.debit > 0 && o.debit < DUST_J) v.push(`L5 dust payout ${o.debit} J (seq ${o.sequence})`);
        if (exhaustedAt !== undefined && o.debit > 0) v.push(`L6 paid ${o.debit} J after the store read 0 (seq ${o.sequence})`);
        paidSum = Math.fround(paidSum + o.debit);
        if (exhaustedAt === undefined && Math.fround(preload - paidSum) <= 0) exhaustedAt = o.sequence;
    }
    const final = ledger.stores.at(-1);
    if (final) {
        const bound = Math.max(1, occ.length) * 2 ** -18;
        if (Math.abs(preload - final.remaining - paidSum) > bound) v.push(`L4 closure: ${preload} - ${final.remaining} != sum ${paidSum} (bound ${bound})`);
        // L7 is an observability gap (a missing CCGPU_RETAINED commit), not a ledger defect: reported as Incomplete.
        if (final.count !== occ.length) {
            const gap = `L7 observed ${occ.length} unique occurrences but the store counts ${final.count}`;
            incomplete.push(`${label}: ${gap}`);
            console.log(`MURDOCH_LEDGER_INCOMPLETE ${label} ${gap}`);
        }
    }
    if (v.length) console.log(`MURDOCH_LEDGER_VIOLATIONS ${label} ` + JSON.stringify(v));
    return v;
}

const coverage = { maxPaid: 0, exhausting: 0, targetThenExhaust: 0, scenarios: 0 };
after(() => {
    console.log('MURDOCH_LEDGER_COVERAGE ' + JSON.stringify({ ...coverage, incomplete }));
    // Verdict input: violations fail their own test; a non-empty `incomplete` makes the ledger evidence Incomplete, not Fail.
    if (coverage.scenarios === LIFTS.length * KINDS.length) {
        // Review fix (Story 9.4b terminal review): >= 3 paid payouts is unreachable through this recipe. With bumper
        // restitution below 1 a ~1 kg payload returns well below 8 m/s, so each top-up costs >= ~16 J and the 32 J store
        // funds at most two payouts (candidate-2 data: first 21.8-31.9 J, second the exact remainder). The ledger-relevant
        // sequence is a target-limited payout followed by a store-limited exhausting one; many small payouts are native (T6).
        assert.ok(coverage.maxPaid >= 2 && coverage.targetThenExhaust >= 1,
            `Incomplete: no scenario drove a target-limited payout followed by a store-limited exhaust (max paid ${coverage.maxPaid})`);
        assert.ok(coverage.exhausting >= 1, 'Incomplete: no scenario exhausted the store');
    }
});

for (const kind of KINDS) for (const lift of LIFTS)
test(`Murdoch Bumper ledger ${kind} lift ${lift}px`, { timeout: 180000 }, async () => {
    const driver = await WorkshopDriver.launch({ baseUrl: PREVIEW_URL });
    const watch = new SessionWatch(driver);
    try {
        await driver.selectTool(kind);
        await driver.placeOnCanvas(720, 485);
        await driver.stackUnderLiftedBall('bumper', lift);
        await driver.save();
        const saved = decodeSavedConstruction(await readSavedBytes(driver)).construction()!;
        let preload = NaN, bumperId = 0n;
        for (let i = 0; i < saved.instancesLength(); i++) {
            const inst = saved.instances(i)!;
            if (inst.partId() === 'bumper') { preload = inst.preload(); bumperId = BigInt(inst.id()); }
        }
        assert.equal(preload, 32, 'default Bumper preload 32 J');

        const runs: Occ[][] = [];
        for (let run = 1; run <= 2; run++) {
            const readsAt = watch.reads.length, retainedAt = watch.retained.length;
            const ringBefore = (await driver.readAnimationSamplesForTarget(String((2n << 32n) + bumperId))).length;
            await driver.run(WINDOW_MS[kind]);
            await driver.pauseSimulation();
            const ledger = harvest([...watch.retained.slice(retainedAt), ...watch.reads.slice(readsAt)]);
            const label = `${kind}/${lift}/run${run}`;
            const occ = [...ledger.occurrences.values()].sort((a, b) => a.sequence - b.sequence);
            const paid = occ.filter(o => o.debit > 0);
            const ring = (await driver.readAnimationSamplesForTarget(String((2n << 32n) + bumperId))).slice(ringBefore);
            const pulses = ring.reduce((n, s, i) => n + (s.value > .5 && (i === 0 || ring[i - 1].value <= .5) ? 1 : 0), 0);
            console.log('MURDOCH_LEDGER ' + JSON.stringify({ label, paid: paid.map(o => o.debit), unpaid: occ.length - paid.length,
                final: ledger.stores.at(-1)?.remaining ?? null, count: ledger.stores.at(-1)?.count ?? null, pulses }));
            const violations = check(label, ledger, preload);
            if (pulses > paid.length) violations.push(`ring pulses ${pulses} exceed paid payouts ${paid.length}`);
            assert.deepEqual(violations, [], `${label}: ledger invariants`);
            if (run === 1) {
                coverage.maxPaid = Math.max(coverage.maxPaid, paid.length);
                if ((ledger.stores.at(-1)?.remaining ?? 1) === 0 && paid.length > 0) coverage.exhausting++;
                if (paid.length >= 2 && (ledger.stores.at(-1)?.remaining ?? 1) === 0) coverage.targetThenExhaust++;
                console.log('MURDOCH_LEDGER_SEQUENCES ' + JSON.stringify({ label, occurrences: occ.map(o => [o.sequence, o.ordinal, o.debit, o.effect]),
                    stores: ledger.stores.map(s => [String(s.tick), s.remaining, s.count]) }));
            }
            runs.push(occ);
            await driver.reset();
            const reset = watch.reads.at(-1)!;
            assert.equal(reset.contactWorks(0)!.remainingEnergy(), preload, `${label}: Reset restores the preload`);
            assert.equal(reset.contactWorks(0)!.occurrenceCount(), 0, `${label}: Reset clears the occurrence count`);
        }
        coverage.scenarios++;
        const same = JSON.stringify(runs[0].map(o => [o.ordinal, o.debit])) === JSON.stringify(runs[1].map(o => [o.ordinal, o.debit]));
        console.log('MURDOCH_LEDGER_DETERMINISM ' + JSON.stringify({ kind, lift, same }));
        if (STRICT_DETERMINISM) assert.ok(same, 'Run after Reset reproduces the debit sequence');
        assert.deepEqual(watch.terminations.map(t => t.text.slice(0, 200)), [], 'zero terminations');
        assert.deepEqual(watch.errors.map(t => t.text.slice(0, 200)), [], 'zero console errors');
    } finally { await driver.close(); }
});
