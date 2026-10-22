// Story 9R.1: browserless unit checks of the Run/Reset snapshot authority (tools/e2e/run-reset-snapshot.ts).
// Run: node --import <tsx loader> --test tools/e2e/unit/run-reset-snapshot.test.ts (not a Chrome suite: tools/e2e/unit/ is
// outside the runner's tools/e2e/*.test.ts catalogue).
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, test } from 'node:test';
import * as flatbuffers from 'flatbuffers';
import {
    ConnectionRecord, ConstructionRecord, InstanceRecord, PuzzleRecord, Quat4f, SavedConstruction, Vec3f, Vec3i,
    decodeWorkshopResponse, encodeWorkshopResponse, type MockResponseParams,
} from '../workshop-wire.ts';
import * as generated from '../workshop-wire.ts';
import {
    SNAPSHOT_SCHEMA, assertLoadRestores, assertRunAdvanced, assertSnapshotEqual, readSnapshot, snapshotFrom, structLayout, tableSlot,
} from '../run-reset-snapshot.ts';

const SCHEMA_SOURCE = readFileSync(new URL('../../../schemas/workshop_wire.fbs', import.meta.url), 'utf8');
const GENERATED_SOURCE = readFileSync(new URL('../workshop-wire.ts', import.meta.url), 'utf8');

// ---- fixtures -------------------------------------------------------------------------------------------------------

const BALL = 1n, RECEIVER = 2n;

function response(overrides: Partial<MockResponseParams> = {}): Uint8Array {
    return new Uint8Array(encodeWorkshopResponse({
        sequence: 7n, kind: 0, outcome: 0, reason: 0, phase: 2, epoch: 3n, tick: 0n, revision: 11n,
        publication: 0n, masterGeneration: 1n, cadence: 1n, projection: 2n, session: [1, 2, 3, 4],
        bodies: [
            { id: BALL, cell: { x: 0, y: 1, z: 0 }, local: { x: 0.25, y: 3, z: 0 }, rotation: { w: 1 }, velocity: { x: 0, y: 0, z: 0 } },
            { id: RECEIVER, cell: { x: -2, y: 0, z: 0 }, local: { x: 0, y: 0.6, z: 0 }, rotation: { w: 1 } },
        ],
        captures: [{ sensor: 9n, phase: 0, eventOrdinal: 0, eventPhase: 0 }],
        occurrences: [{ work: 1, collider: 2, target: BALL, sequence: 0, eventOrdinal: 0 }],
        ...overrides,
    }));
}

interface SavedOptions { mass?: number; toSocket?: number; revision?: bigint; goalTarget?: bigint; connections?: number }
function saved(options: SavedOptions = {}): Uint8Array {
    const builder = new flatbuffers.Builder(1024);
    const instance = (id: bigint, part: string, mass: number) => {
        const name = builder.createString(part);
        InstanceRecord.startInstanceRecord(builder);
        InstanceRecord.addId(builder, id);
        InstanceRecord.addPartId(builder, name);
        InstanceRecord.addLocked(builder, true);
        InstanceRecord.addCell(builder, Vec3i.createVec3i(builder, 0, 1, 0));
        InstanceRecord.addLocal(builder, Vec3f.createVec3f(builder, 0.25, 3, 0));
        InstanceRecord.addRotation(builder, Quat4f.createQuat4f(builder, 0, 0, 0, 1));
        InstanceRecord.addMass(builder, mass);
        return InstanceRecord.endInstanceRecord(builder);
    };
    const instances = [instance(BALL, 'basketball', options.mass ?? 0), instance(RECEIVER, 'receiver', 0)];
    const instancesVector = ConstructionRecord.createInstancesVector(builder, instances);
    const connectionCount = options.connections ?? 1;
    ConstructionRecord.startConnectionsVector(builder, connectionCount);
    for (let i = connectionCount - 1; i >= 0; i--)
        ConnectionRecord.createConnectionRecord(builder, BALL, RECEIVER, 1, 2, options.toSocket ?? 3);
    const connectionsVector = builder.endVector();
    const inventory = builder.createString('pipe');
    PuzzleRecord.startPuzzleRecord(builder);
    PuzzleRecord.addId(builder, 4);
    PuzzleRecord.addInventoryPart(builder, inventory);
    PuzzleRecord.addInventoryCount(builder, 1);
    PuzzleRecord.addGoalKind(builder, 1);
    PuzzleRecord.addGoalBody(builder, BALL);
    PuzzleRecord.addGoalTarget(builder, options.goalTarget ?? RECEIVER);
    const puzzle = PuzzleRecord.endPuzzleRecord(builder);
    ConstructionRecord.startConstructionRecord(builder);
    ConstructionRecord.addRevision(builder, options.revision ?? 5n);
    ConstructionRecord.addPuzzle(builder, puzzle);
    ConstructionRecord.addInstances(builder, instancesVector);
    ConstructionRecord.addConnections(builder, connectionsVector);
    const construction = ConstructionRecord.endConstructionRecord(builder);
    SavedConstruction.startSavedConstruction(builder);
    SavedConstruction.addNextBodyId(builder, 3n);
    SavedConstruction.addConstruction(builder, construction);
    builder.finishSizePrefixed(SavedConstruction.endSavedConstruction(builder), 'CCWS');
    return builder.asUint8Array().slice();
}

