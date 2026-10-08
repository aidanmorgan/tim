using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ImpulseReactionLinearizationTests
{
    public enum TangentScenario { Interior, Exterior, FeasibleExterior, Sliding, Frictionless, FeasibleFrictionless, Apex, Immovable }

    [Theory]
    [InlineData(TangentScenario.Interior)]
    [InlineData(TangentScenario.Exterior)]
    [InlineData(TangentScenario.FeasibleExterior)]
    [InlineData(TangentScenario.Sliding)]
    [InlineData(TangentScenario.Frictionless)]
    [InlineData(TangentScenario.FeasibleFrictionless)]
    [InlineData(TangentScenario.Apex)]
    [InlineData(TangentScenario.Immovable)]
    public void TangentLinearizationMatchesIndependentUnitMassEquations(TangentScenario scenario)
    {
        Assert.True(Enum.IsDefined(scenario));
        var body=scenario==TangentScenario.Immovable
            ?new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default)
            :new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var map=ContactKinematics.AtPoint(body,ground,default,new(0,1,0));
        var friction=scenario is TangentScenario.Frictionless or TangentScenario.FeasibleFrictionless?0:scenario==TangentScenario.Interior?1:scenario is TangentScenario.Exterior or TangentScenario.FeasibleExterior?.5:.3;
        var contact=ContactConstraint.ForAcceleration(map,0,default,map.U*3+map.V*4,friction,
            scenario==TangentScenario.Sliding?FrictionRegime.Sliding:FrictionRegime.Sticking);
        var normal=scenario is TangentScenario.Apex or TangentScenario.Immovable?0:scenario is TangentScenario.Exterior or TangentScenario.FeasibleExterior?1:2;
        contact.Normal.InitializeAccumulatedImpulse(normal);
        var u=scenario==TangentScenario.Interior?.25:scenario==TangentScenario.Exterior?1:scenario is TangentScenario.Apex or TangentScenario.Immovable?0:.1;
        var v=scenario==TangentScenario.Interior?-.5:scenario is TangentScenario.Exterior or TangentScenario.Apex or TangentScenario.Immovable?0:.2;
        var speedU=scenario==TangentScenario.Interior?.5:0;
        var speedV=scenario==TangentScenario.Interior?-.25:scenario==TangentScenario.Exterior?-2:0;
        if(scenario==TangentScenario.FeasibleExterior){u=0;v=0;speedU=-2;speedV=-2;}
        if(scenario==TangentScenario.FeasibleFrictionless){u=0;v=0;}
        body.CommitVelocity(new(map.U*speedU+map.V*speedV,default));
        contact.CommitTangentCoordinates(u,v);
        var gradients=new[]{map.NormalGradient,map.TangentU,map.TangentV};
        var mass=new double[3,3];
        for(var i=0;i<3;i++)for(var j=0;j<3;j++)mass[i,j]=gradients[i].Coupling(gradients[j]);
        var jacobian=new double[3,3];var residual=new double[3];
        contact.LinearizeTangents(mass,jacobian,residual,0,1);
        var expected=new double[3,3];double ru=0,rv=0;
        switch(scenario)
        {
            case TangentScenario.Interior:
                ru=.5;rv=-.25;expected[1,1]=expected[2,2]=1;break;
            case TangentScenario.Exterior:
            case TangentScenario.FeasibleExterior:
                var q=1/Math.Sqrt(2);var factor=.5*q;
                ru=scenario==TangentScenario.FeasibleExterior?-q:2-q;rv=-q;
                expected[1,0]=expected[2,0]=-q;
                expected[1,1]=expected[2,2]=2-.5*factor;
                expected[1,2]=expected[2,1]=.5*factor;
                break;
            case TangentScenario.Sliding:
                ru=.92;rv=1.36;
                expected[1,0]=.36;expected[2,0]=.48;
                expected[1,1]=expected[2,2]=2;break;
            case TangentScenario.Frictionless:
            case TangentScenario.FeasibleFrictionless:
                ru=scenario==TangentScenario.FeasibleFrictionless?0:.2;rv=scenario==TangentScenario.FeasibleFrictionless?0:.4;expected[1,1]=expected[2,2]=2;break;
            case TangentScenario.Apex:
                // Explicit interior generalized derivative at the exact origin.
                expected[1,1]=expected[2,2]=1;break;
            case TangentScenario.Immovable:break;
            default:throw new ArgumentOutOfRangeException(nameof(scenario));
        }
        Assert.InRange(Math.Abs(residual[1]-ru),0,1e-14);
        Assert.InRange(Math.Abs(residual[2]-rv),0,1e-14);
        for(var i=0;i<3;i++)for(var j=0;j<3;j++)
            Assert.InRange(Math.Abs(jacobian[i,j]-expected[i,j]),0,1e-14);
    }

    public enum RollbackScenario { NoDescent, ResidualThrows }
    private sealed class ThrowingResidual : IImpulseConstraint
    {
        public ReadOnlySpan<PhysicsBody> Bodies=>[];
        public IEnumerable<ImpulseConstraint> ScalarRows=>[];
        public double Residual=>throw new ArithmeticException("Deliberate residual failure.");
        void IImpulseConstraint.Solve() { }
    }

    [Theory]
    [InlineData(RollbackScenario.NoDescent)]
    [InlineData(RollbackScenario.ResidualThrows)]
    public void RejectedOrExceptionalCorrectionRestoresExactBodyAndEfforts(RollbackScenario scenario)
    {
        Assert.True(Enum.IsDefined(scenario));
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,new(2,-1,0),default,1,new(1,1,1));
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,ground,default,new(0,1,0)),0,0,.3);
        IImpulseConstraint[] constraints=scenario==RollbackScenario.ResidualThrows?[contact,new ThrowingResidual()]:[contact];
        var correction=new ImpulseSweepAcceleration(constraints,[contact.Normal]);
        var bodyBefore=body.Snapshot();var groundBefore=ground.Snapshot();var impulseBefore=contact.Impulse;
        if(scenario==RollbackScenario.ResidualThrows)
            Assert.Throws<ArithmeticException>(()=>correction.Complete(contact.Residual));
        else
        {
            // Zero is the lower bound of the original physical norm, so no
            // proposed state can strictly improve it. All 32 trials must reject.
            Assert.Equal(0,correction.Complete(0));
            Assert.Equal(32,correction.TrialEvaluations);
        }
        Assert.Equal(bodyBefore,body.Snapshot());Assert.Equal(groundBefore,ground.Snapshot());
        Assert.Equal(impulseBefore,contact.Impulse);
        Assert.Equal(1,correction.Factorizations);
    }

    [Theory]
    [InlineData(-1e-17,1e-17,1)]
    [InlineData(1e-17,0,1)]
    [InlineData(0,0,1)]
    public void UpperBoundKeepsInwardSubUlpError(double target,double expectedResidual,double expectedDerivative)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var row=new ImpulseConstraint(new([new(body,new(1,0,0),default)]),target,0,1);
        row.InitializeAccumulatedImpulse(1);
        var mass=new double[,]{{1}};var derivative=new double[1,1];
        Assert.Equal(expectedResidual,row.LinearizeReaction(mass,derivative,0));
        Assert.Equal(expectedDerivative,derivative[0,0]);
    }

    [Fact]
    public void InteriorSoftnessUsesOriginalSignedPhysicalError()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,new(.5,0,0),default,1,new(1,1,1));
        var row=new ImpulseConstraint(new([new(body,new(1,0,0),default)]),2,double.NegativeInfinity,double.PositiveInfinity,2);
        row.InitializeAccumulatedImpulse(.25);
        var derivative=new double[1,1];
        Assert.Equal(-1,row.LinearizeReaction(new double[,]{{1}},derivative,0));
        Assert.Equal(3,derivative[0,0]);
    }

    [Theory]
    [InlineData(0,1,0)]
    [InlineData(1,0,3)]
    [InlineData(1,33,3)]
    [InlineData(1,1,0)]
    [InlineData(-1,0,0)]
    public void ImpossibleCorrectionWorkRejects(long factorizations,long trials,int coordinates)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ImpulseCorrectionWork(factorizations,trials,coordinates).Validate());

    [Fact]
    public void CorrectionWorkAddsCountsAndTakesCoordinateMaximum()
    {
        var result=new ImpulseCorrectionWork(2,3,5).Add(new(4,8,3));
        Assert.Equal(new ImpulseCorrectionWork(6,11,5),result);
        new ImpulseCorrectionWork(long.MaxValue,long.MaxValue,1).Validate();
    }
}
