using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PressurePlateTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData("ball",1,true,true)]
    [InlineData("ball",1,false,true)]
    [InlineData("tennis",1,true,false)]
    [InlineData("tennis",2,true,true)]
    [InlineData("weight",1,true,true)]
    public void RestingLoadClosesOnlySuppliedContactAndResetClears(string kind,int count,bool supply,bool pressed)
    {
        var world=World();
        try
        {
            var plate=(PressurePlatePart)world.AddPart(new(){Id="plate",Kind="pressure_plate",Position=[0,2,0]});
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-4,2,0]});
            var gate=(PoweredGatePart)world.AddPart(new(){Id="gate",Kind="powered_gate",Position=[4,2,0]});
            if(supply)Assert.True(world.Connect(battery,plate));
            Assert.True(world.Connect(plate,gate));
            for(var i=0;i<count;i++)world.AddPart(new(){Id="load"+i,Kind=kind,Position=[count==1?0:i==0?-.45f:.45f,4,0]});
            world.Start();
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(pressed,plate.Active);
            Assert.Equal(pressed&&supply,gate.HasElectricalPower(SocketIds.PowerIn));
            Assert.Equal(pressed?PressurePlateState.Pressed:PressurePlateState.Underweight,plate.State);
            var mass=world.Bodies.Sum(b=>b.Mass);
            Assert.Equal(mass,plate.SupportedMass,3);
            if(supply&&pressed)Assert.Equal(GateState.Open,gate.State);
            foreach(var body in world.Bodies)body.Position+=Vector3.Back*4; // Native removal fixture.
            world.Step();
            Assert.Equal(PressurePlateState.Empty,plate.State);
            Assert.False(gate.Active);
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Closed,gate.State);
            world.Restore();
            plate=(PressurePlatePart)world.FindPart("plate")!;
            Assert.Equal(0,plate.SupportedMass);
            Assert.Equal(PressurePlateState.Empty,plate.State);
            Assert.False(plate.Active);
            Assert.Equal(supply?2:1,world.Connections.Count);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(0f,.42f,0f,true)]
    [InlineData(0f,.5f,0f,false)]
    [InlineData(1.2f,.42f,0f,false)]
    [InlineData(0f,-.26f,0f,false)]
    public void RotatedPlateOnlyMeasuresItsTopFace(float x,float y,float z,bool pressed)
    {
        var world=World();
        try
        {
            var plate=(PressurePlatePart)world.AddPart(new(){Id="plate",Kind="pressure_plate",
                Position=[0,5,0],Rotation=[20,30,40]});
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,8,0]});
            ball.Position=plate.Transform*new Vector3(x,y,z);
            plate.BeforeNetworks(world);
            Assert.Equal(pressed,plate.Active);
            ball.Visible=false;
            plate.BeforeNetworks(world);
            Assert.Equal(PressurePlateState.Empty,plate.State);
        }
        finally {world.Free();}
    }
    [Fact]
    public void ThresholdRoundTripsAndTriggerCannotReplaceElectricalSupply()
    {
        var world=World();
        try
        {
            var plate=world.AddPart(new(){Id="plate",Kind="pressure_plate",
                Properties=new(){[PressurePlateParameters.MinimumMass]=3}});
            var delay=world.AddPart(new(){Id="delay",Kind="delay"});
            Assert.False(world.Connect(delay,plate));
            var copy=MachineCodec.Clone(world.Snapshot());
            world.LoadMachine(copy);
            Assert.Equal(3,((PressurePlatePart)world.FindPart("plate")!).MinimumMass);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(17f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidThresholdIsRejected(float mass)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id="plate",Kind="pressure_plate",
                Properties=new(){[PressurePlateParameters.MinimumMass]=mass}}));
            Assert.Empty(world.Parts);
        }
        finally {world.Free();}
    }
}