const BENCH = { bodies: 2 } as const;

// The byte position of the ball's local.y inside a response buffer, taken from the generated accessor.
function ballLocalY(bytes: Uint8Array): number {
    const body = decodeWorkshopResponse(bytes).bodies(0)!;
    return body.local()!.bb_pos + 4;
}
function withU32(bytes: Uint8Array, position: number, value: number): Uint8Array {
    const copy = bytes.slice();
    new DataView(copy.buffer, copy.byteOffset, copy.byteLength).setUint32(position, value, true);
    return copy;
}
function failureOf(work: () => void): string {
    try { work(); } catch (error) { return error instanceof Error ? error.message : String(error); }
    assert.fail('expected the snapshot assertion to fail');
}

// ---- schema listing ---------------------------------------------------------------------------------------------------

// A minimal reader of schemas/workshop_wire.fbs: struct and table bodies as [field, type] lists, and enum underlying types.
function parseSchema(source: string): { types: Map<string, { kind: 'struct' | 'table'; fields: [string, string][] }>; enums: Map<string, string> } {
    const types = new Map<string, { kind: 'struct' | 'table'; fields: [string, string][] }>();
    const enums = new Map<string, string>();
    const stripped = source.replace(/\/\/.*$/gm, '');
    for (const match of stripped.matchAll(/\b(struct|table|enum)\s+(\w+)\s*(?::\s*(\w+))?\s*\{([^}]*)\}/g)) {
        const [, kind, name, underlying, body] = match;
        if (kind === 'enum') { enums.set(name, underlying); continue; }
        const fields = [...body.matchAll(/(\w+)\s*:\s*([\w\[\]]+)\s*(?:\(required\)\s*)?;/g)].map(field => [field[1], field[2]] as [string, string]);
        types.set(name, { kind: kind as 'struct' | 'table', fields });
    }
    return { types, enums };
}

// Offsets the generated TypeScript accessors read: struct fields at `this.bb_pos + N`, table fields at vtable slot N.
function generatedOffsets(className: string): Map<string, number> {
    const start = GENERATED_SOURCE.indexOf(`export class ${className} {`);
    assert.ok(start >= 0, `generated class ${className}`);
    const end = GENERATED_SOURCE.indexOf('\nexport class ', start + 1);
    const body = GENERATED_SOURCE.slice(start, end < 0 ? undefined : end);
    const offsets = new Map<string, number>();
    for (const match of body.matchAll(/^(\w+)\([^)]*\)[^{\n]*\{\n\s*(?:const offset = this\.bb!\.__offset\(this\.bb_pos, (\d+)\);|return [^\n]*?this\.bb_pos(?: \+ (\d+))?[,)])/gm)) {
        const [, accessor, slot, position] = match;
        if (accessor.endsWith('Length') || accessor.endsWith('Array')) continue;
        if (!offsets.has(accessor)) offsets.set(accessor, Number(slot ?? position ?? 0));
    }
    return offsets;
}
const camel = (name: string) => name.replace(/_(\w)/g, (_, letter: string) => letter.toUpperCase());

