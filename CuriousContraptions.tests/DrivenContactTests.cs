using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class DrivenContactTests
{
    private enum Participant { Load, Carrier, Shaft, SecondLoad }
    private static readonly CollisionVector X=new(1,0,0), Y=new(0,1,0), Z=new(0,0,1);
    private static PhysicsBody Body(Participant participant,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new((int)participant),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,spin,1,new(1,1,1));
    private static PhysicsBody Carrier()=>new(new((int)Participant.Carrier),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static ContactKinematics Map(PhysicsBody load,PhysicsBody carrier,PhysicsBody shaft,
        double ratio=1,bool reverse=false,RigidRotation? rotation=null)
    {
        var basis=rotation??RigidRotation.Identity;
        var rigid=ContactKinematics.AtPoint(reverse?carrier:load,reverse?load:carrier,default,reverse?-basis.Apply(Y):basis.Apply(Y));
        var axis=basis.Apply(Z); var direction=basis.Apply(X);
        ConstraintGradient Coupled(ConstraintGradient row,CollisionVector tangent)
        {
            var coefficient=(reverse?1:-1)*ratio*CollisionVector.Dot(tangent,direction);
            return new([..row.Terms,new(shaft,default,axis*coefficient),new(carrier,default,-axis*coefficient)]);
        }
        return new(rigid.Normal,rigid.NormalGradient,Coupled(rigid.TangentU,rigid.U),Coupled(rigid.TangentV,rigid.V));
    }
    private static void Near(double expected,double actual,double tolerance=1e-8)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(false,1d)]
    [InlineData(true,1d)]
    [InlineData(false,-1d)]
    [InlineData(true,-1d)]
    [InlineData(false,2d)]
    [InlineData(true,2d)]
    public void FiniteShaftDrivesMaterialContactAndReceivesReaction(bool reverse,double ratio)
    {
        var load=Body(Participant.Load,-Y); var carrier=Carrier(); var shaft=Body(Participant.Shaft,spin:Z*4);
        var map=Map(load,carrier,shaft,ratio,reverse);
        var contact=new ContactConstraint(map,0,0,10);
        var energy=load.KineticEnergy+shaft.KineticEnergy;
        Assert.Equal(3,contact.Bodies.Length);
        ImpulseSolver.Solve([contact]);
        var expected=4*ratio/(1+ratio*ratio);
        Near(expected,load.LinearVelocity.X); Near(4-ratio*expected,shaft.AngularVelocity.Z);
        Near(0,map.Slip.Length); Near(0,load.LinearVelocity.Y);
        Assert.InRange(load.KineticEnergy+shaft.KineticEnergy,0,energy);
        Near(4,shaft.AngularVelocity.Z+ratio*load.LinearVelocity.X);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(.1)]
    [InlineData(.5)]
    public void ContactUsesOneCircularFrictionBudget(double friction)
    {
        var load=Body(Participant.Load,new(0,-2,3)); var carrier=Carrier(); var shaft=Body(Participant.Shaft,spin:Z*4);
        var map=Map(load,carrier,shaft); var contact=new ContactConstraint(map,0,0,friction);
        var energy=load.KineticEnergy+shaft.KineticEnergy;
        ImpulseSolver.Solve([contact]);
        Assert.InRange(contact.TangentImpulse.Length,0,friction*2+1e-9);
        Near(contact.TangentImpulse.X,4-shaft.AngularVelocity.Z);
        Assert.InRange(load.KineticEnergy+shaft.KineticEnergy,0,energy);
        Assert.InRange(CollisionVector.Dot(contact.TangentImpulse,map.Slip),double.NegativeInfinity,1e-8);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    public void ASpinningShaftCannotDriveANonloadedOrSeparatingContact(double normalSpeed)
    {
        var load=Body(Participant.Load,Y*normalSpeed); var carrier=Carrier();
        var shaft=Body(Participant.Shaft,spin:Z*4);
        var before=new[]{load.Snapshot(),shaft.Snapshot()};
        var contact=new ContactConstraint(Map(load,carrier,shaft),0,0,10);
        ImpulseSolver.Solve([contact]);
        Near(0,contact.Impulse.Normal); Near(0,contact.TangentImpulse.Length);
        Assert.Equal(before,new[]{load.Snapshot(),shaft.Snapshot()});
    }

    [Fact]
    public void FiniteWorkMotorChargesShaftAndContactsCannotInventEnergy()
    {
        var load=Body(Participant.Load,-Y); var carrier=Carrier(); var shaft=Body(Participant.Shaft);
        var rotation=new ConstraintJacobian(default,Z,default,-Z);
        var empty=PoweredImpulse.Apply(rotation.Bind(shaft,carrier),10,100,0);
        Assert.Equal(0,empty.Impulse); Near(0,shaft.KineticEnergy);
        var supplied=PoweredImpulse.Apply(rotation.Bind(shaft,carrier),10,100,2);
        Near(2,supplied.SuppliedWork); Near(2,shaft.KineticEnergy);
        var map=Map(load,carrier,shaft); var contact=new ContactConstraint(map,0,0,10);
        ImpulseSolver.Solve([contact]);
        Near(1,load.LinearVelocity.X); Near(1,shaft.AngularVelocity.Z);
        Assert.InRange(load.KineticEnergy+shaft.KineticEnergy,0,supplied.SuppliedWork);
    }

    [Fact]
    public void TwoLoadsShareOneFiniteShaftSupply()
    {
        var first=Body(Participant.Load,-Y); var second=Body(Participant.SecondLoad,-Y);
        var carrier=Carrier(); var shaft=Body(Participant.Shaft,spin:Z*6);
        var firstMap=Map(first,carrier,shaft); var secondMap=Map(second,carrier,shaft);
        var a=new ContactConstraint(firstMap,0,0,10); var b=new ContactConstraint(secondMap,0,0,10);
        ImpulseSolver.Solve([a,b]);
        Near(2,first.LinearVelocity.X); Near(2,second.LinearVelocity.X); Near(2,shaft.AngularVelocity.Z);
        Near(0,firstMap.Slip.Length); Near(0,secondMap.Slip.Length);
        Assert.InRange(first.KineticEnergy+second.KineticEnergy+shaft.KineticEnergy,0,18);
    }

    [Fact]
    public void DynamicCarrierReceivesTheTransmissionReaction()
    {
        var load=Body(Participant.Load,-Y); var carrier=Body(Participant.Carrier); var shaft=Body(Participant.Shaft,spin:Z*4);
        var before=load.KineticEnergy+carrier.KineticEnergy+shaft.KineticEnergy;
        var map=Map(load,carrier,shaft); var contact=new ContactConstraint(map,0,0,10);
        ImpulseSolver.Solve([contact]);
        Near(0,(load.LinearVelocity+carrier.LinearVelocity+Y).Length);
        Near(4,(load.AngularMomentum+carrier.AngularMomentum+shaft.AngularMomentum).Z);
        Near(0,map.Slip.Length);
        Assert.InRange(load.KineticEnergy+carrier.KineticEnergy+shaft.KineticEnergy,0,before);
    }

    [Fact]
    public void RotatingTheWholeContactPreservesTheResponse()
    {
        var rotation=RigidRotation.FromRotationVector(new(.3,.7,-.2));
        var load=Body(Participant.Load,rotation.Apply(-Y)); var carrier=Carrier();
        var shaft=Body(Participant.Shaft,spin:rotation.Apply(Z*4));
        var map=Map(load,carrier,shaft,rotation:rotation);
        ImpulseSolver.Solve([new ContactConstraint(map,0,0,10)]);
        Near(0,(load.LinearVelocity-rotation.Apply(X*2)).Length);
        Near(0,(shaft.AngularVelocity-rotation.Apply(Z*2)).Length);
    }

    [Theory]
    [InlineData(FrictionRegime.Sticking)]
    [InlineData(FrictionRegime.Sliding)]
    public void ForceSolveCouplesShaftTorqueAndPayloadWithoutMutatingPhysicalState(FrictionRegime regime)
    {
        var load=Body(Participant.Load); var carrier=Carrier();
        var shaft=Body(Participant.Shaft,spin:regime==FrictionRegime.Sliding?Z*4:default);
        var map=Map(load,carrier,shaft);
        var before=new[]{load.Snapshot(),carrier.Snapshot(),shaft.Snapshot()};
        var force=new ContactForce(map,0,default,map.Slip,.5,regime);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>
        {
            [load.Id]=new(-Y*10,default),[carrier.Id]=default,[shaft.Id]=new(default,Z*2)
        };
        var result=AccelerationSolver.Solve([load,carrier,shaft],[],[force],loads,1e-9,out _,[],out _,out _,out _);
        var expected=regime==FrictionRegime.Sticking?1:5;
        Near(expected,result[load.Id].Force.X); Near(0,result[load.Id].Force.Y);
        Near(2-expected,result[shaft.Id].Torque.Z);
        Assert.Equal(before,new[]{load.Snapshot(),carrier.Snapshot(),shaft.Snapshot()});
    }

    [Fact]
    public void WarmStartIncludesShaftAndRetractsWhenTheNormalBudgetDisappears()
    {
        var load=Body(Participant.Load); var carrier=Carrier(); var shaft=Body(Participant.Shaft);
        var contact=new ContactConstraint(Map(load,carrier,shaft),0,0,.5);
        contact.WarmStart(new(2,X*4));
        Near(1,load.LinearVelocity.X); Near(-1,shaft.AngularVelocity.Z);
        ImpulseSolver.Solve([contact]);
        Near(0,load.LinearVelocity.Length); Near(0,shaft.AngularVelocity.Length);
        Near(0,contact.Normal.AccumulatedImpulse); Near(0,contact.TangentImpulse.Length);
    }

    [Fact]
    public void StaleShaftPoseRejectsBeforeAnyContactParticipantChanges()
    {
        var load=Body(Participant.Load); var carrier=Carrier(); var shaft=Body(Participant.Shaft,spin:Z);
        var map=Map(load,carrier,shaft);
        var contact=new ContactConstraint(map,0,0,.5);
        shaft.Advance(shaft.CreateTrajectory(.01,default),.01);
        var before=new[]{load.Snapshot(),carrier.Snapshot(),shaft.Snapshot()};
        Assert.Throws<InvalidOperationException>(()=>contact.WarmStart(new(2,X)));
        Assert.Throws<InvalidOperationException>(()=>contact.Solve());
        Assert.Throws<InvalidOperationException>(()=>contact.Residual);
        Assert.Throws<InvalidOperationException>(()=>new ContactConstraint(map,0,0,.5));
        Assert.Throws<InvalidOperationException>(()=>ContactConstraint.ForAcceleration(map,0,default,default,.5,FrictionRegime.Sticking));
        Assert.Equal(before,new[]{load.Snapshot(),carrier.Snapshot(),shaft.Snapshot()});
    }

    [Fact]
    public void RebindingRequiresAllParticipantsAtTheDeclaredPoses()
    {
        var load=Body(Participant.Load); var carrier=Carrier(); var shaft=Body(Participant.Shaft,spin:Z);
        var map=Map(load,carrier,shaft);
        var states=new Dictionary<PhysicsBodyId,PhysicsBody>{{load.Id,load},{carrier.Id,carrier}};
        Assert.Throws<ArgumentException>(()=>map.Rebind(states));
        var moved=Body(Participant.Shaft,spin:Z); moved.Advance(moved.CreateTrajectory(.01,default),.01);
        states.Add(shaft.Id,moved);
        Assert.Throws<ArgumentException>(()=>map.Rebind(states));
        states[shaft.Id]=shaft;
        Assert.Equal(map.Slip,map.Rebind(states).Slip);
    }

    [Fact]
    public void ConflictingParticipantIdentitiesAndInvalidForceModesReject()
    {
        var load=Body(Participant.Load); var carrier=Carrier(); var shaft=Body(Participant.Shaft);
        var map=Map(load,carrier,shaft);
        var impostor=Body(Participant.Shaft);
        Assert.Throws<ArgumentException>(()=>new ContactKinematics(Y,map.NormalGradient,map.TangentU,
            new([..map.TangentV.Terms,new(impostor,default,Z)])));
        Assert.Throws<ArgumentException>(()=>new ContactKinematics(default,map.NormalGradient,map.TangentU,map.TangentV));
        Assert.Throws<ArgumentException>(()=>new ContactForce(map,0,default,default,.5,(FrictionRegime)99));
        var force=new ContactForce(map,0,default,default,.5,FrictionRegime.Sticking);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{load.Id,default},{carrier.Id,default},{shaft.Id,default}};
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([load,carrier,impostor],[],[force],loads,1e-9,out _,[],out _,out _,out _));
    }
}
