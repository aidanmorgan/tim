using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BeamSplitterTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public void BothSidesSplitEqualPowerWithIndependentOcclusion(bool back,bool block)
    {
        var world=World();
        try
        {
            var splitter=world.AddPart(new(){Id="splitter",Kind="beam_splitter",Position=[0,6,0],Rotation=[0,0,45]});
            var direction=back?Vector3.Left:Vector3.Right;
            var reflection=back?Vector3.Up:Vector3.Down;
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Rotation=back?[0,180,0]:[0,0,0]});
            laser.Position=splitter.Position-direction*3;
            var straight=(LightReceiverPart)world.AddPart(new(){Id="straight",Kind="light_receiver",Rotation=back?[0,180,0]:[0,0,0]});
            straight.Position=splitter.Position+direction*3;
            var reflected=(LightReceiverPart)world.AddPart(new(){Id="reflected",Kind="light_receiver",Rotation=back?[0,0,90]:[0,0,-90]});
            reflected.Position=splitter.Position+reflection*3;
            if(block)
            {
                var wall=world.AddPart(new(){Id="wall",Kind="wall"});
                wall.Position=splitter.Position+reflection*1.5f;
            }
            var trace=OpticalNetwork.Trace(world,laser,laser.OpticalPreviewSource!.Value);
            Assert.Equal(block?1:2,trace.Receptions.Count);
            Assert.Equal(3,trace.Segments.Count);
            foreach(var delivery in trace.Receptions)
                Assert.True(delivery.Power.DistanceTo(LaserPart.BeamPower*.5f)<.00001f);
            Assert.Contains(trace.Receptions,r=>r.Receiver==straight);
            Assert.Equal(!block,trace.Receptions.Any(r=>r.Receiver==reflected));
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(straight.Active);Assert.Equal(!block,reflected.Active);
            Assert.Equal(.25f,straight.Threshold);
            world.Restore();
            Assert.Empty(((LaserPart)world.FindPart("laser")!).BeamPath);
            Assert.False(world.FindPart("straight")!.Active);
        }
        finally{world.Free();}
    }
    [Fact]
    public void CascadedSplittersDoNotDuplicatePowerAndQuarterPowerFailsDefaultReceiver()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="first",Kind="beam_splitter",Position=[0,6,0],Rotation=[0,0,45]});
            world.AddPart(new(){Id="second",Kind="beam_splitter",Position=[3,6,0],Rotation=[0,0,45]});
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            var half=(LightReceiverPart)world.AddPart(new(){Id="half",Kind="light_receiver",Position=[0,3,0],Rotation=[0,0,-90]});
            var quarter=(LightReceiverPart)world.AddPart(new(){Id="quarter",Kind="light_receiver",Position=[3,3,0],Rotation=[0,0,-90]});
            var end=(LightReceiverPart)world.AddPart(new(){Id="end",Kind="light_receiver",Position=[6,6,0]});
            var trace=OpticalNetwork.Trace(world,laser,laser.OpticalPreviewSource!.Value);
            Assert.Equal(3,trace.Receptions.Count);
            var total=trace.Receptions.Aggregate(Vector3.Zero,(sum,r)=>sum+r.Power);
            Assert.True(total.DistanceTo(LaserPart.BeamPower)<.00001f);
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(half.Active);Assert.False(quarter.Active);Assert.False(end.Active);
            Assert.True(quarter.Power>0);
        }
        finally{world.Free();}
    }
    [Fact]
    public void GlassStopsBallsButTransmitsLightAndFrameRemainsOpaque()
    {
        var world=World();
        try
        {
            var splitter=world.AddPart(new(){Id="splitter",Kind="beam_splitter",Position=[0,5,0]});
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,5,0]});
            Assert.Equal(8,WorldGeometry.Trace(TraceMedium.Light,world,new(-2,5,0),Vector3.Right,8,laser));
            Assert.True(WorldGeometry.Trace(TraceMedium.Light,world,new(-2,5.74f,0),Vector3.Right,8,laser)<2);
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[-1,5,0]});
            world.Start();ball.Velocity=Vector3.Right*4;
            for(var i=0;i<50;i++)
            {
                world.Step();
                Assert.True(ball.Position.X<0);
            }
            Assert.True(ball.Velocity.X<0);
        }
        finally{world.Free();}
    }
    [Fact]
    public void RepeatedSplittingBetweenMirrorsIsBoundedAndCannotAmplify()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="splitter",Kind="beam_splitter",Position=[0,5,0]});
            world.AddPart(new(){Id="right",Kind="mirror",Position=[2,5,0]});
            world.AddPart(new(){Id="left",Kind="mirror",Position=[-2,5,0],Rotation=[0,180,0]});
            var emitter=world.AddPart(new(){Id="emitter",Kind="laser",Position=[-6,5,0]});
            var trace=OpticalNetwork.Trace(world,emitter,new(new(5,0,0),Vector3.Right,100,Vector3.One));
            Assert.Equal(OpticalNetwork.MaximumSegments,trace.Segments.Count);
            Assert.Empty(trace.Receptions);
            Assert.All(trace.Segments,s=>Assert.InRange(s.Power.X,0,1));
            Assert.Equal(trace.Segments,OpticalNetwork.Trace(world,emitter,new(new(5,0,0),Vector3.Right,100,Vector3.One)).Segments);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(3f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidReceiverThresholdIsRejected(float threshold)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="receiver",Kind="light_receiver",
                Properties=new(){[ReceiverParameters.Threshold]=threshold}}));
        }
        finally{world.Free();}
    }
}
