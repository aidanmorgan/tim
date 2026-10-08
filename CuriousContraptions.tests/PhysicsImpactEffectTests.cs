using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsImpactEffectTests
{
    [Fact]
    public void ImpactCallbackCarriesResolvedPointVelocity()
    {
        var ball=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,0,0)),
            new(10,0,0),new(0,0,3),1,new(.1,.1,.1));
        CallbackEffect effect=null!;
        effect=new(new(1),()=>
        {
            var context=effect.State.Last!.Value;
            var sample=context.A.After.Id==ball.Id?context.A:context.B;
            var point=ball.Center+new CollisionVector(0,.25,0);
            Assert.Equal(ball.AngularVelocity,sample.AngularVelocity);
            Assert.Equal(ball.PointVelocity(point),sample.PointVelocity(point));
            Assert.NotEqual(sample.After.LinearVelocity,sample.PointVelocity(point));
        });
        var world=new PhysicsWorld([effect],[Object(ball,new ConvexSphere(.5)),Wall()],[],new(default,maximumStep:1));
        var initial=world.Capture();
        world.Step([],[],.2);
        Assert.Equal(1,effect.State.Count);
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(initial);world.Step([],[],.2);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Equal(1,effect.State.Count);
    }
    public enum ReentrantOperation { Impulse, AngularImpulse, PoweredImpulse, Step, Capture, Restore, Joints, Surfaces, Colliders, ElasticLoads, EffortLoads, DampingLoads, DragLoads, CompliantLoads, GuideLoads, TransferLoads, RotaryLoads, BodyImpulse, BodyWrench, BodyVelocity, BodyAdvance, BodyRestore }
    private sealed class CallbackEffect(PhysicsBodyId owner,Action callback):PhysicsImpactEffect(owner)
    {
        public State State { get; private set; }=new(0,null);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(State)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1,context);
            callback();
            return new([],[],[]);
        }
    }

    public static TheoryData<ReentrantOperation,bool> ReentrantCases()
    {
        var cases=new TheoryData<ReentrantOperation,bool>();
        foreach(var operation in Enum.GetValues<ReentrantOperation>())
        foreach(var handled in new[]{false,true}) cases.Add(operation,handled);
        return cases;
    }

    [Theory]
    [MemberData(nameof(ReentrantCases))]
    public void SharedWorldRejectsReentrantMutationAndPreservesTransactionalState(
        ReentrantOperation operation,bool handled)
    {
        var ball=Ball();
        PhysicsWorld world=null!;
        PhysicsWorld.Snapshot initial=null!;
        var calls=0;
        void Mutate()
        {
            switch(operation)
            {
                case ReentrantOperation.BodyImpulse: ball.ApplyImpulse(new(100,0,0),ball.Center);break;
                case ReentrantOperation.BodyWrench: ball.ApplyWrench(new(100,0,0),default,.01);break;
                case ReentrantOperation.BodyVelocity: ball.SetKinematicVelocity(new(100,0,0),default);break;
                case ReentrantOperation.BodyAdvance: ball.Advance(ball.CreateTrajectory(.01,default),.01);break;
                case ReentrantOperation.BodyRestore: ball.Restore(ball.Snapshot());break;
                case ReentrantOperation.PoweredImpulse: world.ApplyPoweredImpulse(ball.Id,new(1,0,0),ball.Center,100,100,100);break;
                case ReentrantOperation.AngularImpulse: world.ApplyAngularImpulse(ball.Id,new(0,0,100));break;
                case ReentrantOperation.Impulse: world.ApplyImpulse(ball.Id,new(100,0,0),ball.Center);break;
                case ReentrantOperation.Step: world.Step([],[],.01);break;
                case ReentrantOperation.Capture: world.Capture();break;
                case ReentrantOperation.Restore: world.Restore(initial);break;
                case ReentrantOperation.Joints: world.ReplaceJoints([]);break;
                case ReentrantOperation.ElasticLoads: world.ReplaceLoads(world.Loads with {Elastic=[]});break;
                case ReentrantOperation.EffortLoads: world.ReplaceLoads(world.Loads with {Efforts=[]});break;
                case ReentrantOperation.DampingLoads: world.ReplaceLoads(world.Loads with {Damping=[]});break;
                case ReentrantOperation.DragLoads: world.ReplaceLoads(world.Loads with {Drag=[]});break;
                case ReentrantOperation.CompliantLoads: world.ReplaceLoads(world.Loads with {Compliant=[]});break;
                case ReentrantOperation.RotaryLoads: world.ReplaceLoads(world.Loads with {Rotary=[]});break;
                case ReentrantOperation.TransferLoads: world.ReplaceLoads(world.Loads with {Transfers=[]});break;
                case ReentrantOperation.GuideLoads: world.ReplaceLoads(world.Loads with {Guides=[]});break;
                case ReentrantOperation.Surfaces: world.ReplaceSurfaces([]);break;
                case ReentrantOperation.Colliders: world.ApplyColliderUpdates([]);break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
        var effect=new CallbackEffect(new(1),()=>
        {
            calls++;
            Assert.Equal(PhysicsWorldPhase.Stepping,world.Phase);
            if(handled) Assert.Throws<InvalidOperationException>(Mutate);
            else Mutate();
        });
        world=new([effect],[Object(ball,new ConvexSphere(.5)),Wall()],[],new(default,maximumStep:1));
        initial=world.Capture();
        if(handled)
        {
            world.Step([],[],.2);
            Assert.Equal(1,calls);
            Assert.Equal(1,effect.State.Count);
            Near(-10,ball.LinearVelocity.X);
            var after=world.Capture();
            var impact=world.Impacts.ToArray();
            world.Restore(initial);
            world.Step([],[],.2);
            Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(impact,world.Impacts.ToArray());
        }
        else
        {
            for(var attempt=0;attempt<2;attempt++)
            {
                Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.2));
                Assert.Equal(attempt+1,calls);
                Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
                Assert.Equal(0,effect.State.Count);
                Assert.Null(effect.State.Last);
                Assert.Equal(0,world.Time);
                Assert.Equal(0ul,world.StepIndex);
                Assert.Equal(0,world.RetainedContactPairs);
                Assert.Empty(world.Impacts.ToArray());
            }
            world.ApplyImpulse(ball.Id,new(-20,0,0),ball.Center);
            world.Step([],[],.01);
            Near(-10,ball.LinearVelocity.X);
        }
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
    }

    public enum EffectMode { Launch, Inward, OffCentre, Observe, Throw, ForeignTarget }
    private sealed record State(int Count,PhysicsImpactContext? Last):PhysicsImpactEffectState;
    private sealed class Effect(PhysicsBodyId owner,PhysicsBodyId target,EffectMode mode):PhysicsImpactEffect(owner)
    {
        public State State { get; private set; }=new(0,null);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=(State)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Count+1,context);
            var body=context.A.After.Id==target?context.A:context.B;
            if(mode==EffectMode.Observe) return new([],[],[]);
            if(mode==EffectMode.Throw) throw new InvalidOperationException("Deliberate effect failure.");
            if(mode==EffectMode.ForeignTarget) return new([new(target,new(-10,0,0),body.After.Pose.Center),new(new(99),default,default)],[],[]);
            var impulse=mode switch
            {
                EffectMode.Launch=>new CollisionVector(-10,0,0),
                EffectMode.Inward=>new CollisionVector(20,0,0),
                EffectMode.OffCentre=>new CollisionVector(0,2,0),
                _=>throw new ArgumentOutOfRangeException(nameof(mode))
            };
            var point=body.After.Pose.Center+(mode==EffectMode.OffCentre?new CollisionVector(0,0,.2):default);
            return new([new(target,impulse,point)],[],[]);
        }
    }
    private static PhysicsBody Ball(int id=0)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(new(-2,0,0)),new(10,0,0),default,1,new(.1,.1,.1));
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry geometry)=>new(body,
        new([new(geometry,AffineTransform.Identity)]),new(1,0,0));
    private static PhysicsObject Wall(int id=1,double x=0)=>Object(new(new(id),PhysicsMotionType.Static,
        RigidPose.At(new(x,0,0)),default,default),new ConvexBox(new(.01,10,10)));
    private static PhysicsWorld World(PhysicsBody ball,Effect effect,int wall=1,int maximumEvents=256,bool secondWall=false)=>
        new([effect],secondWall?[Object(ball,new ConvexSphere(.5)),Wall(wall),Wall(2,-3)]:
            [Object(ball,new ConvexSphere(.5)),Wall(wall)],[],new(default,maximumStep:1,maximumEvents:maximumEvents));
    private static void Near(double expected,double actual,double tolerance=1e-7)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EffectSeesOrdinaryResponseAndChangesRemainingFlightUnderBothBodyOrders(bool reverse)
    {
        var ball=Ball(reverse?1:0); var wall=reverse?0:1;
        var effect=new Effect(new(wall),ball.Id,EffectMode.Launch);
        var world=World(ball,effect,wall);
        world.Step([],[],.2);
        Assert.Equal(1,effect.State.Count);
        var context=effect.State.Last!.Value;
        var impacted=context.A.After.Id==ball.Id?context.A:context.B;
        Near(10,impacted.Before.LinearVelocity.X); Near(-10,impacted.After.LinearVelocity.X);
        Near(10,context.ApproachSpeed); Near(-20,ball.LinearVelocity.X);
        var expected=impacted.After.Pose.Center.X-20*(.2-context.Impact.Time);
        Near(expected,ball.Center.X);
        Assert.InRange(context.Impact.Time,.1489,.1491);
        Assert.True(ball.Center.X < -1.5);
    }

    [Fact]
    public void RestoreReplaysEffectCooldownStateBodyAndImpactTimesExactly()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var world=World(ball,effect); var before=world.Capture();
        var result=world.Step([],[],.2); var after=ball.Snapshot(); var state=effect.State;
        var impacts=world.Impacts.ToArray();
        world.Restore(before);
        Assert.Equal(0,effect.State.Count); Assert.Null(effect.State.Last);
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(result,world.Step([],[],.2));
        Assert.Equal(after,ball.Snapshot()); Assert.Equal(state,effect.State);
        Assert.Equal(impacts,world.Impacts.ToArray());
    }

    [Fact]
    public void EffectCannotPushThroughTheContactThatTriggeredIt()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Inward);
        var world=World(ball,effect);
        world.Step([],[],.2);
        Assert.Equal(1,effect.State.Count);
        Assert.True(ball.Center.X<=-.51+1e-6);
        Assert.True(ball.LinearVelocity.X<=1e-8);
    }

    [Fact]
    public void OffCentreEffectUsesTheSameAngularMomentumUpdate()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.OffCentre);
        var world=World(ball,effect); world.Step([],[],.2);
        Near(2,ball.LinearVelocity.Y);
        Near(-.4,ball.AngularMomentum.X);
        Near(0,ball.AngularMomentum.Y); Near(0,ball.AngularMomentum.Z);
        Assert.True(ball.Center.Y>.1);
    }

    [Theory]
    [InlineData(EffectMode.Throw)]
    [InlineData(EffectMode.ForeignTarget)]
    public void RejectedEffectRestoresBodyCacheClockAndComponentState(EffectMode mode)
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,mode);
        var world=World(ball,effect); var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.2));
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(0,effect.State.Count); Assert.Null(effect.State.Last);
        Assert.Equal(0,world.RetainedContactPairs);
        Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        Assert.Empty(world.Impacts.ToArray());
    }

    [Fact]
    public void LaterCollisionFailureRollsBackAnAlreadyAppliedEffect()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var world=World(ball,effect,maximumEvents:1,secondWall:true);
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.4));
        Assert.Equal(before.BodyStates[0],ball.Snapshot());
        Assert.Equal(0,effect.State.Count); Assert.Null(effect.State.Last);
        Assert.Equal(0,world.Time); Assert.Empty(world.Impacts.ToArray());
        Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void ClearMotionDoesNotActivateAnEffect()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var world=World(ball,effect); world.Step([],[],.1);
        Assert.Equal(0,effect.State.Count); Near(-1,ball.Center.X);
    }

    [Fact]
    public void SimultaneousContactsNotifyBothComponentsAfterOneCoupledResponse()
    {
        var ball=Ball();
        var first=new Effect(new(1),ball.Id,EffectMode.Observe);
        var second=new Effect(new(2),ball.Id,EffectMode.Observe);
        var world=new PhysicsWorld([second,first],[Object(ball,new ConvexSphere(.5)),Wall(1),Wall(2)],[],new(default,maximumStep:1));
        world.Step([],[],.2);
        Assert.Equal(1,first.State.Count); Assert.Equal(1,second.State.Count);
        Near(first.State.Last!.Value.Impact.Time,second.State.Last!.Value.Impact.Time);
        Near(-10,first.State.Last.Value.A.After.LinearVelocity.X);
        Near(-10,second.State.Last.Value.A.After.LinearVelocity.X);
        Near(10,first.State.Last.Value.ApproachSpeed); Near(10,second.State.Last.Value.ApproachSpeed);
    }

    [Fact]
    public void InitiallyTouchingClosingBodyReceivesEffectWithoutAZeroTimeSweep()
    {
        var ball=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-.51,0,0)),new(10,0,0),default,1,new(.1,.1,.1));
        var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var world=World(ball,effect); world.Step([],[],.1);
        Assert.Equal(1,effect.State.Count); Near(0,effect.State.Last!.Value.Impact.Time);
        Near(-20,ball.LinearVelocity.X); Near(-2.51,ball.Center.X);
    }

    [Fact]
    public void EndpointImpactStillAppliesItsEffectBeforeReturning()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var world=World(ball,effect); var before=world.Capture();
        world.Step([],[],.2); var arrival=effect.State.Last!.Value.Impact.Time;
        world.Restore(before); world.Step([],[],arrival);
        Assert.Equal(1,effect.State.Count); Near(-20,ball.LinearVelocity.X);
        Near(arrival,world.Time); Assert.InRange(ball.Center.X,-.5102,-.51);
    }

    [Fact]
    public void PersistentContactDoesNotRetriggerButReleaseAndRecontactDoes()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Inward);
        var world=World(ball,effect);
        world.Step([],[],.2);
        for(var i=0;i<100;i++) world.Step([],[],.01);
        Assert.Equal(1,effect.State.Count);
        var returning=Ball(); var repeated=new Effect(new(1),returning.Id,EffectMode.Launch);
        var corridor=World(returning,repeated,secondWall:true);
        corridor.Step([],[],.35);
        Assert.Equal(2,repeated.State.Count); Near(-30,returning.LinearVelocity.X);
    }

    [Fact]
    public void InvalidEffectOwnershipAndDuplicateSubscriptionsReject()
    {
        var ball=Ball(); var effect=new Effect(new(1),ball.Id,EffectMode.Launch);
        var objects=new[]{Object(ball,new ConvexSphere(.5)),Wall()};
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([effect,effect],objects,[],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([new Effect(new(99),ball.Id,EffectMode.Launch)],objects,[],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([null!],objects,[],new(default)));
    }
}
