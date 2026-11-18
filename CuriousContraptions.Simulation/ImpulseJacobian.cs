using System;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>A row view over actual body incidence, including driven-surface and transmission rows.</summary>
internal readonly ref struct ImpulseJacobian
{
    private readonly SimBody? _a, _b;
    private readonly Vector3 _la, _aa, _lb, _ab;
    private readonly ReadOnlySpan<SimBody> _bodies;
    private readonly ReadOnlySpan<Vector3> _linear, _angular;
    private readonly int _stride, _offset;
    public int Count { get; }
    public ImpulseJacobian(SimBody a, Vector3 linear, Vector3 angular)
    { _a = a; _la = linear; _aa = angular; Count = 1; }
    public ImpulseJacobian(SimBody a, Vector3 la, Vector3 aa, SimBody b, Vector3 lb, Vector3 ab)
    { _a = a; _la = la; _aa = aa; _b = b; _lb = lb; _ab = ab; Count = 2; }
    public ImpulseJacobian(ReadOnlySpan<SimBody> bodies, ReadOnlySpan<Vector3> linear,
        ReadOnlySpan<Vector3> angular, int stride = 1, int offset = 0)
    { _bodies = bodies; _linear = linear; _angular = angular; _stride = stride; _offset = offset; Count = bodies.Length; }

    public void Get(int index, out SimBody body, out Vector3 linear, out Vector3 angular)
    {
        if (_a is not null)
        {
            body = index == 0 ? _a : _b!;
            linear = index == 0 ? _la : _lb;
            angular = index == 0 ? _aa : _ab;
            return;
        }
        body = _bodies[index];
        var slot = index * _stride + _offset;
        linear = _linear.IsEmpty ? Vector3.Zero : _linear[slot];
        angular = _angular[slot];
    }

    public float Mass() => Coupling(this, this);

    public static float Coupling(in ImpulseJacobian a, in ImpulseJacobian b)
    {
        var result = 0f;
        for (var i = 0; i < a.Count; i++)
        {
            a.Get(i, out var body, out var linear, out var angular);
            if (body.Motion != RigidMotionKind.Dynamic) continue;
            if (body.Guide is { } guide)
            {
                if (FirstComponent(a, i, guide)) result += guide.Mass(a, b);
                continue;
            }
            for (var j = 0; j < b.Count; j++)
            {
                b.Get(j, out var other, out var otherLinear, out var otherAngular);
                if (other != body) continue;
                result += body.InvMass * Vector3.Dot(linear, otherLinear)
                    + WorkshopCollisionKernels.InverseInertiaCoupling(body, angular, otherAngular);
            }
        }
        return result;
    }

    public void Apply(float impulse)
    {
        for (var i = 0; i < Count; i++)
        {
            Get(i, out var body, out var linear, out var angular);
            if (body.Guide is { } guide)
            {
                if (FirstComponent(this, i, guide)) guide.Apply(this, impulse);
            }
            else ConstraintBodyImpulse.ApplyBodyImpulse(body, linear, angular, impulse);
        }
    }

    private static bool FirstComponent(in ImpulseJacobian row, int index, GuideComponent component)
    {
        for (var i = 0; i < index; i++)
        {
            row.Get(i, out var body, out _, out _);
            if (body.Guide == component) return false;
        }
        return true;
    }
}
