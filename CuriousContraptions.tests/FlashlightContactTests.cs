using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class FlashlightContactTests(NativeSceneFixture godot)
{
    public enum Strike { Button, Underside, DisabledButton }
    private enum Fixture { Torch, Ball }
    private static string Wire(Fixture value)=>value switch
    {
        Fixture.Torch=>"flashlight",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Theory]
    [InlineData(Strike.Button,0)]
    [InlineData(Strike.Button,37)]
    [InlineData(Strike.Underside,0)]
    [InlineData(Strike.Underside,37)]
    [InlineData(Strike.DisabledButton,0)]
    [InlineData(Strike.DisabledButton,37)]
    public void ImpactLocationComesFromPhysicsNotRenderedTransforms(Strike strike,float angle)
    {
        if(!Enum.IsDefined(strike))throw new ArgumentOutOfRangeException(nameof(strike));
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var torch=(FlashlightPart)world.AddPart(new(){Id=Wire(Fixture.Torch),Kind=Wire(Fixture.Torch),
                Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(0,0,angle)});
            var fromBelow=strike==Strike.Underside;
            var local=new Vector3(-.15f,fromBelow?-1.5f:1.5f,0);
            var start=torch.Transform*local;
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),
                Position=[start.X,start.Y,start.Z]});
            ball.InitialVelocity=torch.Transform.Basis*(fromBelow?Vector3.Up:Vector3.Down)*4;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            torch.Position+=new Vector3(9,2,3);torch.RotationDegrees=new(70,15,120);
            torch.Visible=false;ball.Visible=false;
            if(strike==Strike.DisabledButton)
            {
                var body=world.PhysicsAssembly.Body(new(torch,MachinePart.RootBody));
                var collider=world.Physics.Collider(body.Id).Declaration;
                world.Physics.ApplyColliderUpdates([new(body.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            }
            for(var i=0;i<60;i++)world.Step();
            Assert.Equal(strike==Strike.Button,torch.Active);
            Assert.Equal(strike==Strike.Button,torch.LightSource.HasValue);
            Assert.Equal(strike==Strike.Button,world.Events.ContainsKey(new(MachineEventKind.Activated,torch.Uid)));
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Empty(world.Events);
            Assert.False(((FlashlightPart)world.FindPart(Wire(Fixture.Torch))!).Active);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedValues()
    {
        Assert.Equal("flashlight",Wire(Fixture.Torch));Assert.Equal("ball",Wire(Fixture.Ball));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
    }
}
