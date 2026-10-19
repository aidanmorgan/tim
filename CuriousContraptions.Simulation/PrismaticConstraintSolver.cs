using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal struct ConstraintRowData
{
    public Vector3 La;
    public Vector3 Lb;
    public Vector3 Aa;
    public Vector3 Ab;
    public float Error;
    public float Gamma;
    public float Bias;
    public float Minimum;
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
    public SoftConstraintCoefficients Soft;
    public uint Collision;
    /// <summary>Refreshed each tick from the committed switched route (State Latch or Counter contact) feeding the frame owner's PowerIn.</summary>
    public bool SwitchedSupplyClosed;
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

        PrismaticRows.Prepare(frameA, frameB, localA, localB, joint.Lower, joint.Upper, prepared);

        for (var i = 0; i < 8; i++)
        {
            var row = prepared[i];
            var gamma = i == (int)PrismaticRowKind.Spring ? joint.Soft.Gamma : 0f;
            var bias = i == (int)PrismaticRowKind.Spring ? joint.Soft.BiasRate : (IsStop(i) && row.Error >= 0f ? 1f : 0.2f) / h;
            var minimum = IsStop(i) ? 0f : -3.4028234663852886e38f;

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

    public static Vector3 ConstraintInverseInertia(SimBody body, Vector3 angular)
    {
        var m = body.InvIWorld;
        return new Vector3(
            m[0] * angular.X + m[1] * angular.Y + m[2] * angular.Z,
            m[3] * angular.X + m[4] * angular.Y + m[5] * angular.Z,
            m[6] * angular.X + m[7] * angular.Y + m[8] * angular.Z);
    }

    public static float ConstraintMass(SimBody body, Vector3 linear, Vector3 angular)
    {
        if (body.Motion != 1) return 0f;
        return body.InvMass * Vector3.Dot(linear, linear) + Vector3.Dot(angular, ConstraintInverseInertia(body, angular));
    }

    public static float ConstraintVelocity(SimBody body, Vector3 linear, Vector3 angular) =>
        Vector3.Dot(linear, body.LinearVelocity) + Vector3.Dot(angular, body.AngularVelocity);

    private static void ApplyBodyImpulse(SimBody body, Vector3 linear, Vector3 angular, float impulse)
    {
        if (body.Motion != 1) return;
        body.Vx += linear.X * impulse * body.InvMass;
        body.Vy += linear.Y * impulse * body.InvMass;
        body.Vz += linear.Z * impulse * body.InvMass;
        var dw = ConstraintInverseInertia(body, angular * impulse);
        body.Wx += dw.X;
        body.Wy += dw.Y;
        body.Wz += dw.Z;
    }

    public static void ApplyConstraintImpulse(SimJoint joint, in ConstraintRowData row, float impulse)
    {
        ApplyBodyImpulse(joint.A, row.La, row.Aa, impulse);
        ApplyBodyImpulse(joint.B, row.Lb, row.Ab, impulse);
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

        Span<int> remaining = stackalloc int[count];
        Span<int> groupIndices = stackalloc int[4];

        for (var rowIndex = 0; rowIndex < 8; rowIndex++)
        {
            if (count == 1)
            {
                var joint = joints[0];
                ref readonly var row = ref joint.Rows[rowIndex];
                var invMass = ConstraintMass(joint.A, row.La, row.Aa) + ConstraintMass(joint.B, row.Lb, row.Ab);
                var vel = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
                var bias = useBias || rowIndex == (int)PrismaticRowKind.Spring || (IsStop(rowIndex) && row.Error > 0f) ? row.Bias : 0f;

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
                        ref readonly var row = ref joint.Rows[rowIndex];
                        invMasses[lane] = ConstraintMass(joint.A, row.La, row.Aa) + ConstraintMass(joint.B, row.Lb, row.Ab);
                        vels[lane] = ConstraintVelocity(joint.A, row.La, row.Aa) + ConstraintVelocity(joint.B, row.Lb, row.Ab);
                        errors[lane] = row.Error;
                        gammas[lane] = row.Gamma;
                        biases[lane] = useBias || rowIndex == (int)PrismaticRowKind.Spring || (IsStop(rowIndex) && row.Error > 0f) ? row.Bias : 0f;
                        accum[lane] = joint.Impulses[rowIndex];
                        mins[lane] = row.Minimum;
                        maxs[lane] = 3.4028234663852886e38f;
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
                    joint.Impulses[rowIndex] = result.Accumulated.GetElement(lane);
                    ApplyConstraintImpulse(joint, joint.Rows[rowIndex], result.Delta.GetElement(lane));
                }
            }
        }
    }

    public static void ActuatorSliderDrive(List<SimJoint> joints, bool batteryPowered, bool plateLoaded, float dt)
    {
        foreach (var joint in joints)
        {
            if (joint.Lower == 0f && MathF.Abs(joint.Upper - 1.55f) < 0.01f)
            {
                var b = joint.B;
                var frame = joint.A;
                if (b == null || frame == null) continue;

                var gatePowered = (b.Drag > 0.03f && b.Drag < 0.08f && batteryPowered) ||
                                  (b.Drag >= 0.08f && b.Drag < 0.11f && batteryPowered && plateLoaded) ||
                                  joint.SwitchedSupplyClosed;

                if (gatePowered)
                {
                    var frameUp = Vector3.Transform(Vector3.UnitY, frame.Rotation);
                    var rel = b.Position - frame.Position;
                    var pos = Vector3.Dot(rel, frameUp);
                    var currentSpeed = Vector3.Dot(b.LinearVelocity, frameUp);
                    var d = 1.55f - pos;
                    var targetSpeed = MathF.Sign(d) * MathF.Min(2.8f, MathF.Sqrt(2f * 14f * MathF.Abs(d)));
                    var desiredDeltaV = targetSpeed - currentSpeed;
                    var maxDeltaV = 14f * dt;
                    var deltaV = Math.Clamp(desiredDeltaV, -maxDeltaV, maxDeltaV);

                    var fAcc = b.Mass * (deltaV / dt);
                    var fSpring = 14f * pos;
                    var fGravity = -b.Mass * Vector3.Dot(new Vector3(b.Gx, b.Gy, b.Gz), frameUp);
                    var force = Math.Clamp(fAcc + fSpring + fGravity, -60f, 60f);
                    var finalDeltaV = (force / b.Mass) * dt;

                    b.Vx += frameUp.X * finalDeltaV;
                    b.Vy += frameUp.Y * finalDeltaV;
                    b.Vz += frameUp.Z * finalDeltaV;
                }
            }
        }
    }
}
