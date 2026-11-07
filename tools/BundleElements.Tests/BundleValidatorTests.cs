using CuriousContraptions.Abstractions.Mechanical;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.4 AC4: generation fails naming the package and the field (the spec's edge-case matrix).</summary>
public sealed class BundleValidatorTests
{

    [Fact]
    public void ResidenceCapabilityRejectsMalformedRegionsBindingsAndParticipationWithoutThrowing()
    {
        var entry = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [new("participation", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"])]
        });
        var residence = new Abstractions.Mechanical.ResidenceSensorDescriptor(
            new(-.66f, -.4f, -.66f), new(.66f, .45f, .66f),
            Abstractions.Mechanical.MechanicalValue.Fixed(1.5f),
            Abstractions.Mechanical.MechanicalValue.Fixed(.35f), "participation",
            Target: TargetSelector.AllPayloads,
            Position: Vector3.Zero,
            Rotation: Quaternion.Identity,
            UpperMargin: Abstractions.Mechanical.MechanicalValue.Fixed(.02f));
        var guide = new Abstractions.Mechanical.PlanarGuideDescriptor(
            new(-1.1f, .5f, -1.1f), new(1.1f, 1.5f, 1.1f), .5f,
            Abstractions.Mechanical.MechanicalValue.Fixed(0f), Abstractions.Mechanical.MechanicalValue.Fixed(0f),
            Target: TargetSelector.AllPayloads,
            Position: Vector3.Zero,
            Rotation: Quaternion.Identity);

        IReadOnlyList<Bundle.BundleError> CheckResidence(Abstractions.Mechanical.ResidenceSensorDescriptor value) =>
            Bundle.BundleValidator.Validate([new("Fixture", entry with { Mechanical = entry.Mechanical! with { Residence = value } })]);
        IReadOnlyList<Bundle.BundleError> CheckGuide(Abstractions.Mechanical.PlanarGuideDescriptor value) =>
            Bundle.BundleValidator.Validate([new("Fixture", entry with { Mechanical = entry.Mechanical! with { PlanarGuide = value } })]);

        Assert.Empty(CheckResidence(residence));
        Assert.Empty(CheckGuide(guide));

        foreach (var invalid in new[]
        {
            residence with { ParticipationParameter = null! },
            residence with { ParticipationParameter = "" },
            residence with { ParticipationParameter = "unknown" },
            residence with { Minimum = new(float.NaN, 0f, 0f) },
            residence with { Maximum = residence.Minimum },
            residence with { UpperMargin = Abstractions.Mechanical.MechanicalValue.Fixed(-1f) },
            residence with { SpeedLimit = Abstractions.Mechanical.MechanicalValue.Fixed(0f) },
            residence with { Dwell = Abstractions.Mechanical.MechanicalValue.FromParameter("unknown") },
            residence with { Position = new(float.NaN, 0f, 0f) },
            residence with { Rotation = new(1f, 1f, 1f, 1f) },
            residence with { Target = new((TargetSelectorKind)99) }
        }) Assert.NotEmpty(CheckResidence(invalid));

        foreach (var invalid in new[]
        {
            guide with { Minimum = new(float.PositiveInfinity, 0f, 0f) },
            guide with { Maximum = guide.Minimum },
            guide with { SupportHeight = 17f },
            guide with { SupportMargin = Abstractions.Mechanical.MechanicalValue.Fixed(1.01f) },
            guide with { MaximumAcceleration = Abstractions.Mechanical.MechanicalValue.Fixed(12.01f) },
            guide with { Position = new(float.NaN, 0f, 0f) },
            guide with { Rotation = new(1f, 1f, 1f, 1f) },
            guide with { Target = new((TargetSelectorKind)99) }
        }) Assert.NotEmpty(CheckGuide(invalid));

        Assert.NotEmpty(Bundle.BundleValidator.Validate([new("Fixture", entry with
        {
            Metadata = entry.Metadata with { Parameters = [] },
            Mechanical = entry.Mechanical! with { Residence = residence }
        })]));
    }

    // The finite contact-work store (C-Bumper+Spring): one Real m/s strength parameter whose range keeps the reference-mass preload at
    // or below 200 J, a positive reference mass, a 0–64 m/s threshold, a 9–14400-step cooldown and a static, slider-free owner.
    [Fact]
    public void ContactWorkCapabilityRequiresOneTypedStrengthWithinTheTwoHundredJouleStore()
    {
        var strength = new ParameterDescriptor("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, 0f, 20f, 8f, [], ParameterEditorPolicy.Clamp);
        var entry = Bundle.BundleSchema.Entry(new Fixture { Parameters = [strength] });
        var work = new Abstractions.Mechanical.ContactWorkDescriptor(new("work"), "strength", 1f, .05f, 72);
        Bundle.BundleEntry With(Abstractions.Mechanical.ContactWorkDescriptor value, params ParameterDescriptor[] parameters) =>
            entry with { Metadata = entry.Metadata with { Parameters = parameters }, Mechanical = entry.Mechanical! with { ContactWork = value } };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Fixture", candidate)]);
        Assert.Empty(Check(With(work, strength)));
        // Boundaries: exactly 200 J (1 kg at 20 m/s; 4 kg at 10 m/s), the 9- and 14400-step cooldowns and the 0/64 m/s thresholds.
        Assert.Empty(Check(With(work with { ReferenceMass = 4f }, strength with { Maximum = 10f, Default = 8f })));
        foreach (var edge in new[] { work with { CooldownPhysicalSteps = 9 }, work with { CooldownPhysicalSteps = 14400 },
                     work with { Threshold = 0f }, work with { Threshold = 64f } })
            Assert.Empty(Check(With(edge, strength)));
        foreach (var (name, candidate) in new[]
        {
            ("missing parameter", With(work)),
            ("unknown key", With(work with { StrengthParameter = "unknown" }, strength)),
            ("duplicate key", With(work, strength, strength)),
            ("wrong unit", With(work, strength with { Unit = ParameterUnit.Metre })),
            ("not Real", With(work, strength with { Kind = ParameterKind.Choice, Options = ["A", "B"], Minimum = 0f, Maximum = 1f, Default = 0f })),
            ("negative minimum", With(work, strength with { Minimum = -1f })),
            ("maximum above 20 m/s", With(work, strength with { Maximum = 21f })),
            ("over 200 J", With(work with { ReferenceMass = 2f }, strength)),
            ("zero reference mass", With(work with { ReferenceMass = 0f }, strength)),
            ("nonfinite reference mass", With(work with { ReferenceMass = float.NaN }, strength)),
            ("reference mass above 100 kg", With(work with { ReferenceMass = 101f }, strength)),
            ("negative threshold", With(work with { Threshold = -.01f }, strength)),
            ("threshold above 64 m/s", With(work with { Threshold = 64.5f }, strength)),
            ("nonfinite threshold", With(work with { Threshold = float.PositiveInfinity }, strength)),
            ("short cooldown", With(work with { CooldownPhysicalSteps = 8 }, strength)),
            ("long cooldown", With(work with { CooldownPhysicalSteps = 14401 }, strength)),
            ("dynamic owner", With(work, strength) with { Mechanical = entry.Mechanical! with
                { ContactWork = work, KinematicMode = Abstractions.Mechanical.KinematicMode.Dynamic } }),
            ("slider owner", With(work, [.. Fixture.SpringParameters, strength]) with { Mechanical = entry.Mechanical! with
                { ContactWork = work, Sliders = [Fixture.Slider()] } })
        }) Assert.True(Check(candidate).Any(error => error.Detail.Contains(".ContactWork.", StringComparison.Ordinal)), name);
    }

    // Story 10.5: a compliant surface admits exactly the runtime declaration's domain (CompliantSurfaceDeclaration.Validate): half
    // extents and stroke 1/1024–16 m, rest height within ±16 m, arrival depth finite in (0, stroke/2), tension 120–1200 N/m, damping
    // ratio 0.08–0.8, on a static owner.
    [Fact]
    public void CompliantSurfaceCapabilityAdmitsTheRuntimeDomainAndRefusesOutsideIt()
    {
        var entry = Bundle.BundleSchema.Entry(new CompliantFixture());
        var surface = entry.Mechanical!.CompliantSurface!.Value;
        Bundle.BundleEntry With(CompliantSurfaceDescriptor value, KinematicMode mode = KinematicMode.Static) =>
            entry with { Mechanical = entry.Mechanical! with { KinematicMode = mode, CompliantSurface = value } };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Fixture", candidate)]);
        const float least = 1f / 1024f;
        foreach (var admitted in new[]
        {
            surface,
            surface with { Tension = MechanicalValue.Fixed(120f), DampingRatio = MechanicalValue.Fixed(.08f) },
            surface with { Tension = MechanicalValue.Fixed(1200f), DampingRatio = MechanicalValue.Fixed(.8f) },
            surface with { HalfExtents = new(least, least), MaxStroke = least, ArrivalDepth = least / 4f },
            surface with { HalfExtents = new(16f, 16f), MaxStroke = 16f, RestHeight = 16f },
            surface with { RestHeight = -16f },
        }) Assert.Empty(Check(With(admitted)));
        var below = MathF.BitDecrement(least);
        foreach (var (name, invalid, field) in new[]
        {
            ("half x below 1/1024 m", surface with { HalfExtents = new(below, .9f) }, "HalfExtents"),
            ("half z below 1/1024 m", surface with { HalfExtents = new(1.2f, below) }, "HalfExtents"),
            ("half x above 16 m", surface with { HalfExtents = new(MathF.BitIncrement(16f), .9f) }, "HalfExtents"),
            ("zero half z", surface with { HalfExtents = new(1.2f, 0f) }, "HalfExtents"),
            ("nonfinite half z", surface with { HalfExtents = new(1.2f, float.NaN) }, "HalfExtents"),
            ("rest above 16 m", surface with { RestHeight = MathF.BitIncrement(16f) }, "RestHeight"),
            ("nonfinite rest", surface with { RestHeight = float.NaN }, "RestHeight"),
            ("stroke below 1/1024 m", surface with { MaxStroke = below }, "MaxStroke"),
            ("negative stroke", surface with { MaxStroke = -.5f }, "MaxStroke"),
            ("stroke above 16 m", surface with { MaxStroke = MathF.BitIncrement(16f) }, "MaxStroke"),
            ("zero arrival depth", surface with { ArrivalDepth = 0f }, "ArrivalDepth"),
            ("negative arrival depth", surface with { ArrivalDepth = -.1f }, "ArrivalDepth"),
            ("nonfinite arrival depth", surface with { ArrivalDepth = float.NaN }, "ArrivalDepth"),
            ("arrival depth at half the stroke", surface with { ArrivalDepth = surface.MaxStroke / 2f }, "ArrivalDepth"),
            ("tension below 120", surface with { Tension = MechanicalValue.Fixed(119.9f) }, "Tension"),
            ("tension above 1200", surface with { Tension = MechanicalValue.Fixed(1200.1f) }, "Tension"),
            ("damping below 0.08", surface with { DampingRatio = MechanicalValue.Fixed(.079f) }, "DampingRatio"),
            ("damping above 0.8", surface with { DampingRatio = MechanicalValue.Fixed(.801f) }, "DampingRatio"),
            ("unknown tension parameter", surface with { Tension = MechanicalValue.FromParameter("unknown") }, "Tension"),
            ("unknown damping parameter", surface with { DampingRatio = MechanicalValue.FromParameter("unknown") }, "DampingRatio"),
        }) Assert.True(Check(With(invalid)).Any(error => error.Detail.Contains($".CompliantSurface.{field}", StringComparison.Ordinal)), name);
        Assert.Contains(Check(With(surface, KinematicMode.Dynamic)), error => error.Detail.Contains(".CompliantSurface.Owner", StringComparison.Ordinal));
    }

    // The bundle's compliant-surface key is required, its value nullable: a mechanical payload written without the key is refused,
    // and one that names it null (no surface) is read.
    [Fact]
    public void TheMechanicalPayloadRequiresTheCompliantSurfaceKeyAndAdmitsANullValue()
    {
        var json = Bundle.BundleSchema.Write([new Fixture()]);
        Assert.Contains("\"CompliantSurface\": null,", json, StringComparison.Ordinal);
        Assert.Null(Assert.Single(Bundle.BundleSchema.Read(json).Elements).Mechanical!.CompliantSurface);
        var missing = json.Replace("\"CompliantSurface\": null,", "", StringComparison.Ordinal);
        Assert.NotEqual(json, missing);
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => Bundle.BundleSchema.Read(missing));
    }

    // A committed deflection binding reads the row only a declared compliant surface compiles: admitted on the compliant fixture,
    // refused on an element without one.
    [Fact]
    public void ASurfaceDeflectionBindingRequiresADeclaredCompliantSurface()
    {
        var sag = new PresentationBinding("Visual/Bed", PresentationProperty.LocalY, PresentationDriver.CommittedSurfaceDeflection, PresentationValueMapping.Identity,
            .19f, -.81f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f);
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate, PresentationBinding binding) => Bundle.BundleValidator.Validate(
            [new("Fixture", candidate with { Presentation = candidate.Presentation with { Bindings = [binding] } })]);
        Assert.Empty(Check(Bundle.BundleSchema.Entry(new CompliantFixture()), sag));
        Assert.Contains(Check(Bundle.BundleSchema.Entry(new Fixture()), sag),
            error => error.Detail.Contains(".Presentation.CompliantSurface", StringComparison.Ordinal));
        // The deflection is a committed state, read untimed and linear: a timed binding and a curved one are each refused, even on a
        // compliant surface.
        foreach (var invalid in new[] { sag with { Duration = .2f }, sag with { Curve = PresentationCurve.SmoothStep } })
            Assert.Contains(Check(Bundle.BundleSchema.Entry(new CompliantFixture()), invalid),
                error => error.Detail.Contains(".Presentation.DriverCurve", StringComparison.Ordinal));
    }

    // Story 11.1a (CAT-042): a committed drive binding is an on/off level, shown at once (Linear, no duration) or blended over its
    // declared time (SmoothStep, a positive duration), as the Motor's supply lamp is. A SmoothStep without a duration and a timed Linear
    // binding are each refused.
    [Fact]
    public void ACommittedDriveBindingIsInstantLinearOrATimedSmoothStep()
    {
        var lamp = new PresentationBinding("Visual/Lamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActuatorDrive, PresentationValueMapping.Identity,
            85f / 255f, 247f / 255f, PresentationCurve.SmoothStep, .1f, PresentationOverlap.Replace, 0f,
            Actuator: new(CoordinateKind.Prismatic, 0));
        static IReadOnlyList<Bundle.BundleError> Check(PresentationBinding binding, PrismaticDriveDescriptor? drive)
        {
            var targets = drive is not null ? new SupplyTarget[] { new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0)) } : [];
            var candidate = Bundle.BundleSchema.Entry(new Fixture
            {
                Parameters = Fixture.SpringParameters, Ports = [Fixture.PowerIn], Sliders = [Fixture.Slider() with { Drive = drive }]
            }) with
            {
                Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                    new SupplyGraphDescriptor(
                        [new SupplyConsumerDescriptor(new("consumer"), targets)], [],
                        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
            };
            return Bundle.BundleValidator.Validate([new("Fixture", candidate with { Presentation = candidate.Presentation with { Bindings = [binding] } })]);
        }
        Assert.Empty(Check(lamp, Fixture.Drive()));
        Assert.Empty(Check(lamp with { Curve = PresentationCurve.Linear, Duration = 0f }, Fixture.Drive()));
        foreach (var invalid in new[] { lamp with { Duration = 0f }, lamp with { Curve = PresentationCurve.Linear } })
            Assert.Contains(Check(invalid, Fixture.Drive()), error => error.Detail.Contains(".Presentation.DriverCurve", StringComparison.Ordinal));
        // The binding reads only a driven slot of its owner: on an owner with no driven slider or hinge it would read nothing.
        Assert.Contains(Check(lamp, null), error => error.Detail.Contains(".Presentation.Actuator", StringComparison.Ordinal));
    }

    // Story 11.1a: a drive value bound to a parameter reads it in the drive value's own unit; a slider's maximum effort is in newtons.
    [Fact]
    public void AParameterBoundSliderDriveValueMustBeInItsOwnUnit()
    {
        static IReadOnlyList<Bundle.BundleError> UnitErrors(ParameterUnit unit) => Bundle.BundleValidator.Validate([new("FixtureDrive", Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [.. Fixture.SpringParameters, new("effort", unit, ParameterKind.Real, 1f, 60f, 20f, [])],
            Ports = [Fixture.PowerIn],
            Sliders = [Fixture.Slider() with { Drive = Fixture.Drive() with { MaxEffort = MechanicalValue.FromParameter("effort") } }]
        }) with
        {
            Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new SupplyGraphDescriptor(
                    [new SupplyConsumerDescriptor(new("consumer"), [new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))])], [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
        })]);
        Assert.Empty(UnitErrors(ParameterUnit.Newton));
        var refused = Assert.Single(UnitErrors(ParameterUnit.Watt));
        Assert.Equal((Bundle.BundleErrorCode.OutOfRange, "FixtureDrive.Sliders[0].Drive.MaxEffort: outside its declared range"), (refused.Code, refused.Detail));
    }

    // A committed state binding selects its declared driven slider and maps its three states explicitly.
    // Removing that slider's drive makes the exact actuator reference invalid.
    [Fact]
    public void ACommittedActuatorStateBindingNeedsADrivenSlider()
    {
        var state = new PresentationBinding("Visual/Lamp", PresentationProperty.AlbedoRed, PresentationDriver.CommittedActuatorState, PresentationValueMapping.StateValues,
            0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
            Actuator: new(CoordinateKind.Prismatic, 0), StateValues: new(85f / 255f, 247f / 255f, 232f / 255f));
        static IReadOnlyList<Bundle.BundleError> Check(PrismaticDriveDescriptor? drive, PresentationBinding binding)
        {
            var targets = drive is not null ? new SupplyTarget[] { new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0)) } : [];
            var candidate = Bundle.BundleSchema.Entry(new Fixture
            {
                Parameters = Fixture.SpringParameters, Ports = [Fixture.PowerIn], Sliders = [Fixture.Slider() with { Drive = drive }]
            }) with
            {
                Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                    new SupplyGraphDescriptor(
                        [new SupplyConsumerDescriptor(new("consumer"), targets)], [],
                        [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
            };
            return Bundle.BundleValidator.Validate([new("Fixture", candidate with { Presentation = candidate.Presentation with { Bindings = [binding] } })]);
        }
        Assert.Empty(Check(Fixture.Drive(), state));
        Assert.Contains(Check(null, state), error => error.Detail.Contains(".Presentation.Actuator", StringComparison.Ordinal));
    }

    [Fact]
    public void ACommittedElectricalSocketBindingNeedsALogicNodeSocket()
    {
        var binding = new PresentationBinding("Visual/Socket", PresentationProperty.AlbedoRed,
            PresentationDriver.CommittedElectricalSocket, PresentationValueMapping.Identity,
            0f, 1f, PresentationCurve.SmoothStep, .2f, PresentationOverlap.Replace, 0f,
            Socket: PortId.PowerIn);

        var logicGate = Bundle.BundleSchema.Entry(new CuriousContraptions.Elements.BothGate.BothGateElementDeclaration());
        var validErrors = Bundle.BundleValidator.Validate([new("both_gate", logicGate with { Presentation = logicGate.Presentation with { Bindings = [binding] } })]);
        Assert.DoesNotContain(validErrors, e => e.Detail.Contains("Presentation.Socket"));

        var motor = Bundle.BundleSchema.Entry(new CuriousContraptions.Elements.Motor.MotorElementDeclaration());
        var nonLogicErrors = Bundle.BundleValidator.Validate([new("motor", motor with { Presentation = motor.Presentation with { Bindings = [binding] } })]);
        var nonLogic = Assert.Single(nonLogicErrors, e => e.Detail.Contains("SupplyGraph.Presentation.Socket"));
        Assert.Equal(Bundle.BundleErrorCode.OutOfRange, nonLogic.Code);

        var unboundBinding = binding with { Socket = new PortId("unbound_socket") };
        var unboundErrors = Bundle.BundleValidator.Validate([new("both_gate", logicGate with { Presentation = logicGate.Presentation with { Bindings = [unboundBinding] } })]);
        var unbound = Assert.Single(unboundErrors, e => e.Detail.Contains("SupplyGraph.Presentation.Socket"));
        Assert.Equal(Bundle.BundleErrorCode.OutOfRange, unbound.Code);
    }

    // Story 10.2a (CAT-067 D3): a body mass bound to a parameter names one declared Real kilogram parameter whose whole range lies in
    // the compiled mass domain [1/1024, 1024] kg, on a dynamic body, and the fixed declared mass is that parameter's default (one truth).
    // A cylinder's radial lanes are bound together or not at all, so every evaluation keeps X = Z.
    [Fact]
    public void AMassPolynomialNamesARealParameterAndRejectsInvalidEvaluatedFrames()
    {
        var mass = new ParameterDescriptor("mass", ParameterUnit.Kilogram, ParameterKind.Real, .25f, 8f, 4f, [], ParameterEditorPolicy.Clamp);
        var entry = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [mass],
            KinematicMode = Abstractions.Mechanical.KinematicMode.Dynamic,
            Footprint = new(1, 1, 0, 0),
            MassFrame = MassFrameDeclaration.Constant(4f, new(0f, .28f, 0f), new(.23f, .25f, .23f), Quaternion.Identity, 0f, new(0f, -9.81f, 0f)),
            Colliders = [new(ColliderShapeKind.Cylinder, new(.35f, .28f, .35f), new(0f, .28f, 0f), Quaternion.Identity, new MaterialProperties(.08f, .3f, .03f, .1f), 0f)]
        });
        var bound = new MassScalar(new ParameterId("mass"), 0f, 1f, 0f, 0f);
        Bundle.BundleEntry With(MassScalar value, params ParameterDescriptor[] parameters) =>
            entry with { Metadata = entry.Metadata with { Parameters = parameters }, Mechanical = entry.Mechanical! with
                { MassFrame = entry.Mechanical.MassFrame with { Mass = value } } };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Fixture", candidate)]);
        Assert.Empty(Check(With(bound, mass)));
        Assert.Empty(Check(With(MassScalar.Constant(4f), mass)));
        Assert.Empty(Check(With(bound, mass with { Minimum = 1f / 1024f, Maximum = 1024f })));
        foreach (var (name, candidate) in new[]
        {
            ("missing parameter", With(bound)),
            ("unknown key", With(bound with { Parameter = new ParameterId("load") }, mass)),
            ("nonfinite coefficient", With(bound with { C2 = float.NaN }, mass)),
            ("constant with variable coefficients", With(bound with { Parameter = null }, mass)),
            ("whole number", With(bound, mass with { Kind = ParameterKind.WholeNumber, Minimum = 1f })),
            ("below mass domain", With(bound, mass with { Minimum = 1f / 2048f })),
            ("above mass domain", With(bound, mass with { Maximum = 1025f })),
            ("static body", With(bound, mass) with { Mechanical = entry.Mechanical! with
                { MassFrame = entry.Mechanical.MassFrame with { Mass = bound }, KinematicMode = KinematicMode.Static } })
        }) Assert.Contains(Check(candidate), error => error.Detail.Contains(".MassFrame", StringComparison.Ordinal));

        // Endpoints .25/8 and default4 are valid; x=1 is the interior minimum.
        Assert.Contains(Check(With(new(new("mass"), .75f, -2, 1, 0), mass)), e => e.Detail.Contains(".MassFrame", StringComparison.Ordinal));
        Assert.Empty(Check(With(new(new("mass"), 1.25f, -2, 1, 0), mass)));
        // Each individual moment stays positive, but Ix+Iy-Iz has a negative interior minimum.
        var coupled = entry.Mechanical!.MassFrame with { PrincipalMoments = new(
            new(new("mass"), 1.25f, -1, .5f, 0), new(new("mass"), 1.25f, -1, .5f, 0), MassScalar.Constant(2)) };
        Assert.Contains(Check(entry with { Mechanical = entry.Mechanical with { MassFrame = coupled } }),
            e => e.Detail.Contains(".MassFrame", StringComparison.Ordinal));
        Assert.Empty(Check(entry with { Mechanical = entry.Mechanical with { MassFrame = coupled with
            { PrincipalMoments = coupled.PrincipalMoments with { Z = MassScalar.Constant(1) } } } }));
        // Different parameters must be considered together, not one at a time at the other's default.
        var second = mass with { Key = "second", Minimum = 1, Maximum = 2, Default = 2 };
        var first = mass with { Minimum = 1, Maximum = 2, Default = 2 };
        var independent = coupled with { PrincipalMoments = new(bound, bound with { Parameter = new("second") }, MassScalar.Constant(2.5f)) };
        Assert.Contains(Check(entry with { Metadata = entry.Metadata with { Parameters = [first, second] },
            Mechanical = entry.Mechanical with { MassFrame = independent } }), e => e.Detail.Contains(".MassFrame", StringComparison.Ordinal));

        var radial = new ColliderParameterBinding(0, GeometryComponent.HalfExtentX, "mass", .05f, .15f);
        var paired = radial with { Component = GeometryComponent.HalfExtentZ };
        Bundle.BundleEntry Bound(params ColliderParameterBinding[] bindings) => With(bound, mass) with { ColliderBindings = bindings };
        Assert.Empty(Check(Bound()));
        Assert.Empty(Check(Bound(radial, paired, new(0, GeometryComponent.HalfExtentY, "mass", .04f, .12f))));
        foreach (var unequal in new ColliderParameterBinding[][]
        {
            [radial], [paired], [radial, paired with { Multiplier = .06f }], [radial, paired with { Offset = .16f }],
            [radial, paired with { Parameter = "size" }]
        }) Assert.Contains(Check(Bound(unequal) with { Metadata = entry.Metadata with { Parameters =
            [mass, new("size", ParameterUnit.Metre, ParameterKind.Real, .25f, 8f, 4f, [])] } }),
            error => error.Detail.Contains("ColliderBindings[0].CylinderRadius", StringComparison.Ordinal));
        // Control: a box's radial lanes are independent.
        var box = Bound(radial) with { Colliders = [Fixture.Box(new(.35f, .28f, .35f))] };
        Assert.DoesNotContain(Check(box), error => error.Detail.Contains("CylinderRadius", StringComparison.Ordinal));
    }

    /// <summary>engine-hollow-shapes: only a hollow kind uses the radius lane. A solid kind keeps its radius in its half-extents, so a
    /// solid collider declaring a radius lane is refused, and a hollow one declaring it is admitted (its dimensions are the physics
    /// admission's).</summary>
    [Fact]
    public void OnlyAHollowColliderDeclaresTheRadiusLane()
    {
        static IReadOnlyList<Bundle.BundleError> Check(ColliderDescriptor collider) =>
            Bundle.BundleValidator.Validate([new("Fixture", Bundle.BundleSchema.Entry(new Fixture { Colliders = [collider] }))]);
        var box = Fixture.Box(new(.35f, .28f, .35f));
        Assert.DoesNotContain(Check(box), error => error.Detail.Contains("Colliders[0].Radius", StringComparison.Ordinal));
        Assert.Contains(Check(box with { Radius = .1f }), error => error.Detail.Contains("Colliders[0].Radius", StringComparison.Ordinal));
        Assert.Contains(Check(box with { Radius = float.NaN }), error => error.Detail.Contains("Colliders[0].Radius", StringComparison.Ordinal));
        var tube = box with { ShapeKind = ColliderShapeKind.Tube, Radius = .7f, HalfExtents = new(.65f, 1.8f, 0f) };
        Assert.DoesNotContain(Check(tube), error => error.Detail.Contains("Colliders[0].Radius", StringComparison.Ordinal));
    }

    [Fact]
    public void ContactCapabilityRequiresOneTypedParameterAndPulseOutput()
    {
        var entry = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [new("speed", ParameterUnit.MetrePerSecond, ParameterKind.Real, 0f, 64f, .8f, [], ParameterEditorPolicy.Clamp)],
            Ports = [new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX)]
        }) with { Domains = [SimulationDomain.Signal], Mechanical = null,
            Signal = new([new SignalContactDescriptor(new("signal"), new(0), new("speed"), new(TargetSelectorKind.AllDynamic))], [], [new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))]) };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Fixture", candidate)]);
        Assert.Empty(Check(entry));
        Assert.NotEmpty(Check(entry with { Signal = entry.Signal! with { Nodes = [new SignalContactDescriptor(new("signal"), new(0), new("unknown"), new(TargetSelectorKind.AllDynamic))] } }));
        Assert.NotEmpty(Check(entry with { Ports = [] }));
        // An internal memory can coexist without public ports.
        Assert.Empty(Check(entry with { Signal = entry.Signal! with { Nodes = [.. entry.Signal.Nodes, new SignalMemoryDescriptor(new("memory"))] } }));
        foreach (var parameter in new[]
        {
            entry.Metadata.Parameters[0] with { Unit = ParameterUnit.Second },
            entry.Metadata.Parameters[0] with { Minimum = -1f },
            entry.Metadata.Parameters[0] with { Maximum = 65f },
            entry.Metadata.Parameters[0] with { EditorPolicy = (ParameterEditorPolicy)255 }
        }) Assert.NotEmpty(Check(entry with { Metadata = entry.Metadata with { Parameters = [parameter] } }));
    }

    [Fact]
    public void TimerAndSupplyCapabilitiesRejectUnresolvedOrWrongTypedDeclarations()
    {
        var timer = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [new("seconds", ParameterUnit.Second, ParameterKind.Real, .1f, 12f, 1f, [])],
            Ports = [
                new(PortId.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortRole.ActivationIn, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX),
                new(PortId.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortRole.ActivationOut, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX)]
        }) with
        {
            Domains = [SimulationDomain.Signal], Mechanical = null,
            Signal = new([new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation)], [], [new(PortId.ActivationIn, new(new("signal"), SignalTerminal.Input)), new(PortId.ActivationOut, new(new("signal"), SignalTerminal.Output))])
        };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry entry) =>
            Bundle.BundleValidator.Validate([new("Fixture", entry)]);
        Assert.Empty(Check(timer));
        foreach (var policy in new[]
        {
            new SignalTimerDescriptor(new("signal"), new("seconds"), (TimerCompletionPolicy)255, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation),
            new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Latch, (TimerBoundary)255, TimerElapsedSignal.Activation),
            new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, (TimerElapsedSignal)255),
            new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Rearm, TimerBoundary.BeforePhysics, TimerElapsedSignal.None),
            new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.Activation)
        })
            Assert.NotEmpty(Check(timer with { Signal = timer.Signal! with { Nodes = [policy] } }));
        var silent = timer with
        {
            Ports = timer.Ports.Where(port => port.Id != PortId.ActivationOut).ToArray(),
            Signal = timer.Signal! with { Nodes = [new SignalTimerDescriptor(new("signal"), new("seconds"), TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.None)], Ports = [new(PortId.ActivationIn, new(new("signal"), SignalTerminal.Input))] }
        };
        Assert.Empty(Check(silent));
        Assert.NotEmpty(Check(silent with { Ports = [] }));
        Assert.NotEmpty(Check(timer with { Signal = timer.Signal! with { Nodes = [((SignalTimerDescriptor)timer.Signal.Nodes[0]) with { DurationParameter = new("missing") }] } }));
        Assert.NotEmpty(Check(timer with { Ports = [] }));
        Assert.NotEmpty(Check(timer with { Metadata = timer.Metadata with
            { Parameters = [new("seconds", ParameterUnit.Metre, ParameterKind.Real, .1f, 12f, 1f, [])] } }));
        var supply = timer with
        {
            Domains = [SimulationDomain.Electrical], Signal = null,
            Metadata = timer.Metadata with { Parameters = StoreParameters },
            Ports = [new(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, Vector3.Zero, Vector3.UnitX)],
            Electrical = new([new("source", Abstractions.Electrical.ElectricalRole.Source, 12f, .1f, 0f, 100f)],
                new SupplyGraphDescriptor([new SupplySourceDescriptor(new("source"), Store)], [], [new(PortId.Supply, new(new("source"), SupplyTerminal.Output))]))
        };
        var logic=supply with
        {
            Metadata=timer.Metadata,
            Ports=[
                new(PortId.FirstIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.FirstIn,null,Vector3.Zero,Vector3.UnitX),
                new(PortId.SecondIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.SecondIn,null,Vector3.Zero,Vector3.UnitX),
                new(PortId.PowerIn,SimulationDomain.Electrical,PortDirection.Input,PortRole.PowerIn,null,Vector3.Zero,Vector3.UnitX),
                new(PortId.Supply,SimulationDomain.Electrical,PortDirection.Output,PortRole.Supply,null,Vector3.Zero,Vector3.UnitX)],
            Electrical=new([new("logic",Abstractions.Electrical.ElectricalRole.LogicGate,12f,.1f,0f,100f)],
                new SupplyGraphDescriptor(
                    [new SupplyLogicDescriptor(new("logic"), Abstractions.Electrical.LogicGateOperation.And)], [],
                    [
                        new(PortId.PowerIn, new(new("logic"), SupplyTerminal.PowerIn)),
                        new(PortId.FirstIn, new(new("logic"), SupplyTerminal.FirstIn)),
                        new(PortId.SecondIn, new(new("logic"), SupplyTerminal.SecondIn)),
                        new(PortId.Supply, new(new("logic"), SupplyTerminal.Output))
                    ]))
        };
        Assert.Empty(Check(logic));
        static Bundle.BundleEntry WithOp(Bundle.BundleEntry e, Abstractions.Electrical.LogicGateOperation op) => e with
        {
            Electrical = e.Electrical! with
            {
                SupplyGraph = e.Electrical.SupplyGraph! with { Nodes = [new SupplyLogicDescriptor(new("logic"), op)] }
            }
        };
        Assert.Empty(Check(WithOp(logic, Abstractions.Electrical.LogicGateOperation.Nand)));
        Assert.Empty(Check(WithOp(logic, Abstractions.Electrical.LogicGateOperation.Nor)));
        Assert.Empty(Check(WithOp(logic, Abstractions.Electrical.LogicGateOperation.Or)));
        Assert.Empty(Check(WithOp(logic, Abstractions.Electrical.LogicGateOperation.Xor)));
        Assert.NotEmpty(Check(WithOp(logic, (Abstractions.Electrical.LogicGateOperation)5)));
        Assert.NotEmpty(Check(WithOp(logic, (Abstractions.Electrical.LogicGateOperation)255)));
        Assert.NotEmpty(Check(logic with {Ports=logic.Ports.Where(port=>port.Id!=PortId.SecondIn).ToArray()}));
        Assert.NotEmpty(Check(supply with {Electrical=supply.Electrical! with {SupplyGraph=supply.Electrical.SupplyGraph! with {Nodes=[new SupplyLogicDescriptor(new("source"), Abstractions.Electrical.LogicGateOperation.And)]}}}));
        Assert.Empty(Check(supply));
        Assert.NotEmpty(Check(supply with { Electrical = supply.Electrical! with
            { SupplyGraph = new([new CustomDescriptor(new("custom"))], [], []) } }));
        Assert.NotEmpty(Check(supply with { Ports = [] }));
        Assert.NotEmpty(Check(supply with { Electrical = supply.Electrical! with { Nodes = [] } }));
    }
    private sealed record CustomDescriptor(Abstractions.Electrical.LocalSupplyNodeId Id) : Abstractions.Electrical.SupplyNodeDescriptor(Id);
    private static string[] Errors(params (string Package, IPuzzleElementDeclaration Declaration)[] packages) =>
        PackageValidator.Validate(new CuriousContraptions.Bundle.BundleServices(), packages.Select(package => new BundlePackage(package.Package, package.Declaration)).ToArray())
            .Select(error => error.ToString()).ToArray();

    // Story 9A.39: a Source declares its finite store by four of its own parameters, inside the electrical source envelope.
    private static readonly ParameterDescriptor[] StoreParameters =
    [
        new("capacity", ParameterUnit.Joule, ParameterKind.Real, 60f, 14400f, 3600f, [], ParameterEditorPolicy.Clamp),
        new("power_limit", ParameterUnit.Watt, ParameterKind.Real, 10f, 480f, 120f, [], ParameterEditorPolicy.Clamp),
        new("initial_charge", ParameterUnit.None, ParameterKind.Real, 0f, 1f, 1f, [], ParameterEditorPolicy.Clamp),
        new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl)
    ];
    private static readonly Abstractions.Electrical.SupplySourceStoreDescriptor Store = new(new("capacity"), new("power_limit"), new("initial_charge"), new("enabled"));

    private static Bundle.BundleEntry Source(IReadOnlyList<ParameterDescriptor> parameters, Abstractions.Electrical.SupplySourceStoreDescriptor? store)
    {
        var entry = Bundle.BundleSchema.Entry(new Fixture { Parameters = parameters });
        return entry with
        {
            Domains = [SimulationDomain.Mechanical, SimulationDomain.Electrical],
            Ports = [new(PortId.Supply, SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, Vector3.Zero, Vector3.UnitX)],
            Electrical = new([new("source", Abstractions.Electrical.ElectricalRole.Source, 12f, .1f, 0f, 100f)],
                store is null ? null : new SupplyGraphDescriptor([new SupplySourceDescriptor(new("source"), store.Value)], [], [new(PortId.Supply, new(new("source"), SupplyTerminal.Output))]))
        };
    }

    private static IReadOnlyList<Bundle.BundleError> CheckSource(IReadOnlyList<ParameterDescriptor> parameters,
        Abstractions.Electrical.SupplySourceStoreDescriptor? store) => Bundle.BundleValidator.Validate([new("Fixture", Source(parameters, store))]);

    private static ParameterDescriptor[] Replace(string key, Func<ParameterDescriptor, ParameterDescriptor> change) =>
        StoreParameters.Select(parameter => parameter.Key == key ? change(parameter) : parameter).ToArray();

    [Fact]
    public void ASourceStoreNamesFourDistinctTypedParametersInsideTheFiniteDomain()
    {
        Assert.Empty(CheckSource(StoreParameters, Store));
        Assert.Empty(CheckSource(StoreParameters.Select(parameter => parameter.Key switch
        {
            "capacity" => parameter with { Minimum = 30f, Maximum = 20000f, Default = 30f },
            "power_limit" => parameter with { Minimum = 5f, Maximum = 1200f, Default = 5f },
            _ => parameter
        }).ToArray(), Store));
        // Product parameter bounds are declaration data, within the generic finite domain.
        Assert.Empty(CheckSource(Replace("power_limit", p => p with { Minimum = 10f, Maximum = 480f, Default = 10f }), Store));
        foreach (var (name, parameters, store) in new (string, ParameterDescriptor[], Abstractions.Electrical.SupplySourceStoreDescriptor?)[]
        {
            ("missing store", StoreParameters, null),
            ("absent capacity key", StoreParameters.Where(p => p.Key != "capacity").ToArray(), Store),
            ("unknown key", StoreParameters, Store with { PowerLimit = new("power") }),
            ("duplicated key", [.. StoreParameters, StoreParameters[0]], Store),
            ("two roles share one key", StoreParameters, Store with { InitialCharge = new("capacity") }),
            ("capacity in watts", Replace("capacity", p => p with { Unit = ParameterUnit.Watt }), Store),
            ("zero source capacity", Replace("capacity", p => p with { Minimum = 0f }), Store),
            ("nonfinite capacity", Replace("capacity", p => p with { Maximum = float.PositiveInfinity }), Store),
            ("power in joules", Replace("power_limit", p => p with { Unit = ParameterUnit.Joule }), Store),
            ("zero source power", Replace("power_limit", p => p with { Minimum = 0f }), Store),
            ("nonfinite power", Replace("power_limit", p => p with { Maximum = float.PositiveInfinity }), Store),
            ("charge above 1", Replace("initial_charge", p => p with { Maximum = 1.5f }), Store),
            ("charge with a unit", Replace("initial_charge", p => p with { Unit = ParameterUnit.Metre }), Store),
            ("whole-number capacity", Replace("capacity", p => p with { Kind = ParameterKind.WholeNumber }), Store),
            ("enable is Real", Replace("enabled", p => p with { Kind = ParameterKind.Real, Options = [] }), Store),
            ("enable is Boolean", Replace("enabled", p => p with { Kind = ParameterKind.Boolean, Options = [] }), Store),
            ("enable options inverted", Replace("enabled", p => p with { Options = ["Enabled", "Disabled"] }), Store),
            ("enable option misnamed", Replace("enabled", p => p with { Options = ["Off", "On"] }), Store),
            ("enable with three options", Replace("enabled", p => p with { Maximum = 2f, Options = ["Disabled", "Enabled", "Boost"] }), Store),
        })
            Assert.True(CheckSource(parameters, store).Count > 0, name);
        // An unadmitted capability descriptor on a fixture is refused.
        var gate = Source(StoreParameters, Store);
        Assert.NotEmpty(Bundle.BundleValidator.Validate([new("Fixture", gate with { Electrical = gate.Electrical! with
            { SupplyGraph = new([new CustomDescriptor(new("custom"))], [], []) } })]));
    }

    // engine-run-control-table: a RunControl parameter binds exactly one run-controllable lane, found by the one generic resolver
    // (a Source store's enable state is the SourceEnable lane). A RunControl parameter bound to no lane, such as the store's capacity or
    // the enable of a part without a store, is refused. Several bindings: ADriveTargetRunControlBindsOneLaneOnAPartWithExactlyOneDrivenHinge.
    [Fact]
    public void ARunControlParameterBindsExactlyOneRunControllableLane()
    {
        var source = Source(StoreParameters, Store);
        Assert.Equal([RunControlLane.SourceEnable], Bundle.RunControlBindings.Lanes(source, "enabled"));
        Assert.Empty(Bundle.RunControlBindings.Lanes(source, "capacity"));
        Assert.Empty(Bundle.RunControlBindings.Lanes(Source(StoreParameters, null), "enabled"));
        Assert.Empty(CheckSource(StoreParameters, Store));
        Assert.Contains(CheckSource(Replace("capacity", p => p with { RunEdit = ParameterRunEdit.RunControl }), Store), error =>
            error.Code == Bundle.BundleErrorCode.OutOfRange && error.Detail.Contains("Metadata.Parameters[0].RunEdit", StringComparison.Ordinal));
        // A RunControl enable on a part without a store binds no lane and is refused by the validator.
        Assert.Contains(CheckSource(StoreParameters, null), error =>
            error.Code == Bundle.BundleErrorCode.OutOfRange && error.Detail.Contains("Metadata.Parameters[3].RunEdit", StringComparison.Ordinal));
    }

    // engine-revolute-drive-target: a hinge drive whose TargetSpeed is a parameter binds that parameter to the RevoluteDriveTarget lane,
    // once per such hinge. The shipped Motor's RunControl speed binds exactly one lane and is admitted; its torque binds none, so a
    // RunControl torque is refused. A speed bound by two hinges binds two lanes and is refused, and a part whose RunControl drive target
    // sits beside a second driven hinge is refused because its owner's drive slot would not be unique. Each refusal's control is the same
    // part with the parameter BuildOnly.
    [Fact]
    public void ADriveTargetRunControlBindsOneLaneOnAPartWithExactlyOneDrivenHinge()
    {
        var generated = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root);
        var motor = Bundle.BundleSchema.Read(generated.Json!).Elements.Single(entry => entry.Metadata.Id == "motor");
        static bool RunEditRefused(Bundle.BundleEntry candidate, int parameter) => Bundle.BundleValidator.Validate([new("Motor", candidate)])
            .Any(error => error.Code == Bundle.BundleErrorCode.OutOfRange &&
                error.Detail.Contains($"Metadata.Parameters[{parameter}].RunEdit", StringComparison.Ordinal));
        static Bundle.BundleEntry RunEdit(Bundle.BundleEntry entry, string key, ParameterRunEdit edit) => entry with
        {
            Metadata = entry.Metadata with { Parameters = entry.Metadata.Parameters.Select(p => p.Key == key ? p with { RunEdit = edit } : p).ToArray() }
        };
        Assert.Equal(("speed", ParameterRunEdit.RunControl), (motor.Metadata.Parameters[0].Key, motor.Metadata.Parameters[0].RunEdit));
        Assert.Equal([RunControlLane.RevoluteDriveTarget], Bundle.RunControlBindings.Lanes(motor, "speed"));
        Assert.Empty(Bundle.RunControlBindings.Lanes(motor, "torque"));
        Assert.Empty(Bundle.BundleValidator.Validate([new("Motor", motor)]));
        Assert.True(RunEditRefused(RunEdit(motor, "torque", ParameterRunEdit.RunControl), 1));

        var hinge = motor.Mechanical!.Hinges[0];
        var twice = motor with { Mechanical = motor.Mechanical with { Hinges = [hinge, hinge] } };
        Assert.Equal([RunControlLane.RevoluteDriveTarget, RunControlLane.RevoluteDriveTarget], Bundle.RunControlBindings.Lanes(twice, "speed"));
        Assert.True(RunEditRefused(twice, 0));
        Assert.False(RunEditRefused(RunEdit(twice, "speed", ParameterRunEdit.BuildOnly), 0));

        var beside = motor with { Mechanical = motor.Mechanical with
        {
            Hinges = [hinge, hinge with { Drive = new(MechanicalValue.Fixed(6f), MechanicalValue.FromParameter("torque")) }]
        } };
        Assert.Equal([RunControlLane.RevoluteDriveTarget], Bundle.RunControlBindings.Lanes(beside, "speed"));
        Assert.False(RunEditRefused(beside, 0));

        var nonLaneBound = beside with { Mechanical = beside.Mechanical with
        {
            Hinges = [hinge with { Stiffness = MechanicalValue.FromParameter("speed") }]
        } };
        Assert.True(RunEditRefused(nonLaneBound, 0));
        Assert.False(RunEditRefused(RunEdit(beside, "speed", ParameterRunEdit.BuildOnly), 0));
    }

    // Recorded rename (engine-revolute-drive-target): formerly ARunControlIsDeclaredOnlyOnASourceStoreEnableAndAChoiceIsNeverClamped. A
    // second lane exists, so the rule is "only on a parameter bound to a lane"; the assertions are unchanged.
    [Fact]
    public void ARunControlIsDeclaredOnlyOnABoundParameterAndAChoiceIsNeverClamped()
    {
        Assert.Empty(CheckSource(StoreParameters, Store));
        // The enable state may also be build-only: the package then offers no run-time control.
        Assert.Empty(CheckSource(Replace("enabled", p => p with { RunEdit = ParameterRunEdit.BuildOnly }), Store));
        Assert.Contains(CheckSource(Replace("capacity", p => p with { RunEdit = ParameterRunEdit.RunControl }), Store),
            error => error.Detail.Contains("Metadata.Parameters[0].RunEdit", StringComparison.Ordinal));
        Assert.NotEmpty(CheckSource(Replace("enabled", p => p with { RunEdit = (ParameterRunEdit)7 }), Store));
        Assert.Contains(CheckSource(Replace("enabled", p => p with { EditorPolicy = ParameterEditorPolicy.Clamp }), Store),
            error => error.Detail.Contains("Metadata.Parameters[3].EditorPolicy", StringComparison.Ordinal));
        // A parameter bound to no lane (here a Choice on a part without a store or drive) may not be run-editable.
        Assert.NotEmpty(Errors(("Fixture", new Fixture { Parameters =
            [new("participation", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl)] })));
        Assert.NotEmpty(Errors(("Fixture", new Fixture { Parameters =
            [new("participation", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], ParameterEditorPolicy.Clamp)] })));
    }

    [Fact]
    public void AValidPackageIsAdmitted() => Assert.Empty(Errors(("Fixture", new Fixture())));

    [Fact]
    public void ADuplicateIdNamesBothPackages() =>
        Assert.Equal(["BUNDLE001 duplicate id 'x' (FixtureA, FixtureB)"],
            Errors(("FixtureA", new Fixture { Id = "x" }), ("FixtureB", new Fixture { Id = "x", PaletteOrder = 2 })));

    [Fact]
    public void ADuplicatePaletteOrderNamesBothPackages() =>
        Assert.Equal(["BUNDLE008 duplicate palette order 4 (FixtureA, FixtureB)"],
            Errors(("FixtureA", new Fixture { Id = "a", PaletteOrder = 4 }), ("FixtureB", new Fixture { Id = "b", PaletteOrder = 4 })));

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void APaletteOrderMustBePositive(int order) =>
        Assert.Equal(["BUNDLE006 FixtureOrder.Metadata.PaletteOrder: outside its declared range"],
            Errors(("FixtureOrder", new Fixture { PaletteOrder = order })));

    [Theory]
    [InlineData("", "Fixture")]
    [InlineData(" Fixture", "Fixture")]
    [InlineData("Fixture", "")]
    public void TitlesAndDescriptionsMustBeText(string title, string description) =>
        Assert.Equal([$"BUNDLE010 FixtureText.Metadata.{(title.Trim() == title && title.Length > 0 ? "Description" : "DisplayName")}: empty or padded text"],
            Errors(("FixtureText", new Fixture { DisplayName = title, Description = description })));

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void AColourChannelMustLieInUnitRange(float red) =>
        Assert.Equal(["BUNDLE006 FixtureColour.Metadata.Color.R: outside its declared range"],
            Errors(("FixtureColour", new Fixture { Color = new(red, .5f, .5f, 1f) })));

    [Theory]
    [InlineData("res://parts/scenes/fixture.tscn")]
    [InlineData("res://elements/../fixture.tscn")]
    [InlineData("elements/Fixture/fixture.tscn")]
    public void AnAssetPathMustBeAPackagePath(string path) =>
        Assert.Equal([$"BUNDLE007 FixturePath.Metadata.ScenePath: '{path}' is not a package asset path"],
            Errors(("FixturePath", new Fixture { ScenePath = path })));

    [Fact]
    public void APortNormalMustBeAUnitDirection()
    {
        PortDescriptor Port(Vector3 normal) => new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null,
            Vector3.Zero, normal);
        var entry = Bundle.BundleSchema.Entry(new Fixture { Ports = [Port(-Vector3.UnitY)] }) with
        {
            Electrical = new([new("c", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new SupplyGraphDescriptor([new SupplyConsumerDescriptor(new("c"), [])], [],
                    [new(PortId.PowerIn, new(new("c"), SupplyTerminal.PowerIn))]))
        };
        Assert.Empty(Bundle.BundleValidator.Validate([new("Fixture", entry)]));
        Assert.Equal(["BUNDLE006 FixtureNormal.Ports[0].Normal: outside its declared range"],
            Bundle.BundleValidator.Validate([new("FixtureNormal", entry with { Ports = [Port(new(0f, 2f, 0f))] })])
                .Select(e => e.ToString()));
    }

    /// <summary>Stories 9A.14 and 9A.18: a missing icon (or scene) fails the bundle build, and an asset outside the package's own
    /// directory is refused even when the file exists.</summary>
    [Fact]
    public void AMissingOrForeignPackageAssetFailsTheBuild()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Fixture"));
        Directory.CreateDirectory(Path.Combine(root, "elements", "Other"));
        try
        {
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "fixture.tscn"), "");
            File.WriteAllText(Path.Combine(root, "elements", "Other", "icon.svg"), "");
            BundlePackage[] packages = [new("Fixture", new Fixture())];
            Assert.Equal(["BUNDLE009 Fixture.Metadata.IconPath: 'res://elements/Fixture/icon.svg' is not a file in elements/Fixture/"],
                PackageValidator.Assets(root, packages).Select(error => error.ToString()));
            File.WriteAllText(Path.Combine(root, "elements", "Fixture", "icon.svg"), "");
            Assert.Empty(PackageValidator.Assets(root, packages));
            Assert.Equal(["BUNDLE009 Fixture.Metadata.IconPath: 'res://elements/Other/icon.svg' is not a file in elements/Fixture/"],
                PackageValidator.Assets(root, [new("Fixture", new Fixture { IconPath = "res://elements/Other/icon.svg" })])
                    .Select(error => error.ToString()));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EveryRealPackageShipsItsIconAndScene() =>
        Assert.Empty(PackageValidator.Assets(TestRepository.Root, PackageDiscovery.Discover(TestRepository.Root).Packages));

    [Fact]
    public void ANonFiniteValueNamesThePackageAndTheField() =>
        Assert.Equal(["BUNDLE002 FixtureNaN.Colliders[0].HalfExtents.x: not finite"],
            Errors(("FixtureNaN", new Fixture { Colliders = [Fixture.Box(new(float.NaN, .5f, .5f))] })));

    [Fact]
    public void AnInfiniteMassIsNonFinite() =>
        Assert.Equal(["BUNDLE006 FixtureMass.MassFrame: outside its declared range"],
            Errors(("FixtureMass", new Fixture { MassFrame = MassFrameDeclaration.Constant(float.PositiveInfinity, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0f, Vector3.Zero) })));

    [Fact]
    public void AnUnknownEnumNamesThePackageAndTheField() =>
        Assert.Equal(["BUNDLE003 FixtureEnum.Metadata.Category: undefined enum value"],
            Errors(("FixtureEnum", new Fixture { Category = (ElementCategory)99 })));

    [Fact]
    public void ACombinedDomainSetIsDefinedButAnUnknownDomainBitIsNot()
    {
        Assert.Empty(Errors(("Fixture", new Fixture { SupportedDomains = SimulationDomain.Mechanical | SimulationDomain.Signal })));
        Assert.Equal(["BUNDLE003 FixtureDomain.SupportedDomains: undefined enum value"],
            Errors(("FixtureDomain", new Fixture { SupportedDomains = (SimulationDomain)(1u << 9) })));
    }

    [Fact]
    public void ABodyWithoutAColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureBody.Colliders: a body declares no collider"],
            Errors(("FixtureBody", new Fixture { Colliders = [] })));

    [Fact]
    public void ANonMechanicalBodyWithoutAColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureLamp.Colliders: a body declares no collider"],
            Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0, 0), Colliders = [] })));

    [Fact]
    public void ANonMechanicalPackageWithColliderOrWithoutBodyIsAdmitted()
    {
        Assert.Empty(Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0, 0) })));
        Assert.Empty(Errors(("FixtureSignal", new SignalFixture { Footprint = new(0, 0, 0, 0), Colliders = [] })));
    }

    // Story 9A.22: a declared event kind is a defined record kind, declared once; None is no record.
    [Fact]
    public void AnEventKindThatIsNoneUndefinedOrRepeatedIsRefused()
    {
        Assert.Empty(Errors(("FixtureEvents", new Fixture { Events = [new(SimulationEventKind.Impact), new(SimulationEventKind.Captured)] })));
        Assert.Equal(["BUNDLE003 FixtureEvents.Events[0].Kind: undefined enum value"],
            Errors(("FixtureEvents", new Fixture { Events = [new(SimulationEventKind.None)] })));
        Assert.Equal(["BUNDLE003 FixtureEvents.Events[0].Kind: undefined enum value"],
            Errors(("FixtureEvents", new Fixture { Events = [new((SimulationEventKind)9)] })));
        Assert.Equal(["BUNDLE006 FixtureEvents.Events[1].Kind: outside its declared range"],
            Errors(("FixtureEvents", new Fixture { Events = [new(SimulationEventKind.Impact), new(SimulationEventKind.Impact)] })));
    }

    [Fact]
    public void ADefaultOutsideItsBoundsIsRefused() =>
        Assert.Equal(["BUNDLE006 FixtureRange.Metadata.Parameters[0].Default: outside its declared range"],
            Errors(("FixtureRange", new Fixture { Parameters = [new("length", ParameterUnit.Metre, ParameterKind.Real, 1f, 2f, 3f, [])] })));

    [Fact]
    public void AChoiceMustSpanItsOptions()
    {
        Assert.Empty(Errors(("Fixture", new Fixture { Parameters = [new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"])] })));
        Assert.Equal(["BUNDLE006 FixtureChoice.Metadata.Parameters[0].Options: outside its declared range"],
            Errors(("FixtureChoice", new Fixture { Parameters = [new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 2f, 1f, ["Disabled", "Enabled"])] })));
    }

    [Fact]
    public void AValidSliderIsAdmitted() =>
        Assert.Empty(Errors(("Fixture", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider()] })));

    [Fact]
    public void ASliderNamingAnUndeclaredParameterIsRefused() =>
        Assert.Equal(["BUNDLE007 FixtureSlider.Sliders[0].Stiffness.Parameter: 'spring_rate' names no declared Real parameter"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider(stiffness: "spring_rate")] })));

    [Fact]
    public void ASliderWhoseMovingBodyHasNoColliderIsRefused() =>
        Assert.Equal(["BUNDLE004 FixtureSlider.Sliders[0].MovingBody.Colliders: a body declares no collider"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider(moving: [])] })));

    [Fact]
    public void ASliderMustTravelThroughItsRestFrame() =>
        Assert.Equal(["BUNDLE006 FixtureSlider.Sliders[0].LowerLimit: outside its declared range"],
            Errors(("FixtureSlider", new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider() with { LowerLimit = .1f }] })));

    [Fact]
    public void ASliderWithANonFiniteMovingMassIsRefused() =>
        Assert.Equal(["BUNDLE006 FixtureSlider.Sliders[0].MovingBody.MassFrame: outside its declared range"],
            Errors(("FixtureSlider", new Fixture
            {
                Parameters = Fixture.SpringParameters,
                Sliders = [Fixture.Slider() with { MovingBody = Fixture.Slider().MovingBody with { MassFrame = Fixture.Slider().MovingBody.MassFrame with { Mass = MassScalar.Constant(float.NaN) } } }]
            })));

    // C-Gate (9A.19): a supplied drive is declaration data; the bundle refuses one the runtime could not admit.
    private static string[] DriveErrors(Abstractions.Mechanical.PrismaticDriveDescriptor drive, bool hasConsumer = true, int sliders = 1)
    {
        var targets = hasConsumer
            ? Enumerable.Range(0, sliders).Select(i => (SupplyTarget)new SupplyDriveTarget(new(CoordinateKind.Prismatic, i))).ToArray()
            : [];
        var entry = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = Fixture.SpringParameters,
            Ports = [Fixture.PowerIn],
            Sliders = Enumerable.Repeat(Fixture.Slider() with { Drive = drive }, sliders).ToArray()
        }) with
        {
            Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new SupplyGraphDescriptor(
                    [new SupplyConsumerDescriptor(new("consumer"), targets)], [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
        };
        return Bundle.BundleValidator.Validate([new("FixtureDrive", entry)])
            .Select(error => error.ToString()).ToArray();
    }

    [Fact]
    public void ADrivenSliderWithAPowerInIsAdmitted() => Assert.Empty(DriveErrors(Fixture.Drive()));

    [Fact]
    public void ADrivenSliderRequiresAConsumerTarget() =>
        Assert.Equal(["BUNDLE006 fixture.SupplyGraph.Drive.MissingConsumer"],
            DriveErrors(Fixture.Drive(), hasConsumer: false));

    [Fact]
    public void OnePowerInEnablesEveryDeclaredSlider() =>
        Assert.Empty(DriveErrors(Fixture.Drive(), sliders: 2));

    [Theory]
    [InlineData("Target", .1f)]                     // above the slider's 0 upper limit
    [InlineData("Target", -.3f)]                    // below its -0.25 lower limit
    [InlineData("MaxSpeed", 0f)]
    [InlineData("MaxSpeed", 65f)]                   // beyond the envelope's 64 m/s
    [InlineData("StoppingAcceleration", -1f)]
    [InlineData("MaxEffort", float.NaN)]
    [InlineData("MaxPower", float.PositiveInfinity)]
    public void ADriveValueOutsideItsDomainIsRefused(string field, float value)
    {
        var fixedValue = Abstractions.Mechanical.MechanicalValue.Fixed(value);
        var drive = field switch
        {
            "Target" => Fixture.Drive() with { Target = fixedValue },
            "MaxSpeed" => Fixture.Drive() with { MaxSpeed = fixedValue },
            "StoppingAcceleration" => Fixture.Drive() with { StoppingAcceleration = fixedValue },
            "MaxEffort" => Fixture.Drive() with { MaxEffort = fixedValue },
            "MaxPower" => Fixture.Drive() with { MaxPower = fixedValue },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        var error = Assert.Single(DriveErrors(drive));
        Assert.Contains($"FixtureDrive.Sliders[0].Drive.{field}", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ADriveValueNamingAnUndeclaredParameterIsRefused() =>
        Assert.Equal(["BUNDLE007 FixtureDrive.Sliders[0].Drive.MaxEffort.Parameter: 'effort' names no declared Real parameter"],
            DriveErrors(Fixture.Drive() with { MaxEffort = Abstractions.Mechanical.MechanicalValue.FromParameter("effort") }));

    [Fact]
    public void AnAssemblyWithTwoDeclarationsIsRefused() =>
        Assert.Equal(["BUNDLE005 FixturePair: declares 2 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixturePair", typeof(Fixture).Assembly).Errors.Select(error => error.ToString()));

    // C-Lamp+Detector (D3): the indicator names a declared Signal input admitting a level; the passage aperture is finite and in range
    // (radius 0.01–10 m, clearance 0.001–10 m) on a static frame with a pulse ActivationOut; and at most one node-selecting capability
    [Fact]
    public void HingeCapabilityValidatesLimitsFramesMovingBodyCollidersInitialAngleAndFrictionTorque()
    {
        var generated = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root);
        var bundle = Bundle.BundleSchema.Read(generated.Json!).Elements;
        var lever = bundle.Single(entry => entry.Metadata.Id == "impact_lever");

        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) =>
            Bundle.BundleValidator.Validate([new("Candidate", candidate)]);
        static void Refused(Bundle.BundleEntry candidate, string field, Bundle.BundleErrorCode code = Bundle.BundleErrorCode.OutOfRange)
        {
            var error = Assert.Single(Check(candidate));
            Assert.Equal((code, true), (error.Code, error.Detail.StartsWith("Candidate." + field + ":", StringComparison.Ordinal)));
        }

        Assert.Empty(Check(lever));
        var hinge = lever.Mechanical!.Hinges[0];

        // Inverted limits Lower >= Upper
        Refused(lever with
        {
            Mechanical = lever.Mechanical with
            {
                Hinges = [hinge with { LowerLimit = 0.5f, UpperLimit = -0.5f }]
            }
        }, "Hinges[0].UpperLimit");

        // Non-unit quaternion
        Refused(lever with
        {
            Mechanical = lever.Mechanical with
            {
                Hinges = [hinge with { BaseFrame = new(Vector3.Zero, new Quaternion(1f, 1f, 1f, 1f)) }]
            }
        }, "Hinges[0].BaseFrame.Rotation");

        // Moving body without colliders
        Refused(lever with
        {
            Mechanical = lever.Mechanical with
            {
                Hinges = [hinge with { MovingBody = hinge.MovingBody with { Colliders = [] } }]
            }
        }, "Hinges[0].MovingBody.Colliders", Bundle.BundleErrorCode.BodyWithoutCollider);

        // Port body index out of bounds
        Refused(lever with
        {
            Ports = [lever.Ports[0] with { Body = new PackageBodyIndex(99) }, lever.Ports[1]]
        }, "Ports[0].Body");

        // Constant initial angle out of range
        Refused(lever with
        {
            Mechanical = lever.Mechanical with
            {
                Hinges = [hinge with { InitialAngle = MechanicalValue.Fixed(1.5f) }]
            }
        }, "Hinges[0].InitialAngle");

        // Friction torque (Story 10.3b): finite and at least 0; 0 is admitted (frictionless through the same law). Negative infinity
        // breaks both rules, so it is refused for both, on the same field.
        Bundle.BundleEntry WithFriction(float value) => lever with
        {
            Mechanical = lever.Mechanical with { Hinges = [hinge with { FrictionTorque = MechanicalValue.Fixed(value) }] }
        };
        Assert.Empty(Check(WithFriction(0f)));
        Refused(WithFriction(-1f), "Hinges[0].FrictionTorque");
        Refused(WithFriction(float.NaN), "Hinges[0].FrictionTorque.Constant", Bundle.BundleErrorCode.NonFinite);
        Refused(WithFriction(float.PositiveInfinity), "Hinges[0].FrictionTorque.Constant", Bundle.BundleErrorCode.NonFinite);
        var negativeInfinity = Check(WithFriction(float.NegativeInfinity));
        Assert.Equal([Bundle.BundleErrorCode.NonFinite, Bundle.BundleErrorCode.OutOfRange], negativeInfinity.Select(error => error.Code).Order());
        Assert.All(negativeInfinity, error => Assert.StartsWith("Candidate.Hinges[0].FrictionTorque", error.Detail, StringComparison.Ordinal));
    }

    // engine-revolute-drive: a hinge's travel is a defined RevoluteTravel. Continuous travel requires exactly the full-turn limits
    // [−π, π] and no axial spring. A supplied drive requires the owner's electrical PowerIn input, a target speed within ±128 rad/s (the
    // angular envelope) and a positive maximum torque; each boundary value is admitted and the value one step beyond it refused.
    [Fact]
    public void HingeCapabilityValidatesTravelAndTheSuppliedDrive()
    {
        var generated = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root);
        var lever = Bundle.BundleSchema.Read(generated.Json!).Elements.Single(entry => entry.Metadata.Id == "impact_lever");
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) =>
            Bundle.BundleValidator.Validate([new("Candidate", candidate)]);
        static void Refused(Bundle.BundleEntry candidate, string field, Bundle.BundleErrorCode code = Bundle.BundleErrorCode.OutOfRange)
        {
            var error = Assert.Single(Check(candidate));
            Assert.Equal((code, true), (error.Code, error.Detail.StartsWith("Candidate." + field + ":", StringComparison.Ordinal)));
        }
        var hinge = lever.Mechanical!.Hinges[0];
        Bundle.BundleEntry With(Bundle.BundleEntry entry, RevoluteHingeDescriptor value) =>
            entry with { Mechanical = entry.Mechanical! with { Hinges = [value] } };
        var full = hinge with { LowerLimit = -MathF.PI, UpperLimit = MathF.PI, Travel = RevoluteTravel.Continuous };

        Assert.Empty(Check(With(lever, full)));
        Refused(With(lever, hinge with { Travel = RevoluteTravel.Continuous }), "Hinges[0].Travel");
        Refused(With(lever, full with { UpperLimit = MathF.BitDecrement(MathF.PI) }), "Hinges[0].Travel");
        Refused(With(lever, full with { Stiffness = MechanicalValue.Fixed(1f) }), "Hinges[0].Stiffness");
        Refused(With(lever, hinge with { Travel = (RevoluteTravel)2 }), "Hinges[0].Travel", Bundle.BundleErrorCode.UndefinedEnum);

        var driven = full with { Drive = new(MechanicalValue.Fixed(6f), MechanicalValue.Fixed(2f)) };
        Assert.Contains(Check(With(lever, driven)), error => error.Code == Bundle.BundleErrorCode.OutOfRange && (error.Detail.EndsWith("SupplyGraph.Missing", StringComparison.Ordinal) || error.Detail.EndsWith("Drive.MissingConsumer", StringComparison.Ordinal)));
        var powered = lever with
        {
            Ports = [.. lever.Ports, new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, Vector3.Zero, Vector3.UnitZ)],
            Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new SupplyGraphDescriptor(
                    [new SupplyConsumerDescriptor(new("consumer"), [new SupplyDriveTarget(new(CoordinateKind.Revolute, 0))])], [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
        };
        RevoluteHingeDescriptor Drive(float target, float torque) =>
            full with { Drive = new(MechanicalValue.Fixed(target), MechanicalValue.Fixed(torque)) };
        Assert.Empty(Check(With(powered, Drive(128f, 2f))));
        Assert.Empty(Check(With(powered, Drive(-128f, float.Epsilon))));
        Refused(With(powered, Drive(MathF.BitIncrement(128f), 2f)), "Hinges[0].Drive.TargetSpeed");
        Refused(With(powered, Drive(MathF.BitDecrement(-128f), 2f)), "Hinges[0].Drive.TargetSpeed");
        Refused(With(powered, Drive(6f, 0f)), "Hinges[0].Drive.MaxTorque");
        Refused(With(powered, Drive(float.NaN, 2f)), "Hinges[0].Drive.TargetSpeed.Constant", Bundle.BundleErrorCode.NonFinite);

        // Story 11.1a: a drive bound to parameters reads a target in rad/s and a torque in N·m; a parameter in any other unit is refused.
        var bound = full with { Drive = new(MechanicalValue.FromParameter("speed"), MechanicalValue.FromParameter("torque")) };
        Bundle.BundleEntry Parameterised(ParameterUnit speed, ParameterUnit torque) => powered with
        {
            Metadata = powered.Metadata with
            {
                Parameters = [.. powered.Metadata.Parameters, new("speed", speed, ParameterKind.Real, 0f, 20f, 6f, []),
                    new("torque", torque, ParameterKind.Real, .1f, 100f, 20f, [])]
            }
        };
        Assert.Empty(Check(With(Parameterised(ParameterUnit.RadianPerSecond, ParameterUnit.NewtonMetre), bound)));
        Refused(With(Parameterised(ParameterUnit.MetrePerSecond, ParameterUnit.NewtonMetre), bound), "Hinges[0].Drive.TargetSpeed");
        Refused(With(Parameterised(ParameterUnit.RadianPerSecond, ParameterUnit.Joule), bound), "Hinges[0].Drive.MaxTorque");
    }

    // engine-revolute-drive: a drive is supplied through an explicit consumer target in its supply graph.
    [Fact]
    public void ADriveRequiresAConsumerTargetForEitherJointKind()
    {
        var bundle = Bundle.BundleSchema.Read(BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root).Json!).Elements;
        var contact = bundle.Single(entry => entry.Metadata.Id == "pressure_plate").Electrical;
        Assert.IsType<SupplyContactDescriptor>(contact!.SupplyGraph!.Nodes.Single());
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Candidate", candidate)]);
        static bool RefusesDrive(Bundle.BundleEntry candidate) => Check(candidate).Any(error =>
            error.Code == Bundle.BundleErrorCode.OutOfRange && error.Detail.EndsWith("Drive.MissingConsumer", StringComparison.Ordinal));

        var gate = bundle.Single(entry => entry.Metadata.Id == "powered_gate");
        Assert.NotNull(gate.Mechanical!.Sliders[0].Drive);
        Assert.False(RefusesDrive(gate));
        Assert.True(RefusesDrive(gate with { Electrical = contact }));

        var lever = bundle.Single(entry => entry.Metadata.Id == "impact_lever");
        var hinge = lever.Mechanical!.Hinges[0] with
        {
            LowerLimit = -MathF.PI, UpperLimit = MathF.PI, Travel = RevoluteTravel.Continuous,
            Drive = new(MechanicalValue.Fixed(6f), MechanicalValue.Fixed(2f))
        };
        var driven = lever with
        {
            Ports = [.. lever.Ports, new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, null, Vector3.Zero, Vector3.UnitZ)],
            Mechanical = lever.Mechanical with { Hinges = [hinge] },
            Electrical = new([new("consumer", Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new SupplyGraphDescriptor(
                    [new SupplyConsumerDescriptor(new("consumer"), [new SupplyDriveTarget(new(CoordinateKind.Revolute, 0))])],
                    [],
                    [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]))
        };
        Assert.Empty(Check(driven));
        Assert.True(RefusesDrive(driven with { Electrical = contact }));
    }

    [Fact]
    public void AnAssemblyWithNoDeclarationIsRefused() =>
        Assert.Equal(["BUNDLE005 FixtureNone: declares 0 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixtureNone", typeof(IPuzzleElementDeclaration).Assembly).Errors.Select(error => error.ToString()));
}
