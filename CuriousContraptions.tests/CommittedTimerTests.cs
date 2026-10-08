using System.Runtime.CompilerServices;
using CuriousContraptions.Bridge;
using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

public class CommittedTimerTests
{
    private static readonly SimulationTimerId First=new(2),Second=new(7);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static SimulationTimerState Ready(SimulationTimerId id)=>new(id,SimulationTimerPhase.Ready,-1,-1);
    private static SimulationTimerState Counting(SimulationTimerId id)=>new(id,SimulationTimerPhase.Counting,0,12);
    public enum Invalidity { Phase, NegativeStart, MissingDue, ReversedInterval, CountingWithoutInterval, FinishedWithoutInterval, Duplicate, Order, Count, Identity }
    [Theory]
    [InlineData(Invalidity.Phase)]
    [InlineData(Invalidity.NegativeStart)]
    [InlineData(Invalidity.MissingDue)]
    [InlineData(Invalidity.ReversedInterval)]
    [InlineData(Invalidity.CountingWithoutInterval)]
    [InlineData(Invalidity.FinishedWithoutInterval)]
    [InlineData(Invalidity.Duplicate)]
    [InlineData(Invalidity.Order)]
    [InlineData(Invalidity.Count)]
    [InlineData(Invalidity.Identity)]
    public void MalformedTimerRejectsBeforePublicationAndValidStageRetries(Invalidity invalidity)
    {
        SimulationTimerState[] initial=[Ready(First),Ready(Second)];
        SimulationTimerState[] next=[Counting(First),Counting(Second)];
        next=invalidity switch
        {
            Invalidity.Phase=>[next[0],next[1] with {Phase=(SimulationTimerPhase)999}],
            Invalidity.NegativeStart=>[next[0],next[1] with {StartedTick=-2}],
            Invalidity.MissingDue=>[next[0],next[1] with {DueTick=-1}],
            Invalidity.ReversedInterval=>[next[0],next[1] with {StartedTick=12}],
            Invalidity.CountingWithoutInterval=>[next[0],Ready(Second) with {Phase=SimulationTimerPhase.Counting}],
            Invalidity.FinishedWithoutInterval=>[next[0],Ready(Second) with {Phase=SimulationTimerPhase.Finished}],
            Invalidity.Duplicate=>[next[0],next[0]],
            Invalidity.Order=>[next[1],next[0]],
            Invalidity.Count=>[next[0]],
            Invalidity.Identity=>[next[0],Counting(new(99))],
            _=>throw new ArgumentOutOfRangeException(nameof(invalidity))
        };
        if(invalidity is not (Invalidity.Order or Invalidity.Count or Invalidity.Identity))
            Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),[],[],[],[],next,[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],initial,[]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[],[],[],[],next,[]));
        buffer.Stage(Stamp(1),[],[],[],[],initial,[]);buffer.Publish();
        using var read=buffer.Acquire();Assert.Equal(Ready(First),read.ReadTimer(PoseSample.Current,First));
        Assert.Equal(Ready(Second),read.ReadTimer(PoseSample.Current,Second));
    }
    [Fact]
    public void TimerOwnershipPinningDiscardRemovalAndCopiesShareSnapshotLifetime()
    {
        SimulationTimerState[] values=[Ready(First)];
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],values,[]);
        values[0]=Counting(First);var lease=buffer.Acquire();
        Assert.Equal(Ready(First),lease.ReadTimer(PoseSample.Current,First));
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));lease.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[],[],[],[],values,[]);
        values[0]=values[0] with {Phase=SimulationTimerPhase.Finished};
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(Ready(First),read.ReadTimer(PoseSample.Previous,First));
            Assert.Equal(Counting(First),read.ReadTimer(PoseSample.Current,First));
            Assert.Equal(1,read.TimerCount);
            var copy=new SimulationTimerState[1];read.CopyTimers(PoseSample.Current,copy);
            Assert.Equal(Counting(First),Assert.Single(copy));
            Assert.Throws<ArgumentException>(()=>read.ReadTimer(PoseSample.Current,Second));
            Assert.Throws<ArgumentException>(()=>read.CopyTimers(PoseSample.Current,[]));
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.ReadTimer((PoseSample)999,First));
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.CopyTimers((PoseSample)999,copy));
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),[],[],[],[],values,[]);buffer.Discard();
        var final=buffer.Acquire();Assert.Equal(Counting(First),final.ReadTimer(PoseSample.Current,First));
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>final.ReadTimer(PoseSample.Current,First));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedTimerPublicationAllocatesNothing(int count)
    {
        var values=Enumerable.Range(0,count).Select(i=>Counting(new(i))).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],values,[]);var copy=new SimulationTimerState[count];
        for(var i=1;i<=100;i++)
        {buffer.BeginWrite(0);buffer.Stage(Stamp(i),[],[],[],[],values,[]);buffer.Publish();using var read=buffer.Acquire();read.CopyTimers(PoseSample.Current,copy);}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)
        {buffer.BeginWrite(0);buffer.Stage(Stamp(i),[],[],[],[],values,[]);buffer.Publish();using var read=buffer.Acquire();read.CopyTimers(PoseSample.Current,copy);}
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"Timer publication: count={count}, bytes_per_read={Unsafe.SizeOf<SimulationTimerState>()}, cycles=1000, allocated_bytes={allocated}");
        Assert.Equal(0,allocated);Assert.Equal(values,copy);
    }
}

