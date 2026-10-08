using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CounterTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Fact]
    public void OwnedCountRestoresAndParameterBoundaryRejectsUnsupportedValues()
    {
        Assert.Equal("target_count",PartParameterName.Of(CounterParameter.TargetCount));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((CounterParameter)99));
        Assert.Throws<ArgumentException>(()=>PartParameterName.RequireExact<CounterParameter>([]));
        var world=World();
        try
        {
            var counter=(CounterPart)world.AddPart(new(){Id="counter",Kind="counter"});
            Assert.Throws<InvalidOperationException>(()=>world.Activate(counter));
            Assert.Equal(0,counter.Count);world.Start();
            var empty=world.Counters.Capture();world.Activate(counter);var partial=world.Counters.Capture();
            world.Activate(counter);world.Activate(counter);
            Assert.Equal(SimulationCounterPhase.Reached,counter.State);
            world.Counters.Restore(partial);
            Assert.Equal(1,counter.Count);Assert.Equal(SimulationCounterPhase.Counting,counter.State);
            world.Counters.Restore(empty);Assert.Equal(0,counter.Count);
        }
        finally{world.Free();}
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
                Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=target}});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,4,0]});
            var gate=(PoweredGatePart)world.AddPart(new(){Id="gate",Kind="powered_gate",Position=[4,4,0]});
            var timer=(HoldTimerPart)world.AddPart(new(){Id="timer",Kind="hold_timer",Position=[0,8,0],
                Properties=new(){[PartParameterName.Of(HoldTimerParameter.HoldSeconds)]=.1f}});
            if(supply)Assert.True(world.Connect(battery,counter));
            Assert.True(world.Connect(counter,gate));
            Assert.Null(world.SuggestedConnection(counter,timer));
            Assert.True(world.Connect(counter,SocketId.ActivationOut,timer,SocketId.ActivationIn,ConnectionDomain.Activation));
            world.Start();
            for(var count=1;count<=target;count++)
            {
                world.Activate(counter);
                for(var tick=0;tick<4;tick++)world.Step();
                Assert.Equal(count,counter.Count); // A held active input is not sampled as extra events.
                Assert.Equal(count==target,counter.Active);
                Assert.Equal(supply&&count==target,gate.HasElectricalPower(SocketId.PowerIn));
                Assert.Equal(count==target?SimulationTimerPhase.Counting:SimulationTimerPhase.Ready,timer.State);
            }
            var emitted=timer.StartedTick;
            for(var tick=0;tick<60;tick++)world.Step();
            Assert.Equal(SimulationTimerPhase.Ready,timer.State);
            for(var i=0;i<20;i++){world.Activate(counter);world.Step();}
            Assert.Equal(target,counter.Count);
            Assert.Equal(SimulationCounterPhase.Reached,counter.State);
            Assert.Equal(emitted,timer.StartedTick);
            Assert.Equal(SimulationTimerPhase.Ready,timer.State);
            Assert.Equal(supply,gate.Active);
            world.Restore();
            counter=(CounterPart)world.FindPart("counter")!;
            Assert.Equal(0,counter.Count);
            Assert.Equal(SimulationCounterPhase.Counting,counter.State);
            Assert.False(counter.Active);
            Assert.Equal(target,counter.Target);
        }
        finally {world.Free();}
    }
    private enum CrossingRole { Detector, Counter, Lamp, FirstBall, SecondBall, ThirdBall }
    private static string CrossingId(CrossingRole role,bool reverse)=>role switch
    {
        CrossingRole.Detector=>reverse?"z_detector":"a_detector",
        CrossingRole.Counter=>reverse?"a_counter":"z_counter",
        CrossingRole.Lamp=>"lamp",CrossingRole.FirstBall=>"first_ball",
        CrossingRole.SecondBall=>"second_ball",CrossingRole.ThirdBall=>"third_ball",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec CrossingSpec(CrossingRole role,bool reverse)
    {
        var (kind,x)=role switch
        {
            CrossingRole.Detector=>("ball_detector",0f),CrossingRole.Counter=>("counter",4f),
            CrossingRole.Lamp=>("lamp",7f),CrossingRole.FirstBall=>("ball",-1f),
            CrossingRole.SecondBall=>("ball",-2f),CrossingRole.ThirdBall=>("ball",-3f),
            _=>throw new ArgumentOutOfRangeException(nameof(role))
        };
        return new(){Id=CrossingId(role,reverse),Kind=kind,Position=[x,8,0]};
    }

    [Theory]
    [InlineData(false,2)]
    [InlineData(true,2)]
    [InlineData(false,3)]
    [InlineData(true,3)]
    public void PhysicalCrossingsRequireThreeEventsAndReplayRegardlessOfEntityOrder(bool reverse,int deliveries)
    {
        var world=World();
        try
        {
            var detector=world.AddPart(CrossingSpec(CrossingRole.Detector,reverse));
            var counter=world.AddPart(CrossingSpec(CrossingRole.Counter,reverse));
            var lamp=world.AddPart(CrossingSpec(CrossingRole.Lamp,reverse));
            Assert.True(world.Connect(detector,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(counter,SocketId.ActivationOut,lamp,SocketId.ActivationIn,ConnectionDomain.Activation));
            CrossingRole[] balls=[CrossingRole.FirstBall,CrossingRole.SecondBall,CrossingRole.ThirdBall];
            foreach(var role in balls.Take(deliveries))
                world.AddPart(CrossingSpec(role,reverse)).InitialVelocity=Vector3.Right*4;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            string Run()
            {
                world.Start();
                for(var i=0;i<100;i++)world.Step();
                var observed=(BallDetectorPart)world.FindPart(CrossingId(CrossingRole.Detector,reverse))!;
                var counted=(CounterPart)world.FindPart(CrossingId(CrossingRole.Counter,reverse))!;
                Assert.Equal(deliveries,observed.CrossingCount);
                Assert.Equal(deliveries,counted.Count);
                Assert.Equal(deliveries==3?SimulationCounterPhase.Reached:SimulationCounterPhase.Counting,counted.State);
                Assert.Equal(deliveries==3,world.FindPart(CrossingId(CrossingRole.Lamp,reverse))!.Active);
                Assert.Equal(2,world.Connections.Count);
                Assert.All(world.Connections,link=>Assert.Equal(ConnectionDomain.Activation,link.Type));
                return world.StateSignature();
            }
            var first=Run();
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,((CounterPart)world.FindPart(CrossingId(CrossingRole.Counter,reverse))!).Count);
            Assert.Equal(0,((BallDetectorPart)world.FindPart(CrossingId(CrossingRole.Detector,reverse))!).CrossingCount);
            Assert.All(world.Bodies,body=>Assert.Equal(Vector3.Right*4,body.InitialVelocity));
            Assert.Equal(first,Run());
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
                Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=5}});
            world.Start();
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
                Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=target}}));
            Assert.Empty(world.Parts);
        }
        finally {world.Free();}
    }
}
