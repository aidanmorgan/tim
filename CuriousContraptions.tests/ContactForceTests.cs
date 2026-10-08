using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactForceTests
{
    public enum WorldCase { DrivenRolling, Sliding }
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.5),AffineTransform.Identity);
    private static readonly ConvexInstance Floor=new(new ConvexBox(new(10,.5,10)),AffineTransform.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Ground()=>new(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
    private static void Near(double expected,double actual,double tolerance=1e-8)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);
    private static ContactGap Gap(PhysicsBody a,PhysicsBody b)=>Assert.Single(ContactGap.Query(a,Sphere,b,Floor,.001,1e-9));

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    [InlineData(2.4)]
    public void SlidingOpposesPhysicalSlipEvenWhenAppliedAccelerationPointsAgainstIt(double angle)
    {
        var direction=new CollisionVector(Math.Cos(angle),0,Math.Sin(angle));
        var a=Body(0,new(0,.5,0),direction*2); var b=Ground();
        var load=new BodyWrench(direction*(-100)+new CollisionVector(0,-10,0),default);
        var before=a.Snapshot();
        var force=ContactForce.FromGap(Gap(a,b),.5,FrictionRegime.Sliding);
        var result=AccelerationSolver.Solve([a,b],[],[force],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,load},{b.Id,default}},1e-9,out _,[],out _,out _,out _);
        var reaction=result[a.Id].Force-load.Force;
        Near(-5,CollisionVector.Dot(reaction,direction)); Near(10,reaction.Y);
        Near(5,(reaction-new CollisionVector(0,10,0)).Length);
        Assert.True(CollisionVector.Dot(reaction,force.PhysicalSlip)<0);
        Assert.Equal(before,a.Snapshot());
    }

    [Theory]
    [InlineData(3,-6.0/7)]
    [InlineData(100,-5)]
    public void StickingBalancesRollingAccelerationUntilTheCircularBudgetSaturates(double drive,double expectedFriction)
    {
        var a=Body(0,new(0,.5,0)); var b=Ground();
        var load=new BodyWrench(new(drive,-10,0),default);
        var force=ContactForce.FromGap(Gap(a,b),.5,FrictionRegime.Sticking);
        var result=AccelerationSolver.Solve([a,b],[],[force],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,load},{b.Id,default}},1e-9,out _,[],out _,out _,out _);
        Near(expectedFriction,result[a.Id].Force.X-drive); Near(0,result[a.Id].Force.Y);
        Near(.5*expectedFriction,result[a.Id].Torque.Z);
        var slipAcceleration=result[a.Id].Force.X+5*result[a.Id].Torque.Z;
        if(drive==3) Near(0,slipAcceleration); else Assert.True(slipAcceleration>0);
    }

    [Fact]
    public void DynamicFrictionReactionsPreserveLinearAndAngularMomentum()
    {
        var a=Body(0,new(0,1,0),new(2,0,0)); var b=Body(1,default);
        var gap=Assert.Single(ContactGap.Query(a,Sphere,b,Sphere,.001,1e-9));
        var load=new BodyWrench(new(0,-10,0),default);
        var result=AccelerationSolver.Solve([a,b],[],[ContactForce.FromGap(gap,.5,FrictionRegime.Sliding)],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,load},{b.Id,default}},1e-9,out _,[],out _,out _,out _);
        // Relative tangent speed 2 over core distance 1 contributes gap
        // acceleration 4, so the normal reaction is (10-4)/2 = 3.
        Near(4,gap.ConvectiveAcceleration);
        Near(-1.5,result[a.Id].Force.X); Near(1.5,result[b.Id].Force.X);
        Near(-7,result[a.Id].Force.Y); Near(-3,result[b.Id].Force.Y);
        var angular=result[a.Id].Torque+result[b.Id].Torque+
            CollisionVector.Cross(a.Center,result[a.Id].Force-load.Force)+CollisionVector.Cross(b.Center,result[b.Id].Force);
        Near(0,angular.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TangentialBiasDifferentiatesTheMovingContactVelocityField(bool curved)
    {
        var a=Body(0,new(0,.5,0),new(.7,0,.2),new(.3,.4,-.6));
        var b=Body(1,new(0,-.5,0),new(-.2,.1,.3),new(.4,-.1,.2));
        var gap=Assert.Single(ContactGap.Query(a,Sphere,b,curved?Sphere:Floor,.001,1e-9));
        const double h=1e-5;
        ContactGap Moved(double t)
        {
            PhysicsBody At(PhysicsBody body)=>new(body.Id,PhysicsMotionType.Kinematic,
                new(body.Center+body.LinearVelocity*t,RigidRotation.FromRotationVector(body.AngularVelocity*t)),
                body.LinearVelocity,body.AngularVelocity);
            return gap.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>{{a.Id,At(a)},{b.Id,At(b)}});
        }
        var rate=(Moved(h).SurfaceSlip-Moved(-h).SurfaceSlip)/(2*h);
        rate-=gap.Normal*CollisionVector.Dot(gap.Normal,rate);
        Near(0,(rate-gap.TangentialBias).Length,1e-7);
    }

    [Theory]
    [InlineData(WorldCase.DrivenRolling)]
    [InlineData(WorldCase.Sliding)]
    public void SharedWorldMatchesShortIntervalFrictionMotionAndExactReplay(WorldCase mode)
    {
        var (initial,drive,acceleration,spinAcceleration)=mode switch
        {
            WorldCase.DrivenRolling=>(0.0,3.0,15.0/7,-30.0/7),
            WorldCase.Sliding=>(2.0,0.0,-4.9,-24.5),
            _=>throw new ArgumentOutOfRangeException(nameof(mode))
        };
        var a=Body(0,new(0,.5,0),new(initial,0,0)); var b=Ground();
        var world=new PhysicsWorld([],[new(a,new([Sphere]),new(0,0,.5)),new(b,new([Floor]),new(0,0,.5))],[],new(new(0,-9.8,0)));
        var start=world.Capture();
        for(var i=0;i<10;i++) world.Step([new(a.Id,new(drive,0,0),default)],[],.01);
        var end=a.Snapshot();
        Near(initial*.1+acceleration*.005,a.Center.X,2e-7);
        Near(initial+acceleration*.1,a.LinearVelocity.X,2e-7);
        Near(spinAcceleration*.1,a.AngularVelocity.Z,2e-7); Near(.5,a.Center.Y);
        world.Restore(start);
        for(var i=0;i<10;i++) world.Step([new(a.Id,new(drive,0,0),default)],[],.01);
        Assert.Equal(end,a.Snapshot());
    }

    [Theory]
    [InlineData(2,1.0/30)]
    [InlineData(2,1.0/120)]
    [InlineData(2,1.0/480)]
    [InlineData(-2,1.0/120)]
    [InlineData(.001,1.0/120)]
    [InlineData(-.001,1.0/120)]
    public void SlidingTransitionsToRollingWithoutReversingSlip(double speed,double step)
    {
        var a=Body(0,new(0,.5,0),new(speed,0,0)); var b=Ground();
        var world=new PhysicsWorld([],[new(a,new([Sphere]),new(0,0,.5)),new(b,new([Floor]),new(0,0,.5))],[],
            new(new(0,-9.8,0),maximumStep:step));
        var start=world.Capture(); var results=new List<PhysicsStepResult>();
        var energy=.5*speed*speed;
        for(var i=0;i<(int)Math.Round(1/step);i++)
        {
            results.Add(world.Step([],[],step));
            Assert.InRange(Math.Sign(speed)*(a.LinearVelocity.X+.5*a.AngularVelocity.Z),-1e-7,Math.Abs(speed));
            var next=.5*a.LinearVelocity.LengthSquared+.05*a.AngularVelocity.LengthSquared;
            Assert.True(next<=energy+1e-9); energy=next;
        }
        Near(speed*5/7,a.LinearVelocity.X,1e-7); Near(-speed*10/7,a.AngularVelocity.Z,1e-7);
        Assert.True(results.Sum(result=>result.Events)>0);
        var end=a.Snapshot(); world.Restore(start);
        foreach(var result in results) Assert.Equal(result,world.Step([],[],step));
        Assert.Equal(end,a.Snapshot());
    }

    [Fact]
    public void MovingSurfaceDrivesFrictionUsingRelativeMaterialSlip()
    {
        var a=Body(0,new(0,.5,0));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(0,-.5,0)),new(1,0,0),default);
        var gap=Gap(a,b);
        Near(-1,gap.SurfaceSlip.X);
        var result=AccelerationSolver.Solve([a,b],[],[ContactForce.FromGap(gap,.5,FrictionRegime.Sliding)],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(new(0,-10,0),default)},{b.Id,default}},1e-9,out _,[],out _,out _,out _);
        Near(5,result[a.Id].Force.X); Near(0,result[a.Id].Force.Y);
        Near(2.5,result[a.Id].Torque.Z); Assert.Equal(default,result[b.Id]);
    }

    [Fact]
    public void ExhaustedFrictionEventBudgetRestoresBodiesClockAndContactState()
    {
        var a=Body(0,new(-2,.5,0),new(.005,0,0)); var b=Body(1,new(2,.5,0),new(.01,0,0));
        var floor=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
        var world=new PhysicsWorld([],[new(a,new([Sphere]),new(0,0,.5)),new(b,new([Sphere]),new(0,0,.5)),
            new(floor,new([Floor]),new(0,0,.5))],[],new(new(0,-9.8,0),maximumStep:.01,maximumEvents:1));
        var before=world.Capture(); var sa=a.Snapshot(); var sb=b.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.01));
        Assert.Equal(sa,a.Snapshot()); Assert.Equal(sb,b.Snapshot());
        Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex); Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        var first=world.Step([],[],.0001); var afterA=a.Snapshot(); var afterB=b.Snapshot();
        world.Restore(before);
        Assert.Equal(first,world.Step([],[],.0001));
        Assert.Equal(afterA,a.Snapshot()); Assert.Equal(afterB,b.Snapshot());
    }

    [Fact]
    public void InvalidForceDeclarationsAndForeignStatesReject()
    {
        var a=Body(0,new(0,.5,0)); var b=Ground(); var gap=Gap(a,b);
        Assert.Throws<ArgumentException>(()=>ContactForce.FromGap(gap,-1,FrictionRegime.Sticking));
        Assert.Throws<ArgumentException>(()=>ContactForce.FromGap(gap,.5,(FrictionRegime)99));
        Assert.Throws<ArgumentNullException>(()=>ContactForce.FromGap(null!,0,FrictionRegime.Sticking));
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default}};
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([Body(0,a.Center),b],[],
            [ContactForce.FromGap(gap,.5,FrictionRegime.Sticking)],loads,1e-9,out _,[],out _,out _,out _));
    }
}
