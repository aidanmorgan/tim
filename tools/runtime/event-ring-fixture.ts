import type { EventDelivery } from '../../CuriousContraptions.web/wwwroot/event-ring-cursor.js';
import { EventPublicationPreparation, type ManagedImports } from './worker-contracts.js';
enum FixtureEventKind { Passage = 2, ContactLoad = 5 }
enum FixtureDomain { Mechanical = 1 }
export function publication(epoch: bigint, capacity: number, from: bigint, to: bigint) {
    const count = Number(to - from), bytes = new Uint8Array(64 + count * 64), view = new DataView(bytes.buffer);
    view.setUint32(0, capacity, true); view.setUint32(4, 64, true);
    view.setBigUint64(8, epoch, true); view.setBigUint64(16, to, true);
    for (let i = 0; i < count; i++) {
        const sequence = from + BigInt(i) + 1n, offset = 64 + i * 64;
        view.setBigUint64(offset, sequence, true);
        const loaded = sequence % 2n === 0n;
        view.setBigUint64(offset + 8, sequence, true);
        view.setUint32(offset + 16, loaded ? FixtureEventKind.ContactLoad : FixtureEventKind.Passage, true);
        view.setUint32(offset + 20, FixtureDomain.Mechanical, true);
        view.setBigUint64(offset + 24, sequence + 100n, true);
        view.setBigUint64(offset + 32, loaded ? 0n : sequence + 200n, true);
        view.setUint32(offset + 40, Number(sequence * 4n - 1n), true);
        view.setFloat32(offset + 44, Number(sequence % 4n) / 4, true);
        view.setFloat32(offset + 48, 1 + Number(sequence % 1024n) / 4, true);
        view.setUint32(offset + 52, loaded ? 0 : Number(sequence), true);
        view.setUint32(offset + 56, loaded ? 1 : 0, true);
    }
    return bytes;
}
export function publish(host: ManagedImports, batch: Uint8Array) {
    if (host.prepareEvents(batch) !== EventPublicationPreparation.Ready) throw new Error('Ordinary fixture storage must prepare.');
    host.commitEvents(); host.publishEvents();
}
export function sequences(read: EventDelivery) {
    return Array.from({ length: read.records.length / 64 }, (_, i) =>
        new DataView(read.records.buffer, read.records.byteOffset + i * 64, 64).getBigUint64(0, true));
}
// Every delivered field must belong to this occurrence, including across a concurrent overwrite.
// Compare decoded event semantics, never an encoded output byte pattern.
export function hasExpectedFields(read: EventDelivery) {
    return sequences(read).every((sequence, i) => {
        const record = new DataView(read.records.buffer, read.records.byteOffset + i * 64, 64);
        const loaded = sequence % 2n === 0n;
        return record.getBigUint64(8, true) === sequence &&
            record.getUint32(16, true) === (loaded ? FixtureEventKind.ContactLoad : FixtureEventKind.Passage) &&
            record.getUint32(20, true) === FixtureDomain.Mechanical &&
            record.getBigUint64(24, true) === sequence + 100n &&
            record.getBigUint64(32, true) === (loaded ? 0n : sequence + 200n) &&
            record.getUint32(40, true) === Number(sequence * 4n - 1n) &&
            record.getFloat32(44, true) === Number(sequence % 4n) / 4 &&
            record.getFloat32(48, true) === 1 + Number(sequence % 1024n) / 4 &&
            record.getUint32(52, true) === (loaded ? 0 : Number(sequence)) &&
            record.getUint32(56, true) === (loaded ? 1 : 0);
    });
}
export enum StressRole { Writer, Reader }
export enum StressMessage { Ring, Done, Result, Error }
export interface StressData {
    role: StressRole; capacity: number; total: number; epoch: number; seed: number;
    control: SharedArrayBuffer; readers: number; ring?: SharedArrayBuffer;
}
export interface StressResult { delivered: bigint; skipped: bigint; lapped: bigint; torn: number; }
export type StressReply = { kind: StressMessage.Ring; ring: SharedArrayBuffer } |
    { kind: StressMessage.Done; written: bigint } | { kind: StressMessage.Result; result: StressResult } |
    { kind: StressMessage.Error; error: string };
