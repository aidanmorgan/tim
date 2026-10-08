using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Presentation;

public enum SceneMotionProperty { Rotation, Translation }
public enum SceneMotionAxis { X, Y, Z }
public sealed record SceneAcousticMotion(Node3D Target,AnimationOscillationDefinition Definition,
    SceneMotionProperty Property,SceneMotionAxis Axis,AnimationDirection Direction);

/// <summary>Typed committed acoustic occurrences drive the central cosmetic animation batch.
/// Presentation-clock impulses begin at commit admission; hidden motion advances analytically.</summary>
public sealed class SceneAcousticMotionRun
{
    private readonly record struct Binding(Node3D Node,Node Parent,SceneAnimationTargetHandle Target,AnimationHandle Animation,
        AnimationClock Clock,AnimationDirection Direction);
    public SceneAcousticWavefrontRun Wavefronts { get; }
    private readonly SceneAnimationAdapter _adapter;
    private readonly Dictionary<MachinePart,Binding[]> _bindings=[];
    private PoseReadStamp _stamp;
    private double _presentationTime;
    private ulong _lastSequence;
    private bool _removed;
    public SceneAcousticMotionRun(SceneAnimationAdapter adapter,IReadOnlyList<MachinePart> parts,
        IReadOnlyList<Node3D> physicalTargets,PoseReadStamp seed)
    {
        ArgumentNullException.ThrowIfNull(adapter);ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(physicalTargets);
        if(seed.Generation.Value==0||seed.Revision.Value!=0||seed.SimulationTime!=0)
            throw new ArgumentException("Acoustic motion requires the run seed.");
        _adapter=adapter;_stamp=seed;
        Wavefronts=new(adapter,parts,physicalTargets,seed);
        foreach(var part in parts)
        {
            var declarations=part.AcousticMotions;
            if(part.AcousticPlayback is null)
            {
                if(declarations.Count!=0)throw new ArgumentException("Acoustic motion requires a declared emission source.");
                continue;
            }
            var bindings=new Binding[declarations.Count];
            for(var i=0;i<bindings.Length;i++)
            {
                var declaration=declarations[i];ArgumentNullException.ThrowIfNull(declaration);
                ArgumentNullException.ThrowIfNull(declaration.Definition);
                var node=declaration.Target;
                if(node is null||!GodotObject.IsInstanceValid(node)||!part.IsAncestorOf(node))
                    throw new ArgumentException("Acoustic motion requires a live owned cosmetic child.");
                if(!Enum.IsDefined(declaration.Property)||!Enum.IsDefined(declaration.Axis)||!Enum.IsDefined(declaration.Direction))
                    throw new ArgumentException("Unsupported acoustic motion declaration.");
                foreach(var physical in physicalTargets)
                    if(node==physical||node.IsAncestorOf(physical))
                        throw new InvalidOperationException("Acoustic cosmetics cannot own functional pose geometry.");
                var target=adapter.Register(node);
                var animation=declaration.Property switch
                {
                    SceneMotionProperty.Rotation=>adapter.BindOscillatingRotation(target,declaration.Definition,declaration.Axis switch
                    {
                        SceneMotionAxis.X=>AnimationRotationAxis.X,SceneMotionAxis.Y=>AnimationRotationAxis.Y,
                        SceneMotionAxis.Z=>AnimationRotationAxis.Z,_=>throw new ArgumentOutOfRangeException()
                    }),
                    SceneMotionProperty.Translation=>adapter.BindOscillatingTranslation(target,declaration.Definition,declaration.Axis switch
                    {
                        SceneMotionAxis.X=>AnimationTranslationAxis.X,SceneMotionAxis.Y=>AnimationTranslationAxis.Y,
                        SceneMotionAxis.Z=>AnimationTranslationAxis.Z,_=>throw new ArgumentOutOfRangeException()
                    }),
                    _=>throw new ArgumentOutOfRangeException()
                };
                bindings[i]=new(node,node.GetParent(),target,animation,declaration.Definition.Clock,declaration.Direction);
            }
            _bindings.Add(part,bindings);
        }
    }
    public void Commit(PoseReadStamp stamp)
    {
        RequireLive();
        if(stamp.Generation!=_stamp.Generation||stamp.Revision.Value!=checked(_stamp.Revision.Value+1)||
            !double.IsFinite(stamp.SimulationTime)||stamp.SimulationTime<=_stamp.SimulationTime)
            throw new ArgumentException("Acoustic motion must consume adjacent committed ticks.");
        _stamp=stamp;
    }
    public void AdvancePresentation(double time)
    {
        RequireLive();
        if(!double.IsFinite(time)||time<_presentationTime)throw new ArgumentOutOfRangeException(nameof(time));
        _presentationTime=time;
    }
    public void Admit(MachinePart part,PoseReadStamp stamp,CommittedEventId occurrence,float strength)
    {
        RequireLive();
        if(stamp!=_stamp||_stamp.Revision.Value==0||occurrence.Stream!=SceneAcousticRun.StreamId||occurrence.Generation!=_stamp.Generation||
            occurrence.Sequence.Value==0||occurrence.Sequence.Value<=_lastSequence||!_bindings.TryGetValue(part,out var bindings))
            throw new ArgumentException("Acoustic motion requires a known committed occurrence and source.");
        if(!float.IsFinite(strength)||strength<=0||strength>1)throw new ArgumentOutOfRangeException(nameof(strength));
        var id=new AnimationOccurrenceId(occurrence.Sequence.Value);
        foreach(var binding in bindings)
        {
            ValidateTarget(binding);
            _adapter.ValidateOscillationKick(binding.Animation,id,Signed(binding,strength),Time(binding));
        }
        foreach(var binding in bindings)_adapter.KickOscillation(binding.Animation,id,Signed(binding,strength),Time(binding));
        _lastSequence=occurrence.Sequence.Value;
    }
    private double Time(Binding binding)=>binding.Clock==AnimationClock.Presentation?_presentationTime:_stamp.SimulationTime;
    private static double Signed(Binding binding,float strength)=>binding.Direction==AnimationDirection.Forward?strength:-strength;
    private static void ValidateTarget(Binding binding)
    {
        if(!GodotObject.IsInstanceValid(binding.Node)||binding.Node.GetParent()!=binding.Parent)
            throw new InvalidOperationException("Acoustic motion target binding changed.");
    }
    public void RefreshVisibility()
    {
        RequireLive();
        foreach(var bindings in _bindings.Values)
        foreach(var binding in bindings)
        {
            ValidateTarget(binding);_adapter.SetVisible(binding.Target,binding.Node.IsVisibleInTree());
        }
    }
    public AnimationOscillationRead Read(MachinePart part,int index)
    {
        RequireLive();
        if(!_bindings.TryGetValue(part,out var bindings)||(uint)index>=(uint)bindings.Length)
            throw new ArgumentException("Unknown acoustic motion binding.");
        return _adapter.ReadOscillation(bindings[index].Animation);
    }
    public void Remove(){RequireLive();Wavefronts.Remove();_removed=true;}
    private void RequireLive(){if(_removed)throw new InvalidOperationException("Acoustic motion run was removed.");}
}
