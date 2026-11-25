// Hand-written helpers over the FlatBuffers wire accessors: the frame checks, the authored-state dump and the mock encoders.
// The accessors themselves are build output (S5b): CuriousContraptions.FlatBuffers generates them from schemas/*.fbs into the
// ignored tools/e2e/.wire/ (see schemas/README.md). Import a generated name from './.wire/curious-contraptions/wire.ts', never
// from this module, which declares and re-exports none.
import * as flatbuffers from 'flatbuffers';
import {
    ActivationMemoryRecord, BodyRead, CaptureLatchRead, CommandIdentityRecord, ContactWorkStoreRecord, ElectricalEnable,
    ElectricalSourceRecord, EnergyStoreReadRecord, PhysicalCommitRevisionRecord, RunControlCommandRecord, RunControlValueKind,
    SavedConstruction, WorkshopCommand, WorkshopResponse,
    type CommandKind, type CommandOutcome, type InstanceRecord, type ResponseKind, type SimulationPhase,
} from './.wire/curious-contraptions/wire.ts';

// Story 9A.2: every Workshop buffer is size-prefixed and carries its root's identifier, exactly as the C# codec frames it.
export const COMMAND_IDENTIFIER = 'CWCM';
export const RESPONSE_IDENTIFIER = 'CWRM';
export const SAVE_IDENTIFIER = 'CCWS';

/** The buffer positioned at its root, after the same frame checks as the C# decoder: the size prefix covers the buffer exactly
 * and the root identifier is the expected one. */
function openFrame(data: Buffer | Uint8Array, identifier: string): flatbuffers.ByteBuffer {
    const bytes = data instanceof Uint8Array ? data : new Uint8Array(data);
    if (bytes.length < 12) throw new Error(`a ${identifier} buffer is shorter than its frame`);
    const declared = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength).getUint32(0, true) + 4;
    if (declared !== bytes.length) throw new Error(`a ${identifier} buffer declares ${declared} of its ${bytes.length} bytes`);
    const bb = new flatbuffers.ByteBuffer(bytes);
    bb.setPosition(flatbuffers.SIZE_PREFIX_LENGTH);
    if (!bb.__has_identifier(identifier)) throw new Error(`not a ${identifier} buffer`);
    return bb;
}

export function decodeWorkshopResponse(data: Buffer | Uint8Array): WorkshopResponse {
    return WorkshopResponse.getRootAsWorkshopResponse(openFrame(data, RESPONSE_IDENTIFIER));
}

/** Resolve an exact compiled reservoir identity; array order is not its identity. */
export function storedEnergy(store: EnergyStoreReadRecord): number {
    return Math.fround(store.bulk() + store.remainder());
}

export function sourceEnergy(source: ElectricalSourceRecord): number;
export function sourceEnergy(source: ElectricalSourceRecord | null | undefined): number | undefined;
export function sourceEnergy(source: ElectricalSourceRecord | null | undefined): number | undefined {
    return source == null ? undefined : Math.fround(source.remainingBulk() + source.remainingRemainder());
}

export function energyStore(response: WorkshopResponse, id: bigint): EnergyStoreReadRecord {
    for (let i = 0; i < response.energyStoresLength(); i++) {
        const store = response.energyStores(i)!;
        if (store.id() === id) return store;
    }
    throw new Error('Missing committed energy store ' + id);
}

/** Contact-only reservoir fixtures expose counted hit batches and cumulative released work.
 * Each epoch starts with zero released work; recharge does not inflate a hit's debit. */
export function contactHits(responses: readonly WorkshopResponse[], slot = 0): { response: WorkshopResponse; count: number; debit: number }[] {
    const hits: { response: WorkshopResponse; count: number; debit: number }[] = [];
    let previous: WorkshopResponse | undefined;
    for (const response of responses) {
        const after = response.contactWorks(slot);
        const sameEpoch = previous && previous.epoch() === response.epoch();
        const before = sameEpoch ? previous!.contactWorks(slot) : null;
        if (after && after.occurrenceCount() > (before?.occurrenceCount() ?? 0)) {
            const released = energyStore(response, after.store()).released();
            const prior = sameEpoch && before ? energyStore(previous!, before.store()).released() : 0;
            hits.push({ response, count: after.occurrenceCount(), debit: released - prior });
        }
        previous = response;
    }
    return hits;
}

