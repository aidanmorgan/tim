using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ColourOpticsTests(NativeSceneFixture godot)
{
    private enum Role { Laser, Filter, Receiver, Battery, Gate, Ball, First, Second, Preview }
    private static string Id(Role role)=>role switch
    {
        Role.Laser=>"laser",Role.Filter=>"filter",Role.Receiver=>"receiver",
        Role.Battery=>"battery",Role.Gate=>"gate",Role.Ball=>"ball",
        Role.First=>"first",Role.Second=>"second",Role.Preview=>"test",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Laser=>"laser",Role.Receiver=>"light_receiver",Role.Battery=>"battery",
        Role.Gate=>"powered_gate",Role.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string FilterKind(OpticalColour colour)=>colour switch
    {
        OpticalColour.Red=>"red_filter",OpticalColour.Green=>"green_filter",OpticalColour.Blue=>"blue_filter",
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    private static string ReceiverKind(OpticalColour colour)=>colour switch
    {
        OpticalColour.Red=>"red_receiver",OpticalColour.Green=>"green_receiver",OpticalColour.Blue=>"blue_receiver",
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };

    [Theory]
    [InlineData(OpticalColour.Red,"red_filter","red_receiver")]
    [InlineData(OpticalColour.Green,"green_filter","green_receiver")]
    [InlineData(OpticalColour.Blue,"blue_filter","blue_receiver")]
    public void ColourCatalogBoundaryIsCanonical(OpticalColour colour,string filter,string receiver)
    {
        Assert.Equal(filter,FilterKind(colour));
        Assert.Equal(receiver,ReceiverKind(colour));
    }

    [Fact]
    public void UnsupportedFixtureChoicesAreRejected()
    {
        foreach(var colour in Enum.GetValues<OpticalColour>().Where(c=>
            c is not (OpticalColour.Red or OpticalColour.Green or OpticalColour.Blue)).Append((OpticalColour)999))
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>FilterKind(colour));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ReceiverKind(colour));
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind(Role.Filter));
        Assert.Equal(new[]{"laser","filter","receiver","battery","gate","ball","first","second","test"},
            Enum.GetValues<Role>().Select(Id));
        Assert.Equal(new[]{"laser","light_receiver","battery","powered_gate","ball"},
            new[]{Role.Laser,Role.Receiver,Role.Battery,Role.Gate,Role.Ball}.Select(Kind));
    }

    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(OpticalColour.Red,false)]
    [InlineData(OpticalColour.Red,true)]
    [InlineData(OpticalColour.Green,false)]
    [InlineData(OpticalColour.Green,true)]
    [InlineData(OpticalColour.Blue,false)]
    [InlineData(OpticalColour.Blue,true)]
    public void FilterPassesOnlyExistingChannelFromEitherSide(OpticalColour colour,bool back)
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id=Id(Role.Laser),Kind=Kind(Role.Laser),Position=[-3,6,0]});
            var filter=world.AddPart(new(){Id=Id(Role.Filter),Kind=FilterKind(colour),Position=[0,6,0],Orientation = PartOrientation.FromEulerDegrees(0,back?180:0,0)});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id=Id(Role.Receiver),Kind=ReceiverKind(colour),Position=[3,6,0]});
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
    [InlineData(OpticalColour.Red,OpticalColour.Blue)]
    [InlineData(OpticalColour.Green,OpticalColour.Red)]
    [InlineData(OpticalColour.Blue,OpticalColour.Green)]
    public void CrossedFiltersExtinguishAndIdenticalFiltersDoNotAmplify(OpticalColour first,OpticalColour other)
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id=Id(Role.Laser),Kind=Kind(Role.Laser),Position=[-4,6,0]});
            world.AddPart(new(){Id=Id(Role.First),Kind=FilterKind(first),Position=[-1,6,0]});
            var second=world.AddPart(new(){Id=Id(Role.Second),Kind=FilterKind(other),Position=[1,6,0]});
            world.AddPart(new(){Id=Id(Role.Receiver),Kind=Kind(Role.Receiver),Position=[4,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Empty(trace.Receptions);
            Assert.Equal(2,trace.Segments.Count);
            ((ColourFilterPart)second).Colour=((ColourFilterPart)world.FindPart(Id(Role.First))!).Colour;
            trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(3,trace.Segments.Count);
            Assert.Equal(trace.Segments[1].Power,Assert.Single(trace.Receptions).Power);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(OpticalColour.Red)]
    [InlineData(OpticalColour.Green)]
    [InlineData(OpticalColour.Blue)]
    public void ReceiverRequiresPurityPowerAndSeparateSupplyAndResets(OpticalColour colour)
    {
        var world=World();
        try
        {
            var receiver=(LightReceiverPart)world.AddPart(new(){Id=Id(Role.Receiver),Kind=ReceiverKind(colour),Position=[3,6,0]});
            var mask=OpticalColours.Mask(colour);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,Vector3.One}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.249f}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.25f}});Assert.True(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.9f+(Vector3.One-mask)*.051f}});Assert.False(receiver.Active);
            receiver.ReceiveOpticalPower(new Dictionary<OpticalPortId,Vector3>{{OpticalPortId.Main,mask*.91f+(Vector3.One-mask)*.045f}});Assert.True(receiver.Active);
            var laser=world.AddPart(new(){Id=Id(Role.Laser),Kind=Kind(Role.Laser),Position=[-3,6,0]});
            world.AddPart(new(){Id=Id(Role.Filter),Kind=FilterKind(colour),Position=[0,6,0]});
            var battery=world.AddPart(new(){Id=Id(Role.Battery),Kind=Kind(Role.Battery),Position=[-5,2,3]});
            var gate=world.AddPart(new(){Id=Id(Role.Gate),Kind=Kind(Role.Gate),Position=[5,2,3]});
            Assert.True(world.Connect(battery,laser));
            Assert.True(world.Connect(receiver,gate));
            world.Start();world.Activate(laser);
            for(var i=0;i<4;i++)world.Step();
            Assert.True(receiver.Active);Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            Assert.False(world.Connect(battery,receiver)); // Editing is forbidden during Run.
            world.Restore();
            receiver=(LightReceiverPart)world.FindPart(Id(Role.Receiver))!;
            laser=world.FindPart(Id(Role.Laser))!;
            gate=world.FindPart(Id(Role.Gate))!;
            Assert.True(world.Connect(world.FindPart(Id(Role.Battery))!,receiver));
            world.Start();world.Activate(laser);
            for(var i=0;i<4;i++)world.Step();
            Assert.True(gate.HasElectricalPower(SocketId.PowerIn));
            world.Restore();
            receiver=(LightReceiverPart)world.FindPart(Id(Role.Receiver))!;
            Assert.Equal(colour,receiver.Colour);
            Assert.False(receiver.Active);Assert.Equal(Vector3.Zero,receiver.ReceivedPower);
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(OpticalColour.Red)]
    [InlineData(OpticalColour.Green)]
    [InlineData(OpticalColour.Blue)]
    public void FilterGlassCollidesAndOpaqueFrameStopsLight(OpticalColour colour)
    {
        var world=World();
        try
        {
            var laser=world.AddPart(new(){Id=Id(Role.Laser),Kind=Kind(Role.Laser),Position=[-3,6,0]});
            var filter=world.AddPart(new(){Id=Id(Role.Filter),Kind=FilterKind(colour),Position=[0,6,0]});
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source with{At=source.At+Vector3.Up*.74f});
            Assert.Single(trace.Segments);
            Assert.True(trace.Segments[0].To.X<0);
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[-1,6,0]});
            ball.InitialVelocity=Vector3.Right*4;
            var construction=ball.Transform;
            world.Start();
            var ballId=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id;
            var filterId=world.PhysicsAssembly.Body(new(filter,MachinePart.RootBody)).Id;
            var hit=false;
            for(var i=0;i<45;i++)
            {
                world.Step();
                hit|=world.TickImpacts.ToArray().Any(impact=>
                    impact.Pair.A==ballId&&impact.Pair.B==filterId||
                    impact.Pair.B==ballId&&impact.Pair.A==filterId);
            }
            Assert.True(hit);
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            Assert.True(solved.Center.X<-.3);
            Assert.True(solved.LinearVelocity.X<0);
            var after=world.Physics.Capture();
            world.Restore();
            ball=world.FindPart(Id(Role.Ball))!;
            Assert.Equal(construction,ball.Transform);
            Assert.Equal(Vector3.Right*4,ball.InitialVelocity);
            world.Start();
            for(var i=0;i<45;i++)world.Step();
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
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
                node.Refresh([new(new(0,4,0),new(2,4,0),OpticalColours.Mask(colour)*.32f,new OpticalPathOwner(Id(Role.Preview)))]);
                var line=Assert.Single(node.GetChildren().OfType<MeshInstance3D>());
                Assert.True(line.Visible);
                Assert.InRange(((StandardMaterial3D)line.MaterialOverride).AlbedoColor.A,.223f,.225f);
            }
            finally{node.Free();}
        }
    }
}
