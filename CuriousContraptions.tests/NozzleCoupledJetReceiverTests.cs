using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class NozzleCoupledJetReceiverTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);
    private static void NearZero(CollisionVector vector)=>Assert.InRange(vector.Length,0,1e-10);

    [Theory]
    [InlineData(JetReceiverMotion.Linear)]
    [InlineData(JetReceiverMotion.Rotary)]
    public void OffsetReceiverReturnsForceAndTotalMomentToNozzle(JetReceiverMotion motion)
    {
        var spin=new CollisionVector(.2,.3,.4);var drift=new CollisionVector(1,2,3);
        PhysicsBody Body(int id,CollisionVector center)=>new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),
            drift+CollisionVector.Cross(spin,center),spin,1,new(1,1,1));
        var nozzle=Body(0,new(-2,.3,0));var sample=Body(1,new(2,.5,0));var rotor=Body(2,new(2,.7,.1));
        var bodies=new[]{nozzle,sample,rotor}.ToDictionary(body=>body.Id);
        var field=new AirJetGeometry(nozzle.Id,sample.Id,default,X,new(0,.2,.1),6,2,[rotor.Id]);
        var receiver=motion switch
        {
            JetReceiverMotion.Linear=>new NozzleCoupledJetReceiver(),
            JetReceiverMotion.Rotary=>new NozzleCoupledJetReceiver(rotor.Id,X,.4),
            _=>throw new ArgumentOutOfRangeException(nameof(motion))
        };
        Assert.Equal(motion,receiver.Motion);
        var gradient=receiver.CreatePort(field).Bind(bodies,[]);
        Near(0,gradient.Speed);
        CollisionVector force=default,moment=default;
        foreach(var term in gradient.Terms)
        {
            force+=term.Linear;
            moment+=term.Angular+CollisionVector.Cross(term.Body.Center,term.Linear);
        }
        NearZero(force);NearZero(moment);
        var reaction=gradient.Terms.ToArray().Single(term=>term.Body==nozzle);
        Assert.Equal(-X,reaction.Linear);
        var point=sample.Pose.TransformPoint(field.LocalPoint);
        var expected=-CollisionVector.Cross(point-nozzle.Center,X)-
            (motion==JetReceiverMotion.Rotary?X*.4:default);
        NearZero(reaction.Angular-expected);
        // No rotor-bearing frame is inserted as a hidden torque recipient.
        if(motion==JetReceiverMotion.Rotary)
        {
            var spinTerm=gradient.Terms.ToArray().Single(term=>term.Body==rotor);
            Assert.Equal(default,spinTerm.Linear);NearZero(spinTerm.Angular-X*.4);
        }
    }

    [Fact]
    public void InvalidConversionAndNozzleAsRotorRejectBeforeLoadPublication()
    {
        Assert.Throws<ArgumentException>(()=>new NozzleCoupledJetReceiver(new(1),default,1));
        foreach(var pitch in new[]{0.0,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentException>(()=>new NozzleCoupledJetReceiver(new(1),X,pitch));
        var field=new AirJetGeometry(new(0),new(1),default,X,default,4,1,[]);
        Assert.Throws<ArgumentException>(()=>new NozzleCoupledJetReceiver(new(0),X,.4).CreatePort(field));
        Assert.Throws<ArgumentNullException>(()=>new NozzleCoupledJetReceiver().CreatePort(null!));
    }

    [Theory]
    [InlineData(false,false,1)]
    [InlineData(true,false,1)]
    [InlineData(false,true,1)]
    [InlineData(false,false,0)]
    public void MixedReceiversShareSupplyAndBalanceMovingNozzle(bool reverse,bool blocked,double energy)
    {
        const double h=.01;
        var nozzle=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var linear=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,1,new(1,1,1));
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(-X*2),default,default,1,new(2,2,2));
        var blocker=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.At(new(-1,blocked?0:3,0)),default,default);
        var objects=new[]{nozzle,linear,rotor,blocker}.Select(body=>new PhysicsObject(body,
            new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0))).ToArray();
        if(reverse)Array.Reverse(objects);
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:h));
        world.InstallEnergyStores([new(nozzle.Id,1,0)]);world.ChargeEnergyStore(nozzle.Id,energy,1);
        var source=new MechanicalTransferSource(new StoredFlowSource(new(0),nozzle.Id,4,new(1,100)));
        var forward=new AirJetGeometry(nozzle.Id,linear.Id,default,X,default,4,1,[]);
        var backward=new AirJetGeometry(nozzle.Id,rotor.Id,default,-X,default,4,1,[]);
        MechanicalTransferLoad[] loads=[
            new NozzleCoupledJetReceiver().CreateLoad(new(0),source,forward,new(1,1),1e-10),
            new NozzleCoupledJetReceiver(rotor.Id,X,.4).CreateLoad(new(1),source,backward,new(1,1),1e-10)];
        if(reverse)Array.Reverse(loads);
        world.ReplaceLoads(new(){Transfers=loads});
        var before=world.Capture();var result=world.Step([],[],h);
        var active=energy>0;
        var linearImpulse=active?(blocked?.01:.005):0;
        var rotorImpulse=active&&!blocked?.005:0;
        Near(linearImpulse,linear.LinearVelocity.X);Near(-rotorImpulse,rotor.LinearVelocity.X);
        Near(rotorImpulse-linearImpulse,nozzle.LinearVelocity.X);
        Near(-rotorImpulse*.4/2,rotor.AngularVelocity.X);Near(rotorImpulse*.4,nozzle.AngularVelocity.X);
        NearZero(nozzle.LinearVelocity+linear.LinearVelocity+rotor.LinearVelocity);
        NearZero(nozzle.AngularMomentum+linear.AngularMomentum+rotor.AngularMomentum);
        var work=active?.04:0;
        Near(work,world.EnergyStore(nozzle.Id).ReleasedEnergy);
        Near(work-nozzle.KineticEnergy-linear.KineticEnergy-rotor.KineticEnergy,
            world.TransferUse.ToArray().Sum(value=>value.PairedWork.Dissipated));
        Assert.Single(world.SourceUse.ToArray());
        var final=world.Capture();world.Restore(before);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(result,world.Step([],[],h));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
