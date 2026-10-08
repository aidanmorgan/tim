using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineSkinTests(NativeSceneFixture godot)
{

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingBedUsesRelativeImpactSpeedAndRestoresExactly(bool strict)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var bedSpec=Spec(Role.Bed,new(0,4,0));
            var payloadSpec=Spec(Role.Payload,new(0,4.64f,0));
            var target=Spec(Role.Bed,new(0,4.2f,0));
            target.Difficulty=[
                new(){Precision=0,PositionWindow=1,MaxPositionCorrection=.3f,BlendSeconds=.4f},
                new(){Precision=1,PositionWindow=1,MaxPositionCorrection=0,BlendSeconds=.4f}];
            world.LoadMachine(new(){Parts=[bedSpec,payloadSpec],PlacementTargets=[target],Gravity=0,Pressure=0});
            world.Precision=strict?1:0;
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            void Run()
            {
                var bed=Assert.Single(world.Parts.OfType<TrampolinePart>());
                var payload=world.FindPart(payloadSpec.Id)!;
                world.Start();
                var body=world.PhysicsAssembly.Body(new(bed,MachinePart.RootBody));
                Assert.Equal(strict?CuriousContraptions.Physics.PhysicsMotionType.Static:
                    CuriousContraptions.Physics.PhysicsMotionType.Kinematic,body.MotionType);
                for(var i=0;i<60;i++) world.Step();
                if(strict)
                {
                    Assert.Equal(0,bed.ImpactCount);
                    Assert.Equal(4.64f,payload.Position.Y);
                }
                else
                {
                    Assert.True(bed.ImpactCount>0);
                    Assert.True(payload.Position.Y>4.64f);
                }
            }
            Run();
            var first=world.Physics.Capture().BodyStates.ToArray();
            world.Restore();
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Run();
            var replay=world.Physics.Capture().BodyStates.ToArray();
            Assert.Equal(first.Length,replay.Length);
            for(var i=0;i<first.Length;i++)
            {
                Assert.Equal(first[i] with {PrescribedMotion=null},replay[i] with {PrescribedMotion=null});
                if(first[i].PrescribedMotion is not { } motion)
                {
                    Assert.Null(replay[i].PrescribedMotion);
                    continue;
                }
                var restored=Assert.IsType<CuriousContraptions.Physics.PrescribedBodyMotion>(replay[i].PrescribedMotion);
                Assert.Equal(motion.LocalPose,restored.LocalPose);
                Assert.Equal(motion.Time,restored.Time);
                Assert.Equal(motion.Path.StartPose,restored.Path.StartPose);
                Assert.Equal(motion.Path.Duration,restored.Path.Duration);
                Assert.Equal(motion.Path.Translation,restored.Path.Translation);
                Assert.Equal(motion.Path.RotationVector,restored.Path.RotationVector);
            }
        }
        finally {world.Free();}
    }
    private enum Role { Bed, Payload }
    private static PartSpec Spec(Role role, Vector3 at) => new()
    {
        Id = role switch { Role.Bed => "bed", Role.Payload => "payload", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Bed => TrampolinePart.CatalogId, Role.Payload => "ball", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X, at.Y, at.Z]
    };
    [Theory]
    [InlineData(.8f, 0f)]
    [InlineData(-.8f, 0f)]
    [InlineData(.8f, .5f)]
    [InlineData(-.8f, -.5f)]
    [InlineData(0f, .5f)]
    public void OffCentreSagSpreadsAcrossAvailableFabricWithFixedEdges(float x, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed = (TrampolinePart)world.AddPart(Spec(Role.Bed, new(0, 4, 0)));
            var ball = world.AddPart(Spec(Role.Payload, new(x, 4.56f, z)));
            ball.InitialVelocity = Vector3.Down * 5;
            world.Start();
            var checkedDeepContact = false;
            for (var tick = 0; tick < 40; tick++)
            {
                world.Step();
                if (bed.Compression < .15f) continue;
                checkedDeepContact = true;
                Assert.True(TrampolinePart.RestHeight - bed.MembraneHeight(Vector2.Zero) > bed.Compression * .15f);
                for (var i = 0; i <= 20; i++)
                {
                    var u = Mathf.Lerp(-TrampolinePart.BedHalf.X, TrampolinePart.BedHalf.X, i / 20f);
                    var v = Mathf.Lerp(-TrampolinePart.BedHalf.Y, TrampolinePart.BedHalf.Y, i / 20f);
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(u, -TrampolinePart.BedHalf.Y)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(u, TrampolinePart.BedHalf.Y)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(-TrampolinePart.BedHalf.X, v)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(TrampolinePart.BedHalf.X, v)));
                    for (var j = 0; j <= 20; j++)
                    {
                        var sampleZ = Mathf.Lerp(-TrampolinePart.BedHalf.Y, TrampolinePart.BedHalf.Y, j / 20f);
                        Assert.InRange(bed.MembraneHeight(new(u, sampleZ)),
                            TrampolinePart.RestHeight - bed.Compression - .00001f, TrampolinePart.RestHeight);
                    }
                }
                var position = ball.Position;
                var velocity = world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity;
                var energy = bed.StoredElasticEnergy;
                bed._Process(.016);
                bed._Process(.5);
                Assert.Equal(position, ball.Position);
                Assert.Equal(velocity, world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity);
                Assert.Equal(energy, bed.StoredElasticEnergy);
            }
            Assert.True(checkedDeepContact);
            world.Restore();
            var restored = Assert.Single(world.Parts.OfType<TrampolinePart>());
            Assert.Equal(TrampolinePart.RestHeight, restored.MembraneHeight(Vector2.Zero));
        }
        finally { world.Free(); }
    }
}
