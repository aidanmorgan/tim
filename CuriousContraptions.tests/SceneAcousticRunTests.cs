using Godot;
using System.Collections;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneAcousticRunTests(NativeSceneFixture godot)
{
    private static PoseReadStamp Stamp(int tick)=>new(new(1),new(tick+1),(tick+1)*(double)MachineWorld.Tick);
    private static PoseReadStamp Seed=>new(new(1),new(0),0);
    private partial class Source : BatteryPart
    {
        public List<AcousticPulse> Pulses { get; }=[];
        public SceneAcousticBinding? Binding { get; set; }
        public int Enumerations,DetachAtEnumeration;
        private PulseView? _view;
        public override IReadOnlyList<AcousticPulse> AcousticPulses=>_view??=new(this);
        public override SceneAcousticBinding? AcousticPlayback=>Binding;
        private sealed class PulseView(Source owner):IReadOnlyList<AcousticPulse>
        {
            public int Count=>owner.Pulses.Count;
            public AcousticPulse this[int index]=>owner.Pulses[index];
            public IEnumerator<AcousticPulse> GetEnumerator()
            {
                foreach(var pulse in owner.Pulses)yield return pulse;
                if(++owner.Enumerations==owner.DetachAtEnumeration&&owner.Binding!.Player.GetParent() is { } parent)
                    parent.RemoveChild(owner.Binding.Player);
            }
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
    }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId SupplyCatalogue=new("battery");
    private static void AttachSource(MachineWorld world,Source source,FixturePartId id)
    {
        source.Definition=world.Registry.Definitions[SupplyCatalogue.Value];
        source.Configure(new(){Id=FixtureParts.Id(id),Kind=SupplyCatalogue.Value,Position=[0,6,0]});world.AttachPart(source);
    }
    private static AudioStreamPlayer3D Bind(Source source,ToneBand tone)
    {
        var player=new AudioStreamPlayer3D();source.AddChild(player);
        source.Binding=new(player,new Dictionary<ToneBand,AudioStreamWav>{{tone,AcousticAudio.Create(tone,AcousticVoice.Bell)}});
        return player;
    }
    private static SceneAcousticMotionRun Motion(params MachinePart[] sources)=>new(new SceneAnimationAdapter(1),sources,[],Seed);
    private static void Pulse(Source source,int tick,float strength=1)=>source.Pulses.Add(
        new(Vector3.Zero,Vector3.Up,ToneBand.Mid,tick,AcousticPattern.Omnidirectional,strength));
    [Fact]
    public void StagingIsSilentDiscardRetriesAndEveryTickPublishesWithoutRendering()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=new Source();AttachSource(world,source,FixturePartId.First);var player=Bind(source,ToneBand.Mid);
            var run=new SceneAcousticRun([source],Seed,8);var motion=Motion(source);Pulse(source,0,.5f);
            run.Stage(Stamp(0),motion.Wavefronts);Assert.False(player.Playing);run.Discard();Assert.False(player.Playing);
            run.Stage(Stamp(0),motion.Wavefronts);source.Pulses.Clear();run.Publish(motion);
            Assert.True(player.Playing);Assert.Equal(1,run.PublishedCount);Assert.InRange(player.VolumeDb,-21.021f,-21.020f);
            Assert.Throws<InvalidOperationException>(()=>run.Publish(motion));
            Assert.Throws<ArgumentException>(()=>run.Stage(Stamp(0),motion.Wavefronts));
            for(var tick=1;tick<=3;tick++){Pulse(source,tick);run.Stage(Stamp(tick),motion.Wavefronts);run.Publish(motion);}
            Assert.Equal(4,run.PublishedCount);run.Stage(Stamp(4),motion.Wavefronts);run.Remove();Assert.False(player.Playing);
            Assert.Equal(AcousticPublicationPhase.Removed,run.Phase);
            Assert.Throws<InvalidOperationException>(()=>run.Publish(motion));
            Assert.Throws<InvalidOperationException>(()=>run.Stage(Stamp(4),motion.Wavefronts));
        }
        finally{world.Free();}
    }
    [Fact]
    public void UndeclaredToneRejectsBeforePlaybackAndDuplicatePlayerOwnershipRejects()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=new Source();AttachSource(world,source,FixturePartId.First);var player=Bind(source,ToneBand.Low);
            Assert.Throws<ArgumentException>(()=>new SceneAcousticRun([source,source],Seed,8));
            var run=new SceneAcousticRun([source],Seed,8);var motion=Motion(source);Pulse(source,0);
            Assert.Throws<InvalidOperationException>(()=>run.Stage(Stamp(0),motion.Wavefronts));
            Assert.False(player.Playing);Assert.Equal(0,run.PublishedCount);
            run.Discard();source.Pulses.Clear();run.Stage(Stamp(0),motion.Wavefronts);run.Publish(motion);Assert.Equal(0,run.PublishedCount);
        }
        finally{world.Free();}
    }
    [Fact]
    public void FailedSinkRetainsUnconfirmedTailAndCannotReplayOrDiscardIt()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);var sources=new List<Source>();
        try
        {
            foreach(var id in new[]{FixturePartId.First,FixturePartId.Second,FixturePartId.Third})
            {
                var source=new Source();AttachSource(world,source,id);Bind(source,ToneBand.Mid);Pulse(source,0);sources.Add(source);
            }
            var run=new SceneAcousticRun(sources,Seed,8);var motion=Motion(sources.ToArray());run.Stage(Stamp(0),motion.Wavefronts);
            var detached=sources[1].Binding!.Player;sources[1].RemoveChild(detached);
            var failure=Assert.Throws<InvalidOperationException>(()=>run.Publish(motion));
            Assert.Equal(AcousticPublicationPhase.Faulted,run.Phase);Assert.Same(failure,run.Failure);
            Assert.Equal(1,run.PublishedCount);Assert.Equal(2,run.UnconfirmedCount);
            Assert.True(sources[0].Binding!.Player.Playing);Assert.False(detached.Playing);Assert.False(sources[2].Binding!.Player.Playing);
            Assert.Throws<InvalidOperationException>(run.RequireReady);
            Assert.Throws<InvalidOperationException>(()=>run.Publish(motion));
            Assert.Throws<InvalidOperationException>(run.Discard);
            Assert.Throws<InvalidOperationException>(()=>run.Stage(Stamp(1),motion.Wavefronts));
            Assert.Equal(2,run.UnconfirmedCount);run.Remove();Assert.All(sources,s=>Assert.False(s.Binding!.Player.Playing));
            sources[1].AddChild(detached);
        }
        finally
        {
            foreach(var source in sources)if(source.Binding!.Player.GetParent() is null)source.Binding.Player.Free();
            world.Free();
        }
    }
    [Fact]
    public void CapacityRejectionIsSilentAndCannotPublishATruncatedBatch()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=new Source();AttachSource(world,source,FixturePartId.First);var player=Bind(source,ToneBand.Mid);
            var run=new SceneAcousticRun([source],Seed,2);var motion=Motion(source);
            for(var i=0;i<3;i++)Pulse(source,0);
            Assert.Throws<InvalidOperationException>(()=>run.Stage(Stamp(0),motion.Wavefronts));
            Assert.Equal(2,run.HighWaterMark);Assert.Equal(2,run.Capacity);Assert.False(player.Playing);
            Assert.Throws<InvalidOperationException>(()=>run.Publish(motion));Assert.Throws<InvalidOperationException>(run.RequireReady);
            run.Discard();source.Pulses.RemoveAt(2);run.Stage(Stamp(0),motion.Wavefronts);run.Publish(motion);
            Assert.Equal(2,run.PublishedCount);Assert.True(player.Playing);run.Remove();
        }
        finally{world.Free();}
    }
    [Fact]
    public void WorldCapacityFailureRollsBackPhysicsAndCommittedReadsBeforeRetry()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var source=new Source();AttachSource(world,source,FixturePartId.First);var player=Bind(source,ToneBand.Mid);
            for(var i=0;i<=MachineWorld.MaximumAcousticEventsPerTick;i++)Pulse(source,0);
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();var before=world.StateSignature();
            for(var attempt=0;attempt<2;attempt++)
            {
                Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(0,world.Ticks);Assert.Equal(before,world.StateSignature());
                Assert.False(player.Playing);using var read=world.ReadCommittedPoses();Assert.Equal(0,read.Stamp(PoseSample.Current).Revision.Value);
            }
            source.Pulses.RemoveRange(1,source.Pulses.Count-1);world.Step();Assert.Equal(1,world.Ticks);Assert.True(player.Playing);
            world.Restore();Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }
    [Fact]
    public void PostCommitFaultStopsNextTickBeforeMutationAndResetRebuildsTheRun()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);AudioStreamPlayer3D? player=null;
        try
        {
            // One network enumeration precedes the final staged acoustic enumeration.
            var source=new Source {DetachAtEnumeration=2};
            AttachSource(world,source,FixturePartId.First);player=Bind(source,ToneBand.Mid);Pulse(source,0);
            var construction=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(1,world.Ticks);Assert.False(player.Playing);Assert.Null(player.GetParent());
            var committed=world.Physics.Capture();Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(1,world.Ticks);Assert.Equal(committed.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            source.AddChild(player);world.Restore();player=null;
            Assert.Equal(construction,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();world.Step();Assert.Equal(1,world.Ticks);
        }
        finally{if(player is not null&&GodotObject.IsInstanceValid(player)&&player.GetParent() is null)player.Free();world.Free();}
    }
}
