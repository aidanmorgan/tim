using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OccurrenceBindingTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private static PoseReadStamp Seed=>new(new(1),new(0),0);
    private static PoseReadStamp Stamp(int tick)=>new(new(1),new(tick),tick*(double)MachineWorld.Tick);
    private static readonly SceneOccurrenceSlot Slot=new();
    private partial class Source:BatteryPart
    {
        public SceneOccurrenceAnimation[] Animations=[];
        public SceneOccurrenceSlot[] Sources=[Slot];
        public override IReadOnlyList<SceneOccurrenceSlot> OccurrenceSources=>Sources;
        public override IReadOnlyList<SceneOccurrenceAnimation> OccurrenceAnimations=>Animations;
    }
    private static AnimationImpulseDefinition Definition(int capacity,AnimationClock clock=AnimationClock.Presentation)=>
        new(.32,AnimationImpulseCurve.SineSquaredPulse,AnimationImpulseOverlap.SaturatingSum,AnimationImpulseVisibility.DeferUntilVisible,
            clock,capacity,AnimationImpulseTiming.EventTimeRetainFirstFrame,.5);
    private Source Attach(MachineWorld world)
    {
        var source=new Source{Definition=world.Registry.Definitions[Battery.Value]};
        source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,6,0]});world.AttachPart(source);
        var first=PartArt.Ring(source,1,.01f,new("#fff0c2"));var second=PartArt.Ring(source,1,.01f,new("#fff0c2"));
        var key=new SceneOccurrenceKey(source,Slot);
        source.Animations=[SceneOccurrenceAnimation.Scale(first,key,Definition(2),1.24),
            SceneOccurrenceAnimation.Colour(second,key,Definition(1),new("#fff0c2"),new("#f7cb52"))];
        return source;
    }
    [Fact]
    public void FanoutReservesEverySubscriberAndRejectsBatchWithoutAnyAdmission()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var key=new SceneOccurrenceKey(source,Slot);var adapter=new SceneAnimationAdapter(2);
            var run=new SceneOccurrenceRun(adapter,[source],[],Seed);
            run.Begin(Stamp(1));run.Emit(key,1);Assert.Throws<InvalidOperationException>(()=>run.Emit(key,1));
            Assert.Equal(SceneOccurrencePhase.Rejected,run.Phase);Assert.Throws<InvalidOperationException>(run.Seal);
            for(var i=0;i<2;i++)Assert.Equal(0UL,run.Read(key,i).Occurrences.Accepted);
            run.Discard();run.Begin(Stamp(1));run.Emit(key,1);run.Seal();run.Commit();
            Assert.Equal(1,run.UnconfirmedCount);Assert.Equal(0UL,run.Read(key,0).Occurrences.Accepted);
            run.Publish();Assert.Equal(0,run.UnconfirmedCount);Assert.Equal(1UL,run.PublishedCount);
            for(var i=0;i<2;i++)Assert.Equal(1UL,run.Read(key,i).Occurrences.Accepted);
            run.RefreshVisibility();adapter.Present(1,.16,0);run.AdvancePresentation(.16);
            Assert.Equal(1.24,run.Read(key,0).Animation.Value);Assert.Equal(1,run.Read(key,1).Animation.Value);
            run.Begin(Stamp(2));Assert.Throws<InvalidOperationException>(()=>run.Emit(key,1));run.Discard();
            adapter.Present(2,.32,0);run.AdvancePresentation(.32);
            run.Begin(Stamp(2));run.Emit(key,1);run.Seal();run.Commit();run.Publish();
            for(var i=0;i<2;i++)Assert.Equal(2UL,run.Read(key,i).Occurrences.Accepted);
            run.Remove();adapter.Reset();Assert.Throws<InvalidOperationException>(run.RequireReady);
        }
        finally{world.Free();}
    }
    [Fact]
    public void PostCommitFailureRetainsUnconfirmedOccurrenceAndCannotPartiallyAdmitFanout()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);MeshInstance3D? detached=null;
        try
        {
            var source=Attach(world);var key=new SceneOccurrenceKey(source,Slot);var adapter=new SceneAnimationAdapter(2);
            var run=new SceneOccurrenceRun(adapter,[source],[],Seed);run.Begin(Stamp(1));run.Emit(key,1);run.Seal();run.Commit();
            detached=source.Animations[1].Target;source.RemoveChild(detached);
            Assert.Throws<InvalidOperationException>(run.Publish);Assert.Equal(SceneOccurrencePhase.Faulted,run.Phase);
            Assert.Equal(1,run.UnconfirmedCount);Assert.Equal(0UL,run.PublishedCount);
            for(var i=0;i<2;i++)Assert.Equal(0UL,run.Read(key,i).Occurrences.Accepted);
            Assert.Throws<InvalidOperationException>(run.Publish);Assert.Throws<InvalidOperationException>(run.RequireReady);
            Assert.Throws<InvalidOperationException>(run.Discard);source.AddChild(detached);detached=null;run.Remove();adapter.Reset();
        }
        finally{detached?.Free();world.Free();}
    }
    public static IEnumerable<object[]> Channels()
    {foreach(var channel in Enum.GetValues<SceneOccurrenceChannel>())foreach(var clock in Enum.GetValues<AnimationClock>())yield return [channel,clock];}
    [Theory]
    [MemberData(nameof(Channels))]
    public void ChannelsUseDeclaredClocksAndOnlyFramesWriteArtwork(SceneOccurrenceChannel channel,AnimationClock clock)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var key=new SceneOccurrenceKey(source,Slot);var target=source.Animations[0].Target;
            source.Animations=[channel switch
            {
                SceneOccurrenceChannel.Scale=>SceneOccurrenceAnimation.Scale(target,key,Definition(4,clock),1.24),
                SceneOccurrenceChannel.Translation=>SceneOccurrenceAnimation.Translation(target,key,Definition(4,clock),-.16,AnimationTranslationAxis.X),
                SceneOccurrenceChannel.Colour=>SceneOccurrenceAnimation.Colour(target,key,Definition(4,clock),new("#fff0c2"),new("#f7cb52")),
                _=>throw new ArgumentOutOfRangeException(nameof(channel))
            }];
            var baseline=target.Transform;var material=(StandardMaterial3D)target.MaterialOverride;var colour=material.AlbedoColor;
            var adapter=new SceneAnimationAdapter(1);var run=new SceneOccurrenceRun(adapter,[source],[],Seed);
            run.Begin(Stamp(1));run.Emit(key,1);run.Seal();run.Commit();run.Publish();
            Assert.Equal(baseline,target.Transform);Assert.Equal(colour,material.AlbedoColor);
            run.RefreshVisibility();adapter.Present(1,.16,MachineWorld.Tick);run.AdvancePresentation(.16);
            var initial=channel==SceneOccurrenceChannel.Scale?1:0;
            var peak=channel switch{SceneOccurrenceChannel.Scale=>1.24,SceneOccurrenceChannel.Translation=>-.16,SceneOccurrenceChannel.Colour=>1,_=>throw new ArgumentOutOfRangeException()};
            Assert.Equal(clock==AnimationClock.Presentation?peak:initial,run.Read(key,0).Animation.Value);
            adapter.Present(2,.16,MachineWorld.Tick+.16);Assert.InRange(Math.Abs(run.Read(key,0).Animation.Value-peak),0,1e-12);
            run.Remove();adapter.Reset();Assert.Equal(baseline,target.Transform);Assert.Equal(colour,material.AlbedoColor);
        }
        finally{world.Free();}
    }
    public enum InvalidDeclaration { SourceOwner, SourceSlot, DuplicateSource, ForeignTarget, PhysicalTarget, SharedMaterial, Axis }
    [Theory]
    [InlineData(InvalidDeclaration.SourceOwner)]
    [InlineData(InvalidDeclaration.SourceSlot)]
    [InlineData(InvalidDeclaration.DuplicateSource)]
    [InlineData(InvalidDeclaration.ForeignTarget)]
    [InlineData(InvalidDeclaration.PhysicalTarget)]
    [InlineData(InvalidDeclaration.SharedMaterial)]
    [InlineData(InvalidDeclaration.Axis)]
    public void InvalidBindingRejects(InvalidDeclaration fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var first=source.Animations[0].Target;var second=source.Animations[1].Target;
            var key=new SceneOccurrenceKey(source,fault==InvalidDeclaration.SourceSlot?new():Slot);
            if(fault==InvalidDeclaration.SourceOwner)key=key with{Owner=null!};
            if(fault is InvalidDeclaration.SourceOwner or InvalidDeclaration.SourceSlot)
                source.Animations=[SceneOccurrenceAnimation.Scale(first,key,Definition(2),1.24)];
            if(fault==InvalidDeclaration.DuplicateSource)source.Sources=[Slot,Slot];
            if(fault==InvalidDeclaration.ForeignTarget){source.RemoveChild(first);world.AddChild(first);}
            if(fault==InvalidDeclaration.SharedMaterial)second.MaterialOverride=first.MaterialOverride;
            if(fault==InvalidDeclaration.Axis)source.Animations=[SceneOccurrenceAnimation.Translation(first,key,Definition(2),1,(AnimationTranslationAxis)99)];
            var create=()=>new SceneOccurrenceRun(new(4),[source],fault==InvalidDeclaration.PhysicalTarget?[first]:[],Seed);
            if(fault==InvalidDeclaration.PhysicalTarget)Assert.Throws<InvalidOperationException>(()=>create());
            else if(fault==InvalidDeclaration.Axis)Assert.Throws<ArgumentOutOfRangeException>(()=>create());
            else Assert.Throws<ArgumentException>(()=>create());
        }
        finally{world.Free();}
    }
    [Fact]
    public void UnsupportedEmissionAndLifecycleRejectWithoutPublication()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var source=Attach(world);var key=new SceneOccurrenceKey(source,Slot);var run=new SceneOccurrenceRun(new(2),[source],[],Seed);
            Assert.Throws<InvalidOperationException>(()=>run.Emit(key,1));Assert.Throws<InvalidOperationException>(run.Commit);
            Assert.Throws<ArgumentException>(()=>run.Begin(Stamp(2)));
            run.Begin(Stamp(1));Assert.Throws<ArgumentException>(()=>run.Emit(new(source,new()),1));
            foreach(var strength in new[]{double.NaN,double.PositiveInfinity,0,-1,1.01})
                Assert.Throws<ArgumentOutOfRangeException>(()=>run.Emit(key,strength));
            Assert.Equal(0,run.HighWaterMark);Assert.Throws<InvalidOperationException>(run.RefreshVisibility);
            run.Discard();run.Begin(Stamp(1));run.Emit(key,1);run.Discard();Assert.Equal(0UL,run.PublishedCount);
            run.Remove();Assert.Throws<InvalidOperationException>(()=>run.Read(key,0));
        }
        finally{world.Free();}
    }
}
