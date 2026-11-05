using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Simulation;

internal sealed class SimDrivenSurface
{
    public required SimCollider Collider { get; init; }
    public required SimRevoluteJoint Source { get; init; }
    public required BoxFace Face { get; init; }
    public required Vector3 LocalDirection { get; init; }
    public required float TravelPerRadian { get; init; }

    public bool Owns(SimCollider collider, Vector3 outward) =>
        ReferenceEquals(Collider, collider) && PhysicalTolerance.CompatibleContactNormals(
            Vector3.Transform(BoxFaces.Normal(Face), collider.Rotation), outward,
            new Vector3(collider.Hx, collider.Hy, collider.Hz).Length(), WorkshopConstraintSolver.ContactSlop);
}

/// <summary>Three physical wrench rows aggregated by body before velocity, mass or impulse evaluation.</summary>
internal sealed class FrictionJacobian
{
    // Two contacting bodies plus two endpoints for each of two driven coordinates.
    private readonly SimBody[] _bodies = new SimBody[6];
    private readonly Vector3[] _linear = new Vector3[18];
    private readonly Vector3[] _angular = new Vector3[18];
    private readonly Quaternion[] _rotations = new Quaternion[6];
    private int _count;

    public bool FramesUnchanged()
    {
        for (var body = 0; body < _count; body++)
            if (_bodies[body].Rotation != _rotations[body]) return false;
        return true;
    }

    public void Build(ContactPatch patch)
    {
        _count = 0;
        Array.Clear(_linear); Array.Clear(_angular);
        var contact = patch.Points[0];
        for (var row = 0; row < 3; row++)
        {
            Add(contact.A, row, patch.Linear[row], patch.AngularA[row]);
            Add(contact.B, row, -patch.Linear[row], -patch.AngularB[row]);
        }
        AddSurface(patch.SurfaceA, patch, 1f);
        AddSurface(patch.SurfaceB, patch, -1f);
        for (var body = 0; body < _count; body++) _rotations[body] = _bodies[body].Rotation;
    }

    private void AddSurface(SimDrivenSurface? surface, ContactPatch patch, float sign)
    {
        if (surface is null) return;
        var source = surface.Source;
        var axis = Vector3.Transform(Vector3.UnitZ, source.A.Rotation * source.Qa);
        var direction = Vector3.Transform(surface.LocalDirection, surface.Collider.Body.Rotation);
        for (var row = 0; row < 2; row++)
        {
            var angular = sign * surface.TravelPerRadian * Vector3.Dot(patch.Linear[row], direction) * axis;
            Add(source.A, row, Vector3.Zero, -angular);
            Add(source.B, row, Vector3.Zero, angular);
        }
    }

    private void Add(SimBody body, int row, Vector3 linear, Vector3 angular)
    {
        var index = 0;
        while (index < _count && !ReferenceEquals(_bodies[index], body)) index++;
        if (index == _count) _bodies[_count++] = body;
        _linear[index * 3 + row] += linear;
        _angular[index * 3 + row] += angular;
    }

    public float Mass(int first, int second)
    {
        var mass = 0f;
        for (var body = 0; body < _count; body++)
        {
            if (_bodies[body].Motion != Gpu.RigidMotionKind.Dynamic) continue;
            var offset = body * 3;
            mass += _bodies[body].InvMass * Vector3.Dot(_linear[offset + first], _linear[offset + second]) +
                WorkshopCollisionKernels.InverseInertiaCoupling(_bodies[body], _angular[offset + first], _angular[offset + second]);
        }
        return mass;
    }

    public float Velocity(int row)
    {
        var velocity = 0f;
        for (var body = 0; body < _count; body++)
            velocity += ConstraintBodyImpulse.ConstraintVelocity(_bodies[body], _linear[body * 3 + row], _angular[body * 3 + row]);
        return velocity;
    }

    public void Apply(Vector3 impulse)
    {
        for (var body = 0; body < _count; body++)
        {
            var offset = body * 3;
            ConstraintBodyImpulse.ApplyBodyImpulse(_bodies[body],
                impulse.X * _linear[offset] + impulse.Y * _linear[offset + 1] + impulse.Z * _linear[offset + 2],
                impulse.X * _angular[offset] + impulse.Y * _angular[offset + 1] + impulse.Z * _angular[offset + 2], 1f);
        }
    }
}
