using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Abstractions.Mechanical;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal struct ConstraintRowData
{
    public Vector3 La;
    public Vector3 Lb;
    public Vector3 Aa;
    public Vector3 Ab;
    public float Error;
    /// <summary>The row's own relative-velocity target (zero for every row but a velocity drive). No pass gates it, unlike bias, so a
    /// relax pass keeps it.</summary>
    public float VelocityTarget;
    public float Gamma;
    public float Bias;
    public float Minimum;
    /// <summary>The accumulated impulse's upper bound (float.MaxValue for a row bounded only below).</summary>
    public float Maximum;

    /// <summary>A stored impulse clamped to this row's current [<see cref="Minimum"/>, <see cref="Maximum"/>], as the solver projects
    /// it: the warm start applies only an impulse the row could hold now, so a bound that narrowed since the last substep (a drive
    /// whose supply opened) never kicks.</summary>
    public readonly float Bounded(float impulse) => MathF.Min(Maximum, MathF.Max(Minimum, impulse));
}

internal sealed class SimJoint
{
    public int Offset;
    public SimBody A = null!;
    public SimBody B = null!;
    public Vector3 Pa;
    public Quaternion Qa;
    public Vector3 Pb;
    public Quaternion Qb;
    public float Lower;
    public float Upper;
    /// <summary>The joint's declared axial stiffness (N/m), which its supplied drive holds against.</summary>
    public float Stiffness;
    public SimPrismaticRetention? Retention;
    public SoftConstraintCoefficients Soft;
    public uint Collision;
    /// <summary>Refreshed each tick from the compiled supply route feeding the frame owner's PowerIn: closed while its source is
    /// enabled and every switched contact and gate law on the route passes.</summary>
    public bool SupplyClosed;
    public PrismaticDriveKind DriveKind;
    public PrismaticDriveDeclaration Drive;
    public PrismaticCommand Command;
    public PrismaticDrivePhase Phase;
    public bool Holding, MotorActive;
    public float CommandSpeed, HoldCoordinate, MotorImpulse, StartCoordinate, StepWork, Work, TickWork;
    public float EffortMultiplier = 1f;
    /// <summary>Whether the moving body took a positive normal contact impulse from a body other than the base this tick.</summary>
    public bool MovingContact;
    public float[] Impulses = new float[8];
    public ConstraintRowData[] Rows = new ConstraintRowData[8];
}

internal static class PrismaticConstraintSolver
{
    public static bool IsStop(int index) => index == (int)PrismaticRowKind.LowerStop || index == (int)PrismaticRowKind.UpperStop;

    public static void PrepareJointRows(SimJoint joint, float h)
    {
        Span<PrismaticRow> prepared = stackalloc PrismaticRow[PrismaticRows.Count];
        var frameA = new ConstraintBodyFrame(joint.A.Position, joint.A.Rotation, joint.A.CentreOfMass);
        var frameB = new ConstraintBodyFrame(joint.B.Position, joint.B.Rotation, joint.B.CentreOfMass);
        var localA = new ConstraintFrame(joint.Pa, joint.Qa);
        var localB = new ConstraintFrame(joint.Pb, joint.Qb);

        var lower = joint.Retention is { Mode: RetentionMode.Releasing } released ? released.Bound : joint.Lower;
        var upper = joint.Retention is { Mode: RetentionMode.Retained } retained ? retained.Bound : joint.Upper;
        PrismaticRows.Prepare(frameA, frameB, localA, localB, lower, upper, prepared);

        for (var i = 0; i < 8; i++)
        {
            var row = prepared[i];
            if (i == (int)PrismaticRowKind.Spring && joint.Holding)
                row = row with { Error = row.Error - joint.HoldCoordinate };
            var gamma = i == (int)PrismaticRowKind.Spring && !joint.Holding ? joint.Soft.Gamma : 0f;
            var bias = i == (int)PrismaticRowKind.Spring && !joint.Holding ? joint.Soft.BiasRate : (IsStop(i) && row.Error >= 0f ? 1f : 0.2f) / h;
            var freeAxial = i == (int)PrismaticRowKind.Spring && !joint.Holding && joint.Soft == default;
            var minimum = IsStop(i) || freeAxial ? 0f : -3.4028234663852886e38f;

            joint.Rows[i] = new ConstraintRowData
            {
                La = row.LinearA,
                Lb = row.LinearB,
                Aa = row.AngularA,
                Ab = row.AngularB,
                Error = row.Error,
                Gamma = gamma,
                Bias = bias,
                Minimum = minimum,
                Maximum = freeAxial ? 0f : 3.4028234663852886e38f
            };
        }
    }

