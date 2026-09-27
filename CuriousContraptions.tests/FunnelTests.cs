using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class FunnelTests(HeadlessFixture godot)
{
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
            var funnel=(FunnelPart)world.AddPart(new(){Id="funnel",Kind="funnel",Position=[0,5,0],Rotation=[0,0,-90]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[offset,8,0]});
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
            Assert.Equal(new Vector3(offset,8,0),world.FindPart("ball")!.Position);
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
            var funnel=world.AddPart(new(){Id="funnel",Kind="funnel",Position=[0,8,0],Rotation=[x,y,z]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,12,0]});
            ball.Position=funnel.Transform*new Vector3(-2,0,0);
            world.Start();ball.Velocity=funnel.Basis.X*speed;
            for(var i=0;i<(int)Math.Ceiling(4/(speed*MachineWorld.Tick));i++)world.Step();
            Assert.InRange((funnel.Transform.AffineInverse()*ball.Position).X,1.99f,2.34f);
            Assert.InRange(ball.Velocity.Length(),speed-.003f,speed+.003f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void FunnelOutletFeedsJoinedStraightTubeContinuously()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="funnel",Kind="funnel",Position=[0,7,0],Rotation=[0,0,-90]});
            var pipe=world.AddPart(new(){Id="pipe",Kind="pipe",Position=[0,4.12f,0],Rotation=[0,0,-90]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[.75f,10,0]});
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
    public void AnnularProfileHasCorrectSignedNormalsAndOpenEnds()
    {
        var shape=new FrustumProxy(Transform3D.Identity,.9f,1.3f,.65f,.05f);
        var inside=shape.Surface(new(0,.99f,0));
        Assert.True(inside.Distance<0);
        Assert.True(inside.Normal.X<0&&inside.Normal.Y<0);
        var outside=shape.Surface(new(0,1.2f,0));
        Assert.True(outside.Distance>0);
        Assert.True(outside.Normal.X>0&&outside.Normal.Y>0);
        Assert.InRange(shape.Surface(Vector3.Zero).Distance,.8f,1f);
        Assert.True(shape.Surface(new(-1.1f,1.32f,0)).Normal.X<-.9f);
        Assert.True(shape.Surface(new(1.1f,.67f,0)).Normal.X>.9f);
    }

    [Fact]
    public void NarrowOutletMatchesTubeButWideInletDoesNot()
    {
        var world=World();
        try
        {
            var funnel=(FunnelPart)world.AddPart(new(){Id="funnel",Kind="funnel",Position=[0,5,0]});
            var pipe=world.AddPart(new(){Id="pipe",Kind="pipe",Position=[2.95f,5,0]});
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
            var funnel=world.AddPart(new(){Id="funnel",Kind="funnel",Position=[0,6,0]});
            var emitter=world.AddPart(new(){Id="torch",Kind="flashlight",Position=[-5,8,0]});
            Assert.Equal(6,WorldGeometry.Trace(TraceMedium.Light,world,new(0,6,-3),Vector3.Back,6,emitter));
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,8,0]});
            world.Start();ball.Velocity=Vector3.Down*4;
            for(var i=0;i<30;i++)world.Step();
            var shape=Assert.Single(funnel.Frustums);
            Assert.True(shape.Surface(funnel.Transform.AffineInverse()*ball.Position).Distance>=ball.Radius-.001f);
            Assert.True(ball.Velocity.Length()<=4.001f);
        }
        finally {world.Free();}
    }
}
