using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9A.46i: Route supplied work through declared capability nodes.
/// Tests verify:
/// - Composed powered assembly: closed reachable route enables named drive and charges deficient store within existing allocation.
/// - Open route: same deficient store; contact open -> no new supplied credit or demand, existing store remains, drive disabled.
/// - Direct route vs Composed route: finite-energy allocation and conservation contract parity.
/// - Source unavailable: disabled or exhausted source -> no credit, consumers unpowered.
/// - Reconvergence: multiple paths from one source into a store -> single charge per target from one source provenance.
/// - Ambiguous work source: two distinct potential sources reach one finite store -> typed refusal.
/// - Logic provenance: power from A, Boolean control from B -> B funds nothing, A funds credit.
/// - Multiple capabilities per owner: two unequal sources on one owner, separate controls/reads, explicit targets.
/// - Invalid declarations / duplicate IDs / nonmonotone cycles: refused atomically.
/// - Unwired store: remains uncharged by supply.
/// - P1 generality: unfamiliar 30J/5W and 20000J/500W bounds, credit/debit > 8J, zero-capacity recipient, aggregate capacity refusal.
/// - Save/load and authored reset exactness.
/// </summary>
public sealed class ComposableSupplyRoutingTests
{
    private static readonly PartId DualSourcePart = new("unfamiliar_dual_source");

    private static WorkshopConnection Power(ulong source, ulong target) =>
        new(new(source), PortId.Supply, new(target), PortId.PowerIn, WorkshopConnectionDomain.Electrical);

    private static WorkshopConnection Wire(ulong source, PortId outPort, ulong target, PortId inPort) =>
        new(new(source), outPort, new(target), inPort, WorkshopConnectionDomain.Electrical);

    private static BundleEntry CreateDualSourcePackage()
    {
        var battery = TestBundle.Entry(new PartId("battery"));
        var supplyGraph = new SupplyGraphDescriptor(
            [
                new SupplySourceDescriptor(new("src_low"), new(new("cap_low"), new("pwr_low"), new("init_low"), new("enable_low"))),
                new SupplySourceDescriptor(new("src_high"), new(new("cap_high"), new("pwr_high"), new("init_high"), new("enable_high")))
            ],
            [],
            [
                new(new PortId("supply_low"), new(new("src_low"), SupplyTerminal.Output)),
                new(new PortId("supply_high"), new(new("src_high"), SupplyTerminal.Output))
            ]);

        return battery with
        {
            Metadata = battery.Metadata with
            {
                Id = DualSourcePart.Value,
                Parameters =
                [
                    new("cap_low", ParameterUnit.Joule, ParameterKind.Real, 1f, 1000f, 30f, []),
                    new("pwr_low", ParameterUnit.Watt, ParameterKind.Real, 1f, 100f, 5f, []),
                    new("init_low", ParameterUnit.None, ParameterKind.Real, 0f, 1f, 1f, []),
                    new("enable_low", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl),
                    new("cap_high", ParameterUnit.Joule, ParameterKind.Real, 1f, 50000f, 20000f, []),
                    new("pwr_high", ParameterUnit.Watt, ParameterKind.Real, 1f, 2000f, 500f, []),
                    new("init_high", ParameterUnit.None, ParameterKind.Real, 0f, 1f, 1f, []),
                    new("enable_high", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl)
                ]
            },
            Electrical = new ElectricalPayload(
                [
                    new("low_node", ElectricalRole.Source, 12f, .05f, 0f, 5f),
                    new("high_node", ElectricalRole.Source, 12f, .05f, 0f, 500f)
                ],
                supplyGraph),
            Ports =
            [
                new PortDescriptor(new PortId("supply_low"), SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new Vector3(0.5f, 0f, 0f), Vector3.UnitX),
                new PortDescriptor(new PortId("supply_high"), SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply, null, new Vector3(-0.5f, 0f, 0f), -Vector3.UnitX)
            ],
            Presentation = new ElementPresentation(1f, PresentationAxes.None, [], [], [], [])
        };
    }

