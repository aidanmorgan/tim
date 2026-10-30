// Story 9R.1: the one comparison authority for Run/Reset restoration.
// A snapshot is the committed read carried by an Applied Save acknowledgement (the Build-phase state Reset re-admits) plus the
// authored construction in the IndexedDB save slot, flattened to an ordered `path -> value` list. Every f32 is its raw IEEE-754
// bit pattern read with `bb.readUint32` at the field's offset (never a decoded number, so -0 and every NaN payload stay distinct).
// The fields are listed explicitly below, in schema order, because the generated FlatBuffers classes expose only `bb`/`bb_pos`;
// tools/e2e/unit/run-reset-snapshot.test.ts checks the listing against schemas/workshop_wire.fbs and the generated offsets.
import * as flatbuffers from 'flatbuffers';
import { decodeSavedConstruction, decodeWorkshopResponse } from './workshop-wire.ts';

type TypeKind = 'struct' | 'table';
interface TypeListing { readonly kind: TypeKind; readonly fields: readonly (readonly [string, string])[] }

/** The listed schema: field names and type spellings exactly as schemas/workshop_wire.fbs declares them. */
export const SNAPSHOT_SCHEMA = {
    enums: { SimulationPhase: 'uint8', CommandKind: 'uint32', CommandOutcome: 'uint8', ResponseKind: 'uint8', ElectricalEnable: 'uint8', ObservationPayload: 'uint8' } as Readonly<Record<string, string>>,
    types: {
        TensileSpanRecord: { kind: 'table', fields: [["id", "ulong"], ["route", "ulong"], ["order", "uint"], ["body_a", "ulong"], ["local_a", "Vec3f"], ["body_b", "ulong"], ["local_b", "Vec3f"], ["maximum_length", "float"], ["complete", "bool"]] },
        PulleyWheelReadRecord: { kind: 'table', fields: [["pulley", "ulong"], ["travel", "float"], ["angle", "float"]] },
        RopeSpanReadRecord: { kind: 'table', fields: [["id", "ulong"], ["route", "ulong"], ["order", "uint"], ["body_a", "ulong"], ["body_b", "ulong"], ["attachment_a", "Vec3f"], ["attachment_b", "Vec3f"], ["maximum_length", "float"], ["current_length", "float"], ["integrity", "uint"], ["state", "uint"], ["cut_span", "ulong"]] },
        Vec3f: { kind: 'struct', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        Vec3i: { kind: 'struct', fields: [["x", "int32"], ["y", "int32"], ["z", "int32"]] },
        Quat4f: { kind: 'struct', fields: [["x", "float"], ["y", "float"], ["z", "float"], ["w", "float"]] },
        BodyRead: { kind: 'struct', fields: [["id", "uint64"], ["cell", "Vec3i"], ["local", "Vec3f"], ["rotation", "Quat4f"], ["com", "Vec3f"], ["velocity", "Vec3f"], ["angular_velocity", "Vec3f"]] },
        CaptureLatchRead: { kind: 'struct', fields: [["sensor", "uint64"], ["target", "uint64"], ["phase", "uint32"], ["event_ordinal", "uint32"], ["event_phase", "float"]] },
        ElectricalSourceRecord: { kind: 'struct', fields: [["id", "uint64"], ["owner", "uint64"], ["remaining", "float"], ["debit", "float"], ["enabled", "uint8"], ["available", "bool"]] },
        ContactWorkStoreRecord: { kind: 'struct', fields: [["id", "uint64"], ["owner", "uint64"], ["occurrence_count", "uint32"], ["remaining_energy", "float"], ["supplied_energy", "float"]] },
        ActivationRecord: { kind: 'struct', fields: [["node", "uint64"], ["owner", "uint64"], ["source", "uint64"], ["body", "uint64"], ["collider", "uint64"], ["phase", "uint32"], ["event_ordinal", "uint32"], ["event_phase", "float"], ["approach_speed", "float"], ["kind", "uint32"], ["emitter", "uint64"], ["cause_ordinal", "uint32"], ["cause_phase", "float"]] },
        TimerRecord: { kind: 'struct', fields: [["node", "uint64"], ["phase", "uint32"], ["started_tick", "uint64"], ["due_tick", "uint64"], ["input_kind", "uint32"], ["input_ordinal", "uint32"], ["input_phase", "float"], ["input_approach_speed", "float"], ["input_emitter", "uint64"], ["cause_source", "uint64"], ["cause_body", "uint64"], ["cause_collider", "uint64"], ["cause_ordinal", "uint32"], ["cause_phase", "float"]] },
        ActivationMemoryRecord: { kind: 'struct', fields: [["node", "uint64"], ["kind", "uint32"], ["count", "uint32"], ["last_ordinal", "uint32"], ["last_phase", "float"], ["phase", "uint32"], ["due_tick", "uint64"]] },
        ElectricalLogicRecord: { kind: 'struct', fields: [["owner", "uint64"], ["inputs", "uint8"], ["supplied", "bool"], ["active", "bool"]] },
        ActuatorDriveRecord: { kind: 'struct', fields: [["joint", "uint64"], ["owner", "uint64"], ["supply_closed", "bool"], ["moving_contact", "bool"], ["coordinate", "float"], ["speed", "float"], ["lower", "float"], ["upper", "float"]] },
        ContactLoadRecord: { kind: 'struct', fields: [["sensor", "uint64"], ["frame", "uint64"], ["region", "uint64"], ["measured_mass", "float"], ["phase", "uint32"]] },
        ClockStamp: { kind: 'struct', fields: [["time", "int64"], ["uncertainty", "uint64"], ["domain", "uint8"]] },
        ElectricalControl: { kind: 'struct', fields: [["owner", "uint64"], ["enabled", "uint8"]] },
        MemoryObservationRecord: { kind: 'struct', fields: [["used_before", "int64"], ["used_after", "int64"], ["duration_ticks", "int64"], ["frequency", "int64"], ["allocated_before", "int64"], ["allocated_after", "int64"], ["collections_before", "int32"], ["collections_after", "int32"]] },
        CommandIdentityRecord: { kind: 'struct', fields: [["sequence", "uint64"], ["epoch", "uint64"], ["revision", "uint64"], ["cadence", "uint64"], ["projection", "uint64"]] },
        WorkshopResponse: { kind: 'table', fields: [["sequence", "uint64"], ["kind", "ResponseKind"], ["outcome", "CommandOutcome"], ["reason", "uint8"], ["phase", "SimulationPhase"], ["epoch", "uint64"], ["tick", "uint64"], ["revision", "uint64"], ["publication", "uint64"], ["master_generation", "uint64"], ["cadence", "uint64"], ["projection", "uint64"], ["session", "[uint8]"], ["capture", "ClockStamp"], ["bodies", "[BodyRead]"], ["captures", "[CaptureLatchRead]"], ["activations", "[ActivationRecord]"], ["timers", "[TimerRecord]"], ["contact_works", "[ContactWorkStoreRecord]"], ["electrical", "[ElectricalSourceRecord]"], ["activation_memory", "[ActivationMemoryRecord]"], ["motion", "[uint8]"], ["memory", "MemoryObservationRecord"], ["actuator_drives", "[ActuatorDriveRecord]"], ["contact_loads", "[ContactLoadRecord]"], ["electrical_logic", "[ElectricalLogicRecord]"], ["ropes", "[RopeSpanReadRecord]"], ["scene", "CompiledSceneRecord"], ["pulley_wheels", "[PulleyWheelReadRecord]"]] },
        WorkshopCommand: { kind: 'table', fields: [["sequence", "uint64"], ["kind", "CommandKind"], ["epoch", "uint64"], ["revision", "uint64"], ["revision_kind", "uint32"], ["session", "[uint8]"], ["cadence", "uint64"], ["projection", "uint64"], ["electrical", "ElectricalControl"], ["cut_span", "ulong"], ["target", "CommandIdentityRecord"], ["scene", "CompiledSceneRecord"]] },
        ObservationInterval: { kind: 'struct', fields: [["lower_low", "uint64"], ["lower_high", "int64"], ["upper_low", "uint64"], ["upper_high", "int64"]] },
        ObservationSlot: { kind: 'struct', fields: [["receipt", "int64"], ["sequence", "uint64"], ["interval", "ObservationInterval"]] },
        ObservationExchange: { kind: 'struct', fields: [["before", "int64"], ["master", "int64"], ["after", "int64"], ["epoch", "uint32"]] },
        FaultObservation: { kind: 'table', fields: [["kind", "uint32"], ["site", "uint32"], ["exception_type", "string"], ["detail", "string"]] },
        ReplyObservation: { kind: 'table', fields: [["probe", "[uint8]"], ["master_received", "int64"], ["master_sent", "int64"], ["received", "int64"], ["derivation", "uint32"]] },
        RecalibrationObservation: { kind: 'table', fields: [["peer", "[uint8]"], ["cause", "uint32"], ["count", "uint64"], ["state_before", "uint32"], ["has_derivation", "bool"], ["derivation", "uint32"], ["display_epoch", "uint64"], ["at", "int64"], ["recovery_started", "int64"], ["recovery_sends", "int32"], ["pending", "[uint8]"], ["reply", "ReplyObservation"], ["slots", "[ObservationSlot]"]] },
        RejectedClockObservation: { kind: 'table', fields: [["role", "uint32"], ["count", "uint64"], ["length", "int32"], ["received", "int64"], ["detail", "string"]] },
        LateRenewalObservation: { kind: 'table', fields: [["role", "uint32"], ["count", "uint64"], ["lapse_age", "int64"], ["exchanges", "int32"], ["renewed_at", "int64"], ["retained", "ObservationInterval"], ["fresh", "ObservationInterval"], ["has_derivation", "bool"], ["derivation", "uint32"], ["display_epoch", "uint64"]] },
        WideOffsetObservation: { kind: 'table', fields: [["offset", "int64"], ["admitted_lower", "int64"], ["admitted_upper", "int64"], ["declared_uncertainty", "int64"], ["count", "uint64"]] },
        DerivationObservation: { kind: 'table', fields: [["report", "uint32"], ["outcome", "uint32"], ["epoch", "uint32"], ["correction", "int64"], ["has_pending", "bool"], ["pending", "int64"], ["has_admitted", "bool"], ["admitted_lower", "int64"], ["admitted_upper", "int64"], ["exchange", "ObservationExchange"], ["qualified", "int32"], ["startup_attempts", "int32"], ["startup_outliers", "int32"], ["startup_inconclusive", "int32"], ["startup_unanswered", "int32"], ["checks", "uint64"], ["slews", "uint64"], ["rederivations", "uint64"], ["inconclusive", "uint64"], ["unanswered", "uint64"], ["check_retries", "uint64"]] },
        DeferralObservation: { kind: 'table', fields: [["cause", "uint32"], ["count", "uint64"]] },
        DebtObservation: { kind: 'table', fields: [["tick", "uint64"], ["reason", "uint32"], ["oldest", "uint64"], ["age_milliseconds", "int64"]] },
        TickFailureObservation: { kind: 'table', fields: [["outcome", "uint32"], ["reason", "uint32"], ["fault", "uint32"]] },
        TransferObservation: { kind: 'table', fields: [["response", "[uint8]"]] },
        TraceSampleObservation: { kind: 'table', fields: [["record", "[uint8]"]] },
        CaptureObservation: { kind: 'table', fields: [["kind", "uint32"], ["stage", "uint32"], ["identity", "uint64"], ["epoch", "uint64"], ["cadence", "uint32"], ["physical", "uint32"], ["revision", "uint64"], ["end", "uint32"], ["status", "uint32"], ["expected_count", "int32"], ["count", "int32"], ["chunk", "int32"], ["chunks", "int32"], ["first", "int32"], ["scene", "uint64"], ["session", "[uint8]"], ["samples", "[TraceSampleObservation]"], ["milliseconds", "[double]"]] },
        DiagnosticObservation: { kind: 'table', fields: [["kind", "ObservationPayload"], ["fault", "FaultObservation"], ["recalibration", "RecalibrationObservation"], ["rejected_clock", "RejectedClockObservation"], ["late_renewal", "LateRenewalObservation"], ["wide_offset", "WideOffsetObservation"], ["derivation", "DerivationObservation"], ["deferral", "DeferralObservation"], ["debt", "DebtObservation"], ["tick_failure", "TickFailureObservation"], ["transfer", "TransferObservation"], ["capture", "CaptureObservation"]] },
        WorkshopObservationBatch: { kind: 'table', fields: [["session", "[uint8]"], ["role", "uint32"], ["records", "[DiagnosticObservation]"], ["incomplete", "string"]] },
        CapabilityRigidBodyDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["motion", "uint"], ["cell", "CapabilityCellOriginRecord"], ["local", "CapabilityLocalPositionRecord"], ["rotation", "CapabilityCanonicalRotationRecord"], ["velocity", "CapabilityLinearVelocityRecord"], ["angular_velocity", "CapabilityAngularVelocityRecord"], ["mass", "float"], ["gravity", "CapabilityAccelerationVectorRecord"], ["linear_drag", "float"], ["mass_properties", "CapabilityRigidMassPropertiesRecord"], ["payload", "uint"]] },
        CapabilityCellOriginRecord: { kind: 'table', fields: [["x", "int"], ["y", "int"], ["z", "int"]] },
        CapabilityLocalPositionRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityCanonicalRotationRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"], ["w", "float"]] },
        CapabilityLinearVelocityRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityAngularVelocityRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityAccelerationVectorRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityRigidMassPropertiesRecord: { kind: 'table', fields: [["local_centre_of_mass", "CapabilityMetreVectorRecord"], ["principal_frame", "CapabilityCanonicalRotationRecord"], ["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityMetreVectorRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityColliderDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["body", "ulong"], ["material", "ulong"], ["shape", "uint"], ["pose", "CapabilityRigidLocalPoseRecord"], ["radius", "float"], ["half_extents", "CapabilityMetreVectorRecord"]] },
        CapabilityRigidLocalPoseRecord: { kind: 'table', fields: [["translation", "CapabilityMetreVectorRecord"], ["rotation", "CapabilityCanonicalRotationRecord"]] },
        CapabilityContactMaterialDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["restitution", "float"], ["bounce_threshold", "float"], ["friction", "float"], ["rolling_resistance", "float"]] },
        CapabilityResidenceSensorDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["frame", "ulong"], ["targets", "CapabilityBodyTargetSetRecord"], ["pose", "CapabilityRigidLocalPoseRecord"], ["minimum", "CapabilityMetreVectorRecord"], ["maximum", "CapabilityMetreVectorRecord"], ["speed_limit", "float"], ["dwell", "float"], ["participation", "uint"]] },
        CapabilityBodyTargetSetRecord: { kind: 'table', fields: [["kind", "uint"], ["body", "ulong"]] },
        CapabilityPlanarGuideDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["frame", "ulong"], ["targets", "CapabilityBodyTargetSetRecord"], ["pose", "CapabilityRigidLocalPoseRecord"], ["minimum", "CapabilityMetreVectorRecord"], ["maximum", "CapabilityMetreVectorRecord"], ["support_height", "float"], ["support_margin", "float"], ["maximum_acceleration", "float"]] },
        CapabilityContactTriggerDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["owner", "ulong"], ["targets", "CapabilityBodyTargetSetRecord"], ["threshold", "float"]] },
        CapabilityContactWorkDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["owner", "ulong"], ["targets", "CapabilityBodyTargetSetRecord"], ["target_speed", "float"], ["initial_energy", "float"], ["threshold", "float"], ["cooldown_physical_steps", "uint"]] },
        CapabilityOrientationSensorDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["body", "ulong"], ["initial", "CapabilityCanonicalRotationRecord"], ["threshold", "float"]] },
        CapabilityElectricalSourceDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["owner", "ulong"], ["capacity", "float"], ["maximum_power", "float"], ["initial_fraction", "float"], ["enabled", "uint"]] },
        CapabilityElectricalStorageBindingRecord: { kind: 'table', fields: [["source", "ulong"], ["storage", "ulong"]] },
        CapabilityPrismaticConstraintDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["body_a", "ulong"], ["body_b", "ulong"], ["frame_a", "CapabilityConstraintFrameRecord"], ["frame_b", "CapabilityConstraintFrameRecord"], ["lower", "float"], ["upper", "float"], ["stiffness", "float"], ["damping", "float"], ["collision", "uint"]] },
        CapabilityConstraintFrameRecord: { kind: 'table', fields: [["position", "CapabilityVector3Record"], ["rotation", "CapabilityQuaternionRecord"]] },
        CapabilityVector3Record: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"]] },
        CapabilityQuaternionRecord: { kind: 'table', fields: [["x", "float"], ["y", "float"], ["z", "float"], ["w", "float"]] },
        CapabilitySupplyNodeDeclarationRecord: { kind: 'table', fields: [["owner", "ulong"], ["kind", "uint"], ["source", "ulong"], ["consumers", "[ulong]"], ["operation", "uint"]] },
        CapabilitySupplyEdgeRecord: { kind: 'table', fields: [["source", "ulong"], ["target", "ulong"], ["input", "uint"]] },
        CapabilityContactLoadSensorDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["frame", "ulong"], ["region", "ulong"], ["minimum_x", "float"], ["maximum_x", "float"], ["minimum_z", "float"], ["maximum_z", "float"], ["local_normal", "CapabilityVector3Record"], ["minimum_alignment", "float"], ["maximum_gap", "float"], ["minimum_mass", "float"], ["supporting_colliders", "[ulong]"]] },
        CapabilityPrismaticDriveDeclarationRecord: { kind: 'table', fields: [["joint", "ulong"], ["target", "float"], ["max_speed", "float"], ["stopping_acceleration", "float"], ["max_effort", "float"], ["max_power", "float"]] },
        CapabilityActivationNodeDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["owner", "ulong"], ["kind", "uint"], ["trigger", "ulong"], ["duration_ticks", "uint"], ["sensor", "ulong"], ["passage", "ulong"], ["target_count", "uint"], ["interval_ticks", "uint"], ["power_source", "ulong"], ["collider", "ulong"], ["contact_load", "ulong"], ["release_hold_ticks", "uint"], ["completion", "uint"], ["boundary", "uint"], ["elapsed_signal", "uint"]] },
        CapabilityActivationEdgeRecord: { kind: 'table', fields: [["source", "ulong"], ["target", "ulong"], ["input", "uint"]] },
        CapabilityPassageSensorDeclarationRecord: { kind: 'table', fields: [["id", "ulong"], ["frame", "ulong"], ["cell", "CapabilityCellOriginRecord"], ["local", "CapabilityLocalPositionRecord"], ["rotation", "CapabilityCanonicalRotationRecord"], ["radius", "float"], ["rearm_clearance", "float"]] },
        CapabilityPresentationBindingRecord: { kind: 'table', fields: [["property", "uint"], ["driver", "uint"], ["from", "float"], ["to", "float"], ["curve", "uint"], ["duration", "float"], ["overlap", "uint"], ["threshold", "float"], ["phase", "uint"], ["response", "float"], ["intermediate", "float"], ["socket", "uint"], ["event", "uint"], ["event_magnitude", "uint"]] },
        CapabilityWorkshopCadenceSettingsRecord: { kind: 'table', fields: [["simulation", "uint"], ["physical", "uint"], ["animation", "uint"], ["presentation", "uint"], ["presentation_rate", "CapabilityPulseRateRecord"]] },
        CapabilityPulseRateRecord: { kind: 'table', fields: [["numerator", "uint"], ["denominator", "uint"]] },
        CompiledPresentationRecord: { kind: 'table', fields: [["target", "ulong"], ["owner", "ulong"], ["binding", "int"], ["declaration", "CapabilityPresentationBindingRecord"], ["activation", "CapabilityActivationNodeDeclarationRecord"], ["source_capacity", "float"]] },
        CompiledSceneRecord: { kind: 'table', fields: [["identity", "ulong"], ["revision", "ulong"], ["settings", "CapabilityWorkshopCadenceSettingsRecord"], ["document_low", "ulong"], ["document_high", "ulong"], ["next_identity", "ulong"], ["bodies", "[CapabilityRigidBodyDeclarationRecord]"], ["colliders", "[CapabilityColliderDeclarationRecord]"], ["materials", "[CapabilityContactMaterialDeclarationRecord]"], ["sensors", "[CapabilityResidenceSensorDeclarationRecord]"], ["guides", "[CapabilityPlanarGuideDeclarationRecord]"], ["triggers", "[CapabilityContactTriggerDeclarationRecord]"], ["contact_works", "[CapabilityContactWorkDeclarationRecord]"], ["orientation_sensors", "[CapabilityOrientationSensorDeclarationRecord]"], ["electrical_sources", "[CapabilityElectricalSourceDeclarationRecord]"], ["electrical_bindings", "[CapabilityElectricalStorageBindingRecord]"], ["prismatics", "[CapabilityPrismaticConstraintDeclarationRecord]"], ["tensile_spans", "[TensileSpanRecord]"], ["supply_nodes", "[CapabilitySupplyNodeDeclarationRecord]"], ["supply_edges", "[CapabilitySupplyEdgeRecord]"], ["contact_load_sensors", "[CapabilityContactLoadSensorDeclarationRecord]"], ["prismatic_drives", "[CapabilityPrismaticDriveDeclarationRecord]"], ["activation_nodes", "[CapabilityActivationNodeDeclarationRecord]"], ["activation_edges", "[CapabilityActivationEdgeRecord]"], ["passage_sensors", "[CapabilityPassageSensorDeclarationRecord]"], ["activation_substeps", "uint"], ["contact_owners", "[ulong]"], ["owners", "[ulong]"], ["editable_sources", "[ulong]"], ["presentation", "[CompiledPresentationRecord]"]] },
        CadenceSettingsRecord: { kind: 'struct', fields: [["simulation", "uint32"], ["physical", "uint32"], ["animation", "uint32"], ["presentation", "uint32"], ["presentation_rate_numerator", "uint32"], ["presentation_rate_denominator", "uint32"]] },
        ConnectionRecord: { kind: 'struct', fields: [["source", "uint64"], ["target", "uint64"], ["domain", "uint32"], ["from_socket", "uint32"], ["to_socket", "uint32"], ["has_rope_length", "bool"], ["rope_length", "float"]] },
        AssistanceKnotRecord: { kind: 'struct', fields: [["precision", "float"], ["position_window", "float"], ["rotation_window_degrees", "float"], ["maximum_position_correction", "float"], ["maximum_rotation_correction_degrees", "float"], ["blend", "float"], ["capture_margin", "float"], ["capture_speed", "float"], ["capture_dwell", "float"], ["guide_acceleration", "float"], ["trigger_threshold", "float"]] },
        ParameterRecord: { kind: 'table', fields: [["key", "string"], ["value", "float"]] },
        InstanceRecord: { kind: 'table', fields: [["id", "uint64"], ["part_id", "string"], ["locked", "bool"], ["cell", "Vec3i"], ["local", "Vec3f"], ["rotation", "Quat4f"], ["parameters", "[ParameterRecord]"]] },
        PuzzleRecord: { kind: 'table', fields: [["id", "string"], ["placement", "uint32"], ["precision", "float"], ["inventory_part", "string"], ["inventory_count", "uint32"], ["goal_kind", "uint32"], ["goal_body", "uint64"], ["goal_target", "uint64"], ["goal_delay", "float"], ["assistance", "[AssistanceKnotRecord]"]] },
        ConstructionRecord: { kind: 'table', fields: [["revision", "uint64"], ["settings", "CadenceSettingsRecord"], ["puzzle", "PuzzleRecord"], ["instances", "[InstanceRecord]"], ["connections", "[ConnectionRecord]"]] },
        SavedConstruction: { kind: 'table', fields: [["next_body_id", "uint64"], ["construction", "ConstructionRecord"]] },
    } as Readonly<Record<string, TypeListing>>,
} as const;

