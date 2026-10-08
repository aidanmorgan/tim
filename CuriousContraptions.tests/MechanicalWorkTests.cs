using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class MechanicalWorkTests(NativeSceneFixture godot)
{
    public enum Circuit { Parallel, Serial, Reverse, OpenClutch, ClosedClutch, Spring }
    private enum Fixture { Battery, Motor, First, Second, Reverse, Clutch, Spring }
    private enum SupplyParameter { Enabled }
    private static string Id(Fixture role)=>role switch
    {
        Fixture.Battery=>"supply",Fixture.Motor=>"motor",Fixture.First=>"first",Fixture.Second=>"second",
        Fixture.Reverse=>"reverse",Fixture.Clutch=>"clutch",Fixture.Spring=>"spring",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Fixture role)=>role switch
    {
        Fixture.Battery=>"battery",Fixture.Motor=>"motor",Fixture.First or Fixture.Second=>"conveyor",
        Fixture.Reverse=>"reverse_transmission",Fixture.Clutch=>"clutch",Fixture.Spring=>WoundSpringPart.CatalogId,
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Add(MachineWorld world,Fixture role,Vector3 at)=>
        world.AddPart(new(){Id=Id(role),Kind=Kind(role),Position=[at.X,at.Y,at.Z]});
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static double Energy(MachineWorld world)=>world.PhysicsAssembly.Bodies.ToArray().Sum(b=>b.KineticEnergy)+
        world.Parts.OfType<WoundSpringPart>().Sum(s=>s.StoredEnergy);
    private static double HeightEnergy(MachineWorld world)=>world.PhysicsAssembly.Bodies.ToArray()
        .Where(b=>b.MotionType==PhysicsMotionType.Dynamic).Sum(b=>9.81*b.Center.Y/b.InverseMass);

    [Theory]
    [InlineData(Circuit.Parallel)]
    [InlineData(Circuit.Serial)]
    [InlineData(Circuit.Reverse)]
    [InlineData(Circuit.OpenClutch)]
    [InlineData(Circuit.ClosedClutch)]
    [InlineData(Circuit.Spring)]
    public void AuthoredBeltsUseOwnedGuidesAndOneSourceEnergySupply(Circuit circuit)
    {
        if(!Enum.IsDefined(circuit)) throw new ArgumentOutOfRangeException(nameof(circuit));
        var world=World();
        try
        {
            var battery=Add(world,Fixture.Battery,new(-6,6,0));
            var motor=(MotorPart)Add(world,Fixture.Motor,new(-4,6,0));
            Assert.True(world.Connect(battery,motor));
            var load=Add(world,circuit==Circuit.Spring?Fixture.Spring:Fixture.First,new(2,6,0));
            switch(circuit)
            {
                case Circuit.Parallel:
                case Circuit.Serial:
                    var second=Add(world,Fixture.Second,new(6,6,0));
                    Assert.True(world.Connect(motor,load));
                    Assert.True(world.Connect(circuit==Circuit.Serial?load:motor,second));
                    break;
                case Circuit.Reverse:
                    var reverse=Add(world,Fixture.Reverse,new(-1,6,0));
                    Assert.True(world.Connect(motor,reverse));Assert.True(world.Connect(reverse,load));
                    break;
                case Circuit.OpenClutch:
                case Circuit.ClosedClutch:
                    var clutch=Add(world,Fixture.Clutch,new(-1,6,0));
                    Assert.True(world.Connect(motor,clutch));Assert.True(world.Connect(clutch,load));
                    if(circuit==Circuit.ClosedClutch) Assert.True(world.Connect(battery,clutch));
                    break;
                case Circuit.Spring: Assert.True(world.Connect(motor,load));break;
            }
            var saved=Saved(world);world.Start();
            var height=HeightEnergy(world);
            for(var i=0;i<60;i++)
            {
                world.Step();
                Assert.InRange(Energy(world),0,motor.SuppliedWork+Math.Max(0,height-HeightEnergy(world))+.01);
                Assert.All(world.Physics.MotorUse.ToArray(),use=>Assert.Equal(world.PhysicsAssembly.JointId(new(motor,MotorPart.ShaftJoint)),use.Joint));
            }
            if(circuit==Circuit.OpenClutch) Assert.InRange(Math.Abs(MechanicalNetwork.Speed(world,load,SocketId.DriveIn)),0,1e-6);
            else if(circuit==Circuit.Spring) Assert.True(((WoundSpringPart)load).StoredEnergy>.01);
            else Assert.True(MechanicalNetwork.Speed(world,load,SocketId.DriveIn)*(circuit==Circuit.Reverse?-1:1)>0);
            if(circuit!=Circuit.OpenClutch&&circuit!=Circuit.Spring)
                Assert.InRange(Math.Abs(MechanicalNetwork.Speed(world,load,SocketId.DriveIn)-
                    motor.ShaftSpeed*(circuit==Circuit.Reverse?-1:1)),0,1e-5);
            world.QueueBinaryInput(new(battery,BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            world.Step();var work=motor.SuppliedWork;
            for(var i=0;i<10;i++) {world.Step();Assert.Equal(work,motor.SuppliedWork);}
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void TopologyRejectsCompetingInputBeltsButDoesNotUseAPropagationLoopBan()
    {
        var world=World();
        try
        {
            var a=Add(world,Fixture.First,new(-3,6,0));
            var b=Add(world,Fixture.Second,new(3,6,0));
            var motor=Add(world,Fixture.Motor,new(-6,6,0));
            Assert.True(world.Connect(a,b));Assert.True(world.Connect(b,a));
            Assert.False(world.Connect(motor,b));
            world.Start();world.Step();
            Assert.Equal(0,MechanicalNetwork.Speed(world,a,SocketId.Drive));
            Assert.Equal(0,MechanicalNetwork.Speed(world,b,SocketId.Drive));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(ConveyorParameter.SurfacePerRadian,0f)]
    [InlineData(ConveyorParameter.SurfacePerRadian,float.NaN)]
    [InlineData(ConveyorParameter.Length,-1f)]
    [InlineData(ConveyorParameter.Width,0f)]
    public void InvalidConveyorParametersReject(ConveyorParameter parameter,float value)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=Id(Fixture.First),Kind=Kind(Fixture.First),
                Properties=new(){[PartParameterName.Of(parameter)]=value}}));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(101f)]
    public void InvalidAuthoredMotorTorqueIsRejected(float torque)
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=Id(Fixture.Motor),Kind=Kind(Fixture.Motor),
                Properties=new(){[PartParameterName.Of(MotorParameter.Torque)]=torque}}));
        }
        finally {world.Free();}
    }

    [Fact]
    public void RetiredConveyorForceBudgetParameterIsRejected()
    {
        const string retiredParameter="traction"; // Unsupported authored boundary, not a behaviour selector.
        var world=World();
        try
        {
            Assert.Throws<ArgumentException>(()=>world.AddPart(new(){Id=Id(Fixture.First),Kind=Kind(Fixture.First),
                Properties=new(){[retiredParameter]=18}}));
        }
        finally {world.Free();}
    }

    [Fact]
    public void MechanicalBindingsRejectUndefinedPortsAndInvalidRatios()
    {
        var slot=new JointSlot();
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalBinding((SocketId)999,slot,1));
        foreach(var ratio in new[]{0,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalBinding(SocketId.Drive,slot,ratio));
        Assert.Throws<ArgumentNullException>(()=>new MechanicalBinding(SocketId.Drive,null!,1));
    }
}
