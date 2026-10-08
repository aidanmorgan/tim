using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ConveyorRuntimeTests(NativeSceneFixture godot)
{
    private static double MotorWork(MachineWorld world)=>world.Parts.OfType<MotorPart>().Single().SuppliedWork;
    public enum DriveMode { Unpowered, Forward, Reverse, Serial }
    private enum Role { Battery, Motor, Conveyor, Reverse, SecondConveyor, Payload, SecondPayload }
    private enum SupplyParameter { Enabled }
    private static string Id(Role role)=>role switch
    {
        Role.Battery=>"supply",Role.Motor=>"motor",Role.Conveyor=>"conveyor",Role.Reverse=>"reverse",
        Role.SecondConveyor=>"second_conveyor",Role.Payload=>"payload",Role.SecondPayload=>"second_payload",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Battery=>"battery",Role.Motor=>"motor",Role.Conveyor or Role.SecondConveyor=>"conveyor",
        Role.Reverse=>"reverse_transmission",Role.Payload or Role.SecondPayload=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static MachinePart Add(MachineWorld world,Role role,Vector3 position)=>
        world.AddPart(new(){Id=Id(role),Kind=Kind(role),Position=[position.X,position.Y,position.Z]});
    private MachineWorld World()
    {
        var world=new MachineWorld { Pressure=0 };
        godot.Tree.Root.AddChild(world); return world;
    }
    private static (ConveyorPart Belt,MachinePart Load,MachinePart Battery) Build(MachineWorld world,DriveMode mode,bool rotated=false,bool twoLoads=false)
    {
        if(!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var supply=Add(world,Role.Battery,new(-6,7,2));
        var motor=Add(world,Role.Motor,new(-4,7,2));
        var belt=(ConveyorPart)Add(world,Role.Conveyor,new(0,5,0));
        if(rotated) belt.RotationDegrees=new(0,90,0);
        if(mode!=DriveMode.Unpowered) Assert.True(world.Connect(supply,motor));
        var source=motor;
        if(mode==DriveMode.Reverse)
        {
            source=Add(world,Role.Reverse,new(-2,7,2));
            Assert.True(world.Connect(motor,source));
        }
        Assert.True(world.Connect(source,belt));
        if(mode==DriveMode.Serial)
        {
            var second=Add(world,Role.SecondConveyor,new(4,5,0));
            Assert.True(world.Connect(belt,second));
        }
        var load=Add(world,Role.Payload,belt.Transform*new Vector3(twoLoads?-.5f:0,1,0));
        load.Position=belt.Transform*new Vector3(twoLoads?-.5f:0,.12f+load.Radius+.0001f,0);
        if(twoLoads)
        {
            var second=Add(world,Role.SecondPayload,belt.Transform*new Vector3(.5f,1,0));
            second.Position=belt.Transform*new Vector3(.5f,.12f+second.Radius+.0001f,0);
        }
        return (belt,load,supply);
    }

    [Theory]
    [InlineData(DriveMode.Unpowered,false)]
    [InlineData(DriveMode.Forward,false)]
    [InlineData(DriveMode.Reverse,false)]
    [InlineData(DriveMode.Serial,false)]
    [InlineData(DriveMode.Unpowered,true)]
    [InlineData(DriveMode.Forward,true)]
    [InlineData(DriveMode.Reverse,true)]
    [InlineData(DriveMode.Serial,true)]
    public void SharedShaftTransportsWithTypedConnectionsAndExactConstructionReset(DriveMode mode,bool rotated)
    {
        var world=World();
        try
        {
            var rig=Build(world,mode,rotated);
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var initial=rig.Load.Position;
            Assert.Contains(world.Connections,c=>c.To==rig.Belt.Uid&&c.Type==ConnectionDomain.Mechanical&&c.ToPort==SocketId.DriveIn);
            world.Start();
            var shaft=world.PhysicsAssembly.Body(new(rig.Belt,ConveyorPart.ShaftBody));
            var joint=(PhysicsFrameJoint)world.CurrentJoint(new(rig.Belt,ConveyorPart.ShaftJoint));
            Assert.Same(shaft,joint.A); Assert.Equal(FrameJointKind.Hinge,joint.Kind);
            Assert.Contains(world.Physics.Surfaces.ToArray(),s=>s.Drive==joint);
            for(var i=0;i<60;i++) world.Step();
            Assert.InRange(Math.Abs(rig.Belt.ShaftSpeed+joint.Travel.Jacobian.Bind(joint.A,joint.B).Speed),0,1e-6);
            var travel=(rig.Load.Position-initial).Dot(rig.Belt.Basis.X);
            var sign=mode==DriveMode.Reverse?-1:1;
            if(mode==DriveMode.Unpowered)
            {
                Assert.InRange(Math.Abs(travel),0,1e-6); Assert.Equal(0,MotorWork(world));
                Assert.DoesNotContain(new MachineEvent(MachineEventKind.Transported,rig.Belt.Uid,rig.Load.Uid),world.Events.Keys);
            }
            else
            {
                Assert.True(sign*travel>.02,$"Travel {travel}; mode {mode}; rotated {rotated}.");
                Assert.True(sign*rig.Belt.ShaftSpeed>0); Assert.True(MotorWork(world)>0);
                Assert.Contains(new MachineEvent(MachineEventKind.Transported,rig.Belt.Uid,rig.Load.Uid),world.Events.Keys);
            }
            Assert.Same(world.PhysicsAssembly.Body(new(rig.Belt,ConveyorPart.ShaftBody)),MechanicalNetwork.Shaft(world,rig.Belt,SocketId.DriveIn).A);
            var state=world.Physics.Capture();
            rig.Belt._Process(.02);
            Assert.Equal(state.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,MotorWork(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void PowerLossAllowsStoredInertiaToCoastButSuppliesNoNewWork()
    {
        var world=World();
        try
        {
            var rig=Build(world,DriveMode.Forward);
            world.Start(); for(var i=0;i<30;i++) world.Step();
            world.QueueBinaryInput(new(rig.Battery,BatteryPart.EnableInput),Bridge.BinaryInputState.Disabled);
            world.Step();
            var accepted=MotorWork(world);
            var shaft=world.PhysicsAssembly.Body(new(rig.Belt,ConveyorPart.ShaftBody));
            Assert.True(shaft.KineticEnergy>0);
            for(var i=0;i<20;i++)
            {
                world.Step();
                Assert.Equal(accepted,MotorWork(world));
                Assert.All(world.Physics.MotorUse.ToArray(),use=>Assert.Equal(0,use.SuppliedWork));
            }
        }
        finally {world.Free();}
    }

    [Fact]
    public void MultiplePayloadsShareTheActualShaftAndAvailableMotorWork()
    {
        var world=World();
        try
        {
            var rig=Build(world,DriveMode.Forward,twoLoads:true);
            var other=world.FindPart(Id(Role.SecondPayload))!;
            var x0=rig.Load.Position.X; var x1=other.Position.X;
            world.Start(); for(var i=0;i<60;i++) world.Step();
            Assert.True(rig.Load.Position.X>x0+.01); Assert.True(other.Position.X>x1+.01);
            Assert.InRange(MotorWork(world),0,120*60d*MachineWorld.Tick);
            var energy=world.PhysicsAssembly.Body(new(rig.Load,MachinePart.RootBody)).KineticEnergy+
                world.PhysicsAssembly.Body(new(other,MachinePart.RootBody)).KineticEnergy+
                world.PhysicsAssembly.Body(new(rig.Belt,ConveyorPart.ShaftBody)).KineticEnergy;
            Assert.InRange(energy,0,MotorWork(world)+.01);
        }
        finally {world.Free();}
    }

    [Fact]
    public void FixtureBoundariesRejectUnknownRoles()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Kind((Role)999));
        Assert.Equal("conveyor",Kind(Role.Conveyor)); Assert.Equal("second_conveyor",Id(Role.SecondConveyor));
    }
}
