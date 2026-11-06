using System;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Runtime;

public sealed class CoreAnimationCodec : IAnimationCodec
{
    public byte[] Control(RuntimeSessionId session, ClockGeneration master, CadenceRevision cadence,
        WorkshopAnimationControl control) => WorkshopAnimationWire.Control(session, master, cadence, control);
    public WorkshopAnimationControl ReadControl(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence) => WorkshopAnimationWire.ReadControl(bytes, session, master, cadence);
    public void Validate(WorkshopAnimationControl value) => WorkshopAnimationWire.Validate(value);
    public WorkshopAnimationSample Read(ReadOnlySpan<byte> bytes, RuntimeSessionId session,
        ClockGeneration master, CadenceRevision cadence, out AnimationOutputKind kind) => WorkshopAnimationWire.Read(bytes, session, master, cadence, out kind);
    public bool IsChannel(AnimationProperty channel) => WorkshopAnimationWire.IsChannel(channel);
    public AnimationValue Value(AnimationProperty channel, float value) => WorkshopAnimationWire.Value(channel, value);
    public int ControlBytes => WorkshopAnimationWire.ControlBytes;
    public int OutputBytes => WorkshopAnimationWire.OutputBytes;
    public int SessionOffset => WorkshopAnimationWire.SessionOffset;
    public int MasterOffset => WorkshopAnimationWire.MasterOffset;
    public int CadenceOffset => WorkshopAnimationWire.CadenceOffset;
    public int ControlSequenceOffset => WorkshopAnimationWire.ControlSequenceOffset;
    public int ControlGenerationOffset => WorkshopAnimationWire.ControlGenerationOffset;
    public int ControlKindOffset => WorkshopAnimationWire.ControlKindOffset;
    public int ControlVisibleOffset => WorkshopAnimationWire.ControlVisibleOffset;
    public int ControlTargetOffset => WorkshopAnimationWire.ControlTargetOffset;
    public int ControlWorldOffset => WorkshopAnimationWire.ControlWorldOffset;
    public int ControlFromOffset => WorkshopAnimationWire.ControlFromOffset;
    public int ControlToOffset => WorkshopAnimationWire.ControlToOffset;
    public int ControlCurveOffset => WorkshopAnimationWire.ControlCurveOffset;
    public int ControlEventOrdinalOffset => WorkshopAnimationWire.ControlEventOrdinalOffset;
    public int ControlEventPhaseOffset => WorkshopAnimationWire.ControlEventPhaseOffset;
    public int ControlDurationOffset => WorkshopAnimationWire.ControlDurationOffset;
    public int ControlPropertyOffset => WorkshopAnimationWire.ControlPropertyOffset;
    public int OutputGenerationOffset => WorkshopAnimationWire.OutputGenerationOffset;
    public int OutputPulseOffset => WorkshopAnimationWire.OutputPulseOffset;
    public int OutputAppliedAtOffset => WorkshopAnimationWire.OutputAppliedAtOffset;
    public int OutputValueOffset => WorkshopAnimationWire.OutputValueOffset;
    public int OutputKindOffset => WorkshopAnimationWire.OutputKindOffset;
    public int OutputTargetOffset => WorkshopAnimationWire.OutputTargetOffset;
    public int OutputWorldOffset => WorkshopAnimationWire.OutputWorldOffset;
    public int OutputEventOrdinalOffset => WorkshopAnimationWire.OutputEventOrdinalOffset;
    public int OutputEventPhaseOffset => WorkshopAnimationWire.OutputEventPhaseOffset;
    public int OutputPropertyOffset => WorkshopAnimationWire.OutputPropertyOffset;
}
