using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BasketGuideTests(NativeSceneFixture godot)
{
    private enum Role { Basket, Ball }
    private static string Id(Role role)=>role switch
    {
        Role.Basket=>"basket",Role.Ball=>"ball",_=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    [Theory]
    [InlineData(0f,.3f,.9f,0f,-1f,-.9f,true)]
    [InlineData(47f,.3f,.9f,0f,-1f,-.9f,true)]
    [InlineData(0f,.3f,.84f,0f,0f,-.9f,true)]
    [InlineData(0f,0f,.835f,0f,0f,-.9f,false)]
    [InlineData(0f,.3f,.49f,0f,-1f,-1.3f,false)]
    [InlineData(0f,.3f,.9f,1.2f,-1f,-.9f,false)]
    [InlineData(0f,.3f,.9f,0f,1f,-.9f,false)]
    [InlineData(0f,.3f,1.6f,0f,-1f,-.9f,false)]
    public void AuthoredMarginAppliesBoundedForceThroughSharedPhysicsWithoutMovingBasket(
        float degrees,float margin,float height,float depth,float verticalSpeed,float horizontal,bool guided)
    {
        var world=new MachineWorld{Precision=0,Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            const float acceleration=12;
            var basket=world.AddPart(new(){Id=Id(Role.Basket),Kind=Id(Role.Basket),Position=[0,5,0],
                Orientation = PartOrientation.FromEulerDegrees(0,0,degrees),Difficulty=[
                    new(){Precision=0,CaptureMargin=margin,GuideAcceleration=acceleration},
                    new(){Precision=1,CaptureMargin=0,GuideAcceleration=0}]});
            // Collision-free starting positions; old fixture positions inside the
            // rim are no longer valid construction in the shared engine.
            var at=basket.Transform*new Vector3(horizontal,height,depth);
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Id(Role.Ball),Position=[at.X,at.Y,at.Z]});
            var incoming=basket.Basis*new Vector3(0,verticalSpeed,0);
            ball.InitialVelocity=incoming;
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var boxes=basket.Boxes.ToArray();var pose=basket.Transform;
            world.Start();world.Step();
            var target=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var frame=world.PhysicsAssembly.Body(new(basket,MachinePart.RootBody));
            var load=Assert.Single(world.Physics.Loads.Guides.ToArray(),load=>load.Body==target.Id&&load.Frame==frame.Id);
            Assert.Equal(acceleration,load.MaximumAcceleration);
            Assert.Equal(.5,load.MinimumHeight);
            Assert.Equal(.5-Math.Max(0,margin),load.MinimumSupportHeight);
            var change=frame.Pose.Rotation.Inverse().Apply(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity-SceneGeometryAdapter.CaptureVector(incoming));
            Assert.Equal(pose,basket.Transform);Assert.Equal(boxes,basket.Boxes.ToArray());
            Assert.InRange(Mathf.Abs(change.Y),0,.00001f);
            Assert.InRange(change.Length,0,acceleration*MachineWorld.Tick+.00001f);
            if(guided)Assert.True(change.X>9*MachineWorld.Tick);
            else Assert.InRange(change.Length,0,.00001f);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Precision=1;
            ball=world.FindPart(Id(Role.Ball))!;ball.InitialVelocity=incoming;
            world.Start();world.Step();
            Assert.Empty(world.Physics.Loads.Guides.ToArray());
            Assert.InRange((world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity-SceneGeometryAdapter.CaptureVector(incoming)).Length,0,.00001f);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(12f)]
    public void FastPassageThroughTheWholeAuthoredWindowStillUsesSharedGuidanceAndResets(float acceleration)
    {
        var world=new MachineWorld{Precision=0,Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var basket=world.AddPart(new(){Id=Id(Role.Basket),Kind=Id(Role.Basket),Position=[0,5,0],
                Difficulty=[new(){Precision=0,CaptureMargin=.3f,GuideAcceleration=acceleration},
                    new(){Precision=1,CaptureMargin=0,GuideAcceleration=0}]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Id(Role.Ball),Position=[.2f,7,0],
                InitialVelocity=[0,-240,0]});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var pose=basket.Transform;var geometry=basket.Boxes.ToArray();
            world.Start();world.Step();
            var target=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var frame=world.PhysicsAssembly.Body(new(basket,MachinePart.RootBody));
            var local=frame.Pose.InverseTransformPoint(target.Center);
            // The target starts above and finishes below the complete guide region.
            Assert.InRange(local.Y,-1e-6,1e-6);
            var height=1.5-(.5+ball.Radius-Mathf.Clamp(.3f,0,ball.Radius));
            var expected=-acceleration*.2*height/240;
            Assert.InRange(Math.Abs(target.LinearVelocity.X-expected),0,1e-6);
            Assert.Equal(-240,target.LinearVelocity.Y);
            Assert.Equal(pose,basket.Transform);Assert.Equal(geometry,basket.Boxes.ToArray());
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
        }
        finally {world.Free();}
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtraOwnedChildControlsGuideClearanceDespiteStalePresentationAndReset(bool protrudes)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0,Precision=0};godot.Tree.Root.AddChild(world);
        try
        {
            var basket=world.AddPart(new(){Id=Id(Role.Basket),Kind=Id(Role.Basket),Position=[0,5,0],
                Difficulty=[new(){Precision=0,GuideAcceleration=12,CaptureMargin=0}]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Id(Role.Ball),Position=[-.9f,5.9f,-.9f]});
            ball.Spheres.Add(new(new(0,protrudes?-.6f:0,0),.04f,MachinePart.RootBody));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var geometry=world.Physics.Collider(body.Id).Declaration.Geometry;
            Assert.True(geometry.Count>1);
            ball.Spheres.Clear();ball.Position=new(20,20,20);ball.Visible=false;
            basket.Position=new(-20,20,20);basket.Visible=false;
            world.Step();
            Assert.Same(geometry,world.Physics.Collider(body.Id).Declaration.Geometry);
            Assert.Equal(!protrudes,body.LinearVelocity.X>0);
            if(protrudes)Assert.Equal(default,body.LinearVelocity);
            else Assert.InRange(body.LinearVelocity.Length,0,12*MachineWorld.Tick+1e-6);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }
}
