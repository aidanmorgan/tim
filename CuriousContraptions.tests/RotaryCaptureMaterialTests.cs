using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RotaryCaptureMaterialTests
{
    private static RotaryCaptureBranch Branch(int id,double flow,double alignment=1,double conductance=.75,double cap=9)=>
        new(new(id),new(conductance,cap),flow,0,alignment);
    private static double Torque(RotaryCaptureBranch[] branches,RotaryCaptureSetting setting,double speed)=>
        branches.Sum(branch=>setting.Pitch*branch.Alignment*branch.Impedance.Evaluate(
            branch.SourceSpeed,branch.LinearSpeed+setting.Pitch*branch.Alignment*speed).Force)
        +setting.CreateDamping(new(0)).Effort(speed);

    [Theory]
    [InlineData(12,2.0/3,1,6,.4,.48)]
    [InlineData(12,2.0/3,-1,-6,.4,.48)]
    [InlineData(12,4,1,12,.4,.18)]
    [InlineData(1,4,1,3,1.0/3,0)]
    [InlineData(1,4,-1,-3,1.0/3,0)]
    public void ResponseUsesPassiveResistanceAndAFeasibleRatio(double flow,double gain,double alignment,
        double target,double pitch,double damping)
    {
        RotaryCaptureBranch[] branches=[Branch(0,flow,alignment)];
        var setting=new RotaryCaptureMaterial(.4,gain,12,.05).Calibrate(branches);
        Assert.Equal(RotaryCaptureState.Driven,setting.State);
        Assert.Equal(target,setting.TargetSpeed);
        Assert.InRange(Math.Abs(setting.Pitch-pitch),0,1e-15);
        Assert.InRange(Math.Abs(setting.DampingCoefficient-damping),0,1e-14);
        Assert.InRange(Math.Abs(Torque(branches,setting,target)),0,1e-12);
        Assert.True(Math.Sign(target)*Torque(branches,setting,target*.9)>0);
        Assert.True(Math.Sign(target)*Torque(branches,setting,target*1.1)<0);
        Assert.True(setting.DampingCoefficient>0);
    }

    [Fact]
    public void SharedOpposingBranchesHaveOneOrderIndependentOperatingPoint()
    {
        RotaryCaptureBranch[] branches=[Branch(7,12),Branch(2,2,-1)];
        var material=new RotaryCaptureMaterial(.4,1,12,.05);
        var setting=material.Calibrate(branches);
        Assert.Equal(7.5,setting.TargetSpeed);
        Assert.InRange(Math.Abs(setting.DampingCoefficient-.16),0,1e-14);
        Assert.Equal(setting,material.Calibrate(branches.Reverse()));
        Assert.InRange(Math.Abs(Torque(branches,setting,7.5)),0,1e-12);
        Assert.True(Torque(branches,setting,7)>0);
        Assert.True(Torque(branches,setting,8)<0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.049)]
    [InlineData(.05)]
    public void CutInBoundaryAndCalmResistanceAreExplicit(double force)
    {
        var material=new RotaryCaptureMaterial(.4,1,12,.05);
        var setting=material.Calibrate([Branch(0,force,1,1,9)]);
        Assert.Equal(force<.05?RotaryCaptureState.Passive:RotaryCaptureState.Driven,setting.State);
        if(setting.State==RotaryCaptureState.Passive)
        {
            Assert.Equal(0,setting.TargetSpeed);
            Assert.Equal(.4,setting.DampingCoefficient);
        }
        foreach(var speed in new[]{-12.0,0,12.0})
            Assert.True(setting.CreateDamping(new(0)).Effort(speed)*speed<=0);
    }

    [Fact]
    public void OpposingAndEdgeOnFlowCannotProduceAHiddenTarget()
    {
        var material=new RotaryCaptureMaterial(.4,1,12,.05);
        Assert.Equal(RotaryCaptureState.Passive,material.Calibrate([Branch(0,12),Branch(1,12,-1)]).State);
        Assert.Equal(RotaryCaptureState.Passive,material.Calibrate([Branch(0,12,0)]).State);
        Assert.Equal(RotaryCaptureState.Passive,material.Calibrate([]).State);
    }

    [Fact]
    public void SaturatedMaterialRetainsItsActualForceCurve()
    {
        RotaryCaptureBranch[] branches=[Branch(0,12,1,100,1)];
        var setting=new RotaryCaptureMaterial(.4,2,12,.05).Calibrate(branches);
        Assert.Equal(2,setting.TargetSpeed);
        Assert.Equal(.2,setting.DampingCoefficient);
        Assert.InRange(Math.Abs(Torque(branches,setting,2)),0,1e-12);
    }

    [Fact]
    public void PairedPowerRemainsDissipativeAcrossTheOperatingPoint()
    {
        RotaryCaptureBranch[] branches=[Branch(0,1),Branch(1,12,-1)];
        var setting=new RotaryCaptureMaterial(.4,4,12,.05).Calibrate(branches);
        foreach(var speed in new[]{-20.0,setting.TargetSpeed,0,20.0})
        {
            double supplied=0,received=0,loss=0;
            foreach(var branch in branches)
            {
                var response=branch.Impedance.Evaluate(branch.SourceSpeed,
                    branch.LinearSpeed+setting.Pitch*branch.Alignment*speed);
                supplied+=response.SourcePower;received+=response.ReceiverPower;loss+=response.DissipatedPower;
            }
            Assert.InRange(Math.Abs(supplied-received-loss),0,1e-12);
            Assert.True(loss>=0);
            Assert.True(setting.CreateDamping(new(0)).Effort(speed)*speed<=0);
        }
    }

    public enum Parameter { Pitch, Response, MaximumSpeed, CutIn }
    [Theory]
    [InlineData(Parameter.Pitch)]
    [InlineData(Parameter.Response)]
    [InlineData(Parameter.MaximumSpeed)]
    [InlineData(Parameter.CutIn)]
    public void InvalidParametersReject(Parameter parameter)
    {
        Assert.True(Enum.IsDefined(parameter));
        foreach(var value in new[]{0.0,-1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(
                parameter==Parameter.Pitch?value:.4,parameter==Parameter.Response?value:1,
                parameter==Parameter.MaximumSpeed?value:12,parameter==Parameter.CutIn?value:.05));
    }

    [Fact]
    public void InvalidBranchesAndUnrepresentableRangesReject()
    {
        var material=new RotaryCaptureMaterial(.4,1,12,.05);
        var branch=Branch(0,12);
        Assert.Throws<ArgumentException>(()=>material.Calibrate([branch,branch]));
        Assert.Throws<ArgumentException>(()=>material.Calibrate([null!]));
        Assert.Throws<ArgumentNullException>(()=>material.Calibrate(null!));
        foreach(var alignment in new[]{-2.0,2,double.NaN})
            Assert.Throws<ArgumentOutOfRangeException>(()=>Branch(0,12,alignment));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Branch(0,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(double.MaxValue,double.Epsilon,12,.05));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new RotaryCaptureMaterial(double.Epsilon,double.MaxValue,12,.05));
        Assert.Throws<InvalidOperationException>(()=>new RotaryCaptureMaterial(double.MaxValue,1,12,.05)
            .Calibrate([Branch(0,12)]));
    }

    [Theory]
    [InlineData(2.0/3,1)]
    [InlineData(2.0/3,-1)]
    [InlineData(4,1)]
    [InlineData(4,-1)]
    public void CalibratedOperatingPointUsesPaidPairedWorkAndReplays(double gain,int sign)
    {
        var z=new CollisionVector(0,0,1);
        var material=new JetTransferImpedance(.75,9);
        var setting=new RotaryCaptureMaterial(.4,gain,12,.05).Calibrate([Branch(0,12,sign)]);
        var nozzle=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var carrier=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(z*(2*sign)),default,default);
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,default,z*setting.TargetSpeed,1,new(1,1,1));
        var frame=new JointFrame(default,RigidRotation.Identity);
        var hinge=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var shape=new CompoundGeometry([new(new ConvexSphere(.01),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        var world=new PhysicsWorld([],new[]{nozzle,carrier,rotor}.Select(body=>
            new PhysicsObject(body,shape,new(0,0,0))).ToArray(),[hinge],new(default,maximumStep:.01));
        world.InstallEnergyStores([new(nozzle.Id,100,100)]);
        var field=new AirJetGeometry(nozzle.Id,carrier.Id,default,z*sign,default,4,1,[rotor.Id]);
        var transfer=new NozzleCoupledJetReceiver(rotor.Id,z,setting.Pitch).CreateLoad(new(0),
            new(new StoredFlowSource(new(0),nozzle.Id,12,new(9,108))),field,material,1e-10);
        world.ReplaceLoads(new(){Transfers=[transfer],Damping=[setting.CreateDamping(hinge.Id)]});
        var before=world.Capture();var energy=rotor.KineticEnergy;
        var result=world.Step([],[],.01);
        Assert.InRange(Math.Abs(rotor.AngularVelocity.Z-setting.TargetSpeed),0,1e-9);
        Assert.InRange(Math.Abs(rotor.KineticEnergy-energy),0,1e-9);
        var force=.75*(12-setting.Pitch*Math.Abs(setting.TargetSpeed));
        var work=Assert.Single(world.TransferTotals.ToArray());
        Assert.InRange(Math.Abs(work.SourceExtraction.Supplied-force*12*.01),0,1e-10);
        Assert.InRange(Math.Abs(work.ReceiverDelivery.Supplied-force*setting.Pitch*Math.Abs(setting.TargetSpeed)*.01),0,1e-10);
        Assert.InRange(Math.Abs(world.EnergyStore(nozzle.Id).ReleasedEnergy-work.SourceExtraction.Supplied),0,1e-10);
        Assert.True(work.ReceiverDelivery.Supplied>0);
        var after=world.Capture();
        world.Restore(before);
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
