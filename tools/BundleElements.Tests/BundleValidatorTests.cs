using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;

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
        var capture = new Abstractions.Mechanical.ResidenceCaptureDescriptor(
            new(-.66f, -.4f, -.66f), new(.66f, .45f, .66f),
            Abstractions.Mechanical.MechanicalValue.Fixed(.02f),
            Abstractions.Mechanical.MechanicalValue.Fixed(1.5f),
            Abstractions.Mechanical.MechanicalValue.Fixed(.35f), "participation",
            new(new(-1.1f, .5f, -1.1f), new(1.1f, 1.5f, 1.1f), .5f,
                Abstractions.Mechanical.MechanicalValue.Fixed(0f), Abstractions.Mechanical.MechanicalValue.Fixed(0f)));
        IReadOnlyList<Bundle.BundleError> Check(Abstractions.Mechanical.ResidenceCaptureDescriptor value) =>
            Bundle.BundleValidator.Validate([new("Fixture", entry with { Mechanical = entry.Mechanical! with { Capture = value } })]);
        Assert.Empty(Check(capture));
        foreach (var invalid in new[]
        {
            capture with { ParticipationParameter = null! },
            capture with { ParticipationParameter = "" },
            capture with { ParticipationParameter = "unknown" },
            capture with { Minimum = new(float.NaN, 0f, 0f) },
            capture with { Maximum = capture.Minimum },
            capture with { UpperMargin = Abstractions.Mechanical.MechanicalValue.Fixed(-1f) },
            capture with { SpeedLimit = Abstractions.Mechanical.MechanicalValue.Fixed(0f) },
            capture with { Dwell = Abstractions.Mechanical.MechanicalValue.FromParameter("unknown") },
            capture with { Guide = capture.Guide with { Minimum = new(float.PositiveInfinity, 0f, 0f) } },
            capture with { Guide = capture.Guide with { Maximum = capture.Guide.Minimum } },
            capture with { Guide = capture.Guide with { SupportHeight = 17f } },
            capture with { Guide = capture.Guide with { SupportMargin = Abstractions.Mechanical.MechanicalValue.Fixed(1.01f) } },
            capture with { Guide = capture.Guide with { MaximumAcceleration = Abstractions.Mechanical.MechanicalValue.Fixed(12.01f) } }
        }) Assert.NotEmpty(Check(invalid));
        Assert.NotEmpty(Bundle.BundleValidator.Validate([new("Fixture", entry with
        {
            Metadata = entry.Metadata with { Parameters = [] },
            Mechanical = entry.Mechanical! with { Capture = capture }
        })]));
    }

    // The finite contact-work store (C-Bumper+Spring): one Real m/s strength parameter whose range keeps the reference-mass preload at
    // or below 200 J, a positive reference mass, a 0–64 m/s threshold, a 9–14400-step cooldown and a static, slider-free owner.
    [Fact]
    public void ContactWorkCapabilityRequiresOneTypedStrengthWithinTheTwoHundredJouleStore()
    {
        var strength = new ParameterDescriptor("strength", ParameterUnit.MetrePerSecond, ParameterKind.Real, 0f, 20f, 8f, [], ParameterEditorPolicy.Clamp);
        var entry = Bundle.BundleSchema.Entry(new Fixture { Parameters = [strength] });
        var work = new Abstractions.Mechanical.ContactWorkDescriptor("strength", 1f, .05f, 72);
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

    [Fact]
    public void ContactCapabilityRequiresOneTypedParameterAndPulseOutput()
    {
        var entry = Bundle.BundleSchema.Entry(new Fixture
        {
            Parameters = [new("speed", ParameterUnit.MetrePerSecond, ParameterKind.Real, 0f, 64f, .8f, [], ParameterEditorPolicy.Clamp)],
            Ports = [new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX)]
        }) with { Domains = [SimulationDomain.Signal], Mechanical = null,
            Signal = new([], SignalLatchMode.None, null, null, new("speed")) };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry candidate) => Bundle.BundleValidator.Validate([new("Fixture", candidate)]);
        Assert.Empty(Check(entry));
        Assert.NotEmpty(Check(entry with { Signal = entry.Signal! with { Contact = new("unknown") } }));
        Assert.NotEmpty(Check(entry with { Ports = [] }));
        Assert.NotEmpty(Check(entry with { Signal = entry.Signal! with { LatchMode = SignalLatchMode.SetReset } }));
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
                new(PortSocket.ActivationIn, SimulationDomain.Signal, PortDirection.Input, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX),
                new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse, Vector3.Zero, Vector3.UnitX)]
        }) with
        {
            Domains = [SimulationDomain.Signal], Mechanical = null,
            Signal = new([], Abstractions.Signal.SignalLatchMode.None, null, new("seconds", TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation))
        };
        static IReadOnlyList<Bundle.BundleError> Check(Bundle.BundleEntry entry) =>
            Bundle.BundleValidator.Validate([new("Fixture", entry)]);
        Assert.Empty(Check(timer));
        foreach (var policy in new[]
        {
            new SignalTimerDescriptor("seconds", (TimerCompletionPolicy)255, TimerBoundary.BeforePhysics, TimerElapsedSignal.Activation),
            new SignalTimerDescriptor("seconds", TimerCompletionPolicy.Latch, (TimerBoundary)255, TimerElapsedSignal.Activation),
            new SignalTimerDescriptor("seconds", TimerCompletionPolicy.Latch, TimerBoundary.BeforePhysics, (TimerElapsedSignal)255),
            new SignalTimerDescriptor("seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforePhysics, TimerElapsedSignal.None),
            new SignalTimerDescriptor("seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.Activation)
        })
            Assert.NotEmpty(Check(timer with { Signal = timer.Signal! with { Timer = policy } }));
        var silent = timer with
        {
            Ports = timer.Ports.Where(port => port.Socket != PortSocket.ActivationOut).ToArray(),
            Signal = timer.Signal! with { Timer = new("seconds", TimerCompletionPolicy.Rearm, TimerBoundary.BeforeNetworks, TimerElapsedSignal.None) }
        };
        Assert.Empty(Check(silent));
        Assert.NotEmpty(Check(silent with { Ports = [] }));
        Assert.NotEmpty(Check(timer with { Signal = timer.Signal! with { Timer = timer.Signal.Timer!.Value with { DurationParameter = "missing" } } }));
        Assert.NotEmpty(Check(timer with { Ports = [] }));
        Assert.NotEmpty(Check(timer with { Metadata = timer.Metadata with
            { Parameters = [new("seconds", ParameterUnit.Metre, ParameterKind.Real, .1f, 12f, 1f, [])] } }));
        var supply = timer with
        {
            Domains = [SimulationDomain.Electrical], Signal = null,
            Metadata = timer.Metadata with { Parameters = StoreParameters },
            Ports = [new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, Vector3.Zero, Vector3.UnitX)],
            Electrical = new([new("source", Abstractions.Electrical.ElectricalRole.Source, 12f, .1f, 0f, 100f)],
                new(Abstractions.Electrical.SupplyCapabilityKind.Source, null, PortSocket.Supply, Store: Store))
        };
        var logic=supply with
        {
            Metadata=timer.Metadata,
            Ports=[
                new(PortSocket.FirstIn,SimulationDomain.Electrical,PortDirection.Input,null,Vector3.Zero,Vector3.UnitX),
                new(PortSocket.SecondIn,SimulationDomain.Electrical,PortDirection.Input,null,Vector3.Zero,Vector3.UnitX),
                new(PortSocket.PowerIn,SimulationDomain.Electrical,PortDirection.Input,null,Vector3.Zero,Vector3.UnitX),
                new(PortSocket.Supply,SimulationDomain.Electrical,PortDirection.Output,null,Vector3.Zero,Vector3.UnitX)],
            Electrical=new([new("logic",Abstractions.Electrical.ElectricalRole.LogicGate,12f,.1f,0f,100f)],
                new(Abstractions.Electrical.SupplyCapabilityKind.LogicGate,PortSocket.PowerIn,PortSocket.Supply,Abstractions.Electrical.LogicGateOperation.And))
        };
        Assert.Empty(Check(logic));
        Assert.NotEmpty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=null}}}));
        Assert.Empty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=Abstractions.Electrical.LogicGateOperation.Nand}}}));
        Assert.Empty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=Abstractions.Electrical.LogicGateOperation.Nor}}}));
        Assert.Empty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=Abstractions.Electrical.LogicGateOperation.Or}}}));
        Assert.Empty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=Abstractions.Electrical.LogicGateOperation.Xor}}}));
        Assert.NotEmpty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=(Abstractions.Electrical.LogicGateOperation)5}}}));
        Assert.NotEmpty(Check(logic with {Electrical=logic.Electrical! with {Supply=logic.Electrical.Supply!.Value with {Operation=(Abstractions.Electrical.LogicGateOperation)255}}}));
        Assert.NotEmpty(Check(logic with {Ports=logic.Ports.Where(port=>port.Socket!=PortSocket.SecondIn).ToArray()}));
        Assert.NotEmpty(Check(supply with {Electrical=supply.Electrical! with {Supply=supply.Electrical.Supply!.Value with {Operation=Abstractions.Electrical.LogicGateOperation.And}}}));
        Assert.Empty(Check(supply));
        Assert.NotEmpty(Check(supply with { Electrical = supply.Electrical! with
            { Supply = new((Abstractions.Electrical.SupplyCapabilityKind)255, null, PortSocket.Supply) } }));
        Assert.NotEmpty(Check(supply with { Electrical = supply.Electrical! with
            { Supply = new(Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply) } }));
        Assert.NotEmpty(Check(supply with { Ports = [] }));
        Assert.NotEmpty(Check(supply with { Electrical = supply.Electrical! with { Nodes = [] } }));
    }
    private static string[] Errors(params (string Package, IPuzzleElementDeclaration Declaration)[] packages) =>
        PackageValidator.Validate(packages.Select(package => new BundlePackage(package.Package, package.Declaration)).ToArray())
            .Select(error => error.ToString()).ToArray();

    // Story 9A.39: a Source declares its finite store by four of its own parameters, inside the electrical source envelope.
    private static readonly ParameterDescriptor[] StoreParameters =
    [
        new("capacity", ParameterUnit.Joule, ParameterKind.Real, 60f, 14400f, 3600f, [], ParameterEditorPolicy.Clamp),
        new("power_limit", ParameterUnit.Watt, ParameterKind.Real, 10f, 480f, 120f, [], ParameterEditorPolicy.Clamp),
        new("initial_charge", ParameterUnit.None, ParameterKind.Real, 0f, 1f, 1f, [], ParameterEditorPolicy.Clamp),
        new("enabled", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl)
    ];
    private static readonly Abstractions.Electrical.SupplySourceStoreDescriptor Store = new("capacity", "power_limit", "initial_charge", "enabled");

    private static Bundle.BundleEntry Source(IReadOnlyList<ParameterDescriptor> parameters, Abstractions.Electrical.SupplySourceStoreDescriptor? store)
    {
        var entry = Bundle.BundleSchema.Entry(new Fixture { Parameters = parameters });
        return entry with
        {
            Domains = [SimulationDomain.Mechanical, SimulationDomain.Electrical],
            Ports = [new(PortSocket.Supply, SimulationDomain.Electrical, PortDirection.Output, null, Vector3.Zero, Vector3.UnitX)],
            Electrical = new([new("source", Abstractions.Electrical.ElectricalRole.Source, 12f, .1f, 0f, 100f)],
                new(Abstractions.Electrical.SupplyCapabilityKind.Source, null, PortSocket.Supply, Store: store))
        };
    }

    private static IReadOnlyList<Bundle.BundleError> CheckSource(IReadOnlyList<ParameterDescriptor> parameters,
        Abstractions.Electrical.SupplySourceStoreDescriptor? store) => Bundle.BundleValidator.Validate([new("Fixture", Source(parameters, store))]);

    private static ParameterDescriptor[] Replace(string key, Func<ParameterDescriptor, ParameterDescriptor> change) =>
        StoreParameters.Select(parameter => parameter.Key == key ? change(parameter) : parameter).ToArray();

    [Fact]
    public void ASourceStoreNamesFourDistinctTypedParametersInsideTheEnvelope()
    {
        Assert.Empty(CheckSource(StoreParameters, Store));
        // The envelope's own bounds are admitted.
        Assert.Empty(CheckSource(Replace("power_limit", p => p with { Minimum = 10f, Maximum = 480f, Default = 10f }), Store));
        foreach (var (name, parameters, store) in new (string, ParameterDescriptor[], Abstractions.Electrical.SupplySourceStoreDescriptor?)[]
        {
            ("missing store", StoreParameters, null),
            ("absent capacity key", StoreParameters.Where(p => p.Key != "capacity").ToArray(), Store),
            ("unknown key", StoreParameters, Store with { PowerLimit = "power" }),
            ("duplicated key", [.. StoreParameters, StoreParameters[0]], Store),
            ("two roles share one key", StoreParameters, Store with { InitialCharge = "capacity" }),
            ("capacity in watts", Replace("capacity", p => p with { Unit = ParameterUnit.Watt }), Store),
            ("capacity below 60 J", Replace("capacity", p => p with { Minimum = 59f }), Store),
            ("capacity above 14400 J", Replace("capacity", p => p with { Maximum = 14401f }), Store),
            ("power in joules", Replace("power_limit", p => p with { Unit = ParameterUnit.Joule }), Store),
            ("power below 10 W", Replace("power_limit", p => p with { Minimum = 9.99f }), Store),
            ("power above 480 W", Replace("power_limit", p => p with { Maximum = 480.5f }), Store),
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
        // A store on anything but a Source is refused.
        var gate = Source(StoreParameters, Store);
        Assert.NotEmpty(Bundle.BundleValidator.Validate([new("Fixture", gate with { Electrical = gate.Electrical! with
            { Supply = new(Abstractions.Electrical.SupplyCapabilityKind.SwitchedContact, PortSocket.PowerIn, PortSocket.Supply, Store: Store) } })]));
    }

    [Fact]
    public void ARunControlIsDeclaredOnlyOnASourceStoreEnableAndAChoiceIsNeverClamped()
    {
        Assert.Empty(CheckSource(StoreParameters, Store));
        // The enable state may also be build-only: the package then offers no run-time control.
        Assert.Empty(CheckSource(Replace("enabled", p => p with { RunEdit = ParameterRunEdit.BuildOnly }), Store));
        Assert.Contains(CheckSource(Replace("capacity", p => p with { RunEdit = ParameterRunEdit.RunControl }), Store),
            error => error.Detail.Contains("Metadata.Parameters[0].RunEdit", StringComparison.Ordinal));
        Assert.NotEmpty(CheckSource(Replace("enabled", p => p with { RunEdit = (ParameterRunEdit)7 }), Store));
        Assert.Contains(CheckSource(Replace("enabled", p => p with { EditorPolicy = ParameterEditorPolicy.Clamp }), Store),
            error => error.Detail.Contains("Metadata.Parameters[3].EditorPolicy", StringComparison.Ordinal));
        // Off a Source: no parameter may be run-editable.
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
        PortDescriptor Port(Vector3 normal) => new(PortSocket.ActivationOut, SimulationDomain.Signal, PortDirection.Output, PortSignalKind.Pulse,
            Vector3.Zero, normal);
        Assert.Empty(Errors(("Fixture", new Fixture { Ports = [Port(-Vector3.UnitY)] })));
        Assert.Equal(["BUNDLE006 FixtureNormal.Ports[0].Normal: outside its declared range"],
            Errors(("FixtureNormal", new Fixture { Ports = [Port(new(0f, 2f, 0f))] })));
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
        Assert.Equal(["BUNDLE002 FixtureMass.MassProperties.Mass: not finite"],
            Errors(("FixtureMass", new Fixture { MassProperties = new(float.PositiveInfinity, Vector3.Zero, Vector3.Zero, 0f, Vector3.Zero) })));

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
            Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0), Colliders = [] })));

    [Fact]
    public void ANonMechanicalPackageWithColliderOrWithoutBodyIsAdmitted()
    {
        Assert.Empty(Errors(("FixtureLamp", new SignalFixture { Footprint = new(1, 0, 0) })));
        Assert.Empty(Errors(("FixtureSignal", new SignalFixture { Footprint = new(0, 0, 0), Colliders = [] })));
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
        Assert.Equal(["BUNDLE002 FixtureSlider.Sliders[0].MovingBody.Mass: not finite"],
            Errors(("FixtureSlider", new Fixture
            {
                Parameters = Fixture.SpringParameters,
                Sliders = [Fixture.Slider() with { MovingBody = Fixture.Slider().MovingBody with { Mass = float.NaN } }]
            })));

    // C-Gate (9A.19): a supplied drive is declaration data; the bundle refuses one the runtime could not admit.
    private static string[] DriveErrors(Abstractions.Mechanical.PrismaticDriveDescriptor drive, bool powerIn = true, int sliders = 1) =>
        Errors(("FixtureDrive", new Fixture
        {
            Parameters = Fixture.SpringParameters,
            Ports = powerIn ? [Fixture.PowerIn] : [],
            Sliders = Enumerable.Repeat(Fixture.Slider() with { Drive = drive }, sliders).ToArray()
        }));

    [Fact]
    public void ADrivenSliderWithAPowerInIsAdmitted() => Assert.Empty(DriveErrors(Fixture.Drive()));

    [Fact]
    public void ADrivenSliderWithoutAPowerInIsRefused() =>
        Assert.Equal(["BUNDLE007 FixtureDrive.Sliders[0].Drive: a supplied drive requires an electrical PowerIn input"],
            DriveErrors(Fixture.Drive(), powerIn: false));

    [Fact]
    public void OnePowerInDrivesOneSlider() =>
        Assert.Equal(["BUNDLE006 FixtureDrive.Sliders[1].Drive: outside its declared range"], DriveErrors(Fixture.Drive(), sliders: 2));

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

    [Fact]
    public void AnAssemblyWithNoDeclarationIsRefused() =>
        Assert.Equal(["BUNDLE005 FixtureNone: declares 0 elements; a package declares exactly one"],
            PackageDiscovery.FromAssembly("FixtureNone", typeof(IPuzzleElementDeclaration).Assembly).Errors.Select(error => error.ToString()));
}
