using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class LinearPusherIntegrationTests
{
    private static WorkshopGenericInstance Part(string id, ulong owner, Vector3 position, params KeyValuePair<ParameterId, float>[] values) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new(id), new(owner), position.X, position.Y, position.Z, 0, 0, 0, 1,
            values.Length == 0 ? null : new(values));
    private static WorkshopConstruction Machine(bool connected)
    {
        var parts = new WorkshopInstances(
            Part("linear_pusher", 1, new(-2.35f, 4.46f, 0), new KeyValuePair<ParameterId, float>(new("stroke"), 3f), new KeyValuePair<ParameterId, float>(new("speed"), 1.5f),
                new KeyValuePair<ParameterId, float>(new("acceleration"), 12f), new KeyValuePair<ParameterId, float>(new("force"), 40f)),
            PlacedParts.Battery(new(2), 0, 2, 3, enabled: ElectricalEnable.Enabled),
            Part("motor", 3, new(-4, 4, 0), new KeyValuePair<ParameterId, float>(new("speed"), 1f), new KeyValuePair<ParameterId, float>(new("torque"), 20f)),
            Part("conveyor", 4, new(0, 4, 0), new KeyValuePair<ParameterId, float>(new("length"), 3f), new KeyValuePair<ParameterId, float>(new("width"), 1.2f), new KeyValuePair<ParameterId, float>(new("surface_per_radian"), .1f)),
            Part("ball", 5, new(-.6f, 4.46f, 0)),
            Part("switch", 6, new(-6, 1, 0), new KeyValuePair<ParameterId, float>(new("trigger_threshold"), .8f)),
            Part("ball", 7, new(-6, 3, 0)),
            Part("delay", 8, new(-6, 2, 3), new KeyValuePair<ParameterId, float>(new("delay_seconds"), 2f)),
            Part("hold_timer", 9, new(-3, 2, 3), new KeyValuePair<ParameterId, float>(new("hold_seconds"), 6f)),
            Part("pipe", 10, new(2.19f, 4.77f, 0), new KeyValuePair<ParameterId, float>(new("length"), 1f)));
        var links = new WorkshopConnections(
            new(new(2), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(2), PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(2), PortId.Supply, new(9), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(3), PortId.Drive, new(4), PortId.DriveIn, WorkshopConnectionDomain.Mechanical),
            new(new(6), PortId.ActivationOut, new(8), PortId.ActivationIn, WorkshopConnectionDomain.Activation),
            new(new(8), PortId.ActivationOut, new(9), PortId.ActivationIn, WorkshopConnectionDomain.Activation));
        if (connected) links = links.With(new(new(9), PortId.Supply, new(1), new("ExtendIn"), WorkshopConnectionDomain.Electrical));
        return new(new(2), WorkshopCadenceSettings.Default(), parts, Connections: links);
    }
    private static Vector3 Cargo(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(new(5), out var found));
        var body = found.Body;
        return new((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
    }
    private static ActivationTimerState Timer(WorkshopRead read, ulong owner) =>
        Enumerable.Range(0, read.Timers.Count).Select(i => read.Timers[i])
            .Single(row => row.Node == SignalTestNodes.Only(read.Activations, new(owner)));
    private static PrismaticMotorRead Motor(WorkshopRead read) =>
        read.Presentation.Actuators.ToArray().Single(row => row.Owner == new GpuBodyId(1)).Motor!.Value;
    private static bool Entered(WorkshopRead read)
    {
        var cargo = Cargo(read);
        return cargo.X > 1.94f && cargo.X < 2.44f &&
            new Vector2(cargo.Y - 4.77f, cargo.Z).Length() <= .315f; // Full ball inside the straight bore, with 5 mm routing clearance.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PhysicalSensorAndDelayPushConveyorCargoIntoPipeOnlyWithTheCommandConnection(bool connected)
    {
        var reads = await WorkshopIntegrationHarness.Observe(Machine(connected), 1200);
        var waiting = reads.First(read => Timer(read, 8).Phase == ActivationTimerPhase.Counting);
        var delay = Timer(waiting, 8);
        Assert.True(delay.StartedTick > 0); // A physical falling-ball impact starts the timer after Run.
        Assert.Equal(240UL, delay.DueTick - delay.StartedTick); // The authored two-second delay at 120 Hz.
        var opened = reads.First(read => Timer(read, 9).Phase == ActivationTimerPhase.Counting);
        var hold = Timer(opened, 9);
        Assert.Equal(720UL, hold.DueTick - hold.StartedTick); // The authored six-second command window.
        Assert.True(opened.Tick.Value >= delay.DueTick);
        Assert.True(Cargo(opened).X > -.58f, "The conveyor must feed cargo before the delayed head can contact it.");
        if (connected)
        {
            var moving = reads.First(read => Motor(read).Work > 0f);
            Assert.True(moving.Tick.Value >= delay.DueTick);
            Assert.Contains(reads, Entered);
        }
        else
        {
            Assert.All(reads, read => Assert.Equal(0f, Motor(read).Work));
            Assert.DoesNotContain(reads, Entered);
        }
    }
}
