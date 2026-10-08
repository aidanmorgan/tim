using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AssistancePhysicsTests(NativeSceneFixture godot)
{
    public enum Fixture { Torch, Pusher }
    private static string Wire(Fixture value)=>value switch
    {
        Fixture.Torch=>"flashlight",Fixture.Pusher=>"linear_pusher",_=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Theory]
    [InlineData(Fixture.Torch,false)]
    [InlineData(Fixture.Torch,true)]
    [InlineData(Fixture.Pusher,false)]
    [InlineData(Fixture.Pusher,true)]
    public void AssistanceMovesOwnedBodiesAndResetRestoresConstruction(Fixture fixture,bool strict)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var spec=new PartSpec {Id=Wire(fixture),Kind=Wire(fixture),Position=[0,6,0]};
            var target=new PartSpec {Id=Wire(fixture),Kind=Wire(fixture),Position=[.2f,6,0],Orientation = PartOrientation.FromEulerDegrees(0,0,8),
                Difficulty=[new(){Precision=0,PositionWindow=1,RotationWindow=20,MaxPositionCorrection=.3f,MaxRotationCorrection=10,BlendSeconds=.4f},
                    new(){Precision=1,PositionWindow=1,RotationWindow=20,MaxPositionCorrection=0,MaxRotationCorrection=0,BlendSeconds=.4f}]};
            world.LoadMachine(new(){Parts=[spec],PlacementTargets=[target],Gravity=0,Pressure=0});
            world.Precision=strict?1:0;
            var before=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var part=world.FindPart(Wire(fixture))!;
            var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            Assert.Equal(strict?PhysicsMotionType.Static:PhysicsMotionType.Kinematic,body.MotionType);
            var initial=body.Pose;
            for(var i=0;i<12;i++)world.Step();
            if(strict)Assert.Equal(initial,body.Pose);
            else Assert.InRange(body.Center.X,0.01,.19);
            for(var i=0;i<60;i++)world.Step();
            Assert.InRange(Math.Abs(body.Center.X-(strict?0:.2)),0,1e-7);
            if(!strict)
            {
                Assert.Equal(body.PrescribedMotion!.Path.Duration,body.PrescribedMotion.Time);
                Assert.Equal(default,body.LinearVelocity);Assert.Equal(default,body.AngularVelocity);
                Assert.InRange((SceneGeometryAdapter.CaptureVector(part.Position)-body.Center).Length,0,1e-7);
            }
            if(fixture==Fixture.Pusher)
            {
                var head=world.PhysicsAssembly.Body(new(part,LinearPusherPart.HeadBody));
                Assert.Equal(PhysicsMotionType.Dynamic,head.MotionType);
                var expected=body.Pose.TransformPoint(new(LinearPusherPart.RestHeadX,0,0));
                Assert.InRange((head.Center-expected).Length,0,1e-4);
            }
            world.Restore();
            Assert.Equal(before,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Empty(world.Events);
        }
        finally {world.Free();}
    }
    [Fact]
    public void FixtureBoundaryRejectsUndefinedValues()
    {
        Assert.Equal("flashlight",Wire(Fixture.Torch));
        Assert.Equal("linear_pusher",Wire(Fixture.Pusher));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
    }
}
