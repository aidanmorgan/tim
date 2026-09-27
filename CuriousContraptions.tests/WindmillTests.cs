using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WindmillTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false,false,0)]
    [InlineData(false,true,0)]
    [InlineData(true,false,0)]
    [InlineData(true,true,0)]
    [InlineData(false,false,1)]
    [InlineData(true,true,1)]
    [InlineData(false,true,2)]
    public void SignedWindDrivesConveyorsAndReversersInTheSameSubstep(bool backWind,bool reverseOrder,int reversers)
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id=reverseOrder?"z-fan":"a-fan",Kind="fan",Position=[backWind?2:-4,6,0],Rotation=[0,backWind?180:0,0]});
            var windmill=(WindmillPart)world.AddPart(new(){Id=reverseOrder?"a-windmill":"z-windmill",Kind="windmill",Position=[-1,6,0]});
            var first=(ConveyorPart)world.AddPart(new(){Id="first",Kind="conveyor",Position=[2,2,2]});
            var last=(ConveyorPart)world.AddPart(new(){Id="last",Kind="conveyor",Position=[2,2,-2]});
            Assert.True(world.Connect(windmill,first));
            MachinePart previous=first;
            for(var i=0;i<reversers;i++)
            {
                var gear=world.AddPart(new(){Id="reverse_"+i,Kind="reverse_transmission",Position=[-3,2,2-i*2]});
                Assert.True(world.Connect(previous,gear));previous=gear;
            }
            Assert.True(world.Connect(previous,last));
            if(reverseOrder)world.Connections.Reverse();
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();world.Step();
            var windSign=backWind?-1:1;var gearSign=reversers%2==0?1:-1;
            Assert.InRange(windmill.ShaftSpeed*windSign,.14f,.16f);
            Assert.Equal(windmill.ShaftSpeed,first.ShaftSpeed);
            Assert.Equal(windmill.ShaftSpeed*gearSign,last.ShaftSpeed);
            for(var i=1;i<240;i++)world.Step();
            Assert.Equal(6*windSign,windmill.ShaftSpeed,4);
            Assert.Equal(4*windSign*gearSign,last.SurfaceSpeed,4);
            Assert.Contains(new MachineEvent(MachineEventKind.Turned,windmill.Uid),world.Events.Keys);
            Assert.InRange(Math.Abs(Mathf.AngleDifference(windmill.ShaftAngle,windmill.GetNode<Node3D>("Visual/WindRotor").Rotation.X)),0,.00001f);
            Assert.InRange(Math.Abs(Mathf.AngleDifference(-windmill.ShaftAngle,windmill.GetNode<Node3D>("Visual/OutputPulley").Rotation.Z)),0,.00001f);
            var speed=windmill.ShaftSpeed;var angle=windmill.ShaftAngle;
            windmill._Process(.2);Assert.Equal(speed,windmill.ShaftSpeed);Assert.Equal(angle,windmill.ShaftAngle);
            fan.Active=false;world.Step();
            Assert.InRange(windmill.ShaftSpeed*windSign,5.8f,5.9f); // coast, no instant stop
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,windmill.ShaftSpeed);Assert.Equal(0,last.SurfaceSpeed);Assert.False(windmill.Active);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            windmill=(WindmillPart)world.FindPart(windmill.Uid)!;
            Assert.Equal(0,windmill.ShaftSpeed);Assert.Equal(0,windmill.ShaftAngle);Assert.Equal(0,windmill.ShaftTravel);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6*windSign,((WindmillPart)world.FindPart(windmill.Uid)!).ShaftSpeed,4);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,0f,90f)]
    [InlineData(30f,45f,20f)]
    public void WholeAssemblyCanTurnInAllThreeDimensions(float x,float y,float z)
    {
        var world=World();
        try
        {
            var rotation=new Vector3(x,y,z);var basis=Basis.FromEuler(rotation*Mathf.Pi/180);
            var center=new Vector3(0,6,0);var fanAt=center-basis.X*3;
            world.AddPart(new(){Id="fan",Kind="fan",Position=[fanAt.X,fanAt.Y,fanAt.Z],Rotation=[x,y,z]});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[0,6,0],Rotation=[x,y,z]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(9,windmill.AxialForce,3);Assert.Equal(6,windmill.ShaftSpeed,3);
            Assert.Single(windmill.ConnectionPorts);
            Assert.Equal(ConnectionDomain.Mechanical,windmill.ConnectionPorts.Single().Domain);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0,6f)]
    [InlineData(1,4.5f)]
    [InlineData(2,0f)]
    public void RotorAreaSamplesDistinguishPartialAndFullOcclusion(int obstruction,float expectedSpeed)
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0]});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0]});
            if(obstruction>0)
            {
                var wall=(WallPart)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,obstruction==1?6.5f:6,0],Rotation=[0,90,0]});
                if(obstruction==1)wall.SetDimensions(new(3,.4f,.25f)); // top sample only
            }
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(expectedSpeed,windmill.ShaftSpeed,3);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(true,false,false,true)]
    [InlineData(false,false,false,false)]
    [InlineData(true,true,false,false)]
    [InlineData(true,false,true,false)]
    public void BallTransportRequiresWindAndARealMechanicalBelt(bool connected,bool blocked,bool off,bool expected)
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0],Properties=new(){[FanParameters.Powered]=off?0:1}});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0]});
            var belt=(ConveyorPart)world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,3,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[1,4.5f,0]});
            if(connected)Assert.True(world.Connect(windmill,belt));
            if(blocked)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,6,0],Rotation=[0,90,0]});
            world.Start();for(var i=0;i<180;i++)world.Step();
            Assert.Equal(expected,world.Events.ContainsKey(new(MachineEventKind.Transported,belt.Uid,ball.Uid)));
            if(expected)Assert.True(ball.Position.X>2.5f);
            else Assert.Equal(1,ball.Position.X,3);
            // Ball starts below the jet and only falls farther away: fan cannot move it directly.
            Assert.True(ball.Position.Y<5);
        }
        finally{world.Free();}
    }
    [Fact]
    public void EdgeOnOpposingAndWeakAirDoNotCreateDriveAndReversalCrossesZeroSmoothly()
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id="front",Kind="fan",Position=[-4,6,0]});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0],Rotation=[0,90,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();Assert.Equal(0,windmill.ShaftSpeed);
            windmill.RotationDegrees=Vector3.Zero;fan.Properties[FanParameters.Force]=.01f;
            for(var i=0;i<120;i++)world.Step();Assert.Equal(0,windmill.ShaftSpeed);
            fan.Properties[FanParameters.Force]=9;
            var back=world.AddPart(new(){Id="back",Kind="fan",Position=[2,6,0],Rotation=[0,180,0]});
            for(var i=0;i<120;i++)world.Step();Assert.Equal(0,windmill.ShaftSpeed);
            back.Visible=false;
            for(var i=0;i<120;i++)world.Step();Assert.Equal(6,windmill.ShaftSpeed,4);
            fan.Visible=false;back.Visible=true;
            var previous=windmill.ShaftSpeed;var crossed=false;
            for(var i=0;i<120;i++)
            {
                world.Step();Assert.InRange(Math.Abs(windmill.ShaftSpeed-previous),0,.151f);
                crossed|=previous>=0&&windmill.ShaftSpeed<0;previous=windmill.ShaftSpeed;
            }
            Assert.True(crossed);Assert.Equal(-6,windmill.ShaftSpeed,4);
        }
        finally{world.Free();}
    }
    [Fact]
    public void OutputIsBoundedAndCompetingDrivesAreRejected()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0],Properties=new(){[FanParameters.Force]=40}});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0],Properties=new(){[WindmillParameters.RadiansPerForce]=4}});
            var belt=world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,2,0]});
            var motor=world.AddPart(new(){Id="motor",Kind="motor",Position=[-4,2,2]});
            Assert.True(world.Connect(windmill,belt));Assert.False(world.Connect(motor,belt));
            Assert.False(world.Connect(belt,windmill));
            world.Start();for(var i=0;i<240;i++)world.Step();
            Assert.Equal(WindmillPart.MaximumSpeed,windmill.ShaftSpeed);
            foreach(var value in new[]{0f,5f,float.NaN})
                Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="invalid",Kind="windmill",Properties=new(){[WindmillParameters.RadiansPerForce]=value}}));
        }
        finally{world.Free();}
    }
    private partial class InvalidSamples : MachinePart
    {
        public override IReadOnlyList<AirflowSample> AirflowSamples=>[new(Vector3.Zero,2)];
    }
    [Fact]
    public void InvalidSampleWeightsFailBeforeAnyReceiverChanges()
    {
        var world=World();var invalid=new InvalidSamples();
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-4,6,0]});
            var windmill=(WindmillPart)world.AddPart(new(){Id="windmill",Kind="windmill",Position=[-1,6,0]});
            world.Parts.Add(invalid);
            Assert.Throws<ArgumentException>(()=>AirflowNetwork.Step(world,MachineWorld.Tick/4));
            Assert.Equal(0,windmill.ShaftSpeed);
        }
        finally{world.Parts.Remove(invalid);invalid.Free();world.Free();}
    }
}
