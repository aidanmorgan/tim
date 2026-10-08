using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Presentation;

public enum SceneAnimationFeedback { Autonomous, OwnerActive, CounterThreshold, ElectricalInput, ScalarThreshold, BooleanObservation }
public enum SceneAnimationDrive { StartStop, Endpoint }
public enum AnimationDirection { Forward, Reverse }
public sealed record SceneScalarRotation(Node3D Target,SceneScalarObservationKey Source,ScalarUnit Unit,
    double InputFrom,double InputTo,double AngleFrom,double AngleTo,AnimationRotationAxis Axis);
public enum SceneColourFollowFeedback { OwnerActivity, BooleanObservation }
public readonly record struct SceneColourFollowSignal
{
    public SceneColourFollowFeedback Kind { get; }
    public SceneBooleanObservationKey? Observation { get; }
    private SceneColourFollowSignal(SceneColourFollowFeedback kind,SceneBooleanObservationKey? observation)
    {Kind=kind;Observation=observation;}
    public static SceneColourFollowSignal OwnerActive=>new(SceneColourFollowFeedback.OwnerActivity,null);
    public static SceneColourFollowSignal Boolean(SceneBooleanObservationKey observation)
    {
        if(observation.Owner is null)throw new ArgumentException("Boolean colour feedback requires an owner.");
        return new(SceneColourFollowFeedback.BooleanObservation,observation);
    }
}
public sealed record SceneColourFollow(MeshInstance3D Target,AnimationFollowDefinition Definition,Color From,Color To,SceneColourFollowSignal Signal);
public sealed record SceneSpectralColour(MeshInstance3D Target,SceneScalarObservationKey Red,
    SceneScalarObservationKey Green,SceneScalarObservationKey Blue,Color Inactive,double Response,AnimationClock Clock);
public sealed record SceneTimerColour(MeshInstance3D Target,SceneTimerKey Source,TimerColourPalette Palette);
public sealed record TimerColourPalette
{
    public Color Ready { get; }
    public Color Counting { get; }
    public Color Finished { get; }
    public TimerColourPalette(Color ready,Color counting,Color finished)
    {
        SceneAnimationAdapter.ValidateColour(ready);SceneAnimationAdapter.ValidateColour(counting);
        SceneAnimationAdapter.ValidateColour(finished);
        Ready=ready;Counting=counting;Finished=finished;
    }
    public Color Read(SimulationTimerPhase phase)=>phase switch
    {
        SimulationTimerPhase.Ready=>Ready,SimulationTimerPhase.Counting=>Counting,SimulationTimerPhase.Finished=>Finished,
        _=>throw new ArgumentOutOfRangeException(nameof(phase))
    };
}
public enum ScalarExtentVisibility { Always, OwnerActive }
public sealed record SceneScalarExtent(Node3D Target,SceneScalarObservationKey Source,ScalarUnit Unit,
    double InputFrom,double InputTo,ScalarExtentDefinition Definition,ScalarExtentVisibility Visibility);

/// <summary>Validated source declaration; scene keys resolve once at Run construction.</summary>
public readonly record struct SceneAnimationSignal
{
    public SceneAnimationFeedback Kind { get; }
    public SceneCounterKey? Counter { get; }
    public int Threshold { get; }
    public SocketId? Input { get; }
    public SceneScalarObservationKey? Scalar { get; private init; }
    public ScalarUnit Unit { get; private init; }
    public SceneBooleanObservationKey? Observation { get; private init; }
    public static SceneAnimationSignal Boolean(SceneBooleanObservationKey source)
    {
        if(source.Owner is null)throw new ArgumentException("Boolean feedback requires an owner.");
        return new(SceneAnimationFeedback.BooleanObservation,null,0,null){Observation=source};
    }
    public double ScalarThreshold { get; private init; }
    public static SceneAnimationSignal ScalarAtLeast(SceneScalarObservationKey key,ScalarUnit unit,double threshold)
    {
        if(key.Owner is null||!Enum.IsDefined(unit)||!double.IsFinite(threshold))
            throw new ArgumentException("Scalar feedback requires an owner, supported unit and finite threshold.");
        return new(SceneAnimationFeedback.ScalarThreshold,null,0,null){Scalar=key,Unit=unit,ScalarThreshold=threshold};
    }
    private SceneAnimationSignal(SceneAnimationFeedback kind,SceneCounterKey? counter,int threshold,SocketId? input)
    { Kind=kind;Counter=counter;Threshold=threshold;Input=input; }
    public static SceneAnimationSignal Autonomous=>new(SceneAnimationFeedback.Autonomous,null,0,null);
    public static SceneAnimationSignal OwnerActive=>new(SceneAnimationFeedback.OwnerActive,null,0,null);
    public static SceneAnimationSignal InputAvailable(SocketId input)
    {
        if(!Enum.IsDefined(input))throw new ArgumentOutOfRangeException(nameof(input));
        return new(SceneAnimationFeedback.ElectricalInput,null,0,input);
    }
    public static SceneAnimationSignal CounterAtLeast(SceneCounterKey counter,int threshold)
    {
        if(counter.Owner is null||counter.Slot is null||threshold<1)
            throw new ArgumentException("Counter feedback requires an owner, slot and positive threshold.");
        return new(SceneAnimationFeedback.CounterThreshold,counter,threshold,null);
    }
}

/// <summary>Cosmetic child only: never functional contact or interaction geometry.</summary>
public sealed record SceneRotationAnimation(Node3D Target, AnimationDefinition Definition,
    AnimationRotationAxis Axis, SceneAnimationSignal Feedback, SceneAnimationDrive Drive);

/// <summary>Cosmetic scalar gauge with a saturating input range and exponential response.</summary>
public enum ScalarAnimationMapping { Linear, SineCycle }
public sealed record SceneScalarRotationAnimation(Node3D Target,SceneScalarObservationKey Source,ScalarUnit Unit,
    double InputFrom,double InputTo,AnimationFollowDefinition Definition,AnimationRotationAxis Axis,ScalarAnimationMapping Mapping);

public sealed record SceneTranslationAnimation(Node3D Target,AnimationDefinition Definition,
    AnimationTranslationAxis Axis,SceneAnimationSignal Feedback,SceneAnimationDrive Drive);

public sealed record SceneColourAnimation(MeshInstance3D Target, AnimationDefinition Definition,
    Color From, Color To, SceneAnimationSignal Feedback, SceneAnimationDrive Drive);

/// <summary>Cosmetic rotation driven by committed relative angular velocity.
/// SourceAxis is expressed in Reference's local frame; no physical angle feeds back.</summary>
public sealed record SceneAngularVelocityAnimation(Node3D Target, SceneBodyKey Body, SceneBodyKey Reference,
    AnimationRotationAxis SourceAxis, AnimationRotationAxis TargetAxis, AnimationDirection Direction, AnimationClock Clock);

