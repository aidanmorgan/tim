using CuriousContraptions.Abstractions.Signal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

public readonly record struct CompiledSceneId(ulong Value);
public readonly record struct CompiledPresentationBinding(PresentationProperty Property, PresentationDriver Driver, PresentationValueMapping Mapping,
    float From, float To, PresentationCurve Curve, float Duration, PresentationOverlap Overlap, float Threshold,
    PresentationTimerPhase Phase, float Response, float? Intermediate, CuriousContraptions.Abstractions.Electrical.SupplyTerminal? Socket,
    SimulationEventKind Event, PresentationEventMagnitude EventMagnitude, PresentationActuatorStateValues? StateValues = null);
public readonly record struct CompiledPresentationTrack(AnimationTargetId Target, GpuBodyId Owner, int Binding,
    CompiledPresentationBinding Declaration, ActivationNodeDeclaration Activation, float SourceCapacity, SupplyConsumer? Actuator = null, RopeGuideId Guide = default, ElectricalSourceId Source = default, SupplyNodeId SupplyNode = default);
