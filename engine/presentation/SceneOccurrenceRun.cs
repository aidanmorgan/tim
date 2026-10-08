using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

public sealed class SceneOccurrenceSlot { }
public readonly record struct SceneOccurrenceKey(MachinePart Owner,SceneOccurrenceSlot Slot);
public enum SceneOccurrencePhase { Idle, Writing, Rejected, Staged, Committed, Faulted, Removed }
public enum SceneOccurrenceChannel { Scale, Translation, Colour }
public sealed class SceneOccurrenceAnimation
{
    public MeshInstance3D Target { get; }
    public SceneOccurrenceKey Source { get; }
    public AnimationImpulseDefinition Definition { get; }
    public SceneOccurrenceChannel Channel { get; }
    public double Peak { get; }
    public AnimationTranslationAxis Axis { get; }
    public Color From { get; }
    public Color To { get; }
    private SceneOccurrenceAnimation(MeshInstance3D target,SceneOccurrenceKey source,AnimationImpulseDefinition definition,
        SceneOccurrenceChannel channel,double peak,AnimationTranslationAxis axis,Color from,Color to)
    {Target=target;Source=source;Definition=definition;Channel=channel;Peak=peak;Axis=axis;From=from;To=to;}
    public static SceneOccurrenceAnimation Scale(MeshInstance3D target,SceneOccurrenceKey source,AnimationImpulseDefinition definition,double peak)=>
        new(target,source,definition,SceneOccurrenceChannel.Scale,peak,AnimationTranslationAxis.X,default,default);
    public static SceneOccurrenceAnimation Translation(MeshInstance3D target,SceneOccurrenceKey source,AnimationImpulseDefinition definition,double offset,AnimationTranslationAxis axis)=>
        new(target,source,definition,SceneOccurrenceChannel.Translation,offset,axis,default,default);
    public static SceneOccurrenceAnimation Colour(MeshInstance3D target,SceneOccurrenceKey source,AnimationImpulseDefinition definition,Color from,Color to)=>
        new(target,source,definition,SceneOccurrenceChannel.Colour,0,AnimationTranslationAxis.X,from,to);
}
public readonly record struct SceneOccurrenceRead(AnimationRead Animation,AnimationImpulseRead Occurrences);

