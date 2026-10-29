import { PosePublicationPreparation } from '../../tools/runtime/worker-contracts.js';
interface PoseStorage {
    buffer: SharedArrayBuffer;
    sequences: BigInt64Array<SharedArrayBuffer>;
    slots: Uint8Array<SharedArrayBuffer>[];
    capacity: number;
    message: { poseRing: SharedArrayBuffer };
}
interface PreparedPose { owner: bigint; epoch: bigint; bodies: number; storage: PoseStorage; }
// The pose ring layout owned by WorkshopPoseRing; WorkerAbiTests pins every value against its C# owner. Story 9A.2: this worker
// reads no read layout: C# composes each committed read's pose slot (WorkshopPoseRing.ComposeSlot) and the worker copies it.
const POSE_DEFAULT_BODY_CAPACITY = 16; // WorkshopPoseRing.DefaultBodyCapacity
const POSE_SLOT_COUNT = 3;             // WorkshopPoseRing.SlotCount
const POSE_HEADER_BYTES = 16;          // WorkshopPoseRing.HeaderBytes
const POSE_BODY_BYTES = 64;            // WorkshopPoseRing.BodyBytes

// Story 9R.5b: a ring's body capacity is its own length (WorkshopPoseRing.BodyCapacity); there is no fixed capacity.
function poseRingCapacity(buffer: SharedArrayBuffer) {
    const slot = buffer.byteLength / POSE_SLOT_COUNT, capacity = (slot - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (!Number.isInteger(slot) || !Number.isInteger(capacity) || capacity < 0) throw new Error('The pose ring is not a whole number of slots.');
    return capacity;
}
function poseViews(buffer: SharedArrayBuffer): PoseStorage {
    const capacity = poseRingCapacity(buffer), width = buffer.byteLength / POSE_SLOT_COUNT;
    return { buffer, capacity, sequences: new BigInt64Array(buffer), message: { poseRing: buffer },
        slots: Array.from({ length: POSE_SLOT_COUNT }, (_, index) => new Uint8Array(buffer, index * width, width)) };
}
function poseIdentity(identity: Uint8Array) {
    if (identity.length !== 16) throw new Error('Pose ownership requires command and epoch identities.');
    const view = new DataView(identity.buffer, identity.byteOffset, identity.byteLength);
    const owner = view.getBigUint64(0, true), epoch = view.getBigUint64(8, true);
    if (owner === 0n || epoch === 0n) throw new Error('Pose ownership identities must be nonzero.');
    return { owner, epoch };
}

export function createPoseRing(): SharedArrayBuffer {
    return new SharedArrayBuffer(POSE_SLOT_COUNT * (POSE_HEADER_BYTES + POSE_DEFAULT_BODY_CAPACITY * POSE_BODY_BYTES));
}
export class PoseProducer {
    #poseStorage: PoseStorage | undefined;
    #candidatePose: PreparedPose | undefined;
    #poseBodies = 0;
    #poseEpoch = 0n;
    #poseSlotIndex = 0;
    #posePublicationSequence = 0n;
    #disposed = false;
    #advertise: (message: { poseRing: SharedArrayBuffer }) => void;
    constructor(advertise: (message: { poseRing: SharedArrayBuffer }) => void) { this.#advertise = advertise; }
    dispose() { this.#disposed = true; this.#poseStorage = undefined; this.#candidatePose = undefined; }
attach(buffer: SharedArrayBuffer) {
    if (this.#disposed || this.#poseStorage || poseRingCapacity(buffer) !== POSE_DEFAULT_BODY_CAPACITY) throw new Error('Pose bootstrap must have its common reserve and be unique.');
    this.#poseStorage = poseViews(buffer);
}
prepare(identity: Uint8Array, bodies: number, capacity: number): PosePublicationPreparation {
    const { owner, epoch } = poseIdentity(identity);
    if (this.#disposed || this.#candidatePose || !Number.isSafeInteger(bodies) || bodies < 0 || !Number.isSafeInteger(capacity) || capacity < bodies || capacity < POSE_DEFAULT_BODY_CAPACITY || epoch < this.#poseEpoch)
        throw new Error('Invalid pose preparation ownership or population.');
    const bytes = POSE_SLOT_COUNT * (POSE_HEADER_BYTES + capacity * POSE_BODY_BYTES);
    if (!Number.isSafeInteger(bytes)) return PosePublicationPreparation.StorageUnavailable;
    if (epoch === this.#poseEpoch && (this.#poseBodies !== bodies || this.#poseStorage?.capacity !== capacity))
        throw new Error('An admitted epoch cannot change its body population.');
    let storage: PoseStorage;
    try {
        storage = epoch === this.#poseEpoch ? this.#poseStorage! : poseViews(new SharedArrayBuffer(bytes));
    } catch (error) {
        if (error instanceof RangeError) return PosePublicationPreparation.StorageUnavailable;
        throw error;
    }
    this.#candidatePose = { owner, epoch, bodies, storage };
    return PosePublicationPreparation.Ready;
}
commit(identity: Uint8Array) {
    const { owner, epoch } = poseIdentity(identity), candidate = this.#candidatePose;
    if (this.#disposed || !candidate || candidate.owner !== owner || candidate.epoch !== epoch)
        throw new Error('Pose commit does not own the prepared storage.');
    const replacement = this.#poseStorage !== candidate.storage;
    this.#poseStorage = candidate.storage; this.#poseBodies = candidate.bodies; this.#poseEpoch = epoch; this.#candidatePose = undefined;
    if (replacement) { this.#poseSlotIndex = 0; this.#advertise(this.#poseStorage.message); }
}
discard(identity: Uint8Array) {
    const { owner, epoch } = poseIdentity(identity);
    if (this.#candidatePose?.owner === owner && this.#candidatePose.epoch === epoch) this.#candidatePose = undefined;
}

// Copies one C#-composed slot (its 16-byte header, then one record per committed body) into the next ring slot under the
// sequence lock: odd while writing, even when complete. The records past the read are cleared, so no stale body survives.
publish(slot: Uint8Array) {
    if (!this.#poseStorage) throw new Error('Missing admitted pose storage.');
    const bodyCount = (slot.length - POSE_HEADER_BYTES) / POSE_BODY_BYTES;
    if (bodyCount !== this.#poseBodies) throw new Error('A pose slot must match its admitted population.');
    this.#poseSlotIndex = (this.#poseSlotIndex + 1) % POSE_SLOT_COUNT;
    const target = this.#poseStorage.slots[this.#poseSlotIndex], seqIndex = target.byteOffset / 8;
    const nextSeq = this.#posePublicationSequence + 2n;
    this.#posePublicationSequence = nextSeq;
    Atomics.store(this.#poseStorage.sequences, seqIndex, nextSeq - 1n);
    target.set(slot.subarray(8), 8);
    target.fill(0, slot.length);
    Atomics.store(this.#poseStorage.sequences, seqIndex, nextSeq);
}


}
export class PoseReader {
    readonly #capacity: number;
    readonly #sequences: BigInt64Array<SharedArrayBuffer>;
    readonly #data: DataView<SharedArrayBuffer>;
    readonly #floats: Float32Array<SharedArrayBuffer>;
    #disposed = false;
    constructor(buffer: SharedArrayBuffer) {
        if (!(buffer instanceof SharedArrayBuffer)) throw new Error('A pose ring must be shared.');
        this.#capacity = poseRingCapacity(buffer);
        this.#sequences = new BigInt64Array(buffer);
        this.#data = new DataView(buffer);
        this.#floats = new Float32Array(buffer);
    }
    dispose() { this.#disposed = true; }
read(slotIndex: number) {
    if (this.#disposed) return null;
    if (!Number.isInteger(slotIndex) || slotIndex < 0 || slotIndex >= POSE_SLOT_COUNT) return null;
    const s = slotIndex;
    const slotBytes = POSE_HEADER_BYTES + this.#capacity * POSE_BODY_BYTES;
    const seqIndex = s * (slotBytes / 8);
    const seq1 = Atomics.load(this.#sequences, seqIndex);
    if ((seq1 & 1n) !== 0n) return null;
    const slotByteOffset = s * slotBytes;
    const timestamp = this.#data.getBigInt64(slotByteOffset + 8, true);
    const bodyFloatOffset = (slotByteOffset + POSE_HEADER_BYTES) / 4;
    const bodies = [];
    for (let i = 0; i < this.#capacity; i++) {
        const b = bodyFloatOffset + i * (POSE_BODY_BYTES / 4);
        const bodyId = this.#data.getBigUint64((b + 12) * 4, true);
        const flags = this.#data.getUint32((b + 11) * 4, true);
        if (bodyId === 0n || (flags & 1) === 0) continue;
        bodies.push({
            id: bodyId.toString(), // lossless external observation boundary
            px: this.#floats[b + 0],
            py: this.#floats[b + 1],
            pz: this.#floats[b + 2],
            qx: this.#floats[b + 4],
            qy: this.#floats[b + 5],
            qz: this.#floats[b + 6],
            qw: this.#floats[b + 7],
            vx: this.#floats[b + 8],
            vy: this.#floats[b + 9],
            vz: this.#floats[b + 10],
            flags
        });
    }
    const seq2 = Atomics.load(this.#sequences, seqIndex);
    if (seq1 !== seq2) return null;
    return { sequence: seq2, timestamp, bodies };
}

latest() {
    if (this.#disposed) return null;
    // One sampling rule for each candidate: choose an even publication, then verify its sequence after reading.
    const candidates = Array.from({ length: POSE_SLOT_COUNT }, (_, slot) => ({
        slot, sequence: Atomics.load(this.#sequences, slot * ((POSE_HEADER_BYTES + this.#capacity * POSE_BODY_BYTES) / 8))
    })).filter(candidate => (candidate.sequence & 1n) === 0n)
        .sort((a, b) => a.sequence > b.sequence ? -1 : a.sequence < b.sequence ? 1 : a.slot - b.slot);
    for (const candidate of candidates.slice(0, 2)) {
        const sample = this.read(candidate.slot);
        if (sample) return sample;
    }
    return null;
}
}
