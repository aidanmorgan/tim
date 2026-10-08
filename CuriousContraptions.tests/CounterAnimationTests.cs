using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CounterAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Counter=new("counter"),Switch=new("switch"),Battery=new("battery"),Gate=new("powered_gate");
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Color Colour(SceneColourAnimation declaration)=>((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor;
    public static IEnumerable<object[]> Targets()
    {
        for(var target=1;target<=9;target++)
        foreach(var supplied in new[]{false,true})yield return [target,supplied];
    }
    [Theory]
    [MemberData(nameof(Targets))]
    public void EveryConfiguredTargetShowsCommittedProgressWithSupplyControlAndExactReset(int target,bool supplied)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var source=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Switch.Value,Position=[-4,6,0]});
            var counter=(CounterPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Counter.Value,
                Position=[0,6,0],Properties=new(){[PartParameterName.Of(CounterParameter.TargetCount)]=target}});
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[0,10,4]});
            var gate=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Gate.Value,Position=[4,6,0]});
            Assert.True(world.Connect(source,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            if(supplied)Assert.True(world.Connect(battery,SocketId.Supply,counter,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(counter,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            var lamps=counter.ColourAnimations;
            Assert.Equal(target,lamps.Count);
            for(var i=0;i<target;i++)
            {
                Assert.Equal(SceneAnimationFeedback.CounterThreshold,lamps[i].Feedback.Kind);
                Assert.Equal(new SceneCounterKey(counter,CounterPart.Deliveries),lamps[i].Feedback.Counter);
                Assert.Equal(i+1,lamps[i].Feedback.Threshold);Assert.Equal(lamps[i].From,Colour(lamps[i]));
            }
            var saved=Saved(world);var pose=counter.Transform;
            world.Start();var empty=world.Counters.Capture();
            for(var count=1;count<=target;count++)
            {
                world.Activate(source);Assert.Equal(count,counter.Count);
                world.Running=false;world.PresentFrame(.05,1);
                Assert.Equal(lamps[count-1].From,Colour(lamps[count-1])); // Live count is not yet committed.
                world.Running=true;world.Step();
                Assert.Equal(supplied&&count==target,gate.HasElectricalPower(SocketId.PowerIn));
                var physics=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
                world.Running=false;world.PresentFrame(.05,1);
                for(var i=0;i<target;i++)
                    Assert.Equal(i<count-1?lamps[i].To:i==count-1?lamps[i].From.Lerp(lamps[i].To,.5f):lamps[i].From,Colour(lamps[i]));
                Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
                world.PresentFrame(.05,1);
                for(var i=0;i<target;i++)Assert.Equal(i<count?lamps[i].To:lamps[i].From,Colour(lamps[i]));
                world.Running=true;
            }
            Assert.Equal(SimulationCounterPhase.Reached,counter.State);
            var completed=world.Counters.Capture();world.Counters.Restore(empty);
            world.Running=false;world.PresentFrame(.2,1);
            Assert.Equal(0,counter.Count);foreach(var lamp in lamps)Assert.Equal(lamp.To,Colour(lamp));
            world.Counters.Restore(completed);world.Running=true;
            world.Activate(source);world.Step();Assert.Equal(target,counter.Count);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(CounterPart)world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(0,restored.Count);Assert.Equal(target,restored.Target);
            foreach(var lamp in restored.ColourAnimations)Assert.Equal(lamp.From,Colour(lamp));
        }
        finally{world.Free();}
    }
    private partial class Probe : CounterPart
    {
        public int Visits,SendAt,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            Visits++;
            if(Visits==SendAt)world.Activate(this);
            if(Visits==FailAt)throw new InvalidOperationException("Injected counter animation failure.");
        }
    }
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void RejectedTickCannotLightLampAndAcceptedRetryCanFinishWhilePaused(int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=new Probe {Definition=world.Registry.Definitions[Counter.Value]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Counter.Value});world.AttachPart(part);
            var lamp=part.ColourAnimations[0];var saved=Saved(world);
            world.Start();part.SendAt=part.FailAt=substep;
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(0,part.Count);Assert.Equal(0,world.Ticks);
            world.PresentFrame(.2,1);Assert.Equal(lamp.From,Colour(lamp));
            part.FailAt=0;part.SendAt=part.Visits+1;world.Step();
            Assert.Equal(1,part.Count);Assert.Equal(lamp.From,Colour(lamp));
            world.Running=false;world.PresentFrame(.025,1);
            Assert.Equal(lamp.From.Lerp(lamp.To,.103515625f),Colour(lamp));
            world.PresentFrame(.1,1);Assert.Equal(lamp.To,Colour(lamp));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum InvalidBinding { UnknownSlot,ForeignOwner,AboveTarget }
    private partial class InvalidCounter : CounterPart
    {
        public InvalidBinding Failure;
        public MachinePart Foreign=null!;
        private readonly CounterSlot _missing=new();
        public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
            [base.ColourAnimations[0] with { Feedback=Failure switch
            {
                InvalidBinding.UnknownSlot=>SceneAnimationSignal.CounterAtLeast(new(this,_missing),1),
                InvalidBinding.ForeignOwner=>SceneAnimationSignal.CounterAtLeast(new(Foreign,Deliveries),1),
                InvalidBinding.AboveTarget=>SceneAnimationSignal.CounterAtLeast(new(this,Deliveries),Target+1),
                _=>throw new ArgumentOutOfRangeException()
            }}];
    }
    [Theory]
    [InlineData(InvalidBinding.UnknownSlot)]
    [InlineData(InvalidBinding.ForeignOwner)]
    [InlineData(InvalidBinding.AboveTarget)]
    public void InvalidCounterBindingRejectsWithoutInstallingRun(InvalidBinding failure)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Counter.Value,Position=[4,6,0]});
            var part=new InvalidCounter {Definition=world.Registry.Definitions[Counter.Value],Foreign=foreign,Failure=failure};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Counter.Value});world.AttachPart(part);
            var saved=Saved(world);Assert.Throws<ArgumentException>(world.Start);
            Assert.False(world.HasPhysicsState);Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void MalformedSignalRejects()
    {
        Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.CounterAtLeast(default,1));
        var part=new CounterPart();
        try
        {
            Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.CounterAtLeast(new(part,null!),1));
            Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.CounterAtLeast(new(part,CounterPart.Deliveries),0));
            Assert.Throws<ArgumentException>(()=>SceneAnimationSignal.CounterAtLeast(new(part,CounterPart.Deliveries),-1));
        }
        finally{part.Free();}
    }

    private partial class DualCounter : CounterPart
    {
        public static readonly CounterSlot Secondary=new();
        public override IReadOnlyList<SceneCounterDeclaration> SimulationCounters=>
            [new(new(this,Deliveries),Target),new(new(this,Secondary),2)];
        public override IReadOnlyList<SceneRotationAnimation> RotationAnimations=>
            [new(base.ColourAnimations[0].Target,
                new(0,.5,.1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),
                AnimationRotationAxis.Z,SceneAnimationSignal.CounterAtLeast(new(this,Secondary),2),SceneAnimationDrive.Endpoint)];
    }
    [Fact]
    public void IndependentCounterSlotCanDriveRotationWithoutActivatingPrimaryLamps()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=new DualCounter {Definition=world.Registry.Definitions[Counter.Value]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Counter.Value});world.AttachPart(part);
            var rotation=Assert.Single(part.RotationAnimations);var baseline=rotation.Target.Transform;
            world.Start();SceneCounterKey secondary=new(part,DualCounter.Secondary);
            world.IncrementCounter(secondary);world.Step();world.PresentFrame(.2,1);
            Assert.Equal(baseline,rotation.Target.Transform);
            world.IncrementCounter(secondary);world.Step();
            Assert.Equal(baseline,rotation.Target.Transform);
            world.Running=false;world.PresentFrame(.05,1);
            var expected=baseline.Basis*new Basis(Vector3.Back,.25f);
            Assert.InRange((expected.X-rotation.Target.Basis.X).Length(),0,1e-6f);
            Assert.InRange((expected.Y-rotation.Target.Basis.Y).Length(),0,1e-6f);
            Assert.Equal(0,part.Count);
            foreach(var lamp in part.ColourAnimations)Assert.Equal(lamp.From,Colour(lamp));
            world.Restore();
        }
        finally{world.Free();}
    }

    [Fact]
    public void ChangedCounterTargetRejectsBeforeFeedbackAndValidRetryStillApplies()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=(CounterPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Counter.Value});
            world.Start();var state=world.ReadCounter(new(part,CounterPart.Deliveries));
            var map=new Dictionary<SceneCounterKey,SimulationCounterId>{[new(part,CounterPart.Deliveries)]=state.Id};
            SceneAnimationRun run;PoseReadStamp stamp;ElectricalInputRead[] electrical;
            using(var seed=world.ReadCommittedPoses())
            {run=new(world.Parts,world.PhysicsAssembly,seed,map,new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());stamp=seed.Stamp(PoseSample.Current);
                electrical=new ElectricalInputRead[seed.ElectricalInputCount];seed.CopyElectricalInputs(PoseSample.Current,electrical);}
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var altered=state with {Target=state.Target+1,Count=1};
            var foreign=new CommittedPoseBuffer(stamp,bodies,[altered],electrical,[],[],[]);
            foreign.BeginWrite(0);
            foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[altered],electrical,[],[],[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.2);foreach(var lamp in part.ColourAnimations)Assert.Equal(lamp.From,Colour(lamp));
            world.Activate(part);world.Step();
            using(var read=world.ReadCommittedPoses())run.Publish(read);
            run.Present(.1);Assert.Equal(part.ColourAnimations[0].To,Colour(part.ColourAnimations[0]));
            run.Remove();
        }
        finally{world.Free();}
    }
}
