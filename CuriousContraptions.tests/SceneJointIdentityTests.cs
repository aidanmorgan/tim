using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneJointIdentityTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind="motor",Position=[0,8,0]});
        return world;
    }
    private static MotorPart Motor(MachineWorld world)=>Assert.Single(world.Parts.OfType<MotorPart>());
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SceneIdentityResolvesReplacementDetachAndSnapshotRestoration(bool paused)
    {
        var world=World();
        try
        {
            var saved=Saved(world);
            var motor=Motor(world);
            var key=new SceneJointKey(motor,MotorPart.ShaftJoint);
            world.Start();
            var assembly=world.PhysicsAssembly;
            var id=assembly.JointId(key);
            var original=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(key));
            Assert.Equal(original.Id,id);
            Assert.Same(original,Assert.Single(assembly.InitialJoints.ToArray()));
            var before=world.Physics.Capture();
            var replacement=new PhysicsFrameJoint(id,original.Kind,original.A,original.LocalA,
                original.B,new(original.LocalB.Anchor,original.LocalB.Orientation*
                    RigidRotation.FromRotationVector(new(0,0,.25))),
                original.Collision,original.TravelRange,original.Direction);
            world.Physics.ReplaceJoints([replacement]);
            var replaced=world.Physics.Capture();
            if(paused)world.Running=false;
            Assert.Equal(id,assembly.JointId(key));
            Assert.Same(replacement,world.CurrentJoint(key));
            Assert.Same(original,Assert.Single(assembly.InitialJoints.ToArray()));
            world.DriveMotor(key,2,1,.1,2);
            void Run()
            {
                world.Physics.Step([],[new(id,2,1,.1,2)],.05);
                Assert.True(replacement.A.AngularVelocity.Length>0);
                Assert.Equal(id,Assert.Single(world.Physics.MotorUse.ToArray()).Joint);
            }
            Run();
            var driven=world.Physics.Capture();
            world.Physics.Restore(replaced);Run();
            Assert.Equal(driven.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());

            var collider=world.Physics.Collider(original.A.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(original.A.Id,collider.Geometry,collider.Material,
                CollisionParticipation.Disabled)]);
            world.Physics.ReplaceJoints([]);
            var detached=world.Physics.Capture();
            Assert.Equal(id,assembly.JointId(key)); // Identity is not a live-joint cache.
            Assert.Throws<InvalidOperationException>(()=>world.CurrentJoint(key));
            Assert.Throws<InvalidOperationException>(()=>world.DriveMotor(key,2,1,.1,2));
            Assert.Equal(detached.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Physics.Restore(replaced);
            Assert.Same(replacement,world.CurrentJoint(key));
            world.Physics.Restore(before);
            Assert.Same(original,world.CurrentJoint(key));
            world.Restore();Assert.Equal(saved,Saved(world));
            world.Start();
            Assert.Throws<ArgumentException>(()=>world.CurrentJoint(key));
            var fresh=new SceneJointKey(Motor(world),MotorPart.ShaftJoint);
            Assert.Equal(id,world.PhysicsAssembly.JointId(fresh));
            Assert.NotSame(original,world.CurrentJoint(fresh));
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotorSubmissionValidatesTheCurrentJointKindBeforeQueuing(bool paused)
    {
        var world=World();
        try
        {
            world.Start();
            var key=new SceneJointKey(Motor(world),MotorPart.ShaftJoint);
            var original=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(key));
            var saved=world.Physics.Capture();
            var socket=new PhysicsFrameJoint(original.Id,FrameJointKind.BallSocket,original.A,original.LocalA,
                original.B,original.LocalB,original.Collision,null,JointTravelDirection.Both);
            world.Physics.ReplaceJoints([socket]);
            if(paused)world.Running=false;
            Assert.Same(socket,world.CurrentJoint(key));
            Assert.Throws<ArgumentException>(()=>world.DriveMotor(key,2,1,.1,2));
            Assert.Equal(saved.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Physics.Restore(saved);
            world.DriveMotor(key,2,1,.1,2);
            Assert.Same(original,world.CurrentJoint(key));
        }
        finally {world.Free();}
    }

    [Fact]
    public void UnknownDefaultAndForeignSceneIdentitiesAreRejected()
    {
        var world=World();
        var other=World();
        try
        {
            world.Start();other.Start();
            SceneJointKey[] unknown=[default,new(Motor(world),new JointSlot()),new(Motor(other),MotorPart.ShaftJoint)];
            foreach(var key in unknown)
            {
                Assert.Throws<ArgumentException>(()=>world.PhysicsAssembly.JointId(key));
                Assert.Throws<ArgumentException>(()=>world.CurrentJoint(key));
                Assert.Throws<ArgumentException>(()=>world.DriveMotor(key,2,1,.1,2));
            }
        }
        finally {world.Free();other.Free();}
    }
}
