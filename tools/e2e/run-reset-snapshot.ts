// Story 9R.1: compare an Applied Save acknowledgement and its authored construction after Run, Reset and Load.
import { CommandOutcome, ElectricalEnable, type ActivationMemoryRecordT, type ActivationRecordT } from './.wire/curious-contraptions/wire.ts';
import { decodeSavedConstruction, decodeWorkshopResponse } from './workshop-wire.ts';

// Only these exact top-level response fields are excluded. Motion is an opaque encoded pose vector.
const EXCLUDED_PATHS: ReadonlySet<string> = new Set([
    'read.sequence', 'read.revision', 'read.publication', 'read.epoch', 'read.capture', 'read.session', 'read.masterGeneration',
    'read.cadence', 'read.projection', 'read.motion',
]);
// Run requires a physical change. These response envelopes and diagnostics still enter Reset and Load comparisons.
const RUN_METADATA_PATHS = [
    'read.kind', 'read.outcome', 'read.reason', 'read.tick', 'read.phase', 'read.physicalCommit', 'read.memory', 'read.scene',
];
const LOAD_IDENTITY_PATHS: ReadonlySet<string> = new Set(['saved.construction.revision']);

export type SnapshotValue = boolean | string | number | bigint | null;
export type SnapshotEntry = readonly [path: string, value: SnapshotValue];

export interface RunResetSnapshot {
    readonly entries: readonly SnapshotEntry[];
    readonly bodies: number;
    readonly tick: bigint;
    readonly activations: readonly Readonly<ActivationRecordT>[];
    readonly activationMemory: readonly Readonly<ActivationMemoryRecordT>[];
}

function under(path: string, prefix: string): boolean {
    return path === prefix || path.startsWith(`${prefix}.`) || path.startsWith(`${prefix}[`);
}

/** A generated row's first field determines whether its vector has identity or positional indexing. */
function rowKey(row: unknown, index: number): string {
    if (row !== null && typeof row === 'object') {
        const first = Object.entries(row)[0];
        if (first?.[0] === 'id' && typeof first[1] === 'bigint') return `id=${first[1]}`;
    }
    return String(index);
}

function flatten(root: string, value: unknown): SnapshotEntry[] {
    const entries = new Map<string, SnapshotValue>();
    function add(path: string, scalar: SnapshotValue): void {
        if (entries.has(path)) throw new Error(`run-reset snapshot: duplicate path ${path}`);
        entries.set(path, scalar);
    }
    function visit(path: string, field: unknown): void {
        if (EXCLUDED_PATHS.has(path)) return;
        if (Array.isArray(field)) {
            add(`${path}.length`, field.length);
            field.forEach((row: unknown, index) => visit(`${path}[${rowKey(row, index)}]`, row));
        } else if (field !== null && typeof field === 'object') {
            for (const [key, child] of Object.entries(field)) visit(`${path}.${key}`, child);
        } else if (field === null || typeof field === 'number' || typeof field === 'bigint' ||
            typeof field === 'boolean' || typeof field === 'string') {
            add(path, field);
        } else {
            throw new Error(`run-reset snapshot: unsupported decoded value at ${path}`);
        }
    }
    visit(root, value);
    return [...entries];
}

/** The physical half only: the committed read carried by one acknowledgement. */
export function readSnapshot(response: Uint8Array): RunResetSnapshot {
    const decoded = decodeWorkshopResponse(response).unpack();
    if (decoded.outcome !== CommandOutcome.Applied)
        throw new Error(`run-reset snapshot: the acknowledgement is not Applied (outcome ${decoded.outcome})`);
    return {
        entries: flatten('read', decoded), bodies: decoded.bodies.length, tick: decoded.tick,
        activations: decoded.activations, activationMemory: decoded.activationMemory,
    };
}

/** The snapshot of one Applied Save acknowledgement and the slot it saved. The bench declares how many bodies it admits. */
export function snapshotFrom(response: Uint8Array, savedBytes: Uint8Array, bench: { readonly bodies: number }): RunResetSnapshot {
    const read = readSnapshot(response);
    if (read.bodies === 0 && bench.bodies > 0) throw new Error('run-reset snapshot: the committed read has no bodies');
    if (read.bodies !== bench.bodies)
        throw new Error(`run-reset snapshot: the committed read has ${read.bodies} bodies, the bench has ${bench.bodies}`);
    let saved;
    try { saved = decodeSavedConstruction(savedBytes).unpack(); }
    catch (error) { throw new Error('run-reset snapshot: the save slot is not a SavedConstruction', { cause: error }); }
    return { ...read, entries: [...read.entries, ...flatten('saved', saved)] };
}

