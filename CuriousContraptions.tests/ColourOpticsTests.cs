using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ColourOpticsTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData("red",OpticalColour.Red,false)]
    [InlineData("red",OpticalColour.Red,true)]
    [InlineData("green",OpticalColour.Green,false)]
    [InlineData("green",OpticalColour.Green,true)]
    [InlineData("blue",OpticalColour.Blue,false)]
    [InlineData("blue",OpticalColour.Blue,true)]
    public void FilterPassesOnlyExistingChannelFromEitherSide(string name,OpticalColour colour,bool back)
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            var filter=world.AddPart(new(){Id="filter",Kind=name+"_filter",Position=[0,6,0],Rotation=[0,back?180:0,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind=name+"_receiver",Position=[3,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(2,trace.Segments.Count);
            var received=Assert.Single(trace.Receptions);
            Assert.Equal(receiver,received.Receiver);
            var expected=source.Power*OpticalColours.Mask(colour);
            Assert.Equal(expected,received.Power);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,received.Power}});
            Assert.True(receiver.Active);
            Assert.False(receiver.HasElectricalPower(SocketId.Supply));
            Assert.False(laser.Active); // Preview remains non-mutating.
            var missing=OpticalNetwork.Trace(world,laser,source with{Power=Vector3.One-OpticalColours.Mask(colour)});
            Assert.Empty(missing.Receptions);
            Assert.Single(missing.Segments);
            filter.RotationDegrees=new(23,37,11);
            var rotated=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(expected,Assert.Single(rotated.Receptions).Power);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData("red","blue")]
    [InlineData("green","red")]
    [InlineData("blue","green")]
    public void CrossedFiltersExtinguishAndIdenticalFiltersDoNotAmplify(string first,string other)
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-4,6,0]});
            world.AddPart(new(){Id="first",Kind=first+"_filter",Position=[-1,6,0]});
            var second=world.AddPart(new(){Id="second",Kind=other+"_filter",Position=[1,6,0]});
            world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[4,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Empty(trace.Receptions);
            Assert.Equal(2,trace.Segments.Count);
            ((ColourFilterPart)second).Colour=((ColourFilterPart)world.FindPart("first")!).Colour;
            trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(3,trace.Segments.Count);
            Assert.Equal(trace.Segments[1].Power,Assert.Single(trace.Receptions).Power);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData("red",OpticalColour.Red)]
    [InlineData("green",OpticalColour.Green)]
    [InlineData("blue",OpticalColour.Blue)]
    public void ReceiverRequiresPurityPowerAndSeparateSupplyAndResets(string name,OpticalColour colour)
    {
        var world=World();
        try
        {
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind=name+"_receiver",Position=[3,6,0]});
            var mask=OpticalColours.Mask(colour);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,Vector3.One}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.249f}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.25f}});Assert.True(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.9f+(Vector3.One-mask)*.051f}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.91f+(Vector3.One-mask)*.045f}});Assert.True(receiver.Active);
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            world.AddPart(new(){Id="filter",Kind=name+"_filter",Position=[0,6,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-5,2,3]});
            var gate=world.AddPart(new(){Id="gate",Kind="powered_gate",Position=[5,2,3]});
            Assert.True(world.Connect(battery,laser));
            Assert.True(world.Connect(receiver,gate));
            world.Start();world.Activate(laser);
            for(var i=0;i<4;i++)world.Step();
            Assert.True(receiver.Active);Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            Assert.False(world.Connect(battery,receiver)); // Editing is forbidden during Run.
            world.Restore();
            receiver=(LightReceiverPart)world.FindPart("receiver")!;
            laser=world.FindPart("laser")!;
            gate=world.FindPart("gate")!;
            Assert.True(world.Connect(world.FindPart("battery")!,receiver));
            world.Start();world.Activate(laser);
            for(var i=0;i<4;i++)world.Step();
            Assert.True(gate.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            receiver=(LightReceiverPart)world.FindPart("receiver")!;
            Assert.Equal(colour,receiver.Colour);
            Assert.False(receiver.Active);Assert.Equal(Vector3.Zero,receiver.ReceivedPower);
        }
        finally{world.Free();}
    }
    [Fact]
    public void FilterGlassCollidesAndOpaqueFrameStopsLight()
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,6,0]});
            world.AddPart(new(){Id="filter",Kind="red_filter",Position=[0,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source with{At=source.At+Vector3.Up*.74f});
            Assert.Single(trace.Segments);
            Assert.True(trace.Segments[0].To.X<0);
            world.Start();
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[-1,6,0]});
            ball.Velocity=Vector3.Right*4;
            for(var i=0;i<90;i++)world.Step();
            Assert.True(ball.Position.X<-.3f);
        }
        finally{world.Free();}
    }
    [Fact]
    public void BlueAndGreenBeamArtworkRemainVisibleWithoutRedPower()
    {
        foreach(var colour in new[]{OpticalColour.Red,OpticalColour.Green,OpticalColour.Blue})
        {
            var ink=OpticalColours.BeamInk(OpticalColours.Mask(colour));
            Assert.Equal(OpticalColours.Ink(colour),ink);
            var node=new OpticalPathVisual();godot.Tree.Root.AddChild(node);
            try
            {
                node.Refresh([new(new(0,4,0),new(2,4,0),OpticalColours.Mask(colour)*.32f,"test")]);
                var line=Assert.Single(node.GetChildren().OfType<MeshInstance3D>());
                Assert.True(line.Visible);
                Assert.InRange(((StandardMaterial3D)line.MaterialOverride).AlbedoColor.A,.223f,.225f);
            }
            finally{node.Free();}
        }
    }
}
