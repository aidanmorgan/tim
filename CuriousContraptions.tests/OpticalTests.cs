using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class OpticalTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData(false,false,false)]
    [InlineData(false,false,true)]
    [InlineData(false,true,false)]
    [InlineData(false,true,true)]
    [InlineData(true,false,false)]
    [InlineData(true,false,true)]
    [InlineData(true,true,false)]
    [InlineData(true,true,true)]
    public void EnableAndRealSupplyAreRequiredIndependently(bool enabled,bool sourcePower,bool receiverPower)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,4,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[3,4,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[0,8,0]});
            var load=world.AddPart(new(){Id="load",Kind="powered_gate",Position=[6,4,0]});
            if(sourcePower)Assert.True(world.Connect(battery,laser));
            if(receiverPower)Assert.True(world.Connect(battery,receiver));
            Assert.True(world.Connect(receiver,load));
            world.Start();
            if(enabled)world.Activate(laser);
            world.Step();
            Assert.False(receiver.Active); // Optical snapshot precedes this tick's electrical solve.
            world.Step();
            Assert.Equal(enabled&&sourcePower,receiver.Active);
            Assert.Equal(enabled&&sourcePower&&receiverPower,load.HasElectricalPower(SocketId.PowerIn));
            if(receiver.Active)
            {
                Assert.Equal(LaserPart.BeamPower,receiver.ReceivedPower);
                Assert.InRange(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)),5.09f,5.11f);
            }
            var sourceWire=world.Connections.SingleOrDefault(c=>c.To==laser.Uid);
            if(sourceWire!=null)world.Connections.Remove(sourceWire);
            world.Step();world.Step();
            Assert.False(receiver.Active);
            Assert.Equal(0,laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)));
            world.Restore();
            laser=(LaserPart)world.FindPart("laser")!;
            receiver=(LightReceiverPart)world.FindPart("receiver")!;
            Assert.False(laser.Enabled);
            Assert.Equal(0,laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)));
            Assert.Equal(Vector3.Zero,receiver.ReceivedPower);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f,0f,0f)]
    [InlineData(0f,90f,0f)]
    [InlineData(25f,35f,-20f)]
    [InlineData(0f,0f,90f)]
    public void ApertureAndBeamFollowFullThreeDimensionalTransforms(float x,float y,float z)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[0,7,0],Rotation=[x,y,z]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Rotation=[x,y,z]});
            receiver.Position=laser.Position+laser.Basis.X*5;
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(receiver.Active);
            receiver.RotateObjectLocal(Vector3.Up,Mathf.Pi);
            world.Step();
            Assert.False(receiver.Active); // Opaque back cannot detect.
            receiver.RotateObjectLocal(Vector3.Up,Mathf.Pi);
            receiver.Position+=laser.Basis.Y;
            world.Step();
            Assert.False(receiver.Active); // Finite disc, not an infinite target plane.
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData("wall")]
    [InlineData("ball")]
    [InlineData("pipe")]
    public void SharedOpaqueGeometryStopsTheBeamAndRemovalRestoresIt(string kind)
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,5,0]});
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[3,5,0]});
            // A pipe's centre bore transmits; placing its opaque collar at the ray blocks it.
            var blocker=world.AddPart(new(){Id="blocker",Kind=kind,Position=[0,kind=="pipe"?4.3f:5,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.False(receiver.Active);
            Assert.True(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To))<5);
            blocker.Position+=Vector3.Up*3;
            world.Step();
            Assert.True(receiver.Active);
            blocker.Position-=Vector3.Up*3;
            world.Step();
            Assert.False(receiver.Active);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RangeAndNearestAbsorbingTargetPreventDoubleCounting()
    {
        var world=World();
        try
        {
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser",Position=[0,5,0]});
            var near=(LightReceiverPart)world.AddPart(new(){Id="near",Kind="light_receiver",Position=[4,5,0]});
            var far=(LightReceiverPart)world.AddPart(new(){Id="far",Kind="light_receiver",Position=[8,5,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(near.Active);Assert.False(far.Active);
            near.Visible=false;world.Step();
            Assert.True(far.Active);
            far.Position=new(18,5,0);world.Step();
            Assert.False(far.Active);
            Assert.InRange(laser.BeamPath.Sum(s=>s.From.DistanceTo(s.To)),LaserPart.Range-.00001f,LaserPart.Range+.00001f);
        }
        finally{world.Free();}
    }
}
