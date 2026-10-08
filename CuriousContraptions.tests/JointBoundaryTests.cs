using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JointBoundaryTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void InteriorJointDoesNotBrakeBeforeItsStop(FrameJointKind kind)
    {
        var a=Body(0,default,kind==FrameJointKind.Slider?new(0,0,100):default,
            kind==FrameJointKind.Hinge?new(0,0,100):default);
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,Fixed(1),Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        ImpulseSolver.Solve(joint.VelocityConstraints(.01));
        Assert.InRange(Math.Abs(a.LinearVelocity.Z+a.AngularVelocity.Z-100),0,1e-10);
    }

    [Fact]
    public void SlackRopeDoesNotBrakeBeforeItIsTaut()
    {
        var a=Body(0,new(.5,0,0),new(100,0,0));
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(Fixed(1),default)]),1,ConnectedBodyCollision.Disabled);
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
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.01));
        var before=world.Capture();
        var result=world.Step([],[],.01);
        var stop=Assert.Single(world.JointStops.ToArray());
        Assert.Equal(sign<0?JointBoundary.Lower:JointBoundary.Upper,stop.Boundary);
        Assert.InRange(Math.Abs(stop.Time-.00005),0,1e-10);
        Assert.Equal(1,result.Events);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign*.5),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length+a.AngularVelocity.Length,0,1e-8);
        var after=world.Capture(); var stops=world.JointStops.ToArray();
        world.Restore(before); Assert.Empty(world.JointStops.ToArray());
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(stops,world.JointStops.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void RotatingCarrierCannotHideSliderExcursionBetweenClearEndpoints()
    {
        var a=Body(0,new(0,1,0));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,120));
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,frame,b,frame,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var duration=Math.Tau/120;
        var hit=joint.Sweep([a.CreateTrajectory(duration,default),b.CreateTrajectory(duration,default)],duration,1e-7,1e-8);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status); Assert.Equal(JointBoundary.Upper,hit.Boundary);
        Assert.InRange(Math.Abs(hit.Time*120-Math.PI/6),0,1e-7);
    }

    [Theory]
    [InlineData(JointTravelDirection.Both,1)]
    [InlineData(JointTravelDirection.Positive,1)]
    [InlineData(JointTravelDirection.Negative,-1)]
    public void RotatingRailCannotHideDirectionReversalBetweenForwardEndpoints(JointTravelDirection direction,int sign)
    {
        const double spin=10;
        var a=Body(0,new(sign,0,0),new(sign,sign*spin,0),new(0,0,spin));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,spin));
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,frame,b,frame,
            ConnectedBodyCollision.Disabled,null,direction);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Assert.InRange(Math.Abs(joint.Travel.Jacobian.Bind(a,b).Speed-sign),0,1e-7);
        var duration=Math.Tau/spin;
        var pa=a.CreateTrajectory(duration,default); var pb=b.CreateTrajectory(duration,default);
        double Coordinate(double time)
        {
            var first=pa.At(time); var second=pb.At(time);
            var axis=(second.Rotation*frame.Orientation).Apply(new(0,0,1));
            return sign*CollisionVector.Dot(first.Center-second.Center,axis);
        }
        Assert.True(Coordinate(duration)>Coordinate(0));
        Assert.True(Coordinate(.18)<Coordinate(.15));
        var hit=joint.Sweep([pa,pb],duration,1e-7,1e-8);
        if(direction==JointTravelDirection.Both)
            Assert.Equal(JointSweepStatus.Clear,hit.Status);
        else
        {
            Assert.Equal(JointSweepStatus.Boundary,hit.Status);
            Assert.InRange(hit.Time,.1,.18);
        }
    }

    [Theory]
    [InlineData(JointTravelDirection.Positive,-1)]
    [InlineData(JointTravelDirection.Negative,1)]
    public void RotatingRailSupportsAnActiveRatchetWithoutEventChatter(JointTravelDirection direction,int sign)
    {
        var a=Body(0,new(sign,0,0),new(0,sign,0),new(0,0,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,1));
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,frame,b,frame,
            ConnectedBodyCollision.Disabled,null,direction);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.001));
        var initial=world.Capture();
        for(var i=0;i<10;i++) world.Step([],[],.001);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign),0,1e-6);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        var final=world.Capture();
        world.Restore(initial);
        for(var i=0;i<10;i++) world.Step([],[],.001);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void RotatingRopeFindsIntermediateExtensionFromCoincidentEndpoints()
    {
        var a=Body(0,default,spin:new(0,0,120));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(1,0,0)),default,default);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,new(1,0,0)),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        Assert.Empty(rope.VelocityConstraints(1e-7));
        var duration=Math.Tau/120;
        var hit=rope.Sweep([a.CreateTrajectory(duration,default),b.CreateTrajectory(duration,default)],duration,1e-7,1e-8);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status);
        Assert.InRange(Math.Abs(hit.Time*120-Math.PI/3),0,1e-7);
    }

    [Fact]
    public void CoincidentSlackRopeFliesFreelyUntilItsAnalyticStop()
    {
        var a=Body(0,default,new(100,0,0)); var b=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[rope],new(default,maximumStep:.02));
        world.Step([],[],.02);
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
        var j1=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var j2=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,c,Origin,d,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b),Object(c),Object(d)],[j1,j2],new(default,maximumStep:.01,maximumEvents:eventBudget));
        var before=world.Capture();
        if(eventBudget==1)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.01));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,world.Time); Assert.Equal(0UL,world.StepIndex); Assert.Empty(world.JointStops.ToArray());
        }
        else
        {
            Assert.Equal(2,world.Step([],[],.01).Events);
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
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var pa=a.CreateTrajectory(.01,default); var pb=b.CreateTrajectory(.01,default);
        Assert.Throws<InvalidOperationException>(()=>joint.Sweep([pb,pa],.01,1e-7,1e-8));
        a.ApplyImpulse(new(0,0,1),a.Center);
        Assert.Throws<InvalidOperationException>(()=>joint.Sweep([pa,pb],.01,1e-7,1e-8));
    }

    [Fact]
    public void TautRopeSupportsCurvedMotionWithoutAnInitialContactDeadlock()
    {
        var a=Body(0,new(1,0,0),new(0,1,0)); var b=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[rope],new(default));
        var before=world.Capture();
        for(var i=0;i<120;i++)
        {
            world.Step([],[],1.0/120);
            Assert.InRange(rope.Error(1e-8),0,1e-7);
            Assert.InRange(a.LinearVelocity.LengthSquared,0,1.0000001);
        }
        Assert.InRange(Math.Abs(a.Center.X-Math.Cos(1)),0,.01);
        Assert.InRange(Math.Abs(a.Center.Y-Math.Sin(1)),0,.01);
        var after=world.Capture(); var stops=world.JointStops.ToArray();
        world.Restore(before);
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
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
            var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
            const double duration=.1; var pa=a.CreateTrajectory(duration,default); var pb=b.CreateTrajectory(duration,default);
            var hit=joint.Sweep([pa,pb],duration,1e-7,1e-8);
            for(var i=0;i<=200;i++)
            {
                var time=hit.Time*i/200; var first=pa.At(time); var second=pb.At(time);
                var coordinate=JointEquations.Travel(kind,a,b,new(first.Center,first.Rotation),new(second.Center,second.Rotation)).Error;
                Assert.InRange(coordinate,-.5000001,.5000001);
            }
        }
    }

    [Theory]
    [InlineData(FrameJointKind.Slider,JointTravelDirection.Positive)]
    [InlineData(FrameJointKind.Slider,JointTravelDirection.Negative)]
    [InlineData(FrameJointKind.Hinge,JointTravelDirection.Positive)]
    [InlineData(FrameJointKind.Hinge,JointTravelDirection.Negative)]
    public void SampledRotatingTravelCannotReverseBeforeDirectionalEvent(FrameJointKind kind,JointTravelDirection direction)
    {
        var random=new Random(35127);
        double Number()=>2*random.NextDouble()-1;
        CollisionVector Vector()=>new(Number(),Number(),Number());
        var sign=direction==JointTravelDirection.Positive?1:-1;
        var advancing=0;
        for(var sample=0;sample<100;sample++)
        {
            var a=Body(0,Vector(),Vector(),Vector()*10);
            var b=Body(1,Vector(),Vector(),Vector()*10);
            var localA=new JointFrame(Vector()*.2,RigidRotation.Identity);
            var localB=new JointFrame(Vector()*.2,RigidRotation.Identity);
            var joint=new PhysicsFrameJoint(new(0),kind,a,localA,b,localB,
                ConnectedBodyCollision.Disabled,null,direction);
            const double duration=.1;
            var pa=a.CreateTrajectory(duration,default); var pb=b.CreateTrajectory(duration,default);
            var hit=joint.Sweep([pa,pb],duration,1e-7,1e-8);
            if(hit.Time>0) advancing++;
            double Coordinate(double time)
            {
                var first=pa.At(time); var second=pb.At(time);
                var fa=new JointFrame(first.TransformPoint(localA.Anchor),first.Rotation*localA.Orientation);
                var fb=new JointFrame(second.TransformPoint(localB.Anchor),second.Rotation*localB.Orientation);
                return JointEquations.Travel(kind,a,b,fa,fb).Error;
            }
            var previous=Coordinate(0); var dt=hit.Time/200;
            for(var i=1;i<=200;i++)
            {
                var coordinate=Coordinate(hit.Time*i/200);
                var change=coordinate-previous;
                if(kind==FrameJointKind.Hinge) change=Math.IEEERemainder(change,Math.Tau);
                Assert.True(sign*change>=-4e-8*dt-1e-12);
                previous=coordinate;
            }
        }
        Assert.True(advancing>=20);
    }

    [Fact]
    public void InactiveBoundariesStillRejectInvalidDeclarations()
    {
        var a=Body(0,default); var b=Fixed(1);
        var j=new ConstraintJacobian(new(0,0,1),default,new(0,0,-1),default);
        var frame=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var route=new PhysicsRopeJoint(new(1),new([new(a,default),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        var paths=new[]{a.CreateTrajectory(.01,default),b.CreateTrajectory(.01,default)};
        foreach(var speedTolerance in new[]{0,-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>frame.Sweep(paths,.01,1e-7,speedTolerance));
            Assert.Throws<ArgumentOutOfRangeException>(()=>route.Sweep(paths,.01,1e-7,speedTolerance));
        }
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,a,j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,Body(0,default),j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,b,j with {AngularA=new(double.NaN,0,0)},0,-1,1,1e-7));
        Assert.Throws<ArgumentNullException>(()=>JointConstraints.Limits(null!,b,j,0,-1,1,1e-7));
        Assert.Throws<ArgumentException>(()=>new PhysicsRopeJoint(new(0),new([new(a,default),new(a,default)]),1,ConnectedBodyCollision.Disabled));
        Assert.Throws<ArgumentException>(()=>new PhysicsRopeJoint(new(0),new([new(a,default),new(Body(0,default),default)]),1,ConnectedBodyCollision.Disabled));
    }
}
