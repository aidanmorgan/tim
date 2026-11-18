using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

/// <summary>The bounded axial velocity row shared by fixed-target and directional prismatic drives.</summary>
internal static class PrismaticMotorSolver
{
    public static float Coordinate(SimJoint joint)
    {
        var rotation = System.Numerics.Quaternion.Multiply(joint.A.Rotation, joint.Qa);
        var axis = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, rotation);
        var difference = joint.B.Position - joint.A.Position +
            System.Numerics.Vector3.Transform(joint.Pb, joint.B.Rotation) -
            System.Numerics.Vector3.Transform(joint.Pa, joint.A.Rotation);
        return System.Numerics.Vector3.Dot(difference, axis);
    }

    public static void BeginSubstep(SimJoint joint, float h)
    {
        joint.StartCoordinate = Coordinate(joint);
        joint.MotorImpulse = 0f;
        joint.EffortMultiplier = 1f;
        joint.StepWork = 0f;
        joint.MotorActive = false;
        if (joint.DriveKind == PrismaticDriveKind.None) return;
        var drive = joint.Drive;
        var commanded = drive.Selection == PrismaticTargetSelection.Fixed ||
            joint.Command is PrismaticCommand.Positive or PrismaticCommand.Negative;
        var target = drive.Selection == PrismaticTargetSelection.Fixed ? drive.Target :
            joint.Command == PrismaticCommand.Negative ? joint.Lower : joint.Upper;
        var distance = target - joint.StartCoordinate;
        var atTarget = drive.Selection == PrismaticTargetSelection.Fixed
            ? MathF.Abs(distance) <= ActuatorObservations.StopSlop
            : joint.Command == PrismaticCommand.Negative
                ? joint.StartCoordinate <= joint.Lower + ActuatorObservations.StopSlop
                : joint.StartCoordinate >= joint.Upper - ActuatorObservations.StopSlop;
        var lockNow = drive.Inactive == PrismaticInactiveResponse.SelfLock &&
            (!joint.SupplyClosed || !commanded || atTarget);
        if (lockNow && !joint.Holding) joint.HoldCoordinate = joint.StartCoordinate;
        if (joint.Holding != lockNow) joint.Impulses[(int)PrismaticRowKind.Spring] = 0f;
        joint.Holding = lockNow;
        joint.MotorActive = joint.SupplyClosed && commanded && !lockNow;
        joint.Phase = Phase(joint, atTarget);
        if (!joint.MotorActive) { joint.CommandSpeed = 0f; return; }
        var requested = MathF.Sign(distance) * MathF.Min(drive.MaxSpeed,
            MathF.Min(MathF.Sqrt(2f * drive.StoppingAcceleration * MathF.Abs(distance)), MathF.Abs(distance) / h));
        var slew = drive.StoppingAcceleration * h;
        joint.CommandSpeed += Math.Clamp(requested - joint.CommandSpeed, -slew, slew);
    }

    private static PrismaticDrivePhase Phase(SimJoint joint, bool atTarget)
    {
        if (!joint.SupplyClosed) return PrismaticDrivePhase.Unpowered;
        if (joint.Drive.Selection == PrismaticTargetSelection.Directional)
        {
            if (joint.Command == PrismaticCommand.Conflict) return PrismaticDrivePhase.Conflict;
            if (joint.Command == PrismaticCommand.Hold || atTarget) return PrismaticDrivePhase.Holding;
            return joint.Command == PrismaticCommand.Positive ? PrismaticDrivePhase.Positive : PrismaticDrivePhase.Negative;
        }
        if (atTarget) return PrismaticDrivePhase.Holding;
        return joint.Drive.Target > joint.StartCoordinate ? PrismaticDrivePhase.Positive : PrismaticDrivePhase.Negative;
    }

    public static void Solve(List<SimJoint> joints, float h)
    {
        foreach (var joint in joints)
        {
            if (!joint.MotorActive) continue;
            ref readonly var row = ref joint.Rows[(int)PrismaticRowKind.Spring];
            var inverseMass = ConstraintMass(joint.A, row.La, row.Aa, joint.B, row.Lb, row.Ab);
            if (!(inverseMass > 0f)) continue;
            var velocity = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
            var limit = joint.EffortMultiplier * joint.Drive.MaxEffort * h;
            var impulse = Math.Clamp(joint.MotorImpulse + (joint.CommandSpeed - velocity) / inverseMass, -limit, limit);
            var freeVelocity = velocity - inverseMass * joint.MotorImpulse;
            impulse = BoundWork(impulse, freeVelocity, inverseMass, joint.Drive.MaxPower * h);
            PrismaticConstraintSolver.ApplyConstraintImpulse(joint, row, impulse - joint.MotorImpulse);
            joint.MotorImpulse = impulse;
        }
    }

    private static float BoundWork(float impulse, float freeVelocity, float inverseMass, float budget)
    {
        var work = impulse * (freeVelocity + inverseMass * impulse);
        if (work <= budget) return impulse;
        var sign = MathF.Sign(impulse);
        var b = sign * freeVelocity;
        var root = MathF.Sqrt(b * b + 4f * inverseMass * budget);
        var maximum = b >= 0f ? 2f * budget / (root + b) : (root - b) / (2f * inverseMass);
        return sign * MathF.Min(MathF.Abs(impulse), maximum);
    }

    public static void Observe(SimJoint joint, List<ContactPoint> contacts)
    {
        if (!joint.MotorActive || MathF.Abs(joint.CommandSpeed) < ActuatorObservations.RestSpeed) return;
        ref readonly var row = ref joint.Rows[(int)PrismaticRowKind.Spring];
        var speed = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
        if (MathF.Abs(speed) >= ActuatorObservations.RestSpeed) return;
        foreach (var contact in contacts)
        {
            if (!(contact.LambdaN > 0f)) continue;
            var axial = new ImpulseJacobian(joint.A, row.La, row.Aa, joint.B, row.Lb, row.Ab);
            var effect = contact.LambdaN * ImpulseJacobian.Coupling(axial,
                WorkshopConstraintSolver.ContactRow(contact, contact.Normal));
            if (effect * joint.CommandSpeed >= 0f) continue;
            joint.Phase = PrismaticDrivePhase.Blocked;
            return;
        }
    }

    public static bool MeasureWork(SimJoint joint, float h)
    {
        joint.StepWork = joint.MotorActive && joint.MotorImpulse != 0f ? MathF.Max(0f,
            joint.MotorImpulse * ((Coordinate(joint) - joint.StartCoordinate) / h)) : 0f;
        return float.IsFinite(joint.StepWork) && joint.StepWork <= joint.Drive.MaxPower * h;
    }
}