describe('Run/Reset snapshot: the explicit schema listing', () => {
    const { types, enums } = parseSchema(SCHEMA_SOURCE);

    test('every listed struct and table has exactly the fields, types and order of schemas/workshop_wire.fbs', () => {
        for (const [name, listed] of Object.entries(SNAPSHOT_SCHEMA.types)) {
            const declared = types.get(name);
            assert.ok(declared, `${name} is declared in the schema`);
            assert.equal(listed.kind, declared.kind, `${name} kind`);
            assert.equal(listed.fields.length, declared.fields.length, `${name} field count`);
            assert.deepEqual(listed.fields, declared.fields, `${name} fields`);
        }
        for (const [name, underlying] of Object.entries(SNAPSHOT_SCHEMA.enums)) assert.equal(underlying, enums.get(name), `enum ${name}`);
    });

    test('every type reachable from WorkshopResponse and SavedConstruction is listed', () => {
        const pending = ['WorkshopResponse', 'SavedConstruction'];
        const seen = new Set<string>();
        while (pending.length > 0) {
            const name = pending.pop()!;
            if (seen.has(name)) continue;
            seen.add(name);
            assert.ok(name in SNAPSHOT_SCHEMA.types, `${name} is listed`);
            for (const [, type] of types.get(name)!.fields) {
                const element = type.replace(/^\[|\]$/g, '');
                if (types.has(element)) pending.push(element);
                else if (enums.has(element)) assert.ok(element in SNAPSHOT_SCHEMA.enums, `enum ${element} is listed`);
            }
        }
    });

    test('computed struct offsets and sizes equal the generated accessors', () => {
        for (const [name, listed] of Object.entries(SNAPSHOT_SCHEMA.types)) {
            if (listed.kind !== 'struct') continue;
            const layout = structLayout(name);
            const generatedClass = (generated as unknown as Record<string, { sizeOf(): number }>)[name];
            assert.equal(layout.size, generatedClass.sizeOf(), `${name} size`);
            const offsets = generatedOffsets(name);
            for (const field of layout.fields) assert.equal(field.offset, offsets.get(camel(field.name)), `${name}.${field.name} offset`);
        }
    });

    test('table fields use the generated vtable slots', () => {
        for (const [name, listed] of Object.entries(SNAPSHOT_SCHEMA.types)) {
            if (listed.kind !== 'table') continue;
            const offsets = generatedOffsets(name);
            listed.fields.forEach(([field], index) => assert.equal(tableSlot(index), offsets.get(camel(field)), `${name}.${field} slot`));
        }
    });
});

