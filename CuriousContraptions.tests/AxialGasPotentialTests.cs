using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AxialGasPotentialTests
{
    private static SealedGasState Gas() => new(new(200, 400, 100, 1000, 100, 1e7), .02, .01, 2400);

    [Theory]
    [InlineData(.1)]
    [InlineData(-.1)]
    public void OrientedChambersUsePressureAtRestAndPayFiniteTravel(double area)
    {
        var gas = Gas();
        var law = new AxialGasPotential(gas, 2, area);
        Assert.Same(gas, law.State(2));
        Assert.Equal(area * gas.Pressure, law.IntervalEffort(2, 2));
        var end = 2 + .03 / area;
        var state = law.State(end);
        Assert.InRange(Math.Abs(state.Volume - .04), 0, 1e-16);
        Assert.InRange(Math.Abs(state.InternalEnergy - 1200), 0, 1e-10);
        var effort = law.IntervalEffort(2, end);
        Assert.InRange(Math.Abs(effort * (end - 2) - 1200), 0, 1e-10);
        Assert.Equal(effort, law.IntervalEffort(end, 2));
        Assert.Equal(gas.Mass, state.Mass);
        Assert.Equal(2400, gas.InternalEnergy);
        Assert.Equal(state, law.State(end));
    }

    [Theory]
    [InlineData(.1, -.05, .1)]
    [InlineData(-.1, -.1, .05)]
    public void WorkIsIndependentOfSubdivisionAndMatchesPressureQuadrature(double area, double start, double end)
    {
        var law = new AxialGasPotential(Gas(), 0, area);
        var middle = (start + end) / 2;
        var work = law.IntervalEffort(start, end) * (end - start);
        var split = law.IntervalEffort(start, middle) * (middle - start) +
            law.IntervalEffort(middle, end) * (end - middle);
        Assert.InRange(Math.Abs(work - split), 0, 1e-9);
        Assert.InRange(Math.Abs(work - (law.Energy(start) - law.Energy(end))), 0, 1e-9);
        const int count = 10000;
        var width = (end - start) / count;
        double integral = 0;
        for (var i = 0; i < count; i++)
        {
            var coordinate = start + (i + .5) * width;
            // Independent p V^gamma invariant, not the implementation's effort.
            var volume = .01 + area * coordinate;
            integral += area * 120000 * Math.Pow(.01 / volume, 1.5) * width;
        }
        Assert.InRange(Math.Abs(work - integral), 0, .0001);
    }

    [Theory]
    [InlineData(.1)]
    [InlineData(-.1)]
    public void TinyStrokesRetainPressureAndEndpointSymmetry(double area)
    {
        var law = new AxialGasPotential(Gas(), 1, area);
        var next = Math.BitIncrement(1);
        var effort = law.IntervalEffort(1, next);
        Assert.InRange(Math.Abs(effort - area * 120000), 0, 1e-9);
        Assert.Equal(effort, law.IntervalEffort(next, 1));
        Assert.Equal(law.IntervalEffort(1, 1), area * 120000);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonfiniteDeclarationsAndCoordinatesReject(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasPotential(Gas(), value, .1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasPotential(Gas(), 0, value));
        var law = new AxialGasPotential(Gas(), 0, .1);
        Assert.Throws<ArgumentOutOfRangeException>(() => law.State(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => law.IntervalEffort(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => law.IntervalEffort(value, 0));
    }

    [Fact]
    public void UnsupportedVolumeThermalEnvelopeAndNumericalEffortReject()
    {
        Assert.Throws<ArgumentNullException>(() => new AxialGasPotential(null!, 0, .1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasPotential(Gas(), 0, 0));
        var law = new AxialGasPotential(Gas(), 0, .1);
        Assert.Throws<ArgumentOutOfRangeException>(() => law.State(-1));
        Assert.Throws<ArgumentException>(() => law.State(1));
        Assert.Throws<ArgumentException>(() => law.IntervalEffort(0, 1));
        Assert.Throws<ArgumentException>(() => law.IntervalEffort(1, 0));
        Assert.Throws<InvalidOperationException>(() => new AxialGasPotential(Gas(), 0, double.MaxValue).IntervalEffort(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxialGasPotential(Gas(), 0, double.Epsilon).IntervalEffort(0, .01));
    }
}
