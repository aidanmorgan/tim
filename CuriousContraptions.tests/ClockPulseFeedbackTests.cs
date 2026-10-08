using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ClockPulseFeedbackTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Clock=new("clock"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected pulse tick failure.");}
    }
    private partial class FanOutClock : ClockPart
    {
        private MeshInstance3D _other=null!;
        protected override void Build()
        {
            base.Build();_other=PartArt.Sphere(Visual,.05f,new("#556573"),new(.3f,.63f,.39f));
        }
        public override IReadOnlyList<SceneOscillatorColour> OscillatorColourAnimations
        {
            get
            {
                var original=base.OscillatorColourAnimations[0];
                AnimationImpulseDefinition Definition(int capacity)=>new(.25,AnimationImpulseCurve.LinearDecay,
                    AnimationImpulseOverlap.Maximum,AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,
                    capacity,AnimationImpulseTiming.EventTimeRetainFirstFrame,0);
                return [original with {Definition=Definition(2)},original with {Target=_other,Definition=Definition(1)}];
            }
        }
    }
    private partial class BurstClock : ClockPart
    {
        public const int Sources=128;
        private readonly OscillatorSlot[] _slots=Enumerable.Range(0,Sources-1).Select(_=>new OscillatorSlot()).ToArray();
        public override IReadOnlyList<SceneOscillatorDeclaration> SimulationOscillators=>
            new[]{new SceneOscillatorDeclaration(new(this,PulseSchedule),MachineWorld.Tick,SocketId.PowerIn)}
                .Concat(_slots.Select(slot=>new SceneOscillatorDeclaration(new(this,slot),MachineWorld.Tick,SocketId.PowerIn))).ToArray();
    }
    private static Color Colour(SceneOscillatorColour declaration)=>((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor;
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static SceneOscillatorKey Key(ClockPart clock)=>new(clock,ClockPart.PulseSchedule);
    private static T Attach<T>(MachineWorld world) where T:ClockPart,new()
    {
        var clock=new T {Definition=world.Registry.Definitions[Clock.Value]};
        clock.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Clock.Value,Position=[0,5,0],
            Properties=new(){[PartParameterName.Of(ClockParameter.IntervalSeconds)]=.1f}});
        world.AttachPart(clock);return clock;
    }
    private static Probe Power(MachineWorld world,ClockPart clock)
    {
        var source=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
        source.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0]});world.AttachPart(source);
        Assert.True(world.Connect(source,SocketId.Supply,clock,SocketId.PowerIn,ConnectionDomain.Electrical));return source;
    }
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void FailedPulseDoesNotPublishAndSkippedFramesRetainAllOccurrencesThroughResetSave(int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var clock=Attach<ClockPart>(world);var source=Power(world,clock);var lamp=Assert.Single(clock.OscillatorColourAnimations);
            var saved=Saved(world);world.Start();world.Step();
            Assert.Equal(0,world.OscillatorEvents.PendingCount);Assert.Equal(0,clock.PulseCount);
            while(world.Ticks<clock.DueTick)world.Step();
            var stamp=world.OscillatorEvents.CurrentStamp;source.FailAt=source.Visits+substep;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(stamp,world.OscillatorEvents.CurrentStamp);Assert.Equal(0,world.OscillatorEvents.PendingCount);
            Assert.Equal(0,clock.PulseCount);world.PresentFrame(.1,1);Assert.Equal(lamp.From,Colour(lamp));
            source.FailAt=0;world.Step();Assert.Equal(1,world.OscillatorEvents.PendingCount);
            var first=world.OscillatorEvents.Peek();Assert.Equal(clock.LastPulseTick,first.Payload.Tick);
            Assert.Equal(world.ControlRevision,first.Stamp.Revision);Assert.Equal(lamp.From,Colour(lamp));
            while(clock.PulseCount<4)world.Step();
            Assert.Equal(4,world.OscillatorEvents.PendingCount);Assert.Equal(first,world.OscillatorEvents.Peek());
            world.Running=false;var ticks=world.Ticks;var physical=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(0,1);
            Assert.Equal(0,world.OscillatorEvents.PendingCount);Assert.Equal(4UL,world.ReadOscillatorFeedback(Key(clock),0).Accepted);
            Assert.Equal(lamp.To,Colour(lamp)); // Expired first occurrence retains its first visible frame.
            world.PresentFrame(.3,1);Assert.Equal(lamp.From,Colour(lamp));
            Assert.Equal(4UL,world.ReadOscillatorFeedback(Key(clock),0).Completed);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            var events=world.OscillatorEvents;world.Restore();Assert.Equal(EventStreamPhase.Removed,events.Phase);
            Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            world.Start();world.PresentFrame(.5,1);
            var restored=(ClockPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            var restoredLamp=Assert.Single(restored.OscillatorColourAnimations);Assert.Equal(restoredLamp.From,Colour(restoredLamp));
            Assert.Equal(0UL,world.ReadOscillatorFeedback(Key(restored),0).Accepted);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void FanOutAdmissionIsAtomicAndHiddenEssentialFeedbackRetainsBackpressure()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var clock=Attach<FanOutClock>(world);Power(world,clock);world.Start();
            while(clock.PulseCount<4)world.Step();world.Running=false;clock.Visible=false;
            world.PresentFrame(1,1);
            Assert.Equal(3,world.OscillatorEvents.PendingCount);
            for(var i=0;i<2;i++)
            {var read=world.ReadOscillatorFeedback(Key(clock),i);Assert.Equal(1UL,read.Accepted);Assert.Equal(1,read.Pending);}
            world.PresentFrame(1,1);Assert.Equal(3,world.OscillatorEvents.PendingCount);
            clock.Visible=true;world.PresentFrame(0,1);
            foreach(var lamp in clock.OscillatorColourAnimations)Assert.Equal(lamp.To,Colour(lamp));
            world.PresentFrame(.3,1);Assert.Equal(3,world.OscillatorEvents.PendingCount);
            world.PresentFrame(0,1);Assert.Equal(2,world.OscillatorEvents.PendingCount);
            for(var i=0;i<2;i++)Assert.Equal(2UL,world.ReadOscillatorFeedback(Key(clock),i).Accepted);
            for(var i=0;i<6;i++)world.PresentFrame(.3,1);
            Assert.Equal(0,world.OscillatorEvents.PendingCount);
            for(var i=0;i<2;i++)
            {var read=world.ReadOscillatorFeedback(Key(clock),i);Assert.Equal(4UL,read.Accepted);Assert.Equal(4UL,read.Completed);}
        }
        finally{world.Free();}
    }
    [Fact]
    public void WorldCapacityRejectsWholeTickAndExactRetryPreservesPriorOccurrences()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var clock=Attach<BurstClock>(world);Power(world,clock);world.Start();world.Step();
            while(world.OscillatorEvents.PendingCount<MachineWorld.MaximumPendingOscillatorEvents)world.Step();
            var first=world.OscillatorEvents.Peek();var stamp=world.OscillatorEvents.CurrentStamp;
            var state=world.ReadOscillator(Key(clock));var physical=world.Physics.Capture().BodyStates.ToArray();
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(stamp,world.OscillatorEvents.CurrentStamp);Assert.Equal(first,world.OscillatorEvents.Peek());
            Assert.Equal(MachineWorld.MaximumPendingOscillatorEvents,world.OscillatorEvents.PendingCount);
            Assert.Equal(state,world.ReadOscillator(Key(clock)));Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(EventStreamPhase.Idle,world.OscillatorEvents.Phase);
            world.PresentFrame(0,1);Assert.Equal(0,world.OscillatorEvents.PendingCount);
            world.Step();Assert.Equal(state.PulseCount+1,clock.PulseCount);
            Assert.Equal(BurstClock.Sources,world.OscillatorEvents.PendingCount);
            Assert.Equal((ulong)MachineWorld.MaximumPendingOscillatorEvents+1,world.OscillatorEvents.Peek().Id.Sequence.Value);
            var events=world.OscillatorEvents;world.Free();Assert.Equal(EventStreamPhase.Removed,events.Phase);
        }
        finally{if(GodotObject.IsInstanceValid(world))world.Free();}
    }
    [Fact]
    public void SignedOccurrenceAgeAndAdmissionPreflightDoNotMutateOnFailure()
    {
        var batch=new AnimationBatch(1);var handle=batch.RegisterImpulses(new(new(0),AnimationProperty.ColourBlend),
            new(1,AnimationImpulseCurve.LinearDecay,AnimationImpulseOverlap.Maximum,AnimationImpulseVisibility.DeferUntilVisible,
                AnimationClock.Presentation,1,AnimationImpulseTiming.EventTimeRetainFirstFrame,0),0,1);
        Assert.Equal(AnimationImpulseAdmission.Accepted,batch.ValidateImpulseAdmission(handle,new(1),1,-.25));
        Assert.Equal(0UL,batch.ReadImpulses(handle).Accepted);
        batch.EnqueueImpulse(handle,new(1),1,-.25);batch.Advance(1,0,0);Assert.Equal(.75,batch.Read(handle).Value);
        Assert.Equal(AnimationImpulseAdmission.CapacityExhausted,batch.ValidateImpulseAdmission(handle,new(2),1,-1));
        Assert.Throws<ArgumentException>(()=>batch.ValidateImpulseAdmission(handle,new(1),1,0));
        Assert.Equal(1UL,batch.ReadImpulses(handle).Accepted);
    }

    public enum EventFault { Stream, Generation, Revision, Source, Tick, Sequence }
    [Theory]
    [InlineData(EventFault.Stream)]
    [InlineData(EventFault.Generation)]
    [InlineData(EventFault.Revision)]
    [InlineData(EventFault.Source)]
    [InlineData(EventFault.Tick)]
    [InlineData(EventFault.Sequence)]
    public void InvalidEventBoundaryCannotAcknowledgeOrAdmit(EventFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var clock=Attach<ClockPart>(world);var declaration=Assert.Single(clock.OscillatorColourAnimations);
            var id=new SimulationOscillatorId(0);var adapter=new SceneAnimationAdapter(1);
            var feedback=new SceneOscillatorFeedback(adapter,world.Parts,[(clock,declaration)],
                new Dictionary<SceneOscillatorKey,SimulationOscillatorId>{{Key(clock),id}});
            var seed=new PoseReadStamp(new(1),new(0),0);
            var events=new CommittedEventStream<SimulationOscillatorPulse>(
                fault==EventFault.Stream?new EventStreamId(2):SceneOscillatorFeedback.StreamId,seed,1);
            var stamp=new PoseReadStamp(seed.Generation,new(1),MachineWorld.Tick);events.Begin(stamp);
            events.Append(new(fault==EventFault.Source?new(99):id,fault==EventFault.Tick?1:0,fault==EventFault.Sequence?0:1));
            events.Seal();events.Commit();
            var requested=fault switch
            {
                EventFault.Generation=>stamp with {Generation=new(2)},
                EventFault.Revision=>stamp with {Revision=new(2)},
                _=>stamp
            };
            Assert.Throws<ArgumentException>(()=>feedback.Consume(events,requested,0));
            Assert.Equal(1,events.PendingCount);Assert.Equal(0UL,feedback.Read(Key(clock),0).Accepted);
            Assert.Equal(declaration.From,Colour(declaration));adapter.Reset();
        }
        finally{world.Free();}
    }
    [Fact]
    public void ResetCancelsAcceptedAndQueuedOccurrencesAndInvalidatesOldRun()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var clock=Attach<FanOutClock>(world);Power(world,clock);var saved=Saved(world);world.Start();
            while(clock.PulseCount<3)world.Step();
            clock.Visible=false;world.PresentFrame(0,1);
            Assert.Equal(2,world.OscillatorEvents.PendingCount);
            Assert.Equal(1,world.ReadOscillatorFeedback(Key(clock),0).Pending);
            var events=world.OscillatorEvents;world.Restore();
            Assert.Equal(EventStreamPhase.Removed,events.Phase);Assert.Equal(saved,Saved(world));
            Assert.Throws<InvalidOperationException>(()=>events.Peek());
            world.Start();world.PresentFrame(0,1);
            var restored=(ClockPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,world.OscillatorEvents.PendingCount);
            Assert.Equal(0UL,world.ReadOscillatorFeedback(Key(restored),0).Accepted);
            Assert.Equal(Assert.Single(restored.OscillatorColourAnimations).From,Colour(Assert.Single(restored.OscillatorColourAnimations)));
        }
        finally{world.Free();}
    }
}
