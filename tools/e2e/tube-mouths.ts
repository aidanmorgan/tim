// Story 9R.6: tube-mouth geometry read back from a Run/Reset snapshot's saved construction (tools/e2e/run-reset-snapshot.ts).
// The mouth table restates each scene's declared Loc_Port_mouth_* locator (position, outward local +X) as the expected data;
// the committed pose of an instance is (cell + local) / 16 with its decoded f32 rotation.
import assert from 'node:assert/strict';
import { snapshotValue, type RunResetSnapshot } from './run-reset-snapshot.ts';

export type Vec3 = readonly [number, number, number];
export type Quat = readonly [number, number, number, number];
export type TubePart = 'pipe' | 'pipe_bend_45' | 'pipe_bend_90' | 'funnel';
export type MouthEnd = 'start' | 'end';

/** elements/<Package>/<part>.tscn mouth locators: part-local position and outward axis. */
export const MOUTHS: Readonly<Record<TubePart, Readonly<Record<MouthEnd, { at: Vec3; out: Vec3 }>>>> = {
    pipe: { start: { at: [-1.89, 0, 0], out: [-1, 0, 0] }, end: { at: [1.89, 0, 0], out: [1, 0, 0] } },
    pipe_bend_45: {
        start: { at: [-1.00844, 0.182689, 0], out: [-1, 0, 0] },
        end: { at: [0.842256, -0.583894, 0], out: [0.707107, -0.707107, 0] },
    },
    pipe_bend_90: {
        start: { at: [-1.787056, 0.702944, 0], out: [-1, 0, 0] },
        end: { at: [0.702944, -1.787056, 0], out: [0, -1, 0] },
    },
    funnel: { start: { at: [-0.99, 0, 0], out: [-1, 0, 0] }, end: { at: [0.99, 0, 0], out: [1, 0, 0] } },
};

/** Free-workshop camera: the screen point of a world point (as cat-014 and the driver's Z-ring projection). */
export function screen(x: number, y: number, z: number): { x: number; y: number } {
    return { x: Math.round(720 + 53.8 * x - 36.8 * z), y: Math.round(485 + 17.0 * x + 24.8 * z - 57.9 * (y - 3)) };
}

function numberAt(snapshot: RunResetSnapshot, path: string): number {
    const value = snapshotValue(snapshot, path);
    if (typeof value !== 'number') throw new Error(`expected a decoded number at ${path}`);
    return value;
}

export function instanceCount(snapshot: RunResetSnapshot): number {
    return numberAt(snapshot, 'saved.construction.instances.length');
}

export function hasInstance(snapshot: RunResetSnapshot, id: number): boolean {
    return snapshot.entries.some(([path]) => path.startsWith(`saved.construction.instances[id=${id}].`));
}

/** The committed pose of instance `id`. */
export function pose(snapshot: RunResetSnapshot, id: number): { position: Vec3; rotation: Quat; part: string } {
    const prefix = `saved.construction.instances[id=${id}]`;
    const axis = (a: 'x' | 'y' | 'z') => (numberAt(snapshot, `${prefix}.cell.${a}`) + numberAt(snapshot, `${prefix}.local.${a}`)) / 16;
    const q = (c: 'x' | 'y' | 'z' | 'w') => numberAt(snapshot, `${prefix}.rotation.${c}`);
    const part = snapshotValue(snapshot, `${prefix}.partId`);
    if (typeof part !== 'string') throw new Error(`instance ${id} has no part ID`);
    return { position: [axis('x'), axis('y'), axis('z')], rotation: [q('x'), q('y'), q('z'), q('w')], part };
}

export function rotate([x, y, z, w]: Quat, [vx, vy, vz]: Vec3): Vec3 {
    // v' = v + 2w (q × v) + 2 q × (q × v)
    const cx = y * vz - z * vy, cy = z * vx - x * vz, cz = x * vy - y * vx;
    const dx = y * cz - z * cy, dy = z * cx - x * cz, dz = x * cy - y * cx;
    return [vx + 2 * (w * cx + dx), vy + 2 * (w * cy + dy), vz + 2 * (w * cz + dz)];
}

