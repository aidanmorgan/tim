using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class ChamberContractTests
{

    [Theory]
    [InlineData(PresentationProperty.ScaleX, 2f, 1f, 0f, true, 2f)]
    [InlineData(PresentationProperty.ScaleX, 2f, 1f, 1f, true, 1f)]
    [InlineData(PresentationProperty.ScaleX, 2f, 1f, 2f, false, 0f)]
    [InlineData(PresentationProperty.ScaleX, 2f, 1f, 3f, false, -1f)]
    [InlineData(PresentationProperty.AlbedoRed, .25f, .75f, -1f, false, -.25f)]
    [InlineData(PresentationProperty.AlbedoRed, .25f, .75f, 2f, false, 1.25f)]
    [InlineData(PresentationProperty.AlbedoRed, .25f, .75f, -.5f, true, 0f)]
    [InlineData(PresentationProperty.AlbedoRed, .25f, .75f, 1.5f, true, 1f)]
    [InlineData(PresentationProperty.Visible, 1f, 0f, 0f, true, 1f)]
    [InlineData(PresentationProperty.Visible, 1f, 0f, 1f, true, 0f)]
    [InlineData(PresentationProperty.Visible, 1f, 0f, .5f, false, .5f)]
    [InlineData(PresentationProperty.Visible, 1f, 0f, 2f, false, -1f)]
    public void DiscretePropertyAdmissionChecksTheMappedOutput(
        PresentationProperty property, float from, float to, float state, bool admitted, float expected)
    {
        var entry = Entry();
        var values = new PresentationFiniteWorkValues(state, state, state, state, state, state);
        var binding = new PresentationBinding("Visual/First", property,
            PresentationDriver.CommittedFiniteWorkState, PresentationValueMapping.StateValues,
            from, to, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
            FiniteWork: new(3), FiniteWorkValues: values);
        var errors = BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Presentation = entry.Presentation with { Bindings = [binding] }
        })]);
        if (admitted) Assert.Empty(errors);
        else Assert.Contains("Presentation.FiniteWorkValues", Assert.Single(errors).Detail, StringComparison.Ordinal);
        var compiled = new CompiledPresentationBinding(binding.Property, binding.Driver, binding.Mapping,
            binding.From, binding.To, binding.Curve, binding.Duration, binding.Overlap, 0f, 0f, null,
            SimulationEventKind.None, PresentationEventMagnitude.Any, FiniteWorkValues: values);
        Assert.Equal(admitted, PresentationDriverRules.ValidPropertyValues(compiled));
        Assert.Equal(expected, PresentationDriverRules.InitialValue(compiled));

        var fixture = new LawScene(false);
        var frame = fixture.StaticBox(new(0f, 3f, 0f), new(.1f), colliderOffset: new(0f, -2f, 0f));
        var payload = fixture.Sphere(new(0f, 3f, 0f), .35f, 1f, new(0, 0, 0, 0), Vector3.Zero, Vector3.Zero, 0f, 0f);
        var actuator = fixture.FiniteWork(frame, payload, 45f, 45f);
        var world = fixture.Admit();
        var store = world.Scene.FiniteWorkActuators[0].Store;
        ICompiledScene Admit() => RuntimeTestServices.Core.CreateScene(new(1), new(1), WorkshopCadenceSettings.Default(),
            world.Scene, RuntimeTestServices.Core.CreateActivation([], []),
            [new(new(WorkshopDeclaredPresentation.TargetBase), frame, 0, compiled, default, 0f, FiniteWork: actuator, Store: store)],
            world.Scene.Bodies.ToArray().Select(body => body.Id), []);
        if (!admitted) Assert.Throws<ArgumentException>(Admit);
        else
        {
            var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), default, Admit(),
                Session: new(101, 206), Cadence: new(1), Projection: new(1));
            var roundTrip = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command));
            Assert.Equal(compiled, roundTrip.Scene!.Presentation[0].Declaration);
            Assert.Equal(expected, PresentationDriverRules.InitialValue(roundTrip.Scene.Presentation[0].Declaration));
        }

        // These are scale-domain interface cases: normalized zero is a valid two-times scale;
        // positive extrapolation must not admit zero/negative rendered dimensions.
    }


    [Fact]
    public void FractionAndAvailabilityRejectMissingForeignAndInactiveSelectors()
    {
        var entry = Entry();
        var fraction = new PresentationBinding("Visual/First", PresentationProperty.ScaleX,
            PresentationDriver.CommittedStoredFraction, PresentationValueMapping.ClampToEndpoints,
            .001f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f, Store: new(9));
        var state = fraction with { Property = PresentationProperty.LocalX, Driver = PresentationDriver.CommittedFiniteWorkState,
            Mapping = PresentationValueMapping.StateValues, From = 0f, To = 1f, Store = null, FiniteWork = new(3),
            FiniteWorkValues = new(0f, 1f, 2f, 3f, 4f, 5f) };
        IReadOnlyList<BundleError> Check(PresentationBinding binding) => BundleValidator.Validate(
            [new(entry.Metadata.Id, entry with { Presentation = entry.Presentation with { Bindings = [binding] } })]);
        Assert.Empty(Check(fraction));
        Assert.Empty(Check(state));
        foreach (var invalid in new[]
        {
            fraction with { Store = null }, fraction with { Store = new(99) }, fraction with { FiniteWork = new(3) },
            state with { FiniteWork = null }, state with { FiniteWork = new(99) }, state with { Store = new(9) },
            state with { FiniteWorkValues = null },
            state with { FiniteWorkValues = new(0f, 1f, 2f, 3f, 4f, float.NaN) },
            fraction with { FiniteWorkValues = state.FiniteWorkValues }
        }) Assert.NotEmpty(Check(invalid));
    }

    private static BundleEntry Entry(bool reverse = false)
    {
        var wall = TestBundle.Entry(new("wall"));
        EnergyStoreDescriptor[] stores =
        [
            new(new(9), new(0), EnergyExpression.Direct(MechanicalValue.Fixed(45f)), EnergyExpression.Direct(MechanicalValue.Fixed(0f)), MechanicalValue.Fixed(30f)),
            new(new(3), new(0), EnergyExpression.Direct(MechanicalValue.Fixed(70f)), EnergyExpression.Direct(MechanicalValue.Fixed(7f)), MechanicalValue.Fixed(4f))
        ];
        ChamberRegionDescriptor[] regions =
        [
            new(new(9), new(0), new(0), new(-.2f, 0f, 0f), Quaternion.Identity, .48f, .55f, .6f, .84f, .001f, 1f),
            new(new(3), new(0), new(0), new(.2f, 0f, 0f), Quaternion.Identity, .8f, .9f, .2f, .7f, .002f, .25f)
        ];
        FiniteWorkActuatorDescriptor[] actuators =
        [
            new(new(9), new(3), new(9), MechanicalValue.Fixed(7f), .07f),
            new(new(3), new(9), new(3), MechanicalValue.Fixed(12f), .02f)
        ];
        if (reverse) { Array.Reverse(stores); Array.Reverse(regions); Array.Reverse(actuators); }
        return wall with
        {
            Metadata = wall.Metadata with { Id = "chamber_contract" },
            Events = [new(SimulationEventKind.FiniteWorkReleased, FiniteWork: new(9)), new(SimulationEventKind.FiniteWorkReleased, FiniteWork: new(3))],
            Presentation = wall.Presentation with { Bindings = [
                new("Visual/First", PresentationProperty.LocalX, PresentationDriver.Event, PresentationValueMapping.Identity,
                    0f, 1f, PresentationCurve.SmoothStep, 1f, PresentationOverlap.Replace, 0f,
                    Event: SimulationEventKind.FiniteWorkReleased, FiniteWork: new(3)),
                new("Visual/Second", PresentationProperty.LocalX, PresentationDriver.Event, PresentationValueMapping.Identity,
                    0f, 1f, PresentationCurve.SmoothStep, 1f, PresentationOverlap.Replace, 0f,
                    Event: SimulationEventKind.FiniteWorkReleased, FiniteWork: new(9))] },
            Mechanical = wall.Mechanical! with { EnergyStores = stores, ChamberRegions = regions, FiniteWorkActuators = actuators }
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactRegionAndActuatorReferencesSurviveProductionTransportAndReset(bool reverse)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(reverse);
        Assert.Empty(BundleValidator.Validate([new(entry.Metadata.Id, entry)]));
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(new PartId(entry.Metadata.Id), entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(11), 0, 3, 0, 0, 0, 0, 1, WallFixture.Default);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([instance]));
        var ids = construction.Instances.Identities[instance.Id];
        var scene = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(19, 23), new(2));
        var region = Assert.Single(scene.Physics.ChamberRegions.ToArray(), value => value.Id == ids.ChamberRegions[new(3)]);
        Assert.Equal(.8f, region.Radius); Assert.Equal(.9f, region.HalfLength);
        Assert.Equal(.2f, region.MinimumWidth); Assert.Equal(.7f, region.MaximumWidth);
        var actuator = Assert.Single(scene.Physics.FiniteWorkActuators.ToArray(), value => value.Id == ids.FiniteWorkActuators[new(3)]);
        Assert.Equal(ids.EnergyStores[new(9)], actuator.Store); Assert.Equal(region.Id, actuator.Region);
        Assert.Equal(12f, actuator.TargetSpeed.Value); Assert.Equal(.02f, actuator.ExitPadding);
        Assert.Equal(ids.FiniteWorkActuators[new(3)], scene.Presentation[0].FiniteWork);
        Assert.Equal(ids.EnergyStores[new(9)], scene.Presentation[0].Store);
        Assert.Equal(ids.FiniteWorkActuators[new(9)], scene.Presentation[1].FiniteWork);
        Assert.Equal(ids.EnergyStores[new(3)], scene.Presentation[1].Store);
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(3);
        AssertRead(harness.Committed, ids);
        await harness.Require(WorkshopCommandKind.Reset);
        await harness.Run();
        AssertRead(harness.Committed, ids);
    }

    private static void AssertRead(WorkshopRead read, CompiledPartIdentities ids)
    {
        Assert.Equal(2, read.ChamberRegions.Count); Assert.Equal(2, read.FiniteWorkActuators.Count);
        var actuator = Assert.Single(Enumerable.Range(0, read.FiniteWorkActuators.Count).Select(i => read.FiniteWorkActuators[i]),
            value => value.Id == ids.FiniteWorkActuators[new(3)]);
        Assert.Equal(ids.EnergyStores[new(9)], actuator.Store); Assert.Equal(ids.ChamberRegions[new(3)], actuator.Region);
        Assert.Equal(0u, actuator.OccurrenceCount); Assert.Null(actuator.LastResult); Assert.Equal(0f, actuator.LastDebit.Value);
        for (var i = 0; i < read.ChamberRegions.Count; i++)
        {
            Assert.Equal(ChamberPhase.Empty, read.ChamberRegions[i].Phase);
            Assert.Equal(0u, read.ChamberRegions[i].Occupancy);
        }
    }

    [Fact]
    public void DischargeEventsRequireExactDeclaredActuatorSelectors()
    {
        var entry = Entry();
        Assert.Empty(BundleValidator.Validate([new(entry.Metadata.Id, entry)]));
        foreach (var events in new EventDescriptor[][]
        {
            [new(SimulationEventKind.FiniteWorkReleased)],
            [new(SimulationEventKind.FiniteWorkReleased, FiniteWork: new(99))],
            [entry.Events[0], entry.Events[0]],
            [new(SimulationEventKind.Impact, FiniteWork: new(3))]
        })
            Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with { Events = events, Presentation = entry.Presentation with { Bindings = [] } })]));
        foreach (var selector in new LocalFiniteWorkActuatorId?[] { null, new(99) })
            Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
            {
                Presentation = entry.Presentation with { Bindings = [entry.Presentation.Bindings[0] with { FiniteWork = selector }] }
            })]));
    }

    [Fact]
    public void MissingOrWrongKindLocalReferencesRefuseBeforeCompilation()
    {
        var entry = Entry();
        var mechanical = entry.Mechanical!;
        Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Mechanical = mechanical with { FiniteWorkActuators = [mechanical.FiniteWorkActuators[0] with { Store = new(99) }] }
        })]));
        Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Mechanical = mechanical with { ChamberRegions = [mechanical.ChamberRegions[0] with { ParticipationCollider = new(99) }] }
        })]));
        Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Mechanical = mechanical with { ChamberRegions = [mechanical.ChamberRegions[0], mechanical.ChamberRegions[0]] }
        })]));
        Assert.NotEmpty(BundleValidator.Validate([new(entry.Metadata.Id, entry with
        {
            Mechanical = mechanical with { ChamberRegions = [mechanical.ChamberRegions[0] with { MaximumRelativeSpeed = float.NaN }] }
        })]));
    }
}
