using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JointBoundaryTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),Transform3D.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void InteriorJointDoesNotBrakeBeforeItsStop(FrameJointKind kind)
    {
        var a=Body(0,default,kind==FrameJointKind.Slider?new(0,0,100):default,
            kind==FrameJointKind.Hinge?new(0,0,100):default);
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,Fixed(1),Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        ImpulseSolver.Solve(joint.VelocityConstraints(.01));
        Assert.InRange(Math.Abs(a.LinearVelocity.Z+a.AngularVelocity.Z-100),0,1e-10);
    }

    [Fact]
    public void SlackRopeDoesNotBrakeBeforeItIsTaut()
    {
        var a=Body(0,new(.5,0,0),new(100,0,0));
        var rope=new PhysicsRopeJoint(new(0),a,default,Fixed(1),default,1,ConnectedBodyCollision.Disabled);
        ImpulseSolver.Solve(rope.VelocityConstraints(.01));
        Assert.Equal(new CollisionVector(100,0,0),a.LinearVelocity);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge,-1)]
    [InlineData(FrameJointKind.Hinge,1)]
    [InlineData(FrameJointKind.Slider,-1)]
    [InlineData(FrameJointKind.Slider,1)]
    public void WorldStopsAtAnalyticArrivalTimeRatherThanSlowingTheFlight(FrameJointKind kind,int sign)
    {
        var a=Body(0,default,kind==FrameJointKind.Slider?new(0,0,sign*10000):default,
            kind==FrameJointKind.Hinge?new(0,0,sign*10000):default);
        var b=Fixed(1);
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default,maximumStep:.01));
        var before=world.Capture();
        var result=world.Step(.01);
        var stop=Assert.Single(world.JointStops.ToArray());
        Assert.Equal(sign<0?JointBoundary.Lower:JointBoundary.Upper,stop.Boundary);
        Assert.InRange(Math.Abs(stop.Time-.00005),0,1e-10);
        Assert.Equal(1,result.Events);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign*.5),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length+a.AngularVelocity.Length,0,1e-8);
        var after=world.Capture(); var stops=world.JointStops.ToArray();
        world.Restore(before); Assert.Empty(world.JointStops.ToArray());
        Assert.Equal(result,world.Step(.01));
        Assert.Equal(stops,world.JointStops.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void RotatingCarrierCannotHideSliderExcursionBetweenClearEndpoints()
    {
        var a=Body(0,new(0,1,0));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,120));
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,frame,b,frame,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var duration=Math.Tau/120;
        var hit=joint.Sweep(a.CreateTrajectory(duration),b.CreateTrajectory(duration),duration,1e-7);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status); Assert.Equal(JointBoundary.Upper,hit.Boundary);
        Assert.InRange(Math.Abs(hit.Time*120-Math.PI/6),0,1e-7);
    }

    [Fact]
    public void RotatingRopeFindsIntermediateExtensionFromCoincidentEndpoints()
    {
        var a=Body(0,default,spin:new(0,0,120));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(1,0,0)),default,default);
        var rope=new PhysicsRopeJoint(new(0),a,new(1,0,0),b,default,1,ConnectedBodyCollision.Disabled);
        Assert.Empty(rope.VelocityConstraints(1e-7));
        var duration=Math.Tau/120;
        var hit=rope.Sweep(a.CreateTrajectory(duration),b.CreateTrajectory(duration),duration,1e-7);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status);
        Assert.InRange(Math.Abs(hit.Time*120-Math.PI/3),0,1e-7);
    }

    [Fact]
    public void CoincidentSlackRopeFliesFreelyUntilItsAnalyticStop()
    {
        var a=Body(0,default,new(100,0,0)); var b=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),a,default,b,default,1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([Object(a),Object(b)],[rope],new(default,maximumStep:.02));
        world.Step(.02);
        var stop=Assert.Single(world.JointStops.ToArray());
        Assert.InRange(Math.Abs(stop.Time-.01),0,1e-9);
        Assert.InRange(Math.Abs(a.Center.X-1),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length,0,1e-8);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void MultipleStopsShareTheWorldClockAndBudget(int eventBudget)
    {
        var a=Body(0,new(-2,0,0),new(0,0,100)); var b=Fixed(1);
        b.Restore(b.Snapshot() with {Pose=RigidPose.At(new(-2,0,0))});
        var c=Body(2,new(2,0,0),new(0,0,200)); var d=Fixed(3);
        d.Restore(d.Snapshot() with {Pose=RigidPose.At(new(2,0,0))});
        var j1=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var j2=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,c,Origin,d,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var world=new PhysicsWorld([Object(a),Object(b),Object(c),Object(d)],[j1,j2],new(default,maximumStep:.01,maximumEvents:eventBudget));
        var before=world.Capture();
        if(eventBudget==1)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step(.01));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,world.Time); Assert.Equal(0UL,world.StepIndex); Assert.Empty(world.JointStops.ToArray());
        }
        else
        {
            Assert.Equal(2,world.Step(.01).Events);
            var stops=world.JointStops.ToArray(); Assert.Equal(2,stops.Length);
            Assert.Equal(new PhysicsJointId(1),stops[0].Joint); Assert.Equal(new PhysicsJointId(0),stops[1].Joint);
            Assert.InRange(Math.Abs(stops[0].Time-.0025),0,1e-9);
            Assert.InRange(Math.Abs(stops[1].Time-.005),0,1e-9);
        }
    }

    [Fact]
    public void BoundarySweepRejectsStaleOrForeignCapturedMotion()
    {
        var a=Body(0,default); var b=Fixed(1);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var pa=a.CreateTrajectory(.01); var pb=b.CreateTrajectory(.01);
        Assert.Throws<InvalidOperationException>(()=>joint.Sweep(pb,pa,.01,1e-7));
        a.ApplyImpulse(new(0,0,1),a.Center);
        Assert.Throws<InvalidOperationException>(()=>joint.Sweep(pa,pb,.01,1e-7));
    }

    [Fact]
    public void TautRopeSupportsCurvedMotionWithoutAnInitialContactDeadlock()
    {
        var a=Body(0,new(1,0,0),new(0,1,0)); var b=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),a,default,b,default,1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([Object(a),Object(b)],[rope],new(default));
        var before=world.Capture();
        for(var i=0;i<120;i++)
        {
            world.Step(1.0/120);
            Assert.InRange(rope.Error(1e-8),0,1e-7);
            Assert.InRange(a.LinearVelocity.LengthSquared,0,1.0000001);
        }
        Assert.InRange(Math.Abs(a.Center.X-Math.Cos(1)),0,.01);
        Assert.InRange(Math.Abs(a.Center.Y-Math.Sin(1)),0,.01);
        var after=world.Capture(); var stops=world.JointStops.ToArray();
        world.Restore(before);
        for(var i=0;i<120;i++) world.Step(1.0/120);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(stops,world.JointStops.ToArray());
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void SampledRandomRotatingPathsCannotCrossBeforeTheReportedBoundary(FrameJointKind kind)
    {
        var random=new Random(4829);
        double Number()=>2*random.NextDouble()-1;
        CollisionVector Vector()=>new(Number(),Number(),Number());
        for(var sample=0;sample<100;sample++)
        {
            var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,Vector(),Vector()*20,1,new(.1,.2,.4));
            var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Vector(),Vector()*20,1,new(.3,.15,.1));
            var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5));
            const double duration=.1; var pa=a.CreateTrajectory(duration); var pb=b.CreateTrajectory(duration);
            var hit=joint.Sweep(pa,pb,duration,1e-7);
            for(var i=0;i<=200;i++)
            {
                var time=hit.Time*i/200; var first=pa.At(time); var second=pb.At(time);
                var coordinate=JointEquations.Travel(kind,a,b,new(first.Center,first.Rotation),new(second.Center,second.Rotation)).Error;
                Assert.InRange(coordinate,-.5000001,.5000001);
            }
        }
    }

    [Fact]
    public void InactiveBoundariesStillRejectInvalidDeclarations()
    {
        var a=Body(0,default); var b=Fixed(1);
        var j=new ConstraintJacobian(new(0,0,1),default,new(0,0,-1),default);
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,a,j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,Body(0,default),j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,b,j with {AngularA=new(double.NaN,0,0)},0,-1,1,1e-7));
        Assert.Throws<ArgumentNullException>(()=>JointConstraints.Limits(null!,b,j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Rope(a,a,default,default,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Rope(a,Body(0,default),default,default,1,1e-7));
    }
}