/** Identity paths that must advance (spec D2 as amended by the 20 Oct design review), matched by full top-level path only:
 *  a nested field with the same name (`saved.construction.revision`) is compared. */
const EXCLUDED_PATHS: ReadonlySet<string> = new Set([
    'read.sequence', 'read.revision', 'read.publication', 'read.epoch', 'read.capture', 'read.session', 'read.master_generation',
    'read.cadence', 'read.projection',
]);
const PHYSICAL_IDENTITY_FREE: ReadonlySet<string> = new Set(['read.tick', 'read.phase']);

const SCALAR_BYTES: Readonly<Record<string, number>> = {
    bool: 1, uint8: 1, uint16: 2, uint32: 4, int32: 4, float: 4, uint64: 8, int64: 8,
};
const ABSENT = 'absent';

export type SnapshotEntry = readonly [path: string, value: string];
/** An ordered, flattened snapshot. `bodies` and `tick` are taken from the same committed read. */
export interface RunResetSnapshot { readonly entries: readonly SnapshotEntry[]; readonly bodies: number; readonly tick: bigint }

function scalarOf(type: string): string | undefined {
    const declared = SNAPSHOT_SCHEMA.enums[type] ?? type;
    const aliases: Readonly<Record<string, string>> = { ulong: 'uint64', uint: 'uint32', ubyte: 'uint8', ushort: 'uint16', int: 'int32' };
    const resolved = aliases[declared] ?? declared;
    return resolved in SCALAR_BYTES ? resolved : undefined;
}

