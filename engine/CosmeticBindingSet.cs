using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>A part's cosmetic bindings, keyed by its one declared cosmetic curve and an optional timer phase (Story 9A.1 extracted it
/// from the part, which composes it). Phase None follows the blend; a phase binding writes its value whenever the sampled phase
/// matches. The shared Animation worker owns every curve; the set only admits and applies bindings.</summary>
internal sealed class CosmeticBindingSet
{
    private readonly List<(WorkshopVisualBinding Binding, AnimationTimerPhase Phase)> _bindings = new();
    public bool IsEmpty => _bindings.Count == 0;
    public CosmeticCurveDeclaration Declaration { get; private set; }

    public void Add(CosmeticCurveDeclaration declaration, Node3D target, WorkshopVisualProperty property, float neutral, float active,
        AnimationTimerPhase phase)
    {
        declaration.Validate();
        if (!declaration.IsDeclared) throw new ArgumentException("Visual bindings require a declared cosmetic curve.");
        if (_bindings.Count != 0 && Declaration != declaration) throw new ArgumentException("A part binds exactly one cosmetic declaration.");
        if (phase != AnimationTimerPhase.None && declaration.Source != AnimationFeedbackSource.Timer)
            throw new ArgumentException("Phase bindings require timer feedback.");
        _bindings.Add((new(target, property, neutral, active), phase));
        Declaration = declaration;
    }

    public void Apply(WorkshopCosmeticSample sample)
    {
        // Game-grade: an out-of-range or non-finite blend keeps the last committed values.
        if (!float.IsFinite(sample.Blend) || sample.Blend < (float)0 || sample.Blend > (float)1 || !Enum.IsDefined(sample.Phase)) return;
        foreach (var (binding, phase) in _bindings)
            if (phase == AnimationTimerPhase.None) binding.Apply(new(sample.Blend));
            else if (phase == sample.Phase) binding.Apply(new((float)1));
    }
}
