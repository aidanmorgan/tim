using Godot;

namespace CuriousContraptions.Tests;

public class BodyContactConvergenceTests
{
    private static MovingSphereHit Touch(Vector3 normal) => new(SphereSweepStatus.Contact, 0, normal, 0);

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(1e-20f)]
    [InlineData(1f)]
    public void FirstAndUnchangedTinyImpactsAlwaysResolve(float speed)
    {
        var incoming = Vector3.Left * speed;
        var hit = Touch(Vector3.Right);
        var initial = default(BodyContactConvergence);
        Assert.False(initial.HasConverged(incoming, Vector3.Zero, hit, 1));
        var recorded = initial.Observe(incoming, Vector3.Zero, hit.Normal);
        Assert.False(recorded.HasConverged(incoming, Vector3.Zero, hit, 1));
        Assert.Equal((double)speed, recorded.PeakApproachSpeed);
    }

    [Theory]
    [InlineData(1e-20f)]
    [InlineData(1f)]
    [InlineData(100f)]
    public void RepeatedResidualConvergesRelativeToTheResolvedImpact(float scale)
    {
        var normal = Vector3.Right;
        var recorded = default(BodyContactConvergence).Observe(Vector3.Left * scale, Vector3.Zero, normal);
        var residual = Vector3.Left * (scale * 1e-8f);
        Assert.True(recorded.HasConverged(residual, Vector3.Zero, Touch(normal), .002f));
        Assert.False(recorded.HasConverged(Vector3.Left * (scale * .001f), Vector3.Zero, Touch(normal), .002f));
        var next = recorded.Observe(residual, Vector3.Zero, normal);
        Assert.Equal(recorded.PeakApproachSpeed, next.PeakApproachSpeed);
        Assert.False(default(BodyContactConvergence).HasConverged(residual, Vector3.Zero, Touch(normal), .002f));
    }

    [Fact]
    public void OverlapFutureContactAndChangedNormalMustResolve()
    {
        var recorded = default(BodyContactConvergence).Observe(Vector3.Left, Vector3.Zero, Vector3.Right);
        var residual = new Vector3(-1e-8f, -1e-8f, 0);
        Assert.False(recorded.HasConverged(residual, Vector3.Zero,
            new(SphereSweepStatus.Overlapping, 0, Vector3.Right, .01f), .002f));
        Assert.False(recorded.HasConverged(residual, Vector3.Zero,
            new(SphereSweepStatus.Contact, .001f, Vector3.Right, 0), .002f));
        Assert.False(recorded.HasConverged(residual, Vector3.Zero, Touch(Vector3.Up), .002f));
        var changed = recorded.Observe(residual, Vector3.Zero, Vector3.Up);
        Assert.False(changed.HasConverged(residual, Vector3.Zero, Touch(Vector3.Up), .002f));
        Assert.InRange(changed.PeakApproachSpeed, .999e-8, 1.001e-8);
    }

    [Fact]
    public void RelativeConvergenceCannotPermitGeometricallySignificantClosingTravel()
    {
        var recorded = default(BodyContactConvergence).Observe(Vector3.Left, Vector3.Zero, Vector3.Right);
        Assert.False(recorded.HasConverged(Vector3.Left * 1e-8f, Vector3.Zero, Touch(Vector3.Right), 1e8f));
    }
}