export function decodeWorkshopCommand(data: Buffer | Uint8Array): WorkshopCommand {
    return WorkshopCommand.getRootAsWorkshopCommand(openFrame(data, COMMAND_IDENTIFIER));
}

export function decodeSavedConstruction(data: Buffer | Uint8Array): SavedConstruction {
    const saved = SavedConstruction.getRootAsSavedConstruction(openFrame(data, SAVE_IDENTIFIER));
    if (!saved.construction()) throw new Error('a CCWS construction save without its construction');
    return saved;
}

/** One declared generic parameter of a saved instance, by its key; a missing key fails the suite. */
export function parameter(instance: InstanceRecord, key: string): number {
    for (let i = 0; i < instance.parametersLength(); i++) {
        const entry = instance.parameters(i)!;
        if (entry.key() === key) return entry.value();
    }
    throw new Error(`Instance ${instance.id()} has no declared parameter '${key}'`);
}

export function dumpAuthored(saved: SavedConstruction): object {
    const cr = saved.construction();
    if (!cr) return { nextBodyId: String(saved.nextBodyId()), construction: null };
    const instances = [];
    for (let i = 0; i < cr.instancesLength(); i++) {
        const inst = cr.instances(i)!;
        const c = inst.cell();
        const l = inst.local();
        const r = inst.rotation();
        instances.push({
            id: String(inst.id()),
            partId: inst.partId(),
            locked: inst.locked(),
            cell: c ? { x: c.x(), y: c.y(), z: c.z() } : null,
            local: l ? { x: l.x(), y: l.y(), z: l.z() } : null,
            rotation: r ? { x: r.x(), y: r.y(), z: r.z(), w: r.w() } : null,
            parameters: Array.from({length: inst.parametersLength()}, (_, index) => {
                const parameter = inst.parameters(index)!;
                return {key: parameter.key(), value: parameter.value()};
            }),
        });
    }
    const connections = [];
    for (let i = 0; i < cr.connectionsLength(); i++) {
        const conn = cr.connections(i)!;
        connections.push({
            source: String(conn.source()),
            target: String(conn.target()),
            domain: conn.domain(),
            fromSocket: conn.fromSocket(),
            toSocket: conn.toSocket(),
            ropeLength: conn.hasRopeLength() ? conn.ropeLength() : null,
        });
    }
    const p = cr.puzzle();
    const puzzle = p ? {
        id: p.id(),
        placement: p.placement(),
        precision: p.precision(),
        inventoryPart: p.inventoryPart(),
        inventoryCount: p.inventoryCount(),
        goalKind: p.goalKind(),
        goalBody: String(p.goalBody()),
        goalTarget: String(p.goalTarget()),
        goalSourceNode: p.goalSourceNode(),
        goalTargetNode: p.goalTargetNode(),
        goalDelay: p.goalDelay(),
    } : null;

    return {
        nextBodyId: String(saved.nextBodyId()),
        puzzle,
        instances,
        connections,
    };
}

export function authoredState(bytes: Uint8Array | Buffer): object {
    const saved = decodeSavedConstruction(bytes);
    return dumpAuthored(saved);
}

