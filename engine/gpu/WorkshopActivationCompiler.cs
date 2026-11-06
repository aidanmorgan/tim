using CuriousContraptions.Bundle;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public static class WorkshopActivationCompiler
{
    public static IActivationProgram Compile(ICoreServices core, IBundleServices bundle, WorkshopConstruction construction)
    {
        construction.Validate(core.Supply, bundle);
        var graph = new WorkshopSignalGraph(construction.Instances, construction.Connections);
        var supply = WorkshopSupplyCompiler.Compile(core.Supply, construction, DeclaredPhysics.Of);
        var nodes = graph.Nodes.Select(node => CompileNode(core, construction, graph, supply, node)).ToArray();
        var passages = new List<PassageSensorDeclaration>();
        foreach (var resolved in graph.Nodes)
            if (resolved.Declaration is SignalPassageDescriptor passage)
                passages.Add(PassageSensorFor(construction.Instances.Single(owner => owner.Id == resolved.Owner),
                    passage, construction.Instances.Identities));
        return core.CreateActivation(nodes, Edges(graph), new WorkshopGpuProfile(construction.Settings.Simulation,
            construction.Settings.Physical, new(1)).Substeps, passages.ToArray(),
            construction.Instances.Where(instance => DeclaredPhysics.Of(instance.PartId).Electrical?.Supply?.Kind ==
                CuriousContraptions.Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact).Select(instance =>
                {
                    var control = DeclaredPhysics.Of(instance.PartId).Electrical!.Supply!.Value.Control!.Value;
                    return new CompiledSignalContact(instance.Id,
                        construction.Instances.Identities[instance.Id].Signals[control.Node].Node, control.State);
                }).ToArray());
    }

    public static ActivationEdge[] Edges(WorkshopSignalGraph graph) =>
        graph.Edges.Select(edge => new ActivationEdge(edge.Source, edge.Target, edge.Input switch
        {
            SignalTerminal.Input => PortRole.ActivationIn,
            SignalTerminal.Set => PortRole.SetIn,
            SignalTerminal.Reset => PortRole.ResetIn,
            _ => throw new ArgumentException("Unsupported input terminal.")
        })).ToArray();

    private static ActivationNodeDeclaration CompileNode(ICoreServices core, WorkshopConstruction construction, WorkshopSignalGraph graph,
        ISupplyTopology supply, ResolvedSignalNode node)
    {
        var owner = (WorkshopGenericInstance)construction.Instances.Single(instance => instance.Id == node.Owner);
        var ids = construction.Instances.Identities[node.Owner].Signals[node.Declaration.Id];
        var cadence = construction.Settings.Simulation;
        uint Duration(ParameterId parameter) => core.TimerDuration(owner.Parameters[parameter], cadence);
        return node.Declaration switch
        {
            SignalContactDescriptor => new(node.Id, node.Owner, ActivationNodeKind.ContactSource, ids.Trigger),
            SignalOrientationDescriptor => new(node.Id, node.Owner, ActivationNodeKind.OrientationSource, default, Sensor: ids.Orientation),
            SignalPassageDescriptor => new(node.Id, node.Owner, ActivationNodeKind.PassageSource, default, Passage: ids.Passage),
            SignalContactLoadDescriptor load => new(node.Id, node.Owner, ActivationNodeKind.ContactLoadSource, default,
                ContactLoad: ids.ContactLoad, ReleaseHoldTicks: core.TimerDuration(load.ReleaseHoldSeconds, cadence)),
            SignalTimerDescriptor timer => new(node.Id, node.Owner, ActivationNodeKind.Timer, default, Duration(timer.DurationParameter),
                Completion: timer.Completion, Boundary: timer.Boundary, ElapsedSignal: timer.ElapsedSignal),
            SignalPulseCountDescriptor count => new(node.Id, node.Owner, ActivationNodeKind.Counter, default,
                TargetCount: CounterTarget(owner.Parameters[count.TargetParameter])),
            SignalMemoryDescriptor => new(node.Id, node.Owner, ActivationNodeKind.StateLatch, default),
            SignalIndicatorDescriptor => new(node.Id, node.Owner, IndicatorKind(node.Id, graph), default),
            SignalOscillatorDescriptor oscillator => new(node.Id, node.Owner, ActivationNodeKind.Oscillator, default,
                IntervalTicks: Duration(oscillator.IntervalParameter), PowerProvider: Provider(construction, supply, node.Owner, oscillator.SupplyInput)),
            _ => throw new ArgumentException("Unsupported signal operation.")
        };
    }

    private static GpuBodyId Provider(WorkshopConstruction construction, ISupplyTopology supply, GpuBodyId owner, PortId input)
    {
        var wires = construction.Connections.Where(link => link.Domain == WorkshopConnectionDomain.Electrical &&
            link.Target == owner && link.Input == input).ToArray();
        if (wires.Length == 0) return default;
        if (wires.Length != 1 || !supply.Nodes.Any(node => node.Owner == wires[0].Source &&
            node.Kind is SupplyNodeKind.Source or SupplyNodeKind.Contact or SupplyNodeKind.LogicGate))
            throw new WorkshopEditRejectedException(WorkshopEditRejection.PowerInputSupplied,
                "The selected oscillator input requires one declared supply output.");
        return wires[0].Source;
    }

    public static uint CounterTarget(float value)
    {
        if (!float.IsFinite(value) || value < 1 || value >= 4294967296f || value != MathF.Truncate(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        return checked((uint)value);
    }

    public static ActivationNodeKind IndicatorKind(ActivationNodeId id, WorkshopSignalGraph graph) =>
        graph.Edges.Any(edge => edge.Target == id && graph.Nodes.Any(node => node.Id == edge.Source &&
            SignalTerminals.Kind(node.Declaration, SignalTerminal.Output) == PortSignalKind.Level))
            ? ActivationNodeKind.LevelIndicator : ActivationNodeKind.StickyFirstInput;

    public static PassageSensorDeclaration PassageSensorFor(IWorkshopInstance owner, SignalPassageDescriptor passage,
        CompiledIdentityMap identities)
    {
        var rotation = new Quaternion(owner.Rotation.X, owner.Rotation.Y, owner.Rotation.Z, owner.Rotation.W);
        var offset = Vector3.Transform(passage.LocalPosition, rotation) * 16f;
        var turn = Quaternion.Normalize(rotation * passage.LocalRotation);
        return new(identities[owner.Id].Signals[passage.Id].Passage, identities.Body(owner.Id, passage.Frame), owner.Cell,
            new(owner.Local.X + offset.X, owner.Local.Y + offset.Y, owner.Local.Z + offset.Z),
            new(turn.X, turn.Y, turn.Z, turn.W), new(passage.ApertureRadius), new(passage.RearmClearance));
    }
}
