// Story 9R.1: decoded Run/Reset and authored-state checks, deferred to the post-catalogue E2E phase.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, test } from 'node:test';
import * as flatbuffers from 'flatbuffers';
import {
    AssistanceKnotRecordT, CadenceSettingsRecordT, CapabilityPulseRateRecordT, CapabilityWorkshopCadenceSettingsRecordT,
    ClockStampT, CommandOutcome, CompiledSceneRecordT, ConnectionRecordT, ElectricalEnable, InstanceRecordT, MemoryObservationRecordT, ParameterRecordT, PhysicalCommitRevisionRecordT,
    PuzzleRecordT, Quat4fT, ResponseKind, RunControlRecordT, SavedConstruction, SavedConstructionT,
    SimulationPhase, Vec3fT, Vec3iT, WireRootIdentifier, WorkshopResponse, WorkshopResponseT, ConstructionRecordT,
} from '../.wire/curious-contraptions/wire.ts';
import { authoredState, decodeSavedConstruction, decodeWorkshopResponse, encodeWorkshopResponse, type MockResponseParams } from '../workshop-wire.ts';
import { assertLoadRestores, assertRunAdvanced, assertSnapshotEqual, committedEnable, readSnapshot, snapshotFrom, snapshotValue } from '../run-reset-snapshot.ts';
import { pipeLength } from '../tube-mouths.ts';
import { rootIdentifier } from '../../runtime/wire-frame.ts';

const BALL = 0x20000000000001n;
const RECEIVER = 2n;
const BENCH = { bodies: 2 } as const;
const movedBody = { id: BALL, cell: { y: 1 }, local: { x: .25, y: 2.5 }, rotation: { w: 1 } };
const bodies: NonNullable<MockResponseParams['bodies']> = [
    { ...movedBody, local: { x: .25, y: 3 } },
    { id: RECEIVER, cell: { x: -2 }, local: { y: .6 } },
];

function response(overrides: Partial<MockResponseParams> = {}): Uint8Array {
    return encodeWorkshopResponse({
        sequence: 7n, kind: ResponseKind.Acknowledgement, outcome: CommandOutcome.Applied,
        phase: SimulationPhase.Building, epoch: 3n, tick: 0n, physicalRevision: 5n, revision: 11n,
        publication: 0n, masterGeneration: 1n, cadence: 1n, projection: 2n, session: [1, 2, 3, 4],
        bodies,
        captures: [{ sensor: 9n, target: BALL }],
        contactWorks: [{ id: 1n, owner: RECEIVER, occurrenceCount: 0, store: 1n }],
        energyStores: [{ id: 1n, owner: RECEIVER, bulk: 32 }],
        ...overrides,
    });
}

interface SaveOptions {
    revision?: bigint;
    pipeId?: bigint;
    length?: number | null;
    toSocket?: number;
    goalTarget?: bigint;
    connections?: number;
    atmosphere?: number;
    physical?: number;
    nextBodyId?: bigint;
    assistanceBlend?: number;
}

function saved(options: SaveOptions = {}): Uint8Array {
    const pipeId = options.pipeId ?? BALL;
    const pipe = Object.assign(new InstanceRecordT(), {
        id: pipeId, partId: 'pipe', cell: new Vec3iT(), local: new Vec3fT(), rotation: new Quat4fT(0, 0, 0, 1),
        parameters: options.length === null ? [] : [
            Object.assign(new ParameterRecordT(), { key: 'other', value: 12 }),
            Object.assign(new ParameterRecordT(), { key: 'length', value: options.length ?? 3.6 }),
        ],
    });
    const receiver = Object.assign(new InstanceRecordT(), { id: RECEIVER, partId: 'basket', locked: true });
    const puzzle = Object.assign(new PuzzleRecordT(), {
        id: 'snapshot-fixture', inventoryPart: 'pipe', inventoryCount: 1, goalKind: 1, goalBody: pipeId,
        goalTarget: options.goalTarget ?? RECEIVER,
        assistance: [Object.assign(new AssistanceKnotRecordT(), { blend: options.assistanceBlend ?? .25 })],
    });
    const settings = Object.assign(new CadenceSettingsRecordT(), {
        simulation: 2, physical: options.physical ?? 1, animation: 2, presentation: 2, presentationRateNumerator: 60, presentationRateDenominator: 1,
    });
    const construction = Object.assign(new ConstructionRecordT(), {
        revision: options.revision ?? 5n, settings, atmosphere: options.atmosphere ?? 1, puzzle, instances: [pipe, receiver],
        connections: Array.from({ length: options.connections ?? 1 }, () => Object.assign(new ConnectionRecordT(), {
            source: pipeId, target: RECEIVER, domain: 1, fromSocket: 2, toSocket: options.toSocket ?? 3,
        })),
    });
    const builder = new flatbuffers.Builder(512);
    const root = Object.assign(new SavedConstructionT(), { nextBodyId: options.nextBodyId ?? BALL + 1n, construction });
    SavedConstruction.finishSizePrefixedSavedConstructionBuffer(builder, root.pack(builder));
    return builder.asUint8Array();
}

