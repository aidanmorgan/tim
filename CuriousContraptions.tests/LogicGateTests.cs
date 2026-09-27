namespace CuriousContraptions.Tests;

public class LogicGateTests
{
    public static IEnumerable<object[]> TruthRows()
    {
        // Explicit specification, not a second implementation of the evaluator.
        var rows = new[]
        {
            (LogicGateKind.And, new[] { false, false, false, true }),
            (LogicGateKind.Or, new[] { false, true, true, true }),
            (LogicGateKind.Xor, new[] { false, true, true, false }),
            (LogicGateKind.Nor, new[] { true, false, false, false }),
            (LogicGateKind.Nand, new[] { true, true, true, false })
        };
        foreach (var (kind, results) in rows)
            for (var row = 0; row < 4; row++)
                yield return [kind, (row & 2) != 0, (row & 1) != 0, results[row]];
    }

    [Theory]
    [MemberData(nameof(TruthRows))]
    public void TruthAndOpticalSnapshotAgree(LogicGateKind kind, bool first, bool second, bool expected)
    {
        Assert.Equal(expected, LogicGate.Evaluate(kind, first, second));
        var control = new OpticalLogicControl(kind);
        var initial = control.IsOpen;
        control.Sample(first ? 1 : 0, second ? 1 : 0);
        Assert.Equal(initial, control.IsOpen); // no change during the current trace
        control.Advance();
        Assert.Equal(expected, control.IsOpen);
        control.Reset();
        Assert.False(control.First);
        Assert.False(control.Second);
        Assert.Equal(LogicGate.Evaluate(kind, false, false), control.IsOpen);
    }

    [Theory]
    [InlineData(LogicGateKind.Xor)]
    [InlineData(LogicGateKind.Nand)]
    public void SecondInputRetractsOnlyAtNextSnapshot(LogicGateKind kind)
    {
        var control = new OpticalLogicControl(kind);
        control.Sample(1, 0);
        control.Advance();
        Assert.True(control.IsOpen);
        control.Sample(1, 1);
        Assert.True(control.IsOpen);
        control.Advance();
        Assert.False(control.IsOpen);
        control.Sample(1, 0);
        control.Advance();
        Assert.True(control.IsOpen);
    }

    [Fact]
    public void IndependentHysteresisIncludesExactThresholdBoundaries()
    {
        var control = new OpticalLogicControl(LogicGateKind.And);
        control.Sample(.25f, .24f);
        Assert.True(control.First);
        Assert.False(control.Second);
        control.Sample(.23f, .25f);
        control.Advance();
        Assert.True(control.IsOpen);
        control.Sample(.225f, .23f);
        control.Advance();
        Assert.False(control.First);
        Assert.True(control.Second);
        Assert.False(control.IsOpen);
        control.Sample(100, 0);
        control.Advance();
        Assert.False(control.IsOpen); // bright A cannot impersonate B
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(-1f)]
    public void InvalidSampleCannotPartiallyCommit(float invalid)
    {
        var control = new OpticalLogicControl(LogicGateKind.Or);
        Assert.Throws<ArgumentException>(() => control.Sample(1, invalid));
        Assert.False(control.First);
        Assert.False(control.Second);
        Assert.Throws<ArgumentException>(() => control.Sample(invalid, 1));
        Assert.False(control.First);
        Assert.False(control.Second);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 1f)]
    [InlineData(.2f, .3f)]
    [InlineData(1f, -1f)]
    [InlineData(float.NaN, .1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void InvalidThresholdsAreRejected(float on, float off) =>
        Assert.Throws<ArgumentException>(() => new OpticalLogicControl(LogicGateKind.And, on, off));

    [Fact]
    public void UnsupportedOperationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LogicGate.Evaluate((LogicGateKind)99, false, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpticalLogicControl((LogicGateKind)99));
    }
}