    public static void ApplyConstraintImpulse(SimJoint joint, in ConstraintRowData row, float impulse)
    {
        new ImpulseJacobian(joint.A, row.La, row.Aa, joint.B, row.Lb, row.Ab).Apply(impulse);
    }

    private static int SelectIndependentGroup(List<SimJoint> joints, Span<int> remaining, ref int remainingCount,
        Span<int> groupIndices, int rowIndex)
    {
        var groupCount = 0;

        for (var i = 0; i < remainingCount && groupCount < 4;)
        {
            var jointIdx = remaining[i];
            var joint = joints[jointIdx];
            if (joint.Holding && rowIndex == (int)PrismaticRowKind.Spring)
            {
                remaining[i] = remaining[--remainingCount];
                continue;
            }

            var conflict = false;
            for (var g = 0; g < groupCount; g++)
            {
                var gj = joints[groupIndices[g]];
                if (Coupled(gj.A, gj.B, joint.A, joint.B))
                {
                    conflict = true;
                    break;
                }
            }

            if (conflict)
            {
                i++;
                continue;
            }

            groupIndices[groupCount++] = jointIdx;
            remaining[i] = remaining[remainingCount - 1];
            remainingCount--;
        }

        return groupCount;
    }

    public static void SweepConstraints(List<SimJoint> joints, bool useBias)
    {
        var count = joints.Count;
        if (count == 0) return;

        Span<float> invMasses = stackalloc float[4];
        Span<float> vels = stackalloc float[4];
        Span<float> errors = stackalloc float[4];
        Span<float> gammas = stackalloc float[4];
        Span<float> biases = stackalloc float[4];
        Span<float> accum = stackalloc float[4];
        Span<float> mins = stackalloc float[4];
        Span<float> maxs = stackalloc float[4];
        Span<float> targets = stackalloc float[4];

        Span<int> remaining = stackalloc int[count];
        Span<int> groupIndices = stackalloc int[4];

        for (var rowIndex = 5; rowIndex < 8; rowIndex++)
        {
            for (var i = 0; i < count; i++) remaining[i] = i;
            var remainingCount = count;

            while (remainingCount > 0)
            {
                var groupCount = SelectIndependentGroup(joints, remaining, ref remainingCount, groupIndices, rowIndex);

                for (var lane = 0; lane < 4; lane++)
                {
                    if (lane < groupCount)
                    {
                        var joint = joints[groupIndices[lane]];
                        ref readonly var row = ref joint.Rows[rowIndex];
                        invMasses[lane] = ConstraintMass(joint.A, row.La, row.Aa, joint.B, row.Lb, row.Ab);
                        vels[lane] = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
                        errors[lane] = row.Error;
                        gammas[lane] = row.Gamma;
                        biases[lane] = useBias || rowIndex == (int)PrismaticRowKind.Spring || (IsStop(rowIndex) && row.Error > 0f) ? row.Bias : 0f;
                        accum[lane] = joint.Impulses[rowIndex];
                        mins[lane] = row.Minimum;
                        maxs[lane] = row.Maximum;
                        targets[lane] = row.VelocityTarget;
                    }
                    else
                    {
                        invMasses[lane] = 0f;
                        vels[lane] = 0f;
                        errors[lane] = 0f;
                        gammas[lane] = 0f;
                        biases[lane] = 0f;
                        accum[lane] = 0f;
                        mins[lane] = 0f;
                        maxs[lane] = 0f;
                        targets[lane] = 0f;
                    }
                }

                var vInvMass = Vector128.Create(invMasses[0], invMasses[1], invMasses[2], invMasses[3]);
                var vVel = Vector128.Create(vels[0], vels[1], vels[2], vels[3]);
                var vTarget = Vector128.Create(targets[0], targets[1], targets[2], targets[3]);
                var vError = Vector128.Create(errors[0], errors[1], errors[2], errors[3]);
                var vGamma = Vector128.Create(gammas[0], gammas[1], gammas[2], gammas[3]);
                var vBias = Vector128.Create(biases[0], biases[1], biases[2], biases[3]);
                var vAccum = Vector128.Create(accum[0], accum[1], accum[2], accum[3]);
                var vMin = Vector128.Create(mins[0], mins[1], mins[2], mins[3]);
                var vMax = Vector128.Create(maxs[0], maxs[1], maxs[2], maxs[3]);

                var result = SoftConstraint.Solve4(vInvMass, vVel, vTarget, vError, vGamma, vBias, vAccum, vMin, vMax);

                for (var lane = 0; lane < groupCount; lane++)
                {
                    var joint = joints[groupIndices[lane]];
                    joint.Impulses[rowIndex] = result.Accumulated.GetElement(lane);
                    ApplyConstraintImpulse(joint, joint.Rows[rowIndex], result.Delta.GetElement(lane));
                }
            }
        }
    }

}