/// <summary>Run-scoped semantic occurrence publication and typed animation fanout.
/// Reservations reject before physical commit; no animation mutation occurs during a tick.</summary>
public sealed class SceneOccurrenceRun
{
    public static readonly EventStreamId StreamId=new(2);
    public const int Capacity=4096;
    private readonly record struct SourceId(int Index);
    private readonly record struct ObjectId(ulong Value);
    private readonly record struct Pulse(SourceId Source,double Strength);
    private sealed class Source
    {
        public required Binding[] Bindings;
        public int Reserved;
    }
    private readonly record struct Binding(MachinePart Owner,MeshInstance3D Node,Node Parent,StandardMaterial3D? Material,
        SceneAnimationTargetHandle Target,AnimationHandle Animation,AnimationClock Clock,int Capacity);
    private readonly SceneAnimationAdapter _adapter;
    private readonly CommittedEventStream<Pulse> _events;
    private readonly Dictionary<SceneOccurrenceKey,SourceId> _ids=[];
    private readonly Source[] _sources;
    private double _presentationTime,_admissionPresentationTime;
    private PoseReadStamp _pending;
    public Exception? Failure { get; private set; }
    public int UnconfirmedCount=>_events.PendingCount;
    public int HighWaterMark=>_events.HighWaterMark;
    public ulong PublishedCount { get; private set; }
    public SceneOccurrencePhase Phase { get; private set; }
    public SceneOccurrenceRun(SceneAnimationAdapter adapter,IReadOnlyList<MachinePart> parts,
        IReadOnlyList<Node3D> physicalTargets,PoseReadStamp seed)
    {
        ArgumentNullException.ThrowIfNull(adapter);ArgumentNullException.ThrowIfNull(parts);ArgumentNullException.ThrowIfNull(physicalTargets);
        _adapter=adapter;_events=new(StreamId,seed,Capacity);
        var groups=new List<List<Binding>>();
        foreach(var owner in parts)
        foreach(var slot in owner.OccurrenceSources)
        {
            if(slot is null||!_ids.TryAdd(new(owner,slot),new(groups.Count)))throw new ArgumentException("Duplicate or invalid occurrence source.");
            groups.Add([]);
        }
        var targets=new Dictionary<ObjectId,SceneAnimationTargetHandle>();var materialUses=new Dictionary<ObjectId,int>();
        foreach(var part in parts)CountMaterials(part);
        foreach(var owner in parts)
        foreach(var d in owner.OccurrenceAnimations)
        {
            ArgumentNullException.ThrowIfNull(d);ArgumentNullException.ThrowIfNull(d.Definition);
            if(d.Source.Owner!=owner||!_ids.TryGetValue(d.Source,out var source))throw new ArgumentException("Occurrence binding requires an owned declared source.");
            var node=d.Target;
            if(node is null||!GodotObject.IsInstanceValid(node)||!owner.IsAncestorOf(node))
                throw new ArgumentException("Occurrence target must be a live owned child.");
            StandardMaterial3D? material=null;GodotObject resource=node;
            if(d.Channel==SceneOccurrenceChannel.Colour)
            {
                material=node.MaterialOverride as StandardMaterial3D;
                if(material is null||!materialUses.TryGetValue(new(material.GetInstanceId()),out var uses)||uses!=1)
                    throw new ArgumentException("Occurrence colour requires an exclusive material.");
                resource=material;
            }
            else foreach(var physical in physicalTargets)
                if(node==physical||node.IsAncestorOf(physical))throw new InvalidOperationException("Occurrence animation cannot own physical geometry.");
            var identity=new ObjectId(resource.GetInstanceId());
            if(!targets.TryGetValue(identity,out var target)){target=resource is StandardMaterial3D m?adapter.Register(m):adapter.Register(node);targets.Add(identity,target);}
            var animation=d.Channel switch
            {
                SceneOccurrenceChannel.Scale=>adapter.BindImpulseScale(target,d.Definition,d.Peak),
                SceneOccurrenceChannel.Translation=>adapter.BindImpulseTranslation(target,d.Definition,d.Peak,d.Axis),
                SceneOccurrenceChannel.Colour=>adapter.BindImpulseColour(target,d.Definition,d.From,d.To),
                _=>throw new ArgumentOutOfRangeException()
            };
            groups[source.Index].Add(new(owner,node,node.GetParent(),material,target,animation,d.Definition.Clock,d.Definition.Capacity));
        }
        _sources=new Source[groups.Count];
        for(var i=0;i<groups.Count;i++)_sources[i]=new(){Bindings=groups[i].ToArray()};

        void CountMaterials(Node node)
        {
            if(node is MeshInstance3D mesh&&mesh.MaterialOverride is { } material)
            {var id=new ObjectId(material.GetInstanceId());materialUses.TryGetValue(id,out var count);materialUses[id]=checked(count+1);}
            foreach(var child in node.GetChildren())CountMaterials(child);
        }
    }
    private void RequireLive(){if(Phase==SceneOccurrencePhase.Removed)throw new InvalidOperationException("Occurrence run removed.");}
    public void RequireReady()
    {
        RequireLive();if(Phase!=SceneOccurrencePhase.Idle)throw new InvalidOperationException("Occurrence publication is not ready; a faulted run requires Reset or Load.",Failure);
        _events.RequireWritable();
    }
    public void Begin(PoseReadStamp stamp)
    {
        RequireReady();_events.Begin(stamp);_pending=stamp;_admissionPresentationTime=_presentationTime;Phase=SceneOccurrencePhase.Writing;
        foreach(var source in _sources)source.Reserved=0;
    }
    private static void Validate(Binding b)
    {
        if(!GodotObject.IsInstanceValid(b.Owner)||!GodotObject.IsInstanceValid(b.Node)||
            b.Node.GetParent()!=b.Parent||!b.Owner.IsAncestorOf(b.Node)||
            b.Material is not null&&b.Node.MaterialOverride!=b.Material)
            throw new InvalidOperationException("Occurrence target binding changed.");
    }
    private double Time(Binding binding)=>binding.Clock switch
    {
        AnimationClock.Presentation=>_admissionPresentationTime,
        AnimationClock.Simulation=>_pending.SimulationTime,
        _=>throw new InvalidOperationException("Unsupported occurrence clock.")
    };
    public void Emit(SceneOccurrenceKey key,double strength)
    {
        RequireLive();
        if(Phase!=SceneOccurrencePhase.Writing)throw new InvalidOperationException("Occurrences require an active simulation transaction.");
        if(!GodotObject.IsInstanceValid(key.Owner)||!_ids.TryGetValue(key,out var id))throw new ArgumentException("Unknown occurrence source.");
        if(!double.IsFinite(strength)||strength<=0||strength>1)throw new ArgumentOutOfRangeException(nameof(strength));
        var source=_sources[id.Index];
        try
        {
            var occurrence=_events.Append(new(id,strength));var animationId=new AnimationOccurrenceId(occurrence.Sequence.Value);
            foreach(var b in source.Bindings)
            {
                Validate(b);var read=_adapter.ReadImpulses(b.Animation);
                if(read.Pending+read.Playing+source.Reserved>=b.Capacity||
                    _adapter.ValidateImpulseAdmission(b.Animation,animationId,strength,Time(b))!=AnimationImpulseAdmission.Accepted)
                    throw new InvalidOperationException("Occurrence animation capacity exhausted; present or Reset before retry.");
            }
            source.Reserved++;
        }
        catch {Phase=SceneOccurrencePhase.Rejected;throw;}
    }
    public void Seal()
    {
        RequireLive();if(Phase!=SceneOccurrencePhase.Writing)throw new InvalidOperationException("Only an accepted occurrence batch may seal.");
        _events.Seal();Phase=SceneOccurrencePhase.Staged;
    }
    public void Commit()
    {
        RequireLive();if(Phase!=SceneOccurrencePhase.Staged)throw new InvalidOperationException("No staged occurrence tick.");
        _events.Commit();Phase=SceneOccurrencePhase.Committed;
    }
    public void Discard()
    {
        RequireLive();if(Phase is not (SceneOccurrencePhase.Idle or SceneOccurrencePhase.Writing or SceneOccurrencePhase.Rejected or SceneOccurrencePhase.Staged))
            throw new InvalidOperationException("Committed occurrences cannot be discarded.",Failure);
        if(_events.Phase is EventStreamPhase.Writing or EventStreamPhase.Rejected or EventStreamPhase.Staged)_events.Discard();
        foreach(var source in _sources)source.Reserved=0;
        Phase=SceneOccurrencePhase.Idle;
    }
    public void Publish()
    {
        RequireLive();if(Phase!=SceneOccurrencePhase.Committed)throw new InvalidOperationException("No committed occurrence tick to publish.",Failure);
        try
        {
            while(_events.PendingCount>0)
            {
                var e=_events.Peek();var source=_sources[e.Payload.Source.Index];var id=new AnimationOccurrenceId(e.Id.Sequence.Value);
                foreach(var b in source.Bindings)
                {
                    Validate(b);
                    if(_adapter.ValidateImpulseAdmission(b.Animation,id,e.Payload.Strength,Time(b))!=AnimationImpulseAdmission.Accepted)
                        throw new InvalidOperationException("Reserved occurrence admission changed before publication.");
                }
                foreach(var b in source.Bindings)
                    if(_adapter.EnqueueImpulse(b.Animation,id,e.Payload.Strength,Time(b))!=AnimationImpulseAdmission.Accepted)
                        throw new InvalidOperationException("Occurrence admission changed without an intervening writer.");
                _events.Acknowledge(e.Id);PublishedCount=checked(PublishedCount+1);
            }
            foreach(var source in _sources)source.Reserved=0;
            Phase=SceneOccurrencePhase.Idle;
        }
        catch(Exception failure){Failure=failure;Phase=SceneOccurrencePhase.Faulted;throw;}
    }
    public void RefreshVisibility()
    {
        RequireLive();_events.RequireWritable();
        foreach(var source in _sources)
        foreach(var b in source.Bindings){Validate(b);_adapter.SetVisible(b.Target,b.Node.IsVisibleInTree());}
    }
    public void AdvancePresentation(double time)
    {
        RequireLive();_events.RequireWritable();if(!double.IsFinite(time)||time<_presentationTime)throw new ArgumentOutOfRangeException(nameof(time));
        _presentationTime=time;
    }
    public SceneOccurrenceRead Read(SceneOccurrenceKey key,int index)
    {
        RequireLive();
        if(!GodotObject.IsInstanceValid(key.Owner)||!_ids.TryGetValue(key,out var id)||(uint)index>=(uint)_sources[id.Index].Bindings.Length)throw new ArgumentException("Unknown occurrence binding.");
        var animation=_sources[id.Index].Bindings[index].Animation;return new(_adapter.Read(animation),_adapter.ReadImpulses(animation));
    }
    public void Remove(){if(Phase==SceneOccurrencePhase.Removed)return;_events.Remove();Phase=SceneOccurrencePhase.Removed;}
}
