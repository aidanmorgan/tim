using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Render/input node only. Durable construction and motion are canonical worker values.</summary>
public partial class MachinePart : Node3D
{
    public PartDefinition Definition { get; private set; } = null!;
    public string Uid => Name;
    public GpuBodyId AuthoredId { get; internal set; }
    public bool Locked { get; internal set; }
    protected internal Node3D Visual { get; private set; } = null!;
    private float _pickRadius;
    protected float PickRadius
    {
        get => _pickRadius;
        set
        {
            if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _pickRadius = value;
            if (_highlight?.Mesh is TorusMesh ring)
            {
                ring.InnerRadius = Mathf.Max(0, value - .025f);
                ring.OuterRadius = value + .025f;
            }
        }
    }
    private MeshInstance3D? _highlight;
    private bool _built;
    public void Configure(PartDefinition definition)
    {
        if (_built || Definition is not null) throw new InvalidOperationException("Part is already configured.");
        if (definition.WorkshopKind is not (WorkshopPartKind.Basketball or WorkshopPartKind.Receiver or WorkshopPartKind.Ramp or WorkshopPartKind.ImpactSwitch or WorkshopPartKind.SignalLamp or WorkshopPartKind.Wall or WorkshopPartKind.Delay or WorkshopPartKind.PinballBumper) ||
            (definition.WorkshopKind == WorkshopPartKind.Basketball && definition.Basketball is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Ramp && definition.Ramp is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Wall && definition.Wall is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Delay && definition.Delay is null) ||
            (definition.WorkshopKind == WorkshopPartKind.PinballBumper && definition.Bumper is null) ||
            definition.Parameters.Count != 0)
            throw new ArgumentException("Unsupported canonical part declaration.");
        if (definition.WorkshopKind == WorkshopPartKind.Basketball) definition.Basketball!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Ramp) definition.Ramp!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Wall) definition.Wall!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Delay) definition.Delay!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.PinballBumper) definition.Bumper!.Capture();
        Definition = definition;
        Name = definition.Id; // Godot resource/node-name boundary only.
    }
    public override void _Ready() => EnsureConstructed();
    internal void EnsureConstructed()
    {
        if (_built) return;
        if (Definition is null) throw new InvalidOperationException("Part requires an admitted definition.");
        Visual = new Node3D { Name = "Visual" };
        AddChild(Visual);
        Build();
        _highlight = PartArt.Ring(this, PickRadius, .025f, new("#efffbd"), new(0, 0, .02f));
        _highlight.RotationDegrees = new(90, 0, 0);
        _highlight.Visible = false;
        _built = true;
    }
    /// <summary>One binding list keyed by the part's declared feedback and an optional timer phase. Phase None follows the blend;
    /// a phase binding writes its value whenever the sampled phase matches. The shared worker owns every curve.</summary>
    private readonly List<(WorkshopVisualBinding Binding, AnimationTimerPhase Phase)> _bindings = new();
    private CosmeticCurveDeclaration _cosmetic;
    internal bool HasCosmeticBindings => _bindings.Count != 0;
    internal CosmeticCurveDeclaration Cosmetic => _cosmetic;
    protected void BindVisual(CosmeticCurveDeclaration declaration, Node3D target, WorkshopVisualProperty property, Half neutral, Half active) =>
        Bind(declaration, target, property, neutral, active, AnimationTimerPhase.None);
    protected void BindPhaseVisual(CosmeticCurveDeclaration declaration, Node3D target, WorkshopVisualProperty property, AnimationTimerPhase phase, Half value)
    {
        if (phase == AnimationTimerPhase.None) throw new ArgumentException("Phase bindings require a committed timer phase.");
        Bind(declaration, target, property, value, value, phase);
    }
    private void Bind(CosmeticCurveDeclaration declaration, Node3D target, WorkshopVisualProperty property, Half neutral, Half active, AnimationTimerPhase phase)
    {
        declaration.Validate();
        if (!declaration.IsDeclared) throw new ArgumentException("Visual bindings require a declared cosmetic curve.");
        if (_bindings.Count != 0 && _cosmetic != declaration) throw new ArgumentException("A part binds exactly one cosmetic declaration.");
        if (phase != AnimationTimerPhase.None && declaration.Source != AnimationFeedbackSource.Timer)
            throw new ArgumentException("Phase bindings require timer feedback.");
        _cosmetic = declaration;
        _bindings.Add((new(target, property, neutral, active), phase));
    }
    internal void ApplyCosmetic(WorkshopCosmeticSample sample)
    {
        // Game-grade: an out-of-range or non-finite blend keeps the last committed values.
        if (!Half.IsFinite(sample.Blend) || sample.Blend < (Half)0 || sample.Blend > (Half)1 || !Enum.IsDefined(sample.Phase)) return;
        foreach (var (binding, phase) in _bindings)
            if (phase == AnimationTimerPhase.None) binding.Apply(new(sample.Blend));
            else if (phase == sample.Phase) binding.Apply(new((Half)1));
    }
    protected virtual void Build() { }
    public void SetSelected(bool selected)
    {
        if (_highlight is not null) _highlight.Visible = selected;
    }
}
