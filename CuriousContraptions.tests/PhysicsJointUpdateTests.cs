using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsJointUpdateTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center=default,CollisionVector velocity=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body,CollisionVector offset=default)=>new(body,
        new([new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new((float)offset.X,(float)offset.Y,(float)offset.Z))))]),new(0,0,0));
    private static PhysicsFrameJoint Frame(PhysicsBody a,PhysicsBody b,FrameJointKind kind,JointTravelRange? range=null,int id=7)=>
        new(new(id),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,range,JointTravelDirection.Both);
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-7);

    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void RuntimeLatchHoldsReleasesAndRestoresBothConstraintVersions(FrameJointKind kind)
    {
        var body=Body(3); var anchor=Fixed(11); var free=Frame(body,anchor,kind);
        var world=new PhysicsWorld([],[Object(body),Object(anchor,new(10,0,0))],[free],new(default,maximumStep:.01));
        PhysicsWrenchCommand drive=new(body.Id,kind==FrameJointKind.Slider?new(0,0,1):default,
            kind==FrameJointKind.Hinge?new(0,0,.1):default);
        world.Step([drive],[],.1);
        Assert.True(free.Travel.Error>0);
        var coordinate=free.Travel.Error; var pose=body.Pose;
        var released=world.Capture();
        var latch=Frame(body,anchor,kind,new(coordinate,coordinate));
        world.ReplaceJoints([latch]);
        Assert.Equal(pose,body.Pose); Near(.1,world.Time);
        var latched=world.Capture();
        world.Step([drive],[],.1);
        Near(coordinate,latch.Travel.Error); Near(0,body.KineticEnergy);
        var held=body.Snapshot();
        world.ReplaceJoints([free]);
        world.Step([drive],[],.1);
        Assert.True(free.Travel.Error>coordinate);
        world.Restore(latched);
        Assert.Same(latch,Assert.Single(world.Joints.ToArray()));
        world.Step([drive],[],.1);
        Assert.Equal(held,body.Snapshot());
        world.Restore(released);
        Assert.Same(free,Assert.Single(world.Joints.ToArray()));
        Assert.Equal(released.BodyStates[0],body.Snapshot());
        world.Step([drive],[],.1);
        Assert.True(free.Travel.Error>coordinate);
    }

    [Fact]
    public void RemovingRopeReleasesLoadWithoutReplacingWorldOrBody()
    {
        var body=Body(4,new(0,-2,0)); var anchor=Fixed(20);
        var rope=new PhysicsRopeJoint(new(8),new([new(body,default),new(anchor,default)]),2,ConnectedBodyCollision.Enabled);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[rope],new(new(0,-10,0),maximumStep:.01));
        world.Step([],[],.1); Near(-2,body.Center.Y); Near(0,body.LinearVelocity.Y);
        var held=world.Capture();
        world.ReplaceJoints([]);
        world.Step([],[],.1);
        Assert.True(body.Center.Y < -2); Near(-1,body.LinearVelocity.Y);
        var falling=body.Snapshot();
        world.Restore(held);
        Assert.Same(rope,Assert.Single(world.Joints.ToArray()));
        world.Step([],[],.1); Near(-2,body.Center.Y);
        world.Restore(held); world.ReplaceJoints([]); world.Step([],[],.1);
        Assert.Equal(falling,body.Snapshot()); Near(.2,world.Time);
    }

    [Fact]
    public void RuntimeConstraintCannotSnapAnOffAxisBodyIntoPlace()
    {
        var body=Body(0,new(1,0,0)); var anchor=Fixed(1);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[],new(default));
        var before=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.ReplaceJoints([Frame(body,anchor,FrameJointKind.Slider)]));
        Assert.Empty(world.Joints.ToArray()); Assert.Equal(before,body.Snapshot());
        Assert.Equal(0,world.Time);
    }

    [Fact]
    public void RemovingCollisionSuppressionCannotExposeOverlappingSolids()
    {
        var body=Body(0); var anchor=Fixed(1); var joint=Frame(body,anchor,FrameJointKind.Slider);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default));
        var before=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.ReplaceJoints([]));
        Assert.Same(joint,Assert.Single(world.Joints.ToArray())); Assert.Equal(before,body.Snapshot());
        var collider=world.Collider(anchor.Id).Declaration;
        world.ApplyColliderUpdates([new(anchor.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        world.ReplaceJoints([]);
        Assert.Empty(world.Joints.ToArray());
        world.Step([new(body.Id,new(1,0,0),default)],[],.1);
        Assert.True(body.Center.X>0);
        Assert.Throws<InvalidOperationException>(()=>world.ApplyColliderUpdates([collider]));
    }

    [Fact]
    public void AddedSuppressionRemovesRestingCacheAndRemovalRestoresCollision()
    {
        var body=Body(0,new(-1,0,0),new(10,0,0)); var anchor=Fixed(1);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[],new(default,maximumStep:1));
        world.Step([],[],.1);
        Assert.True(world.RetainedContactPairs>0); Near(0,body.LinearVelocity.X);
        var contact=world.Capture();
        var slack=new PhysicsRopeJoint(new(12),new([new(body,default),new(anchor,default)]),10,ConnectedBodyCollision.Disabled);
        world.ReplaceJoints([slack]);
        Assert.Equal(0,world.RetainedContactPairs);
        world.ApplyImpulse(body.Id,new(10,0,0),body.Center);
        world.Step([],[],.1);
        Assert.True(body.Center.X>.7); Near(10,body.LinearVelocity.X);
        world.ReplaceJoints([]);
        world.ApplyImpulse(body.Id,new(-20,0,0),body.Center);
        world.Step([],[],.1);
        Assert.Single(world.Impacts.ToArray()); Near(0,body.LinearVelocity.X);
        Assert.True(body.Center.X>.2);
        world.Restore(contact);
        Assert.Empty(world.Joints.ToArray()); Assert.True(world.RetainedContactPairs>0);
        Assert.Equal(contact.BodyStates[0],body.Snapshot());
    }

    [Fact]
    public void NewConstraintBatchRejectsForeignDuplicateAndNullDeclarationsAtomically()
    {
        var body=Body(0); var anchor=Fixed(1); var original=Frame(body,anchor,FrameJointKind.Slider);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[original],new(default));
        var valid=Frame(body,anchor,FrameJointKind.Hinge);
        var foreign=Frame(Body(0),anchor,FrameJointKind.Slider,id:8);
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([valid,foreign]));
        Assert.Same(original,Assert.Single(world.Joints.ToArray()));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([valid,valid]));
        Assert.Same(original,Assert.Single(world.Joints.ToArray()));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([valid,null!]));
        Assert.Throws<ArgumentNullException>(()=>world.ReplaceJoints(null!));
        Assert.Same(original,Assert.Single(world.Joints.ToArray()));
    }

    [Fact]
    public void JointInputArrayIsCopiedAndOrderedByTypedIdentity()
    {
        var body=Body(0); var anchor=Fixed(1);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[],new(default));
        var first=Frame(body,anchor,FrameJointKind.Slider,id:2);
        var last=Frame(body,anchor,FrameJointKind.Slider,id:19);
        PhysicsJoint[] declarations=[last,first];
        world.ReplaceJoints(declarations); declarations[0]=null!;
        Assert.Equal(new PhysicsJoint[]{first,last},world.Joints.ToArray());
        world.ReplaceJoints(world.Joints.ToArray());
        Assert.Equal(new PhysicsJoint[]{first,last},world.Joints.ToArray());
    }


    [Fact]
    public void RemovedJointCannotStillReceiveMotorWork()
    {
        var body=Body(0); var anchor=Fixed(1); var joint=Frame(body,anchor,FrameJointKind.Slider);
        var world=new PhysicsWorld([],[Object(body),Object(anchor,new(10,0,0))],[joint],new(default));
        world.ReplaceJoints([]);
        var before=body.Snapshot();
        Assert.Throws<ArgumentException>(()=>world.Step([],[new(joint.Id,1,10,10,1000000)],.1));
        Assert.Equal(before,body.Snapshot()); Assert.Equal(0,world.Time); Assert.Empty(world.MotorUse.ToArray());
        world.ReplaceJoints([joint]);
        world.Step([],[new(joint.Id,1,10,10,1000000)],.1);
        Assert.True(body.Center.Z>0); Assert.True(world.MotorUse[0].SuppliedWork>0);
    }

    [Fact]
    public void JointChangesPreserveUnchangedCollisionCachesAndClock()
    {
        var body=Body(0,new(-1,0,0),new(10,0,0)); var anchor=Fixed(1);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[],new(default,maximumStep:1));
        world.Step([],[],.1);
        var retained=world.RetainedContactPairs; Assert.True(retained>0);
        var slack=new PhysicsRopeJoint(new(9),new([new(body,default),new(anchor,default)]),10,ConnectedBodyCollision.Enabled);
        world.ReplaceJoints([slack]);
        Assert.Equal(retained,world.RetainedContactPairs); Near(.1,world.Time); Assert.Equal(1ul,world.StepIndex);
        var before=world.Capture();
        world.ReplaceJoints([slack]);
        Assert.Equal(retained,world.RetainedContactPairs);
        world.Step([],[],.1);
        var result=world.Capture();
        Assert.Empty(world.Impacts.ToArray()); Assert.Equal(before.BodyStates[0],body.Snapshot());
        world.Restore(before); world.Step([],[],.1);
        Assert.Equal(result.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void FailedStepKeepsEditedJointsAndOlderSnapshotStillRestores()
    {
        var body=Body(0,new(-1,0,0),new(10,0,0)); var anchor=Fixed(1); var effect=new ReentrantEffect();
        var world=new PhysicsWorld([effect],[Object(body),Object(anchor)],[],new(default,maximumStep:1));
        effect.World=world;
        var original=world.Capture();
        var slack=new PhysicsRopeJoint(new(9),new([new(body,default),new(anchor,default)]),10,ConnectedBodyCollision.Enabled);
        world.ReplaceJoints([slack]);
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.1));
        Assert.Same(slack,Assert.Single(world.Joints.ToArray()));
        Assert.Equal(original.BodyStates[0],body.Snapshot());
        Assert.Equal(0,world.Time); Assert.Equal(0,world.RetainedContactPairs);
        world.Restore(original);
        Assert.Empty(world.Joints.ToArray());
        Assert.Equal(original.BodyStates[0],body.Snapshot());
    }

    private sealed record EmptyState:PhysicsImpactEffectState;
    private sealed class ReentrantEffect:PhysicsImpactEffect
    {
        public PhysicsWorld World { get; set; }=null!;
        public ReentrantEffect():base(new(1)) { }
        public override PhysicsImpactEffectState Capture()=>new EmptyState();
        public override void Restore(PhysicsImpactEffectState state)=>Assert.IsType<EmptyState>(state);
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            World.ReplaceJoints([]);
            return new([],[],[]);
        }
    }

    [Fact]
    public void DirectJointMutationFromImpactRejectsAndRollsBack()
    {
        var body=Body(0,new(-1,0,0),new(10,0,0)); var anchor=Fixed(1); var effect=new ReentrantEffect();
        var world=new PhysicsWorld([effect],[Object(body),Object(anchor)],[],new(default,maximumStep:1));
        effect.World=world; var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.1));
        Assert.Equal(before.BodyStates[0],body.Snapshot()); Assert.Equal(0,world.Time);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase); Assert.Empty(world.Joints.ToArray());
        Assert.Equal(0,world.RetainedContactPairs);
    }
}
