// Hand-written helpers over the FlatBuffers wire accessors: the authored-state dump and the mock encoders.
// The accessors themselves are build output: CuriousContraptions.Abstractions generates them from schemas/workshop_wire.fbs into the
// ignored tools/e2e/.wire/ (see schemas/README.md). Import a generated name from './.wire/curious-contraptions/wire.ts', never
// from this module, which declares and re-exports none.
import * as flatbuffers from 'flatbuffers';
import {
    ActivationMemoryRecordT, BodyReadT, CaptureLatchReadT, CommandIdentityRecordT, ContactWorkStoreRecordT, ElectricalEnable,
    ElectricalSourceRecord, ElectricalSourceRecordT, EnergyStoreReadRecordT, PhysicalCommitRevisionRecordT,
    Quat4fT, RunControlCommandRecordT, RunControlValueKind, Vec3fT, Vec3iT,
    SavedConstruction, WireRootIdentifier, WorkshopCommand, WorkshopResponse,
    type EnergyStoreReadRecord, type InstanceRecord, type WorkshopCommandT, type WorkshopResponseT,
} from './.wire/curious-contraptions/wire.ts';
import { openFrame, rootIdentifier } from '../runtime/wire-frame.ts';

// Every Workshop buffer is size-prefixed and carries its WireRootIdentifier, checked by wire-frame.ts.

export function decodeWorkshopResponse(data: Buffer | Uint8Array): WorkshopResponse {
    return WorkshopResponse.getRootAsWorkshopResponse(openFrame(data, WireRootIdentifier.WorkshopResponse));
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
    return WorkshopCommand.getRootAsWorkshopCommand(openFrame(data, WireRootIdentifier.WorkshopCommand));
}

