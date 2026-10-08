using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class AxialMotionOwnershipTests(NativeSceneFixture godot)
{
    public enum Mechanism { Motor, Windmill, Conveyor, Pusher, Gate, Shutter }
    private static string Catalogue(Mechanism mechanism)=>mechanism switch
    {
        Mechanism.Motor=>"motor",Mechanism.Windmill=>"windmill",Mechanism.Conveyor=>"conveyor",
        Mechanism.Pusher=>LinearPusherPart.CatalogId,Mechanism.Gate=>"powered_gate",Mechanism.Shutter=>"beam_shutter",
        _=>throw new ArgumentOutOfRangeException(nameof(mechanism))
    };
    private static JointSlot Slot(Mechanism mechanism)=>mechanism switch
    {
        Mechanism.Motor=>MotorPart.ShaftJoint,Mechanism.Windmill=>WindmillPart.RotorJoint,
        Mechanism.Conveyor=>ConveyorPart.ShaftJoint,Mechanism.Pusher=>LinearPusherPart.HeadGuide,
        Mechanism.Gate=>PoweredGatePart.BladeGuide,Mechanism.Shutter=>BeamShutterPart.BladeGuide,
        _=>throw new ArgumentOutOfRangeException(nameof(mechanism))
    };
    private static PhysicsAxialMotion Observed(MachinePart part)=>part switch
    {
        MotorPart motor=>new(-motor.ShaftAngle,-motor.ShaftSpeed),
        WindmillPart mill=>new(mill.ShaftAngle,mill.ShaftSpeed),
        ConveyorPart conveyor=>new(-conveyor.ShaftAngle,-conveyor.ShaftSpeed),
        LinearPusherPart pusher=>new(pusher.Extension,pusher.TravelSpeed),
        PoweredGatePart gate=>new(gate.Opening,gate.BladeSpeed),
        BeamShutterPart shutter=>new(shutter.Opening,shutter.BladeSpeed),
        _=>throw new ArgumentException("Expected an axial mechanism.",nameof(part))
    };
    private static void Check(MachinePart part,PhysicsFrameJoint joint)
    {
        var actual=Observed(part);var expected=joint.Motion;
        Assert.Equal((float)expected.Coordinate,(float)actual.Coordinate);
        Assert.Equal((float)expected.Speed,(float)actual.Speed);
        var world=(MachineWorld)part.GetParent();
        if(part is MotorPart motor)
            Assert.Equal((float)world.Physics.AngularTravel(joint.Id).Distance,motor.ShaftTravel);
        if(part is WindmillPart mill)
            Assert.Equal((float)world.Physics.AngularTravel(joint.Id).Distance,mill.ShaftTravel);
    }

    [Theory]
    [InlineData(Mechanism.Motor,0f)]
    [InlineData(Mechanism.Motor,37f)]
    [InlineData(Mechanism.Windmill,0f)]
    [InlineData(Mechanism.Windmill,37f)]
    [InlineData(Mechanism.Conveyor,0f)]
    [InlineData(Mechanism.Conveyor,37f)]
    [InlineData(Mechanism.Pusher,0f)]
    [InlineData(Mechanism.Pusher,37f)]
    [InlineData(Mechanism.Gate,0f)]
    [InlineData(Mechanism.Gate,37f)]
    [InlineData(Mechanism.Shutter,0f)]
    [InlineData(Mechanism.Shutter,37f)]
    public void MotionReadsCurrentOwnedJointWithoutPresentationAndRestores(Mechanism mechanism,float angle)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=world.AddPart(new(){Id=Catalogue(mechanism),Kind=Catalogue(mechanism),
                Position=[0,8,0],Orientation=PartOrientation.FromEulerDegrees(angle,angle,angle)});
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            Assert.Equal(default,Observed(part));
            world.Start();
            var key=new SceneJointKey(part,Slot(mechanism));
            var joint=(PhysicsFrameJoint)world.CurrentJoint(key);
            var initial=world.Physics.Capture();var initialReading=Observed(part);
            Check(part,joint);
            world.Physics.Step([],[],.01);
            Assert.InRange(Math.Abs(Observed(part).Speed),0,1e-10); // Unpowered control.
            if(mechanism==Mechanism.Pusher)
            {
                world.Physics.SetServoMode(joint.Id,PhysicsServoMode.Upper);
                world.Physics.Step([],[],.05);
                joint=(PhysicsFrameJoint)world.CurrentJoint(key);
            }
            else world.Physics.Step([],[new(joint.Id,1,100,10,1000000)],.05);
            Assert.True(joint.Motion.Coordinate>.01);Assert.True(joint.Motion.Speed>.1);
            Check(part,joint); // No part callback has run.
            var moved=world.Physics.Capture();var movedReading=Observed(part);
            if(part is PoweredGatePart gate)Assert.Equal(GateState.Closing,gate.State);
            if(part is BeamShutterPart shutter)Assert.Equal(GateState.Closing,shutter.State);
            if(part is LinearPusherPart pusher)Assert.False(pusher.Retracted);
            if(part is ConveyorPart conveyor)
            {
                var surface=Assert.Single(world.Physics.Surfaces.ToArray());
                Assert.Equal((float)surface.Speed,conveyor.SurfaceSpeed);
            }
            part.Position+=Vector3.Right*10;
            part.RotationDegrees+=new Vector3(10,20,30);part.Visible=false;
            Check(part,joint); // Scene transform and visibility do not own motion.
            // Same identity, changed frame: reads must resolve the replacement,
            // not the initial declaration retained by the scene assembly.
            var frame=joint.LocalB;
            frame=joint.Kind switch
            {
                FrameJointKind.Slider=>new(frame.Anchor+frame.Orientation.Apply(new(0,0,.02)),frame.Orientation),
                FrameJointKind.Hinge=>new(frame.Anchor,frame.Orientation*RigidRotation.FromRotationVector(new(0,0,.02))),
                _=>throw new InvalidOperationException("Expected an axial joint.")
            };
            var replacement=new PhysicsFrameJoint(joint.Id,joint.Kind,joint.A,joint.LocalA,
                joint.B,frame,joint.Collision,joint.TravelRange,joint.Direction);
            if(mechanism==Mechanism.Pusher)
            {
                Assert.Throws<ArgumentException>(()=>world.Physics.ReplaceJoints([replacement]));
                world.Physics.SetServoMode(joint.Id,PhysicsServoMode.Hold);
                var held=(PhysicsFrameJoint)world.CurrentJoint(key);
                Assert.NotSame(joint,held);Assert.Equal(held.TravelRange!.Lower,held.TravelRange.Upper);
                Check(part,held);
            }
            else
            {
                var surfaces=world.Physics.Surfaces.ToArray();
                world.Physics.ReplaceSurfaces([]);
                world.Physics.ReplaceJoints([replacement]);
                if(part is ConveyorPart unbound)
                    Assert.Throws<InvalidOperationException>(()=>unbound.SurfaceSpeed);
                world.Physics.ReplaceSurfaces(surfaces.Select(surface=>new DrivenSurface(replacement,
                    surface.LocalNormal,surface.LocalDirection,surface.TravelPerRadian*2)));
                if(part is ConveyorPart rebound)
                    Assert.Equal((float)Assert.Single(world.Physics.Surfaces.ToArray()).Speed,rebound.SurfaceSpeed);
                Check(part,replacement);
                Assert.NotEqual(movedReading.Coordinate,Observed(part).Coordinate);
            }
            world.Physics.Restore(initial);Check(part,joint);
            Assert.Equal(initialReading,Observed(part));
            if(part is PoweredGatePart restoredGate)Assert.Equal(GateState.Closed,restoredGate.State);
            if(part is BeamShutterPart restoredShutter)Assert.Equal(GateState.Closed,restoredShutter.State);
            if(part is LinearPusherPart restoredPusher)Assert.True(restoredPusher.Retracted);
            world.Physics.Restore(moved);Assert.Equal(movedReading,Observed(part));
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(default,Observed(Assert.Single(world.Parts)));
        }
        finally{world.Free();}
    }

    [Fact]
    public void FixtureBoundaryRejectsUnknownMechanisms()
    {
        foreach(var mechanism in Enum.GetValues<Mechanism>()){Assert.NotEmpty(Catalogue(mechanism));Assert.NotNull(Slot(mechanism));}
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalogue((Mechanism)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Slot((Mechanism)99));
    }
}
