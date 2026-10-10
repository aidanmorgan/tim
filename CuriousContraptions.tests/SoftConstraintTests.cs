using System;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class SoftConstraintTests
{
    [Fact]
    public void DimensionalDampingMatchesTheExistingBox2DSoftLaw()
    {
        const float h = 1f / 480, mass = .25f, stiffness = 400, damping = .2f;
        var soft = SoftConstraintCoefficients.FromSpring(stiffness, damping, h);
        var omega = MathF.Sqrt(stiffness / mass);
        var zeta = damping / (2 * MathF.Sqrt(stiffness * mass));
        Assert.InRange(zeta, .009999f, .010001f);
        var a2 = h * omega * (2 * zeta + h * omega);
        var massScale = a2 / (1 + a2);
        Assert.InRange(MathF.Abs(1 / (1 / mass + soft.Gamma) - massScale * mass), 0, 1e-7f);
        Assert.InRange(MathF.Abs(soft.BiasRate - omega / (2 * zeta + h * omega)), 0, 1e-4f);
    }

    [Theory]
    [InlineData(0, .2f, .002f)]
    [InlineData(-1, .2f, .002f)]
    [InlineData(float.NaN, .2f, .002f)]
    [InlineData(400, -1, .002f)]
    [InlineData(400, float.PositiveInfinity, .002f)]
    [InlineData(400, .2f, 0)]
    [InlineData(float.MaxValue, float.MaxValue, 1f)]
    [InlineData(1f, float.MaxValue, 2f)]
    [InlineData(float.Epsilon, 1f, float.Epsilon)]
    public void InvalidCoefficientsRejectBeforeStepping(float k, float c, float h) =>
        Assert.Throws<ArgumentException>(() => SoftConstraintCoefficients.FromSpring(k, c, h));

    [Fact]
    public void FourIndependentRowsMatchDoublePrecisionEquationAndProjectTotalImpulse()
    {
        var inverseMass = Vector128.Create(4f, .5f, 2f, 1f);
        var velocity = Vector128.Create(-3f, 2f, -1f, 4f);
        var error = Vector128.Create(-.1f, .04f, -.02f, .03f);
        var gamma = Vector128.Create(2f, 3f, 0f, 1f);
        var bias = Vector128.Create(4f, 2f, 0f, 3f);
        var old = Vector128.Create(.2f, -.3f, 4f, 0f);
        var lo = Vector128.Create(float.NegativeInfinity, -1f, 0f, 0f);
        var hi = Vector128.Create(float.PositiveInfinity, 0f, 2f, 1f);
        var result = SoftConstraint.Solve4(inverseMass, velocity, error, gamma, bias, old, lo, hi);
        for (var lane = 0; lane < 4; lane++)
        {
            var increment = -(velocity.GetElement(lane) + (double)bias.GetElement(lane) * error.GetElement(lane) +
                gamma.GetElement(lane) * (double)old.GetElement(lane)) / (inverseMass.GetElement(lane) + (double)gamma.GetElement(lane));
            var expected = Math.Clamp(old.GetElement(lane) + increment, lo.GetElement(lane), hi.GetElement(lane));
            Assert.InRange(Math.Abs(result.Accumulated.GetElement(lane) - expected), 0, 1e-6);
            Assert.InRange(Math.Abs(result.Delta.GetElement(lane) - (expected - old.GetElement(lane))), 0, 1e-6);
        }
        Assert.True(result.Delta.GetElement(2) < 0); // A unilateral row can shed a previous impulse.
    }

    [Fact]
    public void NumericalFailureIsIsolatedToItsLane()
    {
        var old = Vector128.Create(.25f);
        var result = SoftConstraint.Solve4(Vector128.Create(1f, 0f, 1f, 1f),
            Vector128.Create(float.NaN, 1f, float.MaxValue, -1f), Vector128<float>.Zero,
            Vector128.Create(0f), Vector128.Create(0f), old,
            Vector128.Create(float.NegativeInfinity), Vector128.Create(float.PositiveInfinity));
        Assert.Equal(.25f, result.Accumulated.GetElement(0));
        Assert.Equal(.25f, result.Accumulated.GetElement(1));
        Assert.True(float.IsFinite(result.Accumulated.GetElement(2)));
        Assert.Equal(1.25f, result.Accumulated.GetElement(3));
    }

    [Theory]
    [InlineData(0f, 1)]
    [InlineData(0f, 12)]
    [InlineData(.2f, 1)]
    [InlineData(.2f, 12)]
    [InlineData(8f, 12)]
    public void OscillatorMatchesIndependentImplicitStepAndCannotMultiplyForceByIteration(float damping, int iterations)
    {
        const float h = 1f / 480, mass = .25f, stiffness = 400;
        var soft = SoftConstraintCoefficients.FromSpring(stiffness, damping, h);
        var q = -.1f; var v = 0f; double expectedQ = q, expectedV = v;
        var previousEnergy = .5f * stiffness * q * q;
        for (var step = 0; step < 480; step++)
        {
            // Independent backward-Euler elimination, not the row update formula.
            expectedV = (mass * expectedV - h * stiffness * expectedQ) /
                (mass + h * damping + h * h * stiffness);
            expectedQ += h * expectedV;
            var impulse = 0f;
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var result = Row(1 / mass, v, q, soft, impulse);
                impulse = result.Accumulated.GetElement(0);
                v += result.Delta.GetElement(0) / mass;
            }
            q += h * v;
            Assert.InRange(Math.Abs(q - expectedQ), 0, 2e-6);
            Assert.InRange(Math.Abs(v - expectedV), 0, 1e-4);
            var energy = .5f * mass * v * v + .5f * stiffness * q * q;
            Assert.True(energy <= previousEnergy + 2e-6f);
            previousEnergy = energy;
        }
    }

    [Fact]
    public void PhysicalPeriodAndDampingConvergeAsTheStepShrinks()
    {
        var period = 2 * Math.PI * Math.Sqrt(.25 / 400);
        var coarse = Sample(1f / 480, .2f, period);
        var fine = Sample(1f / 960, .2f, period);
        var analyticAmplitude = .1 * Math.Exp(-.2 * period / (2 * .25));
        // Backward Euler has expected numerical damping; no exact continuous-envelope claim.
        Assert.InRange(coarse.Period, period * .99, period * 1.01);
        Assert.InRange(fine.Period, period * .99, period * 1.01);
        Assert.True(coarse.Amplitude < analyticAmplitude);
        Assert.True(Math.Abs(fine.Amplitude - analyticAmplitude) < Math.Abs(coarse.Amplitude - analyticAmplitude));
    }

    private static (double Period, double Amplitude) Sample(float h, float damping, double period)
    {
        var soft = SoftConstraintCoefficients.FromSpring(400, damping, h);
        float q = -.1f, v = 0; double first = -1, second = -1, amplitude = 0;
        for (var step = 0; step < (int)(2 * period / h) + 2; step++)
        {
            var before = q;
            var result = Row(4, v, q, soft, 0);
            v += 4 * result.Delta.GetElement(0); q += h * v;
            if (before < 0 && q >= 0)
            {
                var crossing = (step + (double)(-before / (q - before))) * h;
                if (first < 0) first = crossing; else if (second < 0) second = crossing;
            }
            if (step == (int)Math.Round(period / h) - 1) amplitude = Math.Abs(q);
        }
        Assert.True(second > first && first > 0);
        return (second - first, amplitude);
    }

    private static ConstraintRowImpulse Row(float inverseMass, float velocity, float error,
        SoftConstraintCoefficients soft, float accumulated) =>
        SoftConstraint.Solve4(Vector128.Create(inverseMass), Vector128.Create(velocity), Vector128.Create(error),
            Vector128.Create(soft.Gamma), Vector128.Create(soft.BiasRate), Vector128.Create(accumulated),
            Vector128.Create(float.NegativeInfinity), Vector128.Create(float.PositiveInfinity));
}
