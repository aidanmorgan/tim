using System;
using System.Collections.Generic;
using Godot;

namespace CuriousContraptions.Presentation;

public enum AnimationTranslationAxis { X, Y, Z }
public enum AnimationRotationAxis { X, Y, Z }
public enum AnimationTargetRemoval { RestoreBaseline, Detach }

public readonly record struct SceneAnimationTargetHandle
{
    internal SceneAnimationAdapter Owner { get; }
    public AnimationGeneration Generation { get; }
    public AnimationTargetId Target { get; }
    public ulong Version { get; }
    internal SceneAnimationTargetHandle(SceneAnimationAdapter owner, AnimationGeneration generation,
        AnimationTargetId target, ulong version)
    {
        Owner = owner; Generation = generation; Target = target; Version = version;
    }
}
public readonly record struct SceneAnimationFrameWork(AnimationFrameWork Animation, int TransformWrites, int ColourWrites, int VisibilityWrites);

/// <summary>Godot boundary for typed presentation targets. Animation and physical pose
/// writers cannot share a transform. Physical poses are queued values, never live bodies.</summary>
public sealed class SceneAnimationAdapter
{
    private enum TargetKind { Spatial, Canvas, Material }
    private enum TransformOwner { Unclaimed, Animation, Physical, ScalarExtent, CommittedRotation }
    private enum ColourOwner { Unclaimed, Animation, Committed, RgbFollow }
    private readonly record struct SceneObjectId(ulong Value);
    private sealed class Target
    {
        public required GodotObject Object;
        public required SceneObjectId Identity;
        public required TargetKind Kind;
        public TransformOwner TransformOwner;
        public ColourOwner ColourOwner;
        public Transform3D Baseline, PendingTransform;
        public Color BaselineColour, PendingColour, PublishedColour, FromColour, ToColour;
        public double Angle, Scale = 1, Blend, Alpha, Translation;
        public AnimationTranslationAxis TranslationAxis;
        public Color FollowedColour;
        public AnimationHandle? Red, Green, Blue;
        public bool Visible = true;
        public bool BaselineNodeVisible, PendingNodeVisible;
        public ScalarExtentDefinition? Extent;
        public AnimationRotationAxis Axis;
        public AnimationHandle? Rotation, Scaling, Opacity, Colour, Position;
        public int DirtyIndex = -1;
    }
    private readonly Target?[] _targets;
    private readonly ulong[] _versions;
    private readonly int[] _free, _dirty;
    private readonly Dictionary<SceneObjectId, int> _identities;
    private readonly AnimationBatch _batch;
    private int _freeCount, _dirtyCount;
    private bool _presenting;
    public int TargetCount => _targets.Length - _freeCount;
    public AnimationGeneration Generation => _batch.Generation;
    public int AnimationCount => _batch.RegisteredCount;

    public SceneAnimationAdapter(int capacity)
    {
        if (capacity <= 0 || capacity > int.MaxValue / 4) throw new ArgumentOutOfRangeException(nameof(capacity));
        _targets = new Target[capacity]; _versions = new ulong[capacity];
        _free = new int[capacity]; _dirty = new int[capacity]; _identities = new(capacity);
        _batch = new(checked(capacity * 4));
        FillFree();
    }

    public SceneAnimationTargetHandle Register(Node3D node) => Register(node, TargetKind.Spatial);
    public SceneAnimationTargetHandle Register(CanvasItem item) => Register(item, TargetKind.Canvas);
    public SceneAnimationTargetHandle Register(StandardMaterial3D material) => Register(material, TargetKind.Material);

    private SceneAnimationTargetHandle Register(GodotObject value, TargetKind kind)
    {
        RequireIdle();
        ArgumentNullException.ThrowIfNull(value);
        if (!GodotObject.IsInstanceValid(value)) throw new ArgumentException("Presentation target has been freed.");
        var identity = new SceneObjectId(value.GetInstanceId());
        if (_identities.ContainsKey(identity)) throw new ArgumentException("Scene object already has a presentation target identity.");
        if (_freeCount == 0) throw new InvalidOperationException("Presentation target capacity exceeded.");
        var target = new Target { Object = value, Identity = identity, Kind = kind };
        if (kind == TargetKind.Spatial)
        {
            target.Baseline = ((Node3D)value).Transform;
            target.BaselineNodeVisible=target.PendingNodeVisible=((Node3D)value).Visible;
            ValidateTransform(target.Baseline);
            target.PendingTransform = target.Baseline;
        }
        else
        {
            target.BaselineColour = kind == TargetKind.Canvas
                ? ((CanvasItem)value).Modulate : ((StandardMaterial3D)value).AlbedoColor;
            ValidateColour(target.BaselineColour);
            target.PendingColour = target.PublishedColour = target.BaselineColour;
            target.Alpha = target.BaselineColour.A;
        }
        var index = _free[_freeCount - 1];
        var version = checked(_versions[index] + 1);
        _freeCount--;
        _versions[index] = version; _targets[index] = target; _identities.Add(identity, index);
        return new(this, Generation, new(index), version);
    }