export const sub = (a: Vec3, b: Vec3): Vec3 => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
export const dot = (a: Vec3, b: Vec3): number => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
export const length = (a: Vec3): number => Math.sqrt(dot(a, a));

/** Story 9R.7: the committed f32 `length` of pipe instance `id` (CAT-048 §2: 1–8 m). */
export function pipeLength(snapshot: RunResetSnapshot, id: number): number {
    const instance = `saved.construction.instances[id=${id}]`;
    const part = snapshotValue(snapshot, `${instance}.partId`);
    if (part !== 'pipe') throw new Error(`instance ${id} has part ID ${part}; expected pipe`);
    const parameters = `${instance}.parameters`;
    const count = numberAt(snapshot, `${parameters}.length`);
    for (let index = 0; index < count; index++) {
        const entry = `${parameters}[${index}]`;
        if (snapshotValue(snapshot, `${entry}.key`) === 'length') return numberAt(snapshot, `${entry}.value`);
    }
    throw new Error(`pipe ${id} saved no length`);
}

/** The pipe's mouths sit at its collar faces, ±(length/2 + 0.09); the table above restates the scene's default 3.6 m pipe. */
const COLLAR_HALF_WIDTH = 0.09;

/** The world position and outward axis of one committed instance's mouth. */
export function mouth(snapshot: RunResetSnapshot, id: number, end: MouthEnd): { at: Vec3; out: Vec3 } {
    const { position, rotation, part } = pose(snapshot, id);
    const declared = MOUTHS[part as TubePart];
    if (!declared) throw new Error(`instance ${id} is a ${part}, not a tube part`);
    const local = part === 'pipe'
        ? { at: [(end === 'start' ? -1 : 1) * (pipeLength(snapshot, id) / 2 + COLLAR_HALF_WIDTH), 0, 0] as Vec3, out: declared[end].out }
        : declared[end];
    const at = rotate(rotation, local.at);
    return { at: [position[0] + at[0], position[1] + at[1], position[2] + at[2]], out: rotate(rotation, local.out) };
}

/** Story 9R.6 AC: the two committed mouths coincide within 1e-4 m and face opposite (dot < -0.9999). */
export function assertJoined(snapshot: RunResetSnapshot, a: { id: number; end: MouthEnd }, b: { id: number; end: MouthEnd }, label: string): void {
    const first = mouth(snapshot, a.id, a.end), second = mouth(snapshot, b.id, b.end);
    const gap = length(sub(first.at, second.at)), facing = dot(first.out, second.out);
    assert.ok(gap <= 1e-4, `${label}: mouths ${a.id}.${a.end} and ${b.id}.${b.end} are ${gap} m apart`);
    assert.ok(facing < -0.9999, `${label}: mouths ${a.id}.${a.end} and ${b.id}.${b.end} face dot ${facing}`);
}

/** The mouths are not joined: the placement kept its grid pose. */
export function assertNotJoined(snapshot: RunResetSnapshot, a: { id: number; end: MouthEnd }, b: { id: number; end: MouthEnd }, label: string): void {
    const gap = length(sub(mouth(snapshot, a.id, a.end).at, mouth(snapshot, b.id, b.end).at));
    assert.ok(gap > 0.01, `${label}: mouths ${a.id}.${a.end} and ${b.id}.${b.end} were joined (${gap} m apart)`);
}

/** The committed position equals the 0.1 grid point within f32 capture precision. */
export function assertAt(snapshot: RunResetSnapshot, id: number, expected: Vec3, label: string, tolerance = 1e-4): void {
    const { position } = pose(snapshot, id);
    assert.ok(length(sub(position, expected)) <= tolerance, `${label}: instance ${id} is at ${position.join(', ')}, not ${expected.join(', ')}`);
}