function listing(name: string): TypeListing {
    const found = SNAPSHOT_SCHEMA.types[name];
    if (!found) throw new Error(`run-reset snapshot: ${name} is not in the listed schema`);
    return found;
}

interface StructField { readonly name: string; readonly type: string; readonly offset: number }
interface StructLayout { readonly size: number; readonly alignment: number; readonly fields: readonly StructField[] }
const layouts = new Map<string, StructLayout>();

/** The FlatBuffers struct layout: each field aligned to its own alignment, the struct padded to its largest alignment. */
export function structLayout(name: string): StructLayout {
    const cached = layouts.get(name);
    if (cached) return cached;
    const type = listing(name);
    if (type.kind !== 'struct') throw new Error(`run-reset snapshot: ${name} is not a struct`);
    let offset = 0, alignment = 1;
    const fields: StructField[] = [];
    for (const [field, fieldType] of type.fields) {
        const scalar = scalarOf(fieldType);
        const { size, alignment: align } = scalar
            ? { size: SCALAR_BYTES[scalar], alignment: SCALAR_BYTES[scalar] } : structLayout(fieldType);
        offset = Math.ceil(offset / align) * align;
        fields.push({ name: field, type: fieldType, offset });
        offset += size;
        alignment = Math.max(alignment, align);
    }
    const layout = { size: Math.ceil(offset / alignment) * alignment, alignment, fields };
    layouts.set(name, layout);
    return layout;
}

