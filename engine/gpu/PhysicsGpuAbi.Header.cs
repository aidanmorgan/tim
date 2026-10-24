namespace CuriousContraptions.Gpu;

/// <summary>
/// The 144-byte world header (Story 9.4b, Murdoch-2 R2-F4; Story 9R.5b adds the prismatic and contact-load counts). Every reader and writer of the physical world names these
/// offsets; <c>WorkshopAbiLiteralScanTests</c> fails on a literal header offset in production code.
/// </summary>
public static partial class PhysicsGpuAbi
{
    /// <summary>One current layout identity; every other value is unsupported.</summary>
    public const int HeaderLayoutTagOffset = 0;
    /// <summary>"CTF4" (Story 9A.44): every declared and committed game value in every record is IEEE-754 f32. An in-memory layout
    /// identity; no save carries it.</summary>
    public const uint CurrentLayoutTag = 0x43544634;
    public const int HeaderStatusOffset = 4;
    public const int HeaderFailureOffset = 8;
    /// <summary>Header word holding the admitted body count.</summary>
    public const int HeaderBodyCountOffset = 12;
    public const int HeaderColliderCountOffset = 16;
    public const int HeaderMaterialCountOffset = 20;
    public const int HeaderSensorCountOffset = 24;
    public const int HeaderDynamicCountOffset = 28;
    public const int HeaderEpochOffset = 32;
    public const int HeaderTickOffset = 40;
    public const int HeaderCadenceOffset = 48;
    public const int HeaderPhysicalProfileOffset = 52;
    public const int HeaderRevisionOffset = 56;
    public const int HeaderDocumentLowOffset = 64;
    public const int HeaderDocumentHighOffset = 72;
    public const int HeaderNextIdentityOffset = 80;
    /// <summary>Header word holding the committed physical-step ordinal the next tick starts from.</summary>
    public const int HeaderSourceOrdinalOffset = 88;
    /// <summary>Header word holding the residence sensors' total captured occurrences.</summary>
    public const int HeaderCapturedCountOffset = 92;
    public const int HeaderGuideCountOffset = 96;
    public const int HeaderTriggerCountOffset = 100;
    public const int HeaderContactWorkCountOffset = 104;
    public const int HeaderWorkOccurrenceCountOffset = 108;
    /// <summary>Header word holding the prismatic joint count (Story 9R.5b; its table no longer carries an in-band header).</summary>
    public const int HeaderPrismaticCountOffset = 112;
    public const int HeaderOrientationSensorCountOffset = 116;
    public const int HeaderElectricalSourceCountOffset = 120;
    public const int HeaderElectricalBindingCountOffset = 124;
    /// <summary>Header word holding the contact-load row count (Story 9R.5b).</summary>
    public const int HeaderContactLoadCountOffset = 128;
    /// <summary>The header's zero tail.</summary>
    public const int HeaderPaddingOffset = 132;

    // Contact-work record (ContactWorkBytes): identity U64 at 0, owner body slot U32, target kind, the f32 target and
    // admission speeds, cooldown, named target slot, occurrence count, preload, padding 40..47, available work, the work
    // supplied this tick, padding from 56. Its first ContactWorkDeclarationBytes never change after admission.
    public const int ContactWorkOwnerOffset = 8;
    public const int ContactWorkTargetKindOffset = 12;
    public const int ContactWorkNamedTargetOffset = 28;
    public const int ContactWorkDeclarationBytes = 32;
    public const int ContactWorkReservedOffset = 40;
    public const int ContactWorkSuppliedOffset = 52;
    public const int ContactWorkPaddingOffset = 56;
    // Work-occurrence record (WorkOccurrenceBytes): the owning contact-work index U32 at 0 and the target body id U64 at 8.
    public const int WorkOccurrenceWorkOffset = 0;
    public const int WorkOccurrenceTargetOffset = 8;
    /// <summary>The body record's motion kind U32 (static or dynamic).</summary>
    public const int BodyMotionOffset = 8;
    /// <summary>The collider record's owning body slot U32.</summary>
    public const int ColliderBodyOffset = 8;

    // Trigger record (TriggerBytes): owner body slot, target kind, f32 threshold, named target slot, then the last
    // qualified contact: count, collider, physical-step ordinal, f32 phase and f32 approach, and the touching body slot.
    public const int TriggerOwnerOffset = 8;
    public const int TriggerTargetKindOffset = 12;
    public const int TriggerThresholdOffset = 16;
    public const int TriggerThresholdPaddingOffset = 20;
    public const int TriggerNamedTargetOffset = 24;
    public const int TriggerNamedPaddingOffset = 28;
    public const int TriggerCountOffset = 32;
    public const int TriggerColliderOffset = 36;
    public const int TriggerOrdinalOffset = 40;
    public const int TriggerPhaseOffset = 44;
    public const int TriggerApproachOffset = 48;
    public const int TriggerBodyOffset = 52;
    public const int TriggerPaddingOffset = 56;

