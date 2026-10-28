// Story 9A.23: the event ring's shared memory and its one reader, imported by the Simulation worker (the writer), the Animation worker
// and the main thread (each reader with its own cursor). The ring layout is owned by EventRingBuffer and EventRingHeader; WorkerAbiTests
// pins every value against its C# owner.
//
// EventRingMemory is the only code that touches the SharedArrayBuffer's contents (owner request, review R2-F2): it alone constructs the
// typed view over it, and every accessor is one atomic whole-word load or store. The slot seqlock is written against it on both sides:
// - the writer (the Simulation worker's copy of each publication) publishes the slot's sequence word as 0, stores every payload word,
//   then publishes the sequence;
// - the reader (EventRingCursor.read) loads the sequence, loads every payload word, then loads the sequence again, and keeps the copy
//   only when both loads equal the sequence it wants. A plain payload access may be reordered past the sequence accesses, so a slot
//   reused while it was copied could pass both checks torn (review F1); the accessors make every access atomic.
// Nothing else can reach the SharedArrayBuffer: the writer posts it with postTo, and a reader adopts it with EventRingCursor.attach,
// whose ring is private.
const EVENT_HEADER_BYTES = 64;          // EventRingBuffer.HeaderBytes
const EVENT_SLOT_BYTES = 64;            // EventRingBuffer.SlotBytes
const EVENT_CAPACITY_OFFSET = 0;        // EventRingHeader.CapacityOffset
const EVENT_SLOT_BYTES_OFFSET = 4;      // EventRingHeader.SlotBytesOffset
const EVENT_EPOCH_OFFSET = 8;           // EventRingHeader.EpochOffset
const EVENT_WRITE_SEQUENCE_OFFSET = 16; // EventRingHeader.WriteSequenceOffset
const EVENT_DELIVERY_BYTES = 16;        // EventRingDelivery.HeaderBytes

export interface EventDelivery { records: Uint8Array<ArrayBuffer>; skipped: bigint; epoch: bigint; }
export interface EventRingRecipient { postMessage(message: { eventRing: SharedArrayBuffer }): void; }

const NO_RECORDS = new Uint8Array(0);

export class EventRingMemory {
    #words: BigUint64Array<SharedArrayBuffer>; #capacity = 0; #epoch = 0n;
    #message: { eventRing: SharedArrayBuffer };

    constructor(buffer: SharedArrayBuffer) {
        if (!(buffer instanceof SharedArrayBuffer) || buffer.byteLength < EVENT_HEADER_BYTES || buffer.byteLength % 8 !== 0)
            throw new Error('An event ring must be shared.');
        this.#message = { eventRing: buffer };
        this.#words = new BigUint64Array(buffer);
    }

