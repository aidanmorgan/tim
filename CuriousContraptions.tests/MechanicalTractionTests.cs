namespace CuriousContraptions.Tests;

public class MechanicalTractionTests
{
    [Theory]
    [InlineData(0f, 4f, 1f, 1d, 100d, 1f, .5d)]
    [InlineData(0f, 4f, 1f, 100d, 2d, 2f, 2d)]
    [InlineData(0f, -4f, 1f, 100d, 2d, -2f, 2d)]
    [InlineData(10f, 4f, 1f, 100d, 0d, 4f, 0d)]
    [InlineData(3f, -4f, 1f, 100d, 0d, 0f, 0d)]
    [InlineData(3f, -4f, 1f, 100d, 2d, -2f, 2d)]
    [InlineData(-3f, 4f, 1f, 100d, 2d, 2f, 2d)]
    [InlineData(0f, 4f, 4f, 1d, 100d, .25f, .125d)]
    public void BoundedDriveAndDissipativeBraking(float incoming, float target, float mass,
        double impulse, double work, float expected, double spent)
    {
        var result = MechanicalTraction.Apply(incoming, target, mass, impulse, work);
        Assert.Equal(expected, result.Speed);
        Assert.Equal(spent, result.Work);
        Assert.InRange(result.Impulse, 0, impulse);
    }

    [Fact]
    public void FloatRoundingNeverOverdrawsAcrossSignedCases()
    {
        foreach (var incoming in new[] {-10f, -1f, 0f, 1f, 10f})
        foreach (var target in new[] {-12f, -3f, 3f, 12f})
        foreach (var mass in new[] {.1f, 1f, 8f})
        foreach (var work in new[] {0d, 1e-20, .001, 10, 100})
        {
            var result = MechanicalTraction.Apply(incoming, target, mass, .3, work);
            Assert.InRange(result.Work, 0, work);
            Assert.InRange(result.Impulse, 0, .3);
            Assert.InRange(result.Speed, Math.Min(incoming, target), Math.Max(incoming, target));
        }
    }

    [Fact]
    public void RepeatedCallsMustShareTheirRemainingAllowance()
    {
        var remainingWork = 1d;
        var remainingImpulse = .7d;
        var speed = 0f;
        for (var i = 0; i < 100; i++)
        {
            var result = MechanicalTraction.Apply(speed, 10, 1, remainingImpulse, remainingWork);
            remainingWork -= result.Work; remainingImpulse -= result.Impulse;
            speed = result.Speed;
            Assert.True(remainingWork >= 0);
            Assert.True(remainingImpulse >= 0);
        }
        Assert.InRange(speed, .69999f, .7f);
        Assert.InRange(.5 * speed * speed, 0, 1 - remainingWork + 1e-12);
    }

    [Theory]
    [InlineData(float.NaN, 1f, 1f, 1d, 1d)]
    [InlineData(0f, float.PositiveInfinity, 1f, 1d, 1d)]
    [InlineData(0f, 1f, 0f, 1d, 1d)]
    [InlineData(0f, 1f, 1f, -1d, 1d)]
    [InlineData(0f, 1f, 1f, 1d, double.NaN)]
    public void InvalidInputRejected(float incoming, float target, float mass, double impulse, double work) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MechanicalTraction.Apply(incoming, target, mass, impulse, work));
}
