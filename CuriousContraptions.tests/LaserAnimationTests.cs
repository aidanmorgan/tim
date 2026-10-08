using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LaserAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Laser=new("laser"),Battery=new("battery"),Receiver=new("light_receiver");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected laser feedback failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Color Colour(SceneColourFollow declaration)=>((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor;
    private static void Blend(SceneColourFollow declaration,double blend)
    {
        var expected=declaration.From.Lerp(declaration.To,(float)blend);var actual=Colour(declaration);
        Assert.InRange(Math.Abs(expected.R-actual.R),0,2e-6);Assert.InRange(Math.Abs(expected.G-actual.G),0,2e-6);
        Assert.InRange(Math.Abs(expected.B-actual.B),0,2e-6);Assert.Equal(expected.A,actual.A);
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var supplied in new[]{false,true})
        foreach(var triggered in new[]{false,true})
        foreach(var substep in new[]{1,4})yield return [supplied,triggered,substep];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void LaserShowsOnlyCommittedPoweredEmissionAndRestoresExactConstruction(bool supplied,bool triggered,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Laser.Value,Position=[-3,6,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Receiver.Value,Position=[3,6,0]});
            var source=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[0,10,4],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=supplied?1:0}});world.AttachPart(source);
            Assert.True(world.Connect(source,SocketId.Supply,laser,SocketId.PowerIn,ConnectionDomain.Electrical));
            var declaration=Assert.Single(laser.FollowingColours);var saved=Saved(world);var baseline=laser.Transform;
            world.Start();if(triggered)world.Activate(laser);world.Step();world.Step();
            var enabled=supplied&&triggered;
            Assert.Equal(enabled,laser.Active);Assert.Equal(enabled,laser.BeamPath.Count>0);
            Assert.Equal(enabled,receiver.Power>0);Assert.Equal(declaration.From,Colour(declaration));
            world.Running=false;var bodies=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.PresentFrame(.1,1);var amount=enabled?1-Math.Exp(-1.2):0;Blend(declaration,amount);
            laser.Active=!enabled;world.PresentFrame(.1,1);laser.Active=enabled;
            amount=enabled?1-Math.Exp(-2.4):0;Blend(declaration,amount);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(bodies,world.Physics.Capture().BodyStates.ToArray());
            world.Running=true;world.QueueBinaryInput(new(source,BatteryPart.EnableInput),BinaryInputState.Disabled);
            source.FailAt=source.Visits+substep;
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(enabled,laser.Active);
            world.PresentFrame(.1,1);amount=enabled?1-Math.Exp(-3.6):0;Blend(declaration,amount);
            source.FailAt=0;laser.Visible=false;world.Step();world.Step();
            Assert.False(laser.Active);world.PresentFrame(.1,1);Blend(declaration,amount);
            laser.Visible=true;world.PresentFrame(0,1);Blend(declaration,amount*Math.Exp(-1.2));
            world.Running=false;world.PresentFrame(3,1);Blend(declaration,0);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(LaserPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,restored.Transform);
            Assert.Equal(declaration.From,Colour(Assert.Single(restored.FollowingColours)));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.Step();world.PresentFrame(.2,1);
            restored=(LaserPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(declaration.From,Colour(Assert.Single(restored.FollowingColours)));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum BindingFault { Initial, Range, Target, Alpha }
    private partial class InvalidLaser : LaserPart
    {
        public BindingFault Fault;
        public override IReadOnlyList<SceneColourFollow> FollowingColours
        {
            get
            {
                var declaration=base.FollowingColours[0];
                return [Fault switch
                {
                    BindingFault.Initial=>declaration with {Definition=new(0,1,1,12,AnimationClock.Presentation)},
                    BindingFault.Range=>declaration with {Definition=new(0,2,0,12,AnimationClock.Presentation)},
                    BindingFault.Target=>declaration with {Target=null!},
                    BindingFault.Alpha=>declaration with {To=new Color(1,1,1,.5f)},
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                }];
            }
        }
    }
    [Theory]
    [InlineData(BindingFault.Initial)]
    [InlineData(BindingFault.Range)]
    [InlineData(BindingFault.Target)]
    [InlineData(BindingFault.Alpha)]
    public void InvalidDeclarationsRejectBeforeRun(BindingFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var laser=new InvalidLaser {Definition=world.Registry.Definitions[Laser.Value],Fault=fault};
            laser.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Laser.Value});world.AttachPart(laser);
            var saved=Saved(world);Assert.Throws<ArgumentException>(world.Start);
            Assert.False(world.Running);Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void SharedOrReplacedMaterialsRejectAndRetryPreservesBaseline()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var first=(LaserPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Laser.Value});
            var second=(LaserPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Laser.Value,Position=[4,0,0]});
            var a=Assert.Single(first.FollowingColours);var b=Assert.Single(second.FollowingColours);
            var original=b.Target.MaterialOverride;b.Target.MaterialOverride=a.Target.MaterialOverride;
            Assert.Throws<ArgumentException>(world.Start);b.Target.MaterialOverride=original;world.Start();
            b.Target.MaterialOverride=a.Target.MaterialOverride;
            Assert.Throws<InvalidOperationException>(()=>world.PresentFrame(.1,1));
            b.Target.MaterialOverride=original;world.PresentFrame(.1,1);Assert.Equal(b.From,Colour(b));
        }
        finally{world.Free();}
    }
}
