using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CannonChamberGeometryTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryOwnedChildMustBeSeatedAndResetPreservesAuthoredConstruction(bool protrudes)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=(CannonPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=CannonPart.CatalogId,Position=[0,5,0]});
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind="ball",Position=[0,5,0]});
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind="battery",Position=[-5,1,0]});
            ball.Spheres.Add(new(new(protrudes?.8f:0,.39f,0),.04f,MachinePart.RootBody));
            Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<200;i++)world.Step();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var geometry=world.Physics.Collider(body.Id).Declaration.Geometry;
            Assert.True(geometry.Count>1);
            Assert.Equal(protrudes?CannonPhase.Loading:CannonPhase.Ready,cannon.Phase);
            ball.Spheres.Clear();ball.Position=new(20,20,20);ball.Visible=false;
            world.Activate(cannon);world.Step();world.Step();
            Assert.Same(geometry,world.Physics.Collider(body.Id).Declaration.Geometry);
            Assert.Equal(protrudes?CannonShotResult.Unseated:CannonShotResult.Fired,cannon.LastShot);
            Assert.Equal(protrudes?0:1,cannon.ShotCount);
            if(protrudes)
            {
                Assert.Equal(45,cannon.StoredEnergy);Assert.Equal(0,cannon.ReleasedEnergy);
                Assert.Equal(default,body.LinearVelocity);
            }
            else
            {
                Assert.True(body.LinearVelocity.X>9);Assert.Same(ball,cannon.LastPayload);
            }
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,((CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!).StoredEnergy);
        }
        finally {world.Free();}
    }
}
