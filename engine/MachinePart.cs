using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions;

/// <summary>Render/input node only. Durable construction and motion are canonical worker values.</summary>
public enum PhysicalVisualSlot : byte { Primary, Secondary }

public sealed partial class MachinePart : Node3D, IResizablePart
{
    public PartDefinition Definition { get; private set; } = null!;
    public PartId PartId => Definition?.PartId ?? default;
    public string Uid => Name;
    public GpuBodyId AuthoredId { get; internal set; }
    public bool Locked { get; internal set; }
    internal Node3D Visual { get; private set; } = null!;
    private float _pickRadius;
    internal float PickRadius
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
    public Vector3 Dimensions { get; private set; } = Vector3.One;
    public ResizeAxes ResizableAxes => Definition is null ? ResizeAxes.None :
        (Definition.Presentation.ResizeAxes.HasFlag(PresentationAxes.X) ? ResizeAxes.X : ResizeAxes.None) |
        (Definition.Presentation.ResizeAxes.HasFlag(PresentationAxes.Y) ? ResizeAxes.Y : ResizeAxes.None) |
        (Definition.Presentation.ResizeAxes.HasFlag(PresentationAxes.Z) ? ResizeAxes.Z : ResizeAxes.None);
    public void SetDimensions(Vector3 dimensions)
    {
        if (!dimensions.IsFinite()) throw new ArgumentException("Nonfinite presentation dimensions.");
        var sizing = Definition.Presentation.Sizing ?? throw new ArgumentException("This package declares no resizing.");
        var values = Definition.Parameters.ToDictionary(p => p.Key, p => p.Default, StringComparer.Ordinal);
        foreach (var parameter in sizing.Parameters)
        {
            var descriptor = Definition.Parameters.Single(p => p.Key == parameter.Parameter);
            var input = parameter.Axis switch
            {
                PresentationAxes.X => dimensions.X, PresentationAxes.Y => dimensions.Y, PresentationAxes.Z => dimensions.Z,
                _ => throw new ArgumentException("A resize parameter must select one axis.")
            };
            values[parameter.Parameter] = Math.Clamp(input, descriptor.Minimum, descriptor.Maximum);
        }
        ApplyPresentationParameters(values);
    }
    internal void ApplyPresentationParameters(IReadOnlyDictionary<string, float> values)
    {
        foreach (var parameter in Definition.Parameters)
            if (!values.TryGetValue(parameter.Key, out var input) || !float.IsFinite(input) || input < parameter.Minimum || input > parameter.Maximum)
                throw new ArgumentException("Missing or invalid declared presentation parameter.");
        if (values.Keys.Any(key => !Definition.Parameters.Any(p => p.Key == key)))
            throw new ArgumentException("Undeclared presentation parameter.");
        var presentation = Definition.Presentation;
        var dimensions = presentation.Sizing is { } sizing
            ? new Vector3(sizing.Dimensions.X, sizing.Dimensions.Y, sizing.Dimensions.Z) : Dimensions;
        if (presentation.Sizing is { } size)
            foreach (var parameter in size.Parameters)
            {
                var value = values[parameter.Parameter];
                switch (parameter.Axis)
                {
                    case PresentationAxes.X: dimensions.X = value; break;
                    case PresentationAxes.Y: dimensions.Y = value; break;
                    case PresentationAxes.Z: dimensions.Z = value; break;
                    default: throw new ArgumentException("Undefined resize axis.");
                }
            }
        var staged = new List<(WorkshopVisualBinding Binding, AnimationValue Value)>();
        foreach (var parameter in presentation.Parameters)
        {
            var target = GetNodeOrNull<Node3D>(parameter.Node) ?? throw new ArgumentException("Parameter target is absent.");
            var value = float.FusedMultiplyAdd(parameter.Multiplier, values[parameter.Parameter], parameter.Offset);
            staged.Add((new(target, VisualProperty(parameter.Property), value, value), WorkshopDeclaredPresentation.Value(parameter.Property, value)));
        }
        var nextRadius = PickRadius;
        if (presentation.Sizing is { } radius)
        {
            var vector = new Vector3(radius.RadiusScale.X, radius.RadiusScale.Y, radius.RadiusScale.Z) * dimensions +
                new Vector3(radius.RadiusOffset.X, radius.RadiusOffset.Y, radius.RadiusOffset.Z);
            nextRadius = vector.Length();
            if (!float.IsFinite(nextRadius) || nextRadius <= 0) throw new ArgumentException("Invalid declared pick radius.");
        }
        foreach (var item in staged) item.Binding.ApplyValue(item.Value);
        Dimensions = dimensions;
        if (presentation.Sizing is not null) PickRadius = nextRadius;
    }

