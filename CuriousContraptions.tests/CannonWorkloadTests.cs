using System.Diagnostics;
using System.Text.Json;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CannonWorkloadTests(NativeSceneFixture godot)
{
    public enum SupplyConnection { Connected, Disconnected }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Cannon=new("cannon"),Ball=new("ball"),Battery=new("battery"),Clock=new("clock");
    private const string ResultPrefix="CCCANNONWORKLOAD ";
    private sealed record WorkloadResult(SupplyConnection Supply,int Ticks,int Shots,double ReleasedEnergy,
        double Milliseconds,long AllocatedBytes,PhysicsBodySnapshot[] Bodies);

    [Theory]
    [InlineData(SupplyConnection.Connected)]
    [InlineData(SupplyConnection.Disconnected)]
    public void BrowserWorkloadRetainsPhysicalResultsAndExactReset(SupplyConnection supply)
    {
        if(!Enum.IsDefined(supply))throw new ArgumentOutOfRangeException(nameof(supply));
        var world=new MachineWorld {Precision=.45f,Realistic=false};
        godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=(CannonPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Cannon.Value,
                Position=[0,3,0],Orientation=PartOrientation.FromEulerDegrees(0,0,90)});
            world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Ball.Value,Position=[0,4.999069f,0]});
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[-4,3,2]});
            var clock=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Clock.Value,Position=[-3,4.9985094f,-2]});
            if(supply==SupplyConnection.Connected)
                Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(battery,SocketId.Supply,clock,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(clock,SocketId.ActivationOut,cannon,SocketId.ActivationIn,ConnectionDomain.Activation));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var start=Stopwatch.GetTimestamp();
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            for(var tick=0;tick<3600;tick++)world.Step();
            var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
            Assert.Equal(3600,world.Ticks);
            Assert.Equal(supply==SupplyConnection.Connected?14:0,cannon.ShotCount);
            Assert.Equal(supply==SupplyConnection.Connected?630:0,cannon.ReleasedEnergy);
            Console.WriteLine(ResultPrefix+JsonSerializer.Serialize(new WorkloadResult(supply,world.Ticks,cannon.ShotCount,
                cannon.ReleasedEnergy,elapsed,allocated,world.Physics.Capture().BodyStates.ToArray())));
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }
}
