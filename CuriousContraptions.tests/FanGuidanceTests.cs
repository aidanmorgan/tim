using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class FanGuidanceTests
{

    public enum Payload { Balloon, Ball, Baseball }

    [Theory]
    [InlineData(Payload.Balloon, true)]
    [InlineData(Payload.Balloon, false)]
    [InlineData(Payload.Ball, true)]
    [InlineData(Payload.Ball, false)]
    [InlineData(Payload.Baseball, true)]
    [InlineData(Payload.Baseball, false)]
    public async Task ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow(Payload payload, bool enabled)
    {
        var part = payload switch
        {
            Payload.Balloon => new PartId("balloon"),
            Payload.Ball => new PartId("ball"),
            Payload.Baseball => new PartId("baseball"),
            _ => throw new ArgumentOutOfRangeException(nameof(payload))
        };
        var cargo = WorkshopInput.Generic(RuntimeTestServices.Bundle, part, new(2), 3, 5, 0, 0, 0, 0, 1);
        await using var h = await WorkshopIntegrationHarness.Running(
            TennisBallTests.Scene(FanIntegrationTests.Fan(powered: enabled), cargo));
        await h.Advance(12);
        Assert.True(h.Committed.Bodies.TryGet(cargo.Id, out var read));
        if (enabled)
        {
            // A tenth-second free-flight window must produce visible lateral guidance; vertical
            // buoyancy/gravity remains the payload's own declared behaviour.
            Assert.True(read.Body.Velocity.X > .1f);
            Assert.True(h.Committed.Airflow[0].Work.Value > 0);
        }
        else
        {
            Assert.Equal(0f, read.Body.Velocity.X);
            Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        }
    }

    public enum HollowObstacle { Funnel, Bend }

    [Theory]
    [InlineData(HollowObstacle.Funnel, true)]
    [InlineData(HollowObstacle.Funnel, false)]
    [InlineData(HollowObstacle.Bend, true)]
    [InlineData(HollowObstacle.Bend, false)]
    public async Task TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream(HollowObstacle shape, bool inStream)
    {
        var part = shape == HollowObstacle.Funnel ? new PartId("funnel") : new PartId("pipe_bend_45");
        var q = MathF.Sqrt(.5f);
        var obstacle = WorkshopInput.Generic(RuntimeTestServices.Bundle, part, new(3),
            1.5f, 5, inStream ? 0 : 3, 0, q, 0, q);
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(),
            TennisBallTests.Ball(id: 2, x: 3, y: 5, drag: 0), obstacle);
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(4);
        if (inStream) Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        else Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }



    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ColdStartRequiresTheConnectedSwitchOccurrenceBeforeAnyTransfer(bool connected)
    {
        var fan = FanIntegrationTests.Fan(powered: false);
        var cargo = TennisBallTests.Ball(id: 2, x: 2, y: 6.25f);
        var trigger = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("switch"), new(3), -4, 1, 0, 0, 0, 0, 1);
        var striker = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(4), -4, 3, 0, 0, 0, 0, 1);
        var construction = TennisBallTests.Scene(fan, cargo, trigger, striker);
        if (connected) construction = construction with { Connections = new(new WorkshopConnection(
            trigger.Id, PortId.ActivationOut, fan.Id, PortId.ActivationIn, WorkshopConnectionDomain.Activation)) };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var occurred = false;
        for (var tick = 0; tick < 120; tick++)
        {
            await h.Advance(1);
            occurred |= Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i])
                .Any(row => row.Owner == trigger.Id && row.Phase == ActivationPhase.Latched);
            if (!occurred || !connected)
            {
                Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
                Assert.Equal(0f, h.Committed.Airflow[0].FlowSpeed);
            }
        }
        Assert.True(occurred);
        if (connected) Assert.True(h.Committed.Airflow[0].Work.Value > 0);
        await h.Reset();
        Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        Assert.Equal(0f, h.Committed.Airflow[0].FlowSpeed);
        Assert.Equal(14400f, h.Committed.EnergyStores[0].Energy.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransparentPipeBlocksAcrossItsShellButAllowsItsOpenBore(bool acrossShell)
    {
        // A one-metre pipe is centred between source and cargo. Its bore radius .65 m clears all
        // four .125 m transverse samples when aligned; rotating 90 degrees puts both shell walls in every ray.
        var q = acrossShell ? MathF.Sqrt(.5f) : 0f;
        var w = acrossShell ? q : 1f;
        var pipe = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("pipe"), new(3),
            1, 5, 0, 0, q, 0, w, new([new(new("length"), 1f)]));
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(),
            TennisBallTests.Ball(id: 2, x: 2, y: 5, drag: 0), pipe);
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(4);
        if (acrossShell) Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        else Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }


    public enum Control { Supplied, NoBattery, EmptyBattery, Away, OutOfReach, SolidWall, Heavy }

    [Theory]
    [InlineData(Control.Supplied)]
    [InlineData(Control.NoBattery)]
    [InlineData(Control.EmptyBattery)]
    [InlineData(Control.Away)]
    [InlineData(Control.OutOfReach)]
    [InlineData(Control.SolidWall)]
    [InlineData(Control.Heavy)]
    public async Task ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls(Control control)
    {
        var fan = FanIntegrationTests.Fan(FanIntegrationTests.Supply.Electrical);
        if (control == Control.Away) fan = fan with { Rotation = CanonicalRotation.Normalize(0, 1, 0, 0) };
        if (control == Control.OutOfReach)
        {
            var parameters = fan.Parameters.ToDictionary(); parameters[new("reach")] = 1;
            fan = fan with { Parameters = new(parameters) };
        }
        var battery = PlacedParts.Battery(new(3), -3, 5, 0, initialCharge: control == Control.EmptyBattery ? 0 : 1);
        var cargo = control == Control.Heavy
            ? WorkshopInput.Generic(RuntimeTestServices.Bundle, new("bowling"), new(2), 2, 5, 0, 0, 0, 0, 1)
            : TennisBallTests.Ball(id: 2, x: 2, y: 5);
        var receiver = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("basket"), new(4), 5.5f, 2, 0, 0, 0, 0, 1);
        var parts = new List<IWorkshopInstance> { fan, cargo, receiver };
        var connections = WorkshopConnections.Empty;
        if (control != Control.NoBattery)
        {
            parts.Add(battery);
            connections = new(new WorkshopConnection(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        }
        if (control == Control.SolidWall)
            parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(5), 1, 5, 0,
                0, MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f), WallFixture.Values(3, 2, .25f)));
        var construction = TennisBallTests.Scene(parts.ToArray()) with { Connections = connections };
        var goal = new WorkshopGoal(WorkshopGoalKind.Captured, cargo.Id, receiver.Id);
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var solved = false;
        // Ten seconds is a stall guard, not an arrival-time feature. Receiver clearance is over a ball radius;
        // the matched no-force drop remains at x=2, well outside the receiver centred at x=5.5.
        for (var tick = 0; tick < 1200 && !solved; tick++)
        {
            await h.Advance(1);
            solved = WorkshopGoalEvaluator.Evaluate(goal, h.Committed, h.Committed.Epoch,
                construction.Instances.Identities) == WorkshopGoalPhase.Solved;
        }
        Assert.Equal(control == Control.Supplied, solved);
        if (control == Control.Supplied)
        {
            Assert.Contains(h.Of(SimulationEventKind.Captured), occurrence => occurrence.Source == new GpuEventSourceId(receiver.Id.Value) && occurrence.Other == cargo.Id.Value);
            Assert.True(h.Committed.Airflow[0].Work.Value > 1);
        }
        else if (control != Control.Heavy) Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
    }
}
