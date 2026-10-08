using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BumperOccurrenceTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Bumper=new("bumper"),Battery=new("battery");
    private partial class Probe:BatteryPart
    {
        public Action? Before,Observed;
        public int Visits,FailAt;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)
        {Observed?.Invoke();if(++Visits==FailAt)throw new InvalidOperationException("Injected occurrence tick failure.");}
    }
    private Probe AddProbe(MachineWorld world)
    {
        var p=new Probe{Definition=world.Registry.Definitions[Battery.Value]};
        p.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-5,6,0]});world.AttachPart(p);return p;
    }
    private static void Strike(MachineWorld world,BumperPart bumper,Probe probe,bool enabled)=>
        bumper.ObserveContact(new(new(bumper,MachinePart.RootBody),new(probe,MachinePart.RootBody),default,default,enabled?2:.01,1),world);
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    public static IEnumerable<object[]> Failures()
    {foreach(var enabled in new[]{false,true})for(var substep=1;substep<=MachineWorld.Substeps;substep++)yield return [enabled,substep];}
    [Theory]
    [MemberData(nameof(Failures))]
    public void ImpactFanoutCommitsTogetherAndRestoresExactConstruction(bool enabled,int substep)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var bumper=(BumperPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Bumper.Value,Position=[0,6,0]});
            var probe=AddProbe(world);var key=new SceneOccurrenceKey(bumper,BumperPart.ImpactOccurrence);
            var ring=bumper.OccurrenceAnimations[0].Target;var material=(StandardMaterial3D)ring.MaterialOverride;
            var baseline=ring.Transform;var colour=material.AlbedoColor;var saved=Saved(world);
            world.Start();probe.Before=()=>Strike(world,bumper,probe,enabled);probe.FailAt=substep;
            var before=world.Physics.Capture().BodyStates.ToArray();Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,world.Ticks);Assert.Equal(0,bumper.HitCount);Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
            for(var i=0;i<2;i++)Assert.Equal(0UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Accepted);
            Assert.Equal(baseline,ring.Transform);Assert.Equal(colour,material.AlbedoColor);
            probe.FailAt=0;world.Step();probe.Before=null;Assert.Equal(enabled?1:0,bumper.HitCount);
            for(var i=0;i<2;i++)Assert.Equal(enabled?1UL:0UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Accepted);
            Assert.Equal(baseline,ring.Transform);Assert.Equal(colour,material.AlbedoColor);
            world.Running=false;before=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.PresentFrame(.08,1);
            Assert.InRange(Math.Abs(ring.Scale.X-(enabled?1.12:1)),0,1e-6);
            var expected=enabled?colour.Lerp(new Color("#f7cb52"),.5f):colour;
            Assert.InRange(Math.Abs(material.AlbedoColor.R-expected.R),0,1e-6);
            Assert.InRange(Math.Abs(material.AlbedoColor.G-expected.G),0,1e-6);
            var shown=ring.Transform;bumper.Active=!enabled;bumper.Visible=false;world.PresentFrame(.4,1);Assert.Equal(shown,ring.Transform);
            bumper.Visible=true;world.PresentFrame(0,1);Assert.Equal(baseline,ring.Transform);Assert.Equal(colour,material.AlbedoColor);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
            world.Restore();Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.PresentFrame(.1,1);
            var restored=(BumperPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,restored.HitCount);Assert.Equal(baseline,restored.OccurrenceAnimations[0].Target.Transform);
            Assert.Equal(0UL,world.ReadOccurrenceFeedback(new(restored,BumperPart.ImpactOccurrence),0).Occurrences.Accepted);
            Assert.Throws<ArgumentException>(()=>world.ReadOccurrenceFeedback(key,0));
        }
        finally{world.Free();}
    }
    [Fact]
    public void LeavingSceneInvalidatesPendingOccurrenceGeneration()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var bumper=(BumperPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Bumper.Value,Position=[0,6,0]});
            var probe=AddProbe(world);probe.Before=()=>Strike(world,bumper,probe,true);
            world.Start();world.Step();var key=new SceneOccurrenceKey(bumper,BumperPart.ImpactOccurrence);
            Assert.Equal(1UL,world.ReadOccurrenceFeedback(key,0).Occurrences.Accepted);
            godot.Tree.Root.RemoveChild(world);
            Assert.Throws<InvalidOperationException>(()=>world.ReadOccurrenceFeedback(key,0));
        }
        finally{world.Free();}
    }
    [Fact]
    public void SaturationRejectsWholeTickAndDrainingAllowsCorrectedRetry()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var bumper=(BumperPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Bumper.Value,Position=[0,6,0]});
            var probe=AddProbe(world);var key=new SceneOccurrenceKey(bumper,BumperPart.ImpactOccurrence);var saved=Saved(world);
            var count=65;probe.Before=()=>{for(var i=0;i<count;i++)Strike(world,bumper,probe,true);};world.Start();
            var before=world.Physics.Capture().BodyStates.ToArray();
            for(var attempt=0;attempt<2;attempt++)
            {
                Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(0,world.Ticks);Assert.Equal(0,bumper.HitCount);
                Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
                for(var i=0;i<2;i++)Assert.Equal(0UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Accepted);
            }
            count=64;world.Step();Assert.Equal(64,bumper.HitCount);
            for(var i=0;i<2;i++)Assert.Equal(64UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Accepted);
            count=1;Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(1,world.Ticks);Assert.Equal(64,bumper.HitCount);
            world.PresentFrame(.16,1);Assert.Equal(1.24,world.ReadOccurrenceFeedback(key,0).Animation.Value);
            world.PresentFrame(.16,1);
            for(var i=0;i<2;i++)Assert.Equal(64UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Completed);
            world.Step();Assert.Equal(2,world.Ticks);Assert.Equal(65,bumper.HitCount);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void PostCommitBindingFaultCannotReplayOrAdvancePhysics()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);MeshInstance3D? ring=null;Node? parent=null;
        try
        {
            var bumper=(BumperPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Bumper.Value,Position=[0,6,0]});
            var probe=AddProbe(world);var key=new SceneOccurrenceKey(bumper,BumperPart.ImpactOccurrence);
            var target=bumper.OccurrenceAnimations[0].Target;var owner=target.GetParent();ring=target;parent=owner;var detached=false;
            probe.Before=()=>Strike(world,bumper,probe,true);
            probe.Observed=()=>{if(!detached){owner.RemoveChild(target);detached=true;}};
            world.Start();Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(1,world.Ticks);Assert.Equal(1,bumper.HitCount);
            for(var i=0;i<2;i++)Assert.Equal(0UL,world.ReadOccurrenceFeedback(key,i).Occurrences.Accepted);
            var committed=world.Physics.Capture().BodyStates.ToArray();Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(1,world.Ticks);Assert.Equal(committed,world.Physics.Capture().BodyStates.ToArray());
            parent.AddChild(ring);ring=null;world.Restore();world.Start();world.Step();Assert.Equal(1,world.Ticks);
        }
        finally{if(ring is not null&&GodotObject.IsInstanceValid(ring)&&ring.GetParent() is null)ring.Free();world.Free();}
    }
}
