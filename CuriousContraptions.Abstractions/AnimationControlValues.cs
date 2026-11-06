using CuriousContraptions.Abstractions;
using System;
using System.Buffers.Binary;
using CuriousContraptions.Presentation;
namespace CuriousContraptions.Gpu;

/// <summary>The main thread's animation controls: UI clips and visibility (Story 9A.25b: one-shot world cues are never relayed; the
/// Animation worker drives them from the event ring).</summary>
public enum AnimationControlKind : uint { Reveal = 1, Hide = 2, Visibility = 3, Endpoint = 4 }
public enum AnimationOutputKind : uint { Acknowledgement = 1, Sample = 2, Rejected = 3 }
public readonly record struct WorkshopAnimationControl(AnimationTargetId Target, SimulationEpoch World,
    ulong Sequence, ulong Generation, AnimationControlKind Kind, bool Visible,
    float From, float To, float Duration, AnimationCurve Curve, uint EventOrdinal = 0, float EventPhase = default, AnimationProperty Property = AnimationProperty.Opacity);
public readonly record struct WorkshopAnimationSample(AnimationTargetId Target, SimulationEpoch World,
    ulong Generation, PulseOrdinal Pulse, MasterTimeNanoseconds AppliedAt, float Value, uint EventOrdinal, float EventPhase, AnimationProperty Property);

public enum AnimationDeferralCause : uint { Preparation = 1, Recalibrating = 2 }
