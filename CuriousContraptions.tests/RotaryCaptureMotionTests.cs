using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RotaryCaptureMotionTests
{
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(1,false)]
    [InlineData(-1,false)]
    [InlineData(1,true)]
    [InlineData(-1,true)]
    public void RotatingSourceIsSampledAtPredictionStagesAndReplays(int sign,bool rotated)
    {
        var rotation=rotated?RigidRotation.FromRotationVector(new(.4,.7,-.3)):RigidRotation.Identity;
        var pose=new RigidPose(default,rotation);
        var rotor=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,pose,default,default,1,new(1,1,1));
        var carrier=new PhysicsBody(new(1),PhysicsMotionType.Static,pose,default,default);
        var source=new PhysicsBody(new(2),PhysicsMotionType.Kinematic,
            new(rotation.Apply(new(0,0,-2*sign)),rotation),default,rotation.Apply(new(0,2,0)));
        var frame=new JointFrame(default,RigidRotation.Identity);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(rotor),Object(carrier),Object(source)],[joint],new(default,maximumStep:.1));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        var supply=new MechanicalTransferSource(new StoredFlowSource(new(0),source.Id,12,new(6,72)));
        var material=new RotaryCaptureMaterial(.4,1.2,100,.05);
        RotaryCaptureTransfer[] samples=[new(new(0),supply,
            new(source.Id,carrier.Id,default,new(0,0,sign),default,10,10,[rotor.Id]),new(.5,6),1e-8)];
        var load=new RotaryCaptureDeclaration(joint.Id,material,samples,1e-7);
        samples[0]=null!; // Declaration owns its immutable sample list.
        world.ReplaceLoads(world.Loads with {Rotary=[load]});
        var initial=world.Capture();
        world.Step([],[],.1);
        // At the midpoint the rotating nozzle has axial material speed
        // -4*sin(.1) at the carrier. Actual slip is force-capped; calibration
        // at the requested7.2*cos(.1) target is unsaturated.
        var alignment=Math.Cos(.1);
        var targetForce=.5*(12+4*Math.Sin(.1)-.4*7.2*alignment*alignment);
        var resistance=.4*targetForce/7.2;
        var expected=sign*.1*6*.4*alignment/(1+.1*resistance/2);
        var speed=joint.Travel.Jacobian.Bind(rotor,carrier).Speed;
        Assert.InRange(Math.Abs(speed-expected),0,1e-8);
        var held=sign*.1*6*.4/(1+.1*(.4*(.5*(12-.4*7.2))/7.2)/2);
        Assert.True(Math.Abs(speed-held)>1e-4);
        Assert.InRange(Math.Abs(joint.Travel.Error-.05*expected),0,1e-8);
        Assert.True(world.EnergyStore(source.Id).ReleasedEnergy>0);
        Assert.True(rotor.KineticEnergy<=world.EnergyStore(source.Id).ReleasedEnergy+1e-8);
        var final=world.Capture();
        world.ReplaceLoads(world.Loads with {Rotary=[]});world.Restore(initial);
        Assert.Same(load,Assert.Single(world.Loads.Rotary));
        world.Step([],[],.1);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());

        var invalid=new RotaryCaptureDeclaration(joint.Id,material,
            [new(new(0),supply,new(source.Id,rotor.Id,default,new(0,0,sign),default,10,10,[]),new(.5,6),1e-8)],1e-7);
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Rotary=[invalid]}));
        Assert.Same(load,Assert.Single(world.Loads.Rotary));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        var reversed=new PhysicsFrameJoint(joint.Id,FrameJointKind.Hinge,carrier,frame,rotor,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([reversed]));
        Assert.Same(joint,Assert.Single(world.Joints.ToArray()));
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(0)]
    [InlineData(2)]
    public void CalmAirDissipatesRelativeMotionAndBalancesTwoBodyMomentum(double speed)
    {
        var frame=new JointFrame(default,RigidRotation.Identity);
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,new(0,0,speed),1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(2,2,2));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,a,frame,b,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.1));
        world.ReplaceLoads(world.Loads with {Rotary=[new(joint.Id,new(.4,1.2,12,.05),[],1e-7)]});
        var energy=a.KineticEnergy+b.KineticEnergy;
        var momentum=a.AngularMomentum+b.AngularMomentum;
        var factor=.1*(.4/1.2)*1.5/2;
        world.Step([],[],.1);
        Assert.InRange(Math.Abs(joint.Travel.Jacobian.Bind(a,b).Speed-speed*(1-factor)/(1+factor)),0,1e-8);
        Assert.InRange((a.AngularMomentum+b.AngularMomentum-momentum).Length,0,1e-8);
        Assert.True(a.KineticEnergy+b.KineticEnergy<=energy);
    }

    [Fact]
    public void InvalidLawsAndUnknownSamplesReject()
    {
        foreach(var value in new[]{0d,-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(value,1,12,.05));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(.4,value,12,.05));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(.4,1,value,.05));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(.4,1,12,value));
        }
        Assert.Throws<ArgumentNullException>(()=>new RotaryCaptureDeclaration(new(0),new(.4,1,12,.05),null!,1e-7));
        Assert.Throws<ArgumentException>(()=>new RotaryCaptureDeclaration(new(0),new(.4,1,12,.05),[null!],1e-7));
    }
}