    // A fresh ring for one epoch and capacity: its fixed header words (capacity and slot size, then epoch) are stored before it is posted.
    static allocate(capacity: number, epoch: bigint) {
        if (!Number.isInteger(capacity) || capacity <= 0 || capacity > 0xffffffff || typeof epoch !== 'bigint' || epoch === 0n)
            throw new Error('Unsupported event ring layout.');
        const ring = new EventRingMemory(new SharedArrayBuffer(EVENT_HEADER_BYTES + capacity * EVENT_SLOT_BYTES));
        ring.#capacity = capacity; ring.#epoch = epoch;
        Atomics.store(ring.#words, EVENT_CAPACITY_OFFSET / 8, BigInt(capacity) | BigInt(EVENT_SLOT_BYTES) << BigInt(8 * EVENT_SLOT_BYTES_OFFSET));
        Atomics.store(ring.#words, EVENT_EPOCH_OFFSET / 8, epoch);
        return ring;
    }

    // Adopts the ring a writer posted, after its fixed header words are checked against the layout.
    static attach(buffer: SharedArrayBuffer) {
        const ring = new EventRingMemory(buffer);
        const layout = Atomics.load(ring.#words, EVENT_CAPACITY_OFFSET / 8), epoch = Atomics.load(ring.#words, EVENT_EPOCH_OFFSET / 8);
        const capacity = Number(layout & 0xffffffffn), slotBytes = Number(layout >> BigInt(8 * EVENT_SLOT_BYTES_OFFSET));
        if (capacity === 0 || capacity > 0xffffffff || slotBytes !== EVENT_SLOT_BYTES ||
            buffer.byteLength !== EVENT_HEADER_BYTES + capacity * EVENT_SLOT_BYTES || epoch === 0n)
            throw new Error('Unsupported event ring layout.');
        for (let word = 3; word < EVENT_HEADER_BYTES / 8; word++)
            if (Atomics.load(ring.#words, word) !== 0n) throw new Error('Event header reserved words must be zero.');
        ring.#capacity = capacity; ring.#epoch = epoch;
        return ring;
    }

    // Posts the ring to one of its readers (the main thread, or the Animation worker's master port) as `{ eventRing }`: the
    // SharedArrayBuffer leaves this module only inside that message, and a reader adopts it with attach (review R3-F1).
    postTo(target: EventRingRecipient) { target.postMessage(this.#message); }
    get capacity() { return this.#capacity; }
    get epoch() { return this.#epoch; }

    loadWriteSequence() { return Atomics.load(this.#words, EVENT_WRITE_SEQUENCE_OFFSET / 8); }
    storeWriteSequence(value: bigint) { Atomics.store(this.#words, EVENT_WRITE_SEQUENCE_OFFSET / 8, value); }
    // The seqlock word of the slot that holds record <sequence>.
    loadSequence(sequence: bigint) { return Atomics.load(this.#words, this.#slot(sequence)); }
    publishSequence(sequence: bigint, value: bigint) { Atomics.store(this.#words, this.#slot(sequence), value); }
    // Payload word <word> (1 to 7; word 0 is the sequence) of the slot that holds record <sequence>.
    loadPayload(sequence: bigint, word: number) { return Atomics.load(this.#words, this.#slot(sequence) + EventRingMemory.#payload(word)); }
    storePayload(sequence: bigint, word: number, value: bigint) { Atomics.store(this.#words, this.#slot(sequence) + EventRingMemory.#payload(word), value); }

    #slot(sequence: bigint) { return EVENT_HEADER_BYTES / 8 + Number((sequence - 1n) % BigInt(this.#capacity)) * (EVENT_SLOT_BYTES / 8); }
    static #payload(word: number) {
        if (!Number.isInteger(word) || word < 1 || word >= EVENT_SLOT_BYTES / 8) throw new Error('A payload word is 1 to 7.');
        return word;
    }
}

// The reader: transport only (D2). It copies whole committed records out of the Simulation worker's ring and leaves kind, domain and
// owner filtering to C#. It reproduces EventRingConsumer's skip-uncommitted semantics across threads:
// - WriteSequence is loaded first, so every slot up to it was published before it (the writer stores it last);
// - a slot the writer reused while it was copied is never delivered: it is skipped and counted;
// - a reader more than a capacity behind jumps to the oldest record the ring still holds and counts the records it passed.
export class EventRingCursor {
    #ring: EventRingMemory | undefined; // the adopted EventRingMemory: private, so no consumer can reach the ring's memory through the cursor (review R3-F1)

    epoch: bigint; cursor: bigint; lapped: bigint;
    constructor() {
        this.epoch = 0n; this.cursor = 0n; this.lapped = 0n;
    }

    // Same-epoch growth preserves each reader's position. A new epoch starts a new sequence.
    attach(buffer: SharedArrayBuffer) {
        const next = EventRingMemory.attach(buffer);
        if (next.epoch === this.epoch && this.#ring) {
            if (next.capacity < this.#ring.capacity || next.loadWriteSequence() < this.#ring.loadWriteSequence())
                throw new Error('A ring replacement cannot shrink or rewind its epoch.');
        } else {
            this.epoch = next.epoch; this.cursor = 0n;
        }
        this.#ring = next;
    }

    // Reads every whole record committed since the cursor, only while the ring's epoch is the one the caller has installed (D5): a
    // ring of an older epoch is ignored and one of a newer epoch is held unread, so its records wait for the matching Commit.
    // Returns the records (64 bytes each, ascending sequence), the records skipped by this read and the ring's epoch.
    read(admitted: bigint): EventDelivery {
        if (typeof admitted !== 'bigint') throw new Error('An event read names its installed epoch.');
        if (!this.#ring || this.epoch !== admitted) return { records: NO_RECORDS, skipped: 0n, epoch: this.epoch };
        const written = this.#ring.loadWriteSequence(), capacity = BigInt(this.#ring.capacity);
        const records: BigUint64Array<ArrayBuffer>[] = [];
        let skipped = 0n;
        while (this.cursor < written) {
            if (written - this.cursor > capacity) {
                skipped += written - capacity - this.cursor;
                this.cursor = written - capacity;
            }
            const target = this.cursor + 1n;
            // The reader's seqlock: the sequence, every payload word, then the sequence again.
            const before = this.#ring.loadSequence(target);
            const copy = new BigUint64Array(EVENT_SLOT_BYTES / 8);
            for (let word = 1; word < EVENT_SLOT_BYTES / 8; word++) copy[word] = this.#ring.loadPayload(target, word);
            const after = this.#ring.loadSequence(target);
            this.cursor = target;
            if (before !== target || after !== target) { skipped++; continue; }
            copy[0] = target;
            records.push(copy);
        }
        this.lapped += skipped;
        if (records.length === 0) return { records: NO_RECORDS, skipped, epoch: this.epoch };
        const words = new BigUint64Array(records.length * EVENT_SLOT_BYTES / 8);
        records.forEach((copy, index) => words.set(copy, index * EVENT_SLOT_BYTES / 8));
        return { records: new Uint8Array(words.buffer), skipped, epoch: this.epoch };
    }
}

// The bytes C# decodes (EventRingDelivery): the epoch and the skipped count as little-endian u64, then the records.
export function eventDelivery(read: EventDelivery) {
    const delivery = new Uint8Array(EVENT_DELIVERY_BYTES + read.records.length);
    const view = new DataView(delivery.buffer);
    view.setBigUint64(0, read.epoch, true);
    view.setBigUint64(8, read.skipped, true);
    delivery.set(read.records, EVENT_DELIVERY_BYTES);
    return delivery;
}
