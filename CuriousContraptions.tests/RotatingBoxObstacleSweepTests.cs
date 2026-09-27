using Godot;

namespace CuriousContraptions.Tests;

public class RotatingBoxObstacleSweepTests
{
    private static readonly Vector3 Half = new(1.8f, .12f, .55f);
    private static readonly Vector3 WallHalf = new(.3f, .2f, .7f);
    private static RotatingObstacleHit Cast(Vector3 center, double speed = 1, double duration = 1,
        Vector3? obstacleHalf = null, Transform3D? pose = null, Vector3? pivot = null,
        Vector3? axis = null, Basis? obstacleBasis = null) =>
        RotatingBoxObstacleSweep.Cast(pivot ?? Vector3.Zero, axis ?? Vector3.Back,
            pose ?? Transform3D.Identity, Half, speed,
            new(obstacleBasis ?? Basis.Identity, center), obstacleHalf ?? WallHalf, duration);

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(20000)]
    [InlineData(-20000)]
    public void CornerHitsWallAtAnalyticTime(double speed)
    {
        var angle = Math.Asin(.4 / Math.Sqrt(1.8 * 1.8 + .12 * .12)) - Math.Atan(.12 / 1.8);
        var hit = Cast(new(1.5f * Math.Sign(speed), .6f, 0), speed, 1 / Math.Abs(speed));
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.InRange(hit.Time, (angle - .0001) / Math.Abs(speed), (angle + .00001) / Math.Abs(speed));
        Assert.InRange(hit.Gap, -.00011, .00011);
        Assert.InRange(hit.Iterations, 1, 500);
    }

    [Fact]
    public void ThinObstacleBetweenClearEndpointsIsNotSkipped()
    {
        var half = new Vector3(.001f, .001f, .3f);
        var hit = Cast(new(1, 1, 0), 10000, Math.PI / 10000, half);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.InRange(hit.Time * 10000, .69, .71);
    }

    [Fact]
    public void TouchingOneEndCanRotateAwayThenCollideLater()
    {
        var wall = new Vector3(1.5f, .32f, 0);
        var clear = Cast(wall, -1, .1);
        Assert.Equal(SphereSweepStatus.Clear, clear.Status);
        var later = Cast(wall, -1, Math.PI);
        Assert.Equal(SphereSweepStatus.Contact, later.Status);
        Assert.True(later.Time > 1);
        Assert.Equal(SphereSweepStatus.Contact, Cast(wall, 1, .1).Status);
    }

    [Fact]
    public void AxialTangentAndDepthMissRemainClear()
    {
        foreach (var depth in new[] { 1.25f, 2f })
        {
            var hit = Cast(new(1, 1, depth), 20, 10);
            Assert.Equal(SphereSweepStatus.Clear, hit.Status);
            Assert.InRange(hit.Iterations, 1, 2);
        }
    }

    [Fact]
    public void StationaryTouchIsClearButDeepOverlapIsDistinct()
    {
        Assert.Equal(SphereSweepStatus.Clear, Cast(new(1.5f, .32f, 0), 0).Status);
        var overlap = Cast(new(1.5f, .2f, 0), 0);
        Assert.Equal(SphereSweepStatus.Overlapping, overlap.Status);
        Assert.Equal(0, overlap.Time);
        Assert.InRange(overlap.Gap, -.120001, -.119999);
    }

    [Fact]
    public void ArbitraryWorldOrientationPreservesImpact()
    {
        var original = Cast(new(1.5f, .6f, 0));
        var basis = new Basis(new Quaternion(new Vector3(1, 2, 3).Normalized(), .83f));
        var pivot = new Vector3(3, 4, -2);
        var rotated = Cast(pivot + basis * new Vector3(1.5f, .6f, 0),
            pose: new(basis, pivot), pivot: pivot, axis: basis.Z, obstacleBasis: basis);
        Assert.Equal(original.Status, rotated.Status);
        Assert.InRange(Math.Abs(original.Time - rotated.Time), 0, .00001);
        Assert.InRange((basis * original.Normal - rotated.Normal).Length(), 0, .00001f);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(37)]
    [InlineData(73)]
    public void IndependentCornerProjectionsFindNoMissedContacts(int seed)
    {
        var random = new Random(seed);
        float Between(float a, float b) => a + random.NextSingle() * (b - a);
        var contacts = 0;
        for (var trial = 0; trial < 60; trial++)
        {
            var center = new Vector3(Between(-2.5f, 2.5f), Between(-2, 2), Between(-1, 1));
            var basis = Basis.FromEuler(new(Between(-1, 1), Between(-1, 1), Between(-1, 1)));
            var obstacle = new Transform3D(basis, center);
            var speed = Between(-10, 10);
            const double duration = .7;
            var hit = Cast(center, speed, duration, obstacleBasis: basis);
            double? firstOverlap = null;
            for (var sample = 0; sample <= 1024; sample++)
            {
                var time = duration * sample / 1024;
                var moving = new Transform3D(new Basis(Vector3.Back, (float)(speed * time)), Vector3.Zero);
                if (CornerGap(moving, obstacle) < -.001)
                {
                    firstOverlap = time;
                    break;
                }
            }
            if (firstOverlap is { } reference)
            {
                contacts++;
                Assert.NotEqual(SphereSweepStatus.Clear, hit.Status);
                Assert.True(hit.Time <= reference, $"seed={seed}, trial={trial}, hit={hit.Time}, sampled={reference}");
            }
            if (hit.Status == SphereSweepStatus.Contact)
            {
                var moving = new Transform3D(new Basis(Vector3.Back, (float)(speed * hit.Time)), Vector3.Zero);
                Assert.InRange(CornerGap(moving, obstacle), -.00015, .00015);
                Assert.InRange(hit.Iterations, 1, 3000);
            }
        }
        Assert.True(contacts >= 10);
    }

    private static double CornerGap(Transform3D a, Transform3D b)
    {
        static Vector3[] Corners(Transform3D pose, Vector3 half) =>
            (from x in new[] { -1, 1 } from y in new[] { -1, 1 } from z in new[] { -1, 1 }
             select pose * new Vector3(x * half.X, y * half.Y, z * half.Z)).ToArray();
        var ac = Corners(a, Half); var bc = Corners(b, WallHalf);
        var axes = new List<Vector3> { a.Basis.X, a.Basis.Y, a.Basis.Z, b.Basis.X, b.Basis.Y, b.Basis.Z };
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++) axes.Add(a.Basis[i].Cross(b.Basis[j]));
        double largest = double.NegativeInfinity;
        foreach (var raw in axes)
        {
            if (raw.LengthSquared() < 1e-16f) continue;
            var axis = raw.Normalized();
            var ap = ac.Select(p => (double)p.Dot(axis)).ToArray();
            var bp = bc.Select(p => (double)p.Dot(axis)).ToArray();
            largest = Math.Max(largest, Math.Max(ap.Min() - bp.Max(), bp.Min() - ap.Max()));
        }
        return largest;
    }

    [Fact]
    public void InvalidInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, 1e-200, 1e200));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, duration: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, obstacleHalf: new(1, 0, 1)));
        Assert.Throws<ArgumentException>(() => Cast(Vector3.Zero, axis: Vector3.Back * 2));
        Assert.Throws<ArgumentException>(() => Cast(Vector3.Zero, obstacleBasis: Basis.Identity.Scaled(new(2, 1, 1))));
    }
}