export function decodeSavedConstruction(data: Uint8Array): SavedConstruction {
    const saved = SavedConstruction.getRootAsSavedConstruction(openFrame(data, WireRootIdentifier.SavedConstruction));
    const construction = saved.construction();
    if (construction === null) throw new Error('a SavedConstruction save without its construction');
    if (construction.settings() === null) throw new Error('a saved construction without its cadence settings');
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

export function authoredState(bytes: Uint8Array | Buffer) {
    const { construction, ...save } = decodeSavedConstruction(bytes).unpack();
    // Admission requires construction, although the generated unpack type permits null.
    // The revision belongs to the current page.
    const { revision: _revision, ...authoredConstruction } = construction!;
    return { ...save, construction: authoredConstruction };
}

type MockBody = Partial<Omit<BodyReadT, 'pack' | 'cell' | 'local' | 'rotation' | 'com' | 'velocity' | 'angularVelocity'>> & {
    cell?: Partial<Omit<Vec3iT, 'pack'>>;
    local?: Partial<Omit<Vec3fT, 'pack'>>;
    rotation?: Partial<Omit<Quat4fT, 'pack'>>;
    com?: Partial<Omit<Vec3fT, 'pack'>>;
    velocity?: Partial<Omit<Vec3fT, 'pack'>>;
    angularVelocity?: Partial<Omit<Vec3fT, 'pack'>>;
};

export interface MockResponseParams extends Partial<Pick<WorkshopResponseT, 'sequence' | 'kind' | 'outcome' | 'reason' |
    'phase' | 'epoch' | 'tick' | 'revision' | 'publication' | 'masterGeneration' | 'cadence' | 'projection'>> {
    physicalRevision: bigint;
    session?: Uint8Array | number[];
    electrical?: (Partial<Omit<ElectricalSourceRecordT, 'pack' | 'enabled'>> & { enabled?: ElectricalEnable })[];
    contactWorks?: Partial<Omit<ContactWorkStoreRecordT, 'pack'>>[];
    energyStores?: Partial<Omit<EnergyStoreReadRecordT, 'pack'>>[];
    bodies?: MockBody[];
    captures?: (Partial<Omit<CaptureLatchReadT, 'pack'>> & Pick<CaptureLatchReadT, 'target'>)[];
    activationMemory?: Partial<Omit<ActivationMemoryRecordT, 'pack'>>[];
}

function withMockFields<T extends object>(row: T, fields: Partial<NoInfer<T>> | undefined): T {
    const defined = Object.entries(fields ?? {}).filter(([, value]: [string, unknown]) => value != null);
    return Object.assign(row, Object.fromEntries(defined));
}

function mockBody({ id, cell, local, rotation, com, velocity, angularVelocity }: MockBody): BodyReadT {
    return Object.assign(new BodyReadT(), {
        id: id ?? 1n,
        cell: withMockFields(new Vec3iT(), cell),
        local: withMockFields(new Vec3fT(), local),
        rotation: withMockFields(Object.assign(new Quat4fT(), { w: 1 }), rotation),
        com: withMockFields(new Vec3fT(), com),
        velocity: withMockFields(new Vec3fT(), velocity),
        angularVelocity: withMockFields(new Vec3fT(), angularVelocity),
    });
}

export function encodeWorkshopResponse(params: MockResponseParams): Buffer {
    const builder = new flatbuffers.Builder(1024);
    const session = params.session ? WorkshopResponse.createSessionVector(builder, params.session) : 0;
    const bodies = structVector(builder, params.bodies?.map(mockBody), WorkshopResponse.startBodiesVector);
    const captures = structVector(builder, params.captures?.map(row => withMockFields(new CaptureLatchReadT(), row)),
        WorkshopResponse.startCapturesVector);
    const electrical = structVector(builder, params.electrical?.map(row =>
        withMockFields(Object.assign(new ElectricalSourceRecordT(), { id: 1n, owner: 1n, enabled: ElectricalEnable.Enabled, available: true }), row)),
        WorkshopResponse.startElectricalVector);
    const contactWorks = structVector(builder, params.contactWorks?.map(row =>
        withMockFields(Object.assign(new ContactWorkStoreRecordT(), { id: 1n, owner: 1n, store: 1n }), row)), WorkshopResponse.startContactWorksVector);
    const energyStores = structVector(builder, params.energyStores?.map(row =>
        withMockFields(Object.assign(new EnergyStoreReadRecordT(), { id: 1n, owner: 1n }), row)), WorkshopResponse.startEnergyStoresVector);
    const memory = structVector(builder, params.activationMemory?.map(row =>
        withMockFields(Object.assign(new ActivationMemoryRecordT(), { node: 1n, kind: 6 }), row)), WorkshopResponse.startActivationMemoryVector);

    // The generated object packer always writes motion, even when empty. Use the direct builder to leave it absent.
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
    WorkshopResponse.addSession(builder, session);
    WorkshopResponse.addBodies(builder, bodies);
    WorkshopResponse.addCaptures(builder, captures);
    WorkshopResponse.addElectrical(builder, electrical);
    WorkshopResponse.addContactWorks(builder, contactWorks);
    WorkshopResponse.addEnergyStores(builder, energyStores);
    WorkshopResponse.addActivationMemory(builder, memory);
    const physicalCommit = Object.assign(new PhysicalCommitRevisionRecordT(), { value: params.physicalRevision });
    WorkshopResponse.addPhysicalCommit(builder, physicalCommit.pack(builder));
    builder.finishSizePrefixed(WorkshopResponse.endWorkshopResponse(builder), rootIdentifier(WireRootIdentifier.WorkshopResponse));
    return Buffer.from(builder.asUint8Array());
}

function structVector(builder: flatbuffers.Builder, rows: flatbuffers.IGeneratedObject[] | undefined,
    start: (builder: flatbuffers.Builder, length: number) => void): flatbuffers.Offset {
    return rows?.length ? builder.createStructOffsetList(rows, start) : 0;
}

export interface MockCommandParams extends Partial<Pick<WorkshopCommandT, 'sequence' | 'kind' | 'epoch' |
    'revision' | 'revisionKind' | 'cadence' | 'projection'>> {
    session?: Uint8Array | number[];
    runControl?: Partial<Omit<RunControlCommandRecordT, 'pack'>>;
    target?: Partial<Omit<CommandIdentityRecordT, 'pack'>>;
}

export function encodeWorkshopCommand(params: MockCommandParams): Buffer {
    const builder = new flatbuffers.Builder(1024);
    const session = params.session ? WorkshopCommand.createSessionVector(builder, params.session) : 0;
    WorkshopCommand.startWorkshopCommand(builder);
    if (params.sequence !== undefined) WorkshopCommand.addSequence(builder, params.sequence);
    if (params.kind !== undefined) WorkshopCommand.addKind(builder, params.kind);
    if (params.epoch !== undefined) WorkshopCommand.addEpoch(builder, params.epoch);
    if (params.revision !== undefined) WorkshopCommand.addRevision(builder, params.revision);
    if (params.revisionKind !== undefined) WorkshopCommand.addRevisionKind(builder, params.revisionKind);
    WorkshopCommand.addSession(builder, session);
    if (params.cadence !== undefined) WorkshopCommand.addCadence(builder, params.cadence);
    if (params.projection !== undefined) WorkshopCommand.addProjection(builder, params.projection);
    if (params.runControl) {
        const control = withMockFields(Object.assign(new RunControlCommandRecordT(), {
            kind: RunControlValueKind.Enable, enable: ElectricalEnable.Disabled,
        }), params.runControl);
        WorkshopCommand.addRunControl(builder, control.pack(builder));
    }
    if (params.target) {
        const target = withMockFields(new CommandIdentityRecordT(), params.target);
        WorkshopCommand.addTarget(builder, target.pack(builder));
    }
    builder.finishSizePrefixed(WorkshopCommand.endWorkshopCommand(builder), rootIdentifier(WireRootIdentifier.WorkshopCommand));
    return Buffer.from(builder.asUint8Array());
}