    public AnimationHandle BindRotation(SceneAnimationTargetHandle handle, AnimationDefinition definition, AnimationRotationAxis axis)
    {
        RequireIdle();
        var target = Resolve(handle);
        RequireCosmeticTransform(target);
        if (!Enum.IsDefined(axis)) throw new ArgumentOutOfRangeException(nameof(axis));
        ValidateScale(target, 1, 1);
        var animation = _batch.Register(new(handle.Target, AnimationProperty.LocalRotationAngle), definition);
        if (!target.Visible) _batch.SetVisible(animation, false);
        target.Axis = axis; target.Rotation = animation; target.TransformOwner = TransformOwner.Animation;
        return animation;
    }

    public AnimationHandle BindTranslation(SceneAnimationTargetHandle handle,AnimationDefinition definition,AnimationTranslationAxis axis)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(definition);
        var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        if(Math.Abs(definition.From)>1e6||Math.Abs(definition.To)>1e6)
            throw new ArgumentOutOfRangeException(nameof(definition));
        var direction=TranslationDirection(axis);
        ValidateTransform(new(target.Baseline.Basis,target.Baseline.Origin+direction*(float)definition.From));
        ValidateTransform(new(target.Baseline.Basis,target.Baseline.Origin+direction*(float)definition.To));
        var animation=_batch.Register(new(handle.Target,AnimationProperty.LocalTranslation),definition);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.TranslationAxis=axis;target.Position=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }

    public AnimationHandle BindImpulseTranslation(SceneAnimationTargetHandle handle,AnimationImpulseDefinition definition,
        double displacement,AnimationTranslationAxis axis)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(definition);
        var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        if(!double.IsFinite(displacement)||Math.Abs(displacement)>1e6)throw new ArgumentOutOfRangeException(nameof(displacement));
        var direction=TranslationDirection(axis);
        ValidateTransform(new(target.Baseline.Basis,target.Baseline.Origin+direction*(float)displacement));
        var animation=_batch.RegisterImpulses(new(handle.Target,AnimationProperty.LocalTranslation),definition,0,displacement);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.TranslationAxis=axis;target.Position=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }
    public AnimationHandle BindImpulseScale(SceneAnimationTargetHandle handle,AnimationImpulseDefinition definition,double peakScale)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(definition);
        var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!double.IsFinite(peakScale))throw new ArgumentOutOfRangeException(nameof(peakScale));
        ValidateScale(target,1,peakScale);
        var basis=target.Baseline.Basis;var peak=(float)peakScale;
        ValidateTransform(new(new(basis.X*peak,basis.Y*peak,basis.Z*peak),target.Baseline.Origin));
        var animation=_batch.RegisterImpulses(new(handle.Target,AnimationProperty.UniformScale),definition,1,peakScale);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.Scaling=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }

    public AnimationHandle BindOscillatingRotation(SceneAnimationTargetHandle handle,AnimationOscillationDefinition definition,AnimationRotationAxis axis)
    {
        RequireIdle();var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        ValidateScale(target,1,1);
        var animation=_batch.RegisterOscillation(new(handle.Target,AnimationProperty.LocalRotationAngle),definition);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.Axis=axis;target.Rotation=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }
    public AnimationHandle BindOscillatingTranslation(SceneAnimationTargetHandle handle,AnimationOscillationDefinition definition,AnimationTranslationAxis axis)
    {
        RequireIdle();var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        var animation=_batch.RegisterOscillation(new(handle.Target,AnimationProperty.LocalTranslation),definition);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.TranslationAxis=axis;target.Position=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }
    public void ValidateOscillationKick(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double clockTime)
    {RequireIdle();_batch.ValidateOscillationKick(handle,occurrence,strength,clockTime);}
    public void KickOscillation(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double clockTime)
    {RequireIdle();_batch.KickOscillation(handle,occurrence,strength,clockTime);}
    public AnimationOscillationRead ReadOscillation(AnimationHandle handle)=>_batch.ReadOscillation(handle);

    public AnimationHandle BindFollowingRotation(SceneAnimationTargetHandle handle,AnimationFollowDefinition definition,AnimationRotationAxis axis)
    {
        RequireIdle();
        var target=Resolve(handle);RequireCosmeticTransform(target);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        ValidateScale(target,1,1);
        var animation=_batch.RegisterFollow(new(handle.Target,AnimationProperty.LocalRotationAngle),definition);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.Axis=axis;target.Rotation=animation;target.TransformOwner=TransformOwner.Animation;
        return animation;
    }

    public AnimationHandle BindScale(SceneAnimationTargetHandle handle, AnimationDefinition definition)
    {
        RequireIdle();
        ArgumentNullException.ThrowIfNull(definition);
        var target = Resolve(handle);
        RequireCosmeticTransform(target);
        ValidateScale(target, definition.From, definition.To);
        var animation = _batch.Register(new(handle.Target, AnimationProperty.UniformScale), definition);
        if (!target.Visible) _batch.SetVisible(animation, false);
        target.Scaling = animation; target.TransformOwner = TransformOwner.Animation;
        return animation;
    }

    public AnimationHandle BindOpacity(SceneAnimationTargetHandle handle, AnimationDefinition definition)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.Kind == TargetKind.Spatial) throw new ArgumentException("Opacity requires a canvas or material target.");
        if (target.ColourOwner == ColourOwner.Committed) throw new InvalidOperationException("Colour already has a committed writer.");
        if (target.Kind == TargetKind.Material && ((StandardMaterial3D)target.Object).Transparency != BaseMaterial3D.TransparencyEnum.Alpha)
            throw new ArgumentException("Material opacity requires declared alpha transparency.");
        var animation = _batch.Register(new(handle.Target, AnimationProperty.Opacity), definition);
        if (!target.Visible) _batch.SetVisible(animation, false);
        target.Opacity = animation;
        if(target.ColourOwner!=ColourOwner.RgbFollow)target.ColourOwner = ColourOwner.Animation;
        return animation;
    }

    /// <summary>Animate RGB independently of opacity; endpoints retain the target's baseline alpha.</summary>
    public AnimationHandle BindColour(SceneAnimationTargetHandle handle, AnimationDefinition definition, Color from, Color to)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.Kind == TargetKind.Spatial) throw new ArgumentException("Colour requires a canvas or material target.");
        if (target.ColourOwner is ColourOwner.Committed or ColourOwner.RgbFollow) throw new InvalidOperationException("Colour already has an exclusive writer.");
        ValidateColourEndpoints(target,from,to);
        var animation = _batch.Register(new(handle.Target, AnimationProperty.ColourBlend), definition);
        if (!target.Visible) _batch.SetVisible(animation, false);
        target.FromColour = from; target.ToColour = to; target.Colour = animation; target.ColourOwner = ColourOwner.Animation;
        return animation;
    }

    public AnimationHandle BindFollowingColour(SceneAnimationTargetHandle handle,AnimationFollowDefinition definition,Color from,Color to)
    {
        RequireIdle();var target=Resolve(handle);
        if(target.Kind==TargetKind.Spatial)throw new ArgumentException("Colour requires a canvas or material target.");
        if(target.ColourOwner is ColourOwner.Committed or ColourOwner.RgbFollow)throw new InvalidOperationException("Colour already has an exclusive writer.");
        ValidateColourEndpoints(target,from,to);
        var animation=_batch.RegisterFollow(new(handle.Target,AnimationProperty.ColourBlend),definition);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.FromColour=from;target.ToColour=to;target.Colour=animation;target.ColourOwner=ColourOwner.Animation;
        return animation;
    }

    /// <summary>Three normalized RGB followers with one material submission; opacity remains independent.</summary>
    public void BindFollowingRgb(SceneAnimationTargetHandle handle,double response,AnimationClock clock)
    {
        RequireIdle();var target=Resolve(handle);
        if(target.Kind==TargetKind.Spatial||target.Colour is not null||
            target.ColourOwner is ColourOwner.Committed or ColourOwner.RgbFollow)
            throw new InvalidOperationException("RGB following requires an unclaimed RGB writer.");
        ValidateNormalizedRgb(target.BaselineColour);
        var red=new AnimationFollowDefinition(0,1,target.BaselineColour.R,response,clock);
        var green=new AnimationFollowDefinition(0,1,target.BaselineColour.G,response,clock);
        var blue=new AnimationFollowDefinition(0,1,target.BaselineColour.B,response,clock);
        target.Red=_batch.RegisterFollow(new(handle.Target,AnimationProperty.ColourRed),red);
        target.Green=_batch.RegisterFollow(new(handle.Target,AnimationProperty.ColourGreen),green);
        target.Blue=_batch.RegisterFollow(new(handle.Target,AnimationProperty.ColourBlue),blue);
        target.FollowedColour=target.BaselineColour;target.ColourOwner=ColourOwner.RgbFollow;
        SetVisible(handle,target.Visible);
    }

    public void FollowRgb(SceneAnimationTargetHandle handle,Color colour,double clockTime)
    {
        RequireIdle();var target=Resolve(handle);
        if(target.ColourOwner!=ColourOwner.RgbFollow)throw new InvalidOperationException("Target has no RGB follower.");
        ValidateNormalizedRgb(colour);
        if(colour.A!=target.BaselineColour.A)throw new ArgumentException("RGB feedback must preserve baseline alpha.");
        _batch.ValidateFollowValue(target.Red!.Value,colour.R,clockTime);
        _batch.ValidateFollowValue(target.Green!.Value,colour.G,clockTime);
        _batch.ValidateFollowValue(target.Blue!.Value,colour.B,clockTime);
        _batch.FollowValue(target.Red.Value,colour.R,clockTime);
        _batch.FollowValue(target.Green.Value,colour.G,clockTime);
        _batch.FollowValue(target.Blue.Value,colour.B,clockTime);
    }

    private static void ValidateNormalizedRgb(Color colour)
    {
        ValidateColour(colour);
        if(colour.R<0||colour.R>1||colour.G<0||colour.G>1||colour.B<0||colour.B>1)
            throw new ArgumentException("RGB followers require normalized channels.");
    }

    public AnimationHandle BindImpulseColour(SceneAnimationTargetHandle handle,AnimationImpulseDefinition definition,Color from,Color to)
    {
        RequireIdle();
        var target=Resolve(handle);
        if(target.Kind==TargetKind.Spatial)throw new ArgumentException("Colour requires a canvas or material target.");
        if(target.ColourOwner is ColourOwner.Committed or ColourOwner.RgbFollow)throw new InvalidOperationException("Colour already has an exclusive writer.");
        ValidateColourEndpoints(target,from,to);
        var animation=_batch.RegisterImpulses(new(handle.Target,AnimationProperty.ColourBlend),definition,0,1);
        if(!target.Visible)_batch.SetVisible(animation,false);
        target.FromColour=from;target.ToColour=to;target.Colour=animation;target.ColourOwner=ColourOwner.Animation;
        return animation;
    }

    private static void ValidateColourEndpoints(Target target,Color from,Color to)
    {
        ValidateColour(from);ValidateColour(to);
        if(from.A!=target.BaselineColour.A||to.A!=target.BaselineColour.A||
            !float.IsFinite(to.R-from.R)||!float.IsFinite(to.G-from.G)||!float.IsFinite(to.B-from.B))
            throw new ArgumentException("Colour endpoints must preserve baseline alpha and have a finite blend range.");
    }

    /// <summary>Reserve a cosmetic transform for direct committed scalar feedback, without a clip clock.</summary>
    public void ClaimCommittedRotation(SceneAnimationTargetHandle handle, AnimationRotationAxis axis)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.Kind != TargetKind.Spatial || target.TransformOwner != TransformOwner.Unclaimed)
            throw new InvalidOperationException("Committed rotation requires an unclaimed cosmetic transform.");
        if (!Enum.IsDefined(axis)) throw new ArgumentOutOfRangeException(nameof(axis));
        ValidateScale(target, 1, 1);
        target.Axis = axis; target.TransformOwner = TransformOwner.CommittedRotation;
    }

    public void ValidateCommittedRotation(SceneAnimationTargetHandle handle, double radians)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.TransformOwner != TransformOwner.CommittedRotation)
            throw new InvalidOperationException("Target has no committed rotation writer.");
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        ValidateTransform(Compose(target, radians));
    }

    public void QueueCommittedRotation(SceneAnimationTargetHandle handle, double radians)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.TransformOwner != TransformOwner.CommittedRotation)
            throw new InvalidOperationException("Target has no committed rotation writer.");
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        var pose = Compose(target, radians);
        ValidateTransform(pose);
        target.PendingTransform = pose;
        MarkDirty(handle.Target.Index);
    }

    /// <summary>Reserve RGBA for direct committed feedback. Clip RGB and opacity cannot share it.</summary>
    public void ClaimCommittedColour(SceneAnimationTargetHandle handle)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.Kind == TargetKind.Spatial || target.ColourOwner != ColourOwner.Unclaimed)
            throw new InvalidOperationException("Committed colour requires an unclaimed canvas or material colour.");
        target.ColourOwner = ColourOwner.Committed;
    }

    public void QueueCommittedColour(SceneAnimationTargetHandle handle, Color colour)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.ColourOwner != ColourOwner.Committed)
            throw new InvalidOperationException("Target has no committed colour writer.");
        ValidateColour(colour);
        target.PendingColour = colour;
        MarkDirty(handle.Target.Index);
    }

    public void ClaimScalarExtent(SceneAnimationTargetHandle handle,ScalarExtentDefinition definition)
    {
        RequireIdle();ArgumentNullException.ThrowIfNull(definition);
        var target=Resolve(handle);
        if(target.Kind!=TargetKind.Spatial||target.TransformOwner!=TransformOwner.Unclaimed)
            throw new InvalidOperationException("Scalar extent requires an unclaimed cosmetic transform.");
        _=definition.Compose(target.Baseline,0);_=definition.Compose(target.Baseline,1);
        target.Extent=definition;target.TransformOwner=TransformOwner.ScalarExtent;
    }
    private Target ExtentTarget(SceneAnimationTargetHandle handle)
    {
        RequireIdle();var target=Resolve(handle);
        if(target.TransformOwner!=TransformOwner.ScalarExtent)throw new InvalidOperationException("Target has no scalar extent writer.");
        return target;
    }
    public void ValidateScalarExtent(SceneAnimationTargetHandle handle,double fraction)
    {
        var target=ExtentTarget(handle);_=target.Extent!.Compose(target.Baseline,fraction);
    }
    public void QueueScalarExtent(SceneAnimationTargetHandle handle,double fraction,bool visible)
    {
        var target=ExtentTarget(handle);
        target.PendingTransform=target.Extent!.Compose(target.Baseline,fraction);
        target.PendingNodeVisible=visible;MarkDirty(handle.Target.Index);
    }

    public void ClaimPhysicalPose(SceneAnimationTargetHandle handle)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.Kind != TargetKind.Spatial || target.TransformOwner != TransformOwner.Unclaimed)
            throw new InvalidOperationException("Physical pose requires an unclaimed spatial transform.");
        target.TransformOwner = TransformOwner.Physical;
    }

    public void ValidatePhysicalPose(SceneAnimationTargetHandle handle, Transform3D pose)
    {
        RequireIdle();
        var target = Resolve(handle);
        if (target.TransformOwner != TransformOwner.Physical)
            throw new InvalidOperationException("Target has no physical-pose writer.");
        ValidateTransform(pose);
    }

    public void QueuePhysicalPose(SceneAnimationTargetHandle handle, Transform3D pose)
    {
        ValidatePhysicalPose(handle, pose);
        var target = Resolve(handle);
        target.PendingTransform = pose;
        MarkDirty(handle.Target.Index);
    }

    public void Start(AnimationHandle handle) { RequireIdle(); _batch.Start(handle); }
    public void StartAt(AnimationHandle handle, double clockTime) { RequireIdle(); _batch.StartAt(handle, clockTime); }
    public void DriveTo(AnimationHandle handle, AnimationEndpoint endpoint, double clockTime) { RequireIdle(); _batch.DriveTo(handle, endpoint, clockTime); }
    public void FollowValue(AnimationHandle handle,double target,double clockTime) { RequireIdle(); _batch.FollowValue(handle,target,clockTime); }
    public void SetLoopRate(AnimationHandle handle, double rate, double clockTime) { RequireIdle(); _batch.SetLoopRate(handle, rate, clockTime); }
    public void Stop(AnimationHandle handle, AnimationStop policy, double clockTime) { RequireIdle(); _batch.Stop(handle, policy, clockTime); }
    public AnimationRead Read(AnimationHandle handle) => _batch.Read(handle);
    public AnimationImpulseAdmission ValidateImpulseAdmission(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double occurredAt)
    { RequireIdle();return _batch.ValidateImpulseAdmission(handle,occurrence,strength,occurredAt); }
    public AnimationImpulseAdmission EnqueueImpulse(AnimationHandle handle,AnimationOccurrenceId occurrence,double strength,double occurredAt)
    { RequireIdle();return _batch.EnqueueImpulse(handle,occurrence,strength,occurredAt); }
    public AnimationImpulseRead ReadImpulses(AnimationHandle handle)=>_batch.ReadImpulses(handle);
    public void CancelImpulses(AnimationHandle handle) { RequireIdle();_batch.CancelImpulses(handle); }

    public void SetVisible(SceneAnimationTargetHandle handle, bool visible)
    {
        RequireIdle();
        var target = Resolve(handle);
        target.Visible = visible;
        if (visible && IsCommittedFeedback(target)) MarkDirty(handle.Target.Index);
        if (target.Rotation is { } rotation) _batch.SetVisible(rotation, visible);
        if (target.Scaling is { } scale) _batch.SetVisible(scale, visible);
        if(target.Position is { } position)_batch.SetVisible(position,visible);
        if (target.Opacity is { } opacity) _batch.SetVisible(opacity, visible);
        if (target.Colour is { } colour) _batch.SetVisible(colour, visible);
        if(target.Red is { } red)_batch.SetVisible(red,visible);
        if(target.Green is { } green)_batch.SetVisible(green,visible);
        if(target.Blue is { } blue)_batch.SetVisible(blue,visible);
    }

    public void RemoveAnimation(AnimationHandle handle)
    {
        RequireIdle();
        var binding = _batch.Binding(handle);
        var target = _targets[binding.Target.Index]!;
        RequireLive(target);
        _batch.Remove(handle);
        switch (binding.Property)
        {
            case AnimationProperty.LocalRotationAngle: target.Rotation = null; target.Angle = 0; break;
            case AnimationProperty.UniformScale: target.Scaling = null; target.Scale = 1; break;
            case AnimationProperty.LocalTranslation:target.Position=null;target.Translation=0;break;
            case AnimationProperty.Opacity:
                target.Opacity = null; target.Alpha = target.BaselineColour.A; break;
            case AnimationProperty.ColourBlend: target.Colour = null; target.Blend = 0; break;
            default: throw new InvalidOperationException("Unsupported animation property.");
        }
        if (target.Kind == TargetKind.Spatial)
        {
            if (target.Rotation is null && target.Scaling is null && target.Position is null) target.TransformOwner = TransformOwner.Unclaimed;
        }
        if (target.Colour is null && target.Opacity is null && target.ColourOwner == ColourOwner.Animation)
            target.ColourOwner = ColourOwner.Unclaimed;
        MarkDirty(binding.Target.Index);
    }

    public void RemoveTarget(SceneAnimationTargetHandle handle, AnimationTargetRemoval policy)
    {
        RequireIdle();
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        var target = Resolve(handle, requireLive: policy == AnimationTargetRemoval.RestoreBaseline);
        _presenting = true;
        try
        {
            if (policy == AnimationTargetRemoval.RestoreBaseline) RestoreBaseline(target);
            RemoveAnimations(target);
            UnmarkDirty(handle.Target.Index);
            _identities.Remove(target.Identity); _targets[handle.Target.Index] = null;
            _free[_freeCount++] = handle.Target.Index;
        }
        finally { _presenting = false; }
    }

    public SceneAnimationFrameWork Present(ulong frame, double presentationTime, double simulationTime
#if PLAYTEST
        ,PerformanceRecorder? performance=null
#endif
    )
    {
        RequireIdle();
        // Validate lifetime before advancing clip state, so a freed target cannot consume
        // the final value of a once-clip. Detach is an explicit recovery/removal operation.
        for (var i = 0; i < _batch.ScheduledCount; i++)
            RequireLive(_targets[_batch.ScheduledBindingAt(i).Target.Index]!);
        for (var i = 0; i < _dirtyCount; i++) RequireLive(_targets[_dirty[i]]!);
        _presenting = true;
        try
        {
            var work = _batch.Advance(frame, presentationTime, simulationTime);
            foreach (var sample in _batch.Samples)
            {
                var target = _targets[sample.Binding.Target.Index]!;
                switch (sample.Binding.Property)
                {
                    case AnimationProperty.LocalRotationAngle: target.Angle = sample.Value; break;
                    case AnimationProperty.UniformScale: target.Scale = sample.Value; break;
                    case AnimationProperty.LocalTranslation:target.Translation=sample.Value;break;
                    case AnimationProperty.Opacity:
                        target.Alpha = sample.Value; break;
                    case AnimationProperty.ColourBlend: target.Blend = sample.Value; break;
                    case AnimationProperty.ColourRed: target.FollowedColour.R=(float)sample.Value; break;
                    case AnimationProperty.ColourGreen: target.FollowedColour.G=(float)sample.Value; break;
                    case AnimationProperty.ColourBlue: target.FollowedColour.B=(float)sample.Value; break;
                    default: throw new InvalidOperationException("Unsupported animation property.");
                }
                MarkDirty(sample.Binding.Target.Index);
            }
            // Compose each dirty target once, then validate the entire outgoing batch
            // before the first scene write. Physical targets retain their queued pose.
            for (var i = 0; i < _dirtyCount; i++)
            {
                var target = _targets[_dirty[i]]!;
                if (target.Kind == TargetKind.Spatial)
                {
                    if (target.TransformOwner is TransformOwner.Unclaimed or TransformOwner.Animation) target.PendingTransform = Compose(target);
                    ValidateTransform(target.PendingTransform);
                }
                else
                {
                    if (target.ColourOwner != ColourOwner.Committed)
                    {
                        target.PendingColour = target.ColourOwner==ColourOwner.RgbFollow ? target.FollowedColour
                            : target.Colour is null ? target.BaselineColour
                            : target.FromColour.Lerp(target.ToColour, (float)target.Blend);
                        target.PendingColour.A = (float)target.Alpha;
                    }
                    ValidateColour(target.PendingColour);
                }
            }
#if PLAYTEST
            performance?.Begin(PerformanceStage.SceneSubmission);
#endif
            var transforms = 0;
            var colours = 0;
            var visibility=0;
            for (var i = 0; i < _dirtyCount; i++)
            {
                var target = _targets[_dirty[i]]!;
                if (!target.Visible && IsCommittedFeedback(target))
                {
                    target.DirtyIndex = -1;
                    continue;
                }
                if (target.Kind == TargetKind.Spatial)
                {
                    if (target.PendingTransform != ((Node3D)target.Object).Transform)
                    {
                        ((Node3D)target.Object).Transform = target.PendingTransform;
                        transforms++;
                    }
                    if(target.Extent is not null&&((Node3D)target.Object).Visible!=target.PendingNodeVisible)
                    {((Node3D)target.Object).Visible=target.PendingNodeVisible;visibility++;}
                }
                else if (target.PendingColour != target.PublishedColour)
                {
                    SetColour(target, target.PendingColour);
                    target.PublishedColour = target.PendingColour; colours++;
                }
                target.DirtyIndex = -1;
            }
            _dirtyCount = 0;
#if PLAYTEST
            performance?.End(PerformanceStage.SceneSubmission);
#endif
            return new(work, transforms, colours, visibility);
        }
        finally { _presenting = false; }
    }

    public void Reset()
    {
        RequireIdle();
        foreach (var target in _targets) if (target is not null) RequireLive(target);
        _presenting = true;
        try
        {
            foreach (var target in _targets) if (target is not null) RestoreBaseline(target);
            _batch.Reset();
            Array.Clear(_targets); Array.Clear(_versions); _identities.Clear();
            _dirtyCount = 0; FillFree();
        }
        finally { _presenting = false; }
    }

    private static bool IsCommittedFeedback(Target target) =>
        target.TransformOwner == TransformOwner.CommittedRotation || target.ColourOwner == ColourOwner.Committed;

    private static Transform3D Compose(Target target) => Compose(target, target.Angle);
    private static Transform3D Compose(Target target, double angle)
    {
        var axis = target.Axis switch
        {
            AnimationRotationAxis.X => Vector3.Right,
            AnimationRotationAxis.Y => Vector3.Up,
            AnimationRotationAxis.Z => Vector3.Back,
            _ => throw new InvalidOperationException("Unsupported animation axis.")
        };
        var rotation = new Basis(axis, (float)Math.IEEERemainder(angle, Math.Tau));
        var basis = target.Baseline.Basis * rotation;
        var scale = (float)target.Scale;
        return new(new(basis.X * scale, basis.Y * scale, basis.Z * scale),
            target.Baseline.Origin+TranslationDirection(target.TranslationAxis)*(float)target.Translation);
    }
    private static Vector3 TranslationDirection(AnimationTranslationAxis axis)=>axis switch
    {
        AnimationTranslationAxis.X=>Vector3.Right,AnimationTranslationAxis.Y=>Vector3.Up,AnimationTranslationAxis.Z=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    private static void ValidateScale(Target target, double from, double to)
    {
        // Supported cosmetic scale envelope keeps float conversion and normal toy-scene
        // transforms representable. Reject outside it; never silently clamp a clip.
        if (!double.IsFinite(from) || !double.IsFinite(to) || from < 1e-6 || to < 1e-6 || from > 1e6 || to > 1e6)
            throw new ArgumentOutOfRangeException(nameof(from));
        var basis = target.Baseline.Basis;
        static float Maximum(Vector3 value) => Math.Max(Math.Abs(value.X), Math.Max(Math.Abs(value.Y), Math.Abs(value.Z)));
        var maximum = Math.Max(Maximum(basis.X), Math.Max(Maximum(basis.Y), Maximum(basis.Z)));
        if ((double)maximum * Math.Max(from, to) * 4 > float.MaxValue)
            throw new ArgumentException("Cosmetic transform exceeds float rendering range.");
    }
    internal static void ValidateTransform(Transform3D transform)
    {
        if (!transform.Origin.IsFinite() || !transform.Basis.X.IsFinite() ||
            !transform.Basis.Y.IsFinite() || !transform.Basis.Z.IsFinite() ||
            !float.IsFinite(transform.Basis.Determinant()) || transform.Basis.Determinant() == 0)
            throw new ArgumentException("Presentation transform must be finite and invertible.");
    }
    internal static void ValidateColour(Color colour)
    {
        if (!float.IsFinite(colour.R) || !float.IsFinite(colour.G) || !float.IsFinite(colour.B) ||
            !float.IsFinite(colour.A) || colour.A < 0 || colour.A > 1)
            throw new ArgumentException("Presentation colour must be finite with bounded alpha.");
    }
    private static void RequireCosmeticTransform(Target target)
    {
        if (target.Kind != TargetKind.Spatial || target.TransformOwner is TransformOwner.Physical or TransformOwner.ScalarExtent or TransformOwner.CommittedRotation)
            throw new InvalidOperationException("Cosmetics require a spatial target without a physical-pose writer.");
    }
    private static void RequireLive(Target target)
    {
        if (!GodotObject.IsInstanceValid(target.Object)) throw new InvalidOperationException("Presentation target was freed without detachment.");
        if (target.Opacity is not null && target.Kind == TargetKind.Material &&
            ((StandardMaterial3D)target.Object).Transparency != BaseMaterial3D.TransparencyEnum.Alpha)
            throw new InvalidOperationException("Bound opacity target no longer has alpha transparency.");
    }
    private static void SetColour(Target target, Color colour)
    {
        if (target.Kind == TargetKind.Canvas) ((CanvasItem)target.Object).Modulate = colour;
        else if (target.Kind == TargetKind.Material) ((StandardMaterial3D)target.Object).AlbedoColor = colour;
        else throw new InvalidOperationException("Spatial target has no colour writer.");
    }
    private static void RestoreBaseline(Target target)
    {
        if (target.Kind == TargetKind.Spatial)
        {
            ((Node3D)target.Object).Transform = target.Baseline;
            if(target.Extent is not null)((Node3D)target.Object).Visible=target.BaselineNodeVisible;
        }
        else SetColour(target, target.BaselineColour);
    }
    private void RemoveAnimations(Target target)
    {
        if (target.Rotation is { } rotation) _batch.Remove(rotation);
        if (target.Scaling is { } scale) _batch.Remove(scale);
        if(target.Position is { } position)_batch.Remove(position);
        if (target.Opacity is { } opacity) _batch.Remove(opacity);
        if (target.Colour is { } colour) _batch.Remove(colour);
        if(target.Red is { } red)_batch.Remove(red);
        if(target.Green is { } green)_batch.Remove(green);
        if(target.Blue is { } blue)_batch.Remove(blue);
    }
    private Target Resolve(SceneAnimationTargetHandle handle, bool requireLive = true)
    {
        if (!ReferenceEquals(handle.Owner, this) || handle.Generation != Generation ||
            handle.Target.Index >= _targets.Length || _targets[handle.Target.Index] is not { } target ||
            handle.Version != _versions[handle.Target.Index])
            throw new ArgumentException("Presentation target handle is stale or foreign.");
        if (requireLive) RequireLive(target);
        return target;
    }
    private void RequireIdle()
    {
        if (_presenting) throw new InvalidOperationException("Presentation bindings cannot mutate during submission.");
    }
    private void MarkDirty(int index)
    {
        var target = _targets[index]!;
        if (target.DirtyIndex >= 0) return;
        target.DirtyIndex = _dirtyCount;
        _dirty[_dirtyCount++] = index;
    }
    private void UnmarkDirty(int index)
    {
        var target = _targets[index]!;
        if (target.DirtyIndex < 0) return;
        var last = _dirty[--_dirtyCount];
        _dirty[target.DirtyIndex] = last;
        _targets[last]!.DirtyIndex = target.DirtyIndex;
        target.DirtyIndex = -1;
    }
    private void FillFree()
    {
        _freeCount = _targets.Length;
        for (var i = 0; i < _free.Length; i++) _free[i] = _free.Length - 1 - i;
    }
}
