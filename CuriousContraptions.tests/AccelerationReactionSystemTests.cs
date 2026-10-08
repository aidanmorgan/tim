using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AccelerationReactionSystemTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static readonly CollisionVector Y=new(0,1,0);
    private static PhysicsBody Dynamic()=>new(new(0),PhysicsMotionType.Dynamic,
        RigidPose.Identity,default,default,1,new(1,1,1));
    private static Dictionary<PhysicsBodyId,BodyWrench> Wrenches(PhysicsBody body,CollisionVector force)=>
        new(){{body.Id,new(force,default)}};

    private static AccelerationReactionEvaluation Evaluate(AccelerationReactionSystem system,double[] coordinates)=>
        system.Evaluate(coordinates,new double[system.CoordinateCount]);

    [Theory]
    [InlineData(2,0)]
    [InlineData(-2,0)]
    [InlineData(2,3)]
    [InlineData(-2,3)]
    public void OppositeStopsPreserveEveryEquationAndNonuniqueFeasibleReactions(double applied,double nullReaction)
    {
        var body=Dynamic();var before=body.Snapshot();
        var gradient=new ConstraintGradient([new(body,X,default)]);
        ConstraintAcceleration[] rows=[
            new(gradient,0,AccelerationRelation.Nonnegative),
            new(gradient,0,AccelerationRelation.Nonpositive)];
        var system=new AccelerationReactionSystem([body],rows,[],Wrenches(body,X*applied),Wrenches(body,default),[]);
        var lower=Math.Max(0,-applied)+nullReaction;
        var upper=-Math.Max(0,applied)-nullReaction;
        var result=Evaluate(system,[lower,upper]);
        Assert.Equal(2,system.CoordinateCount);
        Assert.All(result.Residual,value=>Assert.Equal(0,value));
        Assert.Equal(0,result.PhysicalResidual);
        Assert.Equal(default,result.Forces[body.Id]);
        Assert.Equal(new[]{lower,upper},result.ConstraintEfforts);
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ZeroResponseRowsKeepTheirOriginalSatisfiedOrUnsatisfiedLaw(double bias)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var before=body.Snapshot();
        var row=new ConstraintAcceleration(new([new(body,X,default)]),bias,AccelerationRelation.Equal);
        var system=new AccelerationReactionSystem([body],[row],[],Wrenches(body,default),Wrenches(body,default),[]);
        var result=Evaluate(system,[0]);
        Assert.Equal(bias,result.Residual[0]);
        Assert.Equal(bias,result.PhysicalResidual);
        Assert.Equal(default,result.Forces[body.Id]);
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(FrictionRegime.Sticking,0,0,2)]
    [InlineData(FrictionRegime.Sticking,.5,-.5,1.5)]
    [InlineData(FrictionRegime.Sticking,2,-2,0)]
    [InlineData(FrictionRegime.Sliding,.5,.5,2.5)]
    public void PlanarFrictionUsesItsDeclaredRegimeAndOriginalForceBalance(
        FrictionRegime regime,double friction,double tangent,double acceleration)
    {
        var body=Dynamic();
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var before=new[]{body.Snapshot(),ground.Snapshot()};
        var kinematics=ContactKinematics.AtPoint(body,ground,default,Y);
        var contact=new ContactForce(kinematics,0,default,-X,friction,regime);
        var loads=Wrenches(body,new(2,-1,0));loads.Add(ground.Id,default);
        var candidate=Wrenches(body,X*acceleration);candidate.Add(ground.Id,default);
        var system=new AccelerationReactionSystem([body,ground],[],[contact],loads,candidate,[]);
        var reaction=X*tangent;
        var result=Evaluate(system,[1,CollisionVector.Dot(reaction,kinematics.U),CollisionVector.Dot(reaction,kinematics.V)]);
        Assert.All(result.Residual,value=>Assert.Equal(0,value));
        Assert.Equal(0,result.PhysicalResidual);
        Assert.Equal(candidate[body.Id],result.Forces[body.Id]);
        Assert.Equal(1,result.ContactReactions[0].Normal);
        Assert.Equal(reaction,result.ContactReactions[0].Tangent);
        Assert.Equal(before,new[]{body.Snapshot(),ground.Snapshot()});
    }

    [Fact]
    public void NaturalMapProjectionOwnsTheReportedBoundAndPhysicalAcceptance()
    {
        var body=Dynamic();var before=body.Snapshot();
        var drive=new AccelerationDrive(new(0),new([new(body,X,default)]),0,-6,-20,20);
        var system=new AccelerationReactionSystem([body],[],[],Wrenches(body,X*20),Wrenches(body,default),[drive]);
        var exact=Evaluate(system,[-20]);
        Assert.Equal(0,exact.Residual[0]);
        Assert.Equal(0,exact.PhysicalResidual);
        Assert.Equal(DriveEffortLimit.Lower,exact.DriveResults[0].Limit);
        var interior=Evaluate(system,[Math.BitIncrement(-20)]);
        Assert.Equal(Math.BitIncrement(-20)-(-20),interior.Residual[0]);
        Assert.Equal(0,interior.PhysicalResidual);
        Assert.Equal(DriveEffortLimit.Lower,interior.DriveResults[0].Limit);
        Assert.Equal(default,interior.Forces[body.Id]);
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(20,6,DriveEffortLimit.Upper)]
    [InlineData(6,0,DriveEffortLimit.None)]
    public void UpperAndInteriorDriveRootsKeepForceAndReportConsistent(double effort,double target,DriveEffortLimit limit)
    {
        var body=Dynamic();
        var drive=new AccelerationDrive(new(0),new([new(body,X,default)]),0,target,-20,20);
        var system=new AccelerationReactionSystem([body],[],[],Wrenches(body,-X*effort),Wrenches(body,default),[drive]);
        var projected=new[]{double.NaN};
        var result=system.Evaluate([effort],projected);
        Assert.Equal(effort,projected[0]);
        Assert.Equal(0,result.Residual[0]);Assert.Equal(0,result.PhysicalResidual);
        Assert.Equal(default,result.Forces[body.Id]);
        Assert.Equal(effort,result.DriveResults[0].Effort);Assert.Equal(limit,result.DriveResults[0].Limit);
    }

    [Fact]
    public void NonAxisTangentsRetainExactCoordinatesAndCallerScratchCannotAliasOrEscape()
    {
        var body=Dynamic();
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var before=new[]{body.Snapshot(),ground.Snapshot()};
        var normal=new CollisionVector(1,2,3)/Math.Sqrt(14);
        var kinematics=ContactKinematics.AtPoint(body,ground,default,normal);
        var contact=new ContactForce(kinematics,0,default,default,1,FrictionRegime.Sticking);
        var loads=Wrenches(body,default);loads.Add(ground.Id,default);
        var system=new AccelerationReactionSystem([body,ground],[],[contact],loads,loads,[]);
        const double u=.123456789,v=-.234567891;
        var coordinates=new[]{1.0,u,v};var projected=new[]{double.NaN,double.NaN,double.NaN};
        var first=system.Evaluate(coordinates,projected);
        Assert.Equal(coordinates,projected);
        Assert.NotEqual(u,CollisionVector.Dot(first.ContactReactions[0].Tangent,kinematics.U));
        Assert.Equal(normal+kinematics.U*u+kinematics.V*v,first.Forces[body.Id].Force);
        Assert.All(first.Residual,value=>Assert.Equal(0,value));Assert.Equal(0,first.PhysicalResidual);
        var retained=first.Residual.ToArray();var force=first.Forces[body.Id];
        system.Evaluate([2,0,0],projected);
        Assert.Equal(new[]{2.0,0,0},projected);
        Assert.Equal(retained,first.Residual);Assert.Equal(force,first.Forces[body.Id]);
        Assert.Throws<ArgumentException>(()=>system.Evaluate(coordinates,coordinates));
        Assert.Throws<ArgumentException>(()=>system.Evaluate(coordinates,new double[2]));
        Assert.Equal(before,new[]{body.Snapshot(),ground.Snapshot()});
    }
}
