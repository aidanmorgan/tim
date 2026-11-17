using System;
using System.Collections.Immutable;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Abstractions;

/// <summary>Optional circular rim artwork. Physical transmission does not require cosmetics.</summary>
public readonly record struct MechanicalRimDescriptor(PackageBodyIndex Support, Vector3 Centre, Quaternion Frame,
    float Radius, float AnglePerCoordinate)
{
    public void Validate()
    {
        // A body-local rigid pose and the shared circular-feature envelope, independent of any collider.
        new RigidLocalPose(new(Centre.X, Centre.Y, Centre.Z), new(Frame.X, Frame.Y, Frame.Z, Frame.W)).Validate();
        if (Support.Value < 0 || !float.IsFinite(Radius) || Radius < 1f / 16f || Radius > 16f ||
            !float.IsFinite(AnglePerCoordinate) || AnglePerCoordinate == 0f)
            throw new ArgumentException("Invalid mechanical rim presentation.");
    }
    public static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public readonly record struct CompiledMechanicalRim(GpuBodyId Support, RevoluteConstraintId Joint,
    GpuBodyId CoordinateSupport, Vector3 Centre, Quaternion Frame, float Radius, float AnglePerCoordinate);

public readonly record struct CompiledMechanicalLink(AnimationTargetId Target, CompiledMechanicalRim Source, CompiledMechanicalRim TargetRim);

/// <summary>One complete animation-owned route; positions are world metres at the named committed tick.</summary>
public sealed record MechanicalLinkGeometry(AnimationTargetId Target, SimulationTick Tick,
    ImmutableArray<Vector3> Points, Vector3 SourceMarker, Vector3 TargetMarker)
{
    public const ulong TargetBase = 6UL << 32;
    public void Validate()
    {
        if (Target.Value == 0 || Points.IsDefault || Points.Length < 5 ||
            !MechanicalRimDescriptor.Finite(SourceMarker) || !MechanicalRimDescriptor.Finite(TargetMarker))
            throw new ArgumentException("Invalid mechanical link geometry.");
        foreach (var point in Points)
            if (!MechanicalRimDescriptor.Finite(point)) throw new ArgumentException("Nonfinite mechanical link vertex.");
        if (Points[0] != Points[^1]) throw new ArgumentException("Mechanical route must close.");
    }
}

public sealed record MechanicalLinkSample(SimulationEpoch World, ulong Generation, PulseOrdinal Pulse,
    MasterTimeNanoseconds AppliedAt, MechanicalLinkGeometry Geometry);
