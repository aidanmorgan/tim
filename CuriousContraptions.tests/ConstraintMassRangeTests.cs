using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConstraintMassRangeTests
{
    private static ConstraintMassMatrix Coupled()
    {
        var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, RigidPose.Identity,
            default, default, 1, new(1, 1, 1));
        var x = new ConstraintGradient([new(body, new(1, 0, 0), default)]);
        var tilted = new ConstraintGradient([new(body, new(.6, .8, 0), default)]);
        return new([x, tilted], [0, 0]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(1, -1)]
    [InlineData(3, -1)]
    [InlineData(4, -1)]
    public void CoupledRepresentableSolutionDoesNotLoseSmallEquation(int units, int sign)
    {
        // Analytic matrix [[1,.6],[.6,1]]: determinant .64. The tiny
        // first RHS is real input, even though its contribution to the
        // normal-sized rounded solution is below one output ULP.
        var matrix = Coupled();
        var small = sign * units * double.Epsilon;
        double[] rhs = [small, sign * 2d], result = [11, 12];
        matrix.Solve(rhs, result);
        Assert.InRange(Math.Abs(result[0] - (-.6 * rhs[1] / .64)), 0, 2e-14);
        Assert.InRange(Math.Abs(result[1] - (rhs[1] / .64)), 0, 2e-14);
        Assert.Equal(small, rhs[0]);
        Assert.Equal(sign * 2d, rhs[1]);
    }

    [Fact]
    public void ZeroAndLargeFiniteLoadsHaveAnalyticCoupledSolutions()
    {
        var matrix = Coupled();
        foreach (var large in new[] { 0d, 2d, -2d, double.MaxValue / 16, -double.MaxValue / 16 })
        {
            double[] result = [11, 12];
            matrix.Solve([0, large], result);
            if (large == 0) Assert.Equal(new double[] { 0, 0 }, result);
            else
            {
                Assert.InRange(Math.Abs(result[0] / large + .6 / .64), 0, 2e-14);
                Assert.InRange(Math.Abs(result[1] / large - 1 / .64), 0, 2e-14);
            }
        }
    }

    [Fact]
    public void UnrepresentableSolutionAndNonfiniteInputRejectAtomically()
    {
        var matrix = Coupled();
        double[] result = [11, 12];
        Assert.Throws<InvalidOperationException>(() => matrix.Solve([0, double.MaxValue], result));
        Assert.Equal(new double[] { 11, 12 }, result);
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Assert.Throws<ArgumentException>(() => matrix.Solve([invalid, 2], result));
            Assert.Equal(new double[] { 11, 12 }, result);
        }
    }

    [Fact]
    public void DownshiftLimitPreservesEveryBitIncludingOddSubnormals()
    {
        foreach (var magnitude in new[] { double.Epsilon, 2 * double.Epsilon, 3 * double.Epsilon,
            4 * double.Epsilon, 6 * double.Epsilon, Math.ScaleB(1, -1022),
            Math.BitIncrement(Math.ScaleB(1, -1022)), 1d, Math.BitIncrement(1d), double.MaxValue })
        foreach (var sign in new[] { -1, 1 })
        {
            var value = sign * magnitude;
            var shift = ConstraintMassMatrix.MaximumExactDownshift(value);
            var scaled = Math.ScaleB(value, -shift);
            Assert.Equal(BitConverter.DoubleToInt64Bits(value),
                BitConverter.DoubleToInt64Bits(Math.ScaleB(scaled, shift)));
            Assert.NotEqual(BitConverter.DoubleToInt64Bits(value),
                BitConverter.DoubleToInt64Bits(Math.ScaleB(Math.ScaleB(value, -shift - 1), shift + 1)));
        }
        Assert.Equal(0, ConstraintMassMatrix.MaximumExactDownshift(3 * double.Epsilon));
        // Nonzero survival alone would miss this loss of one original low bit.
        Assert.NotEqual(3 * double.Epsilon, Math.ScaleB(Math.ScaleB(3 * double.Epsilon, -1), 1));
        foreach (var invalid in new[] { 0d, -0d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ConstraintMassMatrix.MaximumExactDownshift(invalid));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void CoupledNearRangeLoadAndOddSubnormalPreserveInputAndFiniteSolution(int sign)
    {
        var matrix = Coupled();
        var large = sign * double.MaxValue / 16;
        double[] rhs = [sign * 3 * double.Epsilon, large], result = [11, 12];
        matrix.Solve(rhs, result);
        Assert.InRange(Math.Abs(result[0] / large + .6 / .64), 0, 2e-14);
        Assert.InRange(Math.Abs(result[1] / large - 1 / .64), 0, 2e-14);
        Assert.Equal(sign * 3 * double.Epsilon, rhs[0]);
    }
}
