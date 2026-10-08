using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SealedGasStateTests
{
    private static IdealGasMaterial Material() => new(200, 400, 100, 1000, 100, 10000000);
    private static SealedGasState Initial() => new(Material(), .02, .01, 2400);

    [Fact]
    public void StateDerivesAbsolutePressureAndTemperatureFromFiniteInventory()
    {
        var state = Initial();
        Assert.Equal(300, state.Temperature);
        Assert.Equal(2, state.Density);
        Assert.Equal(120000, state.Pressure);
        Assert.Equal(1.5, state.Material.HeatCapacityRatio);
        Assert.Equal(state.Mass * state.Material.SpecificGasConstant * state.Temperature,
            state.Pressure * state.Volume);
    }

    [Theory]
    [InlineData(.0025, 4800, 600, 960000, 2400)]
    [InlineData(.04, 1200, 150, 15000, -1200)]
    [InlineData(.01, 2400, 300, 120000, 0)]
    public void AnalyticalCompressionExpansionAndNoMotionBalanceWork(
        double volume, double energy, double temperature, double pressure, double work)
    {
        var before = Initial();
        var result = before.ProposeAdiabaticVolume(volume);
        Assert.Same(before, result.Before);
        Assert.Equal(2400, before.InternalEnergy);
        Assert.Equal(energy, result.After.InternalEnergy);
        Assert.Equal(temperature, result.After.Temperature);
        Assert.Equal(pressure, result.After.Pressure);
        Assert.Equal(work, result.WorkOnGas);
        Assert.Equal(before.Mass, result.After.Mass);
        Assert.Same(before.Material, result.After.Material);
        Assert.Equal(before.InternalEnergy + result.WorkOnGas, result.After.InternalEnergy);
        Assert.Equal(result, before.ProposeAdiabaticVolume(volume));
    }

    [Theory]
    [InlineData(200, 400)]
    [InlineData(300, 750)]
    public void WorkMatchesIndependentPressureIntegralAndSubdivision(double gasConstant, double cv)
    {
        var initial = new SealedGasState(new(gasConstant, cv, 100, 1000, 100, 10000000),
            .02, .01, .02 * cv * 300);
        const double end = .004;
        const int intervals = 10000;
        var width = (end - initial.Volume) / intervals;
        double integral = 0;
        var gamma = 1 + gasConstant / cv;
        for (var i = 0; i < intervals; i++)
        {
            var volume = initial.Volume + (i + .5) * width;
            var pressure = initial.Pressure * Math.Pow(initial.Volume / volume, gamma);
            integral -= pressure * width;
        }
        var whole = initial.ProposeAdiabaticVolume(end);
        Assert.InRange(Math.Abs(whole.WorkOnGas - integral), 0, .00002);
        var first = initial.ProposeAdiabaticVolume(.007);
        var second = first.After.ProposeAdiabaticVolume(end);
        Assert.InRange(Math.Abs(second.After.InternalEnergy - whole.After.InternalEnergy), 0, 1e-10);
        Assert.InRange(Math.Abs(first.WorkOnGas + second.WorkOnGas - whole.WorkOnGas), 0, 1e-10);
        var returned = whole.After.ProposeAdiabaticVolume(initial.Volume);
        Assert.InRange(Math.Abs(returned.After.InternalEnergy - initial.InternalEnergy), 0, 1e-10);
        Assert.InRange(Math.Abs(whole.WorkOnGas + returned.WorkOnGas), 0, 1e-10);
        // Exact restoration is retaining the immutable construction state, not numerically reversing a cycle.
        Assert.Equal(initial, whole.Before);
        Assert.Equal(whole, initial.ProposeAdiabaticVolume(end));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void UnsupportedScalarInputsReject(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(value, 400, 100, 1000, 100, 1e7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, value, 100, 1000, 100, 1e7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, 400, value, 1000, 100, 1e7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, 400, 100, value, 100, 1e7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, 400, 100, 1000, value, 1e7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, 400, 100, 1000, 100, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SealedGasState(Material(), value, .01, 2400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SealedGasState(Material(), .02, value, 2400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SealedGasState(Material(), .02, .01, value));
        var initial = Initial();
        Assert.Throws<ArgumentOutOfRangeException>(() => initial.ProposeAdiabaticVolume(value));
        Assert.Equal(2400, initial.InternalEnergy);
    }

    [Fact]
    public void UnsupportedMaterialAndThermodynamicEnvelopesReject()
    {
        Assert.Throws<ArgumentNullException>(() => new SealedGasState(null!, .02, .01, 2400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdealGasMaterial(200, 400, 1000, 100, 100, 1e7));
        Assert.Throws<ArgumentException>(() => new IdealGasMaterial(double.Epsilon, 400, 100, 1000, 100, 1e7));
        Assert.Throws<ArgumentException>(() => new IdealGasMaterial(double.MaxValue, double.Epsilon, 100, 1000, 100, 1e7));
        Assert.Throws<ArgumentException>(() => new SealedGasState(Material(), .02, .01, 799));
        Assert.Throws<ArgumentException>(() => new SealedGasState(Material(), .02, .01, 8001));
        Assert.Throws<ArgumentException>(() => new SealedGasState(Material(), .02, .0001, 2400));
        Assert.Throws<ArgumentException>(() => new SealedGasState(new(200, 400, 100, 1000, 100, 100000), .02, .01, 2400));
        var initial = Initial();
        Assert.Throws<ArgumentException>(() => initial.ProposeAdiabaticVolume(.0001));
        Assert.Throws<ArgumentException>(() => initial.ProposeAdiabaticVolume(.1));
        Assert.Equal(2400, initial.InternalEnergy);
    }

    [Fact]
    public void OverflowUnderflowAndUnresolvableWorkRejectInsteadOfProducingFreeWork()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SealedGasState(Material(), double.MaxValue, 1, 2400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SealedGasState(
            new(1e-10, 1e-10, 100, 1000, 100, 1e7), double.Epsilon, .01, 2400));
        var initial = Initial();
        Assert.Throws<ArgumentOutOfRangeException>(() => initial.ProposeAdiabaticVolume(double.Epsilon));
        var nearIsothermal = new SealedGasState(new(1e-10, 400, 100, 1000, 100, 1e7), .02, .01, 2400);
        Assert.Throws<ArgumentException>(() => nearIsothermal.ProposeAdiabaticVolume(Math.BitDecrement(.01)));
    }
}
