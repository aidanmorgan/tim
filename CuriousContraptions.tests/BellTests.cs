using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BellTests(HeadlessFixture godot)
{
    private MachineWorld World(float gravity=0)
    {
        var world=new MachineWorld {Gravity=gravity,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(1,0,0)]
    [InlineData(-1,0,0)]
    [InlineData(0,1,0)]
    [InlineData(0,-1,0)]
    [InlineData(0,0,1)]
    [InlineData(0,0,-1)]
    public void RealImpactInEveryAxisRingsWithoutAddingEnergy(int x,int y,int z)
    {
        var world=World();
        try
        {
            var normal=new Vector3(x,y,z);var center=new Vector3(0,6,0);var start=center+normal*2;
            var bell=(BellPart)world.AddPart(new(){Id="bell",Kind="bell",Position=[0,6,0],Rotation=[30,40,20]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[start.X,start.Y,start.Z]});
            world.Start();ball.Velocity=-normal*4;
            for(var i=0;i<100&&bell.PulseCount==0;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);
            Assert.InRange(ball.Velocity.Dot(normal),4*ball.Bounce-.001f,4*ball.Bounce+.001f);
            Assert.True(ball.Position.DistanceTo(center)>=BellPart.CollisionRadius+ball.Radius);
            var pulse=Assert.Single(bell.AcousticPulses);
            Assert.Equal(AcousticPattern.Omnidirectional,pulse.Pattern);
            Assert.InRange(pulse.Strength,.666f,.667f);
            foreach(var axis in new[]{Vector3.Up,Vector3.Down,Vector3.Right,Vector3.Left,Vector3.Forward,Vector3.Back})
                Assert.InRange(pulse.Sample(center+axis*3,pulse.EmissionTick+31),.387f,.388f);
            Assert.Empty(bell.ConnectionPorts);Assert.False(bell.CanReceiveActivation);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false,true,true,false)]
    [InlineData(false,true,true,true)]
    [InlineData(true,true,true,false)]
    [InlineData(false,false,true,false)]
    [InlineData(false,true,false,false)]
    public void FallingBallDrivesSoundMeterTimerAndGateOnlyWithContactClearPathAndSupply(bool wall,bool ballPresent,bool power,bool reverse)
    {
        var world=World(9.81f);
        try
        {
            var bell=(BellPart)world.AddPart(new(){Id=reverse?"z-bell":"a-bell",Kind="bell",Position=[-3,6,0]});
            var meter=(SoundMeterPart)world.AddPart(new(){Id=reverse?"a-meter":"z-meter",Kind="sound_meter",Position=[1,6,0]});
            var timer=(HoldTimerPart)world.AddPart(new(){Id="timer",Kind="hold_timer",Position=[1,3,2]});
            var gate=world.AddPart(new(){Id="gate",Kind="powered_gate",Position=[4,3,0],Rotation=[0,0,90]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,2,3]});
            if(ballPresent)world.AddPart(new(){Id="ball",Kind="ball",Position=[-3,9,0]});
            if(wall)world.AddPart(new(){Id="wall",Kind="wall",Position=[-1,6,0]});
            if(power)Assert.True(world.Connect(battery,meter));
            Assert.True(world.Connect(battery,timer));
            Assert.True(world.Connect(meter,SocketId.ActivationOut,timer,SocketId.ActivationIn,ConnectionDomain.Activation));
            Assert.True(world.Connect(timer,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var received=0f;var opened=false;
            for(var i=0;i<240;i++){world.Step();received=Math.Max(received,meter.Level);opened|=gate.HasElectricalPower(SocketId.PowerIn);}
            Assert.Equal(ballPresent,bell.PulseCount>0);
            Assert.Equal(ballPresent&&!wall,received>0);
            Assert.Equal(ballPresent&&!wall&&power,opened);
            Assert.Equal(opened,meter.TriggerCount>0);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            bell=(BellPart)world.FindPart(bell.Uid)!;
            Assert.Empty(bell.AcousticPulses);Assert.Equal(0,bell.PulseCount);Assert.False(bell.Active);
            Assert.Equal(Vector3.Zero,bell.GetNode<Node3D>("Visual/BellBody").Rotation);
            world.LoadMachine(JsonSerializer.Deserialize(construction,MachineJson.Default.MachineData)!);
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(opened,((SoundMeterPart)world.FindPart(meter.Uid)!).TriggerCount>0);
        }
        finally{world.Free();}
    }
    [Fact]
    public void GentleRestingContactIsSilentAndSeparationRearmsLaterImpact()
    {
        var world=World(9.81f);
        try
        {
            var bell=(BellPart)world.AddPart(new(){Id="bell",Kind="bell",Position=[0,4,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,5.09f,0],Properties=new(){["bounce"]=0}});
            world.Start();
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(0,bell.PulseCount);
            ball.Position=new(0,7,0);ball.Velocity=Vector3.Zero;
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);Assert.Empty(bell.AcousticPulses);
            ball.Position=new(0,7,0);ball.Velocity=Vector3.Zero;
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(2,bell.PulseCount);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(.7f,1f,0)]
    [InlineData(2f,.1f,1)]
    [InlineData(2f,4f,1)]
    public void ImpactThresholdAndMassControlLoudness(float speed,float mass,int expected)
    {
        var world=World();
        try
        {
            var bell=(BellPart)world.AddPart(new(){Id="bell",Kind="bell",Position=[0,6,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,7.1f,0],Properties=new(){["mass"]=mass}});
            world.Start();ball.Velocity=Vector3.Down*speed;
            for(var i=0;i<10;i++)world.Step();
            Assert.Equal(expected,bell.PulseCount);
            if(expected>0)Assert.Equal(Mathf.Clamp(mass*speed/6,.05f,1),Assert.Single(bell.AcousticPulses).Strength,4);
        }
        finally{world.Free();}
    }
    [Fact]
    public void RapidRealStrikesAreBoundedAndPresentationNeverChangesPhysics()
    {
        var world=World();
        try
        {
            var bell=(BellPart)world.AddPart(new(){Id="bell",Kind="bell",Position=[0,6,0]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,8,0]});
            var audio=bell.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            var waveform=Assert.IsType<AudioStreamWav>(audio.Stream);
            Assert.Equal(30870,waveform.Data.Length);Assert.Contains(waveform.Data,b=>b!=0);
            world.Start();
            for(var i=0;i<240;i++)
            {
                // Alternate explicit separated/approaching states to stress actual solver contacts.
                ball.Position=new(0,i%2==0?8:7.1f,0);ball.Velocity=Vector3.Down*6;
                world.Step();Assert.InRange(bell.AcousticPulses.Count,0,5);
                bell._Process(MachineWorld.Tick);
            }
            Assert.InRange(bell.PulseCount,9,10);
            var signature=world.StateSignature();var shape=bell.Spheres.Single();
            world.Running=false;audio.Stop();
            var body=bell.GetNode<Node3D>("Visual/BellBody");
            var before=body.Rotation;
            bell._Process(0);Assert.Equal(before,body.Rotation);
            bell._Process(2);Assert.InRange(Math.Abs(body.Rotation.Z),0,.00001f);
            Assert.Equal(shape,bell.Spheres.Single());Assert.Equal(signature,world.StateSignature());
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousImpactsCoalesceToStrongestRegardlessOfOrder(bool reverse)
    {
        var world=World();
        try
        {
            var bell=(BellPart)world.AddPart(new(){Id="bell",Kind="bell",Position=[0,6,0]});
            var first=world.AddPart(new(){Id="first",Kind="ball",Position=[-1.1f,6,0],Properties=new(){["mass"]=reverse?1:.1f}});
            var second=world.AddPart(new(){Id="second",Kind="ball",Position=[1.1f,6,0],Properties=new(){["mass"]=reverse?.1f:1}});
            world.Start();first.Velocity=Vector3.Right*6;second.Velocity=Vector3.Left*6;
            world.Step();
            Assert.Equal(1,bell.PulseCount);Assert.Equal(1,Assert.Single(bell.AcousticPulses).Strength,4);
            var body=bell.GetNode<Node3D>("Visual/BellBody");
            Assert.Equal(Vector3.Zero,body.Rotation);
            bell._Process(.02);Assert.InRange(Math.Abs(body.Rotation.Z),.07f,.08f);
            var signature=world.StateSignature();
            bell._Process(1);
            Assert.Equal(signature,world.StateSignature());
        }
        finally{world.Free();}
    }
    [Fact]
    public void InvalidAcousticModesStrengthAndVoicesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AcousticPulse(Vector3.Zero,Vector3.Up,ToneBand.Mid,0,(AcousticPattern)99,1));
        foreach(var strength in new[]{0f,-1,1.1f,float.NaN,float.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AcousticPulse(Vector3.Zero,Vector3.Up,ToneBand.Mid,0,AcousticPattern.Omnidirectional,strength));
        Assert.Throws<ArgumentOutOfRangeException>(()=>AcousticAudio.Create(ToneBand.Mid,(AcousticVoice)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>AcousticAudio.Create((ToneBand)99,AcousticVoice.Bell));
        var pulse=new AcousticPulse(Vector3.Zero,Vector3.Up,ToneBand.Mid,0,AcousticPattern.Omnidirectional,1);
        Assert.Equal(0,pulse.Sample(Vector3.Right*9,91));
        Assert.Equal(0,pulse.Sample(Vector3.Left*3,pulse.ExpiresTick));
    }
}
