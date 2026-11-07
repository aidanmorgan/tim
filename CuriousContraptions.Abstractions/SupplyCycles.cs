using System;
using System.Linq;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

/// <summary>Directed cycles of the declared electrical topology. Only monotone rules may share one: a gate that more input power can
/// switch off (a nonmonotone gate) has no settled zero-delay state on a loop, so connection validation refuses such a loop before any
/// solve, even when only an open contact closes it.</summary>
public static class SupplyCycles
{
    /// <summary>Whether more powered inputs can never unpower the output. Only monotone rules may share a directed cycle.</summary>
    public static bool IsMonotone(LogicGateOperation operation) => operation switch
    {
        LogicGateOperation.And => true,
        LogicGateOperation.Nand => false,
        LogicGateOperation.Nor => false,
        LogicGateOperation.Or => true,
        LogicGateOperation.Xor => false,
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    /// <summary>A node without a gate law (source, contact, consumer) only ever passes more power on with more power in.</summary>
    public static bool Monotone(LogicGateOperation? operation) => operation is not { } gate || IsMonotone(gate);

    /// <summary>The strongly connected components of a graph given as predecessor lists, in evaluation order: every edge runs from an
    /// earlier component into a later one or stays inside one. Tarjan over predecessors emits each component after everything
    /// upstream of it; iterative, so a long chain cannot exhaust the worker's stack.</summary>
    public static int[][] Components(int[][] predecessors)
    {
        ArgumentNullException.ThrowIfNull(predecessors);
        var count=predecessors.Length;
        var index=new int[count]; var low=new int[count]; var onStack=new bool[count];
        Array.Fill(index,-1);
        var stack=new System.Collections.Generic.Stack<int>(4);
        var work=new System.Collections.Generic.Stack<(int Node,int Edge)>(16);
        var components=new System.Collections.Generic.List<int[]>(4);
        var next=0;
        for(var root=0;root<count;root++)
        {
            if(index[root]>=0) continue;
            Open(root);
            while(work.TryPop(out var frame))
            {
                var (node,edge)=frame;
                if(edge<predecessors[node].Length)
                {
                    work.Push((node,edge+1));
                    var other=predecessors[node][edge];
                    if(index[other]<0) Open(other);
                    else if(onStack[other]) low[node]=Math.Min(low[node],index[other]);
                    continue;
                }
                if(work.TryPeek(out var parent)) low[parent.Node]=Math.Min(low[parent.Node],low[node]);
                if(low[node]!=index[node]) continue;
                var members=new System.Collections.Generic.List<int>(4);
                int member;
                do { member=stack.Pop(); onStack[member]=false; members.Add(member); } while(member!=node);
                members.Sort();
                components.Add(members.ToArray());
            }
        }
        return components.ToArray();
        void Open(int node) { index[node]=low[node]=next++; stack.Push(node); onStack[node]=true; work.Push((node,0)); }
    }

    public static bool Cyclic(int[] component, int[][] predecessors) =>
        component.Length>1 || Array.IndexOf(predecessors[component[0]],component[0])>=0;

}
