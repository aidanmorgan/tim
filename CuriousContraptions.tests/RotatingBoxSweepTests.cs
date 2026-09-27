using Godot;

namespace CuriousContraptions.Tests;

public class RotatingBoxSweepTests
{
    private static readonly Vector3 Half = new(2, .1f, .4f);
    private const float Radius = .2f;
    private static RotatingBoxHit Cast(Vector3 position, Vector3 velocity, double speed, double duration,
        Transform3D? pose = null, Vector3? pivot = null, Vector3? axis = null, Vector3? half = null) =>
        RotatingBoxSweep.Cast(position, Radius, velocity, pivot ?? Vector3.Zero, axis ?? Vector3.Back,
            pose ?? Transform3D.Identity, half ?? Half, speed, duration);

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(2000)]
    [InlineData(-2000)]
    public void RotatingBeamHitsStationarySphereAtAnalyticTime(double speed)
    {
        var position = new Vector3(Math.Sign(speed), 1, 0);
        var expected = (Math.Acos(.3 / Math.Sqrt(2)) - Math.PI / 4) / Math.Abs(speed);
        var hit = Cast(position, Vector3.Zero, speed, expected * 1.2);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.InRange(hit.Time, expected - .00015 / Math.Abs(speed), expected + .000002 / Math.Abs(speed));
        Assert.InRange(hit.Penetration, 0, .00001f);
        Assert.InRange(Math.Abs(hit.Normal.Length() - 1), 0, .000001f);
        Assert.InRange(position.DistanceTo(hit.Point), Radius - .00001f, Radius + .00011f);
        Assert.InRange(hit.Iterations, 1, 200);
    }

    [Fact]
    public void ClearEndpointPosesDoNotHideAnIntermediateRotationalHit()
    {
        var position = new Vector3(1, 1, 0);
        Assert.True(Gap(position, Transform3D.Identity, Half) > 0);
        Assert.True(Gap(position, new(new Basis(Vector3.Back, Mathf.Pi), Vector3.Zero), Half) > 0);
        var hit = Cast(position, Vector3.Zero, 1, Math.PI);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.InRange(hit.Time, .57, .58);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1000f)]
    public void StationaryBoxMatchesLinearSphereSweep(float speed)
    {
        var origin = new Vector3(.8f, 3, .2f);
        var velocity = Vector3.Down * speed;
        var duration = 4 / speed;
        var linear = SphereSweep.Cast(origin, Radius, velocity * duration, p => SphereSweep.BoxSurface(p, Half));
        var rotating = Cast(origin, velocity, 0, duration);
        Assert.Equal(linear.Status, rotating.Status);
        Assert.InRange(Math.Abs(linear.Distance / speed - rotating.Time), 0, .00001);
        Assert.InRange((linear.Normal - rotating.Normal).Length(), 0, .000001f);
    }

    [Fact]
    public void SeparatingTouchDoesNotGenerateAZeroTimeImpact()
    {
        var hit = Cast(new(1, .3f, 0), Vector3.Zero, -1, .1);
        Assert.Equal(SphereSweepStatus.Clear, hit.Status);
        Assert.Equal(.1, hit.Time);
    }

    [Fact]
    public void AxialTangentRemainsClearWithoutTinyTimeStepping()
    {
        var hit = Cast(new(0, 0, .6f), Vector3.Zero, 20, 10);
        Assert.Equal(SphereSweepStatus.Clear, hit.Status);
        Assert.InRange(hit.Iterations, 1, 2);
    }

    [Fact]
    public void InitiallySeparatingRotationCanHitLater()
    {
        var hit = Cast(new(1, .3f, 0), Vector3.Zero, -1, Math.PI);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.True(hit.Time > 1);
    }

    [Fact]
    public void InitialOverlapAndApproachingTouchHaveDistinctStatuses()
    {
        var overlap = Cast(new(1, .2f, 0), Vector3.Zero, 0, 0);
        Assert.Equal(SphereSweepStatus.Overlapping, overlap.Status);
        Assert.InRange(overlap.Penetration, .09999f, .10001f);
        Assert.Equal(0, overlap.Time);
        var touching = Cast(new(1, .3f, 0), Vector3.Down, 0, .1);
        Assert.Equal(SphereSweepStatus.Contact, touching.Status);
        Assert.Equal(0, touching.Time);
    }

    [Fact]
    public void DepthMissAndOuterRadiusMissStayClearThroughFullTurn()
    {
        Assert.Equal(SphereSweepStatus.Clear, Cast(new(1, 1, 1), Vector3.Zero, 1, Math.Tau).Status);
        Assert.Equal(SphereSweepStatus.Clear, Cast(new(3, 3, 0), Vector3.Zero, 1, Math.Tau).Status);
    }

    [Fact]
    public void OffsetPivotAndArbitraryWorldRotationPreserveHit()
    {
        var position = new Vector3(1, 1, 0);
        var initial = Cast(position, Vector3.Zero, 1, 1);
        var rotation = new Basis(new Quaternion(new Vector3(1, 2, 3).Normalized(), .83f));
        var pivot = new Vector3(3, 4, -2);
        var moved = Cast(pivot + rotation * position, Vector3.Zero, 1, 1,
            new(rotation, pivot), pivot, rotation * Vector3.Back);
        Assert.Equal(initial.Status, moved.Status);
        Assert.InRange(Math.Abs(initial.Time - moved.Time), 0, .000003);
        Assert.InRange((rotation * initial.Normal - moved.Normal).Length(), 0, .000003f);
        Assert.InRange((pivot + rotation * initial.Point - moved.Point).Length(), 0, .000003f);
    }

    [Fact]
    public void OffCentreBoxActuallyOrbitsItsPivot()
    {
        var pose = new Transform3D(Basis.Identity, new Vector3(2, 0, 0));
        var half = new Vector3(.1f, .1f, .1f);
        var hit = Cast(new(0, 2, 0), Vector3.Zero, 1, Math.PI, pose, half: half);
        Assert.Equal(SphereSweepStatus.Contact, hit.Status);
        Assert.InRange(hit.Time, 1.4, 1.5);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(29)]
    [InlineData(47)]
    public void DenseIndependentPoseSamplesFindNoMissedOrLateContacts(int seed)
    {
        var random = new Random(seed);
        float Between(float low, float high) => low + (high - low) * random.NextSingle();
        var contacts = 0;
        for (var trial = 0; trial < 80; trial++)
        {
            var origin = new Vector3(Between(-3, 3), Between(-3, 3), Between(-1, 1));
            var velocity = new Vector3(Between(-3, 3), Between(-3, 3), Between(-1, 1));
            var speed = Between(-8, 8);
            const double duration = .5;
            var hit = Cast(origin, velocity, speed, duration);
            double? firstDeepOverlap = null;
            for (var sample = 0; sample <= 2048; sample++)
            {
                var time = duration * sample / 2048;
                var pose = new Transform3D(new Basis(Vector3.Back, (float)(speed * time)), Vector3.Zero);
                if (Gap(origin + velocity * (float)time, pose, Half) < -.001f)
                {
                    firstDeepOverlap = time;
                    break;
                }
            }
            if (firstDeepOverlap is { } reference)
            {
                contacts++;
                Assert.NotEqual(SphereSweepStatus.Clear, hit.Status);
                Assert.True(hit.Time <= reference, $"seed={seed}, trial={trial}, hit={hit.Time}, sampled={reference}");
            }
            if (hit.Status == SphereSweepStatus.Contact)
            {
                var pose = new Transform3D(new Basis(Vector3.Back, (float)(speed * hit.Time)), Vector3.Zero);
                var gap = Gap(origin + velocity * (float)hit.Time, pose, Half);
                Assert.InRange(gap, -.00011f, .00011f);
                Assert.InRange(hit.Iterations, 1, 1000);
            }
        }
        Assert.True(contacts >= 10);
    }

    [Fact]
    public void InvalidGeometryAndNonfiniteMotionAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(new(1, 1, 0), Vector3.Zero, 1e-200, 1e200));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, Vector3.Zero, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, Vector3.Zero, 1, -1));
        Assert.Throws<ArgumentException>(() => Cast(Vector3.Zero, Vector3.Zero, 1, 1, axis: Vector3.Back * 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cast(Vector3.Zero, Vector3.Zero, 1, 1, half: new(1, 0, 1)));
        Assert.Throws<ArgumentException>(() => Cast(Vector3.Zero, Vector3.Zero, 1, 1,
            pose: new(Basis.Identity.Scaled(new(2, 1, 1)), Vector3.Zero)));
    }

    private static float Gap(Vector3 sphere, Transform3D pose, Vector3 half)
    {
        // Independent closest-point expression for the dense outside samples.
        var local = pose.AffineInverse() * sphere;
        var q = local.Abs() - half;
        var outside = q.Max(Vector3.Zero).Length();
        var inside = Mathf.Min(0, Mathf.Max(q.X, Mathf.Max(q.Y, q.Z)));
        return outside + inside - Radius;
    }
}
