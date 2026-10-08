using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class FlashlightPresentationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Torch=new("flashlight"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public FlashlightPart Torch=null!;
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            if(++Visits!=FailAt)return;
            Torch.Active=!Torch.Active;
            throw new InvalidOperationException("Injected torch publication failure.");
        }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Color Lens(FlashlightPart torch)=>((StandardMaterial3D)Assert.Single(torch.ColourAnimations).Target.MaterialOverride).AlbedoColor;
    private static void Lamp(FlashlightPart torch,double amount)
    {
        var declaration=Assert.Single(torch.ColourAnimations);var expected=declaration.From.Lerp(declaration.To,(float)amount);
        var actual=Lens(torch);
        Assert.InRange(Math.Abs(expected.R-actual.R),0,2e-6);Assert.InRange(Math.Abs(expected.G-actual.G),0,2e-6);
        Assert.InRange(Math.Abs(expected.B-actual.B),0,2e-6);Assert.Equal(expected.A,actual.A);
    }
    [Theory]
    [InlineData(false,1)]
    [InlineData(false,4)]
    [InlineData(true,1)]
    [InlineData(true,4)]
    public void ArtworkUsesCommittedActivationAcrossFailurePauseHiddenAndExactReset(bool activated,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var torch=(FlashlightPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Torch.Value,Position=[0,6,0]});
            var probe=new Probe {Torch=torch,Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,10,0]});world.AttachPart(probe);
            var button=Assert.Single(torch.TranslationAnimations).Target;var baseline=button.Transform;
            var cone=Assert.Single(torch.LightCones).Target;var saved=Saved(world);
            world.Start();if(activated)world.Activate(torch);
            world.PresentFrame(.01,1);
            Assert.False(cone.Visible);Assert.Equal(baseline,button.Transform);Lamp(torch,0);
            probe.FailAt=probe.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(activated,torch.Active);world.PresentFrame(.01,1);
            Assert.False(cone.Visible);Assert.Equal(baseline,button.Transform);Lamp(torch,0);
            probe.FailAt=0;world.Step();
            Assert.False(cone.Visible);Assert.Equal(baseline,button.Transform);Lamp(torch,0);
            world.Running=false;var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.PresentFrame(.03,1);Assert.Equal(activated,cone.Visible);Lamp(torch,activated?.5:0);
            Assert.InRange(Math.Abs(button.Position.Y-(activated?.33:.36)),0,1e-6);
            if(activated)Assert.Equal(LightConeVisual.Sectors*LightConeVisual.ShellCount*9,
                cone.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length);
            torch.Active=!activated;torch.Visible=false;world.PresentFrame(.1,1);
            Lamp(torch,activated?.5:0);Assert.Equal(activated,cone.Visible);
            torch.Visible=true;world.PresentFrame(0,1);
            Lamp(torch,activated?1:0);Assert.Equal(activated,cone.Visible);
            Assert.InRange(Math.Abs(button.Position.Y-(activated?.3:.36)),0,1e-6);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            torch.Active=activated;world.Running=true;probe.FailAt=probe.Visits+substep;
            Assert.Throws<InvalidOperationException>(world.Step);world.PresentFrame(0,1);
            Assert.Equal(activated,cone.Visible);Lamp(torch,activated?1:0);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(FlashlightPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.TranslationAnimations).Target.Transform);
            Assert.False(Assert.Single(restored.LightCones).Target.Visible);Lamp(restored,0);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.PresentFrame(.1,1);
            restored=(FlashlightPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Lamp(restored,0);Assert.False(Assert.Single(restored.LightCones).Target.Visible);
        }
        finally{world.Free();}
    }
}
