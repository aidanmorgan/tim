using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JointRangeTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,RigidPose pose,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,pose,velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0));
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointTravelRange? range)=>
        new(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,range,JointTravelDirection.Both);

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void AssemblyCollectsOneBilateralBlockAndKeepsStopsUnilateral(FrameJointKind kind)
    {
        PhysicsBody Moving(int id,double speed)=>Body(id,RigidPose.Identity,
            kind==FrameJointKind.Slider?new(0,0,speed):default,
            kind==FrameJointKind.Hinge?new(0,0,speed):default);
        var a=Moving(0,1);var ground=Fixed(1);var b=Moving(2,3);
        var first=Joint(kind,a,ground,null);
        var second=new PhysicsFrameJoint(new(1),kind,b,Origin,ground,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var link=new PhysicsTransmissionJoint(new(2),first,second,1,TransmissionEngagement.Engaged);
        var rows=PhysicsJoint.CollectVelocityConstraints([first,second,link],1e-7);
        var block=Assert.IsType<BilateralConstraintBlock>(Assert.Single(rows));
        block.Solve();
        Assert.InRange(Math.Abs(first.Travel.Jacobian.Bind(a,ground).Speed-2),0,1e-12);
        Assert.InRange(Math.Abs(second.Travel.Jacobian.Bind(b,ground).Speed-2),0,1e-12);
        var stop=Joint(kind,a,ground,new(0,.5));
        Assert.Single(stop.UnilateralVelocityConstraints(1e-7));
        var locked=Joint(kind,a,ground,new(0,0));
        Assert.Empty(locked.UnilateralVelocityConstraints(1e-7));
        var acceleration=locked.AccelerationConstraints(locked.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8);
        Assert.Equal(6,acceleration.Count);
        Assert.All(acceleration,row=>Assert.Equal(AccelerationRelation.Equal,row.Relation));
        Assert.Equal(first.BilateralVelocityGradients().Count+1,locked.BilateralVelocityGradients().Count);
        Assert.IsType<BilateralConstraintBlock>(Assert.Single(locked.VelocityConstraints(1e-7)));
        Assert.Empty(PhysicsJoint.CollectVelocityConstraints([],1e-7));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PhysicsJoint.CollectVelocityConstraints([],0));
        Assert.Throws<ArgumentNullException>(()=>PhysicsJoint.CollectVelocityConstraints([null!],1e-7));
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge,-1)]
    [InlineData(FrameJointKind.Hinge,1)]
    [InlineData(FrameJointKind.Slider,-1)]
    [InlineData(FrameJointKind.Slider,1)]
    public void StopsHighSpeedTravelAndReleasesInward(FrameJointKind kind,int sign)
    {
        var a=Body(0,RigidPose.Identity,kind==FrameJointKind.Slider?new(0,0,sign*10000):default,
            kind==FrameJointKind.Hinge?new(0,0,sign*10000):default);
        var b=Fixed(1); var joint=Joint(kind,a,b,new(-.5,.5));
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        var before=world.Capture();
        world.Step([],[],.01);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign*.5),0,1e-7);
        Assert.InRange(a.LinearVelocity.Length+a.AngularVelocity.Length,0,1e-7);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        var after=world.Capture();
        world.Restore(before); world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.Time,world.Time);
        if(kind==FrameJointKind.Slider) world.ApplyImpulse(a.Id,new(0,0,-sign),a.Center);
        else world.ApplyAngularImpulse(a.Id,new(0,0,-sign*.1));
        world.Step([],[],.01);
        Assert.True(sign*joint.Travel.Error<.5-.009);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge,-1)]
    [InlineData(FrameJointKind.Hinge,1)]
    [InlineData(FrameJointKind.Slider,-1)]
    [InlineData(FrameJointKind.Slider,1)]
    public void OutOfRangeConstructionProjectsWithoutPhysicalVelocity(FrameJointKind kind,int sign)
    {
        var pose=kind==FrameJointKind.Hinge?new RigidPose(default,RigidRotation.FromRotationVector(new(0,0,sign))):
            RigidPose.At(new(0,0,sign));
        var a=Body(0,pose); var b=Fixed(1); var joint=Joint(kind,a,b,new(-.5,.5));
        PositionSolver.Solve(()=>[joint],new([a,b],(_,_)=>[],1e-6),1e-7);
        Assert.InRange(Math.Abs(joint.Travel.Error-sign*.5),0,1e-7);
        Assert.Equal(default,a.LinearVelocity); Assert.Equal(default,a.AngularMomentum);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void InteriorMotionIsNotLockedAndZeroWidthRangeLocks(FrameJointKind kind)
    {
        var a=Body(0,RigidPose.Identity,kind==FrameJointKind.Slider?new(0,0,.1):default,
            kind==FrameJointKind.Hinge?new(0,0,.1):default);
        var b=Fixed(1); var free=Joint(kind,a,b,new(-.5,.5));
        ImpulseSolver.Solve(free.VelocityConstraints(1e-7));
        Assert.InRange(Math.Abs(a.LinearVelocity.Z+a.AngularVelocity.Z-.1),0,1e-12);
        var locked=Joint(kind,a,b,new(0,0));
        ImpulseSolver.Solve(locked.VelocityConstraints(1e-7));
        Assert.InRange(a.LinearVelocity.Length+a.AngularVelocity.Length,0,1e-12);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void TravelJacobianMatchesFiniteDifferenceForMovingRotatedFrames(FrameJointKind kind)
    {
        var random=new Random(7201);
        double Number()=>random.NextDouble()*2-1;
        CollisionVector Vector()=>new(Number(),Number(),Number());
        for(var sample=0;sample<100;sample++)
        {
            var a=Body(0,new(Vector(),RigidRotation.FromRotationVector(Vector())),Vector(),Vector());
            var b=Body(1,new(Vector(),RigidRotation.FromRotationVector(Vector())),Vector(),Vector());
            var la=new JointFrame(Vector(),RigidRotation.FromRotationVector(Vector()*.3));
            var lb=new JointFrame(Vector(),RigidRotation.FromRotationVector(Vector()*.3));
            var joint=new PhysicsFrameJoint(new(0),kind,a,la,b,lb,ConnectedBodyCollision.Enabled,null,JointTravelDirection.Both);
            var equation=joint.Travel; var j=equation.Jacobian;
            var rate=CollisionVector.Dot(j.LinearA,a.LinearVelocity)+CollisionVector.Dot(j.AngularA,a.AngularVelocity)+
                CollisionVector.Dot(j.LinearB,b.LinearVelocity)+CollisionVector.Dot(j.AngularB,b.AngularVelocity);
            const double dt=1e-7;
            a.Advance(a.CreateTrajectory(dt,default),dt); b.Advance(b.CreateTrajectory(dt,default),dt);
            var delta=joint.Travel.Error-equation.Error;
            if(kind==FrameJointKind.Hinge) delta=Math.IEEERemainder(delta,Math.Tau);
            Assert.InRange(Math.Abs(delta/dt-rate),0,2e-6);
        }
    }

    [Fact]
    public void TwoMovingSliderBodiesExchangeMomentumAtStop()
    {
        var a=Body(0,RigidPose.At(new(0,0,.5)),new(0,0,2));
        var b=Body(1,RigidPose.Identity,new(0,0,-1));
        var joint=Joint(FrameJointKind.Slider,a,b,new(-.5,.5));
        ImpulseSolver.Solve(joint.VelocityConstraints(1e-7));
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-.5),0,1e-12);
        Assert.Equal(a.LinearVelocity,b.LinearVelocity);
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void RotatedCarrierAndOffCentreAttachmentRespectTravel(FrameJointKind kind)
    {
        var orientation=RigidRotation.FromRotationVector(new(.4,.7,-.3));
        var axis=orientation.Apply(new(0,0,1));
        var offset=orientation.Apply(new(.2,.1,.3));
        var a=Body(0,new(offset,orientation),kind==FrameJointKind.Slider?axis*100:default,
            kind==FrameJointKind.Hinge?axis*100:default);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,new(default,orientation),default,default);
        var localA=new JointFrame(new(-.2,-.1,-.3),RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),kind,a,localA,b,Origin,ConnectedBodyCollision.Disabled,new(-.5,.5),JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default));
        for(var i=0;i<12;i++) world.Step([],[],1.0/120);
        Assert.InRange(joint.Travel.Error,-.5000001,.5000001);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
    }

    [Fact]
    public void SliderContactCanStopBeforeItsTravelLimit()
    {
        var a=Body(0,RigidPose.Identity,new(0,0,100)); var b=Fixed(1);
        var wall=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(0,0,.3)),default,default);
        var joint=Joint(FrameJointKind.Slider,a,b,new(-.5,.5));
        var geometry=new CompoundGeometry([new(new ConvexBox(new(1,1,.001)),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[Object(a),Object(b),new(wall,geometry,new(0,0,0))],[joint],new(default));
        var result=world.Step([],[],.01);
        Assert.True(result.Events>0);
        Assert.InRange(a.Center.Z,.1988,.1991);
        Assert.InRange(a.LinearVelocity.Length,0,1e-8);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
    }

    [Fact]
    public void InvalidRangesAndUnsupportedAngularBranchesAreRejected()
    {
        var a=Body(0,RigidPose.Identity); var b=Fixed(1);
        Assert.Throws<ArgumentException>(()=>new JointTravelRange(1,-1));
        Assert.Throws<ArgumentException>(()=>new JointTravelRange(double.NaN,1));
        Assert.Throws<ArgumentException>(()=>new JointTravelRange(0,double.PositiveInfinity));
        Assert.Throws<ArgumentException>(()=>Joint(FrameJointKind.BallSocket,a,b,new(-1,1)));
        Assert.Throws<ArgumentException>(()=>Joint(FrameJointKind.Hinge,a,b,new(-Math.PI,1)));
        Assert.Throws<ArgumentException>(()=>Joint(FrameJointKind.Hinge,a,b,new(-1,Math.PI)));
        a.Restore(a.Snapshot() with {Pose=new(default,RigidRotation.FromRotationVector(new(Math.PI,0,0)))});
        Assert.Throws<InvalidOperationException>(()=>Joint(FrameJointKind.Hinge,a,b,new(-1,1)).Travel);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PredictionStageCannotChangeCommittedStopActivation(bool active)
    {
        var a=Body(0,RigidPose.At(new(0,0,active?0:-.1)),active?default:new(0,0,-1));
        var ground=Fixed(1);var joint=Joint(FrameJointKind.Slider,a,ground,new(-1,0));
        var sampled=Body(0,RigidPose.At(new(0,0,active?-.1:0)),active?new(0,0,-1):default);
        var states=new Dictionary<PhysicsBodyId,PhysicsBody>{{a.Id,sampled},{ground.Id,ground}};
        var rows=joint.AccelerationConstraints(states,1e-7,1e-8);
        Assert.Equal(active?6:5,rows.Count);
        Assert.Equal(active?1:0,rows.Count(row=>row.Relation==AccelerationRelation.Nonpositive));
        Assert.All(rows,row=>Assert.DoesNotContain(row.Gradient.Bodies.ToArray(),body=>ReferenceEquals(body,a)));
    }
}
