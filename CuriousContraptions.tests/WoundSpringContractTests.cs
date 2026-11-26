using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WoundSpringContractTests
{
    private static BundleEntry Entry => TestBundle.Entry(new("wound_spring"));

    [Theory]
    [InlineData(.2f, 40f, .025f)]
    [InlineData(.8f, 240f, .2f)]
    public void LiteralParameterCornersCompileExactTypedCoordinateContract(float stroke, float stiffness, float lead)
    {
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(1),
            0, 3, 0, 0, 0, 0, 1, new([new(new("stroke"), stroke), new(new("stiffness"), stiffness), new(new("winding_lead"), lead)]));
        var scene = CompiledSceneFixture.Compile(new(new(2), WorkshopCadenceSettings.Default(), new(instance)));
        var slider = Assert.Single(scene.Physics.Prismatics.ToArray());
        var relation = Assert.Single(scene.Physics.Transmissions.ToArray());
        Assert.Equal(-stroke, slider.Lower);
        Assert.Equal(0f, slider.Upper);
        Assert.Equal(stiffness, slider.Stiffness);
        Assert.Equal(lead, relation.Ratio);
        Assert.Equal(new ConstraintCoordinateReference(slider.Id), relation.Driven);
        Assert.Equal(TransmissionConstraintKind.NonPositiveVelocity, relation.ConstraintKind);
        Assert.NotNull(relation.Retention);
    }

    [Fact]
    public void MissingTransmissionConstraintKindIsRefusedAtSerializationBoundary()
    {
        var descriptor = Entry.Mechanical!.Transmissions[0];
        var json = System.Text.Json.JsonSerializer.SerializeToNode(descriptor)!.AsObject();
        Assert.True(json.Remove("ConstraintKind"));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<MechanicalTransmissionDescriptor>(json.ToJsonString()));
    }

    [Fact]
    public void NegationPreservesCorrelatedParameterCornersAndCanonicalRoundTrip()
    {
        var entry = Entry;
        var slider = entry.Mechanical!.Sliders[0] with { Compression = MechanicalValue.FromParameter("stroke") };
        var correlated = entry with { Metadata = entry.Metadata with { Id = "unfamiliar_compressive_store" },
            Mechanical = entry.Mechanical with { Sliders = [slider] } };
        Assert.Empty(BundleValidator.Validate([new("correlated", correlated)]));
        Assert.True(MechanicalValueDomain.TryRange(slider.LowerLimit, entry.Metadata.Parameters, ParameterUnit.Metre, out var low, out var high));
        Assert.Equal(-.8f, low); Assert.Equal(-.2f, high);
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([correlated])).Elements);
        Assert.Equal(slider.LowerLimit, decoded.Mechanical!.Sliders[0].LowerLimit);
        var independent = correlated with { Mechanical = correlated.Mechanical! with
            { Sliders = [slider with { Compression = MechanicalValue.Fixed(.8f) }] } };
        Assert.Contains(BundleValidator.Validate([new("independent", independent)]), error => error.Code == BundleErrorCode.OutOfRange);
    }

    [Fact]
    public void MalformedNegationAndUnsupportedUnitsAreRefused()
    {
        var entry = Entry;
        var values = new Dictionary<string, float> { ["stroke"] = .5f };
        var product = MechanicalValue.Multiply(
            MechanicalOperand.Fixed(2f, ParameterUnit.Newton), MechanicalOperand.Fixed(3f, ParameterUnit.MetrePerSecond)).Product;
        foreach (var value in new[] {
            new MechanicalValue(MechanicalValueSource.NegatedParameter, 1f, "stroke"),
            new MechanicalValue(MechanicalValueSource.NegatedParameter, 0f, null),
            new MechanicalValue(MechanicalValueSource.NegatedParameter, 0f, "missing"),
            new MechanicalValue(MechanicalValueSource.NegatedParameter, 0f, "stroke", product),
            new MechanicalValue((MechanicalValueSource)255, 0f, "stroke") })
        {
            Assert.Throws<ArgumentException>(() => value.Evaluate(values));
            Assert.False(MechanicalValueDomain.TryRange(value, entry.Metadata.Parameters, ParameterUnit.Metre, out _, out _));
        }
        Assert.Throws<ArgumentException>(() => MechanicalValue.NegatedParameter("stroke").Evaluate(
            new Dictionary<string, float> { ["stroke"] = float.NaN }));
        Assert.False(MechanicalValueDomain.TryRange(MechanicalValue.NegatedParameter("stroke"),
            entry.Metadata.Parameters, ParameterUnit.Second, out _, out _));
        var operand = new MechanicalOperand(MechanicalValueSource.NegatedParameter, ParameterUnit.Newton, 0f, "stiffness");
        Assert.False(MechanicalValueDomain.TryRange(MechanicalValue.Multiply(operand,
            MechanicalOperand.Fixed(1f, ParameterUnit.MetrePerSecond)), entry.Metadata.Parameters, ParameterUnit.Watt, out _, out _));
    }

    [Fact]
    public void NegatedTargetCannotHideBehindAnotherDirectRunControlLane()
    {
        var entry = TestBundle.Entry(new("motor"));
        var hinge = entry.Mechanical!.Hinges[0];
        var transformed = hinge with { Drive = hinge.Drive!.Value with { TargetSpeed = MechanicalValue.NegatedParameter("speed") } };
        var probe = entry with { Mechanical = entry.Mechanical with { Hinges = [hinge, transformed],
            MovingBodyColliders = [entry.Mechanical.MovingBodyColliders[0], entry.Mechanical.MovingBodyColliders[0]] } };
        Assert.Single(RunControlBindings.Lanes(probe, "speed"));
        Assert.True(RunControlBindings.IsBoundToNonLane(probe, "speed"));
    }

    [Fact]
    public void RetentionRefusesIncompatibleCoordinateAndWriterContracts()
    {
        var entry = Entry;
        var mechanical = entry.Mechanical!;
        var slider = mechanical.Sliders[0];
        foreach (var invalid in new[] {
            mechanical with { Sliders = [slider with { UpperLimit = MechanicalValue.Fixed(.1f) }] },
            mechanical with { Sliders = [slider with { UpperLimit = MechanicalValue.Fixed(-0f) }] },
            mechanical with { Sliders = [slider with { LowerLimit = MechanicalValue.Fixed(-PhysicalTolerance.ActuatorStopSlopMetres) }] },
            mechanical with { Sliders = [slider with { Compression = MechanicalValue.Fixed(-.1f) }] },
            mechanical with { Retentions = [mechanical.Retentions[0], mechanical.Retentions[0] with { Id = new(2) }] },
            mechanical with { Transmissions = [mechanical.Transmissions[0] with { ConstraintKind = TransmissionConstraintKind.Bilateral }] },
            mechanical with { Retentions = [mechanical.Retentions[0] with { Coordinate = new(CoordinateKind.Revolute, 0) }] } })
            Assert.Contains(BundleValidator.Validate([new("invalid", entry with { Mechanical = invalid })]),
                error => error.Code == BundleErrorCode.OutOfRange);
        // Sole writer: a drive inside the retained travel is refused by the retention contract itself, not by its travel.
        var drive = new PrismaticDriveDescriptor(MechanicalValue.Fixed(-.1f), MechanicalValue.Fixed(.5f), MechanicalValue.Fixed(2f),
            MechanicalValue.Fixed(50f), MechanicalValue.Fixed(20f));
        Assert.Contains(BundleValidator.Validate([new("driven", entry with { Mechanical = mechanical with { Sliders = [slider with { Drive = drive }] } })]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.EndsWith(".Retentions.Coordinate", StringComparison.Ordinal));
        // The retention law itself needs positive stiffness (½kq²); the ordinary slider admits zero.
        Assert.Contains(BundleValidator.Validate([new("stiffless", entry with
                { Mechanical = mechanical with { Sliders = [slider with { Stiffness = MechanicalValue.Fixed(0f) }] } })]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.EndsWith(".Retentions.Coordinate", StringComparison.Ordinal));
        // Any finite non-negative damping, signed zero included, belongs to the ordinary slider domain (Pass 3 F7 amendment B).
        foreach (var damping in new[] { -0f, .1f })
            Assert.Empty(BundleValidator.Validate([new("damped", entry with
                { Mechanical = mechanical with { Sliders = [slider with { Damping = MechanicalValue.Fixed(damping) }] } })]));
        foreach (var (lower, upper, damping, driven) in new[]
        {
            (-.8f, -0f, 0f, false),
            (-PhysicalTolerance.ActuatorStopSlopMetres, 0f, 0f, false),
            (-.8f, 0f, -1f, false),
            (-.8f, 0f, float.NaN, false),
            (-.8f, 0f, 0f, true)
        })
            Assert.Throws<ArgumentException>(() => RetainedScene(lower, upper, damping, driven).Admit());
        foreach (var damping in new[] { -0f, .5f })
            _ = RetainedScene(-.8f, 0f, damping, false).Admit();
        // Control: the same drive is admitted on that slider when no retention is declared.
        _ = RetainedScene(-.8f, 0f, 0f, true, retain: false).Admit();
    }

    private static LawScene RetainedScene(float lower, float upper, float damping, bool driven, bool retain = true)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var head = scene.Box(new(0, 5, 0), new(.1f), .5f, LawMaterial.Domino, gravity: 0);
        var shaftBody = scene.Box(new(2, 5, 0), new(.1f), .25f, LawMaterial.Domino, gravity: 0);
        var identity = new ConstraintFrame(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity);
        var s = scene.Prismatic(frame, head, identity, identity, lower, upper, 160f, damping, ConnectedCollision.Disabled);
        // A powered drive on the retained slider would be a second writer of its coordinate.
        if (driven) scene.Drive(s, -.1f, .5f, 2f, 50f, 20f);
        var r = scene.Revolute(frame, shaftBody, new(new(2, 0, 0), System.Numerics.Quaternion.Identity),
            identity, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        if (retain) scene.Retain(frame, r, s, .1f);
        return scene;
    }

    [Fact]
    public void OrdinarySliderAndHingeValidatorsOwnSpringUnitsAndNonNegativeRanges()
    {
        var entry = Entry;
        var mechanical = entry.Mechanical!;
        var slider = mechanical.Sliders[0];
        var hinge = mechanical.Hinges[0];
        BundleEntry With(ParameterDescriptor parameter, PrismaticSliderDescriptor? s = null, RevoluteHingeDescriptor? h = null) => entry with
        {
            Metadata = entry.Metadata with { Parameters = [.. entry.Metadata.Parameters, parameter] },
            Mechanical = mechanical with { Sliders = [s ?? slider], Hinges = [h ?? hinge] }
        };
        static ParameterDescriptor Real(string key, ParameterUnit unit, float minimum, float maximum) =>
            new(key, unit, ParameterKind.Real, minimum, maximum, maximum, []);
        void Refused(BundleEntry candidate, string field, BundleErrorCode code = BundleErrorCode.OutOfRange) =>
            Assert.Contains(BundleValidator.Validate([new("refused", candidate)]),
                error => error.Code == code && error.Detail.Contains(field + ": ", StringComparison.Ordinal));

        // A damping parameter in N·s/m with a non-negative range is admitted; its unit and minimum are checked like a constant.
        Assert.Empty(BundleValidator.Validate([new("damped", With(Real("damping", ParameterUnit.NewtonSecondPerMetre, 0f, 2f),
            slider with { Damping = MechanicalValue.FromParameter("damping") }))]));
        Refused(With(Real("damping", ParameterUnit.NewtonSecondPerMetre, -.5f, 2f),
            slider with { Damping = MechanicalValue.FromParameter("damping") }), ".Damping");
        Refused(With(Real("damping", ParameterUnit.NewtonPerMetre, 0f, 2f),
            slider with { Damping = MechanicalValue.FromParameter("damping") }), ".Damping");
        Refused(With(Real("spare", ParameterUnit.Newton, 0f, 1f), slider with { Damping = MechanicalValue.Fixed(-.1f) }), ".Damping");
        Refused(With(Real("spare", ParameterUnit.Newton, 0f, 1f), slider with { Damping = MechanicalValue.Fixed(float.NaN) }), ".Damping.Constant",
            BundleErrorCode.NonFinite);
        var wrongUnitStiffness = With(Real("rate", ParameterUnit.NewtonSecondPerMetre, 40f, 240f),
            slider with { Stiffness = MechanicalValue.FromParameter("rate") });
        Refused(wrongUnitStiffness, ".Stiffness");
        // The unit is the ordinary slider's rule alone: the retention adds no second refusal for it.
        Assert.DoesNotContain(BundleValidator.Validate([new("refused", wrongUnitStiffness)]),
            error => error.Detail.EndsWith(".Retentions.Coordinate", StringComparison.Ordinal));

        // No rotational stiffness/damping unit exists: a hinge's own spring values are constants only.
        Refused(With(Real("twist", ParameterUnit.NewtonMetre, 0f, 1f), h: hinge with { Stiffness = MechanicalValue.FromParameter("twist") }), ".Stiffness");
        Refused(With(Real("twist", ParameterUnit.NewtonMetre, 0f, 1f), h: hinge with { Damping = MechanicalValue.FromParameter("twist") }), ".Damping");
        Assert.Empty(BundleValidator.Validate([new("friction", With(Real("friction", ParameterUnit.NewtonMetre, 0f, 1f),
            h: hinge with { FrictionTorque = MechanicalValue.FromParameter("friction") }))]));
        Refused(With(Real("friction", ParameterUnit.Newton, 0f, 1f), h: hinge with { FrictionTorque = MechanicalValue.FromParameter("friction") }), ".FrictionTorque");
        Refused(With(Real("friction", ParameterUnit.NewtonMetre, -1f, 1f), h: hinge with { FrictionTorque = MechanicalValue.FromParameter("friction") }), ".FrictionTorque");

        // Controls: the shipped constant hinges and Springboard's N/m and N·s/m slider parameters remain admitted.
        foreach (var part in new[] { "spring", "motor", "clutch", "impact_lever", "reverse_transmission", "wound_spring" })
            Assert.Empty(BundleValidator.Validate([new(part, TestBundle.Entry(new(part)))]));
    }

    // S11: a ratio or close duration carrying a product payload is refused, whatever its scalar source. S5: the one-connection-
    // per-role rule covers mechanical ports on prismatic coordinates too.
    [Fact]
    public void ScalarTransmissionValuesWithAProductPayloadAndPrismaticPortRolesAreRefused()
    {
        // A build-only seconds parameter with valid timed bounds, so a Parameter-sourced close duration is otherwise admissible.
        var closeTime = new ParameterDescriptor("close_time", ParameterUnit.Second, ParameterKind.Real, .1f, 1f, .5f, []);
        var entry = Entry with { Metadata = Entry.Metadata with { Parameters = [.. Entry.Metadata.Parameters, closeTime] } };
        var mechanical = entry.Mechanical!;
        var relation = mechanical.Transmissions[0];
        Assert.Empty(BundleValidator.Validate([new("valid", entry)]));
        var product = MechanicalValue.Multiply(
            MechanicalOperand.Fixed(2f, ParameterUnit.Newton), MechanicalOperand.Fixed(3f, ParameterUnit.MetrePerSecond)).Product;
        IReadOnlyList<BundleError> Ratio(MechanicalValue value) => BundleValidator.Validate([new("ratio", entry with
            { Mechanical = mechanical with { Transmissions = [relation with { Ratio = value }] } })]);
        IReadOnlyList<BundleError> Close(MechanicalValue value) => BundleValidator.Validate([new("close", entry with
            { Mechanical = mechanical with { Transmissions = [relation with { CloseSeconds = value }] } })]);
        static bool At(BundleError error, string field) =>
            error.Code == BundleErrorCode.OutOfRange && error.Detail.EndsWith(".Transmissions." + field, StringComparison.Ordinal);
        // Constant .1 is a valid m/rad ratio and a valid timed duration; winding_lead (m/rad, positive range) is a valid ratio
        // parameter and close_time a valid duration parameter. Each control without a product is admitted on its field.
        var rows = new (MechanicalValueSource Source, float Constant, string? RatioParameter, string? CloseParameter)[]
        {
            (MechanicalValueSource.Constant, .1f, null, null),
            (MechanicalValueSource.Parameter, 0f, "winding_lead", "close_time")
        };
        foreach (var row in rows)
        {
            var ratio = new MechanicalValue(row.Source, row.Constant, row.RatioParameter, null);
            var close = new MechanicalValue(row.Source, row.Constant, row.CloseParameter, null);
            Assert.DoesNotContain(Ratio(ratio), error => At(error, "Ratio"));
            Assert.DoesNotContain(Close(close), error => At(error, "CloseSeconds"));
            Assert.Contains(Ratio(ratio with { Product = product }), error => At(error, "Ratio"));
            Assert.Contains(Close(close with { Product = product }), error => At(error, "CloseSeconds"));
        }
        var input = entry.Ports.Single(port => port.Role == PortRole.DriveIn);
        var prismaticInput = entry with { Ports = [.. entry.Ports, input with { Coordinate = new(CoordinateKind.Prismatic, 0) }] };
        Assert.Contains(BundleValidator.Validate([new("prismatic_port", prismaticInput)]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.EndsWith(".Transmissions.Role", StringComparison.Ordinal));
    }

    [Fact]
    public void PrismaticToRevoluteRatioUnitsAndRetentionPresentationAndTransmissionBoundariesAreValidated()
    {
        var entry = Entry;
        var mechanical = entry.Mechanical!;
        var parameters = entry.Metadata.Parameters.Append(
            new("rack_pitch", ParameterUnit.RadianPerMetre, ParameterKind.Real, 1f, 20f, 10f, [])).ToArray();
        var hinge = mechanical.Hinges[0];
        var rackEntry = entry with
        {
            Metadata = entry.Metadata with { Id = "unfamiliar_rack_pinion", Parameters = parameters },
            Footprint = new(4, 3, 1, 2),
            Presentation = entry.Presentation with
            {
                Bodies = [.. entry.Presentation.Bodies, new("Visual/Coil", new(3), System.Numerics.Vector3.Zero)]
            },
            Mechanical = mechanical with
            {
                Hinges = [hinge, hinge with { BaseFrame = new(new(.62f, -.55f, .5f), System.Numerics.Quaternion.Identity) }],
                MovingBodyColliders = [mechanical.MovingBodyColliders[0], mechanical.MovingBodyColliders[1], mechanical.MovingBodyColliders[1]],
                Transmissions =
                [
                    mechanical.Transmissions[0],
                    new(new(2), new(CoordinateKind.Prismatic, 0), new(CoordinateKind.Revolute, 1),
                        MechanicalValue.FromParameter("rack_pitch"), TransmissionConstraintKind.Bilateral)
                ]
            }
        };
        Assert.Empty(BundleValidator.Validate([new("valid_rack", rackEntry)]));
        var wrongUnit = rackEntry with
        {
            Mechanical = rackEntry.Mechanical! with
            {
                Transmissions =
                [
                    mechanical.Transmissions[0],
                    rackEntry.Mechanical.Transmissions[1] with { Ratio = MechanicalValue.FromParameter("winding_lead") }
                ]
            }
        };
        Assert.Contains(BundleValidator.Validate([new("wrong_unit", wrongUnit)]),
            e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Transmissions.Ratio", StringComparison.Ordinal));
        var outOfRangeCoordinate = rackEntry with
        {
            Mechanical = rackEntry.Mechanical! with
            {
                Transmissions =
                [
                    mechanical.Transmissions[0],
                    rackEntry.Mechanical.Transmissions[1] with { Driving = new(CoordinateKind.Prismatic, mechanical.Sliders.Count) }
                ]
            }
        };
        Assert.Contains(BundleValidator.Validate([new("oob_coord", outOfRangeCoordinate)]),
            e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Transmissions.Coordinates", StringComparison.Ordinal));

        var binding = entry.Presentation.Bindings[0];
        foreach (var invalidBinding in new[]
        {
            binding with { Retention = null },
            binding with { Retention = new(0) },
            binding with { Retention = new(99) },
            new PresentationBinding("Visual/Latch", PresentationProperty.LocalRotationZ, PresentationDriver.CommittedRevoluteAngle,
                PresentationValueMapping.Identity, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                Actuator: new(CoordinateKind.Revolute, 0), Retention: new(1))
        })
        {
            var candidate = entry with { Presentation = entry.Presentation with { Bindings = [invalidBinding] } };
            Assert.Contains(BundleValidator.Validate([new("pres_retention", candidate)]),
                e => e.Code == BundleErrorCode.OutOfRange && e.Detail.Contains("Presentation.Retention", StringComparison.Ordinal));
        }

        Assert.Contains(BundleValidator.Validate([new("zero_id", entry with
            { Mechanical = mechanical with { Retentions = [mechanical.Retentions[0] with { Id = new(0) }] } })]),
            e => e.Code == BundleErrorCode.DuplicateId && e.Detail.EndsWith(".Retentions", StringComparison.Ordinal));
        Assert.Contains(BundleValidator.Validate([new("dup_id", entry with
            { Mechanical = mechanical with { Retentions = [mechanical.Retentions[0], mechanical.Retentions[0]] } })]),
            e => e.Code == BundleErrorCode.DuplicateId && e.Detail.EndsWith(".Retentions", StringComparison.Ordinal));
        Assert.Contains(BundleValidator.Validate([new("missing_tx", entry with
            { Mechanical = mechanical with { Retentions = [mechanical.Retentions[0] with { WindingTransmission = new(99) }] } })]),
            e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Retentions.WindingTransmission", StringComparison.Ordinal));
        var secondSlider = mechanical.Sliders[0];
        Assert.Contains(BundleValidator.Validate([new("dup_tx", entry with
        {
            Footprint = new(4, 3, 2, 1),
            Presentation = entry.Presentation with
            {
                Bodies = [.. entry.Presentation.Bodies, new("Visual/Coil", new(3), System.Numerics.Vector3.Zero)]
            },
            Mechanical = mechanical with
            {
                Sliders = [mechanical.Sliders[0], secondSlider],
                MovingBodyColliders = [mechanical.MovingBodyColliders[0], mechanical.MovingBodyColliders[0], mechanical.MovingBodyColliders[1]],
                Retentions =
                [
                    mechanical.Retentions[0],
                    new(new(2), new(CoordinateKind.Prismatic, 1), mechanical.Retentions[0].WindingTransmission)
                ]
            }
        })]), e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Retentions.WindingTransmission", StringComparison.Ordinal));
        Assert.Contains(BundleValidator.Validate([new("timed_tx", entry with
        {
            Mechanical = mechanical with
            {
                Transmissions = [mechanical.Transmissions[0] with { CloseSeconds = MechanicalValue.Fixed(.1f) }]
            }
        })]), e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Retentions.WindingTransmission", StringComparison.Ordinal));
    }
}
