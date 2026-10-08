using System;
using System.Collections.Generic;
using Godot;

namespace CuriousContraptions.Presentation;

public enum OpticalPreviewComposition { Separate, MergeCollinear }
public readonly record struct SceneOpticalPreview(OpticalPathVisual Target,OpticalPreviewComposition Composition);

/// <summary>One read-only construction trace batch shared by every selected optical preview.</summary>
public sealed class SceneOpticalPreviews
{
    private readonly record struct TargetIdentity(ulong Value);
    private readonly record struct Binding(MachinePart Owner,SceneOpticalPreview Declaration);
    private readonly List<Binding> _bindings=[];
    private readonly HashSet<TargetIdentity> _targets=[];
    private readonly List<OpticalSegment> _selectedSegments=[];
    public void Present(MachineWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(world.Phase!=MachineWorldPhase.Idle)throw new InvalidOperationException("Preview presentation requires an idle world.");
        _bindings.Clear();_targets.Clear();
        var construction=!world.HasPhysicsState&&!world.Running&&!world.Won;
        var selected=false;
        foreach(var owner in world.Parts)
        {
            if(owner.OpticalPreview is not { } declaration)continue;
            var target=declaration.Target;
            if(target is null||!GodotObject.IsInstanceValid(target)||!owner.IsAncestorOf(target)||!target.Preview)
                throw new ArgumentException("Optical preview requires a live owned preview target.");
            if(!Enum.IsDefined(declaration.Composition))throw new ArgumentException("Unsupported optical preview composition.");
            if(!_targets.Add(new(target.GetInstanceId())))throw new ArgumentException("Optical preview target has multiple writers.");
            _bindings.Add(new(owner,declaration));
            selected|=construction&&owner.IsSelected&&owner.IsVisibleInTree();
        }
        // No trace unless a selected construction target needs it; never query live runtime state.
        var paths=selected?OpticalNetwork.TraceConstructionPreviews(world):Array.Empty<OpticalSegment>();
        foreach(var binding in _bindings)
        {
            var visible=construction&&binding.Owner.IsSelected&&binding.Owner.IsVisibleInTree();
            var target=binding.Declaration.Target;
            if(target.Visible!=visible)target.Visible=visible;
            if(!visible)continue;
            _selectedSegments.Clear();
            foreach(var path in paths)
                if(path.OriginPart==binding.Owner.OpticalIdentity)_selectedSegments.Add(path);
            target.Refresh(binding.Declaration.Composition switch
            {
                OpticalPreviewComposition.Separate=>_selectedSegments,
                OpticalPreviewComposition.MergeCollinear=>OpticalPathVisual.Merge(_selectedSegments),
                _=>throw new InvalidOperationException("Unsupported optical preview composition.")
            });
        }
    }
}
