using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Render/input node only. Durable construction and motion are canonical worker values.</summary>
public enum PhysicalVisualSlot : byte { Primary, Secondary }

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
        if (definition.WorkshopKind is not (WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall or WorkshopPartKind.Receiver or WorkshopPartKind.Ramp or WorkshopPartKind.ImpactSwitch or WorkshopPartKind.SignalLamp or WorkshopPartKind.Wall or WorkshopPartKind.Delay or WorkshopPartKind.PinballBumper or WorkshopPartKind.Domino or WorkshopPartKind.Battery or WorkshopPartKind.Springboard) ||
            (definition.WorkshopKind is WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall && definition.Ball is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Ramp && definition.Ramp is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Wall && definition.Wall is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Delay && definition.Delay is null) ||
            (definition.WorkshopKind == WorkshopPartKind.PinballBumper && definition.ContactWork is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Battery && definition.ElectricalSource is null) ||
            (definition.WorkshopKind == WorkshopPartKind.Springboard && definition.Springboard is null) ||
            definition.Parameters.Count != 0)
            throw new ArgumentException("Unsupported canonical part declaration.");
        if (definition.WorkshopKind is WorkshopPartKind.Basketball or WorkshopPartKind.BowlingBall) definition.Ball!.Capture(definition.WorkshopKind);
        if (definition.WorkshopKind == WorkshopPartKind.Ramp) definition.Ramp!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Wall) definition.Wall!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Battery) definition.ElectricalSource!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.Delay) definition.Delay!.Capture();
        if (definition.WorkshopKind == WorkshopPartKind.PinballBumper) BumperWork.FromCalibration(definition.ContactWork!.Capture());
        if (definition.WorkshopKind == WorkshopPartKind.Springboard) definition.Springboard!.Capture();
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
    private readonly List<(ElectricalIndicator Indicator, WorkshopVisualBinding Binding)> _electricalBindings = new();
    internal bool HasElectricalBindings => _electricalBindings.Count != 0;
    protected void BindElectricalIndicator(Node3D target, ElectricalIndicator indicator)
    {
        if (!Enum.IsDefined(indicator)) throw new ArgumentException("Unknown electrical indicator binding.");
        _electricalBindings.Add((indicator, new(target, WorkshopVisualProperty.AlbedoRed, (Half)(85f / 255f), (Half)(247f / 255f))));
        _electricalBindings.Add((indicator, new(target, WorkshopVisualProperty.AlbedoGreen, (Half)(101f / 255f), (Half)(203f / 255f))));
        _electricalBindings.Add((indicator, new(target, WorkshopVisualProperty.AlbedoBlue, (Half)(115f / 255f), (Half)(82f / 255f))));
    }
    internal void ApplyElectrical(ElectricalIndicatorSample sample)
    {
        foreach (var (indicator, binding) in _electricalBindings) binding.Apply(new((Half)sample[indicator]));
    }
    private readonly Dictionary<PhysicalVisualSlot, (GpuBodyId Id, Node3D Target, Transform3D RestPose)> _physicalBodies = new();
    private readonly List<(Node3D Moving, Vector3 MovingAnchor, Node3D Target, Vector3 FixedAnchor, float RestLength)> _physicalSpans = new();
    protected void BindPhysicalBody(PhysicalVisualSlot slot, Node3D target)
    {
        if (slot != PhysicalVisualSlot.Secondary || !IsAncestorOf(target) ||
            !_physicalBodies.TryAdd(slot, (default, target, target.Transform)))
            throw new ArgumentException("Invalid owned physical visual binding.");
    }
    protected void BindPhysicalSpan(Node3D moving, Vector3 movingAnchor, Node3D target, Vector3 fixedAnchor, float restLength)
    {
        if (!IsAncestorOf(moving) || !IsAncestorOf(target) || moving.GetParent() != target.GetParent() ||
            !float.IsFinite(restLength) || restLength <= 0)
            throw new ArgumentException("Invalid physical span binding.");
        _physicalSpans.Add((moving, movingAnchor, target, fixedAnchor, restLength));
        ApplyPhysicalSpans();
    }
    internal void InstallPhysicalIdentity(PhysicalVisualSlot slot, GpuBodyId id)
    {
        if (AuthoredId.Value == 0 || !WorkshopPhysicsCompiler.IsSecondaryBodyIdentity(AuthoredId, id) ||
            !_physicalBodies.TryGetValue(slot, out var binding))
            throw new ArgumentException("Invalid admitted physical visual identity.");
        _physicalBodies[slot] = (id, binding.Target, binding.RestPose);
    }
    internal Node3D? PhysicalVisual(GpuBodyId id)
    {
        if (id.Value == 0) return null;
        if (id == AuthoredId) return this;
        foreach (var binding in _physicalBodies.Values) if (id == binding.Id) return binding.Target;
        return null;
    }
    internal void ResetPhysicalVisuals()
    {
        foreach (var binding in _physicalBodies.Values) binding.Target.Transform = binding.RestPose;
        ApplyPhysicalSpans();
    }
    internal void ApplyPhysicalSpans()
    {
        foreach (var binding in _physicalSpans)
        {
            var end = binding.Moving.Transform * binding.MovingAnchor;
            var delta = end - binding.FixedAnchor;
            var length = delta.Length();
            if (!float.IsFinite(length) || length <= 0) continue;
            binding.Target.Position = (end + binding.FixedAnchor) * .5f;
            binding.Target.Quaternion = new Quaternion(Vector3.Up, delta / length);
            binding.Target.Scale = new(1, length / binding.RestLength, 1);
        }
    }
    protected virtual void Build() { }
    public void SetSelected(bool selected)
    {
        if (_highlight is not null) _highlight.Visible = selected;
    }
}