/** The vtable slot of a table's field at `index` (FlatBuffers: 4 + 2 * field id). */
export function tableSlot(index: number): number {
    return 4 + 2 * index;
}

function hex32(value: number): string {
    return `0x${value.toString(16).padStart(8, '0')}`;
}

function readScalar(bb: flatbuffers.ByteBuffer, position: number, scalar: string): string {
    switch (scalar) {
        case 'bool': case 'uint8': return String(bb.readUint8(position));
        case 'uint16': return String(bb.readUint16(position));
        case 'uint32': return String(bb.readUint32(position));
        case 'int32': return String(bb.readInt32(position));
        case 'float': return hex32(bb.readUint32(position)); // raw IEEE-754 bits, never a decoded number
        case 'uint64': return bb.readUint64(position).toString();
        case 'int64': return bb.readInt64(position).toString();
        default: throw new Error(`run-reset snapshot: unknown scalar ${scalar}`);
    }
}

class Flattener {
    readonly entries: SnapshotEntry[] = [];
    private readonly paths = new Set<string>();

    constructor(private readonly bb: flatbuffers.ByteBuffer) {}

    private add(path: string, value: string): void {
        if (this.paths.has(path)) throw new Error(`run-reset snapshot: duplicate path ${path} (repeated element identity)`);
        this.paths.add(path);
        this.entries.push([path, value]);
    }

