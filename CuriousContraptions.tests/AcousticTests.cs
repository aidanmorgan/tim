using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AcousticTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(ToneBand.Low)]
    [InlineData(ToneBand.Mid)]
    [InlineData(ToneBand.High)]
    public void PulseHasFiniteTravelConeDurationAndRange(ToneBand tone)
    {
        var pulse=new AcousticPulse(Vector3.Zero,Vector3.Right,tone,10,AcousticPattern.Cone,1);
        Assert.Equal(tone,pulse.Tone);
        Assert.Equal(0,pulse.Sample(new(3,0,0),39));
        Assert.InRange(pulse.Sample(new(3,0,0),41),.58f,.59f);
        Assert.Equal(0,pulse.Sample(new(-3,0,0),41));
        Assert.Equal(0,pulse.Sample(new(0,3,0),41));
        Assert.Equal(0,pulse.Sample(new(9,0,0),101));
        Assert.Equal(0,pulse.Sample(new(3,0,0),60));
        Assert.Equal(0,pulse.Sample(Vector3.Zero,pulse.ExpiresTick));
        var rotated=new AcousticPulse(new(2,4,1),Vector3.Up,tone,10,AcousticPattern.Cone,1);
        Assert.Equal(pulse.Sample(new(3,0,0),41),rotated.Sample(new(2,7,1),41));
    }
    [Fact]
    public void InvalidPulsesAndSamplesAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>new AcousticPulse(Vector3.Zero,Vector3.Zero,ToneBand.Mid,0,AcousticPattern.Cone,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AcousticPulse(Vector3.Zero,Vector3.Right,(ToneBand)99,0,AcousticPattern.Cone,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AcousticPulse(Vector3.Zero,Vector3.Right,ToneBand.Mid,-1,AcousticPattern.Cone,1));
        var pulse=new AcousticPulse(Vector3.Zero,Vector3.Right,ToneBand.Mid,0,AcousticPattern.Cone,1);
        Assert.Throws<ArgumentException>(()=>pulse.Sample(new(float.NaN,0,0),0));
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0};godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClockTriggerIsNextTickRegardlessOfPartOrderAndResetClearsSound(bool reverse)
    {
        var world=World();
        try
        {
            var speaker=(SpeakerPart)world.AddPart(new(){Id=reverse?"a-speaker":"z-speaker",Kind="speaker",Position=[0,4,0]});
            var clock=(ClockPart)world.AddPart(new(){Id=reverse?"z-clock":"a-clock",Kind="clock",Position=[-3,4,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            Assert.True(world.Connect(battery,speaker));
            Assert.True(world.Connect(battery,clock));
            Assert.True(world.Connect(clock,speaker));
            world.Start();
            for(var i=0;i<=clock.IntervalTicks;i++)world.Step();
            Assert.Equal(1,clock.PulseCount);Assert.Equal(0,speaker.PulseCount);
            world.Step();
            Assert.Equal(1,speaker.PulseCount);
            var pulse=Assert.Single(speaker.AcousticPulses);
            Assert.Equal(clock.LastPulseTick+1,pulse.EmissionTick);
            Assert.Equal(speaker.Transform*SpeakerPart.Mouth,pulse.Origin);
            world.Restore();
            speaker=(SpeakerPart)world.FindPart(speaker.Uid)!;
            Assert.Empty(speaker.AcousticPulses);Assert.Equal(0,speaker.PulseCount);Assert.False(speaker.Active);
        }
        finally{world.Free();}
    }
    [Fact]
    public void UnpoweredRequestExpiresAndRetriggerStormIsBounded()
    {
        var world=World();
        try
        {
            var speaker=(SpeakerPart)world.AddPart(new(){Id="speaker",Kind="speaker",Position=[0,4,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,4,0]});
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,speaker));
            world.Start();
            world.Activate(speaker);
            world.Step();world.Step();
            Assert.Equal(0,speaker.PulseCount);
            supply.SetAndSettle(SimulationLatchPhase.On);
            Assert.Equal(0,speaker.PulseCount);
            for(var tick=0;tick<240;tick++)
            {
                for(var duplicate=0;duplicate<4;duplicate++)world.Activate(speaker);
                world.Step();
                Assert.InRange(speaker.AcousticPulses.Count,0,5);
            }
            Assert.InRange(speaker.PulseCount,9,10);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            for(var tick=0;tick<120;tick++)world.Step();
            Assert.Empty(speaker.AcousticPulses);Assert.False(speaker.Active);
            Assert.Throws<ArgumentException>(()=>speaker.HandleActivation(world,ActivationCommand.Set));
        }
        finally{world.Free();}
    }
    [Fact]
    public void AudioPlaybackStateDoesNotDriveSimulation()
    {
        var world=World();
        try
        {
            var speaker=(SpeakerPart)world.AddPart(new(){Id="speaker",Kind="speaker"});
            var battery=world.AddPart(new(){Id="battery",Kind="battery"});
            Assert.True(world.Connect(battery,speaker));
            var player=speaker.GetChildren().OfType<AudioStreamPlayer3D>().Single();
            player.VolumeDb=-80;
            var waveform=Assert.IsType<AudioStreamWav>(player.Stream);
            Assert.Equal(22050,waveform.MixRate);
            Assert.Equal(AudioStreamWav.FormatEnum.Format16Bits,waveform.Format);
            Assert.Contains(waveform.Data,b=>b!=0);
            world.Start();world.Activate(speaker);world.Step();world.Step();
            player.Stop();
            Assert.Equal(1,speaker.PulseCount);
            Assert.Single(speaker.AcousticPulses);
            for(var i=0;i<4;i++)world.Step();
            Assert.True(speaker.Active);
        }
        finally{world.Free();}
    }
}