[Collection<NativeSceneCollection>]
public class CommittedTimerWorldTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Delay=new("delay"),Hold=new("hold_timer"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected timer state publication failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void DelayCompletionAndTriggeredHoldPublishTogetherOrRollback(int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var delay=(DelayPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Delay.Value,Position=[0,5,0],
                Properties=new(){[PartParameterName.Of(DelayParameter.DelaySeconds)]=.1f}});
            var hold=(HoldTimerPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Hold.Value,Position=[4,5,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[-4,5,0]});world.AttachPart(probe);
            Assert.True(world.Connect(delay,SocketId.ActivationOut,hold,SocketId.ActivationIn,ConnectionDomain.Activation));
            var saved=Saved(world);world.Start();
            var delayId=world.ReadTimer(new(delay,DelayPart.Countdown)).Id;
            var holdId=world.ReadTimer(new(hold,HoldTimerPart.ContactWindow)).Id;
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(2,read.TimerCount);
                Assert.Equal(SimulationTimerPhase.Ready,read.ReadTimer(PoseSample.Current,delayId).Phase);
            }
            world.Activate(delay);
            using(var read=world.ReadCommittedPoses())Assert.Equal(SimulationTimerPhase.Ready,read.ReadTimer(PoseSample.Current,delayId).Phase);
            while(world.Ticks<delay.DueTick)world.Step();
            var revision=world.ControlRevision;var physical=world.Physics.Capture().BodyStates.ToArray();
            var before=new SimulationTimerState[2];
            using(var read=world.ReadCommittedPoses())read.CopyTimers(PoseSample.Current,before);
            probe.FailAt=probe.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(revision,world.ControlRevision);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(SimulationTimerPhase.Counting,delay.State);Assert.Equal(SimulationTimerPhase.Ready,hold.State);
            using(var read=world.ReadCommittedPoses())
            {
                var after=new SimulationTimerState[2];read.CopyTimers(PoseSample.Current,after);Assert.Equal(before,after);
            }
            probe.FailAt=0;world.Step();
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(SimulationTimerPhase.Finished,read.ReadTimer(PoseSample.Current,delayId).Phase);
                Assert.Equal(SimulationTimerPhase.Counting,read.ReadTimer(PoseSample.Current,holdId).Phase);
                Assert.Equal(SimulationTimerPhase.Counting,read.ReadTimer(PoseSample.Previous,delayId).Phase);
                Assert.Equal(SimulationTimerPhase.Ready,read.ReadTimer(PoseSample.Previous,holdId).Phase);
                Assert.Equal(world.ReadTimer(new(delay,DelayPart.Countdown)),read.ReadTimer(PoseSample.Current,delayId));
            }
            var completed=world.Timers.Capture();world.Running=false;world.PresentFrame(.1,1);
            Assert.Equal(completed.Tick,world.Timers.Tick);
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadTimer(PoseSample.Current,delayId));
            Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();
            using(var read=world.ReadCommittedPoses())Assert.Equal(SimulationTimerPhase.Ready,read.ReadTimer(PoseSample.Current,delayId).Phase);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
}
