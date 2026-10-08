using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class InitialParticipationTests(NativeSceneFixture godot)
{
    private enum Fixture { Ball, Detector }
    private static string Wire(Fixture value)=>value switch
    {
        Fixture.Ball=>"ball",Fixture.Detector=>"ball_detector",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Fact]
    public void InitiallyHiddenBodyRetainsIdentityAndCanBeEnabledWithoutRecapture()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[0,6,0]});
            ball.Visible=false;
            Assert.DoesNotContain(WorldGeometry.CaptureBodies(world),b=>b.Owner==ball);
            Assert.Contains(WorldGeometry.CapturePhysicsBodies(world),b=>b.Geometry.Owner==ball&&b.InitialParticipation==CollisionParticipation.Disabled);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            Assert.False(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
            var saved=world.Physics.Capture();
            ball.Visible=true;
            world.Step();
            Assert.False(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
            var declaration=world.Physics.Collider(body.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(body.Id,declaration.Geometry,declaration.Material,CollisionParticipation.Enabled)]);
            Assert.True(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
            Assert.Same(body,world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)));
            Assert.Contains(WorldGeometry.CaptureBodies(world),b=>b.Owner==ball);
            world.Physics.Restore(saved);
            Assert.False(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
        }
        finally {world.Free();}
    }

    [Fact]
    public void HiddenOverlappingBodyDoesNotBlockRunButEnablingOverlapIsRejected()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[0,Workbench.SurfaceY,0]});
            ball.Visible=false;world.Start();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var declaration=world.Physics.Collider(body.Id).Declaration;
            Assert.ThrowsAny<InvalidOperationException>(()=>world.Physics.ApplyColliderUpdates(
                [new(body.Id,declaration.Geometry,declaration.Material,CollisionParticipation.Enabled)]));
            Assert.False(WorldGeometry.CaptureSpatialState(world,new(ball,MachinePart.RootBody)).Enabled);
            world.Step();
        }
        finally {world.Free();}
    }

    [Fact]
    public void InitialParticipationRejectsUndefinedEnumAndFixtureBoundaryRejectsUnknownValues()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var geometry=new CompoundGeometry([new(new ConvexSphere(.3),AffineTransform.Identity)]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsObject(body,geometry,new(0,0,0))
            {InitialParticipation=(CollisionParticipation)99});
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
        Assert.Equal("ball",Wire(Fixture.Ball));
        Assert.Equal("ball_detector",Wire(Fixture.Detector));
    }
}
