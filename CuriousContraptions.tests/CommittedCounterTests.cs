using CuriousContraptions.Bridge;
using Godot;

namespace CuriousContraptions.Tests;

public class CommittedCounterBufferTests
{
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision*.01);
    private static readonly SimulationCounterId First=new(3),Second=new(19);
    [Fact]
    public void CountsAreOwnedAtomicAndPinnedWithTheirRevision()
    {
        SimulationCounterState[] input=[new(First,0,4),new(Second,1,7)];
        var buffer=new CommittedPoseBuffer(Stamp(0),[],input,[],[],[],[]);
        input[0]=new(First,2,4);
        var lease=buffer.Acquire();
        Assert.Equal(2,lease.CounterCount);Assert.Equal(0,lease.ReadCounter(PoseSample.Current,First).Count);
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));lease.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[],input,[],[],[],[]);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());
        input[0]=new(First,4,4);buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(Stamp(1),read.Stamp(PoseSample.Current));
            Assert.Equal(0,read.ReadCounter(PoseSample.Previous,First).Count);
            Assert.Equal(2,read.ReadCounter(PoseSample.Current,First).Count);
            Assert.Equal(1,read.ReadCounter(PoseSample.Current,Second).Count);
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),[],input,[],[],[],[]);buffer.Discard();
        var retained=buffer.Acquire();var owned=retained.ReadCounter(PoseSample.Current,First);
        Assert.Equal(2,owned.Count);
        Assert.Throws<ArgumentException>(()=>retained.ReadCounter(PoseSample.Current,new(20)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>retained.ReadCounter((PoseSample)999,First));
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>retained.ReadCounter(PoseSample.Current,First));
        Assert.Throws<InvalidOperationException>(()=>retained.CounterCount);
        retained.Dispose();Assert.Equal(2,owned.Count);
        Assert.Throws<InvalidOperationException>(()=>default(PoseReadLease).ReadCounter(PoseSample.Current,First));
    }
    public enum InvalidPublication { Negative,OverTarget,ZeroTarget,ChangedTarget,ChangedIdentity,Missing,Duplicate,Reordered }
    [Theory]
    [InlineData(InvalidPublication.Negative)]
    [InlineData(InvalidPublication.OverTarget)]
    [InlineData(InvalidPublication.ZeroTarget)]
    [InlineData(InvalidPublication.ChangedTarget)]
    [InlineData(InvalidPublication.ChangedIdentity)]
    [InlineData(InvalidPublication.Missing)]
    [InlineData(InvalidPublication.Duplicate)]
    [InlineData(InvalidPublication.Reordered)]
    public void InvalidStateCannotPartiallyPublish(InvalidPublication invalid)
    {
        SimulationCounterState[] initial=[new(First,0,4),new(Second,0,7)];
        SimulationCounterState[] next=invalid switch
        {
            InvalidPublication.Negative=>[new(First,-1,4),initial[1]],
            InvalidPublication.OverTarget=>[new(First,5,4),initial[1]],
            InvalidPublication.ZeroTarget=>[new(First,0,0),initial[1]],
            InvalidPublication.ChangedTarget=>[new(First,0,5),initial[1]],
            InvalidPublication.ChangedIdentity=>[new(new(4),0,4),initial[1]],
            InvalidPublication.Missing=>[initial[0]],
            InvalidPublication.Duplicate=>[initial[0],initial[0]],
            InvalidPublication.Reordered=>[initial[1],initial[0]],
            _=>throw new ArgumentOutOfRangeException(nameof(invalid))
        };
        if(invalid is InvalidPublication.Negative or InvalidPublication.OverTarget or InvalidPublication.ZeroTarget or InvalidPublication.Duplicate)
            Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),[],next,[],[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[],initial,[],[],[],[]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[],next,[],[],[],[]));buffer.Discard();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(Stamp(0),read.Stamp(PoseSample.Current));
            Assert.Equal(initial[0],read.ReadCounter(PoseSample.Current,First));
            Assert.Equal(initial[1],read.ReadCounter(PoseSample.Current,Second));
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[],[new(First,4,4),new(Second,7,7)],[],[],[],[]);buffer.Publish();
        using var final=buffer.Acquire();
        Assert.Equal(SimulationCounterPhase.Reached,final.ReadCounter(PoseSample.Current,First).Phase);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedCounterPublicationAllocatesNothing(int count)
    {
        var states=Enumerable.Range(0,count).Select(i=>new SimulationCounterState(new(i*2),0,2000)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),[],states,[],[],[],[]);
        for(var revision=1;revision<=100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),[],states,[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();
            for(var i=0;i<count;i++)_=read.ReadCounter(PoseSample.Current,states[i].Id);
        }
        var bytes=GC.GetAllocatedBytesForCurrentThread();var sum=0;
        for(var revision=101;revision<=1100;revision++)
        {
            for(var i=0;i<count;i++)states[i]=states[i] with {Count=revision};
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),[],states,[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();
            for(var i=0;i<count;i++)sum+=read.ReadCounter(PoseSample.Current,states[i].Id).Count;
        }
        bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
        Assert.Equal(0,bytes);Assert.Equal(count*600500,sum);
    }
}

