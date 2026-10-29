using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Main-owned compilation of one installation for simulation, animation and native execution.</summary>
public static class WorkshopSceneCompiler
{
    private static CompiledPresentationBinding CompileBinding(PresentationBinding binding) =>
        new(binding.Property, binding.Driver, binding.From, binding.To, binding.Curve, binding.Duration,
            binding.Overlap, binding.Threshold, binding.Phase, binding.Response, binding.Intermediate,
            binding.Socket, binding.Event, binding.EventMagnitude);

    public static WorkshopCompiledScene Compile(WorkshopConstruction construction, PhysicsDocumentId document, CompiledSceneId identity)
    {
        foreach (var instance in construction.Instances)
            if (instance.PartId == default || !DeclaredPhysics.Entries.ContainsKey(instance.PartId))
                throw new WorkshopEditRejectedException(WorkshopEditRejection.UnsupportedPart,
                    instance.PartId == default ? "An element identity is required." : $"No declaration exists for '{instance.PartId}'.");
        construction.Validate();
        var physics = WorkshopPhysicsCompiler.Compile(construction, document);
        var activation = WorkshopActivationCompiler.Compile(construction);
        var nodes = activation.Nodes.ToArray();
        var tracks = new List<CompiledPresentationTrack>(16);
        var editable = new List<GpuBodyId>(16);
        foreach (var instance in construction.Instances)
        {
            var bindings = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings;
            var node = nodes.SingleOrDefault(candidate => candidate.Owner == instance.Id);
            var owner = (WorkshopGenericInstance)instance;
            var capacity = DeclaredPhysics.SourceStore(owner.PartId) is { } store
                ? owner.Parameters[new ParameterId(store.Capacity)] : 0;
            if (!owner.Locked && DeclaredPhysics.HasRunControlledSource(owner.PartId)) editable.Add(owner.Id);
            for (var i = 0; i < bindings.Count; i++)
                tracks.Add(new(new AnimationTargetId(WorkshopDeclaredPresentation.TargetBase + (ulong)tracks.Count),
                    owner.Id, i, CompileBinding(bindings[i]), node, capacity));
        }
        return new(identity, construction.Revision, construction.Settings, physics, activation, tracks,
            construction.Instances.Select(instance => instance.Id), editable);
    }
}
