using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SpringConstraintObservationTests
{
    public enum DriveCase { Winding, Off, Reverse, Equilibrium, Braking }
    public enum GuideRepresentation { Centered, OffCentre, AdditionalLatch, BilateralLock }
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Head,PhysicsJointId Guide,PhysicsJointId Hinge);
    private static Fixture Create(bool contact,double compression=.5,double scale=1,double angle=0,double lead=.1,GuideRepresentation representation=GuideRepresentation.Centered,double shaftScale=1)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(0,5,0)),default,default);
        var head=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,-compression)),default,default,scale,new(scale,2*scale,3*scale));
        var shaft=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(3,0,0)),default,default,shaftScale,new(shaftScale,shaftScale,shaftScale));
        var wall=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(0,0,-.52)),default,default);
        if(!Enum.IsDefined(representation))throw new ArgumentOutOfRangeException(nameof(representation));
        var offset=representation is GuideRepresentation.Centered or GuideRepresentation.BilateralLock?default:new CollisionVector(.2,.3,0);
        var rotation=RigidRotation.FromRotationVector(new CollisionVector(.3,.4,.5)*angle);
        foreach(var body in new[]{frame,head,shaft,wall})
            body.Restore(body.Snapshot() with {Pose=new(rotation.Apply(body.Center),rotation)});
        var guide=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,head,new(offset,RigidRotation.Identity),frame,new(offset+new CollisionVector(0,-5,0),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,new(-1,0),JointTravelDirection.Negative);
        var hinge=new PhysicsFrameJoint(new(1),FrameJointKind.Hinge,shaft,new(default,RigidRotation.Identity),frame,new(new(3,-5,0),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var transmission=new PhysicsTransmissionJoint(new(2),hinge,guide,lead,TransmissionEngagement.Engaged);
        var sphere=new CompoundGeometry([new(new ConvexSphere(.01),AffineTransform.Identity)]);
        var box=new CompoundGeometry([new(new ConvexBox(new(1,1,.01)),AffineTransform.Identity)]);
        var joints=new List<PhysicsJoint>{guide,hinge,transmission};
        if(representation==GuideRepresentation.AdditionalLatch)
            joints.Add(new PhysicsFrameJoint(new(3),FrameJointKind.Slider,head,guide.LocalA,frame,guide.LocalB,
                ConnectedBodyCollision.Disabled,new(-1,0),JointTravelDirection.Negative));
        if(representation==GuideRepresentation.BilateralLock)
            joints.Add(new PhysicsFrameJoint(new(3),FrameJointKind.BallSocket,head,new(default,RigidRotation.Identity),
                frame,new(new(0,-5,-compression),RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both));
        var world=new PhysicsWorld([],[new(frame,sphere,new(0,0,0)),new(head,sphere,new(0,0,0)),
            new(shaft,sphere,new(0,0,0)),new(wall,box,new(0,0,0))],joints,new(default,maximumStep:.005));
        if(!contact)world.ApplyColliderUpdates([new(wall.Id,box,new(0,0,0),CollisionParticipation.Disabled)]);
        world.ReplaceLoads(new(){Springs=[new(guide.Id,transmission.Id,100*scale,0,1)]});
        return new(world,head,guide.Id,hinge.Id);
    }
    private static PhysicsMotorCommand[] Commands(Fixture f,DriveCase drive)=>drive switch
    {
        DriveCase.Winding=>[new(f.Hinge,-2,100,100,1000000)],
        DriveCase.Off=>[],
        DriveCase.Reverse=>[new(f.Hinge,2,100,100,1000000)],
        DriveCase.Equilibrium=>[new(f.Hinge,-2,5,100,1000000)],
        DriveCase.Braking=>[new(f.Hinge,-2,100,0,1000000)],
        _=>throw new ArgumentOutOfRangeException(nameof(drive))
    };
    [Theory]
    [InlineData(DriveCase.Winding)]
    [InlineData(DriveCase.Off)]
    [InlineData(DriveCase.Reverse)]
    [InlineData(DriveCase.Equilibrium)]
    [InlineData(DriveCase.Braking)]
    public void CoincidentContactRequiresSignedWindingAndActualReaction(DriveCase drive)
    {
        var f=Create(true);
        f.World.Step([],Commands(f,drive),.01);
        var observation=f.World.Spring(f.Guide).ConstraintObservation;
        Assert.True((drive==DriveCase.Winding)==observation.OpposesWinding,$"drive={drive}, observation={observation}");
        if(drive==DriveCase.Winding)
        {
            // Shaft torque -100 / lead .1 = -1000 N. Spring returns +50 N;
            // the wall supplies the remaining +950 N with zero travel/work.
            Assert.InRange(Math.Abs(observation.TransmissionEffort+1000),0,1e-8);
            Assert.InRange(Math.Abs(observation.ContactEffort-950),0,1e-8);
            Assert.Equal(.5,f.World.Spring(f.Guide).Compression);
            Assert.InRange(f.World.MotorUse[0].SuppliedWork,0,1e-10);
        }
        else Assert.InRange(Math.Abs(observation.ContactEffort),0,1e-8);
    }
    [Theory]
    [InlineData(.01)]
    [InlineData(.005)]
    [InlineData(1e-12)]
    public void ClearHeadWithSubPrecisionTravelIsNeverAnObstruction(double duration)
    {
        var f=Create(false);
        f.World.Step([],Commands(f,DriveCase.Winding),duration);
        Assert.False(f.World.Spring(f.Guide).ConstraintObservation.OpposesWinding);
        Assert.True(f.Head.LinearVelocity.Z<0);
        if(duration==1e-12)Assert.Equal(.5,f.World.Spring(f.Guide).Compression);
    }
    [Theory]
    [InlineData(1,0,.1,.01)]
    [InlineData(1,1.7,.1,.005)]
    [InlineData(.001,1.7,.02,.01)]
    [InlineData(1000,.8,.3,.005)]
    public void BalancedAndResolvableMarginsRetainScaling(double scale,double angle,double lead,double duration)
    {
        var resolution=1e-9*(1+1/lead);
        foreach(var factor in new[]{-4.0,0,.1,4})
        {
            var margin=factor*resolution;
            var f=Create(true,scale:scale,angle:angle,lead:lead,shaftScale:scale);
            f.World.Step([],[new(f.Hinge,-2,(50+margin)*scale*lead,100,1000000)],duration);
            var observation=f.World.Spring(f.Guide).ConstraintObservation;
            Assert.True((factor>1)==observation.OpposesWinding,
                $"scale={scale:R}, angle={angle:R}, lead={lead:R}, margin={margin:R}, observation={observation}");
            Assert.InRange(Math.Abs(observation.TransmissionEffort+50*scale-margin*(-scale)),0,1e-7*scale);
        }
    }
    [Fact]
    public void HeavyShaftUsesItsActualAdmissibleAccelerationResolution()
    {
        const double headMass=.001,shaftInertia=1,lead=.02;
        var resolution=1e-9*(headMass+shaftInertia/lead);
        foreach(var factor in new[]{-4.0,0,.1,4})
        {
            var excess=factor*resolution;
            var f=Create(true,scale:headMass,angle:1.7,lead:lead,shaftScale:shaftInertia);
            f.World.Step([],[new(f.Hinge,-2,(50*headMass+excess)*lead,100,1000000)],.01);
            var observation=f.World.Spring(f.Guide).ConstraintObservation;
            Assert.True((factor>1)==observation.OpposesWinding,$"factor={factor:R}, observation={observation}");
            Assert.InRange(observation.EffortResolution,resolution*(1-1e-10),resolution*(1+1e-5));
        }
    }
    [Theory]
    [InlineData(GuideRepresentation.Centered,false)]
    [InlineData(GuideRepresentation.OffCentre,false)]
    [InlineData(GuideRepresentation.AdditionalLatch,false)]
    [InlineData(GuideRepresentation.Centered,true)]
    [InlineData(GuideRepresentation.OffCentre,true)]
    [InlineData(GuideRepresentation.AdditionalLatch,true)]
    public void EquivalentGuideFramesAndAdditionalLatchPreserveObstruction(GuideRepresentation representation,bool winding)
    {
        var f=Create(true,angle:1.3,representation:representation);
        f.World.Step([],Commands(f,winding?DriveCase.Winding:DriveCase.Equilibrium),.005);
        var observation=f.World.Spring(f.Guide).ConstraintObservation;
        Assert.True(winding==observation.OpposesWinding,$"representation={representation}, observation={observation}");
        Assert.InRange(Math.Abs(observation.WindingDriveEffort-(winding?-950:0)),0,1e-8);
    }
    [Fact]
    public void AdmittedBilateralLockHasAnExplicitNonWindingObservation()
    {
        var f=Create(false,representation:GuideRepresentation.BilateralLock);
        f.World.Step([],Commands(f,DriveCase.Winding),.01);
        var state=f.World.Spring(f.Guide);
        Assert.Equal(SpringResponseDomain.BilaterallyLocked,state.ConstraintObservation.Response);
        Assert.False(state.ConstraintObservation.OpposesWinding);
        Assert.Equal(.5,state.Compression);
        var before=f.World.Capture();
        f.World.Step([],[],.01);f.World.Restore(before);
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
    }
    [Fact]
    public void ObservationDomainRejectsUndefinedNonfiniteAndInventedLockedForces()
    {
        Assert.Throws<ArgumentException>(()=>new SpringConstraintObservation((SpringResponseDomain)999,0,0,0,0,0));
        Assert.Throws<ArgumentException>(()=>new SpringConstraintObservation(SpringResponseDomain.Admissible,double.NaN,0,0,0,0));
        Assert.Throws<ArgumentException>(()=>new SpringConstraintObservation(SpringResponseDomain.Admissible,0,0,0,0,-1));
        Assert.Throws<ArgumentException>(()=>new SpringConstraintObservation(SpringResponseDomain.BilaterallyLocked,1,0,0,0,0));
        Assert.False(default(SpringConstraintObservation).OpposesWinding);
    }
    [Fact]
    public void ShutdownReplacementAndSnapshotRestoreOwnTheExactObservation()
    {
        var f=Create(true);
        f.World.Step([],Commands(f,DriveCase.Winding),.01);
        var before=f.World.Capture();
        Assert.True(f.World.Spring(f.Guide).ConstraintObservation.OpposesWinding);
        f.World.Step([],[],.01);
        Assert.False(f.World.Spring(f.Guide).ConstraintObservation.OpposesWinding);
        f.World.Restore(before);
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
        var joints=f.World.Joints.ToArray();
        var bodies=joints.SelectMany(j=>j.Bodies.ToArray()).Distinct().ToDictionary(b=>b.Id);
        var guide=(PhysicsFrameJoint)joints.Single(j=>j.Id==f.Guide).Rebind(bodies);
        var hinge=(PhysicsFrameJoint)joints.Single(j=>j.Id==f.Hinge).Rebind(bodies);
        PhysicsJoint[] replacement=[guide,hinge,new PhysicsTransmissionJoint(new(2),hinge,guide,.1,TransmissionEngagement.Engaged)];
        f.World.ReplaceJoints(replacement);
        Assert.Equal(default,f.World.Spring(f.Guide).ConstraintObservation);
        f.World.Restore(before);
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceJoints([..joints,joints[0]]));
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        f.World.ReplaceLoads(new());
        Assert.Empty(f.World.Springs.ToArray());
        f.World.Restore(before);
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
    }
}
