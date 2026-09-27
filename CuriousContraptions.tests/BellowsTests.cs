using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BellowsTests(HeadlessFixture godot)
{
    private MachineWorld World(float gravity=9.81f)
    {
        var world=new MachineWorld {Gravity=gravity};godot.Tree.Root.AddChild(world);return world;
    }
    private static BellowsPart Pump(MachineWorld world)=> (BellowsPart)world.AddPart(new(){Id="pump",Kind="bellows",Position=[-4,6,0]});
    [Theory]
    [InlineData(false,false,false)]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void PhysicalImpactDrivesWindmillAndConveyorOnlyWithClearAirAndBelt(bool missed,bool blocked,bool disconnected)
    {
        var world=World();
        try
        {
            var pump=Pump(world);
            world.AddPart(new(){Id="striker",Kind="ball",Position=[-4,9,missed?2:0]});
            var rotor=(WindmillPart)world.AddPart(new(){Id="rotor",Kind="windmill",Position=[-1,5.88f,0]});
            var belt=(ConveyorPart)world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,3,0]});
            var cargo=world.AddPart(new(){Id="cargo",Kind="ball",Position=[1,3.6f,0]});
            if(!disconnected)Assert.True(world.Connect(rotor,belt));
            if(blocked)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,5.88f,0],Rotation=[0,90,0]});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var maximumSpeed=0f;var maximumCompression=0f;
            for(var i=0;i<480;i++)
            {
                world.Step();maximumSpeed=Math.Max(maximumSpeed,rotor.ShaftSpeed);
                maximumCompression=Math.Max(maximumCompression,pump.Compression);
                Assert.Equal(pump.Boxes[0].At,pump.GetNode<Node3D>("Visual/PressPlate").Position);
                Assert.InRange(pump.Compression,0,BellowsPart.MaximumStroke);
            }
            Assert.Equal(missed?0:1,pump.StrokeCount);
            Assert.Equal(!missed&&!blocked,maximumSpeed>1);
            Assert.Equal(!missed&&!blocked&&!disconnected,world.Events.ContainsKey(new(MachineEventKind.Transported,belt.Uid,cargo.Uid)));
            if(!missed&&!blocked&&!disconnected)Assert.True(cargo.Position.X>2.5f);
            else Assert.Equal(1,cargo.Position.X,3);
            Assert.Equal(0,rotor.ShaftSpeed);Assert.Null(pump.AirflowSource);
            if(!missed){Assert.Equal(BellowsPart.MaximumStroke,maximumCompression);Assert.Equal(BellowsPhase.Held,pump.Phase);}
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            pump=(BellowsPart)world.FindPart("pump")!;
            Assert.Equal(BellowsPhase.Ready,pump.Phase);Assert.Equal(0,pump.Compression);Assert.Equal(0,pump.StrokeCount);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<480;i++)world.Step();
            Assert.Equal(missed?0:1,((BellowsPart)world.FindPart("pump")!).StrokeCount);
        }
        finally{world.Free();}
    }
    [Fact]
    public void LoadHoldsStrokeAndClearingPlateAllowsSilentRefillThenAnotherImpact()
    {
        var world=World();
        try
        {
            var pump=Pump(world);var body=world.AddPart(new(){Id="ball",Kind="ball",Position=[-4,9,0]});
            world.Start();for(var i=0;i<360;i++)world.Step();
            Assert.Equal(BellowsPhase.Held,pump.Phase);Assert.Equal(1,pump.StrokeCount);
            Assert.InRange(body.Position.Y,6.44f,6.47f);
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(1,pump.StrokeCount);
            body.Position=new(-4,9,2);body.Velocity=Vector3.Zero;
            var previous=pump.Compression;
            for(var i=0;i<240;i++)
            {
                world.Step();Assert.Null(pump.AirflowSource);Assert.True(pump.Compression<=previous);
                previous=pump.Compression;
            }
            Assert.Equal(BellowsPhase.Ready,pump.Phase);Assert.Equal(0,pump.Compression);
            body.Position=new(-4,9,0);body.Velocity=Vector3.Zero;
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(2,pump.StrokeCount);Assert.Equal(BellowsPhase.Held,pump.Phase);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(25f,40f,15f)]
    public void RealLocalTopImpactWorksInAllOrientations(float x,float y,float z)
    {
        var world=World(0);
        try
        {
            var pump=(BellowsPart)world.AddPart(new(){Id="pump",Kind="bellows",Position=[0,6,0],Rotation=[x,y,z]});
            var up=pump.Basis.Y;var start=pump.Position+up*1.1f;
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[start.X,start.Y,start.Z]});
            world.Start();ball.Velocity=-up*5;
            for(var i=0;i<30;i++)world.Step();
            Assert.Equal(1,pump.StrokeCount);Assert.True(pump.Compression>0);
            var emission=pump.AirflowSource!.Value;
            Assert.True((pump.Basis*emission.Direction).IsEqualApprox(pump.Basis.X));
            Assert.Equal(pump.GetNode<Node3D>("Visual/PressPlate").Position,pump.Boxes[0].At);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(.5f,1f)]
    [InlineData(1f,.1f)]
    [InlineData(2f,1f)]
    [InlineData(8f,4f)]
    public void ImpactEnergyBoundsStrokeAndIntegratedBurst(float speed,float mass)
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[-4,6.85f,0],Properties=new(){["mass"]=mass}});
            pump.OnContact(ball,speed,world);pump.BeforeNetworks(world);
            var stroke=speed<BellowsPart.MinimumImpactSpeed?0:BellowsPart.MaximumStroke*Mathf.Min(.5f*mass*speed*speed/12,1);
            var integral=0f;
            for(var i=0;i<600;i++){pump.BeforeStep(world,MachineWorld.Tick/4);integral+=pump.EmissionForce*MachineWorld.Tick/4;}
            Assert.Equal(stroke,pump.Compression,5);
            Assert.Equal(18*stroke/BellowsPart.CompressionSpeed,integral,3);
            Assert.Null(pump.AirflowSource);Assert.Empty(pump.ConnectionPorts);
        }
        finally{world.Free();}
    }
    [Fact]
    public void GentleRestingSideAndUndersideContactsDoNotPump()
    {
        var world=World();
        try
        {
            var pump=Pump(world);
            var body=world.AddPart(new(){Id="body",Kind="ball",Position=[-4,6.85f,0]});
            world.Start();for(var i=0;i<240;i++)world.Step();
            Assert.Equal(0,pump.StrokeCount);Assert.Equal(0,pump.Compression);
            foreach(var at in new[]{new Vector3(-2.9f,6.45f,0),new(-4,6.45f,1),new(-4,5.7f,0)})
            {
                body.Position=at;pump.OnContact(body,10,world);pump.BeforeNetworks(world);
                Assert.Equal(0,pump.StrokeCount);
            }
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameTickHitsCoalesceBeforeCommitRegardlessOfOrder(bool reversed)
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var body=world.AddPart(new(){Id="body",Kind="ball",Position=[-4,6.85f,0]});
            foreach(var speed in reversed?new[]{4f,2f}:new[]{2f,4f})pump.OnContact(body,speed,world);
            Assert.Equal(0,pump.StrokeCount);Assert.Null(pump.AirflowSource);
            pump.BeforeNetworks(world);
            for(var i=0;i<300;i++)pump.BeforeStep(world,MachineWorld.Tick/4);
            Assert.Equal(1,pump.StrokeCount);Assert.Equal(BellowsPart.MaximumStroke*8/12,pump.Compression,5);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(90)]
    [InlineData(150)]
    [InlineData(240)]
    public void ResetAtIntermediateStatesAndRenderCallsCannotChangePhysics(int steps)
    {
        var world=World();
        try
        {
            var pump=Pump(world);world.AddPart(new(){Id="body",Kind="ball",Position=[-4,9,0]});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();for(var i=0;i<steps;i++)world.Step();
            var compression=pump.Compression;var phase=pump.Phase;
            pump._Process(.5);Assert.Equal(compression,pump.Compression);Assert.Equal(phase,pump.Phase);
            world.Restore();Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            pump=(BellowsPart)world.FindPart("pump")!;
            Assert.Equal(0,pump.Compression);Assert.Equal(BellowsPhase.Ready,pump.Phase);Assert.Null(pump.AirflowSource);
        }
        finally{world.Free();}
    }
    [Fact]
    public void AirBurstRingsChimesAndWallSuppressesIt()
    {
        foreach(var blocked in new[]{false,true})
        {
            var world=World();
            try
            {
                Pump(world);world.AddPart(new(){Id="body",Kind="ball",Position=[-4,9,0]});
                var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[-1,6.48f,0]});
                if(blocked)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,5.88f,0],Rotation=[0,90,0]});
                world.Start();for(var i=0;i<480;i++)world.Step();
                Assert.Equal(!blocked,chimes.PulseCount>0);
            }
            finally{world.Free();}
        }
    }
    [Fact]
    public void NewLoadStopsRefillWithoutAirOrPenetration()
    {
        var world=World(0);
        try
        {
            var pump=Pump(world);
            var body=world.AddPart(new(){Id="body",Kind="ball",Position=[-4,6.85f,0]});
            pump.OnContact(body,8,world);pump.BeforeNetworks(world);
            for(var i=0;i<300;i++)pump.BeforeStep(world,MachineWorld.Tick/4);
            body.Position=new(-4,9,2);
            for(var i=0;i<100;i++)pump.BeforeStep(world,MachineWorld.Tick/4);
            Assert.Equal(BellowsPhase.Refilling,pump.Phase);
            var compression=pump.Compression;
            body.Position=new(-4,6+BellowsPart.RestHeight-compression+.06f+body.Radius,0);
            for(var i=0;i<100;i++)pump.BeforeStep(world,MachineWorld.Tick/4);
            Assert.Equal(BellowsPhase.Held,pump.Phase);Assert.Equal(compression,pump.Compression);
            Assert.Null(pump.AirflowSource);Assert.Equal(1,pump.StrokeCount);
            world.Start();world.Restore();
            Assert.Equal(0,((BellowsPart)world.FindPart("pump")!).Compression);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(BellowsParameters.Force,0f)]
    [InlineData(BellowsParameters.Force,41f)]
    [InlineData(BellowsParameters.Reach,-1f)]
    [InlineData(BellowsParameters.Width,5f)]
    public void InvalidParametersAreRejected(string key,float value)
    {
        var world=World();
        try {Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="bad",Kind="bellows",Properties=new(){[key]=value}}));}
        finally{world.Free();}
    }
}
