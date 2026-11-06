using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

public readonly record struct RopeRouteId(ulong Value);
public readonly record struct RopeSpanId(ulong Value);
public readonly record struct RopeGuideId(ulong Value);
public enum RopeIntegrity : uint { Intact = 1, Cut = 2 }
public enum RopeState : uint { Open = 1, Slack = 2, Taut = 3 }

public readonly record struct RopeGuideDeclaration(RopeGuideId Id, GpuBodyId Body, float Radius, Vector3 Offset)
{
    public void Validate()
    {
        if (Id.Value == 0) throw new ArgumentException("Invalid rope guide identity.");
        if (Body.Value == 0) throw new ArgumentException("Invalid rope guide body.");
        if (!float.IsFinite(Radius) || Radius <= 0f) throw new ArgumentException("Invalid rope guide radius.");
        if (!float.IsFinite(Offset.X) || !float.IsFinite(Offset.Y) || !float.IsFinite(Offset.Z))
            throw new ArgumentException("Nonfinite rope guide offset.");
    }
}

/// <summary>One ordered straight span.</summary>
public readonly record struct TensileSpanDeclaration(RopeSpanId Id, RopeRouteId Route, uint Order,
    GpuBodyId BodyA, Vector3 LocalA, GpuBodyId BodyB, Vector3 LocalB, float MaximumLength, bool Complete,
    float GuideRadius = 0f, Vector3 GuideOffset = default, RopeGuideId Guide = default)
{
    public void Validate()
    {
        if (Id.Value == 0 || Route.Value == 0 || BodyA.Value == 0 || BodyB.Value == 0 || BodyA == BodyB)
            throw new ArgumentException("Invalid tensile span ownership.");
        PhysicsDeclarationBounds.Range(MaximumLength, .05f, 200f);
        if (!Finite(LocalA) || !Finite(LocalB)) throw new ArgumentException("Nonfinite tensile attachment.");
        if (!float.IsFinite(GuideRadius) || GuideRadius < 0f) throw new ArgumentException("Invalid guide radius.");
        if (!Finite(GuideOffset)) throw new ArgumentException("Nonfinite guide offset.");
        if ((Guide.Value == 0) != (GuideRadius == 0f) || (Guide.Value == 0 && GuideOffset != Vector3.Zero))
            throw new ArgumentException("Guide identity and geometry must be declared together.");
    }
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
