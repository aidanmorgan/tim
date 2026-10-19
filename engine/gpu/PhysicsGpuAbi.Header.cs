namespace CuriousContraptions.Gpu;

/// <summary>
/// The 128-byte world header (Story 9.4b, Murdoch-2 R2-F4). Every reader and writer of the physical world names these
/// offsets; <c>WorkshopAbiLiteralScanTests</c> fails on a literal header offset in production code.
/// </summary>
public static partial class PhysicsGpuAbi
{
    public const int HeaderVersionOffset = 0;
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
    public const int HeaderCachePairCountOffset = 112;
    public const int HeaderOrientationSensorCountOffset = 116;
    public const int HeaderElectricalSourceCountOffset = 120;
    public const int HeaderElectricalBindingCountOffset = 124;

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
    // Work-occurrence record (WorkOccurrenceBytes): the owning contact-work index U16 at 0 and the target body id U64 at 4.
    public const int WorkOccurrenceWorkOffset = 0;
    public const int WorkOccurrenceTargetOffset = 4;
    /// <summary>The body record's motion kind U32 (static or dynamic).</summary>
    public const int BodyMotionOffset = 8;
    /// <summary>The collider record's owning body slot U32.</summary>
    public const int ColliderBodyOffset = 8;

    // Trigger record (TriggerBytes): owner body slot, target kind, binary16 threshold, named target slot, then the last
    // qualified contact: count, collider, physical-step ordinal, binary16 phase and approach, and the touching body slot.
    public const int TriggerOwnerOffset = 8;
    public const int TriggerTargetKindOffset = 12;
    public const int TriggerThresholdOffset = 16;
    public const int TriggerThresholdPaddingOffset = 18;
    public const int TriggerNamedTargetOffset = 24;
    public const int TriggerNamedPaddingOffset = 28;
    public const int TriggerCountOffset = 32;
    public const int TriggerColliderOffset = 36;
    public const int TriggerOrdinalOffset = 40;
    public const int TriggerPhaseOffset = 44;
    public const int TriggerApproachOffset = 46;
    public const int TriggerBodyOffset = 48;
    public const int TriggerPaddingOffset = 52;
}
