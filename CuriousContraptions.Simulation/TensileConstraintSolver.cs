using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal sealed class SimTensileSpan
{
    public int Offset;
    public SimBody A = null!;
    public SimBody B = null!;
    public Vector3 LocalA, LocalB;
    public float Maximum, Impulse;
    public bool Complete, Cut, Active;
    public ConstraintRowData Row;
}

/// <summary>The generic projected length row, using the shared implicit soft law.</summary>
internal static class TensileConstraintSolver
{
    public static void Prepare(SimTensileSpan span, float step)
    {
        var a = span.A.Position + Vector3.Transform(span.LocalA, span.A.Rotation);
        var b = span.B.Position + Vector3.Transform(span.LocalB, span.B.Rotation);
        var delta = b - a;
        var length = delta.Length();
        span.Active = span.Complete && !span.Cut && length >= span.Maximum && length > 0f && float.IsFinite(length);
        if (!span.Active) { span.Impulse = 0f; span.Row = default; return; }
        var axis = delta / length;
        var row = new ConstraintRowData
        {
            La = axis, Lb = -axis,
            Aa = Vector3.Cross(a - span.A.CentreOfMass, axis),
            Ab = Vector3.Cross(b - span.B.CentreOfMass, -axis),
            Error = span.Maximum - length, Minimum = 0f
        };
        var inverseMass = ConstraintMass(span.A, row.La, row.Aa) + ConstraintMass(span.B, row.Lb, row.Ab);
        if (!(inverseMass > 0f)) { span.Active = false; span.Impulse = 0f; return; }
        const float omega = 2f * MathF.PI * 60f;
        var soft = SoftConstraintCoefficients.FromSpring(omega * omega / inverseMass, 20f * omega / inverseMass, step);
        row.Gamma = soft.Gamma; row.Bias = soft.BiasRate;
        span.Row = row;
    }

    public static void Apply(SimTensileSpan span, float impulse)
    {
        if (!span.Active) return;
        ApplyBodyImpulse(span.A, span.Row.La, span.Row.Aa, impulse);
        ApplyBodyImpulse(span.B, span.Row.Lb, span.Row.Ab, impulse);
    }

    public static void Solve(SimTensileSpan span, bool bias)
    {
        if (!span.Active) return;
        ref readonly var row = ref span.Row;
        var k = ConstraintMass(span.A, row.La, row.Aa) + ConstraintMass(span.B, row.Lb, row.Ab);
        var velocity = ConstraintVelocity(span.A, row.La, row.Aa) + ConstraintVelocity(span.B, row.Lb, row.Ab);
        var result = SoftConstraint.Solve4(Vector128.Create(k), Vector128.Create(velocity),
            Vector128.Create(row.Error), Vector128.Create(row.Gamma), Vector128.Create(bias ? row.Bias : 0f),
            Vector128.Create(span.Impulse), Vector128<float>.Zero, Vector128.Create(float.MaxValue));
        span.Impulse = result.Accumulated.GetElement(0);
        Apply(span, result.Delta.GetElement(0));
    }
}