    struct(prefix: string, name: string, position: number): void {
        for (const field of structLayout(name).fields) {
            const path = `${prefix}.${field.name}`;
            const scalar = scalarOf(field.type);
            if (scalar) this.add(path, readScalar(this.bb, position + field.offset, scalar));
            else this.struct(path, field.type, position + field.offset);
        }
    }

    table(prefix: string, name: string, position: number): void {
        const type = listing(name);
        if (type.kind !== 'table') throw new Error(`run-reset snapshot: ${name} is not a table`);
        type.fields.forEach(([field, fieldType], index) => {
            const path = `${prefix}.${field}`;
            if (EXCLUDED_PATHS.has(path)) return;
            const offset = this.bb.__offset(position, tableSlot(index));
            if (offset === 0) { this.add(path, ABSENT); return; } // a table scalar equal to its default is not written
            const at = position + offset;
            const scalar = scalarOf(fieldType);
            if (scalar) this.add(path, readScalar(this.bb, at, scalar));
            else if (fieldType === 'string') this.add(path, JSON.stringify(this.bb.__string(at) as string));
            else if (fieldType.startsWith('[')) this.vector(path, fieldType.slice(1, -1), at);
            else if (listing(fieldType).kind === 'struct') this.struct(path, fieldType, at);
            else this.table(path, fieldType, this.bb.__indirect(at));
        });
    }

