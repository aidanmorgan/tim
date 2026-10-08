using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SoundMeterTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0};godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false,false,true,false)]
    [InlineData(false,false,true,true)]
    [InlineData(true,false,true,false)]
    [InlineData(false,true,true,false)]
    [InlineData(false,false,false,false)]
    public void SpeakerCanTriggerOnlyThroughAnUnblockedForwardPathWithMeterSupply(bool wall,bool backwards,bool supply,bool reverse)
    {
        var world=World();
        try
        {
            var speaker=(SpeakerPart)world.AddPart(new(){Id=reverse?"z-speaker":"a-speaker",Kind="speaker",Position=[-3,6,0],Orientation = PartOrientation.FromEulerDegrees(0,backwards?180:0,0)});
            var meter=(SoundMeterPart)world.AddPart(new(){Id=reverse?"a-meter":"z-meter",Kind="sound_meter",Position=[1,6,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,2,3]});
            var load=world.AddPart(new(){Id="load",Kind="powered_gate",Position=[4,2,-3]});
            Assert.True(world.Connect(meter,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));
            var counter=(CounterPart)world.AddPart(new(){Id="counter",Kind="counter",Position=[3,2,3]});
            Assert.True(world.Connect(battery,speaker));
            if(supply)Assert.True(world.Connect(battery,meter));
            Assert.True(world.Connect(meter,SocketId.ActivationOut,counter,SocketId.ActivationIn,ConnectionDomain.Activation));
            if(wall)world.AddPart(new(){Id="wall",Kind="wall",Position=[-1,6,0]});
            world.Start();world.Activate(speaker);
            var maximum=0f;var supplied=false;
            for(var i=0;i<100;i++){world.Step();maximum=Math.Max(maximum,meter.Level);supplied|=load.HasElectricalPower(SocketId.PowerIn);}
            Assert.Equal(!wall&&!backwards&&supply,supplied);
            Assert.Equal(meter.TriggerCount,counter.Count);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(!wall&&!backwards&&supply?1:0,meter.TriggerCount);
            Assert.Equal(!wall&&!backwards,maximum>0);
            Assert.False(meter.AboveThreshold);
            Assert.False(meter.Active);
            world.Activate(speaker);
            for(var i=0;i<100;i++)world.Step();
            Assert.Equal(!wall&&!backwards&&supply?2:0,meter.TriggerCount);
            Assert.Equal(meter.TriggerCount,counter.Count);
            world.Restore();
            meter=(SoundMeterPart)world.FindPart(meter.Uid)!;
            Assert.Equal(0,meter.Level);Assert.Equal(0,meter.TriggerCount);Assert.False(meter.AboveThreshold);
        }
        finally{world.Free();}
    }
    [Fact]
    public void ThresholdHysteresisAndInvalidValuesAreExplicit()
    {
        var world=World();
        try
        {
            var meter=(SoundMeterPart)world.AddPart(new(){Id="meter",Kind="sound_meter"});
            meter.ReceiveAcousticLevel(.25f);Assert.True(meter.AboveThreshold);
            meter.ReceiveAcousticLevel(.23f);Assert.True(meter.AboveThreshold);
            meter.ReceiveAcousticLevel(.225f);Assert.False(meter.AboveThreshold);
            meter.ReceiveAcousticLevel(.24f);Assert.False(meter.AboveThreshold);
            Assert.Throws<ArgumentException>(()=>meter.ReceiveAcousticLevel(float.NaN));
            Assert.Throws<ArgumentException>(()=>meter.ReceiveAcousticLevel(1.1f));
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="invalid",Kind="sound_meter",Properties=new(){[PartParameterName.Of(ReceiverParameter.Threshold)]=0}}));
        }
        finally{world.Free();}
    }
    private partial class Source : MachinePart
    {
        public override IReadOnlyList<AcousticPulse> AcousticPulses =>
            [new(Vector3.Up,Vector3.Right,ToneBand.Mid,0,AcousticPattern.Cone,1)];
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousSourcesUseStrongestNotAdditiveLevelAndHiddenMetersClear(bool reverse)
    {
        var world=World();
        var a=new Source();var b=new Source();
        try
        {
            var meter=(SoundMeterPart)world.AddPart(new(){Id="meter",Kind="sound_meter"});
            FixtureParts.Attach(world,a,reverse?FixturePartId.Second:FixturePartId.First);
            FixtureParts.Attach(world,b,reverse?FixturePartId.First:FixturePartId.Second);
            AcousticNetwork.Solve(world);
            Assert.Equal(1,meter.Level);
            meter.Visible=false;AcousticNetwork.Solve(world);
            Assert.Equal(0,meter.Level);
        }
        finally{world.Free();}
    }
}
