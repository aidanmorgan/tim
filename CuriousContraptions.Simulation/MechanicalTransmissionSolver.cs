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
    public bool Engaged;
    public float Impulse;
    public int Offset;
}

internal static class MechanicalTransmissionSolver
{
    public static void Prepare(SimMechanicalTransmission transmission)
    {
        transmission.Count = 0;
        if (!transmission.Engaged)
        {
            transmission.InverseMass = 0f;
            transmission.Impulse = 0f;
            return;
        }
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
        var inverseMass = Row(transmission).Mass();
        transmission.InverseMass = inverseMass;
        if (inverseMass <= 0f) transmission.Impulse = 0f;
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
        transmission.Impulse += impulse;
        Apply(transmission, impulse);
    }

    private static ImpulseJacobian Row(SimMechanicalTransmission transmission) =>
        new(transmission.Bodies.AsSpan(0, transmission.Count), default, transmission.Jacobians);
    public static void Apply(SimMechanicalTransmission transmission, float impulse) => Row(transmission).Apply(impulse);
}