export interface MockResponseParams {
    sequence?: bigint;
    kind?: ResponseKind;
    outcome?: CommandOutcome;
    reason?: number;
    phase?: SimulationPhase;
    epoch?: bigint;
    tick?: bigint;
    physicalRevision: bigint;
    revision?: bigint;
    publication?: bigint;
    masterGeneration?: bigint;
    cadence?: bigint;
    projection?: bigint;
    session?: Uint8Array | number[];
    electrical?: { id?: bigint; owner?: bigint; remainingBulk?: number; remainingRemainder?: number; transactions?: number; debit?: number; enabled?: number; available?: boolean }[];
    contactWorks?: { id?: bigint; owner?: bigint; occurrenceCount?: number; store?: bigint }[];
    energyStores?: { id?: bigint; owner?: bigint; bulk?: number; remainder?: number; commitSupplied?: number; commitReleased?: number; commitDissipated?: number; transactions?: number; supplied?: number; released?: number; dissipated?: number }[];
    bodies?: {
        id?: bigint;
        cell?: { x?: number; y?: number; z?: number };
        local?: { x?: number; y?: number; z?: number };
        rotation?: { x?: number; y?: number; z?: number; w?: number };
        com?: { x?: number; y?: number; z?: number };
        velocity?: { x?: number; y?: number; z?: number };
        angularVelocity?: { x?: number; y?: number; z?: number };
    }[];
    captures?: {
        sensor?: bigint;
        target: bigint;
        phase?: number;
        eventOrdinal?: number;
        eventPhase?: number;
    }[];
    activationMemory?: {
        node?: bigint; kind?: number; count?: number; lastOrdinal?: number; lastPhase?: number; phase?: number; dueTick?: bigint;
        passageSequence: bigint; dispatchedKind: number; dispatchedEmitter: bigint; dispatchedOrdinal: number; dispatchedPhase: number;
        causeSource: bigint; causeBody: bigint; causeCollider: bigint; causeOrdinal: number; causePhase: number; causeSpeed: number; causeKind: number;
    }[];
}

