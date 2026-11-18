using System;
using System.Numerics;

namespace CuriousContraptions.Simulation;

/// <summary>Only a row actually queried in the current component preparation owns cached factor fill.</summary>
internal sealed class GuidePreparedRow
{
    private SimBody[] _bodies = [];
    private Vector3[] _linear = [], _angular = [];
    private int _count;
    public GuidePreparedRow? Next;
    public readonly GuideVector Projected = new(), Response = new(), Removed = new();

    public static int Key(in ImpulseJacobian row)
    {
        var key = new HashCode();
        for (var i = 0; i < row.Count; i++)
        {
            row.Get(i, out var body, out var linear, out var angular);
            key.Add(body.BodySlot); key.Add(linear); key.Add(angular);
        }
        return key.ToHashCode();
    }

    public bool Matches(in ImpulseJacobian row)
    {
        if (_count != row.Count) return false;
        for (var i = 0; i < _count; i++)
        {
            row.Get(i, out var body, out var linear, out var angular);
            if (_bodies[i] != body || _linear[i] != linear || _angular[i] != angular) return false;
        }
        return true;
    }

    public void Capture(in ImpulseJacobian row)
    {
        if (_bodies.Length < row.Count)
        {
            var capacity = Math.Max(row.Count, _bodies.Length * 2);
            Array.Resize(ref _bodies, capacity);
            Array.Resize(ref _linear, capacity);
            Array.Resize(ref _angular, capacity);
        }
        _count = row.Count;
        for (var i = 0; i < _count; i++) row.Get(i, out _bodies[i], out _linear[i], out _angular[i]);
        Removed.Clear();
    }
}
