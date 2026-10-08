using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PressurePlateTests(NativeSceneFixture godot)
{
    public enum Load { Ball, Tennis, Weight }
    private enum Fixture { Plate, Battery, Gate, Ball, Delay }
    private static string Wire(Fixture value)=>value switch
    {
        Fixture.Plate=>"pressure_plate",Fixture.Battery=>"battery",Fixture.Gate=>"powered_gate",
        Fixture.Ball=>"ball",Fixture.Delay=>"delay",_=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    private static string LoadWire(Load value)=>value switch
    {
        Load.Ball=>"ball",Load.Tennis=>"tennis",Load.Weight=>"weight",
        _=>throw new ArgumentOutOfRangeException(nameof(value))
    };
    [Fact]
    public void FixtureMappingsAreCanonicalAndRejectUnknownValues()
    {
        Assert.Equal("weight",LoadWire(Load.Weight));
        Assert.Equal("tennis",LoadWire(Load.Tennis));
        Assert.Equal("pressure_plate",Wire(Fixture.Plate));
        Assert.Throws<ArgumentOutOfRangeException>(()=>LoadWire((Load)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)99));
    }
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData(Load.Ball,1,true,true)]
    [InlineData(Load.Ball,1,false,true)]
    [InlineData(Load.Tennis,1,true,false)]
    [InlineData(Load.Tennis,2,true,true)]
    [InlineData(Load.Weight,1,true,true)]
    public void RestingLoadClosesOnlySuppliedContactAndResetClears(Load kind,int count,bool supply,bool pressed)
    {
        var world=World();
        try
        {
            var plate=(PressurePlatePart)world.AddPart(new(){Id=Wire(Fixture.Plate),Kind=Wire(Fixture.Plate),Position=[0,2,0]});
            var battery=world.AddPart(new(){Id=Wire(Fixture.Battery),Kind=Wire(Fixture.Battery),Position=[-4,2,0]});
            var gate=(PoweredGatePart)world.AddPart(new(){Id=Wire(Fixture.Gate),Kind=Wire(Fixture.Gate),Position=[4,2,0]});
            if(supply)Assert.True(world.Connect(battery,plate));
            Assert.True(world.Connect(plate,gate));
            for(var i=0;i<count;i++)world.AddPart(new(){Id="load"+i,Kind=LoadWire(kind),Position=[count==1?0:i==0?-.45f:.45f,4,0]});
            world.Start();
            for(var i=0;i<240;i++)world.Step();
            Assert.Equal(pressed,plate.Active);
            Assert.Equal(pressed&&supply,gate.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(pressed?PhysicsContactLoadPhase.Loaded:PhysicsContactLoadPhase.Underweight,plate.State);
            var mass=world.Bodies.Sum(b=>b.Mass);
            Assert.Equal(mass,plate.SupportedMass,3);
            if(supply&&pressed)Assert.Equal(GateState.Open,gate.State);
            foreach(var body in world.Bodies)
            {
                var solved=world.PhysicsAssembly.Body(new(body,MachinePart.RootBody));
                var collider=world.Physics.Collider(solved.Id).Declaration;
                world.Physics.ApplyColliderUpdates([new(solved.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            }
            world.Step();
            Assert.Equal(PhysicsContactLoadPhase.Empty,plate.State);
            Assert.False(gate.Active);
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Closed,gate.State);
            world.Restore();
            plate=(PressurePlatePart)world.FindPart(Wire(Fixture.Plate))!;
            Assert.Equal(0,plate.SupportedMass);
            Assert.Equal(PhysicsContactLoadPhase.Empty,plate.State);
            Assert.False(plate.Active);
            Assert.Equal(supply?2:1,world.Connections.Count);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(0f,.42f,0f,true)]
    [InlineData(0f,.5f,0f,false)]
    [InlineData(1.2f,.42f,0f,false)]
    [InlineData(0f,-.66f,0f,false)]
    public void RotatedPlateOnlyMeasuresItsTopFace(float x,float y,float z,bool pressed)
    {
        var world=World();
        try
        {
            var plate=(PressurePlatePart)world.AddPart(new(){Id=Wire(Fixture.Plate),Kind=Wire(Fixture.Plate),
                Position=[0,5,0],Orientation = PartOrientation.FromEulerDegrees(20,30,40)});
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[0,8,0]});
            ball.Position=plate.Transform*new Vector3(x,y,z);
            world.Start();
            plate.BeforeNetworks(world);
            Assert.Equal(pressed,plate.Active);
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var collider=world.Physics.Collider(solved.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(solved.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            plate.BeforeNetworks(world);
            Assert.Equal(PhysicsContactLoadPhase.Empty,plate.State);
        }
        finally {world.Free();}
    }
    [Fact]
    public void ThresholdRoundTripsAndTriggerCannotReplaceElectricalSupply()
    {
        var world=World();
        try
        {
            var plate=world.AddPart(new(){Id=Wire(Fixture.Plate),Kind=Wire(Fixture.Plate),
                Properties=new(){[PartParameterName.Of(PressurePlateParameter.MinimumMass)]=3}});
            var delay=world.AddPart(new(){Id=Wire(Fixture.Delay),Kind=Wire(Fixture.Delay)});
            Assert.False(world.Connect(delay,plate));
            var copy=MachineCodec.Clone(world.Snapshot());
            world.LoadMachine(copy);
            Assert.Equal(3,((PressurePlatePart)world.FindPart(Wire(Fixture.Plate))!).MinimumMass);
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
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=Wire(Fixture.Plate),Kind=Wire(Fixture.Plate),
                Properties=new(){[PartParameterName.Of(PressurePlateParameter.MinimumMass)]=mass}}));
            Assert.Empty(world.Parts);
        }
        finally {world.Free();}
    }
}
