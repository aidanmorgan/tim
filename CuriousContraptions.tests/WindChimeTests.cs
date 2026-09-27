using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WindChimeTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(9f,0f,false,true,false,true)]
    [InlineData(9f,0f,false,true,true,true)]
    [InlineData(.01f,0f,false,true,false,false)]
    [InlineData(0f,0f,false,true,false,false)]
    [InlineData(9f,2f,false,true,false,false)]
    [InlineData(9f,0f,true,true,false,false)]
    [InlineData(9f,0f,false,false,false,false)]
    public void AirMustMoveClapperIntoTubeBeforeSoundCanDriveMeter(float force,float depth,bool wall,bool fanOn,bool reverse,bool expected)
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id=reverse?"z-fan":"a-fan",Kind="fan",Position=[-4,5.4f,depth],Properties=new(){[FanParameters.Force]=force,[FanParameters.Powered]=fanOn?1:0}});
            var chimes=(WindChimesPart)world.AddPart(new(){Id=reverse?"a-chimes":"z-chimes",Kind="wind_chimes",Position=[-1,6,0]});
            var meter=(SoundMeterPart)world.AddPart(new(){Id="meter",Kind="sound_meter",Position=[2,6,0]});
            var counter=(CounterPart)world.AddPart(new(){Id="counter",Kind="counter",Position=[3,2,2]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,2,3]});
            Assert.True(world.Connect(battery,meter));
            Assert.True(world.Connect(meter,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            if(wall)world.AddPart(new(){Id="wall",Kind="wall",Position=[-2.5f,5.4f,0],Rotation=[0,90,0]});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            world.Step();Assert.Equal(0,chimes.PulseCount); // overlap is not a trigger
            var moved=0f;var maxSpeed=0f;
            for(var i=1;i<360;i++)
            {
                world.Step();
                moved=Math.Max(moved,(chimes.Motion.Offset-Vector3.Down*ChimePendulum.Length).Length());
                maxSpeed=Math.Max(maxSpeed,chimes.Motion.Velocity.Length());
                Assert.InRange(chimes.Motion.Offset.Length(),1.4999f,1.5001f);
                Assert.InRange(chimes.AcousticPulses.Count,0,5);
            }
            Assert.Equal(expected,chimes.PulseCount>0);
            Assert.Equal(expected,meter.TriggerCount>0);Assert.Equal(meter.TriggerCount,counter.Count);
            Assert.InRange(maxSpeed,0,20);
            if(force>0&&depth==0&&!wall&&fanOn)Assert.True(moved>0);
            if(expected)Assert.True(moved>.35f);
            var count=chimes.PulseCount;
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            chimes=(WindChimesPart)world.FindPart(chimes.Uid)!;
            Assert.Equal(0,chimes.PulseCount);Assert.Empty(chimes.AcousticPulses);Assert.Equal(Vector3.Zero,chimes.Motion.Velocity);
            Assert.Equal(Vector3.Down*ChimePendulum.Length,chimes.Motion.Offset);
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            world.Start();for(var i=0;i<360;i++)world.Step();
            Assert.Equal(count,((WindChimesPart)world.FindPart(chimes.Uid)!).PulseCount);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void RotatedFanAndChimeWorkThroughDepthWithoutRenderDrivenPhysics(float yaw)
    {
        var world=World();
        try
        {
            var basis=Basis.FromEuler(new(0,Mathf.DegToRad(yaw),0));
            var center=new Vector3(0,6,0);var fanPosition=center+basis*new Vector3(-3,-.6f,0);
            world.AddPart(new(){Id="fan",Kind="fan",Position=[fanPosition.X,fanPosition.Y,fanPosition.Z],Rotation=[0,yaw,0]});
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0],Rotation=[0,yaw,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            var offset=chimes.Motion.Offset;var velocity=chimes.Motion.Velocity;var count=chimes.PulseCount;
            chimes._Process(.016);chimes._Process(.2);
            Assert.Equal(offset,chimes.Motion.Offset);Assert.Equal(velocity,chimes.Motion.Velocity);Assert.Equal(count,chimes.PulseCount);
            Assert.NotEqual(Quaternion.Identity,chimes.GetNode<Node3D>("Visual/Pendulum").Quaternion);
            world.Restore();chimes=(WindChimesPart)world.FindPart("chimes")!;
            Assert.Equal(Vector3.Zero,chimes.Motion.Velocity);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(90,0,0)]
    [InlineData(0,0,90)]
    [InlineData(180,0,0)]
    public void EditingTiltKeepsTheAuthoredRestPoseUntilRunAndResetRestoresIt(float x,float y,float z)
    {
        var world=World();
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0]});
            chimes.RotationDegrees=new(x,y,z);
            chimes._Process(.1);
            Assert.Equal(Quaternion.Identity,chimes.GetNode<Node3D>("Visual/Pendulum").Quaternion);
            world.Start();world.Step();chimes._Process(.016);
            Assert.True(Math.Abs(chimes.GetNode<Node3D>("Visual/Pendulum").Quaternion.Dot(Quaternion.Identity))>.999f);
            world.Restore();chimes=(WindChimesPart)world.FindPart("chimes")!;
            chimes._Process(.1);Assert.Equal(Quaternion.Identity,chimes.GetNode<Node3D>("Visual/Pendulum").Quaternion);
        }
        finally{world.Free();}
    }
    [Fact]
    public void TurningOffAirAllowsCoastThenRestNotAnArtificialSoundLoop()
    {
        var world=World();
        try
        {
            var fan=world.AddPart(new(){Id="fan",Kind="fan",Position=[-3,5.4f,0]});
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            fan.Active=false;
            for(var i=0;i<2400;i++)world.Step();
            var count=chimes.PulseCount;
            Assert.Equal(Vector3.Zero,chimes.LastAirForce);
            Assert.InRange(chimes.Motion.Velocity.Length(),0,.001f);
            Assert.Empty(chimes.AcousticPulses);
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(count,chimes.PulseCount);
            fan.Active=true;
            for(var i=0;i<120;i++)world.Step();
            Assert.True(chimes.PulseCount>count);
        }
        finally{world.Free();}
    }
    [Fact]
    public void BallCanStrikeTubeButRestingOrMissingBallDoesNotRing()
    {
        var world=World();world.Gravity=0;world.Pressure=0;
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[-2,6,0]});
            world.Start();for(var i=0;i<60;i++)world.Step();Assert.Equal(0,chimes.PulseCount);
            ball.Velocity=Vector3.Right*5;
            for(var i=0;i<100;i++)world.Step();
            Assert.True(chimes.PulseCount>0);
            Assert.True(ball.Velocity.X<0); // ordinary tube collision, no special impulse
            world.Restore();chimes=(WindChimesPart)world.FindPart("chimes")!;
            ball=world.FindPart("ball")!;ball.Position=new(-2,6,2);
            world.Start();ball.Velocity=Vector3.Right*5;
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);
        }
        finally{world.Free();}
    }
    [Fact]
    public void SharedAirflowPushesBallsByMassAndTransparentShellsBlockAirNotLight()
    {
        var world=World();world.Gravity=0;
        try
        {
            var fan=world.AddPart(new(){Id="fan",Kind="fan",Position=[-3,4,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,4,0]});
            var sample=AirflowNetwork.Sample(world,fan,fan.AirflowSource!.Value,ball.Position,ball);
            Assert.Equal(Vector3.Right*9,sample);
            Assert.Equal(Vector3.Zero,AirflowNetwork.Sample(world,fan,fan.AirflowSource.Value,new(0,6,0),ball));
            world.Start();world.Step();Assert.InRange(ball.Velocity.X,.074f,.076f);
            var pipe=world.AddPart(new(){Id="pipe",Kind="pipe",Position=[0,4,0]});
            Assert.Equal(5,WorldGeometry.Trace(TraceMedium.Light,world,new(0,4,-2),Vector3.Back,5,fan,ball));
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,new(0,4,-2),Vector3.Back,5,fan,ball),1.1f,1.4f);
            Assert.Equal(10,WorldGeometry.Trace(TraceMedium.Air,world,new(-5,4,0),Vector3.Right,10,fan,ball));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Trace((TraceMedium)99,world,Vector3.Zero,Vector3.Right,2,fan));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData("pipe_bend_45")]
    [InlineData("pipe_bend_90")]
    [InlineData("funnel")]
    public void CurvedClearShellsBlockAirAcrossWallsButRemainOpticallyClear(string kind)
    {
        var world=World();
        try
        {
            var part=world.AddPart(new(){Id="shell",Kind=kind,Position=[0,5,0],Rotation=[20,35,10]});
            var center=part.Bends.Count>0?part.Bends[0].Pose*part.Bends[0].Centre(part.Bends[0].Sweep*.5f):Vector3.Zero;
            var origin=part.Transform*(center+Vector3.Back*2);
            var direction=part.Basis*Vector3.Forward;
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,origin,direction,4,null),.7f,1.4f);
            Assert.Equal(4,WorldGeometry.Trace(TraceMedium.Light,world,origin,direction,4,null));
        }
        finally{world.Free();}
    }
    [Fact]
    public void TheSameJetAcceleratesAHeavierBallLess()
    {
        var world=World();world.Gravity=0;
        try
        {
            world.AddPart(new(){Id="fan",Kind="fan",Position=[-3,4,0]});
            var light=world.AddPart(new(){Id="light",Kind="ball",Position=[0,4,0]});
            var heavy=world.AddPart(new(){Id="heavy",Kind="bowling",Position=[-1,4,.5f]});
            AirflowNetwork.Step(world,MachineWorld.Tick);
            Assert.Equal(9*MachineWorld.Tick/light.Mass,light.Velocity.X,5);
            Assert.Equal(9*MachineWorld.Tick/heavy.Mass,heavy.Velocity.X,5);
            Assert.True(heavy.Velocity.X<light.Velocity.X);
        }
        finally{world.Free();}
    }
    [Fact]
    public void TiltedPendulumStaysConstrainedInThreeDimensions()
    {
        var basis=Basis.FromEuler(new(.6f,.8f,.3f));var motion=new ChimePendulum(basis);
        var strikes=0;
        for(var i=0;i<4800;i++)
        {
            strikes+=motion.Step(basis,i<120?Vector3.Back*9:Vector3.Zero,9.81f,MachineWorld.Tick/4).Count;
            Assert.True(motion.Offset.IsFinite());Assert.True(motion.Velocity.IsFinite());
            Assert.InRange(motion.Offset.Length(),1.4999f,1.5001f);
        }
        Assert.True(strikes>0);
    }
    [Fact]
    public void OppositeFansCancelAndNoPressureMeansNoAirForce()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="left",Kind="fan",Position=[-3,5.4f,0]});
            world.AddPart(new(){Id="right",Kind="fan",Position=[3,5.4f,0],Rotation=[0,180,0]});
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0]});
            world.Start();for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);Assert.InRange(chimes.LastAirForce.Length(),0,.00001f);
            world.FindPart("right")!.Visible=false;world.Pressure=0;
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,chimes.PulseCount);Assert.Equal(Vector3.Zero,chimes.LastAirForce);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousBallStrikesChooseStrongestToneBeforeAudioPlayback(bool reverse)
    {
        var world=World();world.Gravity=0;world.Pressure=0;
        try
        {
            var chimes=(WindChimesPart)world.AddPart(new(){Id="chimes",Kind="wind_chimes",Position=[0,6,0]});
            var left=world.AddPart(new(){Id=reverse?"z-left":"a-left",Kind="ball",Position=[-.815f,6,0],Properties=new(){["mass"]=.1f}});
            var right=world.AddPart(new(){Id=reverse?"a-right":"z-right",Kind="ball",Position=[.815f,6,0]});
            world.Start();left.Velocity=Vector3.Right*6;right.Velocity=Vector3.Left*6;
            world.Step();
            var pulse=Assert.Single(chimes.AcousticPulses);Assert.Equal(1,chimes.PulseCount);
            Assert.Equal(ToneBand.Mid,pulse.Tone);Assert.Equal(1,pulse.Strength);
            world.Step();
            var audio=chimes.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            Assert.Equal(AcousticAudio.Create(ToneBand.Mid,AcousticVoice.Bell).Data,Assert.IsType<AudioStreamWav>(audio.Stream).Data);
            audio.Stop();
            Assert.Equal(1,chimes.PulseCount);Assert.Single(chimes.AcousticPulses);
        }
        finally{world.Free();}
    }
    [Fact]
    public void InvalidFanParametersAndPendulumInputsAreRejected()
    {
        var world=World();
        try
        {
            foreach(var pair in new[]{(FanParameters.Powered,.5f),(FanParameters.Force,-1f),(FanParameters.Reach,0f),(FanParameters.Width,float.NaN)})
                Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="invalid",Kind="fan",Properties=new(){[pair.Item1]=pair.Item2}}));
            var motion=new ChimePendulum(Basis.Identity);
            Assert.Throws<ArgumentException>(()=>motion.Step(Basis.Identity,Vector3.Zero,9.81f,0));
            Assert.Throws<ArgumentException>(()=>motion.Step(Basis.Identity,new(float.NaN,0,0),9.81f,.002f));
        }
        finally{world.Free();}
    }
}
