using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct RopeRouteId(ulong Value);
public readonly record struct RopeSpanId(ulong Value);
public enum RopeIntegrity : uint { Intact = 1, Cut = 2 }
public enum RopeState : uint { Open = 1, Slack = 2, Taut = 3 }

/// <summary>One ordered straight span. Complete routes currently contain exactly one span;
/// incomplete declared-guide routes carry geometry but no force equation.</summary>
public readonly record struct TensileSpanDeclaration(RopeSpanId Id, RopeRouteId Route, uint Order,
    GpuBodyId BodyA, Vector3 LocalA, GpuBodyId BodyB, Vector3 LocalB, float MaximumLength, bool Complete)
{
    public void Validate()
    {
        if (Id.Value == 0 || Route.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB)
            throw new ArgumentException("Invalid tensile span ownership.");
        PhysicsDeclarationBounds.Range(MaximumLength, .05f, 200f);
        if (!Finite(LocalA) || !Finite(LocalB)) throw new ArgumentException("Nonfinite tensile attachment.");
    }
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
