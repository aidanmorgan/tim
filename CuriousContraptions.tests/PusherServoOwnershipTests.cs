using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PusherServoOwnershipTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(0f)]
    [InlineData(37f)]
    public void CapturedControllerReplaysWithoutSceneCallbacksAndRejectsMetadataMutation(float angle)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var pusher=(LinearPusherPart)world.AddPart(new(){Id=LinearPusherPart.CatalogId,Kind=LinearPusherPart.CatalogId,
                Position=[0,6,0],Orientation=PartOrientation.FromEulerDegrees(angle,angle,angle)});
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var stroke=pusher.ReadParameter(PusherParameter.Stroke);
            var speed=pusher.ReadParameter(PusherParameter.Speed);
            var acceleration=pusher.ReadParameter(PusherParameter.Acceleration);
            var force=pusher.ReadParameter(PusherParameter.Force);
            world.Start();
            var id=world.PhysicsAssembly.JointId(new(pusher,LinearPusherPart.HeadGuide));
            var installed=Assert.Single(world.Physics.Servos.ToArray());
            Assert.Equal(id,installed.Declaration.Joint);
            Assert.Equal(stroke,installed.Range.Upper);Assert.Equal(0,installed.Range.Lower);
            Assert.Equal(speed,installed.Declaration.MaximumSpeed);
            Assert.Equal(acceleration,installed.Declaration.Acceleration);
            Assert.Equal(force,installed.Declaration.MaximumEffort);
            Assert.Equal((double)force*speed,installed.Declaration.MaximumPower);
            var initial=world.Physics.Capture();
            // Runtime author metadata cannot redefine the captured servo.
            Assert.Throws<InvalidOperationException>(()=>FixtureParts.ConfigureParameter(pusher,PusherParameter.Stroke,.25f));
            Assert.Throws<InvalidOperationException>(()=>FixtureParts.ConfigureParameter(pusher,PusherParameter.Speed,4));
            Assert.Throws<InvalidOperationException>(()=>FixtureParts.ConfigureParameter(pusher,PusherParameter.Acceleration,30));
            Assert.Throws<InvalidOperationException>(()=>FixtureParts.ConfigureParameter(pusher,PusherParameter.Force,100));
            world.Physics.SetServoMode(id,PhysicsServoMode.Upper);
            world.Physics.Step([],[],.2);
            Assert.Equal(speed,world.Physics.Servo(id).CommandSpeed);
            Assert.True(pusher.Extension>.25f);Assert.False(pusher.Extended);
            var moving=world.Physics.Capture();
            world.Physics.Step([],[],.03);var future=world.Physics.Capture();
            world.Physics.SetServoMode(id,PhysicsServoMode.Hold);
            var locked=pusher.Extension;
            world.Physics.Step([],[],.03);
            Assert.Equal(locked,pusher.Extension);Assert.InRange(Math.Abs(pusher.TravelSpeed),0,1e-8);
            Assert.Equal(0,world.Physics.Servo(id).CommandSpeed);
            world.Physics.Restore(moving);world.Physics.Step([],[],.03);
            Assert.Equal(future.Servos.ToArray(),world.Physics.Servos.ToArray());
            Assert.Equal(future.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(future.MotorTotals.ToArray(),world.Physics.MotorTotals.ToArray());
            world.Physics.Restore(initial);world.Physics.Step([],[],.03);
            Assert.Equal(PhysicsServoMode.Hold,world.Physics.Servo(id).Mode);
            Assert.True(pusher.Retracted);Assert.Equal(0,pusher.DeliveredWork);
            Assert.Same(installed.Declaration,world.Physics.Servo(id).Declaration);
            world.Restore();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }
}
