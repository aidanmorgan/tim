using System;
using System.Numerics;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal sealed class SimMechanicalTransmission
{
    public SimRevoluteJoint DrivingJoint = null!;
    public SimRevoluteJoint DrivenJoint = null!;
    public float Ratio;
    public readonly SimBody[] Bodies = new SimBody[4];
    public readonly Vector3[] Jacobians = new Vector3[4];
    public int Count;
    public float InverseMass;
}

internal static class MechanicalTransmissionSolver
{
    public static void Prepare(SimMechanicalTransmission transmission)
    {
        transmission.Count = 0;
        var driving = transmission.DrivingJoint;
        var driven = transmission.DrivenJoint;
        // Scaling the whole velocity equation preserves its law and avoids ratio-squared overflow.
        var scale = MathF.Max(1f, MathF.Abs(transmission.Ratio));
        var sourceAxis = Vector3.Transform(Vector3.UnitZ, Quaternion.Multiply(driving.A.Rotation, driving.Qa)) * (transmission.Ratio / scale);
        var targetAxis = Vector3.Transform(Vector3.UnitZ, Quaternion.Multiply(driven.A.Rotation, driven.Qa)) / scale;
        Add(transmission, driving.A, sourceAxis);
        Add(transmission, driving.B, -sourceAxis);
        Add(transmission, driven.A, -targetAxis);
        Add(transmission, driven.B, targetAxis);
        var inverseMass = 0f;
        for (var i = 0; i < transmission.Count; i++)
            inverseMass += ConstraintMass(transmission.Bodies[i], Vector3.Zero, transmission.Jacobians[i]);
        transmission.InverseMass = inverseMass;
    }

    private static void Add(SimMechanicalTransmission transmission, SimBody body, Vector3 jacobian)
    {
        for (var i = 0; i < transmission.Count; i++)
        {
            if (!ReferenceEquals(transmission.Bodies[i], body)) continue;
            transmission.Jacobians[i] += jacobian;
            return;
        }
        var slot = transmission.Count++;
        transmission.Bodies[slot] = body;
        transmission.Jacobians[slot] = jacobian;
    }

    public static void Solve(SimMechanicalTransmission transmission)
    {
        if (transmission.InverseMass <= 0f) return;
        var velocity = 0f;
        for (var i = 0; i < transmission.Count; i++)
            velocity += ConstraintVelocity(transmission.Bodies[i], Vector3.Zero, transmission.Jacobians[i]);
        var impulse = -velocity / transmission.InverseMass;
        for (var i = 0; i < transmission.Count; i++)
            ApplyBodyImpulse(transmission.Bodies[i], Vector3.Zero, transmission.Jacobians[i], impulse);
    }
}
