using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using static CuriousContraptions.Simulation.ConstraintBodyImpulse;

namespace CuriousContraptions.Simulation;

internal sealed class SimTensileGuide
{
    public SimBody Body;
    public float Radius;
    public Vector3 Offset;

    public SimTensileGuide(SimBody body, float radius, Vector3 offset)
    {
        Body = body;
        Radius = radius;
        Offset = offset;
    }
}

internal sealed class SimTensileRoute
{
    public SimBody A = null!;
    public SimBody B = null!;
    public Vector3 LocalA, LocalB;
    public SimTensileGuide[] Guides = [];
    public float Maximum, Impulse, Travel;
    public bool Complete, Cut, Active;
    public ConstraintRowData Row;
    public int[] Offsets = [];
    public Vector3 UnitA, UnitB;
}

/// <summary>The generic projected length row, using the shared implicit soft law.</summary>
internal static class TensileConstraintSolver
{
    public static void Prepare(SimTensileRoute route, float step)
    {
        var a = route.A.Position + Vector3.Transform(route.LocalA, route.A.Rotation);
        var b = route.B.Position + Vector3.Transform(route.LocalB, route.B.Rotation);

        var guides = new RouteGuideFrame[route.Guides.Length];
        for (var i = 0; i < route.Guides.Length; i++)
            guides[i] = new(route.Guides[i].Body.Position, route.Guides[i].Body.Rotation,
                            route.Guides[i].Radius, route.Guides[i].Offset);

        var evaluated = RopeGeometry.EvaluateRoute(a, b, guides);
        var length = evaluated.TotalLength;
        var axisA = evaluated.UnitA;
        var axisB = evaluated.UnitB;

        route.UnitA = axisA;
        route.UnitB = axisB;
        route.Active = route.Complete && !route.Cut && length >= route.Maximum && length > 1e-6f && float.IsFinite(length);
        if (!route.Active) { route.Impulse = 0f; route.Row = default; return; }

        var row = new ConstraintRowData
        {
            La = axisA, Lb = axisB,
            Aa = Vector3.Cross(a - route.A.CentreOfMass, axisA),
            Ab = Vector3.Cross(b - route.B.CentreOfMass, axisB),
            Error = route.Maximum - length, Minimum = 0f, Maximum = float.MaxValue
        };
        var inverseMass = ConstraintMass(route.A, row.La, row.Aa) + ConstraintMass(route.B, row.Lb, row.Ab);
        if (!(inverseMass > 0f)) { route.Active = false; route.Impulse = 0f; return; }
        const float omega = 2f * MathF.PI * 60f;
        var soft = SoftConstraintCoefficients.FromSpring(omega * omega / inverseMass, 20f * omega / inverseMass, step);
        row.Gamma = soft.Gamma; row.Bias = soft.BiasRate;
        route.Row = row;
    }

    public static void Apply(SimTensileRoute route, float impulse)
    {
        if (!route.Active) return;
        ApplyBodyImpulse(route.A, route.Row.La, route.Row.Aa, impulse);
        ApplyBodyImpulse(route.B, route.Row.Lb, route.Row.Ab, impulse);
    }

    /// <summary>One sweep of the length row. Its softness is stabilisation, not a declared spring, so like a contact row it acts only
    /// in the solve pass: in relax the row is rigid (γ = 0) with no bias. A row soft in relax converges to Jv = −γλ there and commits
    /// that false lengthening velocity, F·m_inv/(2ζω + hω²), on a load the rope holds at rest.</summary>
    public static void Solve(SimTensileRoute route, bool solvePass)
    {
        if (!route.Active) return;
        ref readonly var row = ref route.Row;
        var k = ConstraintMass(route.A, row.La, row.Aa) + ConstraintMass(route.B, row.Lb, row.Ab);
        var velocity = ConstraintVelocity(route.A, row.La, row.Aa) + ConstraintVelocity(route.B, row.Lb, row.Ab);
        var result = SoftConstraint.Solve4(Vector128.Create(k), Vector128.Create(velocity), Vector128.Create(row.VelocityTarget),
            Vector128.Create(row.Error), Vector128.Create(solvePass ? row.Gamma : 0f), Vector128.Create(solvePass ? row.Bias : 0f),
            Vector128.Create(route.Impulse), Vector128.Create(row.Minimum), Vector128.Create(row.Maximum));
        route.Impulse = result.Accumulated.GetElement(0);
        Apply(route, result.Delta.GetElement(0));
    }
}
