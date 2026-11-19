using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ContactIntegrityTests
{
    [Fact]
    public void MultipointImpulseSumsWithinOneUnorderedPairOnly()
    {
        var tracker = new ContactIntegrity();
        ContactImpulseSample[] points =
        [
            new(new(1), new(2), 1f, 1.5f),
            new(new(2), new(1), 1f, 1.75f),
            new(new(1), new(3), 1f, 2f),
            new(new(3), new(4), 1f, 100f),
            new(new(1), new(2), 0f, 100f),
            new(new(1), new(2), 1f, -1f)
        ];
        Assert.Equal(3.25f, tracker.Measure(points, new(1), .1f));
        Assert.True(ContactIntegrity.Ruptures(tracker.Measure(points, new(1), .1f), 3f));
        Assert.Equal(100f, tracker.Measure(points, new(3), .1f));
    }

    [Fact]
    public void DifferentPairsAndDifferentSubstepsNeverAccumulateDamage()
    {
        var tracker = new ContactIntegrity();
        ContactImpulseSample[] points = [new(new(1), new(2), 1f, 2f), new(new(1), new(3), 1f, 2f)];
        Assert.Equal(2f, tracker.Measure(points, new(1), .1f));
        Assert.False(ContactIntegrity.Ruptures(tracker.Measure(points, new(1), .1f), 3f));
        Assert.Equal(0f, tracker.Measure([], new(1), .1f));
        Assert.Equal(2f, tracker.Measure([new(new(2), new(1), 1f, 2f)], new(1), .1f));
    }

    [Theory]
    [InlineData(2.99f, false)]
    [InlineData(3f, false)]
    [InlineData(3.01f, true)]
    public void ThresholdIsStrictWithoutHiddenTolerance(float impulse, bool expected) =>
        Assert.Equal(expected, ContactIntegrity.Ruptures(impulse, 3f));

    [Fact]
    public void RestingSupportAndApproachAtTheThresholdDoNotQualify()
    {
        var tracker = new ContactIntegrity();
        ContactImpulseSample[] points = [new(new(1), new(2), 0f, 10f), new(new(1), new(2), .1f, 10f)];
        Assert.Equal(0f, tracker.Measure(points, new(1), .1f));
        Assert.Equal(4f, tracker.Measure([new(new(1), new(2), .2f, 4f)], new(1), .1f));
    }

    [Fact]
    public void MalformedSelectorsAndSamplesAreRefused()
    {
        var tracker = new ContactIntegrity();
        Assert.Throws<ArgumentException>(() => tracker.Measure([], default, .1f));
        Assert.Throws<ArgumentException>(() => tracker.Measure([new(new(1), new(1), 1f, 1f)], new(1), .1f));
        Assert.Throws<ArgumentException>(() => tracker.Measure([new(new(1), new(2), float.NaN, 1f)], new(1), .1f));
        Assert.Throws<ArgumentException>(() => ContactIntegrity.Ruptures(1f, float.NaN));
    }
}
