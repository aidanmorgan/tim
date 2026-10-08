using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class TransmissionJointTests
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Shaft(int id,double inertia,double speed=0)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(X*id*3),default,Z*speed,1,new(inertia,inertia,inertia));
    private static PhysicsBody Carrier()=>new(new(9),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsFrameJoint Hinge(int id,PhysicsBody shaft,PhysicsBody carrier)=>new(new(id),FrameJointKind.Hinge,
        shaft,Origin,carrier,new(shaft.Center,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
    private static void Near(double expected,double actual,double tolerance=1e-8)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(-2)]
    public void EngagementSharesRealInertiaAndCannotCreateEnergy(double ratio)
    {
        var a=Shaft(0,2,10);var b=Shaft(1,3);var carrier=Carrier();
        var joint=new PhysicsTransmissionJoint(new(2),Hinge(0,a,carrier),Hinge(1,b,carrier),ratio,TransmissionEngagement.Engaged);
        Assert.Equal(3,joint.Bodies.Length);
        var before=a.KineticEnergy+b.KineticEnergy;
        ImpulseSolver.Solve(joint.VelocityConstraints(1e-7),tolerance:1e-10);
        var expected=20/(2+3*ratio*ratio);
        Near(expected,a.AngularVelocity.Z);Near(ratio*expected,b.AngularVelocity.Z);
        Near(0,joint.SpeedError);
        Assert.True(a.KineticEnergy+b.KineticEnergy<=before);
        Near(20,2*a.AngularVelocity.Z+ratio*3*b.AngularVelocity.Z);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-2)]
    public void DownstreamLoadReactsBackThroughTheSharedAccelerationSolve(double ratio)
    {
        var a=Shaft(0,2);var b=Shaft(1,3);var carrier=Carrier();
        var input=Hinge(0,a,carrier);var output=Hinge(1,b,carrier);
        var joint=new PhysicsTransmissionJoint(new(2),input,output,ratio,TransmissionEngagement.Engaged);
        var load=-Math.Sign(ratio)*4;
        var forces=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(default,Z*10)},{b.Id,new(default,Z*load)},{carrier.Id,default}};
        var solved=AccelerationSolver.Solve([a,b,carrier],joint.AccelerationConstraints(joint.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8),[],forces,1e-9,out _,[],out _,out _,out _);
        var acceleration=(10+ratio*load)/(2+ratio*ratio*3);
        Near(acceleration,a.InverseInertia(solved[a.Id].Torque).Z);
        Near(ratio*acceleration,b.InverseInertia(solved[b.Id].Torque).Z);
        Assert.Equal(default,solved[carrier.Id]);
    }

    [Fact]
    public void BranchesShareSourceInertiaInsteadOfDuplicatingDrive()
    {
        var a=Shaft(0,2,12);var b=Shaft(1,3);var c=Shaft(2,5);var carrier=Carrier();
        var input=Hinge(0,a,carrier);
        var first=new PhysicsTransmissionJoint(new(3),input,Hinge(1,b,carrier),1,TransmissionEngagement.Engaged);
        var second=new PhysicsTransmissionJoint(new(4),input,Hinge(2,c,carrier),-1,TransmissionEngagement.Engaged);
        var rows=first.VelocityConstraints(1e-7).Concat(second.VelocityConstraints(1e-7)).ToArray();
        ImpulseSolver.Solve(rows,tolerance:1e-10);
        Near(2.4,a.AngularVelocity.Z);Near(2.4,b.AngularVelocity.Z);Near(-2.4,c.AngularVelocity.Z);
        Near(28.8,a.KineticEnergy+b.KineticEnergy+c.KineticEnergy);
    }

    [Fact]
    public void HingeToSliderLeadReflectsLinearMassIntoShaftInertia()
    {
        var a=Shaft(0,2,4);var carrier=Carrier();
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*3),default,default,3,new(1,1,1));
        var slider=new PhysicsFrameJoint(new(1),FrameJointKind.Slider,b,Origin,carrier,
            new(b.Center,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        const double lead=.2;
        var joint=new PhysicsTransmissionJoint(new(2),Hinge(0,a,carrier),slider,lead,TransmissionEngagement.Engaged);
        ImpulseSolver.Solve(joint.VelocityConstraints(1e-7),tolerance:1e-10);
        var speed=8/(2+3*lead*lead);
        Near(speed,a.AngularVelocity.Z);Near(lead*speed,b.LinearVelocity.Z);
        Near(0,joint.SpeedError);
    }

    [Fact]
    public void WorldIntegrationCrossesMultipleTurnsAndReplaysExactly()
    {
        var a=Shaft(0,2,80);var b=Shaft(1,3,-160);var carrier=Carrier();
        var input=Hinge(0,a,carrier);var output=Hinge(1,b,carrier);
        var joint=new PhysicsTransmissionJoint(new(2),input,output,-2,TransmissionEngagement.Engaged);
        var shape=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        PhysicsObject Object(PhysicsBody body)=>new(body,shape,new(0,0,0));
        var world=new PhysicsWorld([],[Object(a),Object(b),Object(carrier)],[input,output,joint],new(default,maximumStep:.01));
        var initial=world.Capture();var energy=a.KineticEnergy+b.KineticEnergy;
        world.Step([],[],.2);
        Near(0,joint.SpeedError);Near(energy,a.KineticEnergy+b.KineticEnergy,1e-6);
        var finalA=a.Snapshot();var finalB=b.Snapshot();
        world.Restore(initial);world.Step([],[],.2);
        Assert.Equal(finalA,a.Snapshot());Assert.Equal(finalB,b.Snapshot());
        var replacement=(PhysicsTransmissionJoint)joint.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>{{a.Id,a},{b.Id,b},{carrier.Id,carrier}});
        Near(joint.SpeedError,replacement.SpeedError);Assert.Equal(joint.Ratio,replacement.Ratio);
    }

    [Fact]
    public void RemovingOrReplacingAGuideRequiresUpdatingItsTransmissionAtomically()
    {
        var a=Shaft(0,2);var b=Shaft(1,3);var carrier=Carrier();
        var input=Hinge(0,a,carrier);var output=Hinge(1,b,carrier);
        var link=new PhysicsTransmissionJoint(new(2),input,output,1,TransmissionEngagement.Engaged);
        var shape=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        PhysicsObject Object(PhysicsBody body)=>new(body,shape,new(0,0,0));
        var objects=new[]{Object(a),Object(b),Object(carrier)};
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([],objects,[input,link],new(default)));
        var world=new PhysicsWorld([],objects,[input,output,link],new(default));
        var before=world.Capture();
        var replacement=Hinge(1,b,carrier);
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([input,link]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([input,replacement,link]));
        Assert.Same(output,world.Joints[1]);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        var updated=new PhysicsTransmissionJoint(link.Id,input,replacement,-1,TransmissionEngagement.Engaged);
        world.ReplaceJoints([input,replacement,updated]);
        Assert.Same(updated,world.Joints[2]);
        world.Restore(before);
        Assert.Same(output,world.Joints[1]);Assert.Same(link,world.Joints[2]);
    }

    [Theory]
    [InlineData(TransmissionEngagement.Open)]
    [InlineData(TransmissionEngagement.Engaged)]
    public void ExplicitEngagementControlsEquationsAndSurvivesRebinding(TransmissionEngagement engagement)
    {
        var a=Shaft(0,2,10);var b=Shaft(1,3);var carrier=Carrier();
        var input=Hinge(0,a,carrier);var output=Hinge(1,b,carrier);
        var joint=new PhysicsTransmissionJoint(new(2),input,output,1,engagement);
        var rebound=(PhysicsTransmissionJoint)joint.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>{{a.Id,a},{b.Id,b},{carrier.Id,carrier}});
        Assert.Equal(engagement,rebound.Engagement);
        var rows=joint.VelocityConstraints(1e-7);
        Assert.Equal(engagement==TransmissionEngagement.Open?0:1,rows.Count);
        Assert.Equal(rows.Count,joint.AccelerationConstraints(joint.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8).Count);
        ImpulseSolver.Solve(rows,tolerance:1e-10);
        if(engagement==TransmissionEngagement.Open) {Near(10,a.AngularVelocity.Z);Near(0,b.AngularVelocity.Z);}
        else {Near(4,a.AngularVelocity.Z);Near(4,b.AngularVelocity.Z);}
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsTransmissionJoint(new(2),input,output,1,(TransmissionEngagement)99));
    }

    [Fact]
    public void InvalidRatiosAndNonAxialEndpointsReject()
    {
        var a=Shaft(0,2);var b=Shaft(1,3);var carrier=Carrier();
        var input=Hinge(0,a,carrier);var output=Hinge(1,b,carrier);
        foreach(var ratio in new[]{0,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsTransmissionJoint(new(2),input,output,ratio,TransmissionEngagement.Engaged));
        Assert.Throws<ArgumentException>(()=>new PhysicsTransmissionJoint(new(2),input,input,1,TransmissionEngagement.Engaged));
        var ball=new PhysicsFrameJoint(new(1),FrameJointKind.BallSocket,b,Origin,carrier,
            new(b.Center,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        Assert.Throws<ArgumentException>(()=>new PhysicsTransmissionJoint(new(2),input,ball,1,TransmissionEngagement.Engaged));
    }
}
