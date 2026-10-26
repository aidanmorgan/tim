using System.Numerics;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Tests;

public sealed class PhysicalToleranceTests
{
    [Theory]
    [InlineData(.01f, .000005f)]
    [InlineData(.2f, .000020f)]
    [InlineData(1f, .000050f)]
    [InlineData(.05f, .000005f)]
    [InlineData(.5f, .000050f)]
    public void RestingAllowanceUsesLiteralPhysicalScales(float size, float expected)
    {
        // Multiplying two f32 literals may differ by one representable step from the decimal literal.
        var allowance = PhysicalTolerance.RestingDisplacementMetres(size);
        Assert.InRange(allowance, MathF.BitDecrement(expected), MathF.BitIncrement(expected));
        Assert.True(PhysicalTolerance.WithinRestingDisplacement(allowance, size));
        Assert.False(PhysicalTolerance.WithinRestingDisplacement(MathF.BitIncrement(allowance), size));
        Assert.True(PhysicalTolerance.WithinRestingDisplacement(-allowance, size));
        Assert.False(PhysicalTolerance.WithinRestingDisplacement(-MathF.BitIncrement(allowance), size));
    }

    [Fact]
    public void FloorAndCapTransitionsHaveTheDeclaredSlopeBetweenThem()
    {
        Assert.Equal(.000005f, PhysicalTolerance.RestingDisplacementMetres(.04f));
        Assert.InRange(PhysicalTolerance.RestingDisplacementMetres(.06f), .000005999999f, .000006000001f);
        Assert.InRange(PhysicalTolerance.RestingDisplacementMetres(.49f), .00004899999f, .00004900001f);
        Assert.Equal(.000050f, PhysicalTolerance.RestingDisplacementMetres(.51f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidPhysicalScaleIsExplicitlyRefused(float size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PhysicalTolerance.WithinRestingDisplacement(0f, size));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteOperandsNeverCompareEqual(float value)
    {
        Assert.False(PhysicalTolerance.WithinRestingDisplacement(value, .2f));
        Assert.False(PhysicalTolerance.CompatibleBoreRadii(value, .65f));
        Assert.False(PhysicalTolerance.CompatibleBoreRadii(.65f, value));
        Assert.False(PhysicalTolerance.CompatibleBoreRadii(value, value));
    }

    [Fact]
    public void BoreComparisonIncludesTheRepresentableThresholdAndRejectsItsNextNeighbour()
    {
        Assert.True(PhysicalTolerance.CompatibleBoreRadii(0f, .000001f));
        Assert.True(PhysicalTolerance.CompatibleBoreRadii(.000001f, 0f));
        Assert.False(PhysicalTolerance.CompatibleBoreRadii(0f, MathF.BitIncrement(.000001f)));
    }

    [Fact]
    public void ReturningToTheReferenceDoesNotEraseAnExcursion()
    {
        var observation = new RestingObservation(.2f, Vector3.Zero);
        observation.Observe(new(.000021f, 0f, 0f));
        observation.Observe(Vector3.Zero);
        Assert.Equal(.000021f, observation.PeakMetres);
        Assert.False(observation.IsResting);
    }

    [Fact]
    public void ObservationIncludesTheThresholdAndRejectsItsNextNeighbour()
    {
        var observation = new RestingObservation(1f, Vector3.Zero);
        observation.Observe(new(.000050f, 0f, 0f));
        Assert.True(observation.IsResting);
        observation.Observe(new(MathF.BitIncrement(.000050f), 0f, 0f));
        Assert.False(observation.IsResting);
    }

    [Fact]
    public void NeighbouringCoordinatesBracketThePhysicalResolutionLimit()
    {
        // At the 5 µm floor, allowance/8 = .625 µm.
        // Below 8 m both adjacent steps are 2^-21 m (.476837... µm);
        // at 8 m the upward step is 2^-20 m (.953674... µm).
        var inside = new RestingObservation(.01f, new(MathF.BitDecrement(8f), 0f, 0f));
        Assert.True(inside.Resolved);
        Assert.True(inside.IsResting);
        var outside = new RestingObservation(.01f, new(8f, 0f, 0f));
        Assert.False(outside.Resolved);
        Assert.False(outside.IsResting);
    }

    [Fact]
    public void VerticalMotionDoesNotCountAsHorizontalExcursion()
    {
        var reference = new Vector3(.125f, 0f, -.125f);
        var observation = new RestingObservation(.2f, reference);
        observation.Observe(reference + new Vector3(0f, .0001f, 0f));
        Assert.Equal(0f, observation.PeakMetres);
        Assert.True(observation.Resolved);
        Assert.True(observation.IsResting);
    }

    [Fact]
    public void NegativeDisplacementUsesTheSamePhysicalAllowance()
    {
        var reference = new Vector3(.125f, 0f, -.125f);
        var observation = new RestingObservation(.2f, reference);
        observation.Observe(reference + new Vector3(-.000015f, 0f, 0f));
        // Literal 15/30 µm distances, allowing .02 µm for the reference's f32 spacing.
        Assert.InRange(observation.PeakMetres, .00001498f, .00001502f);
        Assert.True(observation.Resolved);
        Assert.True(observation.IsResting);
        observation.Observe(reference + new Vector3(-.000030f, 0f, 0f));
        Assert.InRange(observation.PeakMetres, .00002998f, .00003002f);
        Assert.True(observation.Resolved);
        Assert.False(observation.IsResting);
    }

    [Fact]
    public void AHorizontalDiagonalOutsideTheRadiusIsNotResting()
    {
        // Each 15 µm component is inside 20 µm, but sqrt(15² + 15²) = 21.213... µm is outside.
        var observation = new RestingObservation(.2f, Vector3.Zero);
        observation.Observe(new(.000015f, 0f, .000015f));
        Assert.InRange(observation.PeakMetres, .00002121f, .00002122f);
        Assert.False(observation.IsResting);
    }

    [Fact]
    public void DisplacementIsMeasuredFromTheOriginalNonzeroReference()
    {
        var reference = new Vector3(.125f, 0f, -.125f);
        var observation = new RestingObservation(.2f, reference);
        observation.Observe(reference + new Vector3(0f, 0f, .000015f));
        // f32 spacing here is at most 2^-26 m; the 0.02 µm observation margin encloses it.
        Assert.InRange(observation.PeakMetres, .00001498f, .00001502f);
        Assert.True(observation.Resolved);
        Assert.True(observation.IsResting);
        observation.Observe(reference + new Vector3(0f, 0f, .000030f));
        // The second increment is only 15 µm, but displacement from the reference is 30 µm.
        Assert.InRange(observation.PeakMetres, .00002998f, .00003002f);
        Assert.True(observation.Resolved);
        Assert.False(observation.IsResting);
    }

    [Theory]
    [InlineData(16f, 0f)]
    [InlineData(-16f, 0f)]
    [InlineData(0f, 16f)]
    [InlineData(0f, -16f)]
    public void ObservationRejectsUnresolvableCoordinatesEvenWhenTheyDoNotMove(float x, float z)
    {
        var observation = new RestingObservation(.01f, new(x, 0f, z));
        Assert.False(observation.Resolved);
        Assert.False(observation.IsResting);
    }

    [Theory]
    [InlineData(16f, 0f)]
    [InlineData(-16f, 0f)]
    [InlineData(0f, 16f)]
    [InlineData(0f, -16f)]
    public void LaterResolutionFailurePersistsAfterResolvableObservations(float x, float z)
    {
        var observation = new RestingObservation(.01f, Vector3.Zero);
        Assert.True(observation.Resolved);
        observation.Observe(new(x, 0f, z));
        Assert.False(observation.Resolved);
        observation.Observe(Vector3.Zero);
        // Check resolution itself: peak excursion alone would also reject this window.
        Assert.False(observation.Resolved);
        Assert.False(observation.IsResting);
    }
}

/// <summary>Every tick is measured against the original settled reference, never the preceding tick.</summary>
internal sealed class RestingObservation
{
    private readonly float _nominalSizeMetres;
    private readonly Vector3 _reference;
    public float AllowanceMetres { get; }
    public float PeakMetres { get; private set; }
    public bool Resolved { get; private set; } = true;
    public bool IsResting => Resolved && PhysicalTolerance.WithinRestingDisplacement(PeakMetres, _nominalSizeMetres);

    public RestingObservation(float nominalSizeMetres, Vector3 reference)
    {
        _nominalSizeMetres = nominalSizeMetres;
        AllowanceMetres = PhysicalTolerance.RestingDisplacementMetres(nominalSizeMetres);
        _reference = reference;
        Observe(reference);
    }

    public void Observe(Vector3 position)
    {
        Resolved &= Resolvable(position.X) && Resolvable(position.Z);
        var offset = new Vector2(position.X - _reference.X, position.Z - _reference.Z);
        PeakMetres = MathF.Max(PeakMetres, offset.Length());
    }

    private bool Resolvable(float component) =>
        float.IsFinite(component) &&
        MathF.Max(MathF.BitIncrement(component) - component, component - MathF.BitDecrement(component)) <= AllowanceMetres / 8f;
}
