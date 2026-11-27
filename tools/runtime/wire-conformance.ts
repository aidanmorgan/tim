import assert from 'node:assert/strict';
import * as flatbuffers from 'flatbuffers';
import * as wire from '../e2e/.wire/curious-contraptions/wire.js';
import { openFrame, rootIdentifier } from './wire-frame.js';

interface PackableObject {
    pack(builder: flatbuffers.Builder): flatbuffers.Offset;
}

interface UnpackableRoot {
    unpack(): PackableObject;
}

type RootDecoder = (bb: flatbuffers.ByteBuffer) => UnpackableRoot;

interface Mutation {
    field: string;
    value: unknown;
}

interface ConformanceOp {
    op: 'identifiers' | 'decode' | 'roundtrip';
    root?: string;
    asRoot?: string;
    hex?: string;
    mutate?: Mutation;
    probe?: number;
}

function requireString(value: unknown, label: string): string {
    if (typeof value !== 'string') {
        throw new Error(`Expected string for ${label}.`);
    }
    return value;
}

function resolveRootId(name: string): wire.WireRootIdentifier {
    const value: unknown = Reflect.get(wire.WireRootIdentifier, name);
    if (typeof value !== 'number') {
        throw new Error(`Unknown WireRootIdentifier member '${name}'.`);
    }
    return value;
}

function resolveRootDecoder(name: string): RootDecoder {
    const ctor: unknown = Reflect.get(wire, name);
    if (ctor === null || (typeof ctor !== 'object' && typeof ctor !== 'function')) {
        throw new Error(`Generated wire module has no root '${name}'.`);
    }
    const fn: unknown = Reflect.get(ctor, `getRootAs${name}`);
    if (typeof fn !== 'function') {
        throw new Error(`Generated root '${name}' has no getRootAs${name}.`);
    }
    return (bb: flatbuffers.ByteBuffer): UnpackableRoot => {
        const result: unknown = Reflect.apply(fn, ctor, [bb]);
        assert.ok(result !== null && typeof result === 'object');
        return result as UnpackableRoot;
    };
}

function float64Hex(value: number): string {
    const view = new DataView(new ArrayBuffer(8));
    view.setFloat64(0, value, false);
    return view.getBigUint64(0, false).toString(16).padStart(16, '0');
}

function serializeValue(_key: string, value: unknown): unknown {
    if (typeof value === 'number') {
        return { f64Hex: float64Hex(value) };
    }
    if (typeof value === 'bigint') {
        return { i64: value.toString() };
    }
    return value instanceof Uint8Array ? Array.from(value) : value;
}

function decodeMutationValue(value: unknown): unknown {
    if (value !== null && typeof value === 'object' && !Array.isArray(value) && 'i64' in value) {
        return BigInt(requireString(Reflect.get(value, 'i64'), 'i64'));
    }
    return value;
}

function applyMutation(target: object, mutation: Mutation): void {
    assert.ok(Object.hasOwn(target, mutation.field), `No unpacked field '${mutation.field}'.`);
    assert.ok(Reflect.set(target, mutation.field, decodeMutationValue(mutation.value)));
}


