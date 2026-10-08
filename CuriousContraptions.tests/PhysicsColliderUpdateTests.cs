using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsColliderUpdateTests
{
    private static readonly ContactMaterial Elastic=new(1,0,0);
    private static CompoundGeometry Sphere()=>new([new(new ConvexSphere(.5),AffineTransform.Identity)]);
    private static CompoundGeometry WallGeometry(double x=0)=>new([new(new ConvexBox(new(.01,10,10)),
        SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new((float)x,0,0))))]);
    private static PhysicsBody Ball(int id=0,double y=0)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(new(-2,y,0)),new(10,0,0),default,1,new(.1,.1,.1));
    private static PhysicsObject Wall(int id=1,double y=0)=>new(new(new(id),PhysicsMotionType.Static,
        RigidPose.At(new(0,y,0)),default,default),WallGeometry(),Elastic);
    private static PhysicsWorld World(PhysicsBody ball)=>new([],[new(ball,Sphere(),Elastic),Wall()],[],new(default,maximumStep:1));
    private static PhysicsColliderUpdate Change(PhysicsWorld world,int body,CollisionParticipation participation,
        CompoundGeometry? geometry=null,ContactMaterial? material=null)
    {
        var old=world.Collider(new(body)).Declaration;
        return new(new(body),geometry??old.Geometry,material??old.Material,participation);
    }
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-6);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void DisabledColliderKeepsBodyMotionButDoesNotBlockIt(int disabledBody)
    {
        var ball=Ball(); var world=World(ball);
        world.ApplyColliderUpdates([Change(world,disabledBody,CollisionParticipation.Disabled)]);
        world.Step([],[],.3);
        Near(1,ball.Center.X); Near(10,ball.LinearVelocity.X);
        Assert.Empty(world.Impacts.ToArray());
        Assert.Equal(CollisionParticipation.Disabled,world.Collider(new(disabledBody)).Declaration.Participation);
    }

    [Fact]
    public void EnablingColliderRejoinsTheSameSharedCollisionPipeline()
    {
        var ball=Ball(); var world=World(ball);
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Disabled)]);
        world.Step([],[],.1);
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Enabled)]);
        world.Step([],[],.1);
        Assert.Single(world.Impacts.ToArray()); Near(-10,ball.LinearVelocity.X);
        Assert.Equal(2ul,world.Impacts[0].Pair.RevisionB.Value);
        Assert.Equal(1,world.Impacts[0].Pair.B.Index);
    }

    [Fact]
    public void GeometryReplacementUsesNewBoundsAndRevision()
    {
        var ball=Ball(); var world=World(ball);
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Enabled,WallGeometry(2))]);
        world.Step([],[],.3);
        Assert.Empty(world.Impacts.ToArray()); Near(1,ball.Center.X);
        world.Step([],[],.1);
        Assert.Single(world.Impacts.ToArray()); Near(-10,ball.LinearVelocity.X);
        Assert.Equal(1ul,world.Impacts[0].Pair.RevisionB.Value);
        Assert.InRange(world.Impacts[0].Time,.3489,.3491);
    }

    [Fact]
    public void RestoreRecoversBothHistoricalGeometryVersionsAndReplay()
    {
        var ball=Ball(); var world=World(ball); var original=world.Capture();
        var first=world.Step([],[],.2); var firstBody=ball.Snapshot(); var firstHits=world.Impacts.ToArray();
        world.Restore(original);
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Enabled,WallGeometry(2))]);
        var changed=world.Capture(); var second=world.Step([],[],.4);
        var secondBody=ball.Snapshot(); var secondHits=world.Impacts.ToArray();
        world.Restore(original);
        Assert.Equal(0ul,world.Collider(new(1)).Revision.Value);
        Assert.Equal(first,world.Step([],[],.2)); Assert.Equal(firstBody,ball.Snapshot());
        Assert.Equal(firstHits,world.Impacts.ToArray());
        world.Restore(changed);
        Assert.Equal(1ul,world.Collider(new(1)).Revision.Value);
        Assert.Equal(second,world.Step([],[],.4)); Assert.Equal(secondBody,ball.Snapshot());
        Assert.Equal(secondHits,world.Impacts.ToArray());
    }

    [Fact]
    public void SameDeclarationDoesNotInvalidateContactsOrIncrementRevision()
    {
        var ball=Ball(); var world=World(ball); world.Step([],[],.15);
        var retained=world.RetainedContactPairs; Assert.True(retained>0);
        var state=world.Collider(new(1));
        world.ApplyColliderUpdates([state.Declaration]);
        Assert.Equal(state,world.Collider(new(1)));
        Assert.Equal(retained,world.RetainedContactPairs);
    }

    [Fact]
    public void UpdatingOneIslandPreservesUnrelatedCachedPairs()
    {
        var first=Ball(0,-30); var second=Ball(2,30);
        var world=new PhysicsWorld([],[new(first,Sphere(),Elastic),Wall(1,-30),new(second,Sphere(),Elastic),Wall(3,30)],[],
            new(default,maximumStep:1));
        world.Step([],[],.15);
        Assert.Equal(2,world.RetainedContactPairs);
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Disabled)]);
        Assert.Equal(1,world.RetainedContactPairs);
        Assert.Equal(0ul,world.Collider(new(3)).Revision.Value);
    }

    [Fact]
    public void InvalidBatchCannotPartiallyDisableAValidBody()
    {
        var ball=Ball(); var world=World(ball); var original=world.Collider(new(1));
        var valid=Change(world,1,CollisionParticipation.Disabled);
        var foreign=new PhysicsColliderUpdate(new(99),WallGeometry(),Elastic,CollisionParticipation.Disabled);
        Assert.Throws<ArgumentException>(()=>world.ApplyColliderUpdates([valid,foreign]));
        Assert.Equal(original,world.Collider(new(1)));
        Assert.Throws<ArgumentException>(()=>world.ApplyColliderUpdates([valid,valid]));
        Assert.Equal(original,world.Collider(new(1)));
        Assert.Throws<ArgumentException>(()=>world.ApplyColliderUpdates([valid,default]));
        Assert.Equal(original,world.Collider(new(1)));
        world.Step([],[],.2); Near(-10,ball.LinearVelocity.X);
    }

    [Fact]
    public void MaterialReplacementInvalidatesOnlyTheUpdatedContactDeclaration()
    {
        var ball=Ball(); var world=World(ball);
        // One absorbing surface is sufficient. The untouched surface keeps its revision.
        world.ApplyColliderUpdates([Change(world,0,CollisionParticipation.Enabled,material:new(0,0,0))]);
        world.Step([],[],.2);
        Near(0,ball.LinearVelocity.X); Assert.InRange(ball.Center.X,-.5102,-.51);
        Assert.Equal(1ul,world.Impacts[0].Pair.RevisionA.Value);
        Assert.Equal(0ul,world.Impacts[0].Pair.RevisionB.Value);
    }

    [Fact]
    public void CollisionDisabledBodyStillReceivesGravityAndJointReactions()
    {
        var ball=Ball();
        var falling=new PhysicsWorld([],[new(ball,Sphere(),Elastic)],[],new(new(0,-10,0),maximumStep:1));
        falling.ApplyColliderUpdates([Change(falling,0,CollisionParticipation.Disabled)]);
        falling.Step([],[],.1); Near(-1,ball.LinearVelocity.Y); Near(-.05,ball.Center.Y); // y = .5*g*t².
        var held=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,-2,0)),default,default,1,new(.1,.1,.1));
        var anchor=Wall().Body;
        var rope=new PhysicsRopeJoint(new(0),new([new(held,default),new(anchor,default)]),2,ConnectedBodyCollision.Enabled);
        var tethered=new PhysicsWorld([],[new(held,Sphere(),Elastic),new(anchor,Sphere(),Elastic)],[rope],
            new(new(0,-10,0),maximumStep:1));
        tethered.ApplyColliderUpdates([Change(tethered,0,CollisionParticipation.Disabled)]);
        tethered.Step([],[],.1); Near(-2,held.Center.Y); Near(0,held.LinearVelocity.Y);
    }

    [Fact]
    public void FailedStepPreservesEditedDeclarationAndOlderSnapshotStillRestores()
    {
        var ball=Ball(); var left=Wall(2);
        var world=new PhysicsWorld([],[new(ball,Sphere(),Elastic),Wall(),new(left.Body,WallGeometry(-3),Elastic)],[],
            new(default,maximumStep:1,maximumEvents:1));
        var original=world.Capture();
        world.ApplyColliderUpdates([Change(world,1,CollisionParticipation.Enabled,WallGeometry())]);
        var edited=world.Collider(new(1));
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.4));
        Assert.Equal(edited,world.Collider(new(1))); Assert.Equal(1ul,edited.Revision.Value);
        Assert.Equal(original.BodyStates[0],ball.Snapshot());
        Assert.Equal(0,world.RetainedContactPairs); Assert.Equal(0,world.Time);
        world.Restore(original); Assert.Equal(0ul,world.Collider(new(1)).Revision.Value);
    }

    private sealed record NoEffectState:PhysicsImpactEffectState;
    private sealed class ReentrantEdit:PhysicsImpactEffect
    {
        public PhysicsWorld World { get; set; }=null!;
        public ReentrantEdit():base(new(1)) { }
        public override PhysicsImpactEffectState Capture()=>new NoEffectState();
        public override void Restore(PhysicsImpactEffectState state) { Assert.IsType<NoEffectState>(state); }
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            World.ApplyColliderUpdates([Change(World,1,CollisionParticipation.Disabled)]);
            return new([],[],[]);
        }
    }
    [Fact]
    public void UnscheduledColliderMutationDuringImpactRejectsAndRollsBack()
    {
        var ball=Ball(); var effect=new ReentrantEdit();
        var world=new PhysicsWorld([effect],[new(ball,Sphere(),Elastic),Wall()],[],new(default,maximumStep:1));
        effect.World=world;
        var original=world.Capture(); var collider=world.Collider(new(1));
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.2));
        Assert.Equal(collider,world.Collider(new(1))); Assert.Equal(original.BodyStates[0],ball.Snapshot());
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase); Assert.Equal(0,world.RetainedContactPairs);
    }

    private sealed record GateState(int Count):PhysicsImpactEffectState;
    private sealed class GateEffect(PhysicsColliderUpdate update):PhysicsImpactEffect(new(1))
    {
        public GateState State { get; private set; }=new(0);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(GateState)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1);
            return new([],[update],[]);
        }
    }

    [Theory]
    [InlineData(CollisionParticipation.Disabled)]
    [InlineData(CollisionParticipation.Enabled)]
    public void ScheduledGateChangeAffectsRemainingFlightAndRestoresExactly(CollisionParticipation next)
    {
        var ball=Ball(); var left=Wall(2);
        var geometry=WallGeometry(-3);
        var effect=new GateEffect(new(left.Body.Id,geometry,Elastic,next));
        var world=new PhysicsWorld([effect],[new(ball,Sphere(),Elastic),Wall(),new(left.Body,geometry,Elastic)],[],
            new(default,maximumStep:1));
        if(next==CollisionParticipation.Enabled)
            world.ApplyColliderUpdates([Change(world,2,CollisionParticipation.Disabled)]);
        var initial=world.Collider(new(2)); var before=world.Capture();
        var result=world.Step([],[],.4); var after=ball.Snapshot(); var updated=world.Collider(new(2));
        Assert.Equal(1,effect.State.Count);
        Assert.Equal(next,updated.Declaration.Participation);
        Assert.Equal(initial.Revision.Value+1,updated.Revision.Value);
        if(next==CollisionParticipation.Disabled)
        {
            Assert.Equal(1,result.Events); Near(-10,ball.LinearVelocity.X);
            Assert.True(ball.Center.X < -3);
        }
        else
        {
            Assert.Equal(2,result.Events); Near(10,ball.LinearVelocity.X);
            Assert.True(ball.Center.X > -2.49);
        }
        world.Restore(before);
        Assert.Equal(initial,world.Collider(new(2))); Assert.Equal(0,effect.State.Count);
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(result,world.Step([],[],.4)); Assert.Equal(after,ball.Snapshot());
        Assert.Equal(updated,world.Collider(new(2))); Assert.Equal(1,effect.State.Count);
    }

    [Fact]
    public void ForeignScheduledUpdateRollsBackEffectAndContactState()
    {
        var ball=Ball();
        var effect=new GateEffect(new(new(99),WallGeometry(),Elastic,CollisionParticipation.Disabled));
        var world=new PhysicsWorld([effect],[new(ball,Sphere(),Elastic),Wall()],[],new(default,maximumStep:1));
        var before=world.Capture(); var declaration=world.Collider(new(1));
        Assert.Throws<ArgumentException>(()=>world.Step([],[],.2));
        Assert.Equal(0,effect.State.Count); Assert.Equal(declaration,world.Collider(new(1)));
        Assert.Equal(before.BodyStates[0],ball.Snapshot()); Assert.Equal(0,world.RetainedContactPairs);
        Assert.Equal(0,world.Time); Assert.Empty(world.Impacts.ToArray());
    }

    [Fact]
    public void CommandBatchCopiesCallerOwnedArrays()
    {
        PhysicsImpactImpulse[] impulses=[new(new(0),new(1,0,0),default)];
        PhysicsColliderUpdate[] colliders=[new(new(1),WallGeometry(),Elastic,CollisionParticipation.Disabled)];
        var commands=new PhysicsImpactCommands(impulses,colliders,[]);
        var expectedImpulse=impulses[0]; var expectedCollider=colliders[0];
        impulses[0]=default; colliders[0]=default;
        Assert.Equal(expectedImpulse,commands.Impulses[0]); Assert.Equal(expectedCollider,commands.Colliders[0]);
    }

    [Fact]
    public void SolidIntroducedAcrossABodyRejectsTheCompleteIdleBatch()
    {
        var ball=Ball(); var world=World(ball);
        var original=world.Collider(new(1)); var before=ball.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.ApplyColliderUpdates([
            Change(world,0,CollisionParticipation.Enabled,material:new(0,0,0)),
            Change(world,1,CollisionParticipation.Enabled,WallGeometry(-2))]));
        Assert.Equal(original,world.Collider(new(1)));
        Assert.Equal(0ul,world.Collider(new(0)).Revision.Value);
        Assert.Equal(before,ball.Snapshot());
    }

    private sealed class ClosingGateEffect:PhysicsImpactEffect
    {
        public GateState State { get; private set; }=new(0);
        public ClosingGateEffect():base(new(1)) { }
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(GateState)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1);
            return new([],[new(new(2),WallGeometry(context.A.After.Pose.Center.X),Elastic,CollisionParticipation.Enabled)],[]);
        }
    }

    [Fact]
    public void ImpactCannotInsertAGateThroughABodyAndEjectIt()
    {
        var ball=Ball(); var effect=new ClosingGateEffect(); var left=Wall(2);
        var world=new PhysicsWorld([effect],[new(ball,Sphere(),Elastic),Wall(),new(left.Body,WallGeometry(-3),Elastic)],[],
            new(default,maximumStep:1));
        world.ApplyColliderUpdates([Change(world,2,CollisionParticipation.Disabled)]);
        var before=world.Capture(); var gate=world.Collider(new(2));
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.4));
        Assert.Equal(before.BodyStates[0],ball.Snapshot()); Assert.Equal(gate,world.Collider(new(2)));
        Assert.Equal(0,effect.State.Count); Assert.Equal(0,world.Time); Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void ColliderUpdatesRejectUndefinedParticipationAndForeignQueries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsColliderUpdate(new(1),WallGeometry(),Elastic,(CollisionParticipation)99));
        Assert.Throws<ArgumentNullException>(()=>new PhysicsColliderUpdate(new(1),null!,Elastic,CollisionParticipation.Enabled));
        var world=World(Ball());
        Assert.Throws<ArgumentException>(()=>world.Collider(new(99)));
    }
}
