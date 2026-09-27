using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class LinearPusherTests(HeadlessFixture godot)
{
    private const string PusherId="pusher", BatteryId="battery", SupplyId="supply", ExtendId="extend", RetractId="retract", CargoId="cargo", WallId="wall", HomeId="home", EndId="end";
    private const string BatteryKind="battery", SwitchKind="switch", BallKind="ball", WallKind="wall";
    private MachineWorld World()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static MachinePart Add(MachineWorld world,string id,string kind,Vector3 at)=>
        world.AddPart(new(){Id=id,Kind=kind,Position=[at.X,at.Y,at.Z]});
    private static void Wire(MachineWorld world,MachinePart from,MachinePart to,SocketId input,SocketId output=SocketId.Supply)=>
        Assert.True(world.Connect(from,output,to,input,ConnectionDomain.Electrical));
    private static (LinearPusherPart Pusher,MachinePart Supply,MachinePart Extend,MachinePart Retract) Circuit(MachineWorld world)
    {
        var pusher=(LinearPusherPart)Add(world,PusherId,LinearPusherPart.CatalogId,new(0,4,0));
        var battery=Add(world,BatteryId,BatteryKind,new(-6,1,0));
        var supply=Add(world,SupplyId,SwitchKind,new(-6,3,0));
        var extend=Add(world,ExtendId,SwitchKind,new(-6,5,0));
        var retract=Add(world,RetractId,SwitchKind,new(-6,7,0));
        foreach(var part in new[]{supply,extend,retract})Wire(world,battery,part,SocketId.PowerIn);
        Wire(world,supply,pusher,SocketId.PowerIn);Wire(world,extend,pusher,SocketId.ExtendIn);Wire(world,retract,pusher,SocketId.RetractIn);
        return(pusher,supply,extend,retract);
    }
    private static void Steps(MachineWorld world,int count){for(var i=0;i<count;i++)world.Step();}

    [Theory]
    [InlineData(false,true,false,PusherPhase.Unpowered)]
    [InlineData(true,false,false,PusherPhase.Holding)]
    [InlineData(true,true,true,PusherPhase.Conflict)]
    [InlineData(true,false,true,PusherPhase.Holding)]
    public void IndependentSupplyAndExclusiveCommandAreRequired(bool power,bool extend,bool retract,PusherPhase phase)
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();
            c.Supply.Active=power;c.Extend.Active=extend;c.Retract.Active=retract;
            Steps(world,120);
            Assert.Equal(0,c.Pusher.Extension);Assert.Equal(phase,c.Pusher.Phase);
            Assert.Equal(0,c.Pusher.DeliveredWork);
        }
        finally{world.Free();}
    }

    [Fact]
    public void FullStrokeReversalAndSuppliedEndOutputsRestoreExactly()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var atHome=Add(world,HomeId,SwitchKind,new(5,2,0));
            var atEnd=Add(world,EndId,SwitchKind,new(5,6,0));
            Wire(world,c.Pusher,atHome,SocketId.PowerIn,SocketId.RetractedOut);
            Wire(world,c.Pusher,atEnd,SocketId.PowerIn,SocketId.ExtendedOut);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;world.Step();
            Assert.True(atHome.HasElectricalPower(SocketId.PowerIn));Assert.False(atEnd.HasElectricalPower(SocketId.PowerIn));
            c.Extend.Active=true;Steps(world,300);
            Assert.True(c.Pusher.Extended);Assert.False(c.Pusher.Retracted);
            Assert.True(atEnd.HasElectricalPower(SocketId.PowerIn));Assert.False(atHome.HasElectricalPower(SocketId.PowerIn));
            c.Supply.Active=false;world.Step();Assert.False(atEnd.HasElectricalPower(SocketId.PowerIn));
            c.Supply.Active=true;c.Extend.Active=false;c.Retract.Active=true;Steps(world,300);
            Assert.True(c.Pusher.Retracted);Assert.True(atHome.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,((LinearPusherPart)world.FindPart(PusherId)!).DeliveredWork);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InterruptedSupplyOrConflictBrakesAtExactMidStroke(bool conflict)
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,40);
            var extension=c.Pusher.Extension;Assert.InRange(extension,.1f,1);
            if(conflict)c.Retract.Active=true;else c.Supply.Active=false;
            Steps(world,120);
            Assert.Equal(extension,c.Pusher.Extension);Assert.Equal(0,c.Pusher.TravelSpeed);
            Assert.Equal(conflict?PusherPhase.Conflict:PusherPhase.Unpowered,c.Pusher.Phase);
            c.Retract.Active=false;c.Supply.Active=true;Steps(world,40);Assert.True(c.Pusher.Extension>extension);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(90f)]
    [InlineData(180f)]
    public void PushesActualCargoWithBoundedImpulseAndWork(float angle)
    {
        var world=World();
        try
        {
            var c=Circuit(world);c.Pusher.RotationDegrees=new(0,angle,0);
            var axis=c.Pusher.Basis.X;
            var cargo=Add(world,CargoId,BallKind,c.Pusher.Transform*new Vector3(1.55f,0,0));
            var start=cargo.Position;
            world.Start();c.Supply.Active=true;c.Extend.Active=true;
            var previousWork=0f;
            for(var i=0;i<180;i++)
            {
                world.Step();
                Assert.InRange(c.Pusher.LastDriveImpulse,0,40f/480+.0001f);
                Assert.InRange(c.Pusher.DeliveredWork-previousWork,0,40f*1.5f/120+.001f);
                previousWork=c.Pusher.DeliveredWork;
            }
            Assert.True((cargo.Position-start).Dot(axis)>1);
            Assert.True(c.Pusher.DeliveredWork>0);
            Assert.InRange(cargo.Velocity.Length(),0,1.501f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void PinnedCargoDoesNotLetHeadPassThroughWall()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var cargo=Add(world,CargoId,BallKind,new(1.55f,4,0));
            var wall=Add(world,WallId,WallKind,new(2.1f,4,0));
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1)));
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,360);
            Assert.True(c.Pusher.Extension<.35f);
            Assert.True(cargo.Position.X+cargo.Radius<=2.091f);
            Assert.True(c.Pusher.HeadPosition.X+LinearPusherPart.HeadRadius<=cargo.Position.X-cargo.Radius+.001f);
            Assert.False(c.Pusher.Extended);
        }
        finally{world.Free();}
    }

    [Fact]
    public void MidStrokeResetRestoresOriginalConstructionAndClearsMotion()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,40);
            Assert.True(c.Pusher.Extension>0);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=(LinearPusherPart)world.FindPart(PusherId)!;
            Assert.Equal(0,restored.Extension);Assert.Equal(0,restored.TravelSpeed);
            Assert.Equal(0,restored.DeliveredWork);Assert.True(restored.Retracted);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(PusherParameters.Stroke,0f)]
    [InlineData(PusherParameters.Stroke,4f)]
    [InlineData(PusherParameters.Speed,float.NaN)]
    [InlineData(PusherParameters.Speed,5f)]
    [InlineData(PusherParameters.Acceleration,0f)]
    [InlineData(PusherParameters.Acceleration,31f)]
    [InlineData(PusherParameters.Force,0f)]
    [InlineData(PusherParameters.Force,float.PositiveInfinity)]
    public void InvalidAuthorParametersAreRejected(string parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=PusherId,Kind=LinearPusherPart.CatalogId,
                Properties=new(){[parameter]=value}}));
        }
        finally{world.Free();}
    }

    [Fact]
    public void FreeMotionAccelerationIsBoundedThroughMidStrokeReversal()
    {
        var world=World();
        try
        {
            var c=Circuit(world);world.Start();c.Supply.Active=true;c.Extend.Active=true;
            var previous=0f;
            for(var i=0;i<60;i++)
            {
                if(i==30){c.Extend.Active=false;c.Retract.Active=true;}
                world.Step();
                Assert.InRange(Mathf.Abs(c.Pusher.TravelSpeed-previous),0,12f/120+.0001f);
                previous=c.Pusher.TravelSpeed;
            }
            Assert.True(c.Pusher.TravelSpeed<0);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(1f,false)]
    [InlineData(40f,true)]
    public void VerticalDriveRespectsAvailableForceAgainstGravity(float force,bool lifts)
    {
        var world=World();
        try
        {
            world.Gravity=9.81f;
            var c=Circuit(world);c.Pusher.RotationDegrees=new(0,0,90);
            c.Pusher.Properties[PusherParameters.Force]=force;
            var cargo=Add(world,CargoId,BallKind,c.Pusher.HeadPosition+Vector3.Up*.6f);
            var initial=cargo.Position.Y;
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,180);
            if(lifts)Assert.True(cargo.Position.Y>initial+.5f,
                $"Cargo {cargo.Position.Y}, initial {initial}, extension {c.Pusher.Extension}, phase {c.Pusher.Phase}, speed {cargo.Velocity.Y}, work {c.Pusher.DeliveredWork}");
            else Assert.True(cargo.Position.Y<initial+.1f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(4f)]
    [InlineData(8f)]
    public void HeavyCargoAcceleratesWithoutExceedingForceBudget(float mass)
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var cargo=world.AddPart(new(){Id=CargoId,Kind=BallKind,Position=[1.55f,4,0],
                Properties=new(){[WeightParameters.Mass]=mass}});
            world.Start();c.Supply.Active=true;c.Extend.Active=true;
            for(var i=0;i<240;i++)
            {
                world.Step();
                Assert.InRange(c.Pusher.LastDriveImpulse,0,40f/480+.0001f);
            }
            Assert.True(cargo.Position.X>2.55f);
            Assert.InRange(.5f*mass*cargo.Velocity.LengthSquared(),0,c.Pusher.DeliveredWork+.01f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void InitiallyOverlappingHeadStopsWithoutMovingItself()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var wall=Add(world,WallId,WallKind,c.Pusher.HeadPosition);
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1)));
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,60);
            Assert.Equal(0,c.Pusher.Extension);Assert.Equal(PusherPhase.Blocked,c.Pusher.Phase);
            Assert.Equal(0,c.Pusher.DeliveredWork);
        }
        finally{world.Free();}
    }

    [Fact]
    public void CurrentJsonReplayReproducesStrokeAndCargoExactly()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            Add(world,CargoId,BallKind,new(1.55f,4,0));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,80);
            var extension=c.Pusher.Extension;var work=c.Pusher.DeliveredWork;
            var position=world.FindPart(CargoId)!.Position;
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();world.FindPart(SupplyId)!.Active=true;world.FindPart(ExtendId)!.Active=true;Steps(world,80);
            var replay=(LinearPusherPart)world.FindPart(PusherId)!;
            Assert.Equal(extension,replay.Extension);Assert.Equal(work,replay.DeliveredWork);
            Assert.Equal(position,world.FindPart(CargoId)!.Position);
        }
        finally{world.Free();}
    }

    [Fact]
    public void ThinStaticObstructionStopsHeadWithoutClipping()
    {
        var world=World();
        try
        {
            var c=Circuit(world);
            var wall=Add(world,WallId,WallKind,new(2,4,0));
            wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.01f,1,1)));
            world.Start();c.Supply.Active=true;c.Extend.Active=true;Steps(world,240);
            Assert.Equal(PusherPhase.Blocked,c.Pusher.Phase);
            Assert.InRange(c.Pusher.Extension,.889f,.891f);
            Assert.Equal(0,c.Pusher.DeliveredWork);
        }
        finally{world.Free();}
    }
}