[Collection<NativeSceneCollection>]
public class CommittedCounterWorldTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Counter=new("counter");
    private partial class Probe : CounterPart
    {
        public static readonly CounterSlot Other=new();
        public Action? Observe;
        public override IReadOnlyList<SceneCounterDeclaration> SimulationCounters=>
            [new(new(this,Deliveries),Target),new(new(this,Other),20)];
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void WholeTickCountersPublishTogetherAndFailedTickAndResetPreserveBoundaries(int failingSubstep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=new Probe {Definition=world.Registry.Definitions[Counter.Value]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Counter.Value,
                Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=9}});world.AttachPart(part);
            var construction=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            SceneCounterKey first=new(part,CounterPart.Deliveries),second=new(part,Probe.Other);
            var firstId=world.ReadCounter(first).Id;var secondId=world.ReadCounter(second).Id;
            var visits=0;var failAt=0;
            part.Observe=()=>
            {
                world.IncrementCounter(first);world.IncrementCounter(second);
                if(++visits==failAt)throw new InvalidOperationException("Injected counter publication failure.");
            };
            world.Step();
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(2,read.CounterCount);
                Assert.Equal(0,read.ReadCounter(PoseSample.Previous,firstId).Count);
                Assert.Equal(4,read.ReadCounter(PoseSample.Current,firstId).Count);
                Assert.Equal(4,read.ReadCounter(PoseSample.Current,secondId).Count);
            }
            var physics=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            failAt=visits+failingSubstep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(tick,world.Ticks);Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(4,world.ReadCounter(first).Count);Assert.Equal(4,world.ReadCounter(second).Count);
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(tick,read.Stamp(PoseSample.Current).Revision.Value);
                Assert.Equal(4,read.ReadCounter(PoseSample.Current,firstId).Count);
                Assert.Equal(4,read.ReadCounter(PoseSample.Current,secondId).Count);
            }
            failAt=0;world.Step();world.Step(); // No intervening frame or read.
            var old=world.ReadCommittedPoses();var generation=old.Stamp(PoseSample.Current).Generation;
            Assert.Equal(8,old.ReadCounter(PoseSample.Previous,firstId).Count);
            Assert.Equal(9,old.ReadCounter(PoseSample.Current,firstId).Count);
            Assert.Equal(12,old.ReadCounter(PoseSample.Current,secondId).Count);
            world.Restore();Assert.Throws<InvalidOperationException>(()=>old.ReadCounter(PoseSample.Current,firstId));old.Dispose();
            Assert.Equal(construction,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();
            using var fresh=world.ReadCommittedPoses();
            Assert.NotEqual(generation,fresh.Stamp(PoseSample.Current).Generation);
            Assert.Equal(0,fresh.ReadCounter(PoseSample.Current,new(0)).Count);
            Assert.Equal(1,fresh.CounterCount); // Restored catalogue part has its declared single counter.
        }
        finally{world.Free();}
    }
}