function snapshot(read = response(), save = saved()) {
    return snapshotFrom(read, save, BENCH);
}

function packedResponse(decoded: WorkshopResponseT): Uint8Array {
    const builder = new flatbuffers.Builder(512);
    builder.finishSizePrefixed(decoded.pack(builder), rootIdentifier(WireRootIdentifier.WorkshopResponse));
    return builder.asUint8Array();
}

describe('Run/Reset snapshot decoded fields and identity', () => {
    test('canonical values include booleans, strings, bigint IDs, f32 metres and vector lengths', () => {
        const decoded = snapshot();
        assert.equal(snapshotValue(decoded, `read.bodies[id=${BALL}].id`), BALL);
        assert.equal(snapshotValue(decoded, 'saved.construction.instances[id=2].locked'), true);
        assert.equal(snapshotValue(decoded, `saved.construction.instances[id=${BALL}].partId`), 'pipe');
        assert.equal(snapshotValue(decoded, 'read.ropes.length'), 0);
        assert.equal(snapshotValue(decoded, 'read.scene'), null);
        assert.throws(() => pipeLength(decoded, Number(RECEIVER)), /expected pipe/);
    });

    test('Pipe length selects its parameter key even when another parameter precedes it', () => {
        const pipeId = 1;
        const pipeSnapshot = (length?: number | null) => snapshot(response(), saved({ pipeId: BigInt(pipeId), length }));
        assert.equal(pipeLength(pipeSnapshot(), pipeId), Math.fround(3.6));
        assert.throws(() => pipeLength(pipeSnapshot(null), pipeId), /saved no length/);
    });

    test('a changed decoded physical value names the field and both values', () => {
        const moved = response({ bodies: [movedBody, bodies[1]] });
        assert.throws(() => assertSnapshotEqual(snapshot(), snapshot(moved)), /read\.bodies\[id=9007199254740993\]\.local\.y 3≠2\.5/);
    });

    test('negative zero differs from positive zero and decoded NaNs compare equal', () => {
        const atX = (x: number) => snapshot(response({ bodies: [{ ...bodies[0], velocity: { x } }, bodies[1]] }));
        assert.throws(() => assertSnapshotEqual(atX(0), atX(-0)), /velocity\.x 0≠-0/);
        assertSnapshotEqual(atX(Number.NaN), atX(Number.NaN));
    });

    test('an omitted and explicitly encoded scalar default compare equally', () => {
        function atZero(writeDefault: boolean): Uint8Array {
            const builder = new flatbuffers.Builder(128);
            WorkshopResponse.startWorkshopResponse(builder);
            builder.forceDefaults(writeDefault);
            WorkshopResponse.addTick(builder, 0n);
            builder.forceDefaults(false);
            WorkshopResponse.addPhysicalCommit(builder, new PhysicalCommitRevisionRecordT().pack(builder));
            builder.finishSizePrefixed(WorkshopResponse.endWorkshopResponse(builder), rootIdentifier(WireRootIdentifier.WorkshopResponse));
            return builder.asUint8Array();
        }
        assert.equal(snapshotValue(readSnapshot(atZero(false)), 'read.tick'), 0n);
        assertSnapshotEqual(readSnapshot(atZero(false)), readSnapshot(atZero(true)));
    });

    test('identity-led rows can reorder but duplicate bigint IDs fail', () => {
        assertSnapshotEqual(snapshot(), snapshot(response({ bodies: [bodies[1], bodies[0]] })));
        assert.throws(() => snapshot(response({ bodies: [bodies[0], { ...bodies[1], id: BALL }] })), /duplicate path.*bodies\[id=9007199254740993\]/);
    });

    test('rows beginning with source stay indexed even with a bigint id field', () => {
        const decoded = decodeWorkshopResponse(response()).unpack();
        const setting = Object.assign(new CapabilityWorkshopCadenceSettingsRecordT(), {
            presentationRate: new CapabilityPulseRateRecordT(60, 1),
        });
        const first = Object.assign(new RunControlRecordT(), { source: 5n, id: BALL });
        const second = Object.assign(new RunControlRecordT(), { source: 6n, id: BALL });
        decoded.scene = Object.assign(new CompiledSceneRecordT(), { settings: setting, runControls: [first, second] });
        const before = readSnapshot(packedResponse(decoded));
        assert.equal(snapshotValue(before, 'read.scene.runControls[1].id'), BALL);
        decoded.scene.runControls.reverse();
        assert.throws(() => assertSnapshotEqual(before, readSnapshot(packedResponse(decoded))), /runControls\[0\]\.source/);
    });

    test('a changed authored link, parameter or goal fails at its decoded path', () => {
        for (const [options, path] of [
            [{ toSocket: 4 }, /connections\[0\]\.toSocket 3≠4/],
            [{ length: 4 }, /parameters\[1\]\.value/],
            [{ goalTarget: 9n }, /puzzle\.goalTarget 2n≠9n/],
        ] as const)
            assert.throws(() => assertSnapshotEqual(snapshot(), snapshot(response(), saved(options))), path);
    });

    test('differences in path sets fail in either direction even when existing values agree', () => {
        const one = snapshot();
        const extra = { ...one, entries: [...one.entries, ['read.extra', 0] as const] };
        assert.throws(() => assertSnapshotEqual(one, extra), /path set differs: read\.extra is after Reset only/);
        assert.throws(() => assertSnapshotEqual(extra, one), /path set differs: read\.extra is in S0 only/);
        const last = one.entries.at(-1);
        assert.ok(last);
        const changedValue = last[1] === false;
        const changed = { ...one, entries: [...one.entries.slice(0, -1), [last[0], changedValue] as const] };
        assert.throws(() => assertSnapshotEqual(one, changed),
            (error: unknown) => error instanceof Error && error.message.startsWith(`Reset: ${last[0]} `));
        assert.throws(() => assertSnapshotEqual(one, snapshot(response(), saved({ connections: 2 }))), /connections\.length 1≠2/);
    });

    test('Reset catches a residual tick even when every other value matches', () => {
        assert.throws(() => assertSnapshotEqual(snapshot(), snapshot(response({ tick: 1n }))), /Reset: read\.tick 0n≠1n/);
    });

    test('Reset catches a changed committed contact occurrence count', () => {
        const changed = response({ contactWorks: [{ id: 1n, owner: RECEIVER, occurrenceCount: 1, store: 1n }] });
        assert.throws(() => assertSnapshotEqual(snapshot(), snapshot(changed)), /Reset: read\.contactWorks\[id=1\]\.occurrenceCount 0≠1/);
    });

    test('advancing transport fields and encoded motion are excluded by exact response path', () => {
        const original = snapshot();
        const updated = response({ sequence: 99n, revision: 98n, publication: 77n, epoch: 97n, masterGeneration: 96n,
            cadence: 95n, projection: 94n, session: [9, 9, 9, 9] });
        const decoded = decodeWorkshopResponse(updated).unpack();
        decoded.motion = [2, 17];
        decoded.capture = Object.assign(new ClockStampT(), { time: 22n });
        assertSnapshotEqual(original, snapshot(packedResponse(decoded)));
        assert.throws(() => snapshotValue(original, 'read.motion.length'), /no path/);
        assert.throws(() => assertSnapshotEqual(original, snapshot(response(), saved({ revision: 6n }))),
            /saved\.construction\.revision 5n≠6n/);
    });

    test('Load ignores only the value of construction revision and still compares admission identity and body values', () => {
        const original = snapshot();
        assertLoadRestores(original, snapshot(response(), saved({ revision: 3n })));
        assert.throws(() => assertLoadRestores(original, snapshot(response({ physicalRevision: 6n }), saved({ revision: 3n }))),
            /Load: read\.physicalCommit\.value 5n≠6n/);
        assert.throws(() => assertSnapshotEqual(original, snapshot(response({ physicalRevision: 6n }))), /Reset: read\.physicalCommit\.value/);
        assert.throws(() => assertLoadRestores(original, snapshot(response({ bodies: [movedBody, bodies[1]] }), saved())), /Load: read\.bodies.*local\.y/);
        assert.throws(() => assertLoadRestores(original, snapshot(response(), saved({ revision: 3n, length: 4 }))), /Load: saved.*parameters/);
        const missing = { ...original, entries: original.entries.filter(([path]) => path !== 'saved.construction.revision') };
        assert.throws(() => assertLoadRestores(original, missing), /saved\.construction\.revision is in S0 only/);
    });

    test('the save decoder and authored projection require current construction settings', () => {
        const fixture = new URL('../../../CuriousContraptions.tests/Fixtures/pre-versionless/retired-versioned-construction.save.base64', import.meta.url);
        const retired = Buffer.from(readFileSync(fixture, 'utf8'), 'base64');
        assert.throws(() => decodeSavedConstruction(retired), /without its cadence settings/);
        assert.throws(() => authoredState(retired), /without its cadence settings/);

        const current = saved();
        assert.equal(decodeSavedConstruction(current).construction()?.settings()?.physical(), 1);
        assert.equal(authoredState(current).construction.settings?.physical, 1);
    });

    test('authored projection retains settings, atmosphere, next identity and full puzzle data', () => {
        const original = authoredState(saved());
        assert.deepEqual(original, authoredState(saved({ revision: 3n })));
        assert.equal(original.nextBodyId, BALL + 1n);
        assert.equal(original.construction.puzzle?.id, 'snapshot-fixture');
        assert.equal(original.construction.puzzle?.assistance[0]?.blend, .25);
        assert.equal(original.construction.settings?.physical, 1);
        assert.equal(original.construction.atmosphere, 1);
        for (const change of [{ length: 4 }, { physical: 2 }, { atmosphere: 2 }, { nextBodyId: BALL + 2n }, { assistanceBlend: .5 }])
            assert.notDeepEqual(authoredState(saved(change)), original);
    });

    test('the mock response keeps motion absent and resolves optional defaults without overriding zero or false', () => {
        const read = decodeWorkshopResponse(response({
            bodies: [{ id: BALL, rotation: { w: undefined } }, { id: RECEIVER, rotation: { w: 0 } }],
            electrical: [{ id: undefined, enabled: undefined, available: undefined }, { id: 2n, enabled: ElectricalEnable.Disabled, available: false }],
        }));
        assert.equal(read.motionArray(), null);
        assert.equal(read.bodies(0)?.rotation()?.w(), 1);
        assert.equal(read.bodies(1)?.rotation()?.w(), 0);
        assert.deepEqual([read.electrical(0)?.id(), read.electrical(0)?.enabled(), read.electrical(0)?.available()], [1n, ElectricalEnable.Enabled, true]);
        assert.deepEqual([read.electrical(1)?.id(), read.electrical(1)?.enabled(), read.electrical(1)?.available()], [2n, ElectricalEnable.Disabled, false]);
    });

    test('committed electrical enable selects the exact bigint owner and refuses a missing owner', () => {
        const other = BALL - 1n;
        const read = snapshot(response({
            electrical: [
                { id: 11n, owner: BALL, enabled: ElectricalEnable.Disabled },
                { id: 12n, owner: other, enabled: ElectricalEnable.Enabled },
            ],
        }));
        assert.equal(committedEnable(read, BALL), ElectricalEnable.Disabled);
        assert.equal(committedEnable(read, other), ElectricalEnable.Enabled);
        assert.throws(() => committedEnable(read, 11n), /no electrical source owned by 11/);
    });

    test('committed enable refuses opposite source states for the same owner', () => {
        const read = snapshot(response({
            electrical: [
                { id: 11n, owner: BALL, enabled: ElectricalEnable.Disabled },
                { id: 12n, owner: BALL, enabled: ElectricalEnable.Enabled },
            ],
        }));
        assert.throws(() => committedEnable(read, BALL), /multiple electrical sources owned by 9007199254740993/);
    });

    test('committed enable refuses an undefined decoded electrical byte', () => {
        const decoded = decodeWorkshopResponse(response({ electrical: [{ owner: BALL }] })).unpack();
        decoded.electrical[0].enabled = 255; // The generated scalar carries the malformed wire value.
        const read = snapshot(packedResponse(decoded));
        assert.throws(() => committedEnable(read, BALL), /invalid enable value 255/);
    });

    test('compared activation memory keeps counter and clock state in generated value types', () => {
        const clock = { node: 2n, kind: 7, count: 0 };
        const counter = { node: 3n, kind: 6, count: 0 };
        const before = snapshot(response({ activationMemory: [clock, counter] }));
        const changedClock = snapshot(response({ activationMemory: [{ ...clock, phase: 1, dueTick: 120n }, counter] }));
        const changedCounter = snapshot(response({ activationMemory: [clock, { ...counter, count: 2, lastOrdinal: 480, lastPhase: .5 }] }));
        assert.equal(changedClock.activationMemory[0].dueTick, 120n);
        assert.equal(changedCounter.activationMemory[1].lastOrdinal, 480);
        assert.equal(changedCounter.activationMemory[1].lastPhase, .5);
        assert.throws(() => assertSnapshotEqual(before, changedClock), /read\.activationMemory\[0\]\.phase 0≠1/);
        assert.throws(() => assertSnapshotEqual(before, changedCounter), /read\.activationMemory\[1\]\.count 0≠2/);
    });
});

