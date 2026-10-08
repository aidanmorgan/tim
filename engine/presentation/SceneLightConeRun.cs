using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Presentation;

/// <summary>Immutable local emission geometry; visibility follows committed owner activity.</summary>
public sealed record SceneLightCone(LightConeVisual Target,LightEmitter Source);

/// <summary>Run-owned render bindings. Live part state never selects emission.</summary>
public sealed class SceneLightConeRun
{
    private readonly record struct TargetIdentity(ulong Value);
    private readonly record struct Binding(MachinePart Owner,PhysicsBodyId Root,LightConeVisual Target,
        LightEmitter Source,bool BaselineVisible);
    private readonly Binding[] _bindings;
    private readonly bool[] _enabled;
    private bool _removed;
    public SceneLightConeRun(IReadOnlyList<MachinePart> parts,ScenePhysicsAssembly assembly,PoseReadLease seed)
    {
        var bindings=new List<Binding>();
        var targets=new HashSet<TargetIdentity>();
        foreach(var owner in parts)
        foreach(var declaration in owner.LightCones)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            var target=declaration.Target;var source=declaration.Source;
            if(target is null||!GodotObject.IsInstanceValid(target)||!owner.IsAncestorOf(target))
                throw new ArgumentException("Light cone requires a live owned target.");
            foreach(var physical in assembly.PresentationTargets)
                if(target==physical||target.IsAncestorOf(physical))
                    throw new InvalidOperationException("Light cone artwork cannot contain a physical pose target.");
            if(!targets.Add(new(target.GetInstanceId())))throw new ArgumentException("Light cone target has multiple writers.");
            if(!source.At.IsFinite()||!source.Direction.IsFinite()||!float.IsFinite(source.Direction.LengthSquared())||
                source.Direction.LengthSquared()==0||!float.IsFinite(source.Range)||source.Range<=0||
                !float.IsFinite(source.ConeCosine)||source.ConeCosine<0||source.ConeCosine>1||
                !float.IsFinite(source.Intensity)||source.Intensity<0)
                throw new ArgumentException("Light cone requires finite local geometry and supported emission limits.");
            bindings.Add(new(owner,assembly.QueryOwnerId(new(owner,MachinePart.RootBody)),target,source,target.Visible));
        }
        _bindings=bindings.ToArray();_enabled=new bool[_bindings.Length];
        Validate(seed);Publish(seed);
    }
    public void Validate(PoseReadLease read)
    {
        RequireLive();
        foreach(var binding in _bindings)
            if(read.ReadQuery(PoseSample.Current,binding.Root.Index).Owner!=binding.Root)
                throw new ArgumentException("Light cone owner topology changed.");
    }
    public void Publish(PoseReadLease read)
    {
        Validate(read);
        for(var i=0;i<_bindings.Length;i++)
            _enabled[i]=read.ReadActivity(PoseSample.Current,_bindings[i].Root.Index)==OwnerActivity.Active;
    }
    public void Present(MachineWorld world)
    {
        RequireLive();
        foreach(var binding in _bindings)
            if(!GodotObject.IsInstanceValid(binding.Target)||!GodotObject.IsInstanceValid(binding.Owner)||
                binding.Owner.GetParent()!=world||!binding.Owner.IsAncestorOf(binding.Target))
                throw new InvalidOperationException("Light cone target binding changed.");
        for(var i=0;i<_bindings.Length;i++)
        {
            var binding=_bindings[i];
            if(binding.Target.Visible!=_enabled[i])binding.Target.Visible=_enabled[i];
            if(_enabled[i]&&binding.Target.IsVisibleInTree())binding.Target.Refresh(world,binding.Owner,binding.Source);
        }
    }
    public void Remove()
    {
        RequireLive();
        foreach(var binding in _bindings)
            if(!GodotObject.IsInstanceValid(binding.Target))throw new InvalidOperationException("Light cone target was freed.");
        foreach(var binding in _bindings)binding.Target.Visible=binding.BaselineVisible;
        _removed=true;
    }
    private void RequireLive()
    {
        if(_removed)throw new InvalidOperationException("Light cone bindings have been removed.");
    }
}
