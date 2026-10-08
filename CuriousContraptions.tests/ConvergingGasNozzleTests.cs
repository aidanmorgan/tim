using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvergingGasNozzleTests
{
    private static SealedGasState Source() => new(new(200, 400, 100, 1000, 100, 1e7), .02, .01, 2400);

    [Theory]
    [InlineData(0, GasNozzleRegime.Choked)]
    [InlineData(30000, GasNozzleRegime.Choked)]
    [InlineData(96000, GasNozzleRegime.Subsonic)]
    public void FluxesConserveContinuityEnthalpyAndMomentum(double backPressure, GasNozzleRegime regime)
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.01);
        var flow = nozzle.Evaluate(source, backPressure);
        Assert.Equal(regime, flow.Regime);
        var density = flow.ExitPressure / (200 * flow.ExitTemperature);
        Assert.Equal(.01 * density * flow.ExitSpeed, flow.MassRate);
        Assert.InRange(Math.Abs(600 * flow.ExitTemperature + .5 * flow.ExitSpeed * flow.ExitSpeed - 180000), 0, 1e-9);
        Assert.Equal(flow.MassRate * 180000, flow.EnthalpyPower);
        Assert.Equal(flow.MassRate * flow.ExitSpeed, flow.MomentumRate);
        Assert.Equal(.01 * (flow.ExitPressure - backPressure), flow.PressureForce);
        Assert.Equal(flow.MomentumRate + flow.PressureForce, flow.Thrust);
        Assert.Equal(2400, source.InternalEnergy);
        Assert.Equal(.02, source.Mass);
        Assert.Equal(flow, nozzle.Evaluate(source, backPressure));
    }

    [Fact]
    public void ChokedRateIsSonicAndIndependentOfLowerBackPressure()
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.01);
        var first = nozzle.Evaluate(source, 30000);
        var vacuum = nozzle.Evaluate(source, 0);
        Assert.InRange(Math.Abs(first.ExitPressure - 61440), 0, 1e-9);
        Assert.InRange(Math.Abs(first.ExitTemperature - 240), 0, 1e-10);
        Assert.InRange(Math.Abs(first.ExitSpeed - Math.Sqrt(72000)), 0, 1e-10);
        Assert.Equal(first.MassRate, vacuum.MassRate);
        Assert.Equal(first.ExitSpeed, vacuum.ExitSpeed);
        Assert.True(vacuum.Thrust > first.Thrust);
        var doubled = new ConvergingGasNozzle(.02).Evaluate(source, 30000);
        Assert.Equal(2 * first.MassRate, doubled.MassRate);
        Assert.Equal(2 * first.EnthalpyPower, doubled.EnthalpyPower);
    }

    [Fact]
    public void ChokingBoundaryIsContinuousAndSubsonicExitMatchesBackPressure()
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.01);
        var threshold = source.Pressure * Math.Pow(.8, 3);
        var low = nozzle.Evaluate(source, threshold - .0001);
        var high = nozzle.Evaluate(source, threshold + .0001);
        Assert.Equal(GasNozzleRegime.Choked, low.Regime);
        Assert.Equal(GasNozzleRegime.Subsonic, high.Regime);
        Assert.InRange(Math.Abs(low.MassRate - high.MassRate), 0, 1e-10);
        Assert.Equal(threshold + .0001, high.ExitPressure);
        Assert.Equal(0, high.PressureForce);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(.01, 120000)]
    public void ClosedAreaAndBalancedPressureHaveNoFlux(double area, double backPressure)
    {
        var flow = new ConvergingGasNozzle(area).Evaluate(Source(), backPressure);
        Assert.Equal(GasNozzleRegime.NoFlow, flow.Regime);
        Assert.Equal(0, flow.MassRate);
        Assert.Equal(0, flow.EnthalpyPower);
        Assert.Equal(0, flow.Thrust);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidInputsReject(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConvergingGasNozzle(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConvergingGasNozzle(.01).Evaluate(Source(), value));
    }

    [Fact]
    public void ReversePressureUnsupportedExitStateAndOverflowReject()
    {
        var nozzle = new ConvergingGasNozzle(.01);
        Assert.Throws<ArgumentNullException>(() => nozzle.Evaluate(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => nozzle.Evaluate(Source(), 120001));
        var cold = new SealedGasState(new(200, 400, 100, 1000, 100, 1e7), .02, .01, 800);
        Assert.Throws<ArgumentException>(() => nozzle.Evaluate(cold, 0));
        Assert.ThrowsAny<ArgumentException>(() => new ConvergingGasNozzle(double.MaxValue).Evaluate(Source(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConvergingGasNozzle(double.Epsilon)
            .Evaluate(Source(), Math.BitDecrement(Source().Pressure)));
    }
}
