using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

public static class WorkshopActivationCompiler
{
    public static ActivationNetwork Compile(WorkshopConstruction construction)
    {
        construction.Validate();
        var nodes = new List<ActivationNodeDeclaration>(16); var edges = new List<ActivationEdge>(16);
        var passageSensors = new List<PassageSensorDeclaration>(4);
        foreach (var instance in construction.Instances)
        {
            if (instance is WorkshopGenericInstance && DeclaredPhysics.Of(instance.PartId).Signal?.Contact is not null)
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.ContactSource, construction.Instances.Identities[instance.Id].Trigger));
            else if (instance is WorkshopGenericInstance && DeclaredPhysics.Of(instance.PartId).Signal?.Indicator is { } indicator)
                nodes.Add(new(new(instance.Id.Value), instance.Id, IndicatorKind(instance.Id, indicator, construction), default));
            else if (instance is WorkshopGenericInstance generic && DeclaredPhysics.Of(instance.PartId).Signal?.Timer is { } timer)
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.Timer, default,
                    ActivationTimerDeclaration.Duration(generic.Parameters[new ParameterId(timer.DurationParameter)], construction.Settings.Simulation),
                    Completion: timer.Completion, Boundary: timer.Boundary, ElapsedSignal: timer.ElapsedSignal));
            // A declared orientation threshold owns a node only while wired: its sensor compiles under the same condition.
            else if (instance is WorkshopGenericInstance && DeclaredPhysics.Of(instance.PartId).Signal?.Orientation is not null &&
                     construction.Connections.HasSource(instance.Id))
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.OrientationSource, default, 0, construction.Instances.Identities[instance.Id].Orientation));
            else if (instance is WorkshopGenericInstance && DeclaredPhysics.Of(instance.PartId).Signal?.Passage is { } aperture)
            {
                var sensor = PassageSensorFor(instance, aperture, construction.Instances.Identities);
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.PassageSource, default, 0, default, sensor.Id));
                passageSensors.Add(sensor);
            }
            // A declared pulse count or memory holds committed discrete state in one node.
            else if (instance is WorkshopGenericInstance held && DeclaredPhysics.Of(instance.PartId).Signal is { } declared &&
                     MemoryNode(held, declared) is { } memory)
                nodes.Add(memory);
            // A declared contact-load sensor commits its own level, wired or not, so its row is one activation node; its declared
            // release hold (Story 9R.5) is compiled to ticks of this construction's cadence.
            else if (instance is WorkshopGenericInstance && DeclaredPhysics.Of(instance.PartId).Signal?.ContactLoad is { } load)
                nodes.Add(new(new(instance.Id.Value), instance.Id, ActivationNodeKind.ContactLoadSource, default,
                    ContactLoad: construction.Instances.Identities[instance.Id].ContactLoad,
                    ReleaseHoldTicks: ActivationTimerDeclaration.Duration(load.ReleaseHoldSeconds, construction.Settings.Simulation)));
            // A declared oscillator pulses while its PowerIn route has one upstream source: its interval compiles to whole ticks of this
            // construction's cadence, and each pulse names the frame's first collider.
            else if (instance is WorkshopGenericInstance periodic && DeclaredPhysics.Of(instance.PartId).Signal?.Oscillator is { } oscillator)
            {
                var sources = WorkshopSupplyCompiler.Compile(construction, DeclaredPhysics.Of).UpstreamSources(periodic.Id);
                if (sources.Length > 1) throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied,
                    "This source-accounted input requires one upstream source.");
                var powerSource = sources.Length == 1 ? sources[0] : default;
                var intervalTicks = ActivationTimerDeclaration.Duration(periodic.Parameters[new ParameterId(oscillator.IntervalParameter)],
                    construction.Settings.Simulation);
                nodes.Add(new(new(periodic.Id.Value), periodic.Id, ActivationNodeKind.Oscillator, default, 0, default, default, 0, intervalTicks,
                    powerSource, construction.Instances.Identities[periodic.Id].Colliders[0]));
            }
        }
        foreach (var link in construction.Connections)
            if (link.Domain == WorkshopConnectionDomain.Activation) edges.Add(new(new(link.Source.Value), new(link.Target.Value), link.Input));
        return new(nodes.ToArray(), edges.ToArray(), new WorkshopGpuProfile(construction.Settings.Simulation,
            construction.Settings.Physical, new(1)).Substeps, passageSensors.ToArray(),
            construction.Instances.Where(instance => DeclaredPhysics.Of(instance.PartId).Electrical?.Supply?.Kind ==
                CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact).Select(instance=>instance.Id).ToArray());
    }

    /// <summary>The node a declared pulse count or memory compiles to (null for any other declaration): a pulse count counts toward
    /// its WholeNumber target parameter (the network admits 1–9), and a memory is one bistable Set/Reset node.</summary>
    public static ActivationNodeDeclaration? MemoryNode(WorkshopGenericInstance owner, CuriousContraptions.Bundle.SignalPayload signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (signal.PulseCount is { } count)
            return new(new(owner.Id.Value), owner.Id, ActivationNodeKind.Counter, default, 0, default, default,
                (uint)owner.Parameters[new ParameterId(count.TargetParameter)]);
        return signal.Memory is null ? null : new(new(owner.Id.Value), owner.Id, ActivationNodeKind.StateLatch, default);
    }

    /// <summary>The node a declared indicator compiles to: an Activation link into its declared input from a level output makes it
    /// follow that level (LevelIndicator); any other wiring, or none, holds its first input (StickyFirstInput).</summary>
    public static ActivationNodeKind IndicatorKind(GpuBodyId owner, SignalIndicatorDescriptor indicator, WorkshopConstruction construction)
    {
        var input = ElementPortCatalogue.Socket(indicator.Input);
        return construction.Connections.Any(link => link.Domain == WorkshopConnectionDomain.Activation &&
            link.Target == owner && link.Input == input && construction.Instances.Any(source => source.Id == link.Source &&
                DeclaredPorts.Current.Signal(source.PartId, link.Output, WorkshopPortDirection.Output) == WorkshopSignalKind.Level))
            ? ActivationNodeKind.LevelIndicator : ActivationNodeKind.StickyFirstInput;
    }

    /// <summary>The passage sensor a declared aperture compiles to on its owner's frame, at the owner's placed pose, with the
    /// declared f32 radius and rearm clearance exactly.</summary>
    public static PassageSensorDeclaration PassageSensorFor(IWorkshopInstance owner, SignalPassageDescriptor aperture, CompiledIdentityMap identities)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return new(identities[owner.Id].Passage, owner.Id, owner.Cell, owner.Local, owner.Rotation,
            new(aperture.ApertureRadius), new(aperture.RearmClearance));
    }
}