export function assertSnapshotEqual(expected: RunResetSnapshot, actual: RunResetSnapshot, label = 'Reset'): void {
    compare(expected, actual, label, new Set());
}

/** Load compares all paths and ignores only the restarted page-local construction revision's value. */
export function assertLoadRestores(expected: RunResetSnapshot, loaded: RunResetSnapshot): void {
    compare(expected, loaded, 'Load', LOAD_IDENTITY_PATHS);
}

function display(value: SnapshotValue | undefined): string {
    if (Object.is(value, -0)) return '-0';
    return typeof value === 'bigint' ? `${value}n` : typeof value === 'string' ? JSON.stringify(value) : String(value);
}

function compare(expected: RunResetSnapshot, actual: RunResetSnapshot, label: string, ignored: ReadonlySet<string>): void {
    const actualValues = new Map(actual.entries);
    for (const [path, value] of expected.entries) {
        if (!actualValues.has(path)) throw new Error(`${label}: path set differs: ${path} is in S0 only`);
        const other = actualValues.get(path);
        if (!Object.is(other, value) && !ignored.has(path))
            throw new Error(`${label}: ${path} ${display(value)}≠${display(other)}`);
    }
    const expectedPaths = new Set(expected.entries.map(([path]) => path));
    for (const [path] of actual.entries)
        if (!expectedPaths.has(path)) throw new Error(`${label}: path set differs: ${path} is after ${label} only`);
}

/** The decoded scalar at an exact camelCase path. A missing path throws; a decoded null is a value. */
export function snapshotValue(snapshot: RunResetSnapshot, path: string): SnapshotValue {
    const found = snapshot.entries.find(([candidate]) => candidate === path);
    if (!found) throw new Error(`run-reset snapshot: no path ${path}`);
    return found[1];
}

export function snapshotEntries(snapshot: RunResetSnapshot, prefix: string): SnapshotEntry[] {
    return snapshot.entries.filter(([path]) => under(path, prefix));
}

/** The committed enable of the electrical source owned by this exact authored instance. */
export function committedEnable(snapshot: RunResetSnapshot, owner: bigint): ElectricalEnable {
    const sources = snapshot.entries.filter(([path, value]) => /^read\.electrical\[id=\d+\]\.owner$/.test(path) && value === owner);
    if (sources.length === 0) throw new Error(`The committed read has no electrical source owned by ${owner}`);
    if (sources.length > 1) throw new Error(`The committed read has multiple electrical sources owned by ${owner}`);
    const [ownerPath] = sources[0];
    const enabled = snapshotValue(snapshot, ownerPath.replace(/\.owner$/, '.enabled'));
    if (enabled === ElectricalEnable.Enabled || enabled === ElectricalEnable.Disabled) return enabled;
    throw new Error(`The electrical source of ${owner} has an invalid enable value ${enabled}`);
}

/** ActivationNodeKind values (engine/gpu/ActivationNetwork.cs), pending the C#-to-TypeScript generator. */
export const MemoryKind = { Counter: 6, Oscillator: 7, ContactLoadSource: 10 } as const;

/** Run must advance beyond tick zero and change a compared physical value. */
export function assertRunAdvanced(s0: RunResetSnapshot, during: RunResetSnapshot): void {
    if (during.tick === 0n) throw new Error('run-reset snapshot: the mid-run committed read is at tick 0');
    const values = new Map(s0.entries);
    const differs = during.entries.some(([path, value]) => {
        if (!under(path, 'read') || RUN_METADATA_PATHS.some(prefix => under(path, prefix)) || !values.has(path)) return false;
        const before = values.get(path);
        if ((typeof before === 'number' && !Number.isFinite(before)) ||
            (typeof value === 'number' && !Number.isFinite(value))) return false;
        // Equal zeros describe the same physical state even when their stored signs differ.
        return before !== value;
    });
    if (!differs) throw new Error(`run-reset snapshot: no compared path differs from S0 at tick ${during.tick}`);
}
