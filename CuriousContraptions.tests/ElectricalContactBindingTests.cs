using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalContactBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    public enum Controller { Counter, Latch, Timer }
    private static CatalogueId Catalogue(Controller kind)=>kind switch
    {
        Controller.Counter=>new("counter"),Controller.Latch=>new("latch"),Controller.Timer=>new("hold_timer"),
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static CatalogueId Receiver(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>new("light_receiver"),OpticalColour.Red=>new("red_receiver"),
        OpticalColour.Green=>new("green_receiver"),OpticalColour.Blue=>new("blue_receiver"),
        OpticalColour.Yellow=>new("yellow_receiver"),OpticalColour.Cyan=>new("cyan_receiver"),
        OpticalColour.Magenta=>new("magenta_receiver"),OpticalColour.White=>new("white_receiver"),
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    private static readonly CatalogueId Meter=new("sound_meter");
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static ElectricalRuntime Runtime(MachineWorld world)=>new(
        world.PhysicsAssembly,world.Physics,
        world.Counters,world.Parts.SelectMany(part=>part.SimulationCounters).ToDictionary(d=>d.Key,d=>world.ReadCounter(d.Key).Id),
        world.Latches,world.Parts.SelectMany(part=>part.SimulationLatches).ToDictionary(d=>d.Key,d=>world.ReadLatch(d.Key).Id),
        world.Timers,world.Parts.SelectMany(part=>part.SimulationTimers).ToDictionary(d=>d.Key,d=>world.ReadTimer(d.Key).Id),
        new Dictionary<SceneBinaryInputKey,SimulationState<Bridge.BinaryInputState>>());

    [Theory]
    [InlineData(Controller.Counter)]
    [InlineData(Controller.Latch)]
    [InlineData(Controller.Timer)]
    public void ControllerContactsRequireBindingAndFollowOwnedStateAcrossReset(Controller kind)
    {
        var world=World();
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Catalogue(kind).Value});
            var declaration=Assert.Single(part.ElectricalRoutes);
            var unbound=new ElectricalContactBinding(part,declaration.Signal);
            Assert.Throws<InvalidOperationException>(()=>unbound.Read());
            var saved=Saved(world);world.Start();
            var runtime=Runtime(world);var binding=unbound.Bind(runtime);
            Assert.False(binding.Read());
            switch(kind)
            {
                case Controller.Counter:
                    var counter=(CounterPart)part;
                    for(var i=1;i<=counter.Target;i++)
                    {
                        world.Activate(part);world.Step();
                        Assert.Equal(i==counter.Target,binding.Read());
                    }
                    break;
                case Controller.Latch:
                    world.SubmitLatch(new(part,LatchPart.Memory),SimulationLatchCommand.Set);
                    world.Step();Assert.False(binding.Read()); // Delivery settles at the following boundary.
                    world.Step();break;
                case Controller.Timer:world.Activate(part);world.Step();break;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
            Assert.True(binding.Read());
            part.Active=false;Assert.True(binding.Read());
            var plan=new ElectricalNetwork(world);plan.BindRuntime(runtime);
            Assert.Throws<InvalidOperationException>(()=>plan.BindRuntime(runtime));
            world.Restore();Assert.Equal(saved,Saved(world));
            part=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            world.Start();
            var reset=new ElectricalContactBinding(part,Assert.Single(part.ElectricalRoutes).Signal).Bind(Runtime(world));
            Assert.False(reset.Read());
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(OpticalColour.Broadband)]
    [InlineData(OpticalColour.Red)]
    [InlineData(OpticalColour.Green)]
    [InlineData(OpticalColour.Blue)]
    [InlineData(OpticalColour.Yellow)]
    [InlineData(OpticalColour.Cyan)]
    [InlineData(OpticalColour.Magenta)]
    [InlineData(OpticalColour.White)]
    public void EveryOpticalContactReadsAnOwnedCheckpointedAcceptanceResult(OpticalColour colour)
    {
        var world=World();
        try
        {
            var part=(LightReceiverPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Receiver(colour).Value});
            var binding=new ElectricalContactBinding(part,Assert.Single(part.ElectricalRoutes).Signal);
            var transaction=new SimulationTransaction(part.RuntimeState.Append(part.BaseRuntimeCheckpoint));
            void Receive(Vector3 power)=>part.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{[OpticalPortId.Main]=power});
            Assert.False(binding.Read());transaction.Begin();Receive(OpticalColours.Mask(colour));
            part.Active=false;Assert.True(binding.Read());Assert.True(part.Matches);
            transaction.Rollback();Assert.False(binding.Read());Assert.False(part.Matches);Assert.Equal(Vector3.Zero,part.ReceivedPower);
            transaction.Begin();Receive(OpticalColours.Mask(colour));transaction.Commit();
            transaction.Begin();Receive(Vector3.Zero);Assert.False(binding.Read());transaction.Rollback();
            Assert.True(binding.Read());Assert.True(part.Matches);Assert.Equal(OpticalColours.Mask(colour),part.ReceivedPower);
        }
        finally{world.Free();}
    }

    [Fact]
    public void AcousticContactPreservesInclusiveOnExclusiveOffAndRollback()
    {
        var world=World();
        try
        {
            var part=(SoundMeterPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Meter.Value});
            var binding=new ElectricalContactBinding(part,Assert.Single(part.ElectricalRoutes).Signal);
            var transaction=new SimulationTransaction(part.RuntimeState);
            part.ReceiveAcousticLevel(MathF.BitDecrement(part.Threshold));Assert.False(binding.Read());
            part.ReceiveAcousticLevel(part.Threshold);Assert.True(binding.Read());Assert.False(part.Active);
            transaction.Begin();part.ReceiveAcousticLevel(part.Threshold*.9f);Assert.False(binding.Read());
            transaction.Rollback();Assert.True(binding.Read());
            part.ReceiveAcousticLevel(MathF.BitIncrement(part.Threshold*.9f));Assert.True(binding.Read());
            part.ReceiveAcousticLevel(part.Threshold*.9f);Assert.False(binding.Read());
        }
        finally{world.Free();}
    }

    [Fact]
    public void IncompleteForeignAndUndeclaredSignalsAreRejected()
    {
        var world=World();
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Catalogue(Controller.Counter).Value});
            var other=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Catalogue(Controller.Counter).Value,Position=[4,0,0]});
            Assert.Throws<ArgumentNullException>(()=>ElectricalContactSignal.BooleanState(null!));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.CounterReached(default));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.LatchOn(default));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.TimerCounting(default));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.ContactLoaded(default));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.ServoAtEndpoint(default,PhysicsServoEndpoint.Lower,0));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.ServoAtEndpoint(new(part,new JointSlot()),(PhysicsServoEndpoint)999,0));
            Assert.Throws<ArgumentException>(()=>ElectricalContactSignal.ServoAtEndpoint(new(part,new JointSlot()),PhysicsServoEndpoint.Lower,double.NaN));
            Assert.Throws<ArgumentException>(()=>new ElectricalContactBinding(part,ElectricalContactSignal.BooleanState(new(false))));
            Assert.Throws<ArgumentException>(()=>new ElectricalContactBinding(part,ElectricalContactSignal.CounterReached(new(other,CounterPart.Deliveries))));
            Assert.Throws<ArgumentException>(()=>new ElectricalContactBinding(part,ElectricalContactSignal.CounterReached(new(part,new CounterSlot()))));
            Assert.Throws<ArgumentException>(()=>new ElectricalContactBinding(part,ElectricalContactSignal.ContactLoaded(new(part,MachinePart.RootBody))));
            Assert.Throws<ArgumentException>(()=>new ElectricalContactBinding(part,ElectricalContactSignal.ServoAtEndpoint(new(part,new JointSlot()),PhysicsServoEndpoint.Lower,0)));
            world.Start();var runtime=Runtime(world);
            var declaration=new ElectricalContactBinding(part,Assert.Single(part.ElectricalRoutes).Signal);
            var invalid=runtime with {CounterIds=new Dictionary<SceneCounterKey,SimulationCounterId>()};
            var plan=new ElectricalNetwork(world);Assert.Throws<ArgumentException>(()=>plan.BindRuntime(invalid));
            Assert.Throws<InvalidOperationException>(()=>plan.Solve());
            plan.BindRuntime(runtime);plan.Solve();Assert.False(declaration.Bind(runtime).Read());
        }
        finally{world.Free();}
    }
}
