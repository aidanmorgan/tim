using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BellTests(NativeSceneFixture godot)
{
    private enum Role { Bell, Ball, Meter, Timer, Gate, Battery, Wall, First, Second }
    private enum PayloadParameter { Mass, Bounce }
    private const string BellVisualPath="Visual/BellBody";
    private static string Id(Role role)=>role switch
    {
        Role.Bell=>"bell",Role.Ball=>"ball",Role.Meter=>"meter",Role.Timer=>"timer",Role.Gate=>"gate",
        Role.Battery=>"battery",Role.Wall=>"wall",Role.First=>"first",Role.Second=>"second",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Bell=>"bell",Role.Ball or Role.First or Role.Second=>"ball",Role.Meter=>"sound_meter",
        Role.Timer=>"hold_timer",Role.Gate=>"powered_gate",Role.Battery=>"battery",Role.Wall=>"wall",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(Role.Bell),Kind=Kind(Role.Bell),Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(30,40,20)});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[start.X,start.Y,start.Z]});
            ball.InitialVelocity=-normal*4;world.Start();
            for(var i=0;i<100&&bell.PulseCount==0;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);
            Assert.InRange(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal)),4*ball.Bounce-.001f,4*ball.Bounce+.001f);
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(reverse?Role.Meter:Role.Bell),Kind=Kind(Role.Bell),Position=[-3,6,0]});
            var meter=(SoundMeterPart)world.AddPart(new(){Id=Id(reverse?Role.Bell:Role.Meter),Kind=Kind(Role.Meter),Position=[1,6,0]});
            var timer=(HoldTimerPart)world.AddPart(new(){Id=Id(Role.Timer),Kind=Kind(Role.Timer),Position=[1,3,2]});
            var gate=world.AddPart(new(){Id=Id(Role.Gate),Kind=Kind(Role.Gate),Position=[4,3,0],Orientation = PartOrientation.FromEulerDegrees(0,0,90)});
            var battery=world.AddPart(new(){Id=Id(Role.Battery),Kind=Kind(Role.Battery),Position=[-4,2,3]});
            if(ballPresent)world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[-3,9,0]});
            if(wall)world.AddPart(new(){Id=Id(Role.Wall),Kind=Kind(Role.Wall),Position=[-1,6,0]});
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
            Assert.Equal(Vector3.Zero,bell.GetNode<Node3D>(BellVisualPath).Rotation);
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(Role.Bell),Kind=Kind(Role.Bell),Position=[0,4,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,5.09f,0],Properties=new(){[PartParameterName.Of(PayloadParameter.Bounce)]=0}});
            world.Start();
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(0,bell.PulseCount);
            var payload=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            world.Physics.ApplyImpulse(payload.Id,new(0,6*ball.Mass,0),payload.Center);
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(1,bell.PulseCount);Assert.Empty(bell.AcousticPulses);
            world.Physics.ApplyImpulse(payload.Id,new(0,6*ball.Mass,0),payload.Center);
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(Role.Bell),Kind=Kind(Role.Bell),Position=[0,6,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,7.1f,0],Properties=new(){[PartParameterName.Of(PayloadParameter.Mass)]=mass}});
            ball.InitialVelocity=Vector3.Down*speed;world.Start();
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(Role.Bell),Kind=Kind(Role.Bell),Position=[0,6,0]});
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,8,0]});
            var audio=bell.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            var waveform=Assert.IsType<AudioStreamWav>(audio.Stream);
            Assert.Equal(30870,waveform.Data.Length);Assert.Contains(waveform.Data,b=>b!=0);
            ball.InitialVelocity=Vector3.Down*6;world.Start();
            for(var i=0;i<240;i++)
            {
                // Re-aim the actual rebounding body with an impulse, never a scene teleport.
                var payload=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
                if(payload.LinearVelocity.Y>0&&payload.Center.Y>6+BellPart.CollisionRadius+ball.Radius+.001)
                    world.Physics.ApplyImpulse(payload.Id,new(0,-(payload.LinearVelocity.Y+6)/payload.InverseMass,0),payload.Center);
                world.Step();Assert.InRange(bell.AcousticPulses.Count,0,5);
                world.PresentFrame(MachineWorld.Tick,1);
            }
            Assert.InRange(bell.PulseCount,9,10);
            var signature=world.StateSignature();var shape=bell.Spheres.Single();
            world.Running=false;audio.Stop();
            var body=bell.GetNode<Node3D>(BellVisualPath);
            var before=body.Rotation;
            world.PresentFrame(0,1);Assert.Equal(before,body.Rotation);
            world.PresentFrame(2,1);Assert.InRange(Math.Abs(body.Rotation.Z),0,.00001f);
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
            var bell=(BellPart)world.AddPart(new(){Id=Id(Role.Bell),Kind=Kind(Role.Bell),Position=[0,6,0]});
            var first=world.AddPart(new(){Id=Id(Role.First),Kind=Kind(Role.Ball),Position=[-1.1f,6,0],Properties=new(){[PartParameterName.Of(PayloadParameter.Mass)]=reverse?1:.1f}});
            var second=world.AddPart(new(){Id=Id(Role.Second),Kind=Kind(Role.Ball),Position=[1.1f,6,0],Properties=new(){[PartParameterName.Of(PayloadParameter.Mass)]=reverse?.1f:1}});
            first.InitialVelocity=Vector3.Right*6;second.InitialVelocity=Vector3.Left*6;
            world.Start();
            world.Step();
            Assert.Equal(1,bell.PulseCount);Assert.Equal(1,Assert.Single(bell.AcousticPulses).Strength,4);
            var body=bell.GetNode<Node3D>(BellVisualPath);
            Assert.Equal(Vector3.Zero,body.Rotation);
            world.PresentFrame(.02,1);Assert.InRange(Math.Abs(body.Rotation.Z),.07f,.08f);
            var signature=world.StateSignature();
            world.PresentFrame(1,1);
            Assert.Equal(signature,world.StateSignature());
        }
        finally{world.Free();}
    }
    [Fact]
    public void FixtureBoundaryNamesAreCanonicalAndUndefinedChoicesAreRejected()
    {
        Assert.Equal("bell",Kind(Role.Bell));Assert.Equal("sound_meter",Kind(Role.Meter));
        Assert.Equal("hold_timer",Kind(Role.Timer));Assert.Equal("powered_gate",Kind(Role.Gate));
        Assert.Equal("mass",PartParameterName.Of(PayloadParameter.Mass));
        Assert.Equal("bounce",PartParameterName.Of(PayloadParameter.Bounce));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((PayloadParameter)999));
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
