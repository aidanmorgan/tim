using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Test construction of real shared compound queries; no alternate collision solver.</summary>
internal sealed class HollowBoxTestProbe
{
    private static readonly HollowGeometrySettings Precision = new(.005);
    private readonly CompoundMotion _box;
    private readonly CompoundMotion _wall;
    public double MaximumSurfaceError { get; }

    public HollowBoxTestProbe(Transform3D pose, Vector3 half, TubeProxy tube)
        : this(pose, half, tube.Pose,
            HollowGeometry.Tube(tube.HalfLength, tube.InnerRadius, tube.OuterRadius, Precision)) { }

    public HollowBoxTestProbe(Transform3D pose, Vector3 half, FrustumProxy funnel)
        : this(pose, half, funnel.Pose, HollowGeometry.Frustum(funnel.HalfLength,
            funnel.InletRadius, funnel.OutletRadius, funnel.Thickness, Precision)) { }

    private HollowBoxTestProbe(Transform3D pose, Vector3 half, Transform3D shellPose, HollowGeometryResult geometry)
    {
        var boxBody = new PhysicsBody(new(0), PhysicsMotionType.Static, SceneGeometryAdapter.CaptureRigidPose(pose), default, default);
        var shellBody = new PhysicsBody(new(1), PhysicsMotionType.Static, SceneGeometryAdapter.CaptureRigidPose(shellPose), default, default);
        _box = new(new([new(new ConvexBox(SceneGeometryAdapter.CaptureVector(half)), AffineTransform.Identity)]),
            boxBody.CreateTrajectory(0, default));
        _wall = new(geometry.Geometry, shellBody.CreateTrajectory(0, default));
        MaximumSurfaceError = geometry.MaximumSurfaceError;
    }

    public CompoundSweepResult Query(double minimumSeparation = 0) =>
        CompoundCollision.Cast(_box, _wall, 0, minimumSeparation);

    // Valid only outside the union. Minimum child penetration is NOT union penetration depth.
    public (double Lower, double Upper) Clearance()
    {
        var lower = double.PositiveInfinity;
        var upper = double.PositiveInfinity;
        for (var i = 0; i < _wall.Count; i++)
        {
            var separation = ConvexSeparation.Query(_box.Child(new(0)).At(0), _wall.Child(new(i)).At(0));
            lower = Math.Min(lower, separation.LowerBound);
            upper = Math.Min(upper, separation.UpperBound);
        }
        if (lower <= 0) throw new InvalidOperationException("Clearance requires a separated box.");
        return (lower, upper);
    }

    public void AssertAnalyticClearance(double ideal)
    {
        var bounds = Clearance();
        Assert.InRange(bounds.Lower, ideal - MaximumSurfaceError - 1e-6, ideal + 1e-6);
        Assert.InRange(bounds.Upper, ideal - MaximumSurfaceError - 1e-6, ideal + 1e-6);
        AssertThreshold(bounds);
    }

    public void AssertThreshold((double Lower, double Upper) bounds)
    {
        Assert.Equal(ConvexSweepStatus.Clear, Query(bounds.Lower - .00001).Status);
        Assert.Equal(ConvexSweepStatus.InitialContact, Query(bounds.Upper + .00001).Status);
    }

    public static bool InteriorWitness(Transform3D pose, Vector3 half, TubeProxy tube, int subdivisions, double depth) =>
        InteriorWitness(pose, half, tube.Pose, tube.HalfLength, tube.InnerRadius, tube.InnerRadius,
            tube.OuterRadius - tube.InnerRadius, subdivisions, depth);

    public static bool InteriorWitness(Transform3D pose, Vector3 half, FrustumProxy funnel, int subdivisions, double depth) =>
        InteriorWitness(pose, half, funnel.Pose, funnel.HalfLength, funnel.InletRadius, funnel.OutletRadius,
            funnel.Thickness, subdivisions, depth);

    // A found point proves intersection with the ideal authored shell. Absence is inconclusive.
    // These scalar point inequalities are independent of GJK/EPA, compound construction and sweeping.
    private static bool InteriorWitness(Transform3D pose, Vector3 half, Transform3D shellPose,
        double length, double inlet, double outlet, double thickness, int subdivisions, double depth)
    {
        if (subdivisions < 1 || !double.IsFinite(depth) || depth < 0)
            throw new ArgumentOutOfRangeException(nameof(subdivisions));
        var local = shellPose.AffineInverse() * pose;
        var slope = (outlet - inlet) / (2 * length);
        var radialDepth = depth * Math.Sqrt(1 + slope * slope);
        for (var x = 0; x <= subdivisions; x++)
        for (var y = 0; y <= subdivisions; y++)
        for (var z = 0; z <= subdivisions; z++)
        {
            var p = local * new Vector3(half.X * (2f * x / subdivisions - 1),
                half.Y * (2f * y / subdivisions - 1), half.Z * (2f * z / subdivisions - 1));
            if (Math.Abs(p.X) >= length - depth) continue;
            var inner = inlet + slope * (p.X + length);
            var radius = Math.Sqrt((double)p.Y * p.Y + (double)p.Z * p.Z);
            if (radius > inner + radialDepth && radius < inner + thickness - radialDepth) return true;
        }
        return false;
    }
}
