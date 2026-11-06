using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Abstractions.Signal;

public enum SignalTopologyRejection { InvalidDelivery, LevelFanIn, InstantaneousCycle }
public sealed class SignalTopologyException(SignalTopologyRejection reason, string message) : ArgumentException(message)
{
    public SignalTopologyRejection Reason { get; } = reason;
}

public readonly record struct SignalTopologyNode(int Slot, bool Delayed);
public readonly record struct SignalTopologyEdge(int SourceSlot, int TargetSlot, SignalTerminal Input, bool Level);

/// <summary>Canonical delivery cardinality and instantaneous-cycle admission shared by authoring and workers.</summary>
public static class SignalTopology
{
    public static void Validate(IReadOnlyList<SignalTopologyNode> nodes, IReadOnlyList<SignalTopologyEdge> edges)
    {
        var indexed = nodes.ToDictionary(node => node.Slot);
        var seen = new HashSet<SignalTopologyEdge>();
        foreach (var edge in edges)
            if (!indexed.ContainsKey(edge.SourceSlot) || !indexed.ContainsKey(edge.TargetSlot) || edge.SourceSlot == edge.TargetSlot ||
                !Enum.IsDefined(edge.Input) || edge.Input == SignalTerminal.Output || !seen.Add(edge))
                throw new SignalTopologyException(SignalTopologyRejection.InvalidDelivery, "Invalid or duplicate signal delivery.");
        foreach (var inputs in edges.GroupBy(edge => (edge.TargetSlot, edge.Input)))
            if (inputs.Any(edge => edge.Level) && inputs.Count() != 1)
                throw new SignalTopologyException(SignalTopologyRejection.LevelFanIn, "A level input takes exactly one source.");
        var indegrees = nodes.ToDictionary(node => node.Slot, _ => 0);
        var outgoing = nodes.ToDictionary(node => node.Slot, _ => new List<int>());
        foreach (var edge in edges)
            if (!indexed[edge.SourceSlot].Delayed)
            {
                outgoing[edge.SourceSlot].Add(edge.TargetSlot);
                indegrees[edge.TargetSlot]++;
            }
        var ready = new Queue<int>(indegrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var count = 0;
        while (ready.TryDequeue(out var node))
        {
            count++;
            foreach (var target in outgoing[node])
                if (--indegrees[target] == 0) ready.Enqueue(target);
        }
        if (count != nodes.Count) throw new SignalTopologyException(SignalTopologyRejection.InstantaneousCycle, "Instantaneous signal cycle.");
    }
}
