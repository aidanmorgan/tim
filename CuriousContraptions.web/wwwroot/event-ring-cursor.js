// Story 9A.23: the one event ring reader, imported by the Animation worker and by the main thread, each with its own cursor. It is
// transport only (D2): it copies whole committed records out of the Simulation worker's ring and leaves kind, domain and owner
// filtering to C#. It reproduces EventRingConsumer's skip-uncommitted semantics across threads:
// - WriteSequence is read with Atomics.load, so every slot up to it was stored before it (the writer stores it last);
// - each slot's sequence word is read with Atomics.load before and after the copy, and every payload word with Atomics.load between
//   them (the writer stores each payload word with Atomics.store while the sequence word is 0), so a slot the writer reused while it
//   was copied is never delivered: it is skipped and counted (review F1: a plain copy may be reordered past the sequence loads);
// - a reader more than a capacity behind jumps to the oldest record the ring still holds and counts the records it passed.
// The ring layout is owned by EventRingBuffer and EventRingHeader; WorkerAbiTests pins every value against its C# owner.
const EVENT_HEADER_BYTES = 64;          // EventRingBuffer.HeaderBytes
const EVENT_SLOT_BYTES = 64;            // EventRingBuffer.SlotBytes
const EVENT_CAPACITY_OFFSET = 0;        // EventRingHeader.CapacityOffset
const EVENT_SLOT_BYTES_OFFSET = 4;      // EventRingHeader.SlotBytesOffset
const EVENT_EPOCH_OFFSET = 8;           // EventRingHeader.EpochOffset
const EVENT_WRITE_SEQUENCE_OFFSET = 16; // EventRingHeader.WriteSequenceOffset
const EVENT_DELIVERY_BYTES = 16;        // EventRingDelivery.HeaderBytes

const NO_RECORDS = new Uint8Array(0);

export class EventRingCursor {
    constructor() {
        this.buffer = undefined; this.words = undefined;
        this.capacity = 0n; this.epoch = 0n; this.cursor = 0n; this.lapped = 0n;
    }

    // Attaches the ring the Simulation worker posted for an admission. A new ring always restarts the cursor at sequence 1: the worker
    // allocates one exactly when the epoch or capacity changes, and the producer's sequences restart with it.
    attach(buffer) {
        if (!(buffer instanceof SharedArrayBuffer) || buffer.byteLength < EVENT_HEADER_BYTES) throw new Error('An event ring must be shared.');
        const view = new DataView(buffer);
        const capacity = view.getUint32(EVENT_CAPACITY_OFFSET, true), epoch = view.getBigUint64(EVENT_EPOCH_OFFSET, true);
        if (capacity === 0 || (capacity & (capacity - 1)) !== 0 || view.getUint32(EVENT_SLOT_BYTES_OFFSET, true) !== EVENT_SLOT_BYTES ||
            buffer.byteLength !== EVENT_HEADER_BYTES + capacity * EVENT_SLOT_BYTES || epoch === 0n)
            throw new Error('Unsupported event ring layout.');
        this.buffer = buffer; this.words = new BigUint64Array(buffer);
        this.capacity = BigInt(capacity); this.epoch = epoch; this.cursor = 0n;
    }

    // Reads every whole record committed since the cursor, only while the ring's epoch is the one the caller has installed (D5): a
    // ring of an older epoch is ignored and one of a newer epoch is held unread, so its records wait for the matching Commit.
    // Returns the records (64 bytes each, ascending sequence), the records skipped by this read and the ring's epoch.
    read(admitted) {
        if (typeof admitted !== 'bigint') throw new Error('An event read names its installed epoch.');
        if (!this.buffer || this.epoch !== admitted) return { records: NO_RECORDS, skipped: 0n, epoch: this.epoch };
        const written = Atomics.load(this.words, EVENT_WRITE_SEQUENCE_OFFSET / 8);
        const copies = [];
        let skipped = 0n;
        while (this.cursor < written) {
            if (written - this.cursor > this.capacity) {
                skipped += written - this.capacity - this.cursor;
                this.cursor = written - this.capacity;
            }
            const target = this.cursor + 1n;
            const slot = EVENT_HEADER_BYTES + Number(this.cursor & (this.capacity - 1n)) * EVENT_SLOT_BYTES;
            // Seqlock (review F1): every payload word is loaded with Atomics.load between the two sequence loads. A plain copy may be
            // reordered past the second sequence load, so a slot reused while it was copied could pass both checks torn.
            const before = Atomics.load(this.words, slot / 8);
            const copy = new BigUint64Array(EVENT_SLOT_BYTES / 8);
            for (let word = 1; word < EVENT_SLOT_BYTES / 8; word++) copy[word] = Atomics.load(this.words, slot / 8 + word);
            const after = Atomics.load(this.words, slot / 8);
            this.cursor = target;
            if (before !== target || after !== target) { skipped++; continue; }
            copy[0] = target;
            copies.push(new Uint8Array(copy.buffer));
        }
        this.lapped += skipped;
        if (copies.length === 0) return { records: NO_RECORDS, skipped, epoch: this.epoch };
        const records = new Uint8Array(copies.length * EVENT_SLOT_BYTES);
        copies.forEach((copy, index) => records.set(copy, index * EVENT_SLOT_BYTES));
        return { records, skipped, epoch: this.epoch };
    }
}

// The bytes C# decodes (EventRingDelivery): the epoch and the skipped count as little-endian u64, then the records.
export function eventDelivery(read) {
    const delivery = new Uint8Array(EVENT_DELIVERY_BYTES + read.records.length);
    const view = new DataView(delivery.buffer);
    view.setBigUint64(0, read.epoch, true);
    view.setBigUint64(8, read.skipped, true);
    delivery.set(read.records, EVENT_DELIVERY_BYTES);
    return delivery;
}