/// <summary>Run-scoped scene host. Copies feedback at successful tick boundaries;
/// frame evaluation reads those values, never live simulation objects.</summary>
public sealed class SceneAnimationRun
{
    private readonly record struct TargetObjectId(ulong Value);
    private readonly record struct ScalarBinding(ScalarReadKey Key,ScalarUnit Unit,double Threshold);
    private readonly record struct CounterBinding(SimulationCounterId Id,int Target,int Threshold);
    private readonly record struct Binding(PhysicsBodyId Owner, Node3D VisibilityTarget,
        AnimationDefinition Definition, SceneAnimationFeedback Feedback, SceneAnimationDrive Drive,
        SceneAnimationTargetHandle Target, AnimationHandle Animation, StandardMaterial3D? Material,
        CounterBinding? Counter,ElectricalInputKey? Input,ScalarBinding? Scalar,BooleanReadKey? Boolean);
    private readonly record struct RateBinding(PhysicsBodyId Owner, PhysicsBodyId Body, PhysicsBodyId Reference,
        Node3D VisibilityTarget, CollisionVector SourceAxis, AnimationDirection Direction, AnimationClock Clock,
        SceneAnimationTargetHandle Target, AnimationHandle Animation);
    private readonly record struct FollowBinding(ScalarReadKey Key,ScalarUnit Unit,double InputFrom,double InputTo,
        AnimationFollowDefinition Definition,ScalarAnimationMapping Mapping,Node3D VisibilityTarget,SceneAnimationTargetHandle Target,AnimationHandle Animation);
    private readonly record struct ExtentBinding(ScalarReadKey Key,ScalarUnit Unit,double InputFrom,double InputTo,
        ScalarExtentVisibility Visibility,Node3D Node,Node3D Parent,SceneAnimationTargetHandle Target);
    private readonly record struct RotationBinding(ScalarReadKey Key,ScalarUnit Unit,double InputFrom,double InputTo,
        double AngleFrom,double AngleTo,Node3D Node,SceneAnimationTargetHandle Target);
    private readonly record struct TimerColourBinding(PhysicsBodyId Owner,SimulationTimerId Timer,TimerColourPalette Palette,
        MeshInstance3D Node,StandardMaterial3D Material,SceneAnimationTargetHandle Target);
    private readonly record struct ColourFollowBinding(PhysicsBodyId Owner,MeshInstance3D Node,StandardMaterial3D Material,
        SceneAnimationTargetHandle Target,AnimationHandle Animation,AnimationClock Clock,SceneColourFollowFeedback Feedback,BooleanReadKey? Boolean);
    private readonly record struct SpectralBinding(PhysicsBodyId Owner,MeshInstance3D Node,StandardMaterial3D Material,
        SceneAnimationTargetHandle Target,ScalarReadKey Red,ScalarReadKey Green,ScalarReadKey Blue,Color Inactive,AnimationClock Clock);
    private readonly BoundEnumPresentation[] _enumBindings;
    private readonly SceneLightConeRun _lightCones;
    private readonly SpectralBinding[] _spectralColours;
    private readonly Color[] _pendingSpectralColours,_lastSpectralColours;
    private readonly ColourFollowBinding[] _followingColours;
    private readonly bool[] _colourFollowEnabled;
    private readonly RotationBinding[] _rotations;
    private readonly double[] _rotationValues,_pendingRotationValues,_lastRotationValues;
    private readonly bool[] _rotationQueued;
    private readonly TimerColourBinding[] _timerColours;
    private readonly Color[] _timerColourValues,_pendingTimerColourValues,_lastTimerColourValues;
    private readonly bool[] _timerColourQueued;
    private readonly ExtentBinding[] _extents;
    private readonly double[] _extentValues,_pendingExtentValues,_lastExtentValues;
    private readonly bool[] _extentEnabled,_pendingExtentEnabled,_lastExtentEnabled,_extentQueued;
    private readonly FollowBinding[] _follows;
    private readonly double[] _lastFollowValues;
    private readonly bool[] _followVisible;
    private readonly RateBinding[] _rates;
    private readonly double[] _lastRates, _pendingRates;
    private readonly bool[] _rateVisible;
    private readonly Binding[] _bindings;
    private readonly bool[] _active, _visible;
    private readonly SceneAnimationAdapter _adapter;
    private readonly SceneOscillatorFeedback _oscillatorFeedback;
    private double _presentationTime, _simulationTime;
    private ulong _frame;
    private bool _removed;
    private PoseReadStamp _stamp;
    private readonly int _bodyCount, _counterCount, _electricalCount, _scalarCount, _timerCount, _booleanCount;
    public SceneAnimationFrameWork LastFrame { get; private set; }
    public SceneAcousticMotionRun AcousticMotion { get; }
    public SceneOccurrenceRun Occurrences { get; }

