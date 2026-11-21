using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>Dynamic response components with explicit resource vertices. A common static support is not a coupling.</summary>
internal sealed class ContinuousWorkComponents
{
    private int[] _parents = [];
    private int _bodyCount;
    private readonly Dictionary<(ContinuousWorkInputKind Kind, uint Slot), int> _owners = [];

    public void Begin(int bodies, int sources)
    {
        _bodyCount = bodies;
        var count = checked(bodies + sources);
        WorkshopPhysicsEngine.Scratch(ref _parents, count);
        for (var i = 0; i < count; i++) _parents[i] = i;
        _owners.Clear();
    }

    public int Root(int vertex)
    {
        while (_parents[vertex] != vertex)
        {
            _parents[vertex] = _parents[_parents[vertex]];
            vertex = _parents[vertex];
        }
        return vertex;
    }

    public int SourceRoot(int source) => Root(_bodyCount + source);

    public bool Join(int a, int b)
    {
        a = Root(a); b = Root(b);
        if (a == b) return false;
        _parents[Math.Max(a, b)] = Math.Min(a, b);
        return true;
    }

    public void JoinBodies(SimBody a, SimBody b)
    {
        if (a.Motion == RigidMotionKind.Dynamic && b.Motion == RigidMotionKind.Dynamic) Join(a.BodySlot, b.BodySlot);
    }

    public void JoinSource(int source, SimBody body)
    {
        if (body.Motion == RigidMotionKind.Dynamic) Join(_bodyCount + source, body.BodySlot);
    }

    public void Share(int source, ContinuousWorkInputKind kind, uint slot)
    {
        if (slot == PhysicsGpuAbi.NoBody) return;
        var key = (kind, slot);
        if (_owners.TryGetValue(key, out var other)) Join(_bodyCount + source, _bodyCount + other);
        else _owners.Add(key, source);
    }

    public void JoinTransmission(SimMechanicalTransmission value)
    {
        JoinBodies(value.Driving.A, value.Driving.B);
        JoinBodies(value.Driven.A, value.Driven.B);
        var driving = value.Driving.A.Motion == RigidMotionKind.Dynamic ? value.Driving.A : value.Driving.B;
        var driven = value.Driven.A.Motion == RigidMotionKind.Dynamic ? value.Driven.A : value.Driven.B;
        JoinBodies(driving, driven);
    }

    public void JoinRoute(SimTensileRoute route)
    {
        var first = route.A.Motion == RigidMotionKind.Dynamic ? route.A : route.B;
        JoinBodies(route.A, route.B);
        foreach (var guide in route.Guides)
        {
            JoinBodies(first, guide.Body);
            if (first.Motion != RigidMotionKind.Dynamic) first = guide.Body;
        }
    }
}
