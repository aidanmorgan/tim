using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsJointTests
{

    [Theory]
    [InlineData(FrameJointKind.Slider,-1)]
    [InlineData(FrameJointKind.Slider,1)]
    [InlineData(FrameJointKind.Hinge,-1)]
    [InlineData(FrameJointKind.Hinge,1)]
    public void AxialMotionReadsImpulseStepAndSnapshotWithoutAnObserver(FrameJointKind kind,int sign)
    {
        var body=Body(0,default);var anchor=Fixed(1);var joint=Joint(kind,body,anchor);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default));
        Assert.Equal(default,joint.Motion);
        var initial=world.Capture();
        if(kind==FrameJointKind.Hinge)world.ApplyAngularImpulse(body.Id,new(0,0,.1*sign));
        else world.ApplyImpulse(body.Id,new(0,0,sign),body.Center);
        Assert.InRange(Math.Abs(joint.Motion.Speed-sign),0,1e-12);
        Assert.Equal(0,joint.Motion.Coordinate);
        world.Step([],[],.02);
        Assert.InRange(Math.Abs(joint.Motion.Coordinate-sign*.02),0,1e-12);
        var moved=world.Capture();var reading=joint.Motion;
        world.Restore(initial);Assert.Equal(default,joint.Motion);
        world.Restore(moved);Assert.Equal(reading,joint.Motion);
        Assert.Equal(moved.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void BallSocketHasNoAxialMotionReading()
    {
        var joint=Joint(FrameJointKind.BallSocket,Body(0,default),Fixed(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>joint.Motion);
    }

    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0));
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointFrame? localA=null)=>
        new(new(0),kind,a,localA??Origin,b,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-7)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Theory]
    [InlineData(FrameJointKind.BallSocket)]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void WorldProjectsDeclaredFrameErrorsWithoutInjectingVelocity(FrameJointKind kind)
    {
        var body=Body(0,new(.2,.3,0)); var anchor=Fixed(1);
        body.Restore(body.Snapshot() with {Pose=new(body.Center,RigidRotation.FromRotationVector(new(.1,.2,0)))});
        var joint=Joint(kind,body,anchor);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default));
        world.Step([],[],.01);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Near(default,body.LinearVelocity); Near(default,body.AngularMomentum);
        Assert.Equal(RigidPose.Identity,anchor.Pose);
    }

    [Theory]
    [InlineData(120)]
    [InlineData(240)]
    [InlineData(480)]
    public void HingePendulumMaintainsItsPivotAndReplaysExactly(int frequency)
    {
        var body=Body(0,new(1,0,0)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.Hinge,body,anchor,new(new(-1,0,0),RigidRotation.Identity));
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(new(0,-9.8,0)));
        var before=world.Capture();
        for(var i=0;i<2*frequency;i++)
        {
            world.Step([],[],1.0/frequency);
            Assert.InRange(joint.Error(1e-8),0,1e-7);
            Near(default,body.PointVelocity(joint.FrameA.Anchor));
            Assert.InRange(Math.Abs(body.Center.Z),0,1e-8);
        }
        Assert.True(body.Center.Y<-.1);
        var after=world.Capture();
        var energy=.5*body.LinearVelocity.LengthSquared+.05*body.AngularVelocity.LengthSquared+9.8*body.Center.Y;
        Assert.True(energy<=1e-4,$"Unpowered pendulum gained energy: {energy:R}.");
        world.Restore(before);
        for(var i=0;i<2*frequency;i++) world.Step([],[],1.0/frequency);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.Time,world.Time);
    }

    [Fact]
    public void SliderAllowsOnlyItsDeclaredTravelAxis()
    {
        var body=Body(0,default,new(2,3,4),new(1,2,3)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.Slider,body,anchor);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(new(1,2,3)));
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        Near(new(0,0,7),body.LinearVelocity);
        Near(default,body.AngularVelocity);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Assert.True(body.Center.Z>5);
    }

    [Theory]
    [InlineData(JointTravelDirection.Both,1,true)]
    [InlineData(JointTravelDirection.Both,-1,true)]
    [InlineData(JointTravelDirection.Positive,1,true)]
    [InlineData(JointTravelDirection.Negative,-1,true)]
    [InlineData(JointTravelDirection.Positive,1,false)]
    [InlineData(JointTravelDirection.Negative,-1,false)]
    public void DirectionPolicyControlsMidStepImpactReversal(JointTravelDirection direction,int sign,bool bounded)
    {
        // Both is the negative control: bounds alone do not make a ratchet.
        var head=Body(0,default,new(0,0,sign));
        var rail=Fixed(1);
        var payload=Body(2,new(0,0,sign),new(0,0,-3*sign));
        var slider=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,head,Origin,
            rail,Origin,ConnectedBodyCollision.Disabled,bounded?new(-2,2):null,direction);
        // Keep the rail collider away from the load path; only the payload hits.
        var fixture=new PhysicsObject(rail,new CompoundGeometry([
            new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new Vector3(10,0,0))))]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(head),fixture,Object(payload)],[slider],
            new(default,maximumStep:.3));
        var initial=world.Capture();
        void Run()
        {
            world.Step([],[],.3);
            var impact=Assert.Single(world.Impacts.ToArray());
            Assert.True(impact.Pair.A==head.Id&&impact.Pair.B==payload.Id||
                impact.Pair.B==head.Id&&impact.Pair.A==payload.Id);
            Assert.InRange(impact.Time,.1999,.2001);
            var reversible=direction==JointTravelDirection.Both;
            Near(new(0,0,reversible?-sign:0),head.LinearVelocity,1e-7);
            Near(head.LinearVelocity,payload.LinearVelocity,1e-7);
            var coordinate=head.Center.Z*sign;
            Assert.InRange(coordinate,reversible?.0998:.1998,reversible?.1002:.2002);
            if(reversible) Assert.True(coordinate<impact.Time-.09);
            else Assert.InRange(Math.Abs(coordinate-impact.Time),0,1e-7);
            Assert.InRange(slider.Error(1e-8),0,1e-7);
        }
        Run(); var final=world.Capture();
        world.Restore(initial); Run();
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(JointTravelDirection.Positive,1,1)]
    [InlineData(JointTravelDirection.Positive,-1,0)]
    [InlineData(JointTravelDirection.Negative,-1,-1)]
    [InlineData(JointTravelDirection.Negative,1,0)]
    public void HingeDirectionUsesAngularTravel(JointTravelDirection direction,double speed,double expected)
    {
        var body=Body(0,default,spin:new(0,0,speed)); var anchor=Fixed(1);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,direction);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default));
        world.Step([],[],.01);
        Near(new(0,0,expected),body.AngularVelocity);
        Near(default,body.LinearVelocity);
    }

    [Fact]
    public void InvalidTravelDirectionsAndNonAxialPoliciesReject()
    {
        var body=Body(0,default); var anchor=Fixed(1);
        Assert.Throws<ArgumentException>(()=>new PhysicsFrameJoint(new(0),FrameJointKind.Slider,
            body,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,null,(JointTravelDirection)999));
        Assert.Throws<ArgumentException>(()=>new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,
            body,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Positive));
    }

    [Fact]
    public void BallSocketLocksTranslationButLeavesThreeRotationalDegrees()
    {
        var body=Body(0,default,new(1,2,3),new(2,3,4)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.BallSocket,body,anchor);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(new(0,-9.8,0)));
        world.Step([],[],.1);
        Near(default,body.Center); Near(default,body.LinearVelocity); Near(new(2,3,4),body.AngularVelocity);
    }

    [Theory]
    [InlineData(.5,.1,.1)]
    [InlineData(1,2,0)]
    public void RopeLeavesSlackFreeButStopsOutwardMotionAtFullExtension(double length,double speed,double expected)
    {
        var body=Body(0,new(length,0,0),new(speed,0,0)); var anchor=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),new([new(body,default),new(anchor,default)]),1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([],[Object(body),Object(anchor)],[rope],new(default));
        world.Step([],[],.01);
        Near(new(expected,0,0),body.LinearVelocity);
        Assert.InRange(rope.Error(1e-8),0,1e-7);
    }

    [Fact]
    public void OverextendedRopeUsesMassWeightedProjectionAndNeverPushes()
    {
        var a=Body(0,new(2,0,0)); var b=Body(1,default);
        var rope=new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),1,ConnectedBodyCollision.Enabled);
        PositionSolver.Solve(()=>[rope],new([a,b],(_,_)=>[],1e-6),1e-7);
        Near(new(1.5,0,0),a.Center); Near(new(.5,0,0),b.Center);
        Near(default,a.LinearVelocity); Near(default,b.LinearVelocity);
        a.ApplyImpulse(new(-1,0,0),a.Center);
        ImpulseSolver.Solve(rope.VelocityConstraints(1e-7));
        Near(new(-1,0,0),a.LinearVelocity);
    }

    [Fact]
    public void CollisionPolicyIsExplicitAndCanExposeAnImpossibleConstruction()
    {
        var a=Body(0,default); var b=Fixed(1);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,Origin,b,Origin,ConnectedBodyCollision.Enabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        var before=a.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.01));
        Assert.Equal(before,a.Snapshot()); Assert.Equal(0,world.Time);
    }

    [Fact]
    public void ImpactAndHingeReactInOneWorldSolve()
    {
        var beam=Body(0,default); var anchor=Fixed(1); var ball=Body(2,new(1,1,0),new(0,-10,0));
        var joint=Joint(FrameJointKind.Hinge,beam,anchor);
        var shape=new CompoundGeometry([new(new ConvexBox(new(2,.1,.1)),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(beam,shape,new(0,0,0)),Object(anchor),Object(ball)],[joint],new(default));
        var result=world.Step([],[],.1);
        Assert.True(result.Events>0);
        Assert.True(beam.AngularVelocity.Z<-.1);
        Assert.True(ball.LinearVelocity.Y>-10);
        Near(default,beam.PointVelocity(joint.FrameA.Anchor));
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Assert.All(world.Impacts.ToArray(),impact=>Assert.NotEqual(new PhysicsBodyId(1),impact.Pair.B));
    }

    [Fact]
    public void WorldRejectsForeignBodyReferencesAndDuplicateJointIdentities()
    {
        var a=Body(0,default); var b=Fixed(1); var foreign=Body(0,default);
        var joint=Joint(FrameJointKind.Hinge,a,b);
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([],[Object(a),Object(b)],[Joint(FrameJointKind.Hinge,foreign,b)],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([],[Object(a),Object(b)],[joint,joint],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsFrameJoint(new(0),(FrameJointKind)999,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both));
        Assert.Throws<ArgumentException>(()=>new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),0,ConnectedBodyCollision.Disabled));
    }
}
