namespace CuriousContraptions.Tests;

public class LatchedSpringStoreTests
{
    private static LatchedSpringStore Spring() => new(100, 1, .125f);
    private static void Balanced(LatchedSpringStore spring) =>
        Assert.InRange(Math.Abs(spring.AcceptedWork - spring.ReleasedWork - spring.Energy),
            0, Math.Max(1e-12, spring.AcceptedWork * 1e-12));

    [Fact]
    public void WindingUsesTravelAndRisingSpringForceRatherThanFlatPower()
    {
        var spring = Spring();
        var first = spring.Wind(2, 100, 1);
        Assert.Equal(SpringWindStatus.Wound, first.Status);
        Assert.Equal(.25, first.CompressionTravel);
        Assert.Equal(2, first.ShaftTravel);
        Assert.Equal(3.125, first.Work);
        Assert.Equal(25, spring.Force);
        Assert.Equal(3.125, spring.RequiredTorque);
        var second = spring.Wind(2, 100, 1);
        Assert.Equal(9.375, second.Work);
        Assert.Equal(12.5, spring.Energy);
        Assert.Equal(50, spring.Capacity);
        Balanced(spring);
    }

    [Fact]
    public void CapacityAndClearanceRejectUnperformedTravel()
    {
        var spring = Spring();
        var stopped = spring.Wind(20, 100, 0);
        Assert.Equal(SpringWindStatus.Blocked, stopped.Status);
        Assert.Equal(0, spring.Energy);
        var partial = spring.Wind(20, 100, .25f);
        Assert.Equal(.25, partial.CompressionTravel);
        Assert.Equal(2, partial.ShaftTravel);
        var full = spring.Wind(20, 100, 1);
        Assert.Equal(.75, full.CompressionTravel);
        Assert.Equal(6, full.ShaftTravel);
        Assert.Equal(50, spring.AcceptedWork);
        Assert.Equal(SpringWindStatus.Full, spring.Wind(20, 100, 1).Status);
        Assert.Equal(50, spring.Energy);
        Balanced(spring);
    }

    [Fact]
    public void InsufficientTorqueLimitsCompressionAndNeverInventsShaftWork()
    {
        var spring = Spring();
        Assert.Equal(SpringWindStatus.TorqueLimited, spring.Wind(10, 0, 1).Status);
        var winding = spring.Wind(10, 6.25f, 1);
        Assert.Equal(.5, spring.Compression);
        Assert.Equal(6.25, spring.RequiredTorque);
        Assert.Equal(12.5, winding.Work);
        Assert.True(winding.Work <= 6.25 * winding.ShaftTravel);
        Assert.Equal(SpringWindStatus.TorqueLimited, spring.Wind(10, 6.25f, 1).Status);
        Assert.Equal(SpringWindStatus.TorqueLimited, spring.Wind(10, 1, 1).Status);
        Assert.Equal(.5, spring.Compression);
        Balanced(spring);
    }

    [Fact]
    public void PowerLossAndReverseRotationDoNotLoseOrAddLatchedCharge()
    {
        var spring = Spring();
        spring.Wind(4, 100, 1);
        Assert.Equal(SpringWindStatus.Stationary, spring.Wind(0, 0, 1).Status);
        Assert.Equal(SpringWindStatus.Reverse, spring.Wind(-100, 100, 1).Status);
        Assert.Equal(12.5, spring.Energy);
        Assert.Equal(SpringLatchState.Latched, spring.State);
        Assert.Equal(SpringTriggerResult.Released, spring.Release());
        Assert.Equal(12.5, spring.Energy);
        Assert.Equal(SpringLatchState.Releasing, spring.State);
    }

