using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticMotionBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private static PoseReadStamp Seed=>new(new(1),new(0),0);
    private static PoseReadStamp Commit=>new(new(1),new(1),MachineWorld.Tick);
    private partial class Source : BatteryPart
    {
        public SceneAcousticBinding? Playback;
        public SceneAcousticMotion[] Motions=[];
        public override SceneAcousticBinding? AcousticPlayback=>Playback;
        public override IReadOnlyList<SceneAcousticMotion> AcousticMotions=>Motions;
    }
    private Source Attach(MachineWorld world)
    {
        var source=new Source {Definition=world.Registry.Definitions[Battery.Value]};
        source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,6,0]});world.AttachPart(source);
        var player=new AudioStreamPlayer3D();source.AddChild(player);
        source.Playback=new(player,new Dictionary<ToneBand,AudioStreamWav>{{ToneBand.Mid,AcousticAudio.Create(ToneBand.Mid,AcousticVoice.Bell)}});
        return source;
    }
    [Fact]
    public void FanoutPreflightRejectsAllTargetsBeforeAnyKickAndCorrectedRetryAccepts()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var first=new Node3D();var second=new Node3D();source.AddChild(first);source.AddChild(second);
            source.Motions=[
                new(first,new(7,48,5,AnimationClock.Presentation),SceneMotionProperty.Rotation,SceneMotionAxis.Z,AnimationDirection.Forward),
                new(second,new(1,1,1e6,AnimationClock.Presentation),SceneMotionProperty.Translation,SceneMotionAxis.X,AnimationDirection.Forward)];
            var adapter=new SceneAnimationAdapter(2);var motion=new SceneAcousticMotionRun(adapter,[source],[],Seed);
            var id=new CommittedEventId(SceneAcousticRun.StreamId,new(1),new(1));
            Assert.Throws<ArgumentException>(()=>motion.Admit(source,Seed,id,.1f));motion.Commit(Commit);
            Assert.Throws<ArgumentOutOfRangeException>(()=>motion.Admit(source,Commit,id,1));
            Assert.Equal(0UL,motion.Read(source,0).Accepted);Assert.Equal(0UL,motion.Read(source,1).Accepted);
            motion.Admit(source,Commit,id,.1f);Assert.Equal(1UL,motion.Read(source,0).Accepted);Assert.Equal(1UL,motion.Read(source,1).Accepted);
            Assert.Throws<ArgumentException>(()=>motion.Admit(source,Commit,id,.1f));
            motion.RefreshVisibility();adapter.Present(1,.1,0);motion.AdvancePresentation(.1);
            Assert.NotEqual(Vector3.Zero,first.Rotation);Assert.NotEqual(Vector3.Zero,second.Position);
            motion.Remove();adapter.Reset();Assert.Equal(Vector3.Zero,first.Rotation);Assert.Equal(Vector3.Zero,second.Position);
            Assert.Throws<InvalidOperationException>(()=>motion.Admit(source,Commit,id,.1f));
        }
        finally{world.Free();}
    }
    public enum InvalidOccurrence { Stream, Generation, Sequence, Stamp }
    [Theory]
    [InlineData(InvalidOccurrence.Stream)]
    [InlineData(InvalidOccurrence.Generation)]
    [InlineData(InvalidOccurrence.Sequence)]
    [InlineData(InvalidOccurrence.Stamp)]
    public void InvalidOccurrenceRejectsWithoutConsumingIdentity(InvalidOccurrence fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var node=new Node3D();source.AddChild(node);
            source.Motions=[new(node,new(7,48,5,AnimationClock.Presentation),SceneMotionProperty.Rotation,SceneMotionAxis.Z,AnimationDirection.Forward)];
            var motion=new SceneAcousticMotionRun(new(1),[source],[],Seed);motion.Commit(Commit);
            var id=new CommittedEventId(fault==InvalidOccurrence.Stream?new(99):SceneAcousticRun.StreamId,
                fault==InvalidOccurrence.Generation?new(2):new(1),fault==InvalidOccurrence.Sequence?default:new(1));
            Assert.Throws<ArgumentException>(()=>motion.Admit(source,fault==InvalidOccurrence.Stamp?Seed:Commit,id,1));
            Assert.Equal(0UL,motion.Read(source,0).Accepted);
            motion.Admit(source,Commit,new(SceneAcousticRun.StreamId,new(1),new(1)),1);Assert.Equal(1UL,motion.Read(source,0).Accepted);
        }
        finally{world.Free();}
    }
    public enum InvalidDeclaration { MissingSource, ForeignTarget, PhysicalTarget, Property, Axis, Direction }
    [Theory]
    [InlineData(InvalidDeclaration.MissingSource)]
    [InlineData(InvalidDeclaration.ForeignTarget)]
    [InlineData(InvalidDeclaration.PhysicalTarget)]
    [InlineData(InvalidDeclaration.Property)]
    [InlineData(InvalidDeclaration.Axis)]
    [InlineData(InvalidDeclaration.Direction)]
    public void InvalidDeclarationsReject(InvalidDeclaration fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var node=new Node3D();source.AddChild(node);
            if(fault==InvalidDeclaration.MissingSource)source.Playback=null;
            source.Motions=[new(fault==InvalidDeclaration.ForeignTarget?world:node,new(7,48,5,AnimationClock.Presentation),
                fault==InvalidDeclaration.Property?(SceneMotionProperty)99:SceneMotionProperty.Rotation,
                fault==InvalidDeclaration.Axis?(SceneMotionAxis)99:SceneMotionAxis.Z,
                fault==InvalidDeclaration.Direction?(AnimationDirection)99:AnimationDirection.Forward)];
            if(fault==InvalidDeclaration.PhysicalTarget)
                Assert.Throws<InvalidOperationException>(()=>new SceneAcousticMotionRun(new(1),[source],[node],Seed));
            else Assert.Throws<ArgumentException>(()=>new SceneAcousticMotionRun(new(1),[source],[],Seed));
        }
        finally{world.Free();}
    }
}
