using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BodyDiagnosticTests(NativeSceneFixture godot)
{
    public enum Fixture { Ball, Domino, Spring }
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Ball=>"ball",Fixture.Domino=>"domino",Fixture.Spring=>WoundSpringPart.CatalogId,
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture)
    {
        var kind=Kind(fixture);
        return world.AddPart(new(){Id=kind,Kind=kind,Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(20,30,10)});
    }
    private static float[] Point(CollisionVector point)=>[(float)point.X,(float)point.Y,(float)point.Z];

    [Theory]
    [InlineData(Fixture.Ball,false)]
    [InlineData(Fixture.Ball,true)]
    [InlineData(Fixture.Domino,false)]
    [InlineData(Fixture.Domino,true)]
    [InlineData(Fixture.Spring,false)]
    [InlineData(Fixture.Spring,true)]
    public void RuntimeReportReadsOwnedBodiesBeforePresentationAndWhilePaused(Fixture fixture,bool paused)
    {
        var world=World();
        try
        {
            var part=Add(world,fixture);
            var moving=fixture==Fixture.Spring?Assert.Single(part.InternalBodies):part;
            if(fixture!=Fixture.Spring) moving.InitialVelocity=new(1,0,0);
            world.Start();
            world.Physics.Step([],[],.01);
            var body=world.PhysicsAssembly.Body(new(moving,MachinePart.RootBody));
            world.Physics.ApplyImpulse(body.Id,new(0,.01,0),body.Center);
            world.Running=!paused;
            var signature=world.StateSignature();
            moving.Position=new(20,30,40);
            moving.Rotation=new(.8f,.3f,.2f);
            moving.Visible=false;
            Assert.Equal(signature,world.StateSignature());
            var before=body.Snapshot();var time=world.Physics.Time;
            var report=PlaytestBody.Capture(world,moving);
            Assert.Equal(moving.Uid,report.Id);Assert.False(report.Visible);
            Assert.Equal(Point(body.Center-body.Pose.Rotation.Apply(SceneGeometryAdapter.CaptureVector(moving.LocalCenterOfMass))),report.Position);
            Assert.Equal(Point(body.LinearVelocity),report.Velocity);
            Assert.NotEqual(new[]{moving.Position.X,moving.Position.Y,moving.Position.Z},report.Position);
            var json=JsonSerializer.Serialize(new PlaytestFrame{Bodies=[report]},PlaytestJson.Default.PlaytestFrame);
            var restored=Assert.Single(JsonSerializer.Deserialize(json,PlaytestJson.Default.PlaytestFrame)!.Bodies);
            Assert.Equal(report.Position,restored.Position);Assert.Equal(report.Velocity,restored.Velocity);
            Assert.Equal(before,body.Snapshot());Assert.Equal(time,world.Physics.Time);
            world.Restore();
            Assert.Throws<InvalidOperationException>(()=>PlaytestBody.Capture(world,world.Parts.Single()));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeSignatureDetectsOwnedMotionAndParticipationBeforePresentation(bool paused)
    {
        var world=World();
        try
        {
            var part=Add(world,Fixture.Ball);
            world.Start();
            world.Running=!paused;
            var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            var saved=world.Physics.Capture();
            var signature=world.StateSignature();
            world.Physics.ApplyImpulse(body.Id,new(.1,.2,.3),body.Center+new CollisionVector(.2,0,0));
            Assert.NotEqual(signature,world.StateSignature());
            world.Physics.Restore(saved);
            Assert.Equal(signature,world.StateSignature());
            var collider=world.Physics.Collider(body.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(body.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            Assert.True(part.Visible);
            Assert.NotEqual(signature,world.StateSignature());
            world.Physics.Restore(saved);
            Assert.Equal(signature,world.StateSignature());
        }
        finally {world.Free();}
    }

    [Fact]
    public void ConstructionAndForeignBodiesCannotMasqueradeAsRuntimeEvidence()
    {
        var world=World();var other=World();
        try
        {
            var local=Add(world,Fixture.Ball);var foreign=Add(other,Fixture.Ball);
            Assert.Throws<InvalidOperationException>(()=>PlaytestBody.Capture(world,local));
            world.Start();
            Assert.Throws<ArgumentException>(()=>PlaytestBody.Capture(world,foreign));
            Assert.Throws<ArgumentNullException>(()=>PlaytestBody.Capture(null!,local));
            Assert.Throws<ArgumentNullException>(()=>PlaytestBody.Capture(world,null!));
        }
        finally {world.Free();other.Free();}
    }
}
