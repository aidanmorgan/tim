using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RoutedRopeTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default,double mass=1)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,mass,new(1,1,1));
    private static PhysicsBody Fixed(int id,CollisionVector center)=>new(new(id),PhysicsMotionType.Static,RigidPose.At(center),default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.05),Transform3D.Identity)]),new(0,0,0));
    private static void Near(double a,double b,double tolerance=1e-8)=>Assert.InRange(Math.Abs(a-b),0,tolerance);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>Near(0,(a-b).Length,tolerance);
    private static PhysicsRopeJoint Route(PhysicsBody a,PhysicsBody guide,PhysicsBody b,double length)=>
        new(new(0),new([new(a,default),new(guide,-X),new(guide,X),new(b,default)]),length,ConnectedBodyCollision.Disabled);

    [Fact]
    public void MovingGuideAndBothLoadsReactInOneWorldSolve()
    {
        // IDs intentionally differ from geometric route order.
        var a=Body(8,-X,-Y*3); var guide=Body(2,Y); var b=Body(5,X,-Y);
        var rope=Route(a,guide,b,4);
        var world=new PhysicsWorld([Object(a),Object(guide),Object(b)],[rope],new(default,maximumStep:.1));
        var result=world.Step([],.1);
        Near(-Y*(7.0/3),a.LinearVelocity); Near(-Y/3,b.LinearVelocity); Near(-Y*(4.0/3),guide.LinearVelocity);
        Near(-Y*4,a.LinearVelocity+b.LinearVelocity+guide.LinearVelocity);
        Near(default(CollisionVector),guide.AngularMomentum); Near(4,rope.Route.CurrentLength);
        Assert.Equal(0,result.Events); Assert.Equal(3,rope.Bodies.Length);
        Assert.Equal(new[]{2,5,8},rope.Bodies.ToArray().Select(body=>body.Id.Index).ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void FixedGuideTransfersTensionButNeverPushes(int sign)
    {
        var a=Body(0,-X,Y*(3*sign)); var guide=Fixed(1,Y); var b=Body(2,X,Y*sign);
        var rope=Route(a,guide,b,4);
        var world=new PhysicsWorld([Object(a),Object(guide),Object(b)],[rope],new(default,maximumStep:.1));
        world.Step([],.1);
        Near(Y*(sign<0?-1:3),a.LinearVelocity); Near(Y,b.LinearVelocity);
        Assert.Equal(RigidPose.At(Y),guide.Pose);
        Assert.True(rope.Route.CurrentLength<=4+1e-7);
    }

    [Fact]
    public void SlackRoutedRopeKeepsSpeedUntilItsAnalyticEventAndReplaysExactly()
    {
        var a=Body(7,-X,-Y*100); var guide=Fixed(3,Y); var b=Body(1,X);
        var rope=Route(a,guide,b,5);
        Assert.Empty(rope.VelocityConstraints(1e-7));
        var world=new PhysicsWorld([Object(a),Object(guide),Object(b)],[rope],new(default,maximumStep:.012));
        var before=world.Capture(); var result=world.Step([],.012);
        var stop=Assert.Single(world.JointStops.ToArray()); Near(.01,stop.Time,1e-9);
        Near(-Y*50,a.LinearVelocity); Near(Y*50,b.LinearVelocity); Near(5,rope.Route.CurrentLength);
        Assert.Equal(1,result.Events);
        var after=world.Capture(); var stops=world.JointStops.ToArray();
        world.Restore(before); Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Empty(world.JointStops.ToArray());
        Assert.Equal(result,world.Step([],.012));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(stops,world.JointStops.ToArray());
    }

    [Fact]
    public void ThreeBodyProjectionUsesSharedMassAndLeavesPhysicalVelocityUntouched()
    {
        var a=Body(0,-X,-Y*3); var guide=Body(1,Y*2); var b=Body(2,X,-Y);
        var rope=Route(a,guide,b,4);
        var momentum=new[]{a.LinearVelocity,guide.LinearVelocity,b.LinearVelocity};
        PositionSolver.Solve([rope],new([a,guide,b],[],1e-6),1e-7);
        Near(-X+Y/3,a.Center); Near(X+Y/3,b.Center); Near(Y*(4.0/3),guide.Center);
        Near(4,rope.Route.CurrentLength);
        Assert.Equal(momentum,new[]{a.LinearVelocity,guide.LinearVelocity,b.LinearVelocity});
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GuideCorrectionChecksUnrelatedObstaclesAndRollsBackWhenBlocked(bool blocked)
    {
        var a=Fixed(0,-X); var guide=Body(1,Y*2); var b=Fixed(2,X);
        var rope=Route(a,guide,b,4);
        var objects=new List<PhysicsObject>{Object(a),Object(guide),Object(b)};
        if(blocked) objects.Add(new(Fixed(3,Y*1.5),
            new([new(new ConvexBox(new(.2,.05,.2)),Transform3D.Identity)]),new(0,0,0)));
        var world=new PhysicsWorld(objects,[rope],new(default));
        var before=world.Capture();
        if(blocked)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step([],.01));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,world.Time); Assert.Equal(0UL,world.StepIndex);
            Assert.Empty(world.JointStops.ToArray()); Assert.Empty(world.Impacts.ToArray());
        }
        else
        {
            world.Step([],.01); Near(Y,guide.Center); Near(4,rope.Route.CurrentLength);
            Near(default(CollisionVector),guide.LinearVelocity);
        }
    }

    [Fact]
    public void RotatingMiddleGuideFindsHiddenExtensionDespiteCoincidentSlackSpans()
    {
        var a=Fixed(4,X); var guide=Body(2,default,spin:Z*120); var b=Fixed(0,X);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(guide,X),new(b,default)]),2,ConnectedBodyCollision.Disabled);
        Assert.Equal(0,rope.Route.CurrentLength); Assert.Empty(rope.VelocityConstraints(1e-7));
        var duration=Math.Tau/120; var paths=rope.Bodies.ToArray().Select(body=>body.CreateTrajectory(duration)).ToArray();
        var hit=rope.Sweep(paths,duration,1e-7);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status); Near(Math.PI/3,hit.Time*120,1e-7);
        for(var i=0;i<paths.Length;i++) rope.Bodies[i].Advance(paths[i],hit.Time);
        Near(2,rope.Route.CurrentLength,1e-7);
    }

    [Fact]
    public void RoutedGeometryDifferentialMatchesFiveBodyFiniteDifferences()
    {
        var random=new Random(3902);
        CollisionVector Vector()=>new(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1);
        for(var sample=0;sample<150;sample++)
        {
            var bodies=Enumerable.Range(0,5).Select(i=>Body(i,Vector(),Vector(),Vector())).ToArray();
            var anchors=bodies.Select(b=>new RopeAnchor(b,Vector())).ToList();
            anchors.Insert(3,new(bodies[2],Vector())); // Constant segment on a repeated body.
            var route=new RopeRoute(anchors); var initial=route.CurrentLength;
            var gradient=route.Equation(initial).Gradient; var rate=gradient.Speed;
            const double h=1e-7;
            foreach(var body in bodies) body.Advance(body.CreateTrajectory(h),h);
            Near(rate,(route.CurrentLength-initial)/h,5e-5);
        }
    }

    [Fact]
    public void SampledAnisotropicRoutesCannotCrossBeforeTheirReportedBoundary()
    {
        var random=new Random(8471); var hits=0;
        CollisionVector Vector()=>new(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1);
        for(var sample=0;sample<100;sample++)
        {
            var bodies=Enumerable.Range(0,4).Select(i=>new PhysicsBody(new(i),PhysicsMotionType.Dynamic,
                RigidPose.At(Vector()),Vector()*3,Vector()*20,1,new(.1,.2,.4))).ToArray();
            var route=new RopeRoute(bodies.Select(b=>new RopeAnchor(b,Vector())));
            var length=route.CurrentLength+.3; var rope=new PhysicsRopeJoint(new(0),route,length,ConnectedBodyCollision.Disabled);
            const double duration=.1; var paths=bodies.Select(b=>b.CreateTrajectory(duration)).ToArray();
            var hit=rope.Sweep(paths,duration,1e-7);
            if(hit.Status==JointSweepStatus.Boundary) hits++;
            for(var point=0;point<=200;point++)
            {
                var time=hit.Time*point/200; double observed=0;
                for(var i=1;i<bodies.Length;i++)
                    observed+=(paths[i].At(time).TransformPoint(route.Anchors[i].LocalPosition)-
                        paths[i-1].At(time).TransformPoint(route.Anchors[i-1].LocalPosition)).Length;
                Assert.True(observed<=length+1e-7,$"Sample {sample}: {observed:R} > {length:R}");
            }
        }
        Assert.True(hits>10);
    }

    [Fact]
    public void TautCollapsedSpanIsRejectedAndWorldStateIsRestored()
    {
        var a=Body(0,default); var guide=Fixed(1,default); var b=Fixed(2,X);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(guide,default),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([Object(a),Object(guide),Object(b)],[rope],new(default));
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],.01));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(0,world.Time);
    }

    [Fact]
    public void OwnershipChecksIncludeMiddleParticipantsAndAllCapturedPaths()
    {
        var a=Body(0,-X); var guide=Body(1,Y); var b=Body(2,X);
        var rope=Route(a,guide,b,4); var copy=Body(1,Y);
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([Object(a),Object(copy),Object(b)],[rope],new(default)));
        var paths=rope.Bodies.ToArray().Select(body=>body.CreateTrajectory(.01)).ToArray();
        Assert.Throws<ArgumentException>(()=>rope.Sweep([paths[0],paths[2]],.01,1e-7));
        guide.ApplyImpulse(Y,guide.Center);
        Assert.Throws<InvalidOperationException>(()=>rope.Sweep(paths,.01,1e-7));
        Assert.Throws<ArgumentException>(()=>PositionSolver.Solve([rope],new([a,copy,b],[],1e-6),1e-7));
        Assert.Throws<ArgumentException>(()=>new RopeRoute([new(a,default),new(copy,default),new(guide,default)]));
        Assert.Throws<ArgumentException>(()=>new RopeRoute([]));
        Assert.Throws<ArgumentNullException>(()=>new RopeRoute([default,new(a,default)]));
        Assert.Throws<ArgumentException>(()=>new RopeAnchor(a,new(double.NaN,0,0)));
    }

    [Fact]
    public void RouteCopiesAuthoringInputAndRigidInternalLengthDoesNotDrift()
    {
        var a=Fixed(0,-X); var guide=Body(1,Y,spin:Z*120); var b=Fixed(2,X);
        RopeAnchor[] anchors=[new(a,default),new(guide,-X),new(guide,X),new(b,default)];
        var route=new RopeRoute(anchors); anchors[1]=default;
        guide.Advance(guide.CreateTrajectory(.01),.01);
        var outside=(route.Anchors[0].Position-route.Anchors[1].Position).Length+
            (route.Anchors[2].Position-route.Anchors[3].Position).Length;
        Near(2,route.CurrentLength-outside,1e-14);
    }
    [Fact]
    public void FrameRolesArePreservedWhenCanonicalBodyOrderIsReversed()
    {
        var body=Body(8,default,spin:Z*100); var anchor=Fixed(2,default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,frame,anchor,frame,ConnectedBodyCollision.Disabled,new(-.5,.5));
        var world=new PhysicsWorld([Object(body),Object(anchor)],[joint],new(default,maximumStep:.01));
        world.Step([],.01);
        Near(.005,Assert.Single(world.JointStops.ToArray()).Time,1e-9);
        Near(.5,joint.Travel.Error,1e-7); Near(default(CollisionVector),body.AngularVelocity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void RoutedStopsShareTheEventBudgetAndRollbackEveryParticipant(int budget)
    {
        var a=Body(0,new(-4,0,0),-Y*100); var g=Fixed(1,new(-3,1,0)); var b=Body(2,new(-2,0,0));
        var c=Body(3,new(2,0,0),-Y*200); var h=Fixed(4,new(3,1,0)); var d=Body(5,new(4,0,0));
        var first=Route(a,g,b,5);
        var second=new PhysicsRopeJoint(new(1),new([new(c,default),new(h,-X),new(h,X),new(d,default)]),5,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld(new[]{a,g,b,c,h,d}.Select(Object),[first,second],
            new(default,maximumStep:.012,maximumEvents:budget));
        var before=world.Capture();
        if(budget==1)
        {
            Assert.Throws<InvalidOperationException>(()=>world.Step([],.012));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            Assert.Equal(0,world.Time); Assert.Empty(world.JointStops.ToArray());
        }
        else
        {
            Assert.Equal(2,world.Step([],.012).Events);
            Assert.Equal(new[]{new PhysicsJointId(1),new PhysicsJointId(0)},world.JointStops.ToArray().Select(stop=>stop.Joint).ToArray());
            Near(.005,world.JointStops[0].Time,1e-9); Near(.01,world.JointStops[1].Time,1e-9);
        }
    }

}