    public void Configure(PartDefinition definition)
    {
        if (_built || Definition is not null) throw new InvalidOperationException("Part is already configured.");
        Definition = definition;
        Name = definition.Id; // Godot resource/node-name boundary only.
    }
    public override void _Ready() => EnsureConstructed();
    internal void EnsureConstructed()
    {
        if (_built) return;
        if (Definition is null) throw new InvalidOperationException("Part requires an admitted definition.");
        Visual = GetNodeOrNull<Node3D>("Visual") ?? new Node3D { Name = "Visual" };
        if (Visual.GetParent() == null) AddChild(Visual);
        BuildDeclarative();
        _highlight = WorkshopArtwork.Ring(this, PickRadius, .025f, new("#efffbd"), new(0, 0, .02f));
        _highlight.RotationDegrees = new(90, 0, 0);
        _highlight.Visible = false;
        _built = true;
    }
    private readonly Dictionary<PhysicalVisualSlot, (GpuBodyId Id, Node3D Target, Transform3D RestPose)> _physicalBodies = new();
    private readonly List<(Node3D Moving, Vector3 MovingAnchor, Node3D Target, Vector3 FixedAnchor, float RestLength)> _physicalSpans = new();
    /// <summary>Each bound visual's scene rest pose (after its declared rest offset): the frame a slider compression displaces.</summary>
    private readonly Dictionary<PhysicalVisualSlot, Transform3D> _sceneRestPoses = new();
    private void BindPhysicalBody(PhysicalVisualSlot slot, Node3D target)
    {
        if (slot != PhysicalVisualSlot.Secondary || !IsAncestorOf(target) ||
            !_physicalBodies.TryAdd(slot, (default, target, target.Transform)))
            throw new ArgumentException("Invalid owned physical visual binding.");
        _sceneRestPoses.Add(slot, target.Transform);
    }
    /// <summary>The slider body's visual: its admitted identity, resting at its scene pose displaced along the owner's local Y by the
    /// declared starting compression (the compiled moving body's pose; never compounded across edits).</summary>
    internal void InstallSliderPresentation(GpuBodyId id, float compression)
    {
        if (!float.IsFinite(compression)) throw new ArgumentException("Slider compression must be finite.");
        InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, id);
        var scene = _sceneRestPoses[PhysicalVisualSlot.Secondary];
        SetPhysicalRestPose(PhysicalVisualSlot.Secondary, new Transform3D(scene.Basis, scene.Origin - Vector3.Up * compression));
    }
    private void BindPhysicalSpan(Node3D moving, Vector3 movingAnchor, Node3D target, Vector3 fixedAnchor, float restLength)
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
    private void SetPhysicalRestPose(PhysicalVisualSlot slot, Transform3D pose)
    {
        if (!_physicalBodies.TryGetValue(slot, out var binding))
            throw new ArgumentException("Absent physical visual binding.");
        _physicalBodies[slot] = (binding.Id, binding.Target, pose);
        binding.Target.Transform = pose;
        ApplyPhysicalSpans();
    }
    internal void ResetPhysicalVisuals()
    {
        foreach (var binding in _physicalBodies.Values) binding.Target.Transform = binding.RestPose;
        ApplyPhysicalSpans();
        foreach (var pair in _declaredBindings)
        {
            var declaration = Definition.Presentation.Bindings[pair.Key];
            pair.Value.ApplyValue(WorkshopDeclaredPresentation.Value(declaration.Property,
                declaration.Curve == PresentationCurve.ExponentialFollow ? 0 : declaration.From));
        }
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
    private readonly Dictionary<int, WorkshopVisualBinding> _declaredBindings = new();
    internal void ApplyDeclaredProperty(int index, AnimationValue value)
    {
        if (!_declaredBindings.TryGetValue(index, out var binding))
            throw new ArgumentException("Unowned declared property.");
        binding.ApplyValue(value);
    }
    private void BuildDeclarative()
    {
        var presentation = Definition.Presentation;
        ApplyPresentationParameters(Definition.Parameters.ToDictionary(p => p.Key, p => p.Default, StringComparer.Ordinal));
        if (presentation.Sizing is null) PickRadius = presentation.PickRadius;
        var ownedMaterials = new HashSet<Node3D>();
        for (var i = 0; i < presentation.Bindings.Count; i++)
        {
            var declaration = presentation.Bindings[i];
            var target = GetNodeOrNull<Node3D>(declaration.Node) ?? throw new ArgumentException("Declared visual target is absent.");
            var property = VisualProperty(declaration.Property);
            if (property is >= WorkshopVisualProperty.AlbedoRed and <= WorkshopVisualProperty.EmissionBlue && ownedMaterials.Add(target))
            {
                if (target is not MeshInstance3D mesh || mesh.GetActiveMaterial(0) is not StandardMaterial3D ownedMaterial)
                    throw new ArgumentException("Declared colour target lacks its material.");
                var copy = (StandardMaterial3D)ownedMaterial.Duplicate();
                foreach (var channel in presentation.Bindings)
                    if (channel.Node == declaration.Node && channel.Property is PresentationProperty.EmissionRed or PresentationProperty.EmissionGreen or PresentationProperty.EmissionBlue)
                        copy.EmissionEnabled = true;
                mesh.MaterialOverride = copy;
            }
            // Every binding, one-shot cues included (Story 9A.25b), is applied from the Animation worker's declared samples.
            var binding = new WorkshopVisualBinding(target, property, declaration.From, declaration.To);
            _declaredBindings.Add(i, binding);
            binding.ApplyValue(WorkshopDeclaredPresentation.Value(declaration.Property,
                declaration.Curve == PresentationCurve.ExponentialFollow ? 0 : declaration.From));
        }
        foreach (var binding in presentation.Bodies)
        {
            var target = GetNodeOrNull<Node3D>(binding.Node) ?? throw new ArgumentException("Physical visual target is absent.");
            var slot = binding.Body switch
            {
                PresentationBody.Secondary => PhysicalVisualSlot.Secondary,
                _ => throw new ArgumentException("Only secondary bodies need a child visual binding.")
            };
            target.Position += new Vector3(binding.RestOffset.X, binding.RestOffset.Y, binding.RestOffset.Z);
            BindPhysicalBody(slot, target);
        }
        foreach (var binding in presentation.Spans)
            BindPhysicalSpan(GetNode<Node3D>(binding.MovingNode),
                new(binding.MovingAnchor.X, binding.MovingAnchor.Y, binding.MovingAnchor.Z),
                GetNode<Node3D>(binding.SpanNode), new(binding.FixedAnchor.X, binding.FixedAnchor.Y, binding.FixedAnchor.Z), binding.RestLength);
    }

    private static WorkshopVisualProperty VisualProperty(PresentationProperty property) => property switch
    {
        PresentationProperty.Visible => WorkshopVisualProperty.Visible,
        PresentationProperty.LocalX => WorkshopVisualProperty.LocalX,
        PresentationProperty.LocalY => WorkshopVisualProperty.LocalY,
        PresentationProperty.LocalZ => WorkshopVisualProperty.LocalZ,
        PresentationProperty.LocalRotationZ => WorkshopVisualProperty.LocalRotationZ,
        PresentationProperty.UniformScale => WorkshopVisualProperty.UniformScale,
        PresentationProperty.ScaleX => WorkshopVisualProperty.ScaleX,
        PresentationProperty.ScaleY => WorkshopVisualProperty.ScaleY,
        PresentationProperty.ScaleZ => WorkshopVisualProperty.ScaleZ,
        PresentationProperty.AlbedoRed => WorkshopVisualProperty.AlbedoRed,
        PresentationProperty.AlbedoGreen => WorkshopVisualProperty.AlbedoGreen,
        PresentationProperty.AlbedoBlue => WorkshopVisualProperty.AlbedoBlue,
        PresentationProperty.EmissionRed => WorkshopVisualProperty.EmissionRed,
        PresentationProperty.EmissionGreen => WorkshopVisualProperty.EmissionGreen,
        PresentationProperty.EmissionBlue => WorkshopVisualProperty.EmissionBlue,
        _ => throw new ArgumentException("Undefined presentation property.")
    };

    public void SetSelected(bool selected)
    {
        if (_highlight is not null) _highlight.Visible = selected;
    }
}
