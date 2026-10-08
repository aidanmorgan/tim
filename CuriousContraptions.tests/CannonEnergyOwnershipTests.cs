using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CannonEnergyOwnershipTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SceneReadsRestoredWorldLedgerWithoutCachingAndConstructionResetReplays(bool powered)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=(CannonPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=CannonPart.CatalogId,Position=[0,5,0]});
            var payload=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind="ball",Position=[0,5,0]});
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind="battery",Position=[-5,1,0]});
            if(powered)Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.Equal(0,cannon.StoredEnergy);Assert.Equal(0,cannon.AcceptedEnergy);Assert.Equal(0,cannon.ReleasedEnergy);
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var connections=world.Connections.ToArray();
            void StartAndCharge()
            {
                world.Start();for(var i=0;i<60;i++)world.Step();
            }
            StartAndCharge();
            var owner=world.PhysicsAssembly.Body(new(cannon,MachinePart.RootBody));
            var body=world.PhysicsAssembly.Body(new(payload,MachinePart.RootBody));
            Assert.Single(world.Physics.EnergyStores.ToArray());
            Assert.Equal(powered,cannon.StoredEnergy>0);
            var charged=world.Physics.Capture();
            var state=world.Physics.EnergyStore(owner.Id);
            Assert.Equal(cannon.StoredEnergy,state.Energy);Assert.Equal(cannon.AcceptedEnergy,state.AcceptedEnergy);
            var release=world.Physics.ReleaseEnergyStore(owner.Id,body.Id,new(1,0,0),body.Center,4,100);
            Assert.Equal(powered,release.Impulse>0);
            Assert.Equal(state.Energy-release.SuppliedWork,cannon.StoredEnergy);
            Assert.Equal(release.SuppliedWork,cannon.ReleasedEnergy);
            var released=world.Physics.Capture();
            cannon.Position=new(20,20,20);cannon.Visible=false;
            world.Physics.Restore(charged);
            Assert.Equal(state,world.Physics.EnergyStore(owner.Id));
            Assert.Equal(state.Energy,cannon.StoredEnergy);Assert.Equal(state.AcceptedEnergy,cannon.AcceptedEnergy);
            Assert.Equal(state.ReleasedEnergy,cannon.ReleasedEnergy);
            Assert.Equal(charged.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(release,world.Physics.ReleaseEnergyStore(owner.Id,body.Id,new(1,0,0),body.Center,4,100));
            Assert.Equal(released.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            Assert.Equal(released.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(connections,world.Connections.ToArray());Assert.False(world.HasPhysicsState);
            cannon=(CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            payload=world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            Assert.Equal(0,cannon.StoredEnergy);Assert.Equal(0,cannon.AcceptedEnergy);Assert.Equal(0,cannon.ReleasedEnergy);
            StartAndCharge();
            owner=world.PhysicsAssembly.Body(new(cannon,MachinePart.RootBody));
            body=world.PhysicsAssembly.Body(new(payload,MachinePart.RootBody));
            Assert.Equal(charged.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
            Assert.Equal(release,world.Physics.ReleaseEnergyStore(owner.Id,body.Id,new(1,0,0),body.Center,4,100));
            Assert.Equal(released.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(released.EnergyStates.ToArray(),world.Physics.EnergyStores.ToArray());
        }
        finally {world.Free();}
    }
}