    private vector(path: string, element: string, at: number): void {
        const start = this.bb.__vector(at), length = this.bb.__vector_len(at);
        this.add(`${path}.length`, String(length));
        const scalar = scalarOf(element);
        for (let i = 0; i < length; i++) {
            if (scalar) { this.add(`${path}[${i}]`, readScalar(this.bb, start + i * SCALAR_BYTES[scalar], scalar)); continue; }
            const kind = listing(element).kind;
            const position = kind === 'struct' ? start + i * structLayout(element).size : this.bb.__indirect(start + i * 4);
            const key = this.elementKey(element, kind, position, i);
            if (kind === 'struct') this.struct(`${path}${key}`, element, position);
            else this.table(`${path}${key}`, element, position);
        }
    }

    // Elements that carry an `id` (bodies, instances, electrical sources, contact-work stores) are keyed by it.
    private elementKey(element: string, kind: TypeKind, position: number, index: number): string {
        const first = listing(element).fields[0];
        if (first?.[0] !== 'id' || first[1] !== 'uint64') return `[${index}]`;
        if (kind === 'struct') return `[id=${this.bb.readUint64(position).toString()}]`;
        const offset = this.bb.__offset(position, tableSlot(0));
        return `[id=${offset === 0 ? '0' : this.bb.readUint64(position + offset).toString()}]`;
    }
}