export function encodeWorkshopResponse(params: MockResponseParams): Buffer {
    const builder = new flatbuffers.Builder(1024);
    let sessionOffset: flatbuffers.Offset | null = null;
    if (params.session) {
        sessionOffset = WorkshopResponse.createSessionVector(builder, params.session);
    }
    let electricalOffset: flatbuffers.Offset | null = null;
    if (params.electrical && params.electrical.length > 0) {
        WorkshopResponse.startElectricalVector(builder, params.electrical.length);
        for (let i = params.electrical.length - 1; i >= 0; i--) {
            const e = params.electrical[i]!;
            ElectricalSourceRecord.createElectricalSourceRecord(
                builder,
                e.id ?? 1n,
                e.owner ?? 1n,
                e.remainingBulk ?? 0,
                e.remainingRemainder ?? 0,
                e.transactions ?? 0,
                e.debit ?? 0,
                e.enabled ?? 1,
                e.available ?? true
            );
        }
        electricalOffset = builder.endVector();
    }
    let contactWorksOffset: flatbuffers.Offset | null = null;
    if (params.contactWorks && params.contactWorks.length > 0) {
        WorkshopResponse.startContactWorksVector(builder, params.contactWorks.length);
        for (let i = params.contactWorks.length - 1; i >= 0; i--) {
            const w = params.contactWorks[i]!;
            ContactWorkStoreRecord.createContactWorkStoreRecord(
                builder,
                w.id ?? 1n,
                w.owner ?? 1n,
                w.occurrenceCount ?? 0,
                w.store ?? 1n
            );
        }
        contactWorksOffset = builder.endVector();
    }
    let energyStoresOffset: flatbuffers.Offset | null = null;
    if (params.energyStores && params.energyStores.length > 0) {
        WorkshopResponse.startEnergyStoresVector(builder, params.energyStores.length);
        for (let i = params.energyStores.length - 1; i >= 0; i--) {
            const store = params.energyStores[i]!;
            EnergyStoreReadRecord.createEnergyStoreReadRecord(builder, store.id ?? 1n, store.owner ?? 1n,
                store.bulk ?? 0, store.remainder ?? 0, store.commitSupplied ?? 0, store.commitReleased ?? 0,
                store.commitDissipated ?? 0, store.transactions ?? 0, store.supplied ?? 0, store.released ?? 0, store.dissipated ?? 0);
        }
        energyStoresOffset = builder.endVector();
    }
    let bodiesOffset: flatbuffers.Offset | null = null;
    if (params.bodies && params.bodies.length > 0) {
        WorkshopResponse.startBodiesVector(builder, params.bodies.length);
        for (let i = params.bodies.length - 1; i >= 0; i--) {
            const b = params.bodies[i]!;
            BodyRead.createBodyRead(
                builder,
                b.id ?? 1n,
                b.cell?.x ?? 0, b.cell?.y ?? 0, b.cell?.z ?? 0,
                b.local?.x ?? 0, b.local?.y ?? 0, b.local?.z ?? 0,
                b.rotation?.x ?? 0, b.rotation?.y ?? 0, b.rotation?.z ?? 0, b.rotation?.w ?? 1,
                b.com?.x ?? 0, b.com?.y ?? 0, b.com?.z ?? 0,
                b.velocity?.x ?? 0, b.velocity?.y ?? 0, b.velocity?.z ?? 0,
                b.angularVelocity?.x ?? 0, b.angularVelocity?.y ?? 0, b.angularVelocity?.z ?? 0
            );
        }
        bodiesOffset = builder.endVector();
    }
    let capturesOffset: flatbuffers.Offset | null = null;
    if (params.captures && params.captures.length > 0) {
        WorkshopResponse.startCapturesVector(builder, params.captures.length);
        for (let i = params.captures.length - 1; i >= 0; i--) {
            const c = params.captures[i]!;
            CaptureLatchRead.createCaptureLatchRead(
                builder,
                c.sensor ?? 0n,
                c.target,
                c.phase ?? 0,
                c.eventOrdinal ?? 0,
                c.eventPhase ?? 0
            );
        }
        capturesOffset = builder.endVector();
    }
    let memoryOffset: flatbuffers.Offset | null = null;
    if (params.activationMemory && params.activationMemory.length > 0) {
        WorkshopResponse.startActivationMemoryVector(builder, params.activationMemory.length);
        for (let i = params.activationMemory.length - 1; i >= 0; i--) {
            const m = params.activationMemory[i]!;
            ActivationMemoryRecord.createActivationMemoryRecord(builder, m.node ?? 1n, m.kind ?? 6, m.count ?? 0, m.lastOrdinal ?? 0,
                m.lastPhase ?? 0, m.phase ?? 0, m.dueTick ?? 0n, m.passageSequence,
                m.dispatchedKind, m.dispatchedEmitter, m.dispatchedOrdinal, m.dispatchedPhase,
                m.causeSource, m.causeBody, m.causeCollider, m.causeOrdinal,
                m.causePhase, m.causeSpeed, m.causeKind);
        }
        memoryOffset = builder.endVector();
    }

    WorkshopResponse.startWorkshopResponse(builder);
    if (params.sequence !== undefined) WorkshopResponse.addSequence(builder, params.sequence);
    if (params.kind !== undefined) WorkshopResponse.addKind(builder, params.kind);
    if (params.outcome !== undefined) WorkshopResponse.addOutcome(builder, params.outcome);
    if (params.reason !== undefined) WorkshopResponse.addReason(builder, params.reason);
    if (params.phase !== undefined) WorkshopResponse.addPhase(builder, params.phase);
    if (params.epoch !== undefined) WorkshopResponse.addEpoch(builder, params.epoch);
    if (params.tick !== undefined) WorkshopResponse.addTick(builder, params.tick);
    if (params.revision !== undefined) WorkshopResponse.addRevision(builder, params.revision);
    if (params.publication !== undefined) WorkshopResponse.addPublication(builder, params.publication);
    if (params.masterGeneration !== undefined) WorkshopResponse.addMasterGeneration(builder, params.masterGeneration);
    if (params.cadence !== undefined) WorkshopResponse.addCadence(builder, params.cadence);
    if (params.projection !== undefined) WorkshopResponse.addProjection(builder, params.projection);
    if (sessionOffset !== null) WorkshopResponse.addSession(builder, sessionOffset);
    if (bodiesOffset !== null) WorkshopResponse.addBodies(builder, bodiesOffset);
    if (capturesOffset !== null) WorkshopResponse.addCaptures(builder, capturesOffset);
    if (electricalOffset !== null) WorkshopResponse.addElectrical(builder, electricalOffset);
    if (contactWorksOffset !== null) WorkshopResponse.addContactWorks(builder, contactWorksOffset);
    if (energyStoresOffset !== null) WorkshopResponse.addEnergyStores(builder, energyStoresOffset);
    if (memoryOffset !== null) WorkshopResponse.addActivationMemory(builder, memoryOffset);
    WorkshopResponse.addPhysicalCommit(builder, PhysicalCommitRevisionRecord.createPhysicalCommitRevisionRecord(builder, params.physicalRevision));
    const end = WorkshopResponse.endWorkshopResponse(builder);
    builder.finishSizePrefixed(end, RESPONSE_IDENTIFIER);
    return Buffer.from(builder.asUint8Array());
}

