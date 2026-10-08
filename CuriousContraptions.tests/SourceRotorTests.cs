using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SourceRotorTests(NativeSceneFixture godot)
{
    public enum SourceKind { Motor, Windmill }
    private enum Fixture { Battery, Fan, Motor, Windmill }
    public enum AirDirection { Forward, Reverse }
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Battery=>"battery",Fixture.Fan=>"fan",Fixture.Motor=>"motor",Fixture.Windmill=>"windmill",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);return world;
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 at,Vector3 rotation=default)=>
        world.AddPart(new(){Id=Kind(fixture),Kind=Kind(fixture),Position=[at.X,at.Y,at.Z],Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z)});
    private static JointSlot Guide(SourceKind kind)=>kind switch
    {
        SourceKind.Motor=>MotorPart.ShaftJoint,SourceKind.Windmill=>WindmillPart.RotorJoint,
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static BodySlot Rotor(SourceKind kind)=>kind switch
    {
        SourceKind.Motor=>MotorPart.ShaftBody,SourceKind.Windmill=>WindmillPart.RotorBody,
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static MachinePart Build(MachineWorld world,SourceKind kind,bool supplied,Vector3 rotation)
    {
        var at=new Vector3(0,6,0);
        var basis=Basis.FromEuler(rotation*Mathf.Pi/180);
        switch(kind)
        {
            case SourceKind.Motor:
                var motor=Add(world,Fixture.Motor,at,rotation);
                var battery=Add(world,Fixture.Battery,new(-4,6,0));
                if(supplied) Assert.True(world.Connect(battery,motor));
                return motor;
            case SourceKind.Windmill:
                var mill=Add(world,Fixture.Windmill,at,rotation);
                if(supplied) Add(world,Fixture.Fan,at-basis.X*3,rotation);
                return mill;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
    private static void CheckResistance(MachineWorld world,WindmillPart mill)
    {
        var joint=(PhysicsFrameJoint)world.CurrentJoint(new(mill,WindmillPart.RotorJoint));
        Assert.DoesNotContain(world.Physics.Loads.Damping.ToArray(),load=>load.Joint==joint.Id);
        Assert.DoesNotContain(world.Physics.Loads.Efforts.ToArray(),load=>load.Joint==joint.Id);
        var load=Assert.Single(world.Physics.Loads.Rotary,load=>load.Joint==joint.Id);
        Assert.Equal((double)mill.ReadParameter(WindmillParameter.RadiansPerForce),load.Material.SpeedPerForce);
        Assert.Equal((double)WindmillPart.TorqueArm,load.Material.MaximumPitch);
        Assert.Equal((double)WindmillPart.CutInForce,load.Material.CutInForce);
        Assert.Equal((double)WindmillPart.MaximumSpeed,load.Material.MaximumSpeed);
        Assert.Equal(AirflowNetwork.ControlForceTolerance,load.ForceTolerance);
        foreach(var branch in load.Branches)Assert.Equal(joint.B.Id,branch.Field.Body);
    }
    private static double Speed(PhysicsFrameJoint guide)=>guide.Travel.Jacobian.Bind(guide.A,guide.B).Speed;
    private static float ObservedSpeed(MachinePart part)=>part switch
    {
        MotorPart motor=>-motor.ShaftSpeed,WindmillPart mill=>mill.ShaftSpeed,
        _=>throw new ArgumentException("Fixture must declare a source rotor.")
    };
    private static float Travel(MachinePart part)=>part switch
    {
        MotorPart motor=>motor.ShaftTravel,WindmillPart mill=>mill.ShaftTravel,
        _=>throw new ArgumentException("Fixture must declare a source rotor.")
    };
    private static string Snapshot(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(SourceKind.Motor,true,false)]
    [InlineData(SourceKind.Motor,false,false)]
    [InlineData(SourceKind.Motor,true,true)]
    [InlineData(SourceKind.Windmill,true,false)]
    [InlineData(SourceKind.Windmill,false,false)]
    [InlineData(SourceKind.Windmill,true,true)]
    public void OwnedRotorMovesOnlyWithSupplyAndConstructionResetIsExact(SourceKind kind,bool supplied,bool rotated)
    {
        var world=World();
        try
        {
            var part=Build(world,kind,supplied,rotated?new(25,40,15):default);
            var saved=Snapshot(world);var id=part.Uid;
            world.Start();
            var body=world.PhysicsAssembly.Body(new(part,Rotor(kind)));
            var guide=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(part,Guide(kind))));
            Assert.Same(body,guide.A);Assert.Equal(PhysicsMotionType.Dynamic,body.MotionType);
            var initial=body.Pose;
            for(var i=0;i<180;i++)
            {
                world.Step();
                if(part is WindmillPart mill)CheckResistance(world,mill);
                Assert.InRange(Math.Abs(Speed(guide)-ObservedSpeed(part)),0,1e-6);
                Assert.InRange(guide.Error(1e-8),0,1.01e-7);
            }
            Assert.Equal(supplied,Math.Abs(Speed(guide))>1);
            Assert.Equal(supplied,Travel(part)>1);
            if(supplied) Assert.NotEqual(initial.Rotation,body.Pose.Rotation);
            else Assert.InRange(body.KineticEnergy,0,1e-14);
            if(part is MotorPart motor)
                Assert.InRange(body.KineticEnergy,0,motor.SuppliedWork+1e-7);
            var observed=ObservedSpeed(part);var pose=body.Pose;
            part._Process(.5);
            Assert.Equal(observed,ObservedSpeed(part));Assert.Equal(pose,body.Pose);
            world.Restore();
            Assert.Equal(saved,Snapshot(world));
            var reset=world.FindPart(id)!;
            Assert.Equal(0,ObservedSpeed(reset));Assert.Equal(0,Travel(reset));
            world.LoadMachine(JsonSerializer.Deserialize(saved,MachineJson.Default.MachineData)!);
            Assert.Equal(saved,Snapshot(world));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(SourceKind.Motor)]
    [InlineData(SourceKind.Windmill)]
    public void SharedGuideLockBlocksTheDrivenRotorWithoutAnIndependentAnimation(SourceKind kind)
    {
        var world=World();
        try
        {
            var part=Build(world,kind,true,default);world.Start();
            var guide=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(new(part,Guide(kind))));
            var locked=new PhysicsFrameJoint(guide.Id,guide.Kind,guide.A,guide.LocalA,guide.B,guide.LocalB,
                guide.Collision,new(0,0),guide.Direction);
            world.Physics.ReplaceJoints(world.Physics.Joints.ToArray().Select(j=>j.Id==guide.Id?locked:j));
            var initial=guide.A.Pose;
            for(var i=0;i<20;i++) world.Step();
            Assert.InRange(Math.Abs(ObservedSpeed(part)),0,1e-7);
            Assert.InRange(Travel(part),0,1e-7);
            Assert.InRange((initial.Center-guide.A.Center).Length,0,1e-7);
            world.Physics.ReplaceJoints(world.Physics.Joints.ToArray().Select(j=>j.Id==guide.Id?guide:j));
            for(var i=0;i<20;i++) world.Step();
            Assert.True(Math.Abs(ObservedSpeed(part))>.1);
        }
        finally {world.Free();}
    }

    [Fact]
    public void DisconnectedMotorPreservesAnAppliedShaftImpulseWithoutCreatingWork()
    {
        var world=World();
        try
        {
            var motor=(MotorPart)Build(world,SourceKind.Motor,false,default);world.Start();
            var body=world.PhysicsAssembly.Body(new(motor,MotorPart.ShaftBody));
            world.Physics.ApplyImpulse(body.Id,new(0,-.01,0),body.Center+new CollisionVector(1,0,0));
            world.Step();
            var speed=motor.ShaftSpeed;var energy=body.KineticEnergy;
            for(var i=0;i<60;i++) world.Step();
            Assert.True(speed>0);Assert.InRange(Math.Abs(motor.ShaftSpeed-speed),0,1e-6);
            Assert.InRange(Math.Abs(body.KineticEnergy-energy),0,1e-8);
            Assert.Equal(0,motor.SuppliedWork);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(AirDirection.Forward,9f)]
    [InlineData(AirDirection.Reverse,9f)]
    [InlineData(AirDirection.Forward,40f)]
    [InlineData(AirDirection.Reverse,40f)]
    public void SignedAirTorqueDrivesTheRotorAndCalmAirDissipatesItsEnergy(AirDirection direction,float airForce)
    {
        var sign=direction switch
        {
            AirDirection.Forward=>1,AirDirection.Reverse=>-1,
            _=>throw new ArgumentOutOfRangeException(nameof(direction))
        };
        var world=World();
        try
        {
            var mill=(WindmillPart)Add(world,Fixture.Windmill,new(0,6,0));
            var fan=Add(world,Fixture.Fan,new(-3*sign,6,0),new(0,sign<0?180:0,0));
            FixtureParts.ConfigureParameter(fan,FanParameter.Force,airForce);
            world.Start();
            for(var i=0;i<120;i++) world.Step();
            var body=world.PhysicsAssembly.Body(new(mill,WindmillPart.RotorBody));
            CheckResistance(world,mill);
            Assert.True(mill.AxialForce*sign>0);
            Assert.InRange(mill.ShaftSpeed*sign,1,WindmillPart.MaximumSpeed);
            var guide=(PhysicsFrameJoint)world.CurrentJoint(new(mill,WindmillPart.RotorJoint));
            var axis=guide.FrameB.Orientation.Apply(new(0,0,1));
            var inverseInertia=CollisionVector.Dot(axis,body.InverseInertia(axis));
            var target=Math.Clamp(sign*airForce*(double)mill.ReadParameter(WindmillParameter.RadiansPerForce),
                -WindmillPart.MaximumSpeed,WindmillPart.MaximumSpeed);
            var damping=Math.Abs(airForce*(double)WindmillPart.TorqueArm/target);
            var halfDecay=damping*inverseInertia*(MachineWorld.Tick/MachineWorld.Substeps)*.5;
            var factor=(1-halfDecay)/(1+halfDecay);
            var expected=target*(1-Math.Pow(factor,120*MachineWorld.Substeps));
            Assert.InRange(Math.Abs(mill.ShaftSpeed-expected),0,1e-6);
            var energy=body.KineticEnergy;var speed=mill.ShaftSpeed;
            fan.Active=false;world.Step();
            CheckResistance(world,mill);
            Assert.Equal(0,mill.AxialForce);
            Assert.True(mill.ShaftSpeed*sign>0);Assert.True(Math.Abs(mill.ShaftSpeed)<Math.Abs(speed));
            for(var i=0;i<120;i++) world.Step();
            Assert.InRange(body.KineticEnergy,0,energy*.1);
        }
        finally {world.Free();}
    }

    [Fact]
    public void MotorAccelerationIsBoundedByDeclaredTorqueAndInertia()
    {
        var world=World();
        try
        {
            var motor=(MotorPart)Build(world,SourceKind.Motor,true,default);
            FixtureParts.ConfigureParameter(motor,MotorParameter.Torque,.01f);
            world.Start();world.Step();
            var guide=(PhysicsFrameJoint)world.CurrentJoint(new(motor,MotorPart.ShaftJoint));
            var axis=guide.FrameA.Orientation.Apply(new(0,0,1));
            var acceleration=.01*CollisionVector.Dot(axis,guide.A.InverseInertia(axis));
            Assert.InRange(Math.Abs(motor.ShaftSpeed-acceleration*MachineWorld.Tick),0,1e-8);
            Assert.InRange(guide.A.KineticEnergy,0,motor.SuppliedWork+1e-10);
        }
        finally {world.Free();}
    }
}
