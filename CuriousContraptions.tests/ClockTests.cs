using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ClockTests(NativeSceneFixture godot)
{
    [Fact]
    public void OwnedReadingsAndCanonicalParameterBoundaryRestoreExactly()
    {
        Assert.Equal("interval_seconds",PartParameterName.Of(ClockParameter.IntervalSeconds));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((ClockParameter)99));
        Assert.Throws<ArgumentException>(()=>PartParameterName.RequireExact<ClockParameter>([]));
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var clock=(ClockPart)world.AddPart(new(){Id="clock",Kind="clock"});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,4,0]});
            Assert.True(world.Connect(battery,clock));world.Start();
            var stopped=world.Oscillators.Capture();
            world.Step();var running=world.Oscillators.Capture();var due=clock.DueTick;
            while(world.Ticks<=due)world.Step();
            Assert.Equal(1,clock.PulseCount);Assert.Equal(due,clock.LastPulseTick);
            world.Oscillators.Restore(running);
            Assert.Equal(SimulationOscillatorPhase.Running,clock.State);
            Assert.Equal(due,clock.DueTick);Assert.Equal(0,clock.PulseCount);
            Assert.Equal(-1,clock.LastPulseTick);Assert.Equal(0,clock.Progress);
            world.Oscillators.Restore(stopped);
            Assert.Equal(SimulationOscillatorPhase.Stopped,clock.State);
            Assert.Equal(-1,clock.DueTick);Assert.Equal(0,clock.PulseCount);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(.1f,false)]
    [InlineData(.1f,true)]
    [InlineData(1f,false)]
    [InlineData(1f,true)]
    [InlineData(12f,false)]
    [InlineData(12f,true)]
    public void FullIntervalsRepeatOncePerTickRegardlessOfEntityOrder(float seconds,bool reverse)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var clockId=reverse?"z-clock":"a-clock";
            var specs=new List<PartSpec>{
                new(){Id=clockId,Kind="clock",Properties=new(){[PartParameterName.Of(ClockParameter.IntervalSeconds)]=seconds}},
                new(){Id="battery",Kind="battery",Position=[-4,4,0]},
                new(){Id="counter",Kind="counter",Position=[4,4,0],Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=9}}};
            if(reverse)specs.Reverse();
            foreach(var spec in specs)world.AddPart(spec);
            var clock=(ClockPart)world.FindPart(clockId)!;
            var counter=(CounterPart)world.FindPart("counter")!;
            Assert.True(world.Connect(world.FindPart("battery")!,clock));
            Assert.True(world.Connect(clock,counter));
            world.Start();
            var interval=clock.IntervalTicks;
            for(var tick=0;tick<=interval*4;tick++)
            {
                world.Step();
                Assert.Equal(tick/interval,clock.PulseCount);
                Assert.Equal(clock.PulseCount,counter.Count);
                Assert.Equal(tick>0&&tick%interval==0,clock.Active);
                Assert.InRange(clock.Progress,0,1);
            }
            Assert.Equal(interval*4,clock.LastPulseTick);
            world.Restore();
            clock=(ClockPart)world.FindPart(clockId)!;
            Assert.Equal(SimulationOscillatorPhase.Stopped,clock.State);
            Assert.Equal(0,clock.PulseCount);
            Assert.Equal(-1,clock.DueTick);
            Assert.Equal(seconds,clock.Interval);
        }
        finally{world.Free();}
    }

    [Fact]
    public void NoSupplyMeansNoEventsAndRestorationStartsAFullNewInterval()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var clock=(ClockPart)world.AddPart(new(){Id="clock",Kind="clock"});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,4,0]});
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,clock));
            world.Start();
            for(var i=0;i<150;i++)world.Step();
            Assert.Equal(0,clock.PulseCount);
            Assert.Equal(SimulationOscillatorPhase.Stopped,clock.State);
            supply.SetAndSettle(SimulationLatchPhase.On);
            for(var i=0;i<60;i++)world.Step();
            Assert.Equal(0,clock.PulseCount);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.Equal(0,clock.Progress);
            for(var i=0;i<150;i++)world.Step();
            var restoredTick=supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.Equal(restoredTick+clock.IntervalTicks,clock.DueTick);
            while(world.Ticks<clock.DueTick)world.Step();
            Assert.Equal(0,clock.PulseCount);
            world.Step();
            Assert.Equal(1,clock.PulseCount);
            Assert.Equal(restoredTick+clock.IntervalTicks,clock.LastPulseTick);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.False(clock.Active);
            Assert.Equal(-1,clock.DueTick);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(.01f)]
    [InlineData(13f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RejectsInvalidInterval(float seconds)
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="clock",Kind="clock",
                Properties=new(){[PartParameterName.Of(ClockParameter.IntervalSeconds)]=seconds}}));
            Assert.Empty(world.Parts);
        }
        finally{world.Free();}
    }
}
