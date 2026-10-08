using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class FunnelTests(NativeSceneFixture godot)
{
    private enum Role { Funnel, Ball, Pipe, Torch }
    private static string Id(Role role) => role switch
    {
        Role.Funnel => "funnel", Role.Ball => "ball", Role.Pipe => "pipe", Role.Torch => "torch",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role) => role switch
    {
        Role.Funnel => "funnel", Role.Ball => "ball", Role.Pipe => "pipe", Role.Torch => "flashlight",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.75f)]
    [InlineData(-.75f)]
    public void WideInletGuidesOffsetDropThroughNarrowOutlet(float offset)
    {
        var world=World();
        try
        {
            var funnel=(FunnelPart)world.AddPart(new(){Id=Id(Role.Funnel),Kind=Kind(Role.Funnel),Position=[0,5,0],Orientation = PartOrientation.FromEulerDegrees(0,0,-90)});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[offset,8,0]});
            world.Start();
            var crossed=false;
            var previous=ball.Position;
            for(var i=0;i<360;i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous)<.34f,"No teleport through the funnel.");
                previous=ball.Position;
                var local=funnel.Transform.AffineInverse()*ball.Position;
                if(local.X>1.34f)
                {
                    Assert.InRange(new Vector2(local.Y,local.Z).Length(),0,.36f);
                    crossed=true;
                    break;
                }
            }
            Assert.True(crossed,"Ball never exited the narrow mouth.");
            world.Restore();
            Assert.Equal(new Vector3(offset,8,0),world.FindPart(Id(Role.Ball))!.Position);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(4f,0,0,0)]
    [InlineData(40f,20,30,40)]
    public void CentredTravelIsOpenAndDoesNotAddEnergy(float speed,float x,float y,float z)
    {
        var world=World();
        world.Gravity=world.Pressure=0;
        try
        {
            var funnel=world.AddPart(new(){Id=Id(Role.Funnel),Kind=Kind(Role.Funnel),Position=[0,8,0],Orientation = PartOrientation.FromEulerDegrees(x,y,z)});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,12,0]});
            ball.Position=funnel.Transform*new Vector3(-2,0,0);
            ball.InitialVelocity=funnel.Basis.X*speed;
            world.Start();
            for(var i=0;i<(int)Math.Ceiling(4/(speed*MachineWorld.Tick));i++)world.Step();
            Assert.InRange((funnel.Transform.AffineInverse()*ball.Position).X,1.99f,2.34f);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,speed-.003f,speed+.003f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void FunnelOutletFeedsJoinedStraightTubeContinuously()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id=Id(Role.Funnel),Kind=Kind(Role.Funnel),Position=[0,7,0],Orientation = PartOrientation.FromEulerDegrees(0,0,-90)});
            var pipe=world.AddPart(new(){Id=Id(Role.Pipe),Kind=Kind(Role.Pipe),Position=[0,4.12f,0],Orientation = PartOrientation.FromEulerDegrees(0,0,-90)});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[.75f,10,0]});
            world.Start();
            var exited=false;
            var previous=ball.Position;
            for(var i=0;i<360;i++)
            {
                world.Step();
                Assert.True(ball.Position.DistanceTo(previous)<.34f);
                previous=ball.Position;
                if(ball.Position.Y<1.8f)
                {
                    Assert.InRange(Mathf.Abs(ball.Position.X),0,.4f);
                    exited=true;break;
                }
            }
            Assert.True(exited);
        }
        finally {world.Free();}
    }

    [Fact]
    public void NarrowOutletMatchesTubeButWideInletDoesNot()
    {
        var world=World();
        try
        {
            var funnel=(FunnelPart)world.AddPart(new(){Id=Id(Role.Funnel),Kind=Kind(Role.Funnel),Position=[0,5,0]});
            var pipe=world.AddPart(new(){Id=Id(Role.Pipe),Kind=Kind(Role.Pipe),Position=[2.95f,5,0]});
            Assert.NotNull(TubePlacementSnap.Find(world,pipe));
            pipe.Position=new(-2.95f,5,0);
            Assert.Null(TubePlacementSnap.Find(world,pipe));
            Assert.Equal(FunnelPart.InletRadius,funnel.Mouths.Single(m=>m.Id==TubeMouthId.Start).BoreRadius);
        }
        finally {world.Free();}
    }

    [Fact]
    public void OutsideWallRejectsEntryAndGlassTransmitsLight()
    {
        var world=World();world.Gravity=world.Pressure=0;
        try
        {
            var funnel=world.AddPart(new(){Id=Id(Role.Funnel),Kind=Kind(Role.Funnel),Position=[0,6,0]});
            var emitter=world.AddPart(new(){Id=Id(Role.Torch),Kind=Kind(Role.Torch),Position=[-5,8,0]});
            Assert.Equal(6,WorldGeometry.Trace(TraceMedium.Light,world,new(0,6,-3),Vector3.Back,6,emitter));
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,8,0]});
            ball.InitialVelocity=Vector3.Down*4;
            world.Start();
            for(var i=0;i<30;i++)world.Step();
            var ballBody=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var funnelBody=world.PhysicsAssembly.Body(new(funnel,MachinePart.RootBody));
            var ballGeometry=world.Physics.Collider(ballBody.Id).Declaration.Geometry;
            var funnelGeometry=world.Physics.Collider(funnelBody.Id).Declaration.Geometry;
            Assert.Null(CompoundCollision.FindOverlap(
                new(ballGeometry,ballBody.CreateTrajectory(0,default)),
                new(funnelGeometry,funnelBody.CreateTrajectory(0,default)),.001,out _));
            Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length<=4.001f);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedRoles()
    {
        Assert.Equal("funnel",Kind(Role.Funnel));
        Assert.Equal("ball",Kind(Role.Ball));
        Assert.Equal("pipe",Kind(Role.Pipe));
        Assert.Equal("flashlight",Kind(Role.Torch));
        Assert.Equal("funnel",Id(Role.Funnel));
        Assert.Equal("ball",Id(Role.Ball));
        Assert.Equal("pipe",Id(Role.Pipe));
        Assert.Equal("torch",Id(Role.Torch));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
    }
}