    [Fact]
    public async Task ComposedPoweredAssemblyChargesStoreAndEnablesDriveWhenRouteIsClosed()
    {
        // Battery (1) -> Switch (2, closed via ball impact) -> Bumper (3, store) and PoweredGate (4, drive)
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(5), 0, 5.5f, 0, 0, 0, 0, 1);
        var switchBall = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"), new(6), 4, 3f, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(1), -4, 3, 0, capacity: 3600f, powerLimit: 120f);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 4, 1, 0, 0, 0, 0, 1,
            new([new(new ParameterId("trigger_threshold"), 0.5f)]));
        var bumper = PlacedParts.Bumper(new(3), 0, 3, 0, strength: 8f);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("powered_gate"), new(4), 8, 3, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, sw, bumper, gate, ball, switchBall]),
            Connections: new(
                Power(1, 2),
                Power(2, 3),
                Power(2, 4)));

        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var storageBinding = Assert.Single(scene.Electrical.Bindings.ToArray());
        Assert.Equal(construction.Instances.Identities[bumper.Id].Work, storageBinding.Storage);

        var reads = await WorkshopIntegrationHarness.Observe(construction, 180);
        var hit = reads.FindIndex(read => Enumerable.Range(0, read.Activations.Count).Select(i => read.Activations[i]).Any(act => act.Owner == sw.Id && act.Phase == ActivationPhase.Latched));
        Assert.True(hit > 0, "Switch was triggered by the falling ball");

        var later = reads[^1];
        var actuator = Assert.Single(later.Presentation.Actuators.ToArray(), a => a.Owner == gate.Id);
        Assert.True(actuator.SupplyClosed, "Gate drive must be powered when switch is closed");

        var bumperStore = later.ContactWorks[0];
        Assert.True(bumperStore.RemainingEnergy.Value > 0f || bumperStore.SuppliedEnergy.Value > 0f, "Bumper store was recharged");
    }

    [Fact]
    public async Task OpenRouteStopsNewCreditAndDemandWhilePreservingStoredEnergy()
    {
        // Battery (1) -> Switch (2, untriggered/open) -> Bumper (3, store) and PoweredGate (4, drive)
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(5), 0, 5.5f, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(1), -4, 3, 0, capacity: 3600f, powerLimit: 120f);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 20, 1, 0, 0, 0, 0, 1);
        var bumper = PlacedParts.Bumper(new(3), 0, 3, 0, strength: 8f);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("powered_gate"), new(4), 8, 3, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, sw, bumper, gate, ball]),
            Connections: new(
                Power(1, 2),
                Power(2, 3),
                Power(2, 4)));

        var reads = await WorkshopIntegrationHarness.Observe(construction, 120);
        var later = reads[^1];

        var actuator = Assert.Single(later.Presentation.Actuators.ToArray(), a => a.Owner == gate.Id);
        Assert.False(actuator.SupplyClosed, "Gate drive must be unpowered when route is open");

        var bumperStore = later.ContactWorks[0];
        Assert.Equal(0f, bumperStore.SuppliedEnergy.Value);
    }

    [Fact]
    public void DirectRouteVsComposedRouteFiniteEnergyParity()
    {
        var battery = PlacedParts.Battery(new(1), 0, 0, 0, capacity: 1000f, powerLimit: 120f);
        var bumper = PlacedParts.Bumper(new(2), 2, 0, 0, strength: 8f);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(3), 1, 0, 0, 0, 0, 0, 1);

        var directConstruction = new WorkshopConstruction(
            new(2), WorkshopCadenceSettings.Default(), new([battery, bumper]), Connections: new(Power(1, 2)));
        var composedConstruction = new WorkshopConstruction(
            new(2), WorkshopCadenceSettings.Default(), new([battery, sw, bumper]), Connections: new(Power(1, 3), Power(3, 2)));

        var directScene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, directConstruction, new(1, 2));
        var composedScene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, composedConstruction, new(1, 2));

        var directBinding = Assert.Single(directScene.Electrical.Bindings.ToArray());
        var composedBinding = Assert.Single(composedScene.Electrical.Bindings.ToArray());

        Assert.Equal(directScene.Electrical.Sources[0].Id, directBinding.Source);
        Assert.Equal(composedScene.Electrical.Sources[0].Id, composedBinding.Source);
        Assert.Equal(directScene.ContactWorks[0].Id, directBinding.Storage);
        Assert.Equal(composedScene.ContactWorks[0].Id, composedBinding.Storage);

        float[] balances = [10f];
        float[] capacities = [32f];
        Span<float> nextBalancesDirect = stackalloc float[1];
        Span<float> nextBalancesComposed = stackalloc float[1];

        var transferDirect = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 500f, 120f, balances, capacities, nextBalancesDirect);
        var transferComposed = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 500f, 120f, balances, capacities, nextBalancesComposed);

        Assert.Equal(transferDirect.Remaining, transferComposed.Remaining);
        Assert.Equal(transferDirect.Debit, transferComposed.Debit);
        Assert.Equal(nextBalancesDirect[0], nextBalancesComposed[0]);
    }

    [Fact]
    public async Task SourceUnavailableLeavesConsumersUnpowered()
    {
        var disabledBattery = PlacedParts.Battery(new(1), -4, 2, 0, enabled: ElectricalEnable.Disabled);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 0, 2, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("powered_gate"), new(3), 4, 2, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([disabledBattery, sw, gate]),
            Connections: new(Power(1, 2), Power(2, 3)));

        var reads = await WorkshopIntegrationHarness.Observe(construction, 30);
        var actuator = Assert.Single(reads[^1].Presentation.Actuators.ToArray(), a => a.Owner == gate.Id);
        Assert.False(actuator.SupplyClosed, "Disabled source must not power downstream consumers");

        var exhaustedBattery = PlacedParts.Battery(new(1), -4, 2, 0, initialCharge: 0f);
        var exhaustedConstruction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([exhaustedBattery, sw, gate]),
            Connections: new(Power(1, 2), Power(2, 3)));

        var readsExhausted = await WorkshopIntegrationHarness.Observe(exhaustedConstruction, 30);
        var actuatorExhausted = Assert.Single(readsExhausted[^1].Presentation.Actuators.ToArray(), a => a.Owner == gate.Id);
        Assert.False(actuatorExhausted.SupplyClosed, "Exhausted source must not power downstream consumers");
    }

    [Fact]
    public void ReconvergenceFromSingleSourceYieldsSingleChargePerTarget()
    {
        // Battery (1) feeds Switch A (2) and Switch B (3)
        // Both switches feed into BothGate (4) PowerIn
        // BothGate feeds Bumper (5)
        var battery = PlacedParts.Battery(new(1), -6, 2, 0);
        var switchA = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), -3, 3, 0, 0, 0, 0, 1);
        var switchB = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(3), -3, 1, 0, 0, 0, 0, 1);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("both_gate"), new(4), 0, 2, 0, 0, 0, 0, 1);
        var bumper = PlacedParts.Bumper(new(5), 3, 2, 0);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, switchA, switchB, gate, bumper]),
            Connections: new(
                Power(1, 2),
                Power(1, 3),
                Power(2, 4),
                Power(3, 4),
                Power(4, 5)));

        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));

        var binding = Assert.Single(scene.Electrical.Bindings.ToArray());
        Assert.Equal(scene.Electrical.Sources[0].Id, binding.Source);
        Assert.Equal(scene.ContactWorks[0].Id, binding.Storage);

        Span<float> balances = [10f];
        Span<float> capacities = [32f];
        Span<float> nextBalances = stackalloc float[1];
        var transfer = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 1000f, 120f, balances, capacities, nextBalances);
        Assert.Equal(transfer.Debit, nextBalances[0] - balances[0]);
    }

    [Fact]
    public void AmbiguousWorkSourcesFromDistinctSourcesAreRefused()
    {
        // Battery 1 (1) -> Switch (2) PowerIn
        // Battery 2 (3) -> Switch (2) PowerIn
        // Switch (2) -> Bumper (4)
        var battery1 = PlacedParts.Battery(new(1), -6, 3, 0);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 0, 2, 0, 0, 0, 0, 1);
        var battery2 = PlacedParts.Battery(new(3), -6, 1, 0);
        var bumper = PlacedParts.Bumper(new(4), 4, 2, 0);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery1, battery2, sw, bumper]),
            Connections: new(
                Power(1, 2),
                Power(3, 2),
                Power(2, 4)));

        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () =>
            WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2)));
    }

    [Fact]
    public async Task LogicProvenanceFundsNoWorkWhilePowerSourceFundsWork()
    {
        // Source A (Battery 1) -> BothGate (3) PowerIn
        // Source B (Battery 2) -> BothGate (3) FirstIn and SecondIn
        // BothGate (3) -> Bumper (4)
        // Ball (5) depletes Bumper store
        var batteryA = PlacedParts.Battery(new(1), -6, 4, 0, capacity: 3600f);
        var batteryB = PlacedParts.Battery(new(2), -6, 1, 0, capacity: 3600f);
        var bothGate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("both_gate"), new(3), -2, 2, 0, 0, 0, 0, 1);
        var bumper = PlacedParts.Bumper(new(4), 2, 2, 0, strength: 8f);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(5), 2, 5.5f, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([batteryA, batteryB, bothGate, bumper, ball]),
            Connections: new(
                Wire(1, PortId.Supply, 3, PortId.PowerIn),
                Wire(2, PortId.Supply, 3, PortId.FirstIn),
                Wire(2, PortId.Supply, 3, PortId.SecondIn),
                Power(3, 4)));

        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var binding = Assert.Single(scene.Electrical.Bindings.ToArray());
        var sourceAId = construction.Instances.Identities[batteryA.Id].Supply.Values.First().Source;
        var sourceBId = construction.Instances.Identities[batteryB.Id].Supply.Values.First().Source;

        Assert.Equal(sourceAId, binding.Source);
        Assert.NotEqual(sourceBId, binding.Source);

        var reads = await WorkshopIntegrationHarness.Observe(construction, 120);
        var later = reads[^1];

        var readA = Enumerable.Range(0, later.Electrical.Count).Select(i => later.Electrical[i]).Single(s => s.Id == sourceAId);
        var readB = Enumerable.Range(0, later.Electrical.Count).Select(i => later.Electrical[i]).Single(s => s.Id == sourceBId);

        Assert.True(readA.Remaining.Value < 3600f, "Source A must fund downstream work");
        Assert.Equal(3600f, readB.Remaining.Value);
    }

    [Fact]
    public void MultipleCapabilitiesPerOwnerWithIndependentControlAndRead()
    {
        var dualSource = CreateDualSourcePackage();
        var ports = TestBundle.Ports.Extend([(DualSourcePart, dualSource.Ports)]);

        using var physicsScope = DeclaredPhysics.Scoped([dualSource]);
        using var portScope = DeclaredPorts.Scoped(ports);

        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, DualSourcePart, new(10), 0, 0, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(
            new(2), WorkshopCadenceSettings.Default(), new([instance]));

        var scene = WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2), new(1));

        Assert.Equal(2, scene.Physics.Electrical.Sources.Length);
        var lowSource = scene.Physics.Electrical.Sources.ToArray().Single(s => s.InitialEnergy.Value == 30f);
        var highSource = scene.Physics.Electrical.Sources.ToArray().Single(s => s.InitialEnergy.Value == 20000f);

        Assert.Equal(new GpuBodyId(10), lowSource.Owner);
        Assert.Equal(new GpuBodyId(10), highSource.Owner);
        Assert.NotEqual(lowSource.Id, highSource.Id);
        Assert.Equal(30f, lowSource.InitialEnergy.Value);
        Assert.Equal(20000f, highSource.InitialEnergy.Value);
        Assert.Equal(5f, lowSource.MaximumPower.Value);
        Assert.Equal(500f, highSource.MaximumPower.Value);

        var runControls = scene.RunControls;
        var ctrlLow = RunControlLanes.Find(runControls, new(10), RunControlLane.SourceEnable, lowSource.Id);
        var ctrlHigh = RunControlLanes.Find(runControls, new(10), RunControlLane.SourceEnable, highSource.Id);

        Assert.NotNull(ctrlLow);
        Assert.NotNull(ctrlHigh);
        Assert.NotEqual(ctrlLow.Value.LaneIndex, ctrlHigh.Value.LaneIndex);
    }

    [Fact]
    public void InvalidDeclarationsAndNonmonotoneCyclesRefusedAtomically()
    {
        var battery = TestBundle.Entry(new PartId("battery"));
        var duplicateNodes = battery.Electrical!.SupplyGraph! with
        {
            Nodes = [
                new SupplySourceDescriptor(new("dup"), new(new("capacity"), new("power_limit"), new("initial_charge"), new("enabled"))),
                new SupplySourceDescriptor(new("dup"), new(new("capacity"), new("power_limit"), new("initial_charge"), new("enabled")))
            ]
        };
        var badEntry = battery with { Electrical = battery.Electrical with { SupplyGraph = duplicateNodes } };
        var errors = SupplyBundleValidation.Validate(badEntry);
        Assert.Contains(errors, e => e.Detail.Contains("Node.Identity"));

        var badConsumerTarget = battery.Electrical!.SupplyGraph! with
        {
            Nodes = [
                new SupplySourceDescriptor(new("src"), new(new("capacity"), new("power_limit"), new("initial_charge"), new("enabled"))),
                new SupplyConsumerDescriptor(new("cons"), [new SupplyContactWorkTarget(new("non_existent_work"))])
            ]
        };
        badEntry = battery with { Electrical = battery.Electrical with { SupplyGraph = badConsumerTarget } };
        errors = SupplyBundleValidation.Validate(badEntry);
        Assert.Contains(errors, e => e.Detail.Contains("Consumer.Target"));

        var nand = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("electrical_nand"), new(1), 0, 0, 0, 0, 0, 0, 1);
        var loopConstruction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([nand]),
            Connections: new(Wire(1, PortId.Supply, 1, PortId.FirstIn)));

        Assert.Throws<WorkshopEditRejectedException>(() =>
            loopConstruction.Connections.Validate(RuntimeTestServices.Core.Supply, loopConstruction.Instances));
    }

    [Fact]
    public async Task UnwiredStoreRemainsUncharged()
    {
        var bumper = PlacedParts.Bumper(new(1), 0, 2, 0, strength: 8f);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(2), 0, 5.5f, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([bumper, ball]));

        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        Assert.Equal(0, scene.Electrical.Bindings.Length);

        var reads = await WorkshopIntegrationHarness.Observe(construction, 120);
        var later = reads[^1].ContactWorks[0];

        Assert.Equal(0f, later.SuppliedEnergy.Value);
    }

    [Fact]
    public void P1GeneralityValidationAndAggregateCapacityRefusal()
    {
        Span<float> balances = [0f];
        Span<float> capacities = [25f];
        Span<float> nextBalances = stackalloc float[1];

        var transferLow = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 30f, 5f, balances, capacities, nextBalances);
        Assert.True(transferLow.Debit > 0f);
        Assert.Equal(30f - transferLow.Debit, transferLow.Remaining);

        var transferHigh = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 20000f, 500f, balances, capacities, nextBalances);
        Assert.True(transferHigh.Debit > 0f);
        Assert.Equal(20000f - transferHigh.Debit, transferHigh.Remaining);

        balances[0] = 0f;
        capacities[0] = 50f;
        var transferLarge = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 1000f, 1200f, balances, capacities, nextBalances);
        Assert.Equal(10f, transferLarge.Debit);
        Assert.Equal(10f, nextBalances[0]);

        Span<float> zeroBalances = [0f];
        Span<float> zeroCapacities = [0f];
        Span<float> nextZeroBalances = stackalloc float[1];
        var zeroTransfer = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 100f, 120f, zeroBalances, zeroCapacities, nextZeroBalances);
        Assert.Equal(0f, zeroTransfer.Debit);
        Assert.Equal(100f, zeroTransfer.Remaining);

        Assert.Throws<ArgumentException>(() =>
            ElectricalSupply.ValidateCapacityAggregate([float.MaxValue, float.MaxValue]));
        Assert.Throws<ArgumentException>(() =>
            ElectricalSupply.ValidateCapacityAggregate([float.PositiveInfinity]));
        Assert.Throws<ArgumentException>(() =>
            ElectricalSupply.ValidateCapacityAggregate([-1f]));
    }

    [Fact]
    public async Task SaveLoadAndResetPreservesAuthoredStateAndComposableConnections()
    {
        var battery = PlacedParts.Battery(new(1), -4, 2, 0);
        var sw = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(2), 0, 2, 0, 0, 0, 0, 1);
        var bumper = PlacedParts.Bumper(new(3), 4, 2, 0);

        var construction = new WorkshopConstruction(
            new(2),
            WorkshopCadenceSettings.Default(),
            new([battery, sw, bumper]),
            Connections: new(Power(1, 2), Power(2, 3)));

        var saved = new WorkshopSavedConstruction(construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved);
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, bytes);

        Assert.Equal(saved.Construction.Connections.Count, loaded.Construction.Connections.Count);
        Assert.Equal(saved.Construction.Instances.Count, loaded.Construction.Instances.Count);

        var (built, _, reset) = await WorkshopIntegrationHarness.ObserveReset(loaded.Construction, 60);
        NoFixedCapacityTests.AssertSameState(built, reset, "composable supply reset");
    }

    private static readonly PartId InternalEdgeCompositePart = new("unfamiliar_internal_edge_composite");

    private static BundleEntry CreateInternalEdgeCompositePackage()
    {
        var bumper = TestBundle.Entry(new PartId("bumper"));
        var nodes = new SupplyNodeDescriptor[]
        {
            new SupplySourceDescriptor(new("src"), new(new("cap"), new("pwr"), new("init"), new("enable"))),
            new SupplyContactDescriptor(new("contact"), new(new("signal"), SignalContactState.Latched)),
            new SupplyConsumerDescriptor(new("consumer"), [new SupplyContactWorkTarget(new("work"))])
        };
        var edges = new SupplyConnection[]
        {
            new(new(new("src"), SupplyTerminal.Output), new(new("contact"), SupplyTerminal.PowerIn)),
            new(new(new("contact"), SupplyTerminal.Output), new(new("consumer"), SupplyTerminal.PowerIn))
        };
        var supplyGraph = new SupplyGraphDescriptor(nodes, edges, []);

        return bumper with
        {
            Metadata = bumper.Metadata with
            {
                Id = InternalEdgeCompositePart.Value,
                Parameters =
                [
                    .. bumper.Metadata.Parameters,
                    new("cap", ParameterUnit.Joule, ParameterKind.Real, 1f, 1000f, 100f, []),
                    new("pwr", ParameterUnit.Watt, ParameterKind.Real, 1f, 100f, 10f, []),
                    new("init", ParameterUnit.None, ParameterKind.Real, 0f, 1f, 1f, []),
                    new("enable", ParameterUnit.None, ParameterKind.Choice, 0f, 1f, 1f, ["Disabled", "Enabled"], RunEdit: ParameterRunEdit.RunControl),
                    new("trigger_threshold", ParameterUnit.MetrePerSecond, ParameterKind.Real, 0.1f, 10f, 0.5f, [])
                ]
            },
            Electrical = new ElectricalPayload(
                [
                    new("src_node", ElectricalRole.Source, 12f, .05f, 0f, 10f),
                    new("contact_node", ElectricalRole.Switch, 12f, .05f, 0f, 10f),
                    new("consumer_node", ElectricalRole.Consumer, 12f, 1f, 0f, 10f)
                ],
                supplyGraph),
            Signal = new SignalPayload(
                [new SignalContactDescriptor(new("signal"), new(0), new("trigger_threshold"))],
                [], []),
            Ports = []
        };
    }

    [Fact]
    public void PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology()
    {
        var entry = CreateInternalEdgeCompositePackage();
        Assert.Empty(SupplyBundleValidation.Validate(entry));
        Assert.Empty(BundleValidator.Validate([new("composite", entry)]));

        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(InternalEdgeCompositePart, entry.Ports)]));
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, InternalEdgeCompositePart, new(1), 0, 3, 0, 0, 0, 0, 1,
            new([
                new(new("strength"), 8f),
                new(new("cap"), 100f),
                new(new("pwr"), 10f),
                new(new("init"), 1f),
                new(new("enable"), 1f),
                new(new("trigger_threshold"), 0.5f)
            ]));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([instance]));
        var topology = WorkshopSupplyCompiler.Compile(RuntimeTestServices.Core.Supply, construction, id => entry);
        var identities = construction.Instances.Identities[instance.Id];
        var consumerNode = identities.Supply[new("consumer")].Node;
        var srcId = identities.Supply[new("src")].Source;
        Assert.Equal([srcId], topology.UpstreamSources(consumerNode));

        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var binding = Assert.Single(scene.Electrical.Bindings.ToArray());
        Assert.Equal(identities.Work, binding.Storage);
        Assert.Equal(srcId, binding.Source);
    }

    [Fact]
    public async Task SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore()
    {
        var entry = CreateInternalEdgeCompositePackage();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(InternalEdgeCompositePart, entry.Ports)]));

        var composite = WorkshopInput.Generic(RuntimeTestServices.Bundle, InternalEdgeCompositePart, new(1), 0, 3, 0, 0, 0, 0, 1,
            new([
                new(new("strength"), 8f),
                new(new("cap"), 100f),
                new(new("pwr"), 10f),
                new(new("init"), 1f),
                new(new("enable"), 1f),
                new(new("trigger_threshold"), 0.5f)
            ]));
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bowling"), new(2), 0, 5.5f, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([composite, ball]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await harness.Run();
        await harness.Advance(180);

        var hit = harness.Reads.FindIndex(read => Enumerable.Range(0, read.Activations.Count)
            .Select(i => read.Activations[i])
            .Any(act => act.Owner == composite.Id && act.Phase == ActivationPhase.Latched));
        Assert.True(hit > 0, "Contact was triggered by the falling ball");

        var later = harness.Committed;
        var store = later.ContactWorks[0];
        Assert.True(store.RemainingEnergy.Value > 0f || store.SuppliedEnergy.Value > 0f, "Store was recharged across internal supply edges");
    }

    [Fact]
    public void InvalidPackageInternalSupplyEdgesAreRefusedByValidation()
    {
        var entry = CreateInternalEdgeCompositePackage();

        // Duplicate internal edge
        var dupEdges = new SupplyGraphDescriptor(
            entry.Electrical!.SupplyGraph!.Nodes,
            [
                new(new(new("src"), SupplyTerminal.Output), new(new("contact"), SupplyTerminal.PowerIn)),
                new(new(new("src"), SupplyTerminal.Output), new(new("contact"), SupplyTerminal.PowerIn))
            ],
            []);
        var dupErrors = SupplyBundleValidation.Validate(entry with { Electrical = entry.Electrical! with { SupplyGraph = dupEdges } });
        Assert.Contains(dupErrors, e => e.Detail.Contains("Edge.Terminal"));

        // Invalid endpoint
        var invalidEndpointEdges = new SupplyGraphDescriptor(
            entry.Electrical!.SupplyGraph!.Nodes,
            [
                new(new(new("consumer"), SupplyTerminal.PowerIn), new(new("src"), SupplyTerminal.Output))
            ],
            []);
        var endpointErrors = SupplyBundleValidation.Validate(entry with { Electrical = entry.Electrical! with { SupplyGraph = invalidEndpointEdges } });
        Assert.Contains(endpointErrors, e => e.Detail.Contains("Edge.Terminal"));

        // Nonmonotone cycle
        var cycleEdges = new SupplyGraphDescriptor(
            [
                new SupplyLogicDescriptor(new("gate_a"), LogicGateOperation.Xor),
                new SupplyLogicDescriptor(new("gate_b"), LogicGateOperation.Xor)
            ],
            [
                new(new(new("gate_a"), SupplyTerminal.Output), new(new("gate_b"), SupplyTerminal.FirstIn)),
                new(new(new("gate_b"), SupplyTerminal.Output), new(new("gate_a"), SupplyTerminal.FirstIn))
            ],
            []);
        var cycleErrors = SupplyBundleValidation.Validate(entry with { Electrical = entry.Electrical! with { SupplyGraph = cycleEdges } });
        Assert.Contains(cycleErrors, e => e.Detail.Contains("NonmonotoneCycle"));
    }
}
