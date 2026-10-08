using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AdiabaticGasDischargeTests
{
    private static SealedGasState Initial(double gasConstant = 200, double cv = 400) =>
        new(new(gasConstant, cv, 100, 1000, 100, 1e7), .02, .01, .02 * cv * 300);

    [Fact]
    public void QuarterInventoryHasAnalyticalPressureTemperatureAndOutletEnthalpy()
    {
        var source = Initial();
        var result = AdiabaticGasDischarge.Propose(source, .005);
        Assert.Same(source, result.Before);
        Assert.Same(source.Material, result.After.Material);
        Assert.Equal(source.Volume, result.After.Volume);
        Assert.Equal(.005, result.After.Mass);
        Assert.InRange(Math.Abs(result.After.InternalEnergy - 300), 0, 1e-12);
        Assert.InRange(Math.Abs(result.After.Temperature - 150), 0, 1e-12);
        Assert.InRange(Math.Abs(result.After.Pressure - 15000), 0, 1e-10);
        Assert.Equal(source.Mass - result.After.Mass, result.DischargedMass);
        Assert.Equal(source.InternalEnergy - result.After.InternalEnergy, result.OutletEnthalpy);
        Assert.Equal(2400, source.InternalEnergy);
        Assert.Equal(.02, source.Mass);
        Assert.Equal(result, AdiabaticGasDischarge.Propose(source, .005));
        Assert.True(result.OutletEnthalpy > result.DischargedMass * 400 * result.After.Temperature);
    }

    [Theory]
    [InlineData(200, 400)]
    [InlineData(300, 750)]
    public void DischargeMatchesIndependentEnthalpyIntegralAndPartition(double gasConstant, double cv)
    {
        var source = Initial(gasConstant, cv);
        const double remaining = .008;
        const int intervals = 10000;
        var width = (source.Mass - remaining) / intervals;
        double outletEnergy = 0;
        for (var i = 0; i < intervals; i++)
        {
            var mass = remaining + (i + .5) * width;
            var temperature = source.Temperature * Math.Pow(mass / source.Mass, gasConstant / cv);
            outletEnergy += (cv + gasConstant) * temperature * width;
        }
        var whole = AdiabaticGasDischarge.Propose(source, remaining);
        Assert.InRange(Math.Abs(whole.OutletEnthalpy - outletEnergy), 0, 2e-6);
        var first = AdiabaticGasDischarge.Propose(source, .014);
        var second = AdiabaticGasDischarge.Propose(first.After, remaining);
        Assert.InRange(Math.Abs(second.After.InternalEnergy - whole.After.InternalEnergy), 0, 1e-10);
        Assert.InRange(Math.Abs(first.OutletEnthalpy + second.OutletEnthalpy - whole.OutletEnthalpy), 0, 1e-10);
        Assert.InRange(Math.Abs(first.DischargedMass + second.DischargedMass - whole.DischargedMass), 0, 1e-17);
        Assert.True(whole.After.Temperature < source.Temperature);
        Assert.True(whole.After.Pressure < source.Pressure);
        Assert.True(whole.After.Density < source.Density);
    }

    [Fact]
    public void ClosedInventoryIsAnExactIdentity()
    {
        var source = Initial();
        var result = AdiabaticGasDischarge.Propose(source, source.Mass);
        Assert.Same(source, result.After);
        Assert.Equal(0, result.DischargedMass);
        Assert.Equal(0, result.OutletEnthalpy);
    }

    [Fact]
    public void InitialNozzleEnthalpyRateMatchesDifferentialReservoirDebit()
    {
        var source = Initial();
        var nozzle = new ConvergingGasNozzle(.00001);
        var flow = nozzle.Evaluate(source, 0);
        const double removed = 1e-8;
        var discharge = AdiabaticGasDischarge.Propose(source, source.Mass - removed);
        var averageSpecificEnthalpy = discharge.OutletEnthalpy / discharge.DischargedMass;
        Assert.InRange(Math.Abs(averageSpecificEnthalpy - flow.EnthalpyPower / flow.MassRate), 0, .03);
        var depletedFlow = nozzle.Evaluate(discharge.After, 0);
        Assert.True(depletedFlow.MassRate < flow.MassRate);
        Assert.True(depletedFlow.EnthalpyPower < flow.EnthalpyPower);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(.021)]
    public void UnsupportedFinalInventoryRejects(double mass)
    {
        var source = Initial();
        Assert.Throws<ArgumentOutOfRangeException>(() => AdiabaticGasDischarge.Propose(source, mass));
        Assert.Equal(Initial(), source);
    }

    [Fact]
    public void MaterialAndNumericalBoundariesRejectWithoutMutatingSource()
    {
        var source = Initial();
        Assert.Throws<ArgumentNullException>(() => AdiabaticGasDischarge.Propose(null!, .01));
        Assert.Throws<ArgumentException>(() => AdiabaticGasDischarge.Propose(source, .001));
        Assert.Throws<ArgumentException>(() => AdiabaticGasDischarge.Propose(source, double.Epsilon));
        Assert.Equal(Initial(), source);
        // Adjacent final inventories either yield an accounted representable debit or reject.
        var adjacent = AdiabaticGasDischarge.Propose(source, Math.BitDecrement(source.Mass));
        Assert.True(adjacent.DischargedMass > 0);
        Assert.True(adjacent.OutletEnthalpy > 0);
        Assert.Equal(Math.BitDecrement(source.Mass), adjacent.After.Mass);
    }
}
