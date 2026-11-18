using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements.Tests;

public sealed class MechanicalProductAdmissionTests
{
    private static BundleEntry Entry(MechanicalValue power, ParameterUnit forceUnit = ParameterUnit.Newton,
        float minimumForce = 1f, float maximumForce = 100f)
    {
        var source = BundleSchema.Entry(new Fixture
        {
            Parameters = [.. Fixture.SpringParameters,
                new("force", forceUnit, ParameterKind.Real, minimumForce, maximumForce, minimumForce, []),
                new("speed", ParameterUnit.MetrePerSecond, ParameterKind.Real, .25f, 4f, 1.5f, [])],
            Ports = [Fixture.PowerIn],
            Sliders = [Fixture.Slider() with { Drive = Fixture.Drive() with { MaxPower = power } }]
        });
        return source with
        {
            Electrical = new([new("motor", ElectricalRole.Consumer, 12f, 1f, 0f, 400f)], new(
                [new SupplyConsumerDescriptor(new("motor"), [new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))])], [],
                [new(PortId.PowerIn, new(new("motor"), SupplyTerminal.PowerIn))]))
        };
    }

    private static MechanicalValue Power(bool reverse = false)
    {
        var force = MechanicalOperand.FromParameter("force", ParameterUnit.Newton);
        var speed = MechanicalOperand.FromParameter("speed", ParameterUnit.MetrePerSecond);
        return reverse ? MechanicalValue.Multiply(speed, force) : MechanicalValue.Multiply(force, speed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForceTimesSpeedIsAdmittedAndDecodedWithItsUnits(bool reverse)
    {
        var entry = Entry(Power(reverse));
        Assert.Empty(BundleValidator.Validate([new("fixture", entry)]));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        var power = Assert.Single(decoded.Mechanical!.Sliders).Drive!.Value.MaxPower;
        Assert.Equal(.25f, power.Evaluate(new Dictionary<string, float> { ["force"] = 1f, ["speed"] = .25f }));
        Assert.Equal(400f, power.Evaluate(new Dictionary<string, float> { ["force"] = 100f, ["speed"] = 4f }));
        Assert.Equal(60f, power.Evaluate(new Dictionary<string, float> { ["force"] = 40f, ["speed"] = 1.5f }));
    }

    [Fact]
    public void WrongUnitsNegativeDomainsAndOverflowAreRefusedBeforeInstallation()
    {
        foreach (var entry in new[]
        {
            Entry(Power(), ParameterUnit.Metre),
            Entry(Power(), minimumForce: -1f),
            Entry(Power(), maximumForce: float.MaxValue),
            Entry(MechanicalValue.Multiply(MechanicalOperand.FromParameter("absent", ParameterUnit.Newton),
                MechanicalOperand.Fixed(1f, ParameterUnit.MetrePerSecond))),
            Entry(MechanicalValue.Multiply(MechanicalOperand.Fixed(2f, ParameterUnit.Metre),
                MechanicalOperand.Fixed(3f, ParameterUnit.Metre)))
        })
            Assert.Contains(BundleValidator.Validate([new("fixture", entry)]),
                error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains("Drive.MaxPower", StringComparison.Ordinal));
    }

    [Fact]
    public void SocketPresentationRoundTripsAndMissingOrUnknownValuesRefuse()
    {
        var entry = Entry(Power());
        var port = entry.Ports[0] with { Presentation = new("Advance carriage", SocketGlyph.Extend) };
        entry = entry with { Ports = [port] };
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        Assert.Equal(port.Presentation, Assert.Single(decoded.Ports).Presentation);
        foreach (var presentation in new[] { default(SocketPresentation), new(" ", SocketGlyph.Text), new("Advance", (SocketGlyph)255) })
            Assert.NotEmpty(BundleValidator.Validate([new("fixture", entry with { Ports = [port with { Presentation = presentation }] })]));
    }

    [Theory]
    [InlineData(-1f, -.1f, false)]
    [InlineData(-1f, 0f, true)]
    [InlineData(0f, 1f, true)]
    [InlineData(-1f, 1f, true)]
    public void SliderTravelIncludesItsAuthoredRestFrame(float lower, float upper, bool accepted)
    {
        var entry = Entry(Power());
        var slider = entry.Mechanical!.Sliders[0] with
        {
            LowerLimit = MechanicalValue.Fixed(lower), UpperLimit = MechanicalValue.Fixed(upper),
            Compression = MechanicalValue.Fixed(upper < 0f ? .5f : 0f), Drive = null
        };
        entry = entry with { Mechanical = entry.Mechanical with { Sliders = [slider] }, Electrical = null, Ports = [] };
        var errors = BundleValidator.Validate([new("fixture", entry)]);
        if (accepted) Assert.Empty(errors);
        else Assert.Contains(errors, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains(".Travel", StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultValidTravelRefusesANegativeOnlyParameterCorner()
    {
        var entry = Entry(Power());
        var slider = entry.Mechanical!.Sliders[0] with
        {
            LowerLimit = MechanicalValue.Fixed(-1f), UpperLimit = MechanicalValue.FromParameter("end"),
            Compression = MechanicalValue.Fixed(.5f), Drive = null
        };
        entry = entry with
        {
            Metadata = entry.Metadata with { Parameters = [.. entry.Metadata.Parameters,
                new("end", ParameterUnit.Metre, ParameterKind.Real, -.1f, 1f, 1f, [])] },
            Mechanical = entry.Mechanical with { Sliders = [slider] }, Electrical = null, Ports = []
        };
        Assert.Contains(BundleValidator.Validate([new("fixture", entry)]),
            e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains(".Travel", StringComparison.Ordinal));
    }

    [Fact]
    public void TravelAndTargetShareTheSameParameterAtEveryDomainCorner()
    {
        var entry = Entry(Power());
        var slider = entry.Mechanical!.Sliders[0] with
        {
            LowerLimit = MechanicalValue.Fixed(0f),
            UpperLimit = MechanicalValue.FromParameter("stroke"),
            Compression = MechanicalValue.Fixed(0f),
            Drive = entry.Mechanical.Sliders[0].Drive!.Value with { Target = MechanicalValue.FromParameter("stroke") }
        };
        entry = entry with
        {
            Metadata = entry.Metadata with { Parameters = [.. entry.Metadata.Parameters,
                new("stroke", ParameterUnit.Metre, ParameterKind.Real, .25f, 3f, 2.4f, [])] },
            Mechanical = entry.Mechanical with { Sliders = [slider] }
        };
        Assert.Empty(BundleValidator.Validate([new("fixture", entry)]));
        foreach (var invalid in new[] { slider with { LowerLimit = MechanicalValue.Fixed(.5f) },
            slider with { UpperLimit = Power() }, slider with { Drive = slider.Drive!.Value with { Target = MechanicalValue.Fixed(3f) } } })
            Assert.Contains(BundleValidator.Validate([new("fixture", entry with { Mechanical = entry.Mechanical with { Sliders = [invalid] } })]),
                error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains(".Travel", StringComparison.Ordinal));
    }
}
