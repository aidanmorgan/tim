using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class CounterTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData(1,false)]
    [InlineData(1,true)]
    [InlineData(3,false)]
    [InlineData(3,true)]
    [InlineData(9,false)]
    [InlineData(9,true)]
    public void SaturatesEmitsOnceAndLatchesOnlyRealSupply(int target,bool supply)
    {
        var world=World();
        try
        {
            var counter=(CounterPart)world.AddPart(new(){Id="counter",Kind="counter",
                Properties=new(){[CounterParameters.Target]=target}});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,4,0]});
            var gate=(PoweredGatePart)world.AddPart(new(){Id="gate",Kind="powered_gate",Position=[4,4,0]});
            var timer=(HoldTimerPart)world.AddPart(new(){Id="timer",Kind="hold_timer",Position=[0,8,0],
                Properties=new(){[HoldTimerParameters.Seconds]=.1f}});
            if(supply)Assert.True(world.Connect(battery,counter));
            Assert.True(world.Connect(counter,gate));
            Assert.Null(world.SuggestedConnection(counter,timer));
            Assert.True(world.Connect(counter,SocketIds.ActivationOut,timer,SocketIds.ActivationIn,ConnectionDomain.Activation));
            world.Start();
            for(var count=1;count<=target;count++)
            {
                world.Activate(counter);
                for(var tick=0;tick<4;tick++)world.Step();
                Assert.Equal(count,counter.Count); // A held active input is not sampled as extra events.
                Assert.Equal(count==target,counter.Active);
                Assert.Equal(supply&&count==target,gate.HasElectricalPower(SocketIds.PowerIn));
                Assert.Equal(count==target?HoldTimerState.Holding:HoldTimerState.Ready,timer.State);
            }
            var emitted=timer.StartedTick;
            for(var tick=0;tick<60;tick++)world.Step();
            Assert.Equal(HoldTimerState.Ready,timer.State);
            for(var i=0;i<20;i++){world.Activate(counter);world.Step();}
            Assert.Equal(target,counter.Count);
            Assert.Equal(CounterState.Reached,counter.State);
            Assert.Equal(emitted,timer.StartedTick);
            Assert.Equal(HoldTimerState.Ready,timer.State);
            Assert.Equal(supply,gate.Active);
            world.Restore();
            counter=(CounterPart)world.FindPart("counter")!;
            Assert.Equal(0,counter.Count);
            Assert.Equal(CounterState.Counting,counter.State);
            Assert.False(counter.Active);
            Assert.Equal(target,counter.Target);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreePhysicalCrossingsReachTargetRegardlessOfEntityOrder(bool reverse)
    {
        var world=World();
        try
        {
            var specs=new List<PartSpec>{
                new(){Id="detector",Kind="ball_detector",Position=[0,8,0]},
                new(){Id="counter",Kind="counter",Position=[4,8,0]},
                new(){Id="lamp",Kind="lamp",Position=[7,8,0]}};
            if(reverse)specs.Reverse();
            foreach(var spec in specs)world.AddPart(spec);
            Assert.True(world.Connect(world.FindPart("detector")!,world.FindPart("counter")!));
            Assert.True(world.Connect(world.FindPart("counter")!,world.FindPart("lamp")!));
            for(var i=0;i<3;i++)world.AddPart(new(){Id="ball"+i,Kind="ball",Position=[-1-i,8,0]});
            world.Start();
            foreach(var body in world.Bodies)body.Velocity=Vector3.Right*4;
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(3,((BallDetectorPart)world.FindPart("detector")!).CrossingCount);
            Assert.Equal(3,((CounterPart)world.FindPart("counter")!).Count);
            Assert.True(world.FindPart("lamp")!.Active);
        }
        finally {world.Free();}
    }
    [Fact]
    public void AuthoredTargetRoundTripsWithoutPersistingPartialRunCount()
    {
        var world=World();
        try
        {
            var counter=(CounterPart)world.AddPart(new(){Id="counter",Kind="counter",
                Properties=new(){[CounterParameters.Target]=5}});
            world.Activate(counter);world.Activate(counter);
            Assert.Equal(2,counter.Count);
            var data=MachineCodec.Clone(world.Snapshot());
            world.LoadMachine(data);
            counter=(CounterPart)world.FindPart("counter")!;
            Assert.Equal(5,counter.Target);
            Assert.Equal(0,counter.Count);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(10f)]
    [InlineData(1.5f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidTargetIsRejected(float target)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="counter",Kind="counter",
                Properties=new(){[CounterParameters.Target]=target}}));
            Assert.Empty(world.Parts);
        }
        finally {world.Free();}
    }
}
