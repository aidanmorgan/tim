using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ClutchTests(NativeSceneFixture godot)
{
    private enum Fixture { Battery, Motor, Clutch, Conveyor, Switch, Timer }
    private readonly record struct CatalogueId(string Value);
    private static CatalogueId Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>new("battery"),Fixture.Motor=>new("motor"),Fixture.Clutch=>new("clutch"),
        Fixture.Conveyor=>new("conveyor"),Fixture.Switch=>new("switch"),Fixture.Timer=>new("hold_timer"),
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static PartSpec Spec(Fixture fixture,Vector3 position,PartOrientation? orientation=null)
    {
        var kind=Kind(fixture);
        return new(){Id=kind.Value,Kind=kind.Value,Position=[position.X,position.Y,position.Z],
            Orientation=orientation??PartOrientation.Identity};
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 position)=>
        world.AddPart(Spec(fixture,position));
    private static PhysicsTransmissionJoint Coupling(MachineWorld world,ClutchPart clutch)=>
        (PhysicsTransmissionJoint)world.CurrentJoint(new(clutch,ClutchPart.CouplingJoint));
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-5);
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
            if(reverseOrder)
            {
                var links=world.Connections.ToArray();
                foreach(var link in links)Assert.True(world.Disconnect(link));
                foreach(var link in links.Reverse())
                    Assert.True(world.Connect(world.FindPart(link.From)!,link.FromPort!.Value,
                        world.FindPart(link.To)!,link.ToPort!.Value,link.Type));
            }
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
    public void PowerLossOpensConstraintWithoutErasingMomentumAndReengagementWaitsForClosure()
    {
        var world=World();
        try
        {
            var battery=Add(world,Fixture.Battery,new(-6,6,0));
            var motor=(MotorPart)Add(world,Fixture.Motor,new(-3,6,0));
            var clutch=(ClutchPart)Add(world,Fixture.Clutch,new(0,6,0));
            var belt=(ConveyorPart)Add(world,Fixture.Conveyor,new(4,6,0));
            var coilSwitch=Add(world,Fixture.Switch,new(-6,3,0));
            Assert.True(world.Connect(battery,motor));Assert.True(world.Connect(battery,coilSwitch));
            Assert.True(world.Connect(coilSwitch,clutch));Assert.True(world.Connect(motor,clutch));
            Assert.True(world.Connect(clutch,belt));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();coilSwitch.Active=true;
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(TransmissionEngagement.Engaged,Coupling(world,clutch).Engagement);
            Assert.True(clutch.OutputSpeed>0);Near(clutch.InputSpeed,clutch.OutputSpeed);
            var outputSpeed=clutch.OutputSpeed;
            coilSwitch.Active=false;world.Step();
            Assert.Equal(ClutchPhase.Opening,clutch.Phase);
            Assert.Equal(TransmissionEngagement.Open,Coupling(world,clutch).Engagement);
            Near(outputSpeed,clutch.OutputSpeed);Near(clutch.OutputSpeed,belt.ShaftSpeed);
            // Load only the disconnected output shaft. No copied input speed may overwrite it.
            var output=((PhysicsFrameJoint)world.CurrentJoint(new(clutch,ClutchPart.OutputJoint))).A;
            world.Physics.ApplyImpulse(output.Id,output.Pose.Rotation.Apply(new(0,.01,0)),
                output.Center+output.Pose.Rotation.Apply(new(1,0,0)));
            var inputSpeed=clutch.InputSpeed;
            world.Step();
            Near(inputSpeed,clutch.InputSpeed);
            Assert.True(Math.Abs(clutch.OutputSpeed-inputSpeed)>.01);
            Near(clutch.OutputSpeed,belt.ShaftSpeed);
            var freeSpeed=clutch.OutputSpeed;
            var closure=clutch.Closure;clutch._Process(.5);Assert.Equal(closure,clutch.Closure);
            for(var i=0;i<60;i++)world.Step();
            Assert.Equal(ClutchPhase.Open,clutch.Phase);Assert.Equal(0,clutch.Closure);
            Near(freeSpeed,clutch.OutputSpeed);
            coilSwitch.Active=true;world.Step();
            Assert.Equal(ClutchPhase.Closing,clutch.Phase);
            Assert.Equal(TransmissionEngagement.Open,Coupling(world,clutch).Engagement);
            Near(freeSpeed,clutch.OutputSpeed);
            for(var i=0;i<60;i++)world.Step();
            Assert.Equal(TransmissionEngagement.Engaged,Coupling(world,clutch).Engagement);
            Near(clutch.InputSpeed,clutch.OutputSpeed);Near(clutch.OutputSpeed,belt.ShaftSpeed);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }
    [Fact]
    public void OpenClutchLoopUsesSharedConstraintsButCannotAcceptCompetingInputBelts()
    {
        var world=World();
        try
        {
            var clutch=(ClutchPart)Add(world,Fixture.Clutch,new(0,6,0));
            var belt=Add(world,Fixture.Conveyor,new(4,6,0));
            var motor=Add(world,Fixture.Motor,new(-4,6,0));
            Assert.True(world.Connect(clutch,belt));
            Assert.True(world.Connect(belt,clutch));
            Assert.False(world.Connect(motor,belt));
            Assert.False(world.Connect(motor,clutch));
            MechanicalNetwork.Validate(world.Parts,world.Connections);
            world.Start();world.Step();
            Assert.Equal(TransmissionEngagement.Open,Coupling(world,clutch).Engagement);
            Near(0,clutch.InputSpeed);Near(0,clutch.OutputSpeed);
            var duplicate=world.Snapshot();
            duplicate.Connections.Add(new(){From=motor.Uid,To=clutch.Uid,Type=ConnectionDomain.Mechanical,
                FromPort=SocketId.Drive,ToPort=SocketId.DriveIn});
            Assert.Throws<ArgumentException>(()=>world.ValidateMachine(duplicate));
            world.Restore();Assert.Equal(2,world.Connections.Count);
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
            var battery=Add(world,Fixture.Battery,new(-6,6,0));
            var motor=Add(world,Fixture.Motor,new(-3,6,0));
            var clutch=(ClutchPart)world.AddPart(Spec(Fixture.Clutch,new(0,6,0),PartOrientation.FromEulerDegrees(x,y,z)));
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
            var battery=Add(world,Fixture.Battery,new(-6,6,0));
            var motor=(MotorPart)Add(world,Fixture.Motor,new(-3,6,0));
            var timer=(HoldTimerPart)Add(world,Fixture.Timer,new(-6,3,0));
            var clutch=(ClutchPart)Add(world,Fixture.Clutch,new(0,6,0));
            Assert.True(world.Connect(battery,motor));Assert.True(world.Connect(battery,timer));Assert.True(world.Connect(timer,clutch));
            Assert.True(world.Connect(motor,clutch));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(6,motor.ShaftSpeed);Assert.Equal(0,clutch.OutputSpeed);
            world.Activate(timer);for(var i=0;i<60;i++)world.Step();
            Assert.Equal(6,clutch.OutputSpeed);
            for(var i=0;i<300;i++)world.Step();
            Assert.Equal(6,motor.ShaftSpeed);Near(6,clutch.OutputSpeed);Assert.Equal(ClutchPhase.Open,clutch.Phase);
            Assert.Equal(TransmissionEngagement.Open,Coupling(world,clutch).Engagement);
            // An open coupling releases drive; it does not erase output momentum.
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
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
            var battery=Add(world,Fixture.Battery,new(-6,6,0));
            var spec=Spec(Fixture.Clutch,new(0,6,0));
            spec.Properties[PartParameterName.Of(ClutchParameter.CloseSeconds)]=seconds;
            var clutch=(ClutchPart)world.AddPart(spec);
            var clutchId=clutch.Uid;
            Assert.True(world.Connect(battery,clutch));world.Start();
            var previous=0f;var step=0;
            while(clutch.Phase!=ClutchPhase.Engaged&&step<300)
            {
                world.Step();step++;Assert.InRange(clutch.Closure-previous,0,MachineWorld.Tick/seconds+.00001f);previous=clutch.Closure;
            }
            Assert.InRange(step*MachineWorld.Tick,seconds-MachineWorld.Tick,seconds+MachineWorld.Tick*2);
            world.Restore();clutch=(ClutchPart)world.FindPart(clutchId)!;
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
        try{Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="bad",Kind="clutch",Properties=new(){[PartParameterName.Of(ClutchParameter.CloseSeconds)]=value}}));}
        finally{world.Free();}
    }
}
