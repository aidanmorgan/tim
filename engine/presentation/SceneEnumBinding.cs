using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

/// <summary>Complete, immutable enum mapping. Domain values never become numeric selectors.</summary>
public sealed class EnumPresentationMap<T,TValue> where T:unmanaged,Enum
{
    private readonly Dictionary<T,TValue> _values;
    public EnumPresentationMap(IReadOnlyDictionary<T,TValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);_values=new(values);
        foreach(var key in _values.Keys)
            if(!Enum.IsDefined(key))throw new ArgumentException("Undefined presentation state.");
        foreach(var key in Enum.GetValues<T>())
            if(!_values.ContainsKey(key))throw new ArgumentException("Every presentation state requires an explicit mapping.");
    }
    public TValue Read(T state)=>_values.TryGetValue(state,out var value)?value:
        throw new ArgumentException("Unsupported presentation state.");
    internal IEnumerable<TValue> Values=>_values.Values;
}

internal readonly record struct EnumMaterialId(ulong Value);

public abstract class SceneEnumBinding
{
    public Node3D Target { get; }
    private protected SceneEnumBinding(Node3D target)
    {ArgumentNullException.ThrowIfNull(target);Target=target;}
    internal abstract BoundEnumPresentation Bind(MachinePart owner,ScenePhysicsAssembly assembly,
        SceneAnimationAdapter adapter,PoseReadLease seed,IReadOnlyList<Node3D> physicalTargets,
        IReadOnlyDictionary<EnumMaterialId,int> materials);
}

public sealed class SceneEnumRotation<T>:SceneEnumBinding where T:unmanaged,Enum
{
    public SceneEnumObservationKey<T> Source { get; }
    public EnumPresentationMap<T,double> Angles { get; }
    public AnimationRotationAxis Axis { get; }
    public SceneEnumRotation(Node3D target,SceneEnumObservationKey<T> source,
        EnumPresentationMap<T,double> angles,AnimationRotationAxis axis):base(target)
    {
        ArgumentNullException.ThrowIfNull(angles);
        if(!Enum.IsDefined(axis))throw new ArgumentOutOfRangeException(nameof(axis));
        foreach(var angle in angles.Values)
            if(!double.IsFinite(angle))throw new ArgumentException("Rotation angles must be finite.");
        Source=source;Angles=angles;Axis=axis;
    }
    internal override BoundEnumPresentation Bind(MachinePart owner,ScenePhysicsAssembly assembly,
        SceneAnimationAdapter adapter,PoseReadLease seed,IReadOnlyList<Node3D> physicalTargets,
        IReadOnlyDictionary<EnumMaterialId,int> materials)
    {
        if(Source.Owner!=owner)throw new ArgumentException("Enum feedback requires an owned source.");
        foreach(var physical in physicalTargets)
            if(Target==physical||Target.IsAncestorOf(physical))throw new InvalidOperationException("Enum rotation cannot own physical geometry.");
        var key=new EnumReadKey<T>(assembly.QueryOwnerId(new(owner,MachinePart.RootBody)),Source.Slot);
        _=seed.ReadEnum(PoseSample.Current,key);
        var handle=adapter.Register(Target);adapter.ClaimCommittedRotation(handle,Axis);
        foreach(var angle in Angles.Values)adapter.ValidateCommittedRotation(handle,angle);
        return new BoundEnumRotation<T>(Target,key,Angles,adapter,handle);
    }
}

public sealed class SceneEnumColour<T>:SceneEnumBinding where T:unmanaged,Enum
{
    public SceneEnumObservationKey<T> Source { get; }
    public EnumPresentationMap<T,Color> Colours { get; }
    public SceneEnumColour(MeshInstance3D target,SceneEnumObservationKey<T> source,
        EnumPresentationMap<T,Color> colours):base(target)
    {
        ArgumentNullException.ThrowIfNull(colours);
        foreach(var colour in colours.Values)SceneAnimationAdapter.ValidateColour(colour);
        Source=source;Colours=colours;
    }
    internal override BoundEnumPresentation Bind(MachinePart owner,ScenePhysicsAssembly assembly,
        SceneAnimationAdapter adapter,PoseReadLease seed,IReadOnlyList<Node3D> physicalTargets,
        IReadOnlyDictionary<EnumMaterialId,int> materials)
    {
        if(Source.Owner!=owner)throw new ArgumentException("Enum feedback requires an owned source.");
        var node=(MeshInstance3D)Target;
        if(node.MaterialOverride is not StandardMaterial3D material||
            !materials.TryGetValue(new(material.GetInstanceId()),out var uses)||uses!=1)
            throw new ArgumentException("Enum colour requires an exclusive standard material.");
        var key=new EnumReadKey<T>(assembly.QueryOwnerId(new(owner,MachinePart.RootBody)),Source.Slot);
        _=seed.ReadEnum(PoseSample.Current,key);
        var handle=adapter.Register(material);adapter.ClaimCommittedColour(handle);
        return new BoundEnumColour<T>(node,material,key,Colours,adapter,handle);
    }
}

internal abstract class BoundEnumPresentation
{
    internal abstract void Stage(PoseReadLease read);
    internal abstract void Commit();
    internal abstract void Queue();
}
internal abstract class BoundEnumPresentation<T,TValue>(Node3D node,EnumReadKey<T> key,
    EnumPresentationMap<T,TValue> map):BoundEnumPresentation where T:unmanaged,Enum
{
    private readonly Node _parent=node.GetParent();
    private TValue _pending=default!,_current=default!,_last=default!;
    private bool _queued;
    internal override void Stage(PoseReadLease read)=>_pending=map.Read(read.ReadEnum(PoseSample.Current,key).Value);
    internal override void Commit()=>_current=_pending;
    protected virtual void ValidateTarget()
    {
        if(!GodotObject.IsInstanceValid(node)||!GodotObject.IsInstanceValid(_parent)||node.GetParent()!=_parent)
            throw new InvalidOperationException("Enum presentation target binding changed.");
    }
    protected abstract void Submit(TValue value);
    internal override void Queue()
    {
        ValidateTarget();
        if(!node.IsVisibleInTree()||(_queued&&EqualityComparer<TValue>.Default.Equals(_current,_last)))return;
        Submit(_current);_last=_current;_queued=true;
    }
}
internal sealed class BoundEnumRotation<T>(Node3D node,EnumReadKey<T> key,EnumPresentationMap<T,double> map,
    SceneAnimationAdapter adapter,SceneAnimationTargetHandle handle):BoundEnumPresentation<T,double>(node,key,map)
    where T:unmanaged,Enum
{
    protected override void Submit(double value)=>adapter.QueueCommittedRotation(handle,value);
}
internal sealed class BoundEnumColour<T>(MeshInstance3D node,StandardMaterial3D material,EnumReadKey<T> key,
    EnumPresentationMap<T,Color> map,SceneAnimationAdapter adapter,SceneAnimationTargetHandle handle):
    BoundEnumPresentation<T,Color>(node,key,map) where T:unmanaged,Enum
{
    protected override void ValidateTarget()
    {
        base.ValidateTarget();
        if(node.MaterialOverride!=material)throw new InvalidOperationException("Enum colour material binding changed.");
    }
    protected override void Submit(Color value)=>adapter.QueueCommittedColour(handle,value);
}
