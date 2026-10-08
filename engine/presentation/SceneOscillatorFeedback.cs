using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

public sealed record SceneOscillatorColour(MeshInstance3D Target,SceneOscillatorKey Source,
    AnimationImpulseDefinition Definition,Color From,Color To);

/// <summary>Run-scoped bindings share the central adapter. Events are acknowledged only after
/// every subscriber accepts; finite signed presentation timestamps preserve pre-origin event age.</summary>
internal sealed class SceneOscillatorFeedback
{
    internal static readonly EventStreamId StreamId=new(1);
    private readonly record struct MaterialId(ulong Value);
    private readonly record struct Binding(MeshInstance3D Node,StandardMaterial3D Material,
        SceneAnimationTargetHandle Target,AnimationHandle Animation,AnimationClock Clock);
    private readonly SceneAnimationAdapter _adapter;
    private readonly Dictionary<SimulationOscillatorId,Binding[]> _bindings=new();
    private readonly Dictionary<SceneOscillatorKey,SimulationOscillatorId> _ids;
    public SceneOscillatorFeedback(SceneAnimationAdapter adapter,IReadOnlyList<MachinePart> parts,
        IReadOnlyList<(MachinePart Owner,SceneOscillatorColour Declaration)> declarations,
        IReadOnlyDictionary<SceneOscillatorKey,SimulationOscillatorId> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);_adapter=adapter;_ids=new(ids);
        var groups=new Dictionary<SimulationOscillatorId,List<Binding>>();
        foreach(var id in ids.Values)
            if(!groups.TryAdd(id,new()))throw new ArgumentException("Duplicate oscillator feedback identity.");
        var materials=new Dictionary<MaterialId,int>();
        foreach(var part in parts)CountMaterials(part);
        foreach(var (owner,declaration) in declarations)
        {
            ArgumentNullException.ThrowIfNull(declaration);ArgumentNullException.ThrowIfNull(declaration.Definition);
            if(declaration.Source.Owner!=owner||declaration.Source.Slot is null||!ids.TryGetValue(declaration.Source,out var id))
                throw new ArgumentException("Oscillator feedback requires an owned declared source.");
            if(declaration.Target is null||!GodotObject.IsInstanceValid(declaration.Target)||!owner.IsAncestorOf(declaration.Target)||
                declaration.Target.MaterialOverride is not StandardMaterial3D material||
                !materials.TryGetValue(new(material.GetInstanceId()),out var uses)||uses!=1)
                throw new ArgumentException("Oscillator colour requires a live owned mesh and exclusive material.");
            var target=adapter.Register(material);
            var animation=adapter.BindImpulseColour(target,declaration.Definition,declaration.From,declaration.To);
            groups[id].Add(new(declaration.Target,material,target,animation,declaration.Definition.Clock));
        }
        foreach(var pair in groups)_bindings.Add(pair.Key,pair.Value.ToArray());

        void CountMaterials(Node node)
        {
            if(node is MeshInstance3D mesh&&mesh.MaterialOverride is { } material)
            {
                var key=new MaterialId(material.GetInstanceId());
                materials.TryGetValue(key,out var count);materials[key]=checked(count+1);
            }
            foreach(var child in node.GetChildren())CountMaterials(child);
        }
    }
    public AnimationImpulseRead Read(SceneOscillatorKey source,int bindingIndex)
    {
        if(!_ids.TryGetValue(source,out var id))throw new ArgumentException("Unknown oscillator feedback source.");
        var bindings=_bindings[id];
        if((uint)bindingIndex>=(uint)bindings.Length)throw new ArgumentOutOfRangeException(nameof(bindingIndex));
        return _adapter.ReadImpulses(bindings[bindingIndex].Animation);
    }
    public void RefreshVisibility()
    {
        foreach(var bindings in _bindings.Values)
        foreach(var binding in bindings)
        {
            if(!GodotObject.IsInstanceValid(binding.Node)||binding.Node.MaterialOverride!=binding.Material)
                throw new InvalidOperationException("Oscillator feedback target binding changed.");
            _adapter.SetVisible(binding.Target,binding.Node.IsVisibleInTree());
        }
    }
    private static double OccurredAt(Binding binding,PoseReadStamp occurrence,PoseReadStamp current,double presentationTime)=>
        binding.Clock switch
        {
            AnimationClock.Simulation=>occurrence.SimulationTime,
            AnimationClock.Presentation=>presentationTime-(current.SimulationTime-occurrence.SimulationTime),
            _=>throw new InvalidOperationException("Unsupported impulse clock.")
        };
    public void Consume(CommittedEventStream<SimulationOscillatorPulse> events,PoseReadStamp current,double presentationTime)
    {
        ArgumentNullException.ThrowIfNull(events);events.RequireWritable();
        if(events.Id!=StreamId||events.CurrentStamp!=current)
            throw new ArgumentException("Oscillator events must match the current committed snapshot.");
        RefreshVisibility();
        while(events.PendingCount>0)
        {
            var occurrence=events.Peek();var pulse=occurrence.Payload;
            if(occurrence.Id.Stream!=StreamId||occurrence.Id.Generation!=current.Generation||
                occurrence.Stamp.Generation!=current.Generation||occurrence.Stamp.Revision.Value>current.Revision.Value||
                occurrence.Stamp.SimulationTime>current.SimulationTime||
                pulse.Tick!=occurrence.Stamp.Revision.Value-1||pulse.Sequence<1||
                !_bindings.TryGetValue(pulse.Id,out var bindings))
                throw new ArgumentException("Invalid committed oscillator occurrence.");
            var id=new AnimationOccurrenceId(occurrence.Id.Sequence.Value);
            foreach(var binding in bindings)
                if(_adapter.ValidateImpulseAdmission(binding.Animation,id,1,OccurredAt(binding,occurrence.Stamp,current,presentationTime))
                    ==AnimationImpulseAdmission.CapacityExhausted)return;
            foreach(var binding in bindings)
                if(_adapter.EnqueueImpulse(binding.Animation,id,1,OccurredAt(binding,occurrence.Stamp,current,presentationTime))
                    !=AnimationImpulseAdmission.Accepted)
                    throw new InvalidOperationException("Impulse admission changed without an intervening writer.");
            events.Acknowledge(occurrence.Id);
        }
    }
}