// Story 9A.2: every buffer is size-prefixed, so its root offset follows the 4-byte size.
function rootPosition(bb: flatbuffers.ByteBuffer): number {
    const at = bb.position() + flatbuffers.SIZE_PREFIX_LENGTH;
    return bb.readInt32(at) + at;
}

function flattenResponse(response: Uint8Array): { entries: SnapshotEntry[]; bodies: number; tick: bigint; outcome: number } {
    const decoded = decodeWorkshopResponse(response);
    const bb = new flatbuffers.ByteBuffer(response);
    const flattener = new Flattener(bb);
    flattener.table('read', 'WorkshopResponse', rootPosition(bb));
    return { entries: flattener.entries, bodies: decoded.bodiesLength(), tick: decoded.tick(), outcome: decoded.outcome() };
}

function requireApplied(outcome: number): void {
    if (outcome !== 0) throw new Error(`run-reset snapshot: the acknowledgement is not Applied (outcome ${outcome})`);
}

/** The physical half only: the committed read carried by one acknowledgement (used for the mid-run Pause read). */
export function readSnapshot(response: Uint8Array): RunResetSnapshot {
    const read = flattenResponse(response);
    requireApplied(read.outcome);
    return { entries: read.entries, bodies: read.bodies, tick: read.tick };
}

