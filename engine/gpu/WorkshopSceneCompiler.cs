using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Gpu;

/// <summary>Main-owned compilation of one installation for simulation, animation and native execution.</summary>
public static class WorkshopSceneCompiler
{
    private static CompiledPresentationBinding CompileBinding(PresentationBinding binding, PartId partId) =>
        new(binding.Property, binding.Driver, binding.Mapping, binding.From, binding.To, binding.Curve, binding.Duration,
            binding.Overlap, binding.Threshold, binding.Phase, binding.Response, binding.Intermediate,
            binding.Socket.HasValue ? DeclaredPorts.Current.Role(partId, binding.Socket.Value) : null, binding.Event, binding.EventMagnitude, binding.StateValues);

    private static SupplyConsumer ResolveActuator(CompiledPartIdentities ids, CoordinateReference coordinate) => coordinate.Kind switch
    {
        CoordinateKind.Prismatic => SupplyConsumer.Prismatic(ids.Sliders[coordinate.Index].Joint),
        CoordinateKind.Revolute => SupplyConsumer.Revolute(ids.Hinges[coordinate.Index].Joint),
        _ => throw new ArgumentException("Unsupported actuator coordinate.")
    };

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
        var runControls = new List<RunControlRow>(16);
        foreach (var instance in construction.Instances)
        {
            var bindings = DeclaredPhysics.Of(instance.PartId).Presentation.Bindings;
            var owner = (WorkshopGenericInstance)instance;
            var capacity = DeclaredPhysics.SourceStore(owner.PartId) is { } store
                ? owner.Parameters[new ParameterId(store.Capacity)] : 0;
            // Each declared run-time control of an unlocked part compiles one row; a locked part offers none. A scalar lane's row carries
            // its declared parameter's bounds; an enable lane's carries none.
            if (!owner.Locked)
                foreach (var control in DeclaredPhysics.RunControls(owner.PartId))
                {
                    var declared = DeclaredPhysics.Of(owner.PartId).Metadata.Parameters.Single(parameter => parameter.Key == control.Parameter.Value);
                    var scalar = RunControlLanes.ValueKind(control.Lane) == RunControlValueKind.Scalar;
                    runControls.Add(new(new RunControlId(checked((ulong)runControls.Count + 1)), owner.Id, control.Lane,
                        RunControlLanes.Slot(physics, control.Lane, owner.Id), scalar ? declared.Minimum : 0f, scalar ? declared.Maximum : 0f));
                }
            for (var i = 0; i < bindings.Count; i++)
                tracks.Add(new(new AnimationTargetId(WorkshopDeclaredPresentation.TargetBase + (ulong)tracks.Count),
                    owner.Id, i, CompileBinding(bindings[i], owner.PartId),
                    bindings[i].SignalNode is { } selected ? nodes.Single(node => node.Id == construction.Instances.Identities[owner.Id].Signals[selected].Node) : default, capacity,
                    bindings[i].Actuator is { } actuator ? ResolveActuator(construction.Instances.Identities[owner.Id], actuator) : null,
                    bindings[i].GuidePort is { } guide ? construction.Instances.Identities[owner.Id].Guides[guide] : default));
        }
        return new(identity, construction.Revision, construction.Settings, physics, activation, tracks,
            construction.Instances.Select(instance => instance.Id), runControls);
    }
}
