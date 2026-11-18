using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class HollowBoundsTests
{
    [Theory]
    [InlineData(ColliderShapeKind.Tube)]
    [InlineData(ColliderShapeKind.Frustum)]
    public void NearAxisFramesKeepTheirTransverseSurfaceExtent(ColliderShapeKind kind)
    {
        // Y.Y rounds to one, while X.Y remains nonzero. A complement sqrt(1-Y.Y²)
        // loses 1.6 mm of radial extent for this admitted 16 m radius.
        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .0001f);
        var x = Vector3.Transform(Vector3.UnitX, turn);
        var y = Vector3.Transform(Vector3.UnitY, turn);
        var z = Vector3.Transform(Vector3.UnitZ, turn);
        var solid = new HollowSolid(kind, new(0, 3, 0), x, y, z, 16f, .5f, .5f, kind == ColliderShapeKind.Frustum ? 15f : 0f);
        var radial = kind == ColliderShapeKind.Frustum ? 15f : 16f;
        Assert.True(x.Y > 0f);
        Assert.Equal(1f, y.Y);
        AssertEnclosed(solid, solid.Position + radial * x + .5f * y);
        AssertEnclosed(solid, solid.Position - 16f * x - .5f * y);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(32f)]
    [InlineData(128f)]
    public void ThinPairsRetainOverlapAndSpeculationInBothOrders(float shift)
    {
        // 128 m deliberately exceeds the eligibility scale: uncertain bounds retain
        // the existing query. The 1 mm overlap and 5 mm gap are independent fixtures.
        var centre = new Vector3(shift, 3f, -shift);
        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, .0001f);
        var x = Vector3.Transform(Vector3.UnitX, turn);
        var y = Vector3.Transform(Vector3.UnitY, turn);
        var z = Vector3.Transform(Vector3.UnitZ, turn);
        foreach (var kind in new[] { ColliderShapeKind.Tube, ColliderShapeKind.Frustum, ColliderShapeKind.Torus })
        {
            var radius = kind == ColliderShapeKind.Torus ? .75f : 1f;
            var inner = kind == ColliderShapeKind.Torus ? 0f : .2f;
            const float half = .05f;
            var end = kind == ColliderShapeKind.Torus ? ColliderDeclaration.FullSweep
                : kind == ColliderShapeKind.Frustum ? .8f : 0f;
            foreach (var gap in new[] { -.001f, .005f, .03f })
            {
                var a = new HollowSolid(kind, centre, x, y, z, radius, inner, half, end);
                var b = new HollowSolid(kind, centre + y * (2f * half + gap), -x, -y, z, radius, inner, half, end);
                CheckPair(a, b, gap);
                CheckPair(b, a, gap);
            }
        }
    }

    [Theory]
    [InlineData(.5f)]
    [InlineData(MathF.PI)]
    public void PartialTorusBoundsContainEndpointsAndInteriorExtrema(float sweep)
    {
        var turn = Quaternion.CreateFromYawPitchRoll(.37f, .19f, -.42f);
        var solid = new HollowSolid(ColliderShapeKind.Torus, new(32, 3, -32),
            Vector3.Transform(Vector3.UnitX, turn), Vector3.Transform(Vector3.UnitY, turn),
            Vector3.Transform(Vector3.UnitZ, turn), 2f, .1f, .2f, sweep);
        // Endpoints plus a full deterministic arc traversal include interior extrema.
        for (var arc = 0; arc <= 64; arc++)
            for (var profile = 0; profile < 16; profile++)
            {
                var angle = sweep * arc / 64f;
                var phase = MathF.Tau * profile / 16f;
                var radial = MathF.Cos(angle) * solid.X + MathF.Sin(angle) * solid.Z;
                AssertEnclosed(solid, solid.Position + (solid.Radius + solid.LaneY * MathF.Cos(phase)) * radial
                    + solid.LaneY * MathF.Sin(phase) * solid.Y);
            }
    }

    private static void CheckPair(HollowSolid a, HollowSolid b, float gap)
    {
        var partner = new HollowPartner(a.Kind, a.Position, a.X, a.Y, a.Z, a.Radius, a.LaneX, a.LaneY, a.LaneZ);
        Span<HollowContact> contacts = stackalloc HollowContact[HollowContactQuery.Capacity];
        var margin = gap < 0f ? 0f : .01f;
        var count = HollowContactQuery.Measure(partner, b, margin, contacts);
        if (gap > margin)
        {
            Assert.Equal(0, count);
            Assert.False(HollowContactQuery.Overlaps(partner, b, 0f));
            return;
        }
        Assert.True(count > 0, $"{a.Kind}: gap {gap}, centre {a.Position} lost contact");
        Assert.Equal(gap < 0f, HollowContactQuery.Overlaps(partner, b, 0f));
        var distance = float.PositiveInfinity;
        foreach (var contact in contacts[..count])
            if (contact.Support == HollowContactSupport.DistanceOnly) distance = MathF.Min(distance, contact.Gap);
        Assert.InRange(distance, gap - CuriousContraptions.Abstractions.PhysicalTolerance.ContactSeparationMetres, gap + CuriousContraptions.Abstractions.PhysicalTolerance.ContactSeparationMetres);
    }

    private static void AssertEnclosed(HollowSolid solid, Vector3 point)
    {
        solid.Bounds(out var min, out var max);
        // The existing contact geometry allowance covers witness reconstruction;
        // this is enclosure, not an assertion of incidental motion or a new epsilon.
        var allowance = new Vector3(CuriousContraptions.Abstractions.PhysicalTolerance.ContactSeparationMetres);
        Assert.True(Vector3.Min(point, min - allowance) == min - allowance);
        Assert.True(Vector3.Max(point, max + allowance) == max + allowance);
    }
}