/** The snapshot of one Applied Save acknowledgement and the slot it saved. The bench declares how many bodies it admits. */
export function snapshotFrom(response: Uint8Array, savedBytes: Uint8Array, bench: { readonly bodies: number }): RunResetSnapshot {
    const read = readSnapshot(response);
    if (read.bodies === 0) throw new Error('run-reset snapshot: the committed read has no bodies');
    if (read.bodies !== bench.bodies)
        throw new Error(`run-reset snapshot: the committed read has ${read.bodies} bodies, the bench has ${bench.bodies}`);
    try { decodeSavedConstruction(savedBytes); }
    catch (error) { throw new Error('run-reset snapshot: the save slot is not a CCWS construction', { cause: error }); }
    const bb = new flatbuffers.ByteBuffer(savedBytes);
    const flattener = new Flattener(bb);
    flattener.table('saved', 'SavedConstruction', rootPosition(bb));
    return { entries: [...read.entries, ...flattener.entries], bodies: read.bodies, tick: read.tick };
}

/** Bit-equality of two snapshots: throws on the first differing path (in S0 order), or when the path sets differ. */
export function assertSnapshotEqual(expected: RunResetSnapshot, actual: RunResetSnapshot, label = 'Reset'): void {
    compare(expected, actual, label, new Set());
}

/** After a reload and Load, the page's construction revision restarts (it counts this page's edits), so the loaded snapshot is
 *  compared with S0 except `saved.construction.revision`; every other path, the committed read included, must be bit-equal. */
export function assertLoadRestores(expected: RunResetSnapshot, loaded: RunResetSnapshot): void {
    compare(expected, loaded, 'Load', LOAD_IDENTITY_PATHS);
}
const LOAD_IDENTITY_PATHS: ReadonlySet<string> = new Set(['saved.construction.revision']);

function compare(expected: RunResetSnapshot, actual: RunResetSnapshot, label: string, ignored: ReadonlySet<string>): void {
    const actualValues = new Map(actual.entries);
    for (const [path, value] of expected.entries) {
        const other = actualValues.get(path);
        if (other === undefined) throw new Error(`${label}: path set differs: ${path} is in S0 only`);
        if (other !== value && !ignored.has(path)) throw new Error(`${label}: ${path} ${value}≠${other}`);
    }
    const expectedPaths = new Set(expected.entries.map(([path]) => path));
    for (const [path] of actual.entries)
        if (!expectedPaths.has(path)) throw new Error(`${label}: path set differs: ${path} is after ${label} only`);
}

/** The compared value at one exact path (an element-specific assertion's read of the same snapshot). */
export function snapshotValue(snapshot: RunResetSnapshot, path: string): string {
    const found = snapshot.entries.find(([candidate]) => candidate === path);
    if (!found) throw new Error(`run-reset snapshot: no path ${path}`);
    return found[1];
}

/** The entries under one path prefix (for example one body's `read.bodies[id=…]`), in snapshot order. */
export function snapshotEntries(snapshot: RunResetSnapshot, prefix: string): SnapshotEntry[] {
    return snapshot.entries.filter(([path]) => path === prefix || path.startsWith(`${prefix}.`) || path.startsWith(`${prefix}[`));
}

/** One committed Counter / System Clock memory row as compared (decimal integers; last_phase as f32 bits). */
export interface ActivationMemoryRow {
    readonly node: string; readonly kind: string; readonly count: string; readonly last_ordinal: string; readonly last_phase: string;
    readonly phase: string; readonly due_tick: string;
}
/** ActivationNodeKind values of the rows (engine/gpu/ActivationNetwork.cs). A ContactLoadSource row (a Pressure Plate) holds its
 *  release hold's due tick (Story 9R.5). */
export const MemoryKind = { Counter: '6', Oscillator: '7', ContactLoadSource: '10' } as const;

/** The committed activation memory rows of a snapshot's read, in wire order. */
export function activationMemoryRows(snapshot: RunResetSnapshot): ActivationMemoryRow[] {
    const length = Number(snapshot.entries.find(([path]) => path === 'read.activation_memory.length')?.[1] ?? '0');
    const rows: ActivationMemoryRow[] = [];
    for (let i = 0; i < length; i++) {
        const at = (field: string) => snapshotValue(snapshot, `read.activation_memory[${i}].${field}`);
        rows.push({ node: at('node'), kind: at('kind'), count: at('count'), last_ordinal: at('last_ordinal'), last_phase: at('last_phase'),
            phase: at('phase'), due_tick: at('due_tick') });
    }
    return rows;
}

/** A run cycle is not vacuous: its committed read is past tick 0 and differs from S0 in at least one compared physical path. */
export function assertRunAdvanced(s0: RunResetSnapshot, during: RunResetSnapshot): void {
    if (during.tick === 0n) throw new Error('run-reset snapshot: the mid-run committed read is at tick 0');
    const values = new Map(s0.entries);
    const differs = during.entries.some(([path, value]) =>
        !PHYSICAL_IDENTITY_FREE.has(path) && values.has(path) && values.get(path) !== value);
    if (!differs) throw new Error(`run-reset snapshot: no compared path differs from S0 at tick ${during.tick}`);
}
