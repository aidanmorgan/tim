using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticMotionTests(NativeSceneFixture godot)
{
    public enum Emitter { Bell, Speaker }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Bell=new("bell"),Speaker=new("speaker"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Before;
        public int Visits,FailAt;
        public override void BeforeNetworks(MachineWorld world)=>Before?.Invoke();
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected acoustic motion tick failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static void Pose(MachinePart part,Transform3D baseline,double strength,double time)
    {
        var declaration=Assert.Single(part.AcousticMotions);var d=declaration.Definition;
        var amount=d.VelocityPerStrength/d.Frequency*Math.Exp(-d.Decay*time)*Math.Sin(d.Frequency*time)*strength;
        if(declaration.Direction==AnimationDirection.Reverse)amount=-amount;
        if(declaration.Property==SceneMotionProperty.Translation)
            Assert.InRange(declaration.Target.Position.DistanceTo(baseline.Origin+Vector3.Right*(float)amount),0,2e-6);
        else
        {
            var expected=baseline.Basis*new Basis(Vector3.Back,(float)amount);
            Assert.InRange(expected.X.DistanceTo(declaration.Target.Basis.X),0,2e-6);
            Assert.InRange(expected.Y.DistanceTo(declaration.Target.Basis.Y),0,2e-6);
        }
    }
    [Theory]
    [InlineData(Emitter.Bell,false,1)]
    [InlineData(Emitter.Bell,false,4)]
    [InlineData(Emitter.Bell,true,1)]
    [InlineData(Emitter.Bell,true,4)]
    [InlineData(Emitter.Speaker,false,1)]
    [InlineData(Emitter.Speaker,false,4)]
    [InlineData(Emitter.Speaker,true,1)]
    [InlineData(Emitter.Speaker,true,4)]
    public void CommittedKicksIgnoreFailureAndLiveActivityAndRestoreExactConstruction(Emitter emitter,bool enabled,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var catalogue=emitter==Emitter.Bell?Bell:Speaker;
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=catalogue.Value,Position=[0,6,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-5,10,0],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=enabled?1:0}});world.AttachPart(probe);
            if(emitter==Emitter.Speaker)Assert.True(world.Connect(probe,SocketId.Supply,part,SocketId.PowerIn,ConnectionDomain.Electrical));
            var target=Assert.Single(part.AcousticMotions).Target;var baseline=target.Transform;var saved=Saved(world);
            world.Start();
            if(emitter==Emitter.Speaker){world.Activate(part);world.Step();}
            else probe.Before=()=>part.ObserveContact(new(new(part,MachinePart.RootBody),new(probe,MachinePart.RootBody),
                default,default,enabled?2:.2,1),world);
            probe.FailAt=probe.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0UL,world.ReadAcousticMotion(part,0).Accepted);Assert.Equal(baseline,target.Transform);
            Assert.False(part.AcousticPlayback!.Player.Playing);
            probe.FailAt=0;world.Step();probe.Before=null;
            Assert.Equal(enabled?1UL:0UL,world.ReadAcousticMotion(part,0).Accepted);Assert.Equal(baseline,target.Transform);
            var strength=enabled?Assert.Single(part.AcousticPulses).Strength:0;
            world.Running=false;var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.PresentFrame(.03,1);Pose(part,baseline,strength,.03);
            var shown=target.Transform;part.Active=!enabled;part.Visible=false;world.PresentFrame(.1,1);Assert.Equal(shown,target.Transform);
            part.Visible=true;world.PresentFrame(0,1);Pose(part,baseline,strength,.13);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            part.Active=enabled;world.Restore();Assert.Equal(saved,Saved(world));
            var restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.AcousticMotions).Target.Transform);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.PresentFrame(.1,1);
            restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0UL,world.ReadAcousticMotion(restored,0).Accepted);
            Assert.Equal(baseline,Assert.Single(restored.AcousticMotions).Target.Transform);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(Emitter.Bell)]
    [InlineData(Emitter.Speaker)]
    public void MultipleCommittedOccurrencesBeforeRenderingAllContribute(Emitter emitter)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=(emitter==Emitter.Bell?Bell:Speaker).Value,Position=[0,6,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-5,10,0]});world.AttachPart(probe);
            if(emitter==Emitter.Speaker)Assert.True(world.Connect(probe,SocketId.Supply,part,SocketId.PowerIn,ConnectionDomain.Electrical));
            var baseline=Assert.Single(part.AcousticMotions).Target.Transform;world.Start();
            for(var emission=0;emission<2;emission++)
            {
                if(emitter==Emitter.Speaker){world.Activate(part);world.Step();world.Step();}
                else
                {
                    probe.Before=()=>part.ObserveContact(new(new(part,MachinePart.RootBody),new(probe,MachinePart.RootBody),default,default,2,1),world);
                    world.Step();probe.Before=null;
                }
                if(emission==0)for(var i=0;i<24;i++)world.Step();
            }
            Assert.Equal(2UL,world.ReadAcousticMotion(part,0).Accepted);
            var strength=part.AcousticPulses.Sum(p=>p.Strength);world.PresentFrame(.03,1);Pose(part,baseline,strength,.03);
        }
        finally{world.Free();}
    }
}
