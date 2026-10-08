using Godot;
using CuriousContraptions.Bridge;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SampledPresentationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Ball=new("ball");
    [Fact]
    public void SceneBindingSamplesAcceptedTimeWithoutChangingPhysicsOrConstruction()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            world.Start();world.Step();
            var before=world.Physics.Capture().BodyStates.ToArray();
            var saved=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            using(var lease=world.ReadCommittedPoses())
            {
                var time=lease.Stamp(PoseSample.Current).SimulationTime*.5;
                world.PhysicsAssembly.PresentCommitted(lease,time);
                Assert.InRange(Math.Abs(ball.Position.X-time),0,1e-9);
                Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
                Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
                world.PhysicsAssembly.PresentCommitted(lease,lease.Stamp(PoseSample.Current).SimulationTime);
                Assert.InRange(Math.Abs(ball.Position.X-MachineWorld.Tick),0,1e-9);
            }
            world.Restore();world.Start();
            using var reset=world.ReadCommittedPoses();
            world.PhysicsAssembly.PresentCommitted(reset,0);
            Assert.Equal(Vector3.Zero with {Y=6},world.FindPart(FixtureParts.Id(FixturePartId.First))!.Position);
        }
        finally {world.Free();}
    }
}
