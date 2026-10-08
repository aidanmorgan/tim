using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class MotorAccountingOwnershipTests(NativeSceneFixture godot)
{
    public enum Actuator { Motor, Pusher }
    private static string Catalogue(Actuator actuator)=>actuator switch
    {
        Actuator.Motor=>"motor",Actuator.Pusher=>LinearPusherPart.CatalogId,
        _=>throw new ArgumentOutOfRangeException(nameof(actuator))
    };
    private static JointSlot Guide(Actuator actuator)=>actuator switch
    {
        Actuator.Motor=>MotorPart.ShaftJoint,Actuator.Pusher=>LinearPusherPart.HeadGuide,
        _=>throw new ArgumentOutOfRangeException(nameof(actuator))
    };
    private static double Work(MachinePart part)=>part switch
    {
        MotorPart motor=>motor.SuppliedWork,LinearPusherPart pusher=>pusher.DeliveredWork,
        _=>throw new ArgumentException("Expected a motor or pusher.",nameof(part))
    };

    [Theory]
    [InlineData(Actuator.Motor,0f)]
    [InlineData(Actuator.Pusher,0f)]
    [InlineData(Actuator.Motor,37f)]
    [InlineData(Actuator.Pusher,37f)]
    public void WorkReadsOwnedPhysicsWithoutCallbacksAndRestoresExactly(Actuator actuator,float angle)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=Catalogue(actuator),Kind=Catalogue(actuator),
                Position=[0,6,0],Orientation=PartOrientation.FromEulerDegrees(angle,angle,angle)});
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            Assert.Equal(0,Work(part));
            world.Start();
            var joint=world.CurrentJoint(new(part,Guide(actuator))).Id;
            var initial=world.Physics.Capture();
            if(actuator==Actuator.Pusher)world.Physics.Step([],[],.01);
            else world.Physics.Step([],[new(joint,1,0,10,1000000)],.01);
            Assert.Equal(0,Work(part)); // A disabled supply cannot report delivered work.
            if(actuator==Actuator.Pusher)
            {
                world.Physics.SetServoMode(joint,PhysicsServoMode.Upper);
                world.Physics.Step([],[],.01);
            }
            else world.Physics.Step([],[new(joint,1,100,10,1000000)],.01);
            var report=Assert.Single(world.Physics.MotorUse.ToArray());
            Assert.True(report.SuppliedWork>0);
            Assert.Equal(report.SuppliedWork,Work(part)); // No ObservePhysics call.
            if(part is LinearPusherPart driven)Assert.Equal(report.AbsoluteImpulse,driven.LastDriveImpulse);
            var powered=world.Physics.Capture();
            part.ObservePhysics(world,.01f);part.ObservePhysics(world,.01f);
            Assert.Equal(report.SuppliedWork,Work(part)); // Repeated observation cannot double count.
            if(actuator==Actuator.Pusher)world.Physics.SetServoMode(joint,PhysicsServoMode.Hold);
            world.Physics.Step([],[],.01);
            Assert.Equal(report.SuppliedWork,Work(part));
            if(part is LinearPusherPart idle)Assert.Equal(0,idle.LastDriveImpulse);
            world.Physics.Restore(initial);
            Assert.Equal(0,Work(part));
            if(part is LinearPusherPart reset)Assert.Equal(0,reset.LastDriveImpulse);
            world.Physics.Restore(powered);
            Assert.Equal(report.SuppliedWork,Work(part));
            if(part is LinearPusherPart replay)Assert.Equal(report.AbsoluteImpulse,replay.LastDriveImpulse);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(0,Work(Assert.Single(world.Parts)));
        }
        finally{world.Free();}
    }

    [Fact]
    public void FixtureBoundaryRejectsUnknownActuators()
    {
        Assert.Equal("motor",Catalogue(Actuator.Motor));
        Assert.Equal(LinearPusherPart.CatalogId,Catalogue(Actuator.Pusher));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalogue((Actuator)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Guide((Actuator)99));
    }
}