export interface MockCommandParams {
    sequence?: bigint;
    kind?: CommandKind;
    epoch?: bigint;
    revision?: bigint;
    revisionKind?: number;
    session?: Uint8Array | number[];
    cadence?: bigint;
    projection?: bigint;
    settings?: Record<string, unknown>;
    runControl?: { owner?: bigint; control?: bigint; scalar?: number; kind?: RunControlValueKind; enable?: ElectricalEnable };
    target?: {
        sequence?: bigint;
        epoch?: bigint;
        revision?: bigint;
        cadence?: bigint;
        projection?: bigint;
    };
}

export function encodeWorkshopCommand(params: MockCommandParams): Buffer {
    const builder = new flatbuffers.Builder(1024);
    let sessionOffset: flatbuffers.Offset | null = null;
    if (params.session) {
        sessionOffset = WorkshopCommand.createSessionVector(builder, params.session);
    }
    WorkshopCommand.startWorkshopCommand(builder);
    if (params.sequence !== undefined) WorkshopCommand.addSequence(builder, params.sequence);
    if (params.kind !== undefined) WorkshopCommand.addKind(builder, params.kind);
    if (params.epoch !== undefined) WorkshopCommand.addEpoch(builder, params.epoch);
    if (params.revision !== undefined) WorkshopCommand.addRevision(builder, params.revision);
    if (params.revisionKind !== undefined) WorkshopCommand.addRevisionKind(builder, params.revisionKind);
    if (sessionOffset !== null) WorkshopCommand.addSession(builder, sessionOffset);
    if (params.cadence !== undefined) WorkshopCommand.addCadence(builder, params.cadence);
    if (params.projection !== undefined) WorkshopCommand.addProjection(builder, params.projection);
    if (params.runControl) {
        const controlOffset = RunControlCommandRecord.createRunControlCommandRecord(
            builder,
            params.runControl.owner ?? 0n,
            params.runControl.control ?? 0n,
            params.runControl.scalar ?? 0,
            params.runControl.kind ?? RunControlValueKind.Enable,
            params.runControl.enable ?? ElectricalEnable.Disabled
        );
        WorkshopCommand.addRunControl(builder, controlOffset);
    }
    if (params.target) {
        const t = params.target;
        const targetOffset = CommandIdentityRecord.createCommandIdentityRecord(
            builder,
            t.sequence ?? 0n,
            t.epoch ?? 0n,
            t.revision ?? 0n,
            t.cadence ?? 0n,
            t.projection ?? 0n
        );
        WorkshopCommand.addTarget(builder, targetOffset);
    }
    const end = WorkshopCommand.endWorkshopCommand(builder);
    builder.finishSizePrefixed(end, COMMAND_IDENTIFIER);
    return Buffer.from(builder.asUint8Array());
}