    // Every record identity is a U64 at 0. Every scalar game value is one f32 (4 bytes), every vector three consecutive f32 and every
    // rotation four (x, y, z, w); a pose is a vector then a rotation (PoseBytes). Story 9A.44: no record holds a half-precision lane.
    public const int RecordIdentityOffset = 0;
    public const int VectorBytes = 12;
    public const int RotationBytes = 16;
    /// <summary>A rotation's f32 components after its x: y, z, then w.</summary>
    public const int RotationYOffset = 4;
    public const int RotationZOffset = 8;
    public const int RotationWOffset = VectorBytes;
    public const int PoseRotationOffset = VectorBytes;
    public const int PoseBytes = VectorBytes + RotationBytes;

    // Body record (BodyBytes): motion, cell, f32 local remainder, f32 rotation and velocities, then the immutable declared lanes
    // from BodyMassOffset: mass, drag, gravity, centre of mass, principal frame and moments, and the owned collider slot.
    public const int BodyCellPaddingOffset = 28;
    public const int BodyLocalPaddingOffset = 44;
    public const int BodyColliderSlotOffset = 148;
    public const int BodyPaddingOffset = 152;

    // Collider record (ColliderBytes): owning body slot, material slot, shape, f32 pose, radius and half-extents.
    public const int ColliderMaterialOffset = 12;
    public const int ColliderShapeOffset = 16;
    public const int ColliderPoseOffset = 24;
    public const int ColliderRadiusOffset = 52;
    public const int ColliderHalfExtentsOffset = 56;
    public const int ColliderPaddingOffset = 68;

    // Material record (MaterialBytes): f32 restitution, bounce threshold, friction and rolling resistance.
    public const int MaterialRestitutionOffset = 8;
    public const int MaterialBounceThresholdOffset = 12;
    public const int MaterialFrictionOffset = 16;
    public const int MaterialRollingResistanceOffset = 20;
    public const int MaterialPaddingOffset = 24;

    // Residence sensor record (SensorBytes): the immutable declaration (frame and target slots, participation, f32 pose, bounds,
    // speed limit and dwell) in its first SensorDeclarationBytes, then the committed residence state with f32 phases.
    public const int SensorFrameOffset = 8;
    public const int SensorTargetOffset = 12;
    public const int SensorParticipationOffset = 16;
    public const int SensorPoseOffset = 24;
    public const int SensorMinimumOffset = 52;
    public const int SensorMaximumOffset = 64;
    public const int SensorSpeedLimitOffset = 76;
    public const int SensorDwellOffset = 80;
    public const int SensorDeclarationBytes = 84;
    public const int SensorPhaseOffset = 84;
    public const int SensorStartOrdinalOffset = 88;
    public const int SensorStartPhaseOffset = 92;
    public const int SensorOccurrenceCountOffset = 96;
    public const int SensorEventOrdinalOffset = 100;
    public const int SensorEventPhaseOffset = 104;
    public const int SensorPaddingOffset = 108;

    // Planar guide record (GuideBytes): frame and target slots, f32 pose, bounds, maximum acceleration, support height and margin.
    public const int GuideFrameOffset = 8;
    public const int GuideTargetOffset = 12;
    public const int GuidePoseOffset = 16;
    public const int GuideMinimumOffset = 44;
    public const int GuideMaximumOffset = 56;
    public const int GuideMaximumAccelerationOffset = 68;
    public const int GuideSupportHeightOffset = 72;
    public const int GuideSupportMarginOffset = 76;

    // Orientation sensor record (OrientationSensorBytes): the immutable declaration (body slot, initial rotation and the f32
    // cosine of the half threshold angle) in its first OrientationSensorDeclarationBytes, then the sticky fired state.
    public const int OrientationSensorBodyOffset = 8;
    public const int OrientationSensorBodyPaddingOffset = 12;
    public const int OrientationSensorCosineOffset = 32;
    public const int OrientationSensorCosinePaddingOffset = 36;
    public const int OrientationSensorDeclarationBytes = 40;
    public const int OrientationSensorPhaseOffset = 40;
    public const int OrientationSensorOrdinalOffset = 44;
    public const int OrientationSensorEventPhaseOffset = 48;
    public const int OrientationSensorPaddingOffset = 52;
}