// The object API collapses absent vectors to empty lists. Keep the source's motion
// presence while using the generated builders for all response fields.
function packResponse(builder: flatbuffers.Builder, source: wire.WorkshopResponse, mutation?: Mutation): flatbuffers.Offset {
    const motion = source.motionArray();
    const value = source.unpack();
    if (mutation) {
        applyMutation(value, mutation);
    }
    const Response = wire.WorkshopResponse;
    const session = Response.createSessionVector(builder, value.session);
    const bodies = builder.createStructOffsetList(value.bodies, Response.startBodiesVector);
    const captures = builder.createStructOffsetList(value.captures, Response.startCapturesVector);
    const activations = builder.createStructOffsetList(value.activations, Response.startActivationsVector);
    const timers = builder.createStructOffsetList(value.timers, Response.startTimersVector);
    const contactWorks = builder.createStructOffsetList(value.contactWorks, Response.startContactWorksVector);
    const electrical = builder.createStructOffsetList(value.electrical, Response.startElectricalVector);
    const activationMemory = builder.createStructOffsetList(value.activationMemory, Response.startActivationMemoryVector);
    const motionOffset = motion === null ? 0 : Response.createMotionVector(builder, motion);
    const actuatorDrives = builder.createStructOffsetList(value.actuatorDrives, Response.startActuatorDrivesVector);
    const contactLoads = builder.createStructOffsetList(value.contactLoads, Response.startContactLoadsVector);
    const electricalLogic = builder.createStructOffsetList(value.electricalLogic, Response.startElectricalLogicVector);
    const ropes = Response.createRopesVector(builder, builder.createObjectOffsetList(value.ropes));
    const scene = value.scene?.pack(builder) ?? 0;
    const ropeGuides = Response.createRopeGuidesVector(builder, builder.createObjectOffsetList(value.ropeGuides));
    const compliantSurfaces = Response.createCompliantSurfacesVector(builder, builder.createObjectOffsetList(value.compliantSurfaces));
    const driveTargets = builder.createStructOffsetList(value.driveTargets, Response.startDriveTargetsVector);
    const transmissions = Response.createTransmissionsVector(builder, builder.createObjectOffsetList(value.transmissions));
    const retentions = Response.createRetentionsVector(builder, builder.createObjectOffsetList(value.retentions));
    const energyStores = builder.createStructOffsetList(value.energyStores, Response.startEnergyStoresVector);
    const chamberRegions = builder.createStructOffsetList(value.chamberRegions, Response.startChamberRegionsVector);
    const finiteWorkActuators = builder.createStructOffsetList(value.finiteWorkActuators, Response.startFiniteWorkActuatorsVector);
    const gasStores = builder.createStructOffsetList(value.gasStores, Response.startGasStoresVector);
    const integrityObservers = builder.createStructOffsetList(value.integrityObservers, Response.startIntegrityObserversVector);
    const airflow = builder.createStructOffsetList(value.airflow, Response.startAirflowVector);
    const coordinateTravel = builder.createStructOffsetList(value.coordinateTravel, Response.startCoordinateTravelVector);

    Response.startWorkshopResponse(builder);
    Response.addSequence(builder, value.sequence);
    Response.addKind(builder, value.kind);
    Response.addOutcome(builder, value.outcome);
    Response.addReason(builder, value.reason);
    Response.addPhase(builder, value.phase);
    Response.addEpoch(builder, value.epoch);
    Response.addTick(builder, value.tick);
    Response.addRevision(builder, value.revision);
    Response.addPublication(builder, value.publication);
    Response.addMasterGeneration(builder, value.masterGeneration);
    Response.addCadence(builder, value.cadence);
    Response.addProjection(builder, value.projection);
    Response.addSession(builder, session);
    Response.addBodies(builder, bodies);
    Response.addCaptures(builder, captures);
    Response.addActivations(builder, activations);
    Response.addTimers(builder, timers);
    Response.addContactWorks(builder, contactWorks);
    Response.addElectrical(builder, electrical);
    Response.addActivationMemory(builder, activationMemory);
    Response.addActuatorDrives(builder, actuatorDrives);
    Response.addContactLoads(builder, contactLoads);
    Response.addElectricalLogic(builder, electricalLogic);
    Response.addRopes(builder, ropes);
    Response.addScene(builder, scene);
    Response.addRopeGuides(builder, ropeGuides);
    Response.addCompliantSurfaces(builder, compliantSurfaces);
    Response.addDriveTargets(builder, driveTargets);
    Response.addTransmissions(builder, transmissions);
    Response.addRetentions(builder, retentions);
    Response.addEnergyStores(builder, energyStores);
    Response.addChamberRegions(builder, chamberRegions);
    Response.addFiniteWorkActuators(builder, finiteWorkActuators);
    Response.addGasStores(builder, gasStores);
    Response.addIntegrityObservers(builder, integrityObservers);
    Response.addAirflow(builder, airflow);
    Response.addCoordinateTravel(builder, coordinateTravel);
    if (motion !== null) {
        Response.addMotion(builder, motionOffset);
    }
    if (value.capture !== null) {
        Response.addCapture(builder, value.capture.pack(builder));
    }
    if (value.memory !== null) {
        Response.addMemory(builder, value.memory.pack(builder));
    }
    assert.ok(value.physicalCommit);
    Response.addPhysicalCommit(builder, value.physicalCommit.pack(builder));
    return Response.endWorkshopResponse(builder);
}

function packRoot(builder: flatbuffers.Builder, root: UnpackableRoot, mutation?: Mutation): flatbuffers.Offset {
    if (root instanceof wire.WorkshopResponse) {
        return packResponse(builder, root, mutation);
    }
    const value = root.unpack();
    if (mutation) {
        applyMutation(value, mutation);
    }
    return value.pack(builder);
}

function runOp(req: ConformanceOp): unknown {
    if (req.op === 'identifiers') {
        const identifiers: Record<string, string> = {};
        for (const [name, value] of Object.entries(wire.WireRootIdentifier)) {
            if (typeof value === 'number') {
                identifiers[name] = rootIdentifier(value);
            }
        }
        return { ok: true, identifiers, probe: req.probe === undefined ? undefined : rootIdentifier(req.probe) };
    }
    const root = requireString(req.root, 'root');
    const hex = requireString(req.hex, 'hex');
    const rootId = resolveRootId(root);
    const decode = resolveRootDecoder(root);
    const bytes = Buffer.from(hex, 'hex');

    let bb: flatbuffers.ByteBuffer;
    try {
        bb = openFrame(bytes, rootId);
    } catch (err) {
        return { ok: false, error: err instanceof Error ? err.message : String(err) };
    }

    const decoded = decode(bb);
    if (req.op === 'decode') {
        const unpacked = decoded.unpack();
        const value = decoded instanceof wire.WorkshopResponse
            ? { ...unpacked, motion: decoded.motionArray() }
            : unpacked;
        return { ok: true, value };
    }

    if (req.op === 'roundtrip') {

        if (decoded instanceof wire.WorkshopResponse && req.mutate?.field === 'motion') {
            return { ok: false, error: 'Response motion editing is unsupported by the conformance harness.' };
        }
        const builder = new flatbuffers.Builder(1024);
        const offset = packRoot(builder, decoded, req.mutate);
        const outputId = req.asRoot === undefined ? rootId : resolveRootId(req.asRoot);
        builder.finishSizePrefixed(offset, rootIdentifier(outputId));
        return { ok: true, hex: Buffer.from(builder.asUint8Array()).toString('hex') };
    }

    throw new Error(`Unsupported op '${String(req.op)}'.`);
}

let text = '';
for await (const chunk of process.stdin) text += String(chunk);
const request: unknown = JSON.parse(text);
assert.ok(Array.isArray(request));
const results = request.map((item: unknown) => {
    assert.ok(item !== null && typeof item === 'object');
    return runOp(item as ConformanceOp);
});
process.stdout.write(JSON.stringify(results, serializeValue));
