using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Render/input node only. Durable construction and motion are canonical worker values.</summary>
public enum PhysicalVisualSlot : byte { Primary, Secondary }

public partial class MachinePart : Node3D, IResizablePart
{
    public PartDefinition Definition { get; private set; } = null!;
    public PartId PartId => Definition?.PartId ?? default;
    public string Uid => Name;
    public GpuBodyId AuthoredId { get; internal set; }
    public bool Locked { get; internal set; }
    protected internal Node3D Visual { get; private set; } = null!;
    private float _pickRadius;
    protected internal float PickRadius
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
    public WallDimensions? WallDimensions { get; private set; }
    public RampDimensions? RampDimensions { get; private set; }
    public Vector3 Dimensions { get; private set; } = Vector3.One;
    public ResizeAxes ResizableAxes => Definition?.PartId.Value switch
    {
        "wall" => ResizeAxes.All,
        "ramp" => ResizeAxes.X | ResizeAxes.Z,
        _ => ResizeAxes.None
    };
    public void SetDimensions(Vector3 dimensions)
    {
        if (Definition?.PartId == PartId.Wall)
            ApplyWallDimensions(Gpu.WallDimensions.FromInput(dimensions.X, dimensions.Y, dimensions.Z));
        else if (Definition?.PartId == PartId.Ramp)
        {
            if (!dimensions.IsFinite()) throw new ArgumentException("Nonfinite ramp dimensions.");
            ApplyRampDimensions(new(new((Half)dimensions.X), new((Half)dimensions.Z)));
        }
    }
    public void ApplyWallDimensions(WallDimensions dimensions)
    {
        dimensions.Validate();
        WallDimensions = dimensions;
        Dimensions = new((float)dimensions.Width.Value, (float)dimensions.Height.Value, (float)dimensions.Thickness.Value);
        if (Visual is not null) Visual.Scale = Dimensions;
        PickRadius = Dimensions.Length() * .5f;
    }
    public void ApplyRampDimensions(RampDimensions dimensions)
    {
        dimensions.Validate();
        RampDimensions = dimensions;
        Dimensions = new((float)dimensions.Length.Value, (float)Gpu.RampDimensions.Thickness.Value, (float)dimensions.Width.Value);
        var origLen = Definition?.Colliders.Count > 0 ? (float)Definition.Colliders[0].HalfExtents.X * 2f : 3f;
        var origWidth = Definition?.Colliders.Count > 0 ? (float)Definition.Colliders[0].HalfExtents.Z * 2f : 1.3f;
        if (Visual is not null)
            Visual.Scale = new((float)dimensions.Length.Value / origLen, 1, (float)dimensions.Width.Value / origWidth);
        PickRadius = (float)dimensions.Length.Value * .5f;
    }
    public DelayDuration CanonicalDuration { get; private set; } = DelayDuration.Default;
    public void ApplyDuration(DelayDuration duration) { duration.Validate(); CanonicalDuration = duration; }

    public ElectricalSourceSettings CanonicalSource { get; private set; } = ElectricalSourceSettings.Default;
    public void ApplySource(ElectricalSourceSettings source) { source.Validate(); CanonicalSource = source; }

    public SpringboardSettings CanonicalSpring { get; private set; } = SpringboardSettings.Default;
    private bool _physicalReady;
    public void ApplySpring(SpringboardSettings value)
    {
        value.Validate();
        CanonicalSpring = value;
        if (_physicalReady)
            SetPhysicalRestPose(PhysicalVisualSlot.Secondary,
                new Transform3D(Basis.Identity, new Vector3(0, .14f - value.InitialCompression, 0)));
    }

    public BumperWork CanonicalWork { get; private set; } = BumperWork.Default;
    public void ApplyWork(BumperWork work) { work.Validate(); CanonicalWork = work; }