describe('Run/Reset admission checks and physical change', () => {
    test('a static bench admits exactly zero bodies and retains the saved revision', () => {
        const noBodies = response({ bodies: [] });
        const original = snapshotFrom(noBodies, saved(), { bodies: 0 });
        const restarted = snapshotFrom(noBodies, saved({ revision: 3n }), { bodies: 0 });
        assert.equal(original.bodies, 0);
        assert.equal(snapshotValue(original, 'saved.construction.revision'), 5n);
        assert.throws(() => assertSnapshotEqual(original, restarted), /saved\.construction\.revision 5n≠3n/);
        assertLoadRestores(original, restarted);
        assert.throws(() => snapshotFrom(response(), saved(), { bodies: 0 }), /2 bodies, the bench has 0/);
    });

    test('snapshot refuses missing expected bodies or wrong body populations, non-Applied acknowledgements and invalid save frames', () => {
        assert.throws(() => snapshot(response({ bodies: [] })), /no bodies/);
        assert.throws(() => snapshotFrom(response(), saved(), { bodies: 3 }), /2 bodies, the bench has 3/);
        assert.throws(() => snapshot(response({ outcome: CommandOutcome.Rejected })), /Applied/);
        assert.throws(() => snapshot(response(), new Uint8Array()), /not a SavedConstruction/);
    });

    test('Run rejects a zero tick and commits that only change tick, phase and physical identity', () => {
        const before = snapshot();
        const running = { phase: SimulationPhase.Running, physicalRevision: 7n };
        assert.throws(() => assertRunAdvanced(before, readSnapshot(response(running))), /tick 0/);
        assert.throws(() => assertRunAdvanced(before, readSnapshot(response({ ...running, tick: 1n }))), /no compared path differs/);
        const handshake = readSnapshot(response({ ...running, tick: 1n, kind: ResponseKind.Read, reason: 1 }));
        assert.throws(() => assertRunAdvanced(before, handshake), /no compared path differs/);
        assertRunAdvanced(before, readSnapshot(response({ ...running, tick: 1n, bodies: [movedBody, bodies[1]] })));
    });

    test('a signed-zero-only change does not witness Run and still differs for Reset and Load', () => {
        const bodiesWithNegativeZero = [{ ...bodies[0], velocity: { x: -0 } }, bodies[1]];
        const before = snapshot();
        const during = readSnapshot(response({ tick: 1n, bodies: bodiesWithNegativeZero }));
        assert.throws(() => assertRunAdvanced(before, during), /no compared path differs/);
        const changed = snapshot(response({ bodies: bodiesWithNegativeZero }));
        assert.throws(() => assertSnapshotEqual(before, changed), /Reset: .*velocity\.x 0≠-0/);
        assert.throws(() => assertLoadRestores(before, changed), /Load: .*velocity\.x 0≠-0/);
    });

    test('a nonfinite-only change in either direction does not witness Run', () => {
        function withVelocity(x: number, tick: bigint) {
            return readSnapshot(response({ tick, bodies: [{ ...bodies[0], velocity: { x } }, bodies[1]] }));
        }
        for (const value of [NaN, Infinity, -Infinity]) {
            assert.throws(() => assertRunAdvanced(withVelocity(1, 0n), withVelocity(value, 1n)), /no compared path differs/);
            assert.throws(() => assertRunAdvanced(withVelocity(value, 0n), withVelocity(1, 1n)), /no compared path differs/);
        }
    });

    test('different decoded memory diagnostics do not witness Run and still differ for Reset and Load', () => {
        function diagnostic(usedAfter: bigint, tick = 0n, count = 0) {
            const decoded = decodeWorkshopResponse(response({
                tick, activationMemory: [{ node: 3n, kind: 6, count }],
            })).unpack();
            decoded.memory = Object.assign(new MemoryObservationRecordT(), { usedBefore: 100n, usedAfter });
            return snapshot(packedResponse(decoded));
        }
        const before = diagnostic(110n);
        assert.throws(() => assertRunAdvanced(before, diagnostic(120n, 1n)), /no compared path differs/);
        assert.throws(() => assertSnapshotEqual(before, diagnostic(120n)), /Reset: read\.memory\.usedAfter 110n≠120n/);
        assert.throws(() => assertLoadRestores(before, diagnostic(120n)), /Load: read\.memory\.usedAfter 110n≠120n/);
        assertRunAdvanced(before, diagnostic(120n, 1n, 1));
    });
});
