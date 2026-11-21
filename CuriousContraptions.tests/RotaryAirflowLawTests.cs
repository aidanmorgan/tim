using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RotaryAirflowLawTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(-1f)]
    public void LowFlowSelectsFirstZeroRatherThanFarSideOfPlateau(float sign)
    {
        RotaryAirflowBranch[] branches = [new(1, 0, .75f, .75f, sign)];
        var value = RotaryAirflowLaw.Prepare(branches, .4f, 4, .05f, 12);
        PitchClose(1f / 3, value.Pitch);
        ResistanceClose(0, value.Resistance);
        Assert.Equal(sign * 3, value.TargetRate);
        Assert.True(sign * value.Torque(branches, sign * 2.7f) > 0);
        TorqueClose(0, value.Torque(branches, sign * 3));
        TorqueClose(0, value.Torque(branches, sign * 3.3f));
    }

    [Theory]
    [InlineData(.25f, .25f)]
    [InlineData(1f / 3, 1f / 3)]
    [InlineData(.4f, 1f / 3)]
    public void DeclaredPitchLimitBeforeAtOrAfterBoundaryUsesSameLaw(float limit, float expected)
    {
        RotaryAirflowBranch[] branches = [new(1, 0, .75f, .75f, 1)];
        var value = RotaryAirflowLaw.Prepare(branches, limit, 4, .05f, 12);
        PitchClose(expected, value.Pitch);
        Assert.True(value.Resistance >= 0);
        TorqueClose(0, value.Torque(branches, 3));
        Assert.True(value.Torque(branches, 2.7f) > 0);
        if (limit == .25f) Assert.True(value.Torque(branches, 3.3f) < 0);
        else TorqueClose(0, value.Torque(branches, 3.3f));
    }

    [Theory]
    [InlineData(1f, .6666667f, 6f, .48f)]
    [InlineData(-1f, .6666667f, 6f, .48f)]
    [InlineData(1f, 4f, 12f, .18f)]
    [InlineData(-1f, 4f, 12f, .18f)]
    public void OrdinaryUnloadedEquilibriumHasOpposingTorqueAboveTarget(float sign, float response,
        float speed, float damping)
    {
        RotaryAirflowBranch[] branches = [new(12, 0, .75f, 9, sign)];
        var value = RotaryAirflowLaw.Prepare(branches, .4f, response, .05f, 12);
        PitchClose(.4f, value.Pitch); ResistanceClose(damping, value.Resistance); RateClose(sign * speed, value.TargetRate);
        Assert.True(sign * value.Torque(branches, sign * speed * .9f) > 0);
        TorqueClose(0, value.Torque(branches, sign * speed));
        Assert.True(sign * value.Torque(branches, sign * speed * 1.1f) < 0);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(-1f)]
    public void CappedOpposedBranchesKeepTheirDeclaredCapsDuringSearch(float sign)
    {
        // H(p)=min(.75,max(2-p,0))-.5*(1+p); R0=.25, response 4 => target 1, first zero .5.
        RotaryAirflowBranch[] branches = [new(2, 0, 1, .75f, sign), new(1, 0, .5f, 4, -sign)];
        var value = RotaryAirflowLaw.Prepare(branches, 1, 4, .05f, 12);
        PitchClose(.5f, value.Pitch); ResistanceClose(0, value.Resistance); RateClose(sign, value.TargetRate);
        Assert.Equal(.75f, branches[0].ForceLimit);
        Array.Reverse(branches);
        var reordered = RotaryAirflowLaw.Prepare(branches, 1, 4, .05f, 12);
        PitchClose(value.Pitch, reordered.Pitch); RateClose(value.TargetRate, reordered.TargetRate);
    }

    [Fact]
    public void MovingSampleOrChangedAllocationRecomputesRestForceAndResponse()
    {
        RotaryAirflowBranch[] stationary = [new(12, 0, .75f, 9, 1)];
        RotaryAirflowBranch[] moving = [new(12, 4, .75f, 9, 1)];
        RotaryAirflowBranch[] depleted = [new(12, 0, 0, 0, 1)];
        var a = RotaryAirflowLaw.Prepare(stationary, .4f, 1, .05f, 12);
        var b = RotaryAirflowLaw.Prepare(moving, .4f, 1, .05f, 12);
        Assert.Equal(9, a.TargetRate); Assert.Equal(6, b.TargetRate);
        TorqueClose(0, b.Torque(moving, 6));
        var c = RotaryAirflowLaw.Prepare(depleted, .4f, 1, .05f, 12);
        Assert.Equal(0, c.TargetRate); Assert.Equal(0, c.Pitch); Assert.Equal(.4f, c.Resistance);
        Assert.True(c.Torque(depleted, 1) < 0); Assert.True(c.Torque(depleted, -1) > 0);
    }

    [Theory]
    [InlineData(.049f, false)]
    [InlineData(.05f, true)]
    public void CutInIsInclusiveForceThreshold(float force, bool active)
    {
        RotaryAirflowBranch[] branches = [new(1, 0, force, force, 1)];
        var value = RotaryAirflowLaw.Prepare(branches, .4f, 1, .05f, 12);
        Assert.Equal(active, value.Pitch > 0);
        Assert.Equal(active ? force : 0, value.TargetRate);
    }

    [Fact]
    public void EqualOppositionAndEdgeOnArePassive()
    {
        RotaryAirflowBranch[] opposed = [new(12, 0, .75f, 9, 1), new(12, 0, .75f, 9, -1)];
        Assert.Equal(0, RotaryAirflowLaw.Prepare(opposed, .4f, 1, .05f, 12).Pitch);
        Assert.Equal(0, RotaryAirflowLaw.Prepare([new(12, 0, .75f, 9, 0)], .4f, 1, .05f, 12).Pitch);
        Assert.Equal(0, RotaryAirflowLaw.Prepare([], .4f, 1, .05f, 12).Pitch);
    }

    [Fact]
    public void InvalidParametersAndFrozenBranchesAreRefused()
    {
        Assert.Throws<ArgumentException>(() => RotaryAirflowLaw.Prepare([], 0, 1, .05f, 12));
        Assert.Throws<ArgumentException>(() => RotaryAirflowLaw.Prepare([], .4f, float.NaN, .05f, 12));
        Assert.Throws<ArgumentException>(() => RotaryAirflowLaw.Prepare([new(1, 0, 1, 1, 2)], .4f, 1, .05f, 12));
    }

    [Fact]
    public void UnrepresentableResponseCombinationsAreRefused()
    {
        Assert.Throws<ArgumentException>(() => RotaryAirflowLaw.Prepare([], .4f, float.Epsilon, .05f, 12));
        Assert.Throws<ArgumentException>(() => RotaryAirflowLaw.Prepare([], float.Epsilon, 1e-20f, float.Epsilon, 12));
    }

    // Local constitutive interfaces only, never scene-motion allowances. Pitch discriminates
    // the .4 vs 1/3 boundary; resistance separates the zero plateau from ordinary .18/.48 damping.
    private static void PitchClose(float expected, float actual) =>
        Assert.InRange(actual, expected - 2e-6f, expected + 2e-6f); // m/rad
    private static void ResistanceClose(float expected, float actual) =>
        Assert.InRange(actual, expected - 2e-6f, expected + 2e-6f); // N m s/rad
    // Rate protects response/cap mapping through at most 12 rad/s; torque protects the zero
    // of opposing terms up to 3.6 N m. These permit local f32 evaluation without hiding a changed law.
    private static void RateClose(float expected, float actual) =>
        Assert.InRange(actual, expected - 2e-5f, expected + 2e-5f); // rad/s
    private static void TorqueClose(float expected, float actual) =>
        Assert.InRange(actual, expected - 2e-5f, expected + 2e-5f); // N m
}
