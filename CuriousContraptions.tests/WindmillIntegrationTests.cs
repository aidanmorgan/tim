using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WindmillIntegrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RotorTravelEventPaidMotionAndLifecycleUseCommittedPhysicalState(bool powered)
    {
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(powered: powered), Windmill());
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        await h.Require(WorkshopCommandKind.Construct, construction);
        var initial = h.Committed;
        var observer = Assert.Single(h.Construction.Instances.Identities[new(2)].CoordinateTravelObservers).Value;
        await h.Run();
        await h.Advance(360);
        var rotor = Coordinate(h);
        var travel = Assert.Single(Enumerable.Range(0, h.Committed.CoordinateTravelObservers.Count)
            .Select(i => h.Committed.CoordinateTravelObservers[i]));
        Assert.Equal(observer, travel.Id);
        if (powered)
        {
            Assert.InRange(rotor.Speed, 5.9f, 6.1f);
            Assert.True(rotor.Coordinate > MathF.Tau);
            Assert.True(h.Committed.Airflow[0].Work.Value > 0);
            var occurred = Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
            Assert.Equal(observer.Value, occurred.Source.Value);
            Assert.Equal(MathF.Tau, occurred.Magnitude);
            Assert.Equal(1u, occurred.Count);
            Assert.Equal(CoordinateTravelPhase.Reached, travel.Phase);
        }
        else
        {
            Assert.Equal(0, rotor.Speed);
            Assert.Equal(0, rotor.Coordinate);
            Assert.Equal(0, h.Committed.Airflow[0].Work.Value);
            Assert.Empty(h.Of(SimulationEventKind.CoordinateTravelReached));
            Assert.Equal(CoordinateTravelPhase.Accumulating, travel.Phase);
        }
        await h.Require(WorkshopCommandKind.Pause);
        var paused = h.Committed;
        Assert.Equal(WorkshopRejection.WrongPhase, (await h.AdvanceOne()).Reason);
        Assert.Equal(paused.Tick, h.Committed.Tick);
        Assert.Equal(travel, h.Committed.CoordinateTravelObservers[0]);
        await h.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Tick.Value + 1, h.Committed.Tick.Value);
        await h.Require(WorkshopCommandKind.Resume);
        await h.Advance(120);
        Assert.Equal(powered ? 1 : 0, h.Of(SimulationEventKind.CoordinateTravelReached).Count());
        await h.Reset();
        Assert.Equal(initial.Presentation.Actuators.ToArray(), h.Committed.Presentation.Actuators.ToArray());
        Assert.Equal(initial.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
        Assert.Equal(initial.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(0, h.Committed.Airflow[0].Work.Value);

        var saved = new WorkshopSavedConstruction(construction, new(3));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        await h.DisposeAsync();
        await using var fresh = await WorkshopIntegrationHarness.Running(loaded.Construction);
        await fresh.Advance(360);
        Assert.Equal(powered, Coordinate(fresh).Speed > 1);
        Assert.Equal(powered ? 1 : 0, fresh.Of(SimulationEventKind.CoordinateTravelReached).Count());
    }

    [Theory]
    [InlineData(true, true, 0, false)]
    [InlineData(false, true, 0, false)]
    [InlineData(true, false, 0, false)]
    [InlineData(true, true, 1, false)]
    [InlineData(true, true, 2, false)]
    [InlineData(true, true, 0, true)]
    public async Task PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection(bool connected, bool powered, int reversers, bool blocked)
    {
        var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("conveyor"), new(3),
            0, 4, 4, 0, 0, 0, 1);
        var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(4),
            0, 4.46f, 4, 0, 0, 0, 1);
        var parts = new List<IWorkshopInstance> { FanIntegrationTests.Fan(powered: powered), Windmill(), conveyor, cargo };
        if (blocked)
        {
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(8), 1.5f, 5, 0, q.X, q.Y, q.Z, q.W));
        }
        var links = new List<WorkshopConnection>();
        GpuBodyId upstream = new(2);
        for (var i = 0; i < reversers; i++)
        {
            var reverse = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("reverse_transmission"), new((ulong)(5 + i)),
                -3 - i * 3, 4, 4, 0, 0, 0, 1);
            parts.Add(reverse);
            links.Add(new(upstream, PortId.Drive, reverse.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
            upstream = reverse.Id;
        }
        if (connected) links.Add(new(upstream, PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        var construction = TennisBallTests.Scene(parts.ToArray()) with { Connections = new(links.ToArray()) };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var direction = reversers % 2 == 0 ? 1 : -1;
        var crossed = false;
        for (var i = 0; i < 10; i++)
        {
            await h.Advance(120);
            Assert.True(h.Committed.Bodies.TryGet(cargo.Id, out var body));
            var x = (body.Body.Cell.X + body.Body.Local.X) / 16f;
            crossed |= direction * x > .68f; // One complete ball diameter beyond the initial centre.
            if (crossed) break;
        }
        Assert.Equal(connected && powered && !blocked, crossed);
        if (connected && powered && !blocked) Assert.True(h.Committed.Airflow[0].Work.Value > 0);
        if (blocked) Assert.Equal(0, h.Committed.Airflow[0].Work.Value);
    }

    [Theory]
    [InlineData(FanIntegrationTests.Supply.Electrical, true)]
    [InlineData(FanIntegrationTests.Supply.Electrical, false)]
    [InlineData(FanIntegrationTests.Supply.Shaft, true)]
    [InlineData(FanIntegrationTests.Supply.Shaft, false)]
    public async Task AlternateFanSuppliesRetainTheirPaidWorkBoundary(FanIntegrationTests.Supply supply, bool connected)
    {
        var fan = FanIntegrationTests.Fan(supply);
        var battery = PlacedParts.Battery(new(3), -6, 5, 0);
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(4), -3, 5, 0, 0, 0, 0, 1);
        var parts = new List<IWorkshopInstance> { fan, Windmill(), battery };
        var links = new List<WorkshopConnection>();
        if (supply == FanIntegrationTests.Supply.Electrical && connected)
            links.Add(new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        if (supply == FanIntegrationTests.Supply.Shaft)
        {
            parts.Add(motor);
            links.Add(new(battery.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
            if (connected) links.Add(new(motor.Id, PortId.Drive, fan.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        }
        await using var h = await WorkshopIntegrationHarness.Running(
            TennisBallTests.Scene(parts.ToArray()) with { Connections = new(links.ToArray()) });
        await h.Advance(120);
        Assert.Equal(connected, Coordinate(h).Speed > .1f);
        Assert.Equal(connected, h.Committed.Airflow[0].Work.Value > 0);
        Assert.Equal(0, h.Committed.EnergyStores.Count);
        if (supply == FanIntegrationTests.Supply.Electrical)
            Assert.Equal(connected, h.Committed.Electrical[0].Debit.Value > 0);
        // Motor electrical drain remains its later owner; shaft conversion is proved by actual work above.
    }

    [Fact]
    public async Task HeavyCargoLoadsTheFiniteShaftAndUnloadedRigAcceleratesAgain()
    {
        async Task<float> Speed(float mass)
        {
            var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("conveyor"), new(3), 0, 4, 4, 0, 0, 0, 1);
            var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("tennis"), new(4), 0, 4.37f, 4, 0, 0, 0, 1,
                new([new(new("mass"), mass), new(new("radius"), .25f), new(new("drag"), 0),
                    new(new("buoyancy"), 0), new(new("bounce"), 0)]));
            var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(), Windmill(), conveyor, cargo) with
                { Connections = new(new WorkshopConnection(new(2), PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical)) };
            await using var h = await WorkshopIntegrationHarness.Running(construction);
            await h.Advance(120);
            var loaded = Coordinate(h).Speed;
            Assert.True(h.Committed.Airflow[0].Work.Value > 0);
            await h.Reset();
            var unloaded = construction with { Revision = new(construction.Revision.Value + 1),
                Instances = new(construction.Instances.Where(i => i.Id != cargo.Id).ToArray()) };
            await h.Require(WorkshopCommandKind.Construct, unloaded);
            await h.Run(); await h.Advance(360);
            Assert.InRange(Coordinate(h).Speed, 5.9f, 6.1f);
            return loaded;
        }
        var light = await Speed(1);
        var heavy = await Speed(1024);
        Assert.True(light > heavy + .1f, $"The 1024 kg supported load must measurably slow this finite shaft: light={light}, heavy={heavy}.");
    }

    [Fact]
    public async Task ElectricalCutPreservesCoastThenReenableRestartsCapture()
    {
        var battery = PlacedParts.Battery(new(3), -6, 5, 0);
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(FanIntegrationTests.Supply.Electrical), Windmill(), battery)
            with { Connections = new(new WorkshopConnection(battery.Id, PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical)) };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(360);
        Assert.InRange(Coordinate(h).Speed, 5.9f, 6.1f);
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(battery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var paid = h.Committed.Airflow[0].Work.Value;
        await h.Advance(1);
        Assert.True(Coordinate(h).Speed > 1);
        Assert.Equal(paid, h.Committed.Airflow[0].Work.Value);
        await h.Advance(1200);
        var coast = Coordinate(h).Coordinate;
        await h.Advance(120);
        Assert.InRange(MathF.Abs(Coordinate(h).Coordinate - coast), 0, .01f);
        Assert.Equal(paid, h.Committed.Airflow[0].Work.Value);
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(battery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        await h.Advance(360);
        Assert.InRange(Coordinate(h).Speed, 5.9f, 6.1f);
        Assert.True(h.Committed.Airflow[0].Work.Value > paid);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
    }

    [Fact]
    public async Task ReversingBeforeOneTurnAccumulatesAbsoluteTravelWithoutInstantaneousFlip()
    {
        var firstBattery = PlacedParts.Battery(new(3), -6, 5, 0);
        var rearBattery = PlacedParts.Battery(new(4), 9, 5, 0, enabled: ElectricalEnable.Disabled);
        var front = FanIntegrationTests.Fan(FanIntegrationTests.Supply.Electrical);
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        var rear = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(5), 6, 5, 0,
            q.X, q.Y, q.Z, q.W, front.Parameters);
        var construction = TennisBallTests.Scene(front, Windmill(), firstBattery, rearBattery, rear) with
        {
            Connections = new(
                new WorkshopConnection(firstBattery.Id, PortId.Supply, front.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(rearBattery.Id, PortId.Supply, rear.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(30);
        Assert.True(Coordinate(h).Speed > 0);
        Assert.Empty(h.Of(SimulationEventKind.CoordinateTravelReached));
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(firstBattery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(rearBattery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        await h.Advance(1);
        Assert.True(Coordinate(h).Speed > 0, "Finite inertia prevents an instantaneous sign assignment.");
        await h.Advance(60);
        Assert.True(Coordinate(h).Speed < 0);
        var travel = h.Committed.CoordinateTravelObservers[0];
        Assert.True(travel.Travel > MathF.Abs(Coordinate(h).Coordinate) + .1f);
        Assert.Equal(CoordinateTravelPhase.Accumulating, travel.Phase);
        await h.Advance(360);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
        Assert.Equal(CoordinateTravelPhase.Reached, h.Committed.CoordinateTravelObservers[0].Phase);
    }

    [Fact]
    public async Task TranslatingAndRotatingTheAssemblyPreservesItsLocalCaptureBehavior()
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, .7f);
        var origin = new Vector3(-7, 8, 3);
        var target = origin + Vector3.Transform(new Vector3(3, 0, 0), q);
        var fan = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(1), origin.X, origin.Y, origin.Z,
            q.X, q.Y, q.Z, q.W, FanIntegrationTests.Fan().Parameters);
        var rotor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("windmill"), new(2), target.X, target.Y, target.Z,
            q.X, q.Y, q.Z, q.W);
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(fan, rotor));
        await h.Advance(1200);
        Assert.InRange(Coordinate(h).Speed, 5.9f, 6.1f);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }

    public enum Exposure { Rear, EdgeOn, Vacuum, Missed, EqualOpposition, UnequalOpposition }

    [Theory]
    [InlineData(Exposure.Rear, -6f)]
    [InlineData(Exposure.EdgeOn, 0f)]
    [InlineData(Exposure.Vacuum, 0f)]
    [InlineData(Exposure.Missed, 0f)]
    [InlineData(Exposure.EqualOpposition, 0f)]
    [InlineData(Exposure.UnequalOpposition, 4f)]
    public async Task ActualSourcePoseAndSignedExposureDetermineDrive(Exposure exposure, float expected)
    {
        var fan = FanIntegrationTests.Fan();
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        var rear = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(5), 6, 5, 0,
            q.X, q.Y, q.Z, q.W, fan.Parameters);
        var parts = new List<IWorkshopInstance> { Windmill() };
        if (exposure == Exposure.Rear) parts.Add(rear);
        else if (exposure == Exposure.EdgeOn)
        {
            q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(1), 3, 5, 3,
                q.X, q.Y, q.Z, q.W, fan.Parameters));
        }
        else if (exposure == Exposure.Missed)
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(1), 0, 5, 3, 0, 0, 0, 1, fan.Parameters));
        else
        {
            parts.Add(fan);
            if (exposure is Exposure.EqualOpposition or Exposure.UnequalOpposition)
            {
                var parameters = rear.Parameters.ToDictionary();
                parameters[new("force")] = exposure == Exposure.UnequalOpposition ? 3 : 9;
                parts.Add(rear with { Parameters = new(parameters) });
            }
        }
        var construction = TennisBallTests.Scene(parts.ToArray());
        if (exposure == Exposure.Vacuum) construction = construction with { Atmosphere = AtmospherePreset.Vacuum };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(1200);
        var start = Coordinate(h).Coordinate;
        await h.Advance(120);
        if (expected == 0)
        {
            Assert.InRange(MathF.Abs(Coordinate(h).Coordinate - start), 0, .01f);
            Assert.All(Enumerable.Range(0, h.Committed.Airflow.Count), i => Assert.Equal(0, h.Committed.Airflow[i].Work.Value));
            Assert.Empty(h.Of(SimulationEventKind.CoordinateTravelReached));
        }
        else
        {
            Assert.InRange(Coordinate(h).Speed, expected - .1f, expected + .1f);
            Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConnectedNondefaultSaveLoadDeliversPaidCargoAndResetsAuthoredState(bool connected, bool powered)
    {
        var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("conveyor"), new(3), 0, 4, 4, 0, 0, 0, 1);
        var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(4), 0, 4.46f, 4, 0, 0, 0, 1);
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(powered: powered), Windmill(1), conveyor, cargo) with
        {
            Connections = connected ? new(new WorkshopConnection(new(2), PortId.Drive, conveyor.Id, PortId.DriveIn,
                WorkshopConnectionDomain.Mechanical)) : new()
        };
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(construction, new(5))));
        Assert.Equal(new GpuBodyId(5), loaded.NextBodyId);
        Assert.Equal(construction.Connections, loaded.Construction.Connections);
        var savedRotor = Assert.IsType<WorkshopGenericInstance>(loaded.Construction.Instances.Single(p => p.Id == new GpuBodyId(2)));
        Assert.Equal(1, savedRotor.Parameters[new("radians_per_force")]);
        foreach (var authored in construction.Instances)
        {
            var restored = loaded.Construction.Instances.Single(p => p.Id == authored.Id);
            Assert.Equal(authored.Cell, restored.Cell);
            Assert.Equal(authored.Local, restored.Local);
            Assert.Equal(authored.Rotation, restored.Rotation);
        }
        foreach (var candidate in new[] { construction, loaded.Construction })
        {
            await using var h = await WorkshopIntegrationHarness.Building(candidate);
            var initial = h.Committed;
            await h.Run();
            var delivered = false;
            for (var second = 0; second < 10 && !delivered; second++)
            {
                await h.Advance(120);
                Assert.True(h.Committed.Bodies.TryGet(cargo.Id, out var body));
                delivered = (body.Body.Cell.X + body.Body.Local.X) / 16f > .68f;
            }
            Assert.Equal(connected && powered, delivered);
            if (delivered) Assert.True(h.Committed.Airflow[0].Work.Value > 0);
            await h.Reset();
            Assert.Equal(candidate.Connections, h.Construction.Connections);
            Assert.Equal(initial.Presentation.Actuators.ToArray(), h.Committed.Presentation.Actuators.ToArray());
            Assert.Equal(initial.EnergyStores[0], h.Committed.EnergyStores[0]);
            Assert.Equal(initial.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
            foreach (var id in new[] { cargo.Id, h.Construction.Instances.Identities[new(2)].Hinges[0].Body })
            {
                Assert.True(initial.Bodies.TryGet(id, out var before));
                Assert.True(h.Committed.Bodies.TryGet(id, out var after));
                Assert.Equal(before.Body.Cell, after.Body.Cell);
                Assert.Equal(before.Body.Local, after.Body.Local);
                Assert.Equal(before.Body.Velocity, after.Body.Velocity);
                Assert.Equal(before.Rotation, after.Rotation);
                Assert.Equal(before.AngularVelocity, after.AngularVelocity);
            }
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConnectedExternalDriveBackdrivesActualWindmillAboveAerodynamicTarget(bool connected, bool powered)
    {
        var entry = TestBundle.Entry(new("motor"));
        var part = new PartId("independently_driven_receiver");
        var drive = entry.Ports.Single(port => port.Id == PortId.Drive);
        entry = entry with { Metadata = entry.Metadata with { Id = part.Value },
            Ports = [entry.Ports.Single(port => port.Id == PortId.PowerIn),
                drive with { Id = PortId.DriveIn, Direction = PortDirection.Input, Role = PortRole.DriveIn }] };
        Assert.Empty(CuriousContraptions.Bundle.BundleValidator.Validate([new("driven receiver", entry)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portsScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var receiver = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(3), 0, 4, 4, 0, 0, 0, 1,
            new([new(new("speed"), 18), new(new("torque"), 20)])) with { PartId = part };
        var battery = PlacedParts.Battery(new(4), -3, 4, 4,
            enabled: powered ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var links = new List<WorkshopConnection> { new(battery.Id, PortId.Supply, receiver.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical) };
        if (connected) links.Add(new(new(2), PortId.Drive, receiver.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        await h.Require(WorkshopCommandKind.Construct,
            TennisBallTests.Scene(Windmill(), receiver, battery) with { Connections = new(links.ToArray()) });
        await h.Run();
        await h.Advance(120);
        var speed = MathF.Abs(Coordinate(h).Speed);
        Assert.Equal(connected && powered, speed > 12);
        if (!connected || !powered) Assert.Equal(0, speed);
        Assert.Equal(0, h.Committed.Airflow.Count);
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(battery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await h.Advance(30);
        var coasting = MathF.Abs(Coordinate(h).Speed);
        if (connected && powered) Assert.InRange(coasting, .1f, speed);
        else Assert.Equal(0, coasting);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task OpposingSourcesPayTheirOwnWorkAndInactiveBranchesPayNothing(bool rearDisabled, bool rearBlocked)
    {
        var front = FanIntegrationTests.Fan();
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        var parameters = FanIntegrationTests.Fan(powered: !rearDisabled).Parameters.ToDictionary();
        parameters[new("force")] = 3;
        var rear = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(5), 6, 5, 0,
            q.X, q.Y, q.Z, q.W, new(parameters));
        var parts = new List<IWorkshopInstance> { front, Windmill(), rear };
        if (rearBlocked)
        {
            var wallRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(8), 4.5f, 5, 0,
                wallRotation.X, wallRotation.Y, wallRotation.Z, wallRotation.W));
        }
        await using var h = await WorkshopIntegrationHarness.Building(TennisBallTests.Scene(parts.ToArray()));
        var sources = h.Simulation.Scene.Physics.AirflowSources.ToArray();
        var initial = Enumerable.Range(0, h.Committed.EnergyStores.Count)
            .ToDictionary(i => h.Committed.EnergyStores[i].Id, i => h.Committed.EnergyStores[i].Balance);
        await h.Run(); await h.Advance(120);
        foreach (var source in sources)
        {
            var read = Enumerable.Range(0, h.Committed.Airflow.Count).Select(i => h.Committed.Airflow[i])
                .Single(value => value.Id == source.Id);
            var store = Enumerable.Range(0, h.Committed.EnergyStores.Count).Select(i => h.Committed.EnergyStores[i])
                .Single(value => value.Id == source.Input.Store);
            var active = source.Owner == front.Id || (!rearDisabled && !rearBlocked);
            Assert.Equal(source.Owner, read.Owner);
            Assert.Equal(active, read.Work.Value > 0);
            if (!active) Assert.Equal(0, read.Work.Value);
            var debit = initial[store.Id].DecreaseTo(store.Balance);
            // 0.01 J is below one percent of a single rated source's one-second paid transfer,
            // and excludes a free branch, duplicate debit or charging the other source's store.
            Assert.InRange(MathF.Abs(debit - read.Work.Value), 0, .01f);
            Assert.True(float.IsFinite(read.Work.Value));
            Assert.True(store.Balance.Available >= 0);
        }
        Assert.True(Coordinate(h).Speed > 0);
    }

    internal static WorkshopGenericInstance Windmill(float response = .6666667f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new("windmill"), new(2), 3, 5, 0, 0, 0, 0, 1,
            new([new(new("radians_per_force"), response)]));

    internal static ActuatorDriveRead Coordinate(WorkshopIntegrationHarness h) =>
        Assert.Single(h.Committed.Presentation.Actuators.ToArray(), row =>
            row.Drive.RevoluteJoint == h.Construction.Instances.Identities[new(2)].Hinges[0].Joint);
}
