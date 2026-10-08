using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class GasNozzleTransitTests
{
    private static SealedGasState Source() => new(new(400, 400, 10, 1000, 100, 1e7), .02, .01, 3200);
    private static GasTransitAccuracy Accuracy() => new(1e-10, 1e-8, 1e-7, 20000);

    [Theory]
    [InlineData(0)]
    [InlineData(30000)]
    public void ChokedTransitMatchesIndependentAnalyticalTimeMomentumPressureAndEnergy(double backPressure)
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.00001);
        var result = GasNozzleTransit.ToInventory(nozzle, source, backPressure, .01, Accuracy());
        const double x = .5;
        var temperature = 400 * 2.0 / 3;
        var pressure = 320000 * 4.0 / 9;
        var speed = Math.Sqrt(2 * 400 * temperature);
        var rate = nozzle.Area * pressure / (400 * temperature) * speed;
        var elapsed = 2 * source.Mass / rate * (1 / Math.Sqrt(x) - 1);
        var momentum = source.Mass * speed * 2 / 3 * (1 - Math.Pow(x, 1.5));
        var pressureImpulse = nozzle.Area * source.Mass / rate *
            (pressure * 2 / 3 * (1 - Math.Pow(x, 1.5)) - 2 * backPressure * (1 / Math.Sqrt(x) - 1));
        var kinetic = .25 * source.Mass * speed * speed * (1 - x * x);
        Assert.InRange(Math.Abs(result.Duration - elapsed), 0, 1e-9);
        Assert.InRange(Math.Abs(result.MomentumImpulse - momentum), 0, 1e-7);
        Assert.InRange(Math.Abs(result.PressureImpulse - pressureImpulse), 0, 1e-7);
        Assert.InRange(Math.Abs(result.KineticEnergy - kinetic), 0, 1e-6);
        Assert.Equal(result.Discharge.OutletEnthalpy, result.KineticEnergy + result.OutletThermalEnthalpy);
        Assert.True(result.Duration > result.Discharge.DischargedMass / rate);
        Assert.Equal(source, result.Discharge.Before);
        Assert.Equal(3200, source.InternalEnergy);
        Assert.Equal(result, GasNozzleTransit.ToInventory(nozzle, source, backPressure, .01, Accuracy()));
    }

    [Theory]
    [InlineData(.016)]
    [InlineData(.015)]
    public void SubsonicTransitAndEqualizationHaveFiniteAnalyticalTime(double remaining)
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.00001);
        const double back = 180000;
        const double equilibriumRatio = .75;
        var result = GasNozzleTransit.ToInventory(nozzle, source, back, remaining, Accuracy());
        var low = Math.Sqrt(remaining / source.Mass - equilibriumRatio);
        var high = Math.Sqrt(1 - equilibriumRatio);
        var coefficient = nozzle.Area * Math.Sqrt(4 * source.Density * source.Pressure);
        var elapsed = 2 * source.Mass / (coefficient * equilibriumRatio) * (high - low);
        var speedCoefficient = Math.Sqrt(2 * 800 * 400);
        var momentum = 2 * source.Mass * speedCoefficient / 3 * (Math.Pow(high, 3) - Math.Pow(low, 3));
        var kinetic = 3200 * (Math.Pow(high, 4) - Math.Pow(low, 4));
        Assert.InRange(Math.Abs(result.Duration - elapsed), 0, 1e-9);
        Assert.InRange(Math.Abs(result.MomentumImpulse - momentum), 0, 1e-7);
        Assert.InRange(Math.Abs(result.KineticEnergy - kinetic), 0, 1e-6);
        Assert.Equal(0, result.PressureImpulse);
        Assert.InRange(result.TimeErrorEstimate, 0, Accuracy().Time);
        Assert.InRange(result.MomentumErrorEstimate, 0, Accuracy().Impulse);
        Assert.InRange(result.EnergyErrorEstimate, 0, Accuracy().Energy);
    }

    [Theory]
    [InlineData(0, .01, .014)]
    [InlineData(20000, .006, .0075)]
    [InlineData(180000, .015, .017)]
    public void PartitionAndAreaScalingPreserveTransportedQuantities(double back, double remaining, double middle)
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.00001);
        var whole = GasNozzleTransit.ToInventory(nozzle, source, back, remaining, Accuracy());
        var first = GasNozzleTransit.ToInventory(nozzle, source, back, middle, Accuracy());
        var second = GasNozzleTransit.ToInventory(nozzle, first.Discharge.After, back, remaining, Accuracy());
        var doubled = GasNozzleTransit.ToInventory(new(.00002), source, back, remaining, Accuracy());
        Assert.InRange(Math.Abs(whole.Duration - first.Duration - second.Duration), 0, 1e-9);
        Assert.InRange(Math.Abs(whole.MomentumImpulse - first.MomentumImpulse - second.MomentumImpulse), 0, 1e-7);
        Assert.InRange(Math.Abs(whole.PressureImpulse - first.PressureImpulse - second.PressureImpulse), 0, 1e-7);
        Assert.InRange(Math.Abs(whole.KineticEnergy - first.KineticEnergy - second.KineticEnergy), 0, 1e-6);
        Assert.InRange(Math.Abs(whole.Duration - doubled.Duration * 2), 0, 1e-9);
        Assert.InRange(Math.Abs(whole.ThrustImpulse - doubled.ThrustImpulse), 0, 1e-7);
        Assert.InRange(Math.Abs(whole.KineticEnergy - doubled.KineticEnergy), 0, 1e-6);
        Assert.InRange(Math.Abs(whole.Discharge.OutletEnthalpy -
            first.Discharge.OutletEnthalpy - second.Discharge.OutletEnthalpy), 0, 1e-10);
    }

    [Fact]
    public void ZeroInventoryChangeHasZeroTransitAndClosedFlowCannotSpendMass()
    {
        var source = Source();
        var result = GasNozzleTransit.ToInventory(new(0), source, 0, source.Mass, Accuracy());
        Assert.Equal(0, result.Duration);
        Assert.Equal(0, result.ThrustImpulse);
        Assert.Equal(0, result.KineticEnergy);
        Assert.Equal(0, result.Evaluations);
        Assert.Same(source, result.Discharge.After);
        Assert.Throws<ArgumentException>(() => GasNozzleTransit.ToInventory(new(0), source, 0, .01, Accuracy()));
        Assert.Throws<ArgumentException>(() => GasNozzleTransit.ToInventory(new(.00001), source, source.Pressure, .01, Accuracy()));
        Assert.ThrowsAny<ArgumentException>(() => GasNozzleTransit.ToInventory(new(.00001), source, 180000, .014, Accuracy()));
    }

    [Fact]
    public void InsufficientNumericalBudgetRejectsWithoutPartialOutput()
    {
        var source = Source();
        Assert.Throws<InvalidOperationException>(() =>
            GasNozzleTransit.ToInventory(new(.00001), source, 0, .01, new(1e-15, 1e-15, 1e-15, 5)));
        Assert.Equal(Source(), source);
        Assert.Throws<ArgumentNullException>(() => GasNozzleTransit.ToInventory(null!, source, 0, .01, Accuracy()));
        Assert.Throws<ArgumentNullException>(() => GasNozzleTransit.ToInventory(new(1), null!, 0, .01, Accuracy()));
        Assert.Throws<ArgumentNullException>(() => GasNozzleTransit.ToInventory(new(1), source, 0, .01, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasTransitAccuracy(1, 1, 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasTransitAccuracy(1, 1, 1, 1000001));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAccuracyRejects(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasTransitAccuracy(value, 1, 1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasTransitAccuracy(1, value, 1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasTransitAccuracy(1, 1, value, 100));
    }

    [Theory]
    [InlineData(200, 400)]
    [InlineData(300, 750)]
    public void OtherMaterialsMatchIndependentMassCoordinateIntegration(double gasConstant, double cv)
    {
        var source = new SealedGasState(new(gasConstant, cv, 100, 1000, 100, 1e7),
            .02, .01, .02 * cv * 300);
        var nozzle = new ConvergingGasNozzle(.00001);
        var back = source.Pressure * .25;
        const double remaining = .009;
        var result = GasNozzleTransit.ToInventory(nozzle, source, back, remaining, Accuracy());
        const int panels = 20000;
        var width = (source.Mass - remaining) / panels;
        var gamma = 1 + gasConstant / cv;
        var critical = Math.Pow(2 / (gamma + 1), gamma / (gamma - 1));
        double time = 0, momentum = 0, pressureImpulse = 0, kinetic = 0;
        for (var i = 0; i < panels; i++)
        {
            var fraction = (remaining + (i + .5) * width) / source.Mass;
            var temperature = 300 * Math.Pow(fraction, gamma - 1);
            var pressure = source.Pressure * Math.Pow(fraction, gamma);
            var exitRatio = Math.Max(critical, back / pressure);
            var exitTemperature = temperature * Math.Pow(exitRatio, (gamma - 1) / gamma);
            var speed = Math.Sqrt(2 * (cv + gasConstant) * (temperature - exitTemperature));
            var exitPressure = pressure * exitRatio;
            var rate = nozzle.Area * exitPressure / (gasConstant * exitTemperature) * speed;
            time += width / rate;
            momentum += width * speed;
            pressureImpulse += nozzle.Area * (exitPressure - back) * width / rate;
            kinetic += .5 * speed * speed * width;
        }
        Assert.InRange(Math.Abs(result.Duration - time), 0, 1e-7);
        Assert.InRange(Math.Abs(result.MomentumImpulse - momentum), 0, 1e-6);
        Assert.InRange(Math.Abs(result.PressureImpulse - pressureImpulse), 0, 1e-6);
        Assert.InRange(Math.Abs(result.KineticEnergy - kinetic), 0, 1e-4);
        Assert.Equal(GasNozzleRegime.Choked, nozzle.Evaluate(source, back).Regime);
        Assert.Equal(GasNozzleRegime.Subsonic, nozzle.Evaluate(result.Discharge.After, back).Regime);
    }

    [Theory]
    [InlineData(0, .01)]
    [InlineData(20000, .006)]
    [InlineData(180000, .016)]
    public void RequestedDurationFindsInventoryWithSharedErrorBudget(double back, double expectedMass)
    {
        var source = Source();
        var nozzle = new ConvergingGasNozzle(.00001);
        var expected = GasNozzleTransit.ToInventory(nozzle, source, back, expectedMass, Accuracy());
        var minimum = back == 180000 ? .015 : .0055;
        var actual = GasNozzleTransit.AtDuration(nozzle, source, back, expected.Duration, minimum, Accuracy());
        Assert.Equal(expected.Duration, actual.Duration);
        Assert.InRange(Math.Abs(actual.Discharge.After.Mass - expectedMass), 0, 1e-10);
        Assert.InRange(Math.Abs(actual.ThrustImpulse - expected.ThrustImpulse), 0, 2e-7);
        Assert.InRange(Math.Abs(actual.KineticEnergy - expected.KineticEnergy), 0, 2e-6);
        Assert.InRange(actual.TimeErrorEstimate, 0, Accuracy().Time);
        Assert.InRange(actual.MomentumErrorEstimate, 0, Accuracy().Impulse);
        Assert.InRange(actual.PressureErrorEstimate, 0, Accuracy().Impulse);
        Assert.InRange(actual.EnergyErrorEstimate, 0, Accuracy().Energy);
        Assert.InRange(actual.Evaluations, 5, Accuracy().MaximumEvaluations);
        Assert.Equal(actual, GasNozzleTransit.AtDuration(nozzle, source, back, expected.Duration, minimum, Accuracy()));
    }

    [Fact]
    public void TimeRequestsPreserveNoFlowElapsedTimeAndRejectUnreachableHorizon()
    {
        var source = Source();
        var closed = GasNozzleTransit.AtDuration(new(0), source, 0, 10, .01, Accuracy());
        Assert.Equal(10, closed.Duration);
        Assert.Same(source, closed.Discharge.After);
        Assert.Equal(0, closed.ThrustImpulse);
        var balanced = GasNozzleTransit.AtDuration(new(.00001), source, source.Pressure, 10, .01, Accuracy());
        Assert.Equal(10, balanced.Duration);
        Assert.Same(source, balanced.Discharge.After);
        var zero = GasNozzleTransit.AtDuration(new(.00001), source, 0, 0, .01, Accuracy());
        Assert.Equal(0, zero.Duration);
        Assert.Same(source, zero.Discharge.After);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, 100, .01, Accuracy()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, double.NaN, .01, Accuracy()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, -1, .01, Accuracy()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, 1, 0, Accuracy()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, 1, .03, Accuracy()));
        Assert.Throws<InvalidOperationException>(() =>
            GasNozzleTransit.AtDuration(new(.00001), source, 0, .01, .01, new(1e-10, 1e-8, 1e-7, 5)));
    }
}
