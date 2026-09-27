using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ClutchTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(true,true,false,false)]
    [InlineData(true,true,true,false)]
    [InlineData(true,true,false,true)]
    [InlineData(false,true,false,false)]
    [InlineData(true,false,false,false)]
    public void CoilAndMechanicalInputAreBothRequiredAndSignedDrivePropagates(bool coil,bool motorPower,bool reversed,bool reverseOrder)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,2,2]});
            var motor=(MotorPart)world.AddPart(new(){Id=reverseOrder?"z-motor":"a-motor",Kind="motor",Position=[-4,5,0]});
            var clutch=(ClutchPart)world.AddPart(new(){Id=reverseOrder?"a-clutch":"z-clutch",Kind="clutch",Position=[-1,5,0]});
            var belt=(ConveyorPart)world.AddPart(new(){Id="belt",Kind="conveyor",Position=[2,3,0]});
            var cargo=world.AddPart(new(){Id="cargo",Kind="ball",Position=[2,4.5f,0]});
            if(motorPower)Assert.True(world.Connect(battery,motor));
            if(coil)Assert.True(world.Connect(battery,clutch));
            MachinePart source=motor;
            if(reversed)
            {
                source=world.AddPart(new(){Id="reverse",Kind="reverse_transmission",Position=[-4,3,-2]});
                Assert.True(world.Connect(motor,source));
            }
            Assert.True(world.Connect(source,clutch));Assert.True(world.Connect(clutch,belt));
            if(reverseOrder)world.Connections.Reverse();
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();world.Step();
            Assert.Equal(0,clutch.OutputSpeed); // plates not yet closed
            for(var i=1;i<120;i++)world.Step();
            var sign=reversed?-1:1;
            Assert.Equal(motorPower?6*sign:0,clutch.InputSpeed,4);
            Assert.Equal(coil&&motorPower?6*sign:0,clutch.OutputSpeed,4);
            Assert.Equal(clutch.OutputSpeed,belt.ShaftSpeed);
            Assert.Equal(coil?ClutchPhase.Engaged:ClutchPhase.Open,clutch.Phase);
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(coil&&motorPower,world.Events.ContainsKey(new(MachineEventKind.Transported,belt.Uid,cargo.Uid)));
            if(coil&&motorPower)Assert.True((cargo.Position.X-2)*sign>1);
            else Assert.Equal(2,cargo.Position.X,3);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            clutch=(ClutchPart)world.FindPart(clutch.Uid)!;
            Assert.Equal(ClutchPhase.Open,clutch.Phase);Assert.Equal(0,clutch.Closure);Assert.Equal(0,clutch.InputAngle);Assert.Equal(0,clutch.OutputAngle);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(coil&&motorPower?6*sign:0,((ClutchPart)world.FindPart(clutch.Uid)!).OutputSpeed,4);
        }
        finally{world.Free();}
    }
    [Fact]
    public void PowerLossDisconnectsImmediatelyWhileInputKeepsTurningAndReengagementWaitsForClosure()
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var motor=(MotorPart)world.AddPart(new(){Id="motor",Kind="motor"});
            var clutch=(ClutchPart)world.AddPart(new(){Id="clutch",Kind="clutch"});
            var belt=(ConveyorPart)world.AddPart(new(){Id="belt",Kind="conveyor"});
            var coilSwitch=world.AddPart(new(){Id="coil-switch",Kind="switch"});
            Assert.True(world.Connect(battery,motor));Assert.True(world.Connect(battery,coilSwitch));Assert.True(world.Connect(coilSwitch,clutch));
            Assert.True(world.Connect(motor,clutch));Assert.True(world.Connect(clutch,belt));
            world.Start();coilSwitch.Active=true;for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6,clutch.OutputSpeed);
            var outputAngle=clutch.OutputAngle;var inputAngle=clutch.InputAngle;
            coilSwitch.Active=false;
            world.Step();
            Assert.Equal(ClutchPhase.Opening,clutch.Phase);
            Assert.Equal(0,clutch.OutputSpeed);Assert.Equal(0,belt.ShaftSpeed);Assert.Equal(6,clutch.InputSpeed);
            Assert.Equal(outputAngle,clutch.OutputAngle);Assert.NotEqual(inputAngle,clutch.InputAngle);
            var closure=clutch.Closure;clutch._Process(.5);Assert.Equal(closure,clutch.Closure);
            for(var i=0;i<60;i++)world.Step();
            Assert.Equal(ClutchPhase.Open,clutch.Phase);Assert.Equal(0,clutch.Closure);
            coilSwitch.Active=true;
            world.Step();Assert.Equal(ClutchPhase.Closing,clutch.Phase);Assert.Equal(0,clutch.OutputSpeed);
            for(var i=0;i<60;i++)world.Step();Assert.Equal(6,clutch.OutputSpeed);
            Assert.InRange(Math.Abs(Mathf.AngleDifference(-clutch.OutputAngle,clutch.GetNode<Node3D>("Visual/OutputPulley").Rotation.Z)),0,.00001f);
            Assert.Equal(-.09f,clutch.GetNode<Node3D>("Visual/InputPlate").Position.X,4);
            Assert.Equal(.09f,clutch.GetNode<Node3D>("Visual/OutputPlate").Position.X,4);
        }
        finally{world.Free();}
    }
    [Fact]
    public void OpenClutchCannotHideLoopsOrMultipleDrives()
    {
        var world=World();
        try
        {
            var a=world.AddPart(new(){Id="a",Kind="clutch"});
            var b=world.AddPart(new(){Id="b",Kind="conveyor"});
            var c=world.AddPart(new(){Id="c",Kind="motor"});
            Assert.True(world.Connect(a,b));
            Assert.False(world.Connect(b,a)); // even though a is not supplied
            Assert.False(world.Connect(c,b));
            Assert.True(world.Connect(c,a));
            Assert.Throws<ArgumentException>(()=>MechanicalNetwork.Validate(world.Parts,[
                new(){From="a",To="b",Type=ConnectionDomain.Mechanical,FromPort=SocketId.Drive,ToPort=SocketId.DriveIn},
                new(){From="b",To="a",Type=ConnectionDomain.Mechanical,FromPort=SocketId.Drive,ToPort=SocketId.DriveIn}
            ]));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(90f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(0f,0f,90f)]
    public void OrientationDoesNotChangeTypedPortsOrDrive(float x,float y,float z)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var motor=world.AddPart(new(){Id="motor",Kind="motor"});
            var clutch=(ClutchPart)world.AddPart(new(){Id="clutch",Kind="clutch",Rotation=[x,y,z]});
            Assert.True(world.Connect(battery,motor));Assert.True(world.Connect(battery,clutch));Assert.True(world.Connect(motor,clutch));
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6,clutch.OutputSpeed);Assert.Collection(clutch.ConnectionPorts,
                p=>Assert.Equal(ConnectionDomain.Mechanical,p.Domain),p=>Assert.Equal(ConnectionDomain.Mechanical,p.Domain),p=>Assert.Equal(ConnectionDomain.Electrical,p.Domain));
        }
        finally{world.Free();}
    }
    [Fact]
    public void TimerReleasesClutchWithoutSwitchingOffMotor()
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var motor=(MotorPart)world.AddPart(new(){Id="motor",Kind="motor"});
            var timer=(HoldTimerPart)world.AddPart(new(){Id="timer",Kind="hold_timer"});
            var clutch=(ClutchPart)world.AddPart(new(){Id="clutch",Kind="clutch"});
            Assert.True(world.Connect(battery,motor));Assert.True(world.Connect(battery,timer));Assert.True(world.Connect(timer,clutch));
            Assert.True(world.Connect(motor,clutch));
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6,motor.ShaftSpeed);Assert.Equal(0,clutch.OutputSpeed);
            world.Activate(timer);for(var i=0;i<60;i++)world.Step();
            Assert.Equal(6,clutch.OutputSpeed);
            for(var i=0;i<300;i++)world.Step();
            Assert.Equal(6,motor.ShaftSpeed);Assert.Equal(0,clutch.OutputSpeed);Assert.Equal(ClutchPhase.Open,clutch.Phase);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(.05f)]
    [InlineData(.2f)]
    [InlineData(2f)]
    public void ClosingDurationAndResetAreBounded(float seconds)
    {
        var world=World();
        try
        {
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            var clutch=(ClutchPart)world.AddPart(new(){Id="clutch",Kind="clutch",Properties=new(){[ClutchParameters.CloseSeconds]=seconds}});
            Assert.True(world.Connect(battery,clutch));world.Start();
            var previous=0f;var step=0;
            while(clutch.Phase!=ClutchPhase.Engaged&&step<300)
            {
                world.Step();step++;Assert.InRange(clutch.Closure-previous,0,MachineWorld.Tick/seconds+.00001f);previous=clutch.Closure;
            }
            Assert.InRange(step*MachineWorld.Tick,seconds-MachineWorld.Tick,seconds+MachineWorld.Tick*2);
            world.Restore();clutch=(ClutchPart)world.FindPart("clutch")!;
            Assert.Equal(ClutchPhase.Open,clutch.Phase);Assert.Equal(0,clutch.Closure);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(3f)]
    [InlineData(float.NaN)]
    public void InvalidClosingTimeRejected(float value)
    {
        var world=World();
        try{Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="bad",Kind="clutch",Properties=new(){[ClutchParameters.CloseSeconds]=value}}));}
        finally{world.Free();}
    }
}