    public float CanonicalIntervalSeconds { get; private set; } = WorkshopClock.DefaultIntervalSeconds;
    public void ApplyIntervalSeconds(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < WorkshopClock.MinIntervalSeconds || seconds > WorkshopClock.MaxIntervalSeconds)
            throw new ArgumentException($"Clock interval seconds '{seconds}' is outside admitted range.");
        CanonicalIntervalSeconds = seconds;
    }

    public void Configure(PartDefinition definition)
    {
        if (_built || Definition is not null) throw new InvalidOperationException("Part is already configured.");
        if (definition.Parameters.Count != 0 && definition.PartId != PartId.Clock)
            throw new ArgumentException("Unsupported canonical part declaration.");
        var id = definition.PartId;
        if (id == PartId.Clock && definition.Parameters.TryGetValue("interval_seconds", out var intervalSec))
        {
            ApplyIntervalSeconds(intervalSec);
        }
        if (id == PartId.Basketball || id == PartId.BowlingBall)
        {
            if (definition.Ball is null) throw new ArgumentException("Canonical ball material resource is required.");
            definition.Ball.Capture(id);
        }
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
    protected void SetPhysicalRestPose(PhysicalVisualSlot slot, Transform3D pose)
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
        if (Definition?.PartId == PartId.Counter) UpdateCounterLamps(0);
        if (Definition?.PartId == PartId.Clock) UpdateClockLamp(false);
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
    private void BuildDeclarative()
    {
        if (Definition?.Ball is not null && (Definition.PartId == PartId.Basketball || Definition.PartId == PartId.BowlingBall))
        {
            var material = Definition.Ball.Capture(Definition.PartId);
            var radius = (float)material.Radius.Value;
            PickRadius = radius + .12f;
            foreach (var child in Visual.GetChildren())
            {
                if (child is MeshInstance3D meshInstance && meshInstance.Mesh is SphereMesh sphereMesh)
                {
                    sphereMesh.Radius = radius;
                    sphereMesh.Height = radius * 2f;
                }
            }
        }
        else if (Definition?.PartId == PartId.Receiver)
        {
            PickRadius = .85f;
            var halo = FindChild("Visual_Halo", recursive: true, owned: false) as Node3D;
            if (halo is not null)
            {
                halo.Scale = Vector3.One * (1 + (float)ReceiverCaptureSettings.Free.Margin.Value);
                var neutral = new Color("#bdf4bd"); var captured = Colors.White;
                if (halo is MeshInstance3D meshHalo)
                {
                    if (meshHalo.MaterialOverride is not StandardMaterial3D mat)
                        meshHalo.MaterialOverride = new StandardMaterial3D { AlbedoColor = neutral };
                    else
                        meshHalo.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                }
                var curve = CosmeticCurves.Receiver;
                BindVisual(curve, halo, WorkshopVisualProperty.AlbedoRed, (Half)neutral.R, (Half)captured.R);
                BindVisual(curve, halo, WorkshopVisualProperty.AlbedoGreen, (Half)neutral.G, (Half)captured.G);
                BindVisual(curve, halo, WorkshopVisualProperty.AlbedoBlue, (Half)neutral.B, (Half)captured.B);
            }
        }
        else if (Definition?.PartId == PartId.Wall)
            ApplyWallDimensions(Gpu.WallDimensions.Default);
        else if (Definition?.PartId == PartId.Ramp)
            ApplyRampDimensions(Gpu.RampDimensions.Default);
        else if (Definition?.PartId == PartId.PinballBumper)
        {
            PickRadius = .8f;
            var head = FindChild("BumperHead", recursive: true, owned: false) as Node3D;
            var pulseRing = FindChild("ImpactRing", recursive: true, owned: false) as Node3D;
            if (head is not null && pulseRing is not null)
            {
                if (pulseRing is MeshInstance3D ringMesh)
                {
                    if (ringMesh.MaterialOverride is StandardMaterial3D mat)
                        ringMesh.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        ringMesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#fff0c2") };
                }
                var curve = CosmeticCurves.PinballBumper;
                BindVisual(curve, head, WorkshopVisualProperty.UniformScale, (Half)1, (Half)0.88);
                BindVisual(curve, pulseRing, WorkshopVisualProperty.UniformScale, (Half)1, (Half)1.24);
                BindVisual(curve, pulseRing, WorkshopVisualProperty.AlbedoRed, (Half)1, (Half)(247d / 255));
                BindVisual(curve, pulseRing, WorkshopVisualProperty.AlbedoGreen, (Half)(240d / 255), (Half)(203d / 255));
                BindVisual(curve, pulseRing, WorkshopVisualProperty.AlbedoBlue, (Half)(194d / 255), (Half)(82d / 255));
            }
        }
        else if (Definition?.PartId == PartId.Domino)
        {
            PickRadius = .6f;
        }
        else if (Definition?.PartId == PartId.Springboard)
        {
            PickRadius = .9f;
            var plate = FindChild("Plate", recursive: true, owned: false) as Node3D;
            var coil = FindChild("Coil", recursive: true, owned: false) as Node3D;
            if (plate is not null && coil is not null)
            {
                BindPhysicalBody(PhysicalVisualSlot.Secondary, plate);
                BindPhysicalSpan(plate, new(0, -.075f, 0), coil, new(0, -.3f, 0), .365f);
                _physicalReady = true;
                if (CanonicalSpring.InitialCompression != 0)
                    SetPhysicalRestPose(PhysicalVisualSlot.Secondary,
                        new Transform3D(Basis.Identity, new Vector3(0, .14f - CanonicalSpring.InitialCompression, 0)));
            }
        }
        else if (Definition?.PartId == PartId.ImpactSwitch)
        {
            PickRadius = .65f;
            var button = FindChild("Button", recursive: true, owned: false) as Node3D;
            if (button is not null)
            {
                if (button is MeshInstance3D meshBtn)
                {
                    if (meshBtn.MaterialOverride is StandardMaterial3D mat)
                        meshBtn.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        meshBtn.MaterialOverride = new StandardMaterial3D { AlbedoColor = Definition.Color };
                }
                var pressed = new Color("#bff5b0");
                var curve = CosmeticCurves.ImpactSwitch;
                BindVisual(curve, button, WorkshopVisualProperty.LocalY, (Half).06, (Half)(-.02));
                BindVisual(curve, button, WorkshopVisualProperty.AlbedoRed, (Half)Definition.Color.R, (Half)pressed.R);
                BindVisual(curve, button, WorkshopVisualProperty.AlbedoGreen, (Half)Definition.Color.G, (Half)pressed.G);
                BindVisual(curve, button, WorkshopVisualProperty.AlbedoBlue, (Half)Definition.Color.B, (Half)pressed.B);
            }
        }
        else if (Definition?.PartId == PartId.SignalLamp)
        {
            PickRadius = .65f;
            var bulb = FindChild("Bulb", recursive: true, owned: false) as Node3D;
            if (bulb is not null)
            {
                var neutral = new Color("#556573"); var lit = new Color("#fff0a5"); var emission = new Color("#e9b24c");
                if (bulb is MeshInstance3D meshBulb)
                {
                    if (meshBulb.MaterialOverride is StandardMaterial3D mat)
                    {
                        var copy = (StandardMaterial3D)mat.Duplicate();
                        copy.EmissionEnabled = true;
                        copy.Emission = Colors.Black;
                        meshBulb.MaterialOverride = copy;
                    }
                    else
                    {
                        meshBulb.MaterialOverride = new StandardMaterial3D { AlbedoColor = neutral, EmissionEnabled = true, Emission = Colors.Black };
                    }
                }
                var curve = CosmeticCurves.SignalLamp;
                BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoRed, (Half)neutral.R, (Half)lit.R);
                BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoGreen, (Half)neutral.G, (Half)lit.G);
                BindVisual(curve, bulb, WorkshopVisualProperty.AlbedoBlue, (Half)neutral.B, (Half)lit.B);
                BindVisual(curve, bulb, WorkshopVisualProperty.EmissionRed, (Half)0, (Half)emission.R);
                BindVisual(curve, bulb, WorkshopVisualProperty.EmissionGreen, (Half)0, (Half)emission.G);
                BindVisual(curve, bulb, WorkshopVisualProperty.EmissionBlue, (Half)0, (Half)emission.B);
            }
        }
        else if (Definition?.PartId == PartId.Delay)
        {
            PickRadius = .9f;
            var hand = FindChild("CountdownHand", recursive: true, owned: false) as Node3D;
            var indicator = FindChild("Indicator", recursive: true, owned: false) as Node3D;
            if (indicator is MeshInstance3D meshInd)
            {
                if (meshInd.MaterialOverride is StandardMaterial3D mat)
                    meshInd.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                else
                    meshInd.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
            }
            if (hand is not null && indicator is not null)
            {
                var curve = CosmeticCurves.Delay;
                BindVisual(curve, hand, WorkshopVisualProperty.LocalRotationZ, (Half)0, (Half)(-Math.Tau));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoRed, AnimationTimerPhase.Ready, (Half)(85d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoGreen, AnimationTimerPhase.Ready, (Half)(101d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoBlue, AnimationTimerPhase.Ready, (Half)(115d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoRed, AnimationTimerPhase.Counting, (Half)(232d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoGreen, AnimationTimerPhase.Counting, (Half)(183d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoBlue, AnimationTimerPhase.Counting, (Half)(100d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoRed, AnimationTimerPhase.Finished, (Half)(247d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoGreen, AnimationTimerPhase.Finished, (Half)(203d / 255));
                BindPhaseVisual(curve, indicator, WorkshopVisualProperty.AlbedoBlue, AnimationTimerPhase.Finished, (Half)(82d / 255));
            }
        }
        else if (Definition?.PartId == PartId.Battery)
        {
            PickRadius = .8f;
            var terminal = FindChild("Terminal", recursive: true, owned: false) as Node3D;
            if (terminal is not null)
            {
                if (terminal is MeshInstance3D meshTerm)
                {
                    if (meshTerm.MaterialOverride is StandardMaterial3D mat)
                        meshTerm.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        meshTerm.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
                }
                BindElectricalIndicator(terminal, ElectricalIndicator.Supply);
            }
            for (var i = 0; i < 4; i++)
            {
                var mark = FindChild($"Mark_{i}", recursive: true, owned: false) as Node3D;
                if (mark is not null)
                {
                    if (mark is MeshInstance3D meshMark)
                    {
                        if (meshMark.MaterialOverride is StandardMaterial3D mat)
                            meshMark.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                        else
                            meshMark.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
                    }
                    BindElectricalIndicator(mark, (ElectricalIndicator)(i + 1));
                }
            }
        }
        else if (Definition?.PartId == PartId.Pipe)
        {
            PickRadius = 2.05f;
        }
        else if (Definition?.PartId == PartId.PipeBend45 || Definition?.PartId == PartId.PipeBend90)
        {
            PickRadius = 2.2f;
        }
        else if (Definition?.PartId == PartId.Funnel)
        {
            PickRadius = 1.75f;
        }
        else if (Definition?.PartId == PartId.BallDetector)
        {
            PickRadius = 1.15f;
            var indicator = FindChild("Visual_Indicator", recursive: true, owned: false) as Node3D;
            if (indicator is not null)
            {
                var neutral = new Color("#556573");
                var active = new Color("#f7cb52");
                if (indicator is MeshInstance3D meshInd)
                {
                    if (meshInd.MaterialOverride is StandardMaterial3D mat)
                        meshInd.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        meshInd.MaterialOverride = new StandardMaterial3D { AlbedoColor = neutral };
                }
                var curve = CosmeticCurves.BallDetector;
                BindVisual(curve, indicator, WorkshopVisualProperty.AlbedoRed, (Half)neutral.R, (Half)active.R);
                BindVisual(curve, indicator, WorkshopVisualProperty.AlbedoGreen, (Half)neutral.G, (Half)active.G);
                BindVisual(curve, indicator, WorkshopVisualProperty.AlbedoBlue, (Half)neutral.B, (Half)active.B);
            }
        }
        else if (Definition?.PartId == PartId.PoweredGate)
        {
            PickRadius = 1.5f;
            var blade = FindChild("Blade", recursive: true, owned: false) as Node3D;
            if (blade is not null)
            {
                BindPhysicalBody(PhysicalVisualSlot.Secondary, blade);
            }
        }
        else if (Definition?.PartId == PartId.PressurePlate)
        {
            PickRadius = 1.5f;
            for (var i = 0; i < 4; i++)
            {
                var stud = FindChild($"Stud_{i}", recursive: true, owned: false) as Node3D;
                if (stud is MeshInstance3D meshStud)
                {
                    if (meshStud.MaterialOverride is StandardMaterial3D mat)
                        meshStud.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        meshStud.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
                }
            }
        }
        else if (Definition?.PartId == PartId.Counter)
        {
            PickRadius = 1.2f;
            for (var i = 0; i < 9; i++)
            {
                var dot = FindChild($"DotLamp_{i}", recursive: true, owned: false) as Node3D;
                if (dot is MeshInstance3D meshDot)
                {
                    if (meshDot.MaterialOverride is StandardMaterial3D mat)
                        meshDot.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                    else
                        meshDot.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
                }
            }
        }
        else if (Definition?.PartId == PartId.Clock)
        {
            PickRadius = 1.1f;
            var indicator = FindChild("Visual_Indicator", recursive: true, owned: false) as Node3D;
            if (indicator is MeshInstance3D meshInd)
            {
                if (meshInd.MaterialOverride is StandardMaterial3D mat)
                    meshInd.MaterialOverride = (StandardMaterial3D)mat.Duplicate();
                else
                    meshInd.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#556573") };
            }
        }
        else if (Definition?.PartId == PartId.Latch)
        {
            PickRadius = 1.2f;
            // Off: rocker at -0.25 rad and Slate lamp; On: +0.25 rad and Gold lamp. The blend follows committed latch memory.
            var curve = CosmeticCurves.StateLatch;
            var off = new Color("#556573"); var on = new Color("#f7cb52");
            if (FindChild("Rocker", recursive: true, owned: false) is Node3D rocker)
                BindVisual(curve, rocker, WorkshopVisualProperty.LocalRotationZ, (Half)(-0.25f), (Half)0.25f);
            if (FindChild("IndicatorLamp", recursive: true, owned: false) is MeshInstance3D lamp)
            {
                lamp.MaterialOverride = new StandardMaterial3D { AlbedoColor = off };
                BindVisual(curve, lamp, WorkshopVisualProperty.AlbedoRed, (Half)off.R, (Half)on.R);
                BindVisual(curve, lamp, WorkshopVisualProperty.AlbedoGreen, (Half)off.G, (Half)on.G);
                BindVisual(curve, lamp, WorkshopVisualProperty.AlbedoBlue, (Half)off.B, (Half)on.B);
            }
        }
        Build();
    }

    public void UpdateClockLamp(bool active)
    {
        if (Definition?.PartId != PartId.Clock) return;
        var indicator = FindChild("Visual_Indicator", recursive: true, owned: false) as MeshInstance3D;
        if (indicator?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = active ? new Color("#f7cb52") : new Color("#556573");
        }
    }

    public void UpdateCounterLamps(int count)
    {
        if (Definition?.PartId != PartId.Counter) return;
        var reachedColor = new Color("#f7cb52");
        var unreachedColor = new Color("#556573");
        for (var i = 0; i < 9; i++)
        {
            var dot = FindChild($"DotLamp_{i}", recursive: true, owned: false) as MeshInstance3D;
            if (dot?.MaterialOverride is StandardMaterial3D mat)
            {
                mat.AlbedoColor = i < count ? reachedColor : unreachedColor;
            }
        }
    }
    public void SetSelected(bool selected)
    {
        if (_highlight is not null) _highlight.Visible = selected;
    }

    public bool TryGetPortTransform(string portId, out Vector3 position, out Quaternion rotation)
    {
        if (Definition is not null)
        {
            if (Definition.PortLocators.TryGetValue(portId, out var locator) ||
                (string.Equals(portId, "PowerIn", StringComparison.OrdinalIgnoreCase) && Definition.PortLocators.TryGetValue("power_in", out locator)) ||
                (portId.EndsWith("In", StringComparison.OrdinalIgnoreCase) && Definition.PortLocators.TryGetValue("in", out locator)) ||
                (portId.EndsWith("Out", StringComparison.OrdinalIgnoreCase) && Definition.PortLocators.TryGetValue("out", out locator)) ||
                (string.Equals(portId, "Supply", StringComparison.OrdinalIgnoreCase) && Definition.PortLocators.TryGetValue("out", out locator)))
            {
                position = locator.Position;
                rotation = locator.Rotation;
                return true;
            }
        }
        var node = FindChild($"Loc_Port_{portId}", recursive: true, owned: false) as Node3D;
        if (node is not null)
        {
            var relative = GetRelativeTransform(node);
            position = relative.Origin;
            rotation = relative.Basis.GetRotationQuaternion().Normalized();
            return true;
        }
        position = default;
        rotation = Quaternion.Identity;
        return false;
    }

    public (Vector3 Position, Quaternion Rotation) GetPortTransform(string portId)
    {
        if (TryGetPortTransform(portId, out var pos, out var rot))
            return (pos, rot);
        throw new KeyNotFoundException($"Port locator '{portId}' was not found on part '{Name}'.");
    }

    private Transform3D GetRelativeTransform(Node3D target)
    {
        var t = target.Transform;
        var current = target.GetParent() as Node3D;
        while (current is not null && current != this)
        {
            t = current.Transform * t;
            current = current.GetParent() as Node3D;
        }
        return t;
    }
}