describe('Run/Reset snapshot: bit-exact comparison', () => {
    test('identical snapshots pass', () => {
        assertSnapshotEqual(snapshotFrom(response(), saved(), BENCH), snapshotFrom(response(), saved(), BENCH));
    });

    test('a flipped f32 bit fails and names the field path with both bit patterns', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        const bytes = response();
        const flipped = withU32(bytes, ballLocalY(bytes), 0x40400001);
        const message = failureOf(() => assertSnapshotEqual(s0, snapshotFrom(flipped, saved(), BENCH)));
        assert.match(message, /read\.bodies\[id=1\]\.local\.y 0x40400000≠0x40400001/);
    });

    test('-0 against +0 in a body field fails', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        const negative = response({ bodies: [
            { id: BALL, cell: { x: 0, y: 1, z: 0 }, local: { x: 0.25, y: 3, z: 0 }, rotation: { w: 1 }, velocity: { x: -0, y: 0, z: 0 } },
            { id: RECEIVER, cell: { x: -2, y: 0, z: 0 }, local: { x: 0, y: 0.6, z: 0 }, rotation: { w: 1 } },
        ] });
        assert.match(failureOf(() => assertSnapshotEqual(s0, snapshotFrom(negative, saved(), BENCH))),
            /read\.bodies\[id=1\]\.velocity\.x 0x00000000≠0x80000000/);
    });

    test('two different NaN payloads differ', () => {
        const bytes = response();
        const quiet = withU32(bytes, ballLocalY(bytes), 0x7fc00000);
        const payload = withU32(bytes, ballLocalY(bytes), 0x7fc00001);
        assert.match(failureOf(() => assertSnapshotEqual(snapshotFrom(quiet, saved(), BENCH), snapshotFrom(payload, saved(), BENCH))),
            /read\.bodies\[id=1\]\.local\.y 0x7fc00000≠0x7fc00001/);
    });

    test('a differing connection fails', () => {
        assert.match(failureOf(() => assertSnapshotEqual(snapshotFrom(response(), saved(), BENCH),
            snapshotFrom(response(), saved({ toSocket: 4 }), BENCH))), /saved\.construction\.connections\[0\]\.to_socket 3≠4/);
    });

    test('a differing part parameter fails', () => {
        assert.match(failureOf(() => assertSnapshotEqual(snapshotFrom(response(), saved(), BENCH),
            snapshotFrom(response(), saved({ mass: 4 }), BENCH))), /saved\.construction\.instances\[id=1\]\.mass absent≠0x40800000/);
    });

    test('a differing authored goal fails', () => {
        assert.match(failureOf(() => assertSnapshotEqual(snapshotFrom(response(), saved(), BENCH),
            snapshotFrom(response(), saved({ goalTarget: 9n }), BENCH))), /saved\.construction\.puzzle\.goal_target 2≠9/);
    });

    test('a difference in only the last compared path fails', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        const last = s0.entries[s0.entries.length - 1]!;
        const changed = { ...s0, entries: [...s0.entries.slice(0, -1), [last[0], `${last[1]}-changed`] as const] };
        assert.match(failureOf(() => assertSnapshotEqual(s0, changed)), new RegExp(last[0].replace(/[[\].]/g, '\\$&')));
    });

    test('differing path sets fail, in either direction', () => {
        const one = snapshotFrom(response(), saved({ connections: 1 }), BENCH);
        const two = snapshotFrom(response(), saved({ connections: 2 }), BENCH);
        assert.match(failureOf(() => assertSnapshotEqual(one, two)), /saved\.construction\.connections\.length 1≠2/);
        // With equal lengths and values, a path present on one side only still fails.
        const extra = { ...one, entries: [...one.entries, ['read.extra', '0'] as const] };
        assert.match(failureOf(() => assertSnapshotEqual(one, extra)), /path set differs: read\.extra is after Reset only/);
        assert.match(failureOf(() => assertSnapshotEqual(extra, one)), /path set differs: read\.extra is in S0 only/);
    });

    test('only the named top-level identity paths are excluded', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        const advanced = response({ sequence: 99n, revision: 98n, publication: 0n, epoch: 97n, masterGeneration: 96n, cadence: 95n,
            projection: 94n, session: [9, 9, 9, 9] });
        assertSnapshotEqual(s0, snapshotFrom(advanced, saved(), BENCH));
        assert.ok(!s0.entries.some(([path]) => /^read\.(sequence|revision|publication|epoch|capture|session|master_generation|cadence|projection)\b/.test(path)));
    });

    test('a nested field that shares an excluded name is still compared', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        assert.match(failureOf(() => assertSnapshotEqual(s0, snapshotFrom(response({
            occurrences: [{ work: 1, collider: 2, target: BALL, sequence: 1, eventOrdinal: 0 }] }), saved(), BENCH))),
        /read\.occurrences\[0\]\.sequence 0≠1/);
        assert.match(failureOf(() => assertSnapshotEqual(s0, snapshotFrom(response(), saved({ revision: 6n }), BENCH))),
            /saved\.construction\.revision 5≠6/);
    });

    test('a Load comparison ignores only the restarted construction revision', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        assertLoadRestores(s0, snapshotFrom(response(), saved({ revision: 3n }), BENCH));
        assert.match(failureOf(() => assertLoadRestores(s0, snapshotFrom(response(), saved({ revision: 3n, mass: 4 }), BENCH))),
            /Load: saved\.construction\.instances\[id=1\]\.mass/);
        const bytes = response();
        assert.match(failureOf(() => assertLoadRestores(s0, snapshotFrom(withU32(bytes, ballLocalY(bytes), 0x40400001), saved(), BENCH))),
            /Load: read\.bodies\[id=1\]\.local\.y/);
    });

    test('committed counter and clock memory is compared', () => {
        const clockRunning = { node: 2n, kind: 7, count: 0, phase: 1, dueTick: 120n };
        const s0 = snapshotFrom(response({ activationMemory: [{ node: 2n, kind: 7 }, { node: 3n, kind: 6 }] }), saved(), BENCH);
        assert.match(failureOf(() => assertSnapshotEqual(s0, snapshotFrom(response({
            activationMemory: [clockRunning, { node: 3n, kind: 6 }] }), saved(), BENCH))), /read\.activation_memory\[0\]\.phase 0≠1/);
        assert.match(failureOf(() => assertSnapshotEqual(s0, snapshotFrom(response({
            activationMemory: [{ node: 2n, kind: 7 }, { node: 3n, kind: 6, count: 2 }] }), saved(), BENCH))), /read\.activation_memory\[1\]\.count 0≠2/);
    });

    test('the tick is compared', () => {
        assert.match(failureOf(() => assertSnapshotEqual(snapshotFrom(response(), saved(), BENCH),
            snapshotFrom(response({ tick: 1n }), saved(), BENCH))), /read\.tick absent≠1/); // tick 0 is the omitted default
    });
});

describe('Run/Reset snapshot: no vacuous pass', () => {
    test('empty bodies are rejected', () => {
        assert.throws(() => snapshotFrom(response({ bodies: [] }), saved(), BENCH), /no bodies/);
    });

    test('a body count other than the bench is rejected', () => {
        assert.throws(() => snapshotFrom(response(), saved(), { bodies: 3 }), /2 bodies, the bench has 3/);
    });

    test('a non-Applied acknowledgement is rejected', () => {
        assert.throws(() => snapshotFrom(response({ outcome: 1 }), saved(), BENCH), /Applied/);
    });

    test('an advanced read must have tick > 0', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        assert.throws(() => assertRunAdvanced(s0, readSnapshot(response({ phase: 6 }))), /tick 0/);
    });

    test('an advanced read must differ from S0 in a physical path', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        assert.throws(() => assertRunAdvanced(s0, readSnapshot(response({ tick: 40n, phase: 6 }))), /no compared path differs/);
    });

    test('an advanced read with a moved body passes', () => {
        const s0 = snapshotFrom(response(), saved(), BENCH);
        const bytes = response({ tick: 40n, phase: 6 });
        assertRunAdvanced(s0, readSnapshot(withU32(bytes, ballLocalY(bytes), 0x40200000)));
    });
});