    [Fact]
    public void ReleaseNeedsPhysicalTravelAndCannotWindOrRelatchMidStroke()
    {
        var spring = Spring();
        spring.Wind(8, 100, 1);
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(.5f));
        Assert.Equal(SpringTriggerResult.Released, spring.Release());
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(0));
        Assert.Equal(50, spring.Energy);
        Assert.Equal(SpringWindStatus.Unlatched, spring.Wind(8, 100, 1).Status);
        Assert.Equal(SpringTriggerResult.AlreadyReleased, spring.Release());
        Assert.Equal(SpringLatchResult.StillCompressed, spring.Latch());
        var partial = spring.Extend(.25f);
        Assert.Equal(.25, partial.Travel);
        Assert.Equal(21.875, partial.Work);
        Assert.Equal(28.125, spring.Energy);
        var end = spring.Extend(100);
        Assert.Equal(.75, end.Travel);
        Assert.Equal(28.125, end.Work);
        Assert.Equal(SpringLatchState.Spent, spring.State);
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(100));
        Assert.Equal(SpringWindStatus.Unlatched, spring.Wind(8, 100, 1).Status);
        Assert.Equal(SpringLatchResult.Latched, spring.Latch());
        Assert.Equal(SpringLatchResult.AlreadyLatched, spring.Latch());
        Assert.Equal(SpringTriggerResult.Empty, spring.Release());
        Assert.Equal(0, spring.Energy);
        Balanced(spring);
    }

    [Fact]
    public void EmptyTriggerDoesNotQueueAFutureRelease()
    {
        var spring = Spring();
        Assert.Equal(SpringTriggerResult.Empty, spring.Release());
        spring.Wind(1, 100, 1);
        Assert.Equal(SpringLatchState.Latched, spring.State);
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(1));
        Assert.Equal(SpringTriggerResult.Released, spring.Release());
        Assert.True(spring.Extend(1).Work > 0);
    }

    [Fact]
    public void StepPartitioningPreservesAcceptedAndReleasedWork()
    {
        var whole = Spring();
        var split = Spring();
        whole.Wind(8, 100, 1);
        for (var i = 0; i < 64; i++) split.Wind(.125f, 100, 1);
        Assert.Equal(whole.Compression, split.Compression);
        Assert.Equal(whole.AcceptedWork, split.AcceptedWork);
        whole.Release(); split.Release();
        whole.Extend(1);
        for (var i = 0; i < 64; i++) split.Extend(.015625f);
        Assert.Equal(whole.ReleasedWork, split.ReleasedWork);
        Assert.Equal(whole.State, split.State);
        Balanced(split);
    }

    [Fact]
    public void RepeatedPartialShotsAndResetHaveNoFreeRecharge()
    {
        var spring = Spring();
        for (var i = 0; i < 1000; i++)
        {
            spring.Wind(.3f, 100, 1);
            spring.Release();
            spring.Extend(.01f);
            spring.Extend(1);
            spring.Latch();
            Balanced(spring);
            Assert.Equal(0, spring.Energy);
        }
        Assert.True(spring.ReleasedWork > 0);
        spring.Reset();
        Assert.Equal(0, spring.AcceptedWork);
        Assert.Equal(0, spring.ReleasedWork);
        Assert.Equal(0, spring.Compression);
        Assert.Equal(SpringLatchState.Latched, spring.State);
    }

    [Fact]
    public void SubPrecisionTravelCannotAccumulateUnperformedWork()
    {
        var spring = Spring();
        spring.Wind(4, 100, 1);
        var energy = spring.Energy;
        Assert.Equal(SpringWindStatus.PrecisionLimited,
            spring.Wind(float.Epsilon, 100, 1).Status);
        Assert.Equal(energy, spring.Energy);
        Assert.Equal(energy, spring.AcceptedWork);
        spring.Release();
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(float.Epsilon));
        Assert.Equal(energy, spring.Energy);
        Assert.Equal(0, spring.ReleasedWork);
    }

    [Fact]
    public void ResetAlsoClearsAnInterruptedChargedRelease()
    {
        var spring = Spring();
        spring.Wind(8, 100, 1);
        spring.Release();
        spring.Extend(.25f);
        spring.Reset();
        Assert.Equal(SpringLatchState.Latched, spring.State);
        Assert.Equal(0, spring.Compression);
        Assert.Equal(0, spring.AcceptedWork);
        Assert.Equal(0, spring.ReleasedWork);
        Assert.Equal(new SpringExtension(0, 0), spring.Extend(1));
        Assert.Equal(SpringTriggerResult.Empty, spring.Release());
    }

    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(1f, -1f, 1f)]
    [InlineData(1f, 1f, 0f)]
    [InlineData(float.NaN, 1f, 1f)]
    [InlineData(1f, float.PositiveInfinity, 1f)]
    [InlineData(1f, 1f, float.NegativeInfinity)]
    public void InvalidConfigurationIsRejected(float stiffness, float compression, float lead) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatchedSpringStore(stiffness, compression, lead));

    [Theory]
    [InlineData(float.NaN, 1f, 1f)]
    [InlineData(float.PositiveInfinity, 1f, 1f)]
    [InlineData(1f, -1f, 1f)]
    [InlineData(1f, float.NaN, 1f)]
    [InlineData(1f, 1f, -1f)]
    [InlineData(1f, 1f, float.PositiveInfinity)]
    public void InvalidWindingIsAtomic(float travel, float torque, float clearance)
    {
        var spring = Spring();
        spring.Wind(1, 100, 1);
        var energy = spring.Energy;
        Assert.Throws<ArgumentOutOfRangeException>(() => spring.Wind(travel, torque, clearance));
        Assert.Equal(energy, spring.Energy);
        Assert.Equal(energy, spring.AcceptedWork);
        Assert.Equal(SpringLatchState.Latched, spring.State);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidExtensionIsAtomic(float travel)
    {
        var spring = Spring();
        spring.Wind(1, 100, 1);
        spring.Release();
        var energy = spring.Energy;
        Assert.Throws<ArgumentOutOfRangeException>(() => spring.Extend(travel));
        Assert.Equal(energy, spring.Energy);
        Assert.Equal(0, spring.ReleasedWork);
        Assert.Equal(SpringLatchState.Releasing, spring.State);
    }

    [Fact]
    public void FiniteFloatScalesDoNotOverflowInternalWorkAccounting()
    {
        foreach (var stiffness in new[] { float.Epsilon, 100f, float.MaxValue })
        foreach (var compression in new[] { float.Epsilon, 1f, float.MaxValue })
        foreach (var lead in new[] { float.Epsilon, .125f, float.MaxValue })
        {
            var spring = new LatchedSpringStore(stiffness, compression, lead);
            var wind = spring.Wind(float.MaxValue, float.MaxValue, float.MaxValue);
            Assert.True(double.IsFinite(spring.Capacity));
            Assert.True(double.IsFinite(spring.Energy));
            Assert.True(double.IsFinite(wind.Work));
            Assert.InRange(spring.Compression, 0, spring.MaximumCompression);
            Assert.InRange(wind.Work, 0, spring.Capacity);
            Assert.True(wind.Work <= (double)float.MaxValue * wind.ShaftTravel * (1 + 1e-12));
            spring.Release();
            spring.Extend(float.MaxValue);
            Assert.Equal(0, spring.Energy);
            Balanced(spring);
        }
    }
}
