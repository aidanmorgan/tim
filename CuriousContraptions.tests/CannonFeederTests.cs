using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class CannonFeederTests(HeadlessFixture godot, ITestOutputHelper output)
{
    private const string CannonId="cannon", FirstId="first", SecondId="second",
        BatteryId="battery", ClockId="clock", DelayId="delay", LatchId="latch", GateId="gate";
    private const string BallKind="ball", BatteryKind="battery", ClockKind="clock",
        DelayKind="delay", LatchKind="latch", GateKind="powered_gate";

    [Theory]
    [InlineData(.05f,true)]
    [InlineData(0f,true)]
    [InlineData(0f,false)]
    public void TimedGateFeedsDistinctPhysicalBallAfterFirstShot(float feederX,bool poweredGate)
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var cannon=(CannonPart)world.AddPart(new(){Id=CannonId,Kind=CannonPart.CatalogId,
                Position=[0,3,0],Rotation=[0,0,75]});
            var first=world.AddPart(new(){Id=FirstId,Kind=BallKind,Position=[0,3,0]});
            var second=world.AddPart(new(){Id=SecondId,Kind=BallKind,Position=[feederX,8.8f,0]});
            var battery=world.AddPart(new(){Id=BatteryId,Kind=BatteryKind,Position=[-4,3,2]});
            var clock=world.AddPart(new(){Id=ClockId,Kind=ClockKind,Position=[-3,5,-2]});
            var delay=world.AddPart(new(){Id=DelayId,Kind=DelayKind,Position=[-5,6,-2]});
            var latch=world.AddPart(new(){Id=LatchId,Kind=LatchKind,Position=[3,5,2]});
            var gate=world.AddPart(new(){Id=GateId,Kind=GateKind,Position=[feederX,8,0],Rotation=[0,0,90]});
            void Link(MachinePart a,SocketId from,MachinePart b,SocketId to,ConnectionDomain domain)
                =>Assert.True(world.Connect(a,from,b,to,domain));
            Link(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical);
            Link(battery,SocketId.Supply,clock,SocketId.PowerIn,ConnectionDomain.Electrical);
            Link(battery,SocketId.Supply,latch,SocketId.PowerIn,ConnectionDomain.Electrical);
            if(poweredGate) Link(latch,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical);
            Link(clock,SocketId.ActivationOut,cannon,SocketId.ActivationIn,ConnectionDomain.Activation);
            Link(clock,SocketId.ActivationOut,delay,SocketId.ActivationIn,ConnectionDomain.Activation);
            Link(delay,SocketId.ActivationOut,latch,SocketId.SetIn,ConnectionDomain.Activation);
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var fired=new List<MachinePart>();
            world.Start();
            for(var tick=0;tick<960;tick++)
            {
                world.Step();
                if(cannon.ShotCount>fired.Count)fired.Add(cannon.LastPayload!);
                Assert.Equal(8,world.Parts.Count);
                Assert.Same(first,world.FindPart(FirstId));
                Assert.Same(second,world.FindPart(SecondId));
                if(tick%60==0)output.WriteLine($"tick={tick} shots={cannon.ShotCount} result={cannon.LastShot} second={second.Position} velocity={second.Velocity}");
            }
            Assert.Equal(poweredGate?2:1,fired.Count);
            Assert.Same(first,fired[0]);
            if(poweredGate) Assert.Same(second,fired[1]);
            else
            {
                Assert.True(second.Visible);
                Assert.InRange(second.Position.Y,8.39f,8.41f);
            }
            var expectedEnergy=poweredGate?90:45;
            Assert.InRange(cannon.ReleasedEnergy,expectedEnergy-.01,expectedEnergy);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally {world.Free();}
    }
}
