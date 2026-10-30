using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal sealed class SimRevoluteJoint
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
    public float Stiffness;
    public float Damping;
    public SoftConstraintCoefficients Soft;
    public uint Collision;
    public float[] Impulses = new float[8];
    public ConstraintRowData[] Rows = new ConstraintRowData[8];
}

internal static class RevoluteConstraintSolver
{
    public static bool IsStop(int index) => index == (int)RevoluteRowKind.LowerStop || index == (int)RevoluteRowKind.UpperStop;

    public static void PrepareJointRows(SimRevoluteJoint joint, float h)
    {
        Span<RevoluteRow> prepared = stackalloc RevoluteRow[RevoluteRows.Count];
        var frameA = new ConstraintBodyFrame(joint.A.Position, joint.A.Rotation, joint.A.CentreOfMass);
        var frameB = new ConstraintBodyFrame(joint.B.Position, joint.B.Rotation, joint.B.CentreOfMass);
        var localA = new ConstraintFrame(joint.Pa, joint.Qa);
        var localB = new ConstraintFrame(joint.Pb, joint.Qb);

        RevoluteRows.Prepare(frameA, frameB, localA, localB, joint.Lower, joint.Upper, prepared);

        for (var i = 0; i < 8; i++)
        {
            var row = prepared[i];
            float gamma;
            float bias;
            float minimum;

            if (i == (int)RevoluteRowKind.Axial)
            {
                if (joint.Stiffness > 0f || joint.Damping > 0f)
                {
                    gamma = joint.Soft.Gamma;
                    bias = joint.Soft.BiasRate;
                }
                else
                {
                    gamma = 0f;
                    bias = 0f;
                }
                minimum = -3.4028234663852886e38f;
            }
            else if (IsStop(i))
            {
                gamma = 0f;
                bias = (row.Error < 0f ? 1f : 0.2f) / h;
                minimum = 0f;
            }
            else
            {
                gamma = 0f;
                bias = 0.2f / h;
                minimum = -3.4028234663852886e38f;
            }

            joint.Rows[i] = new ConstraintRowData
            {
                La = row.LinearA,
                Lb = row.LinearB,
                Aa = row.AngularA,
                Ab = row.AngularB,
                Error = row.Error,
                Gamma = gamma,
                Bias = bias,
                Minimum = minimum
            };
        }
    }

    public static void ApplyConstraintImpulse(SimRevoluteJoint joint, in ConstraintRowData row, float impulse)
    {
        ApplyBodyImpulse(joint.A, row.La, row.Aa, impulse);
        ApplyBodyImpulse(joint.B, row.Lb, row.Ab, impulse);
    }

    public static void SweepConstraints(List<SimRevoluteJoint> joints, bool useBias)
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

        Span<int> remaining = stackalloc int[count];
        Span<int> groupIndices = stackalloc int[4];