    public SceneAnimationRun(IReadOnlyList<MachinePart> parts, ScenePhysicsAssembly assembly, PoseReadLease seed,
        IReadOnlyDictionary<SceneCounterKey,SimulationCounterId> counterIds,
        IReadOnlyDictionary<SceneTimerKey,SimulationTimerId> timerIds,
        IReadOnlyDictionary<SceneOscillatorKey,SimulationOscillatorId> oscillatorIds)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(counterIds);
        ArgumentNullException.ThrowIfNull(timerIds);
        _stamp = seed.Stamp(PoseSample.Current);
        if (_stamp.Revision.Value != 0 || _stamp.SimulationTime != 0)
            throw new ArgumentException("Animation run requires an initial committed snapshot.");
        _bodyCount = seed.Count;_counterCount = seed.CounterCount;_electricalCount = seed.ElectricalInputCount;_scalarCount=seed.ScalarCount;_timerCount=seed.TimerCount;_booleanCount=seed.BooleanCount;
        var physicalTargets = assembly.PresentationTargets;
        var physical = new HashSet<TargetObjectId>();
        foreach (var target in physicalTargets)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!GodotObject.IsInstanceValid(target)) throw new ArgumentException("Physical target has been freed.");
            if (!physical.Add(new(target.GetInstanceId()))) throw new ArgumentException("Duplicate physical target.");
        }
        var enumBindings=new List<(MachinePart Owner,SceneEnumBinding Declaration)>();
        var translations=new List<(MachinePart Owner,SceneTranslationAnimation Declaration)>();
        var acousticMotionCount=0;
        var declarations = new List<(MachinePart Owner, SceneRotationAnimation Declaration)>();
        var colours = new List<(MachinePart Owner, SceneColourAnimation Declaration)>();
        var spectralColours=new List<(MachinePart Owner,SceneSpectralColour Declaration)>();
        var followingColours=new List<(MachinePart Owner,SceneColourFollow Declaration)>();
        var rates = new List<(MachinePart Owner, SceneAngularVelocityAnimation Declaration)>();
        var follows=new List<(MachinePart Owner,SceneScalarRotationAnimation Declaration)>();
        var extents=new List<(MachinePart Owner,SceneScalarExtent Declaration)>();
        var rotations=new List<(MachinePart Owner,SceneScalarRotation Declaration)>();
        var timerColours=new List<(MachinePart Owner,SceneTimerColour Declaration)>();
        var pulses=new List<(MachinePart Owner,SceneOscillatorColour Declaration)>();
        var materialUses = new Dictionary<TargetObjectId, int>();
        foreach (var owner in parts)
        {
            ArgumentNullException.ThrowIfNull(owner);
            foreach(var declaration in owner.EnumBindings)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Enum presentation target must be a live owned child.");
                enumBindings.Add((owner,declaration));
            }
            acousticMotionCount=checked(acousticMotionCount+owner.OccurrenceAnimations.Count+owner.AcousticMotions.Count+2*(owner.AcousticWavefronts?.Rings.Count??0));
            foreach(var pulse in owner.OscillatorColourAnimations)pulses.Add((owner,pulse));
            foreach(var declaration in owner.SpectralColours)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                if(declaration.Target is null||!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target)||
                    declaration.Target.MaterialOverride is not StandardMaterial3D)
                    throw new ArgumentException("Spectral colour requires a live owned mesh with a standard material.");
                if(declaration.Red.Owner!=owner||declaration.Green.Owner!=owner||declaration.Blue.Owner!=owner)
                    throw new ArgumentException("Spectral colour sources must belong to the declared owner.");
                SceneAnimationAdapter.ValidateColour(declaration.Inactive);
                if(declaration.Inactive!=((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor)
                    throw new ArgumentException("Spectral inactive colour must equal construction baseline.");
                spectralColours.Add((owner,declaration));
            }
            foreach(var declaration in owner.FollowingColours)
            {
                ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Definition);
                if(declaration.Target is null||!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target)||
                    declaration.Target.MaterialOverride is not StandardMaterial3D)
                    throw new ArgumentException("Activity colour requires a live owned mesh with a standard material.");
                if(declaration.Definition.From!=0||declaration.Definition.To!=1||declaration.Definition.Initial!=0)
                    throw new ArgumentException("Following colour requires a normalized inactive baseline.");
                if(!Enum.IsDefined(declaration.Signal.Kind))throw new ArgumentException("Unsupported following colour feedback.");
                followingColours.Add((owner,declaration));
            }
            foreach(var declaration in owner.TranslationAnimations)
            {
                ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                ValidateDrive(declaration.Feedback.Kind,declaration.Drive,declaration.Definition);
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Translation requires a live cosmetic child.");
                if(HasPhysicalWriter(declaration.Target))throw new InvalidOperationException("Translation cannot own physical geometry.");
                translations.Add((owner,declaration));
            }
            foreach (var declaration in owner.RotationAnimations)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                ValidateDrive(declaration.Feedback.Kind, declaration.Drive, declaration.Definition);
                if (!GodotObject.IsInstanceValid(declaration.Target) || !owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Animation target must be a live cosmetic child of its owner.");
                if (HasPhysicalWriter(declaration.Target))
                    throw new InvalidOperationException("Cosmetic target already has a physical pose writer.");
                declarations.Add((owner, declaration));
            }
            foreach(var declaration in owner.ScalarRotations)
            {
                ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Target);
                if(declaration.Source.Owner!=owner||!Enum.IsDefined(declaration.Unit)||!Enum.IsDefined(declaration.Axis)||
                    !double.IsFinite(declaration.InputFrom)||!double.IsFinite(declaration.InputTo)||
                    declaration.InputTo<=declaration.InputFrom||!double.IsFinite(declaration.InputTo-declaration.InputFrom)||
                    !double.IsFinite(declaration.AngleFrom)||!double.IsFinite(declaration.AngleTo)||
                    !double.IsFinite(declaration.AngleTo-declaration.AngleFrom))
                    throw new ArgumentException("Committed rotation requires an owned scalar, valid unit/axis and finite ranges.");
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Committed rotation requires a live cosmetic child.");
                if(HasPhysicalWriter(declaration.Target))throw new InvalidOperationException("Committed rotation cannot own physical geometry.");
                rotations.Add((owner,declaration));
            }
            foreach(var declaration in owner.TimerColours)
            {
                ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Palette);
                if(declaration.Source.Owner!=owner||!timerIds.ContainsKey(declaration.Source))
                    throw new ArgumentException("Timer colour requires a declared timer on its owner.");
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target)||
                    declaration.Target.MaterialOverride is not StandardMaterial3D)
                    throw new ArgumentException("Timer colour requires a live child with an exclusive standard-material override.");
                timerColours.Add((owner,declaration));
            }
            foreach(var declaration in owner.ScalarExtents)
            {
                ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                if(declaration.Source.Owner!=owner||!Enum.IsDefined(declaration.Unit)||!Enum.IsDefined(declaration.Visibility)||
                    !double.IsFinite(declaration.InputFrom)||!double.IsFinite(declaration.InputTo)||
                    declaration.InputTo<=declaration.InputFrom||!double.IsFinite(declaration.InputTo-declaration.InputFrom))
                    throw new ArgumentException("Scalar extent requires an owned source, supported units/visibility and finite increasing range.");
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target)||
                    declaration.Target.GetParent() is not Node3D)
                    throw new ArgumentException("Scalar extent requires a live cosmetic child with a spatial parent.");
                if(HasPhysicalWriter(declaration.Target))throw new InvalidOperationException("Scalar extent cannot own physical geometry.");
                extents.Add((owner,declaration));
            }
            foreach(var declaration in owner.ScalarRotationAnimations)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                if(declaration.Source.Owner!=owner||!Enum.IsDefined(declaration.Unit)||!Enum.IsDefined(declaration.Axis)||!Enum.IsDefined(declaration.Mapping)||
                    !double.IsFinite(declaration.InputFrom)||!double.IsFinite(declaration.InputTo)||
                    declaration.InputTo<=declaration.InputFrom||!double.IsFinite(declaration.InputTo-declaration.InputFrom))
                    throw new ArgumentException("Scalar gauge requires an owned source, supported units/axis and finite increasing input range.");
                if(!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Scalar gauge target must be a live cosmetic child.");
                if(HasPhysicalWriter(declaration.Target))throw new InvalidOperationException("Scalar gauge cannot own physical geometry.");
                follows.Add((owner,declaration));
            }
            foreach (var declaration in owner.AngularVelocityAnimations)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                ArgumentNullException.ThrowIfNull(declaration.Target);
                if (!Enum.IsDefined(declaration.SourceAxis) || !Enum.IsDefined(declaration.TargetAxis) ||
                    !Enum.IsDefined(declaration.Direction) || !Enum.IsDefined(declaration.Clock))
                    throw new ArgumentException("Unsupported angular animation axis, direction or clock.");
                if (declaration.Body.Owner != owner || declaration.Reference.Owner != owner)
                    throw new ArgumentException("Angular animation sources must belong to their declaring owner.");
                if (!GodotObject.IsInstanceValid(declaration.Target) || !owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Angular animation target must be a live cosmetic child.");
                if (HasPhysicalWriter(declaration.Target))
                    throw new InvalidOperationException("Cosmetic target already has a physical pose writer.");
                rates.Add((owner, declaration));
            }
            foreach (var declaration in owner.ColourAnimations)
            {
                ArgumentNullException.ThrowIfNull(declaration);
                ArgumentNullException.ThrowIfNull(declaration.Target);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                ValidateDrive(declaration.Feedback.Kind, declaration.Drive, declaration.Definition);
                if (!GodotObject.IsInstanceValid(declaration.Target) || !owner.IsAncestorOf(declaration.Target))
                    throw new ArgumentException("Colour target must be a live child of its owner.");
                if (declaration.Target.MaterialOverride is not StandardMaterial3D)
                    throw new ArgumentException("Animated colour requires an exclusive standard-material override.");
                colours.Add((owner, declaration));
            }
        }
        if (enumBindings.Count > 0 || colours.Count > 0 || timerColours.Count > 0 || followingColours.Count > 0 || spectralColours.Count > 0)
        {
            foreach (var owner in parts) CountMaterials(owner);
            foreach (var (_, declaration) in colours)
                if (materialUses[new(declaration.Target.MaterialOverride.GetInstanceId())] != 1)
                    throw new ArgumentException("Animated colour requires an exclusive standard-material override.");
        }
        foreach(var (_,declaration) in timerColours)
            if(materialUses[new(declaration.Target.MaterialOverride.GetInstanceId())]!=1)
                throw new ArgumentException("Timer colour requires an exclusive standard-material override.");
        foreach(var (_,declaration) in followingColours)
            if(materialUses[new(declaration.Target.MaterialOverride.GetInstanceId())]!=1)
                throw new ArgumentException("Activity colour requires an exclusive material.");
        foreach(var (_,declaration) in spectralColours)
            if(materialUses[new(declaration.Target.MaterialOverride.GetInstanceId())]!=1)
                throw new ArgumentException("Spectral colour requires an exclusive material.");
        var count = checked(declarations.Count + colours.Count + translations.Count);
        _adapter = new(Math.Max(1, checked(count + rates.Count + follows.Count + extents.Count + rotations.Count + timerColours.Count + pulses.Count + followingColours.Count + spectralColours.Count + acousticMotionCount + enumBindings.Count)));
        _spectralColours=new SpectralBinding[spectralColours.Count];
        _pendingSpectralColours=new Color[spectralColours.Count];_lastSpectralColours=new Color[spectralColours.Count];
        for(var i=0;i<spectralColours.Count;i++)
        {
            var (owner,declaration)=spectralColours[i];var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            var material=(StandardMaterial3D)declaration.Target.MaterialOverride;var target=_adapter.Register(material);
            var binding=new SpectralBinding(root,declaration.Target,material,target,new(root,declaration.Red.Slot),
                new(root,declaration.Green.Slot),new(root,declaration.Blue.Slot),declaration.Inactive,declaration.Clock);
            _=SpectralColour(seed,binding);
            _adapter.BindFollowingRgb(target,declaration.Response,declaration.Clock);
            _spectralColours[i]=binding;_lastSpectralColours[i]=declaration.Inactive;
        }
        _followingColours=new ColourFollowBinding[followingColours.Count];_colourFollowEnabled=new bool[followingColours.Count];
        for(var i=0;i<followingColours.Count;i++)
        {
            var (owner,declaration)=followingColours[i];var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            if(seed.ReadQuery(PoseSample.Current,root.Index).Owner!=root)throw new ArgumentException("Activity colour owner is not committed.");
            BooleanReadKey? boolean=null;
            if(declaration.Signal.Kind==SceneColourFollowFeedback.BooleanObservation)
            {
                if(declaration.Signal.Observation is not { } source||source.Owner!=owner)
                    throw new ArgumentException("Following colour requires an owned Boolean observation.");
                boolean=new(root,source.Slot);_=seed.ReadBoolean(PoseSample.Current,boolean.Value);
            }
            var material=(StandardMaterial3D)declaration.Target.MaterialOverride;var target=_adapter.Register(material);
            var animation=_adapter.BindFollowingColour(target,declaration.Definition,declaration.From,declaration.To);
            _followingColours[i]=new(root,declaration.Target,material,target,animation,declaration.Definition.Clock,declaration.Signal.Kind,boolean);
        }
        _rotations=new RotationBinding[rotations.Count];_rotationValues=new double[rotations.Count];
        _pendingRotationValues=new double[rotations.Count];_lastRotationValues=new double[rotations.Count];
        _rotationQueued=new bool[rotations.Count];
        _timerColours=new TimerColourBinding[timerColours.Count];_timerColourValues=new Color[timerColours.Count];
        _pendingTimerColourValues=new Color[timerColours.Count];_lastTimerColourValues=new Color[timerColours.Count];
        _timerColourQueued=new bool[timerColours.Count];
        _extents=new ExtentBinding[extents.Count];_extentValues=new double[extents.Count];
        _pendingExtentValues=new double[extents.Count];_lastExtentValues=new double[extents.Count];
        _extentEnabled=new bool[extents.Count];_pendingExtentEnabled=new bool[extents.Count];
        _lastExtentEnabled=new bool[extents.Count];_extentQueued=new bool[extents.Count];
        _follows=new FollowBinding[follows.Count];_lastFollowValues=new double[follows.Count];
        _followVisible=new bool[follows.Count];Array.Fill(_followVisible,true);
        _rates = new RateBinding[rates.Count];
        _lastRates = new double[rates.Count];
        _pendingRates = new double[rates.Count];
        _rateVisible = new bool[rates.Count];
        Array.Fill(_rateVisible, true);
        _bindings = new Binding[count];
        _active = new bool[count];
        _visible = new bool[count];
        Array.Fill(_visible, true);
        for (var i = 0; i < declarations.Count; i++)
        {
            var (owner, declaration) = declarations[i];
            var target = _adapter.Register(declaration.Target);
            var animation = _adapter.BindRotation(target, declaration.Definition, declaration.Axis);
            var body = assembly.QueryOwnerId(new(owner, MachinePart.RootBody));
            if (seed.ReadQuery(PoseSample.Current, body.Index).Owner != body)
                throw new ArgumentException("Animation feedback must identify a committed owner root.");
            _bindings[i] = new(body, declaration.Target, declaration.Definition, declaration.Feedback.Kind,
                declaration.Drive, target, animation, null,
                ResolveCounter(owner,declaration.Feedback),ResolveInput(body,declaration.Feedback),ResolveScalar(owner,body,declaration.Feedback),ResolveBoolean(owner,body,declaration.Feedback));
        }
        for (var i = 0; i < colours.Count; i++)
        {
            var (owner, declaration) = colours[i];
            var material = (StandardMaterial3D)declaration.Target.MaterialOverride;
            var target = _adapter.Register(material);
            var animation = _adapter.BindColour(target, declaration.Definition, declaration.From, declaration.To);
            var body = assembly.QueryOwnerId(new(owner, MachinePart.RootBody));
            if (seed.ReadQuery(PoseSample.Current, body.Index).Owner != body)
                throw new ArgumentException("Animation feedback must identify a committed owner root.");
            _bindings[declarations.Count + i] = new(body, declaration.Target, declaration.Definition, declaration.Feedback.Kind,
                declaration.Drive,
                target, animation, material,
                ResolveCounter(owner,declaration.Feedback),ResolveInput(body,declaration.Feedback),ResolveScalar(owner,body,declaration.Feedback),ResolveBoolean(owner,body,declaration.Feedback));
        }
        for(var i=0;i<translations.Count;i++)
        {
            var (owner,declaration)=translations[i];var target=_adapter.Register(declaration.Target);
            var animation=_adapter.BindTranslation(target,declaration.Definition,declaration.Axis);
            var body=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            if(seed.ReadQuery(PoseSample.Current,body.Index).Owner!=body)throw new ArgumentException("Translation owner is not committed.");
            _bindings[declarations.Count+colours.Count+i]=new(body,declaration.Target,declaration.Definition,declaration.Feedback.Kind,
                declaration.Drive,target,animation,null,ResolveCounter(owner,declaration.Feedback),
                ResolveInput(body,declaration.Feedback),ResolveScalar(owner,body,declaration.Feedback),ResolveBoolean(owner,body,declaration.Feedback));
        }
        for (var i = 0; i < rates.Count; i++)
        {
            var (owner, declaration) = rates[i];
            var body = assembly.Body(declaration.Body).Id;
            var reference = assembly.Body(declaration.Reference).Id;
            var root = assembly.QueryOwnerId(new(owner, MachinePart.RootBody));
            var target = _adapter.Register(declaration.Target);
            var definition = new AnimationDefinition(0, Math.Tau, 1,
                AnimationCurve.Linear, AnimationRepeat.Loop, declaration.Clock);
            var animation = _adapter.BindRotation(target, definition, declaration.TargetAxis);
            _rates[i] = new(root, body, reference, declaration.Target, Axis(declaration.SourceAxis),
                declaration.Direction, declaration.Clock, target, animation);
            ValidateRateTopology(seed, _rates[i]);
        }
        for(var i=0;i<follows.Count;i++)
        {
            var (owner,declaration)=follows[i];
            var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            var key=new ScalarReadKey(root,declaration.Source.Slot);
            if(seed.ReadScalar(PoseSample.Current,key).Unit!=declaration.Unit)
                throw new ArgumentException("Scalar gauge units differ from its observation.");
            var target=_adapter.Register(declaration.Target);
            var animation=_adapter.BindFollowingRotation(target,declaration.Definition,declaration.Axis);
            _follows[i]=new(key,declaration.Unit,declaration.InputFrom,declaration.InputTo,
                declaration.Definition,declaration.Mapping,declaration.Target,target,animation);
            _lastFollowValues[i]=declaration.Definition.Initial;
        }
        for(var i=0;i<extents.Count;i++)
        {
            var (owner,declaration)=extents[i];
            var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            var key=new ScalarReadKey(root,declaration.Source.Slot);
            if(seed.ReadScalar(PoseSample.Current,key).Unit!=declaration.Unit)
                throw new ArgumentException("Scalar extent units differ from its observation.");
            var target=_adapter.Register(declaration.Target);_adapter.ClaimScalarExtent(target,declaration.Definition);
            _extents[i]=new(key,declaration.Unit,declaration.InputFrom,declaration.InputTo,
                declaration.Visibility,declaration.Target,(Node3D)declaration.Target.GetParent(),target);
        }
        for(var i=0;i<rotations.Count;i++)
        {
            var (owner,declaration)=rotations[i];var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            var key=new ScalarReadKey(root,declaration.Source.Slot);
            if(seed.ReadScalar(PoseSample.Current,key).Unit!=declaration.Unit)
                throw new ArgumentException("Committed rotation units differ from its observation.");
            var target=_adapter.Register(declaration.Target);_adapter.ClaimCommittedRotation(target,declaration.Axis);
            _adapter.ValidateCommittedRotation(target,declaration.AngleFrom);
            _adapter.ValidateCommittedRotation(target,declaration.AngleTo);
            _rotations[i]=new(key,declaration.Unit,declaration.InputFrom,declaration.InputTo,
                declaration.AngleFrom,declaration.AngleTo,declaration.Target,target);
        }
        for(var i=0;i<timerColours.Count;i++)
        {
            var (owner,declaration)=timerColours[i];var root=assembly.QueryOwnerId(new(owner,MachinePart.RootBody));
            var timer=timerIds[declaration.Source];_=seed.ReadTimer(PoseSample.Current,timer);
            var material=(StandardMaterial3D)declaration.Target.MaterialOverride;
            var target=_adapter.Register(material);_adapter.ClaimCommittedColour(target);
            _timerColours[i]=new(root,timer,declaration.Palette,declaration.Target,material,target);
        }
        var enumMaterials=new Dictionary<EnumMaterialId,int>();
        foreach(var pair in materialUses)enumMaterials.Add(new(pair.Key.Value),pair.Value);
        _enumBindings=new BoundEnumPresentation[enumBindings.Count];
        for(var i=0;i<enumBindings.Count;i++)
        {
            var (owner,declaration)=enumBindings[i];
            _enumBindings[i]=declaration.Bind(owner,assembly,_adapter,seed,physicalTargets,enumMaterials);
        }
        _oscillatorFeedback=new(_adapter,parts,pulses,oscillatorIds);
        _lightCones=new(parts,assembly,seed);
        AcousticMotion=new(_adapter,parts,physicalTargets,seed.Stamp(PoseSample.Current));
        Occurrences=new(_adapter,parts,physicalTargets,seed.Stamp(PoseSample.Current));
        Apply(seed);

        BooleanReadKey? ResolveBoolean(MachinePart owner,PhysicsBodyId body,SceneAnimationSignal signal)
        {
            if(signal.Kind!=SceneAnimationFeedback.BooleanObservation)return null;
            if(signal.Observation is not { } source||source.Owner!=owner)
                throw new ArgumentException("Boolean feedback requires an owned observation.");
            var key=new BooleanReadKey(body,source.Slot);
            _=seed.ReadBoolean(PoseSample.Current,key);
            return key;
        }

        ScalarBinding? ResolveScalar(MachinePart owner,PhysicsBodyId body,SceneAnimationSignal signal)
        {
            if(signal.Kind!=SceneAnimationFeedback.ScalarThreshold)return null;
            if(signal.Scalar is not { } source||source.Owner!=owner)throw new ArgumentException("Scalar feedback requires an owned observation.");
            var key=new ScalarReadKey(body,source.Slot);
            if(seed.ReadScalar(PoseSample.Current,key).Unit!=signal.Unit)throw new ArgumentException("Scalar feedback units differ.");
            return new(key,signal.Unit,signal.ScalarThreshold);
        }

        ElectricalInputKey? ResolveInput(PhysicsBodyId owner,SceneAnimationSignal signal)
        {
            if(signal.Kind!=SceneAnimationFeedback.ElectricalInput)return null;
            if(signal.Input is not { } port)throw new ArgumentException("Electrical feedback requires an input socket.");
            var key=new ElectricalInputKey(owner,port);
            _=seed.ReadElectricalInput(PoseSample.Current,key);
            return key;
        }

        CounterBinding? ResolveCounter(MachinePart owner,SceneAnimationSignal signal)
        {
            if(signal.Kind!=SceneAnimationFeedback.CounterThreshold)return null;
            if(signal.Counter is not { } key||key.Owner!=owner||!counterIds.TryGetValue(key,out var id))
                throw new ArgumentException("Counter feedback must identify a declared counter on its owner.");
            var target=seed.ReadCounter(PoseSample.Current,id).Target;
            if(signal.Threshold>target)
                throw new ArgumentException("Counter feedback threshold exceeds its declared target.");
            return new(id,target,signal.Threshold);
        }

        bool HasPhysicalWriter(Node3D target)
        {
            if (physical.Contains(new(target.GetInstanceId()))) return true;
            foreach (var physicalTarget in physicalTargets)
                if (target.IsAncestorOf(physicalTarget)) return true;
            return false;
        }

        void CountMaterials(Node node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (node is MeshInstance3D mesh)
            {
                var used = new HashSet<TargetObjectId>();
                if (mesh.MaterialOverride is { } material) used.Add(new(material.GetInstanceId()));
                else if (mesh.Mesh is { } geometry)
                    for (var surface = 0; surface < geometry.GetSurfaceCount(); surface++)
                        if (mesh.GetActiveMaterial(surface) is { } activeMaterial) used.Add(new(activeMaterial.GetInstanceId()));
                foreach (var identity in used)
                    materialUses[identity] = materialUses.TryGetValue(identity, out var occurrences) ? occurrences + 1 : 1;
            }
            foreach (var child in node.GetChildren()) CountMaterials(child);
        }
    }

    private static void ValidateDrive(SceneAnimationFeedback feedback, SceneAnimationDrive drive, AnimationDefinition definition)
    {
        if (!Enum.IsDefined(feedback) || !Enum.IsDefined(drive))
            throw new ArgumentException("Unsupported animation feedback or drive.");
        if (drive == SceneAnimationDrive.Endpoint && definition.Repeat != AnimationRepeat.Once)
            throw new ArgumentException("Endpoint feedback requires a reversible once-clip.");
    }

    private static CollisionVector Axis(AnimationRotationAxis axis) => axis switch
    {
        AnimationRotationAxis.X => new(1, 0, 0),
        AnimationRotationAxis.Y => new(0, 1, 0),
        AnimationRotationAxis.Z => new(0, 0, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    private static void ValidateRateTopology(PoseReadLease read, RateBinding binding)
    {
        if (read.ReadQuery(PoseSample.Current, binding.Owner.Index).Owner != binding.Owner ||
            read.ReadQuery(PoseSample.Current, binding.Body.Index).Owner != binding.Owner ||
            read.ReadQuery(PoseSample.Current, binding.Reference.Index).Owner != binding.Owner)
            throw new ArgumentException("Angular animation source topology changed.");
    }

    public void Publish(PoseReadLease read)
    {
        RequireLive();
        var stamp = read.Stamp(PoseSample.Current);
        if (stamp.Generation != _stamp.Generation || stamp.Revision.Value != checked(_stamp.Revision.Value + 1) ||
            stamp.SimulationTime <= _stamp.SimulationTime || read.Stamp(PoseSample.Previous) != _stamp ||
            read.Count != _bodyCount || read.CounterCount != _counterCount || read.ElectricalInputCount != _electricalCount || read.ScalarCount != _scalarCount || read.TimerCount != _timerCount || read.BooleanCount != _booleanCount)
            throw new ArgumentException("Animation feedback must consume adjacent commits from its run.");
        foreach (var binding in _bindings)
        {
            if (read.ReadQuery(PoseSample.Current, binding.Owner.Index).Owner != binding.Owner)
                throw new ArgumentException("Animation feedback owner topology changed.");
            if(binding.Counter is { } counter&&read.ReadCounter(PoseSample.Current,counter.Id).Target!=counter.Target)
                throw new ArgumentException("Animation counter topology changed.");
            if(binding.Input is { } input)_=read.ReadElectricalInput(PoseSample.Current,input);
            if(binding.Boolean is { } boolean)_=read.ReadBoolean(PoseSample.Current,boolean);
            if(binding.Scalar is { } scalar&&read.ReadScalar(PoseSample.Current,scalar.Key).Unit!=scalar.Unit)
                throw new ArgumentException("Animation scalar units changed.");
        }
        foreach(var binding in _followingColours)
        {
            if(read.ReadQuery(PoseSample.Current,binding.Owner.Index).Owner!=binding.Owner)
                throw new ArgumentException("Following colour owner topology changed.");
            if(binding.Boolean is { } boolean)_=read.ReadBoolean(PoseSample.Current,boolean);
        }
        foreach(var binding in _rotations)
            if(read.ReadScalar(PoseSample.Current,binding.Key).Unit!=binding.Unit||
                read.ReadQuery(PoseSample.Current,binding.Key.Owner.Index).Owner!=binding.Key.Owner)
                throw new ArgumentException("Committed rotation topology changed.");
        foreach(var binding in _timerColours)
        {
            if(read.ReadQuery(PoseSample.Current,binding.Owner.Index).Owner!=binding.Owner)
                throw new ArgumentException("Timer colour owner topology changed.");
            _=read.ReadTimer(PoseSample.Current,binding.Timer);
        }
        foreach(var binding in _extents)
            if(read.ReadScalar(PoseSample.Current,binding.Key).Unit!=binding.Unit||
                read.ReadQuery(PoseSample.Current,binding.Key.Owner.Index).Owner!=binding.Key.Owner)
                throw new ArgumentException("Scalar extent topology changed.");
        foreach(var binding in _follows)
            if(read.ReadScalar(PoseSample.Current,binding.Key).Unit!=binding.Unit)
                throw new ArgumentException("Scalar gauge topology changed.");
        foreach (var binding in _rates) ValidateRateTopology(read, binding);
        Apply(read);
        _stamp = stamp;
    }

    private static Color SpectralColour(PoseReadLease read,SpectralBinding binding)
    {
        if(read.ReadQuery(PoseSample.Current,binding.Owner.Index).Owner!=binding.Owner)
            throw new ArgumentException("Spectral colour owner topology changed.");
        double Channel(ScalarReadKey key)
        {
            var value=read.ReadScalar(PoseSample.Current,key);
            if(value.Unit!=ScalarUnit.GameOpticalPower||value.Value<0||value.Value>float.MaxValue)
                throw new ArgumentException("Spectral feedback requires nonnegative finite optical power.");
            return value.Value;
        }
        var red=Channel(binding.Red);var green=Channel(binding.Green);var blue=Channel(binding.Blue);
        if(!double.IsFinite(red+green+blue)||red+green+blue>float.MaxValue)
            throw new ArgumentException("Spectral feedback exceeds the supported render range.");
        if(read.ReadActivity(PoseSample.Current,binding.Owner.Index)!=OwnerActivity.Active)return binding.Inactive;
        if(red+green+blue==0)throw new ArgumentException("Active optical feedback requires nonzero output power.");
        var colour=OpticalColours.BeamInk(new((float)red,(float)green,(float)blue));
        colour.A=binding.Inactive.A;
        return colour;
    }

    private void Apply(PoseReadLease read)
    {
        var simulationTime = read.Stamp(PoseSample.Current).SimulationTime;
        _lightCones.Validate(read);
        foreach(var binding in _enumBindings)binding.Stage(read);
        for(var i=0;i<_spectralColours.Length;i++)_pendingSpectralColours[i]=SpectralColour(read,_spectralColours[i]);
        // Validate every source before changing animation controls. Convert to cycles before
        // subtraction to avoid overflow for large, oppositely signed finite velocities.
        for (var i = 0; i < _rates.Length; i++)
        {
            var binding = _rates[i];
            var axis = read.Read(PoseSample.Current, binding.Reference.Index).Pose.Rotation.Apply(binding.SourceAxis);
            var body = read.ReadVelocity(PoseSample.Current, binding.Body).AngularRadiansPerSecond / Math.Tau;
            var reference = read.ReadVelocity(PoseSample.Current, binding.Reference).AngularRadiansPerSecond / Math.Tau;
            var rate = CollisionVector.Dot(body - reference, axis);
            if (binding.Direction == AnimationDirection.Reverse) rate = -rate;
            if (!double.IsFinite(rate)) throw new ArgumentException("Angular animation rate exceeds numeric range.");
            _pendingRates[i] = rate;
        }
        for(var i=0;i<_extents.Length;i++)
        {
            var binding=_extents[i];var value=read.ReadScalar(PoseSample.Current,binding.Key).Value;
            var fraction=(Math.Clamp(value,binding.InputFrom,binding.InputTo)-binding.InputFrom)/(binding.InputTo-binding.InputFrom);
            _adapter.ValidateScalarExtent(binding.Target,fraction);
            _pendingExtentValues[i]=fraction;
            _pendingExtentEnabled[i]=binding.Visibility switch
            {
                ScalarExtentVisibility.Always=>true,
                ScalarExtentVisibility.OwnerActive=>read.ReadActivity(PoseSample.Current,binding.Key.Owner.Index)==OwnerActivity.Active,
                _=>throw new InvalidOperationException("Unsupported scalar extent visibility.")
            };
        }
        for(var i=0;i<_rotations.Length;i++)
        {
            var binding=_rotations[i];var value=read.ReadScalar(PoseSample.Current,binding.Key).Value;
            var fraction=(Math.Clamp(value,binding.InputFrom,binding.InputTo)-binding.InputFrom)/(binding.InputTo-binding.InputFrom);
            var angle=Math.FusedMultiplyAdd(binding.AngleTo-binding.AngleFrom,fraction,binding.AngleFrom);
            _adapter.ValidateCommittedRotation(binding.Target,angle);_pendingRotationValues[i]=angle;
        }
        for(var i=0;i<_timerColours.Length;i++)
        {
            var binding=_timerColours[i];
            _pendingTimerColourValues[i]=binding.Palette.Read(read.ReadTimer(PoseSample.Current,binding.Timer).Phase);
        }
        foreach(var binding in _enumBindings)binding.Commit();
        Array.Copy(_pendingRotationValues,_rotationValues,_rotations.Length);
        Array.Copy(_pendingTimerColourValues,_timerColourValues,_timerColours.Length);
        Array.Copy(_pendingExtentValues,_extentValues,_extents.Length);
        Array.Copy(_pendingExtentEnabled,_extentEnabled,_extents.Length);
        for(var i=0;i<_follows.Length;i++)
        {
            var binding=_follows[i];
            var value=read.ReadScalar(PoseSample.Current,binding.Key).Value;
            var amount=(Math.Clamp(value,binding.InputFrom,binding.InputTo)-binding.InputFrom)/(binding.InputTo-binding.InputFrom);
            amount=binding.Mapping switch
            {
                ScalarAnimationMapping.Linear=>amount,
                ScalarAnimationMapping.SineCycle=>.5+.5*double.SinPi(2*amount),
                _=>throw new InvalidOperationException("Unsupported scalar animation mapping.")
            };
            var target=Math.FusedMultiplyAdd(binding.Definition.To-binding.Definition.From,amount,binding.Definition.From);
            if(target==_lastFollowValues[i])continue;
            _adapter.FollowValue(binding.Animation,target,binding.Definition.Clock==AnimationClock.Simulation?simulationTime:_presentationTime);
            _lastFollowValues[i]=target;
        }
        for(var i=0;i<_followingColours.Length;i++)
        {
            var binding=_followingColours[i];
            var enabled=binding.Feedback switch
            {
                SceneColourFollowFeedback.OwnerActivity=>read.ReadActivity(PoseSample.Current,binding.Owner.Index)==OwnerActivity.Active,
                SceneColourFollowFeedback.BooleanObservation=>read.ReadBoolean(PoseSample.Current,binding.Boolean!.Value).Value,
                _=>throw new InvalidOperationException("Unsupported following colour feedback.")
            };
            if(enabled==_colourFollowEnabled[i])continue;
            _adapter.FollowValue(binding.Animation,enabled?1:0,binding.Clock==AnimationClock.Simulation?simulationTime:_presentationTime);
            _colourFollowEnabled[i]=enabled;
        }
        for(var i=0;i<_spectralColours.Length;i++)
        {
            if(_pendingSpectralColours[i]==_lastSpectralColours[i])continue;
            var binding=_spectralColours[i];
            _adapter.FollowRgb(binding.Target,_pendingSpectralColours[i],
                binding.Clock==AnimationClock.Simulation?simulationTime:_presentationTime);
            _lastSpectralColours[i]=_pendingSpectralColours[i];
        }
        _lightCones.Publish(read);
        _simulationTime = simulationTime;
        for (var i = 0; i < _rates.Length; i++)
        {
            if (_pendingRates[i] == _lastRates[i]) continue;
            var binding = _rates[i];
            _adapter.SetLoopRate(binding.Animation, _pendingRates[i],
                binding.Clock == AnimationClock.Simulation ? simulationTime : _presentationTime);
            _lastRates[i] = _pendingRates[i];
        }
        for (var i = 0; i < _bindings.Length; i++)
        {
            var binding = _bindings[i];
            var enabled = binding.Feedback switch
            {
                SceneAnimationFeedback.Autonomous => true,
                SceneAnimationFeedback.BooleanObservation => read.ReadBoolean(PoseSample.Current,binding.Boolean!.Value).Value,
                SceneAnimationFeedback.CounterThreshold => read.ReadCounter(PoseSample.Current,binding.Counter!.Value.Id).Count >= binding.Counter.Value.Threshold,
                SceneAnimationFeedback.ScalarThreshold => read.ReadScalar(PoseSample.Current,binding.Scalar!.Value.Key).Value >= binding.Scalar.Value.Threshold,
                SceneAnimationFeedback.ElectricalInput => read.ReadElectricalInput(PoseSample.Current,binding.Input!.Value).Availability == ElectricalAvailability.Available,
                SceneAnimationFeedback.OwnerActive => read.ReadActivity(PoseSample.Current, binding.Owner.Index) == OwnerActivity.Active,
                _ => throw new InvalidOperationException("Unsupported animation feedback.")
            };
            if (enabled == _active[i]) continue;
            var time = binding.Definition.Clock == AnimationClock.Simulation ? simulationTime : _presentationTime;
            switch (binding.Drive)
            {
                case SceneAnimationDrive.Endpoint:
                    _adapter.DriveTo(binding.Animation, enabled ? AnimationEndpoint.To : AnimationEndpoint.From, time);
                    break;
                case SceneAnimationDrive.StartStop:
                    if (enabled) _adapter.StartAt(binding.Animation, time);
                    else _adapter.Stop(binding.Animation, AnimationStop.Hold, time);
                    break;
                default: throw new InvalidOperationException("Unsupported feedback drive.");
            }
            _active[i] = enabled;
        }
    }

    public void ConsumeOscillatorEvents(CommittedEventStream<SimulationOscillatorPulse> events)
    {
        RequireLive();_oscillatorFeedback.Consume(events,_stamp,_presentationTime);
    }
    public AnimationImpulseRead ReadOscillatorFeedback(SceneOscillatorKey source,int bindingIndex)=>
        _oscillatorFeedback.Read(source,bindingIndex);

    public void PresentLightCones(MachineWorld world) {RequireLive();_lightCones.Present(world);}

    public SceneAnimationFrameWork Present(double delta
#if PLAYTEST
        ,PerformanceRecorder? performance=null
#endif
    )
    {
        RequireLive();
        if (!double.IsFinite(delta) || delta < 0 || !double.IsFinite(_presentationTime + delta))
            throw new ArgumentOutOfRangeException(nameof(delta));
        var nextFrame = checked(_frame + 1);
        for (var i = 0; i < _bindings.Length; i++)
        {
            var binding = _bindings[i];
            if (!GodotObject.IsInstanceValid(binding.VisibilityTarget))
                throw new InvalidOperationException("Animation target has been freed.");
            if (binding.Material is { } material &&
                ((MeshInstance3D)binding.VisibilityTarget).MaterialOverride != material)
                throw new InvalidOperationException("Animated material binding changed during its run.");
            var visible = binding.VisibilityTarget.IsVisibleInTree();
            if (visible == _visible[i]) continue;
            _adapter.SetVisible(binding.Target, visible);
            _visible[i] = visible;
        }
        for (var i = 0; i < _rates.Length; i++)
        {
            var binding = _rates[i];
            if (!GodotObject.IsInstanceValid(binding.VisibilityTarget))
                throw new InvalidOperationException("Angular animation target has been freed.");
            var visible = binding.VisibilityTarget.IsVisibleInTree();
            if (visible == _rateVisible[i]) continue;
            _adapter.SetVisible(binding.Target, visible);
            _rateVisible[i] = visible;
        }
        for(var i=0;i<_follows.Length;i++)
        {
            var binding=_follows[i];
            if(!GodotObject.IsInstanceValid(binding.VisibilityTarget))throw new InvalidOperationException("Scalar gauge target has been freed.");
            var visible=binding.VisibilityTarget.IsVisibleInTree();
            if(visible==_followVisible[i])continue;
            _adapter.SetVisible(binding.Target,visible);_followVisible[i]=visible;
        }
        for(var i=0;i<_extents.Length;i++)
        {
            var binding=_extents[i];
            if(!GodotObject.IsInstanceValid(binding.Node)||!GodotObject.IsInstanceValid(binding.Parent)||
                binding.Node.GetParent()!=binding.Parent)throw new InvalidOperationException("Scalar extent target binding changed.");
            if(!binding.Parent.IsVisibleInTree())continue;
            if(_extentQueued[i]&&_extentValues[i]==_lastExtentValues[i]&&_extentEnabled[i]==_lastExtentEnabled[i])continue;
            _adapter.QueueScalarExtent(binding.Target,_extentValues[i],_extentEnabled[i]);
            _lastExtentValues[i]=_extentValues[i];_lastExtentEnabled[i]=_extentEnabled[i];_extentQueued[i]=true;
        }
        for(var i=0;i<_rotations.Length;i++)
        {
            var binding=_rotations[i];
            if(!GodotObject.IsInstanceValid(binding.Node))throw new InvalidOperationException("Committed rotation target was freed.");
            if(!binding.Node.IsVisibleInTree())continue;
            if(_rotationQueued[i]&&_rotationValues[i]==_lastRotationValues[i])continue;
            _adapter.QueueCommittedRotation(binding.Target,_rotationValues[i]);
            _lastRotationValues[i]=_rotationValues[i];_rotationQueued[i]=true;
        }
        for(var i=0;i<_timerColours.Length;i++)
        {
            var binding=_timerColours[i];
            if(!GodotObject.IsInstanceValid(binding.Node)||binding.Node.MaterialOverride!=binding.Material)
                throw new InvalidOperationException("Timer colour target binding changed.");
            if(!binding.Node.IsVisibleInTree())continue;
            if(_timerColourQueued[i]&&_timerColourValues[i]==_lastTimerColourValues[i])continue;
            _adapter.QueueCommittedColour(binding.Target,_timerColourValues[i]);
            _lastTimerColourValues[i]=_timerColourValues[i];_timerColourQueued[i]=true;
        }
        foreach(var binding in _followingColours)
        {
            if(!GodotObject.IsInstanceValid(binding.Node)||binding.Node.MaterialOverride!=binding.Material)
                throw new InvalidOperationException("Activity colour target binding changed.");
            _adapter.SetVisible(binding.Target,binding.Node.IsVisibleInTree());
        }
        foreach(var binding in _spectralColours)
        {
            if(!GodotObject.IsInstanceValid(binding.Node)||binding.Node.MaterialOverride!=binding.Material)
                throw new InvalidOperationException("Spectral colour target binding changed.");
            _adapter.SetVisible(binding.Target,binding.Node.IsVisibleInTree());
        }
        foreach(var binding in _enumBindings)binding.Queue();
        _oscillatorFeedback.RefreshVisibility();
        AcousticMotion.RefreshVisibility();Occurrences.RefreshVisibility();
        LastFrame = _adapter.Present(nextFrame, _presentationTime + delta, _simulationTime
#if PLAYTEST
            ,performance
#endif
        );
        _frame = nextFrame;
        _presentationTime += delta;
        AcousticMotion.AdvancePresentation(_presentationTime);Occurrences.AdvancePresentation(_presentationTime);
#if PLAYTEST
        performance?.Begin(PerformanceStage.SceneSubmission);
#endif
        AcousticMotion.Wavefronts.Present();
#if PLAYTEST
        performance?.End(PerformanceStage.SceneSubmission);
#endif
        return LastFrame;
    }

    public void Remove()
    {
        RequireLive();
        _lightCones.Remove();
        AcousticMotion.Remove();Occurrences.Remove();
        _adapter.Reset();
        _removed = true;
    }

    private void RequireLive()
    {
        if (_removed) throw new InvalidOperationException("Animation run has been removed.");
    }
}
