using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SpringAnimationTests(NativeSceneFixture godot)
{
    private enum Role { Spring, Ball }
    private static string Id(Role role)=>role switch
    {
        Role.Spring=>"spring",Role.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static readonly NodePath PlatePath=new("Visual/SpringPlate");
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static SpringPart Spring(MachineWorld world,float preload=0)=>(SpringPart)world.AddPart(new()
    {
        Id=Id(Role.Spring),Kind=Id(Role.Spring),Position=[0,4,0],
        Properties=new()
        {
            [PartParameterName.Of(SpringParameter.InitialCompression)]=preload,
            [PartParameterName.Of(SpringParameter.Damping)]=0
        }
    });
    private static MachinePart Ball(MachineWorld world,SpringPart spring,Vector3 local,Vector3 velocity)
    {
        var at=spring.Transform*local;
        var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Id(Role.Ball),Position=[at.X,at.Y,at.Z]});
        ball.InitialVelocity=spring.Basis*velocity;return ball;
    }
    private static double Energy(MachineWorld world,SpringPart spring,MachinePart ball)=>
        world.PhysicsAssembly.Body(new(spring,SpringPart.PlateBody)).KineticEnergy+
        world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).KineticEnergy+spring.StoredElasticEnergy;

    [Theory]
    [InlineData(0,0,0)]
    [InlineData(30,50,70)]
    [InlineData(90,0,0)]
    public void PassivePlateLoadsAndReboundsWithoutInventingEnergy(float x,float y,float z)
    {
        var world=World();
        try
        {
            var spring=Spring(world);spring.RotationDegrees=new(x,y,z);
            var ball=Ball(world,spring,new(0,1,0),new(0,-2,0));
            world.Start();
            var initial=Energy(world,spring,ball);double maximumCompression=0,maximumReturnSpeed=0;
            var normal=SceneGeometryAdapter.CaptureVector(spring.Basis.Y.Normalized());
            for(var tick=0;tick<120;tick++)
            {
                world.Step();
                maximumCompression=Math.Max(maximumCompression,-spring.PlateOffset);
                maximumReturnSpeed=Math.Max(maximumReturnSpeed,
                    CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,normal));
                Assert.InRange(Energy(world,spring,ball),0,initial+1e-5);
            }
            Assert.True(spring.HitCount>0);
            Assert.InRange(maximumCompression,.01,SpringPart.MaximumStroke+1e-6);
            Assert.True(maximumReturnSpeed>.2);
            Assert.Empty(spring.PhysicsImpact(default,default,1));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(0,false)]
    [InlineData(.2f,false)]
    [InlineData(.2f,true)]
    public void InitialEnergyIsFiniteAndMissedOrUnchargedPayloadReceivesNoLaunch(float preload,bool misses)
    {
        var world=World();
        try
        {
            var spring=Spring(world,preload);
            var ball=Ball(world,spring,new(misses?2:0,SpringPart.RestHeight-preload+.075f+.34f+.002f,0),Vector3.Zero);
            world.Start();var initial=Energy(world,spring,ball);double speed=0;
            Assert.InRange(Math.Abs(initial-new AxialElasticPotential(400,0).Energy(preload)),0,1e-5);
            for(var tick=0;tick<90;tick++)
            {
                world.Step();Assert.InRange(Energy(world,spring,ball),0,initial+1e-5);
                speed=Math.Max(speed,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length);
            }
            if(preload==0||misses){Assert.Equal(0,speed);Assert.Equal(0,spring.HitCount);}
            else {Assert.True(speed>1);Assert.True(spring.HitCount>0);}
        }
        finally {world.Free();}
    }

    [Fact]
    public void FunctionalPlateMatchesColliderAndRenderingCannotChangeReplayOrReset()
    {
        var world=World();
        try
        {
            Spring(world,.1f);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            PhysicsBodySnapshot[] Run(int renderFrames)
            {
                var spring=world.Parts.OfType<SpringPart>().Single();
                world.Start();
                for(var tick=0;tick<40;tick++)
                {
                    world.Step();
                    var before=world.Physics.Capture().BodyStates.ToArray();
                    for(var frame=0;frame<renderFrames;frame++)world.PresentFrame(1.0/120/renderFrames,1);
                    Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
                    var plate=world.PhysicsAssembly.Body(new(spring,SpringPart.PlateBody));
                    var visual=spring.GetNode<Node3D>(PlatePath);
                    Assert.InRange((plate.Center-SceneGeometryAdapter.CaptureVector(visual.GlobalPosition)).Length,0,1e-6);
                }
                return world.Physics.Capture().BodyStates.ToArray();
            }
            var first=Run(1);world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,world.Parts.OfType<SpringPart>().Single().HitCount);
            var second=Run(4);Assert.Equal(first,second);world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(SpringParameter.Stiffness,119)]
    [InlineData(SpringParameter.Stiffness,1201)]
    [InlineData(SpringParameter.Damping,-.1f)]
    [InlineData(SpringParameter.Damping,8.1f)]
    [InlineData(SpringParameter.InitialCompression,-.01f)]
    [InlineData(SpringParameter.InitialCompression,.21f)]
    [InlineData(SpringParameter.Stiffness,float.NaN)]
    public void InvalidParametersReject(SpringParameter parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new()
            {
                Id=Id(Role.Spring),Kind=Id(Role.Spring),Position=[0,4,0],
                Properties=new(){[PartParameterName.Of(parameter)]=value}
            }));
        }
        finally {world.Free();}
    }

    [Fact]
    public void RemovedStrengthAndUndefinedSelectorsReject()
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new()
            {
                Id=Id(Role.Spring),Kind=Id(Role.Spring),Position=[0,4,0],
                Properties=new(){["strength"]=8.5f} // Removed external resource field.
            }));
            Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((SpringParameter)999));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
        }
        finally {world.Free();}
    }
}