        for (var rowIndex = 0; rowIndex < 8; rowIndex++)
        {
            if (count == 1)
            {
                var joint = joints[0];
                if (rowIndex == (int)RevoluteRowKind.Axial && joint.Stiffness == 0f && joint.Damping == 0f)
                {
                    joint.Impulses[rowIndex] = 0f;
                    continue;
                }
                ref readonly var row = ref joint.Rows[rowIndex];
                var invMass = ConstraintMass(joint.A, row.La, row.Aa) + ConstraintMass(joint.B, row.Lb, row.Ab);
                var vel = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
                var bias = useBias || (rowIndex == (int)RevoluteRowKind.Axial && (joint.Stiffness > 0f || joint.Damping > 0f)) || (IsStop(rowIndex) && row.Error < 0f) ? row.Bias : 0f;

                var vInvMass = Vector128.Create(invMass, 0f, 0f, 0f);
                var vVel = Vector128.Create(vel, 0f, 0f, 0f);
                var vError = Vector128.Create(row.Error, 0f, 0f, 0f);
                var vGamma = Vector128.Create(row.Gamma, 0f, 0f, 0f);
                var vBias = Vector128.Create(bias, 0f, 0f, 0f);
                var vAccum = Vector128.Create(joint.Impulses[rowIndex], 0f, 0f, 0f);
                var vMin = Vector128.Create(row.Minimum, 0f, 0f, 0f);
                var vMax = Vector128.Create(3.4028234663852886e38f, 0f, 0f, 0f);

                var result = SoftConstraint.Solve4(vInvMass, vVel, vError, vGamma, vBias, vAccum, vMin, vMax);
                joint.Impulses[rowIndex] = result.Accumulated.GetElement(0);
                ApplyConstraintImpulse(joint, joint.Rows[rowIndex], result.Delta.GetElement(0));
                continue;
            }

            for (var i = 0; i < count; i++) remaining[i] = i;
            var remainingCount = count;

            while (remainingCount > 0)
            {
                var groupCount = 0;

                for (var i = 0; i < remainingCount && groupCount < 4;)
                {
                    var jointIdx = remaining[i];
                    var joint = joints[jointIdx];

                    var conflict = false;
                    for (var g = 0; g < groupCount; g++)
                    {
                        var gj = joints[groupIndices[g]];
                        if (gj.A.BodySlot == joint.A.BodySlot || gj.A.BodySlot == joint.B.BodySlot ||
                            gj.B.BodySlot == joint.A.BodySlot || gj.B.BodySlot == joint.B.BodySlot)
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

                for (var lane = 0; lane < 4; lane++)
                {
                    if (lane < groupCount)
                    {
                        var joint = joints[groupIndices[lane]];
                        if (rowIndex == (int)RevoluteRowKind.Axial && joint.Stiffness == 0f && joint.Damping == 0f)
                        {
                            invMasses[lane] = 0f;
                            vels[lane] = 0f;
                            errors[lane] = 0f;
                            gammas[lane] = 0f;
                            biases[lane] = 0f;
                            accum[lane] = 0f;
                            mins[lane] = 0f;
                            maxs[lane] = 0f;
                        }
                        else
                        {
                            ref readonly var row = ref joint.Rows[rowIndex];
                            invMasses[lane] = ConstraintMass(joint.A, row.La, row.Aa) + ConstraintMass(joint.B, row.Lb, row.Ab);
                            vels[lane] = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
                            errors[lane] = row.Error;
                            gammas[lane] = row.Gamma;
                            biases[lane] = useBias || (rowIndex == (int)RevoluteRowKind.Axial && (joint.Stiffness > 0f || joint.Damping > 0f)) || (IsStop(rowIndex) && row.Error < 0f) ? row.Bias : 0f;
                            accum[lane] = joint.Impulses[rowIndex];
                            mins[lane] = row.Minimum;
                            maxs[lane] = 3.4028234663852886e38f;
                        }
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
                    }
                }

                var vInvMass = Vector128.Create(invMasses[0], invMasses[1], invMasses[2], invMasses[3]);
                var vVel = Vector128.Create(vels[0], vels[1], vels[2], vels[3]);
                var vError = Vector128.Create(errors[0], errors[1], errors[2], errors[3]);
                var vGamma = Vector128.Create(gammas[0], gammas[1], gammas[2], gammas[3]);
                var vBias = Vector128.Create(biases[0], biases[1], biases[2], biases[3]);
                var vAccum = Vector128.Create(accum[0], accum[1], accum[2], accum[3]);
                var vMin = Vector128.Create(mins[0], mins[1], mins[2], mins[3]);
                var vMax = Vector128.Create(maxs[0], maxs[1], maxs[2], maxs[3]);

                var result = SoftConstraint.Solve4(vInvMass, vVel, vError, vGamma, vBias, vAccum, vMin, vMax);

                for (var lane = 0; lane < groupCount; lane++)
                {
                    var joint = joints[groupIndices[lane]];
                    if (rowIndex == (int)RevoluteRowKind.Axial && joint.Stiffness == 0f && joint.Damping == 0f)
                    {
                        joint.Impulses[rowIndex] = 0f;
                        continue;
                    }
                    joint.Impulses[rowIndex] = result.Accumulated.GetElement(lane);
                    ApplyConstraintImpulse(joint, joint.Rows[rowIndex], result.Delta.GetElement(lane));
                }
            }
        }
    }
}
