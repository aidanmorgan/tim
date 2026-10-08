using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsImpactJointTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static readonly ContactMaterial Elastic=new(1,0,0);
    private sealed record EffectState(int Count,PhysicsImpactContext? Last):PhysicsImpactEffectState;
    private sealed class Effect(PhysicsBodyId owner,Func<PhysicsImpactContext,PhysicsImpactCommands> commands):PhysicsImpactEffect(owner)
    {
        public EffectState State { get; private set; }=new(0,null);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(EffectState)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1,context);
            return State.Count==1?commands(context):new([],[],[]);
        }
    }
    private static PhysicsBody Ball(int id=0,CollisionVector? center=null,CollisionVector? velocity=null)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center??new(-2,0,0)),velocity??new(10,0,0),default,1,new(.1,.1,.1));
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry geometry)=>new(body,
        new([new(geometry,AffineTransform.Identity)]),Elastic);
    private static PhysicsObject Wall(int id=1,double x=0)=>Object(new(new(id),PhysicsMotionType.Static,
        RigidPose.At(new(x,0,0)),default,default),new ConvexBox(new(.01,10,10)));
    private static PhysicsBody Anchor()=>new(new(11),PhysicsMotionType.Static,RigidPose.At(new(0,30,0)),default,default);
    private static PhysicsObject Mount(PhysicsBody anchor)=>new(anchor,
        new([new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new(2,0,0))))]),Elastic);
    private static PhysicsFrameJoint Slider(PhysicsBody body,PhysicsBody anchor,JointTravelRange? range=null)=>
        new(new(7),FrameJointKind.Slider,body,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,range,JointTravelDirection.Both);
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-7);

    [Fact]
    public void ImpactAttachesAtCurrentPoseStopsRemainingMotionAndReplaysAtEndpoint()
    {
        var ball=Ball(); var wall=Wall();
        var effect=new Effect(wall.Body.Id,c=>new([],[],[PhysicsJointChange.Attach(
            new PhysicsFrameJoint(new(7),FrameJointKind.BallSocket,ball,Origin,wall.Body,
                new(c.A.After.Pose.Center,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both))]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall],[],new(default,maximumStep:1));
        var before=world.Capture();
        var result=world.Step([],[],.2);
        var after=ball.Snapshot(); var context=effect.State.Last!.Value;
        Near(context.A.After.Pose.Center.X,ball.Center.X); Near(0,ball.LinearVelocity.Length);
        Near(-10,context.A.After.LinearVelocity.X);
        Assert.Equal(1,effect.State.Count); Assert.Single(world.Joints.ToArray());
        world.Restore(before); Assert.Empty(world.Joints.ToArray());
        Assert.Equal(result,world.Step([],[],.2)); Assert.Equal(after,ball.Snapshot());
        world.Restore(before); world.Step([],[],context.Impact.Time);
        Assert.Single(world.Joints.ToArray()); Near(0,ball.LinearVelocity.Length);
        Near(context.A.After.Pose.Center.X,ball.Center.X);
    }

    [Theory]
    [InlineData(PhysicsJointChangeKind.Attach)]
    [InlineData(PhysicsJointChangeKind.Detach)]
    public void ColliderAndJointChangesValidateTheCombinedFinalGraph(PhysicsJointChangeKind kind)
    {
        var ball=Ball(); var wall=Wall(); var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor();
        var joint=Slider(load,anchor,new(0,0));
        var loadObject=Object(load,new ConvexSphere(.5));
        var effect=new Effect(wall.Body.Id,_=>new([],
            [new(load.Id,loadObject.Geometry,Elastic,kind==PhysicsJointChangeKind.Attach?CollisionParticipation.Enabled:CollisionParticipation.Disabled)],
            [kind==PhysicsJointChangeKind.Attach?PhysicsJointChange.Attach(joint):PhysicsJointChange.Detach(joint.Id)]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall,loadObject,Object(anchor,new ConvexSphere(.5))],
            kind==PhysicsJointChangeKind.Attach?[]:[joint],new(default,maximumStep:1));
        if(kind==PhysicsJointChangeKind.Attach)
            world.ApplyColliderUpdates([new(load.Id,loadObject.Geometry,Elastic,CollisionParticipation.Disabled)]);
        var before=world.Capture(); var declaration=world.Collider(load.Id);
        world.Step([],[],.2);
        Assert.Equal(kind==PhysicsJointChangeKind.Attach?1:0,world.Joints.Length);
        Assert.Equal(kind==PhysicsJointChangeKind.Attach?CollisionParticipation.Enabled:CollisionParticipation.Disabled,
            world.Collider(load.Id).Declaration.Participation);
        var after=world.Capture();
        world.Restore(before); Assert.Equal(declaration,world.Collider(load.Id));
        Assert.Equal(kind==PhysicsJointChangeKind.Attach?0:1,world.Joints.Length);
        Assert.Equal(0,effect.State.Count);
        world.Step([],[],.2);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ImpactReleasesRopeWhileMissingImpactLeavesLoadHeld(bool hit)
    {
        var ball=Ball(0,new(-2,hit?0:20,0)); var wall=Wall();
        var load=Ball(10,new(0,28,0),default(CollisionVector)); var anchor=Anchor();
        var rope=new PhysicsRopeJoint(new(7),new([new(load,default),new(anchor,default)]),2,ConnectedBodyCollision.Enabled);
        var effect=new Effect(wall.Body.Id,_=>new([],[],[PhysicsJointChange.Detach(rope.Id)]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall,Object(load,new ConvexSphere(.1)),Object(anchor,new ConvexSphere(.1))],
            [rope],new(new(0,-10,0),maximumStep:.01));
        var before=world.Capture(); world.Step([],[],.3);
        if(hit)
        {
            Assert.Empty(world.Joints.ToArray()); Assert.True(load.Center.Y<27.95); Assert.True(load.LinearVelocity.Y < -1);
        }
        else { Assert.Single(world.Joints.ToArray()); Near(28,load.Center.Y); Near(0,load.LinearVelocity.Y); }
        var after=load.Snapshot();
        world.Restore(before); Assert.Same(rope,Assert.Single(world.Joints.ToArray()));
        world.Step([],[],.3); Assert.Equal(after,load.Snapshot());
    }

    [Theory]
    [InlineData(PhysicsJointChangeKind.Detach,true)]
    [InlineData(PhysicsJointChangeKind.Replace,true)]
    [InlineData(PhysicsJointChangeKind.Detach,false)]
    [InlineData(PhysicsJointChangeKind.Replace,false)]
    public void DrivenJointChangeRetainsSpentWorkAndUsesCurrentConstraint(PhysicsJointChangeKind kind,bool hit)
    {
        var ball=Ball(center:new(-2,hit?0:20,0)); var wall=Wall();
        var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor();
        var slider=Slider(load,anchor);
        var effect=new Effect(wall.Body.Id,_=>new([],[],[kind==PhysicsJointChangeKind.Detach?
            PhysicsJointChange.Detach(slider.Id):PhysicsJointChange.Replace(Slider(load,anchor,new(slider.Travel.Error,slider.Travel.Error)))]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall,Object(load,new ConvexSphere(.1)),Mount(anchor)],
            [slider],new(default,maximumStep:.1));
        PhysicsMotorCommand motor=new(slider.Id,10,1,10,1000000);
        var before=world.Capture(); world.Step([],[motor],.3);
        var report=world.MotorUse[0];
        // A held unit force accelerates until the event. Detachment ends force;
        // replacing the guide with a lock retains force, but performs no further work.
        var acceleratingTime=hit?effect.State.Last!.Value.Impact.Time:.3;
        var expectedImpulse=hit&&kind==PhysicsJointChangeKind.Detach?acceleratingTime:.3;
        Near(expectedImpulse,report.AbsoluteImpulse);
        Near(hit&&kind==PhysicsJointChangeKind.Replace?0:acceleratingTime,load.LinearVelocity.Z);
        Near(.5*acceleratingTime*acceleratingTime,report.SuppliedWork);
        Near(10-report.SuppliedWork,report.RemainingWork);
        Assert.Equal(hit?1:0,effect.State.Count);
        if(hit) Assert.InRange(effect.State.Last!.Value.Impact.Time,.1,.2);
        else Assert.Same(slider,Assert.Single(world.Joints.ToArray()));
        Assert.Equal(slider.Id,report.Joint);
        var after=world.Capture();var effectAfter=effect.State;
        world.Restore(before); world.Step([],[motor],.3);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(report,world.MotorUse[0]);Assert.Equal(effectAfter,effect.State);
    }

    [Fact]
    public void DetachThenReattachDoesNotReplenishTheStepMotorBudget()
    {
        var ball=Ball(); var right=Wall(); var left=Wall(2,-3);
        var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor(); var slider=Slider(load,anchor);
        var detach=new Effect(right.Body.Id,_=>new([],[],[PhysicsJointChange.Detach(slider.Id)]));
        var attach=new Effect(left.Body.Id,_=>new([],[],[PhysicsJointChange.Attach(slider)]));
        var world=new PhysicsWorld([detach,attach],[Object(ball,new ConvexSphere(.5)),right,left,
            Object(load,new ConvexSphere(.1)),Mount(anchor)],[slider],new(default,maximumStep:.1));
        var before=world.Capture();
        world.Step([],[new(slider.Id,10,1,.025,1000000)],.5);
        Assert.Equal(1,detach.State.Count); Assert.Equal(1,attach.State.Count);
        Assert.Same(slider,Assert.Single(world.Joints.ToArray()));
        var report=world.MotorUse[0];
        Assert.InRange(report.SuppliedWork,.0249999,.025);
        Near(0,report.RemainingWork);
        Near(Math.Sqrt(.05),load.LinearVelocity.Z);
        Near(Math.Sqrt(.05),report.AbsoluteImpulse);
        var after=load.Snapshot();
        world.Restore(before); world.Step([],[new(slider.Id,10,1,.025,1000000)],.5);
        Assert.Equal(after,load.Snapshot()); Assert.Equal(report,world.MotorUse[0]);
    }

    [Fact]
    public void ActiveMotorCannotChangeFromLinearToAngularSupply()
    {
        var ball=Ball(); var wall=Wall(); var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor();
        var slider=Slider(load,anchor);
        var effect=new Effect(wall.Body.Id,_=>new([],[],[PhysicsJointChange.Replace(
            new PhysicsFrameJoint(slider.Id,FrameJointKind.Hinge,load,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both))]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall,Object(load,new ConvexSphere(.1)),Object(anchor,new ConvexSphere(.1))],
            [slider],new(default,maximumStep:.1));
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[new(slider.Id,10,1,10,1000000)],.3));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Same(slider,Assert.Single(world.Joints.ToArray())); Assert.Equal(0,effect.State.Count);
        Assert.Empty(world.MotorUse.ToArray()); Assert.Equal(0,world.Time);
    }

    public enum InvalidChange { MissingDetach, MissingReplace, DuplicateAttach, ConflictingBatch, Uninitialised, ForeignBody, OffAxis }

    [Theory]
    [InlineData(InvalidChange.MissingDetach)]
    [InlineData(InvalidChange.MissingReplace)]
    [InlineData(InvalidChange.DuplicateAttach)]
    [InlineData(InvalidChange.ConflictingBatch)]
    [InlineData(InvalidChange.Uninitialised)]
    [InlineData(InvalidChange.ForeignBody)]
    [InlineData(InvalidChange.OffAxis)]
    public void InvalidScheduledChangeRollsBackTheWholeStep(InvalidChange invalid)
    {
        var ball=Ball(); var wall=Wall(); var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor();
        var slider=Slider(load,anchor);
        PhysicsJointChange[] changes=invalid switch
        {
            InvalidChange.MissingDetach=>[PhysicsJointChange.Detach(slider.Id)],
            InvalidChange.MissingReplace=>[PhysicsJointChange.Replace(slider)],
            InvalidChange.DuplicateAttach=>[PhysicsJointChange.Attach(slider)],
            InvalidChange.ConflictingBatch=>[PhysicsJointChange.Attach(slider),PhysicsJointChange.Detach(slider.Id)],
            InvalidChange.Uninitialised=>[default],
            InvalidChange.ForeignBody=>[PhysicsJointChange.Attach(Slider(Ball(10,new(0,30,0),default(CollisionVector)),anchor))],
            InvalidChange.OffAxis=>[PhysicsJointChange.Attach(Slider(ball,anchor))],
            _=>throw new ArgumentOutOfRangeException(nameof(invalid))
        };
        var effect=new Effect(wall.Body.Id,_=>new([],[],changes));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),wall,Object(load,new ConvexSphere(.1)),Object(anchor,new ConvexSphere(.1))],
            invalid==InvalidChange.DuplicateAttach?[slider]:[],new(default,maximumStep:1));
        // The control load overlaps its carrier only in the declared linked case.
        if(invalid!=InvalidChange.DuplicateAttach)
        {
            var c=world.Collider(load.Id).Declaration;
            world.ApplyColliderUpdates([new(load.Id,c.Geometry,c.Material,CollisionParticipation.Disabled)]);
        }
        var before=world.Capture();
        if(invalid==InvalidChange.OffAxis) Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.2));
        else Assert.Throws<ArgumentException>(()=>world.Step([],[],.2));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(invalid==InvalidChange.DuplicateAttach?1:0,world.Joints.Length);
        Assert.Equal(0,effect.State.Count); Assert.Equal(0,world.Time); Assert.Equal(0,world.RetainedContactPairs);
    }


    [Fact]
    public void SimultaneousEffectsCannotSilentlyOverrideEachOthersJointChanges()
    {
        var ball=Ball(); var first=Wall(); var second=Wall(2);
        var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor(); var slider=Slider(load,anchor);
        var a=new Effect(first.Body.Id,_=>new([],[],[PhysicsJointChange.Detach(slider.Id)]));
        var b=new Effect(second.Body.Id,_=>new([],[],[PhysicsJointChange.Detach(slider.Id)]));
        var world=new PhysicsWorld([b,a],[Object(ball,new ConvexSphere(.5)),first,second,Object(load,new ConvexSphere(.1)),Mount(anchor)],
            [slider],new(default,maximumStep:1));
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.Step([],[],.2));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Same(slider,Assert.Single(world.Joints.ToArray()));
        Assert.Equal(0,a.State.Count); Assert.Equal(0,b.State.Count);
        Assert.Equal(0,world.Time); Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void LaterFailureRestoresAlreadyCommittedJointAndColliderChanges()
    {
        var ball=Ball(); var right=Wall(); var left=Wall(2,-3);
        var load=Ball(10,new(0,30,0),default(CollisionVector)); var anchor=Anchor(); var slider=Slider(load,anchor);
        var loadObject=Object(load,new ConvexSphere(.1));
        var effect=new Effect(right.Body.Id,_=>new([],
            [new(load.Id,loadObject.Geometry,Elastic,CollisionParticipation.Disabled)],[PhysicsJointChange.Attach(slider)]));
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),right,left,loadObject,Mount(anchor)],[],
            new(default,maximumStep:1,maximumEvents:1));
        var before=world.Capture(); var collider=world.Collider(load.Id);
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.4));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Empty(world.Joints.ToArray()); Assert.Equal(collider,world.Collider(load.Id));
        Assert.Equal(0,effect.State.Count); Assert.Equal(0,world.Time);
        Assert.Empty(world.Impacts.ToArray()); Assert.Equal(0,world.RetainedContactPairs);
        // Same declarations succeed when the requested interval ends before the later failure.
        world.Step([],[],.2);
        Assert.Same(slider,Assert.Single(world.Joints.ToArray()));
        Assert.Equal(CollisionParticipation.Disabled,world.Collider(load.Id).Declaration.Participation);
        Assert.Equal(1,effect.State.Count);
    }

    [Fact]
    public void JointCommandArrayIsCopiedAndNullDeclarationsReject()
    {
        PhysicsJointChange[] source=[PhysicsJointChange.Detach(new(8))];
        var commands=new PhysicsImpactCommands([],[],source);
        source[0]=default;
        Assert.Equal(PhysicsJointChangeKind.Detach,commands.Joints[0].Kind);
        Assert.Equal(new PhysicsJointId(8),commands.Joints[0].Id);
        Assert.Throws<ArgumentNullException>(()=>PhysicsJointChange.Attach(null!));
        Assert.Throws<ArgumentNullException>(()=>PhysicsJointChange.Replace(null!));
    }
}
