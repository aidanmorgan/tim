using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AccelerationDriveTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,double mass=2)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.Identity,default,default,mass,new(2,3,4));
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-8);
    private static IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Solve(PhysicsBody[] bodies,
        ConstraintAcceleration[] constraints,AccelerationDrive[] drives,
        Dictionary<PhysicsBodyId,BodyWrench> loads,out IReadOnlyList<AccelerationDriveResult> results)=>
        AccelerationSolver.Solve(bodies,constraints,[],loads,1e-10,out _,drives,out results,out _,out _);

    [Theory]
    [InlineData(2,20,-20,DriveEffortLimit.None,10,2)]
    [InlineData(20,20,-20,DriveEffortLimit.Upper,20,7)]
    [InlineData(-20,20,-20,DriveEffortLimit.Lower,-20,-13)]
    [InlineData(2,0,0,DriveEffortLimit.Disabled,0,-3)]
    public void LoadedAxisUsesFiniteEffortAndReportsActualAcceleration(double target,double upper,double lower,
        DriveEffortLimit limit,double effort,double achieved)
    {
        var body=Body(0);
        var drive=new AccelerationDrive(new(0),new([new(body,X,default)]),0,target,lower,upper);
        var before=body.Snapshot();
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,new(-X*6,default)}};
        var wrenches=Solve([body],[],[drive],loads,out var results);
        var result=Assert.Single(results);
        Assert.Equal(drive.Id,result.Id); Assert.Equal(limit,result.Limit);
        Near(effort,result.Effort); Near(achieved,result.AchievedAcceleration);
        Near(achieved,wrenches[body.Id].Force.X*body.InverseMass);
        var repeated=Solve([body],[],[drive],loads,out var replay);
        Assert.Equal(results,replay); Assert.Equal(wrenches[body.Id],repeated[body.Id]);
        Assert.Equal(before,body.Snapshot());
    }

    [Fact]
    public void PairDriveConservesMomentumAndIncludesConvectiveAcceleration()
    {
        var a=Body(0,2);var b=Body(1,3);
        var drive=new AccelerationDrive(new(4),new([new(a,X,default),new(b,-X,default)]),.5,3,-20,20);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default}};
        var result=Solve([a,b],[],[drive],loads,out var drives);
        Near(3,Assert.Single(drives).Effort);Near(3,drives[0].AchievedAcceleration);
        Near(0,(result[a.Id].Force+result[b.Id].Force).Length);
        Near(1.5,result[a.Id].Force.X*a.InverseMass);Near(-1,result[b.Id].Force.X*b.InverseMass);
    }

    [Fact]
    public void AngularDriveUsesInertiaAndCoupledTransmissionReaction()
    {
        var a=Body(0);var b=Body(1);
        var coupling=new ConstraintAcceleration(new([new(a,default,-Z*2),new(b,default,Z)]),0,AccelerationRelation.Equal);
        var drive=new AccelerationDrive(new(0),new([new(a,default,Z)]),0,1,-100,100);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default}};
        var result=Solve([a,b],[coupling],[drive],loads,out var drives);
        Near(20,Assert.Single(drives).Effort);
        Near(1,a.InverseInertia(result[a.Id].Torque).Z);
        Near(2,b.InverseInertia(result[b.Id].Torque).Z);
    }

    [Theory]
    [InlineData(-2,DriveEffortLimit.Lower,-10,0)]
    [InlineData(2,DriveEffortLimit.None,4,2)]
    public void UnilateralStopBlocksAndReleasesWithoutAttractiveReaction(double target,DriveEffortLimit limit,
        double expectedEffort,double expectedAcceleration)
    {
        var body=Body(0);var gradient=new ConstraintGradient([new(body,Y,default)]);
        var stop=new ConstraintAcceleration(gradient,0,AccelerationRelation.Nonnegative);
        var drive=new AccelerationDrive(new(0),gradient,0,target,-10,10);
        var result=Solve([body],[stop],[drive],new(){{body.Id,default}},out var drives);
        var actual=Assert.Single(drives);Assert.Equal(limit,actual.Limit);
        Near(expectedEffort,actual.Effort);Near(expectedAcceleration,actual.AchievedAcceleration);
        Near(expectedAcceleration,result[body.Id].Force.Y*body.InverseMass);
    }

    [Fact]
    public void CompetingDrivesResolveTogetherInIdentityOrder()
    {
        var body=Body(0);var gradient=new ConstraintGradient([new(body,X,default)]);
        var positive=new AccelerationDrive(new(9),gradient,0,10,0,8);
        var negative=new AccelerationDrive(new(2),gradient,0,-10,-4,0);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,default}};
        var result=Solve([body],[],[positive,negative],loads,out var first);
        var replay=Solve([body],[],[negative,positive],loads,out var second);
        Assert.Equal(first,second);Assert.Equal(result[body.Id],replay[body.Id]);
        Assert.Equal(negative.Id,first[0].Id);Assert.Equal(positive.Id,first[1].Id);
        Assert.Equal(DriveEffortLimit.Lower,first[0].Limit);Assert.Equal(DriveEffortLimit.Upper,first[1].Limit);
        Near(2,first[0].AchievedAcceleration);Near(2,first[1].AchievedAcceleration);
    }


    [Theory]
    [InlineData(1,20,7,1,DriveEffortLimit.None)]
    [InlineData(2,8,8,1.5,DriveEffortLimit.Upper)]
    [InlineData(-2,8,-8,-1.5,DriveEffortLimit.Lower)]
    public void DriveAndCoulombFrictionShareTheSameForceSolve(double target,double bound,double effort,
        double acceleration,DriveEffortLimit limit)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(Y*.5),default,default,2,new(2,3,4));
        var floor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(-Y*.5),default,default);
        var sphere=new ConvexInstance(new ConvexSphere(.5),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity));
        var box=new ConvexInstance(new ConvexBox(new(10,.5,10)),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity));
        var gap=Assert.Single(ContactGap.Query(body,sphere,floor,box,.001,1e-9));
        var contact=ContactForce.FromGap(gap,.5,FrictionRegime.Sticking);
        var rotation=new ConstraintAcceleration(new([new(body,default,Z)]),0,AccelerationRelation.Equal);
        var drive=new AccelerationDrive(new(0),new([new(body,X,default)]),0,target,-bound,bound);
        var before=new[]{body.Snapshot(),floor.Snapshot()};
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,new(-Y*10,default)},{floor.Id,default}};
        var result=AccelerationSolver.Solve([body,floor],[rotation],[contact],loads,1e-10,out _,[drive],out var drives,out _,out _);
        var actual=Assert.Single(drives);Assert.Equal(limit,actual.Limit);Near(effort,actual.Effort);
        Near(acceleration,actual.AchievedAcceleration);Near(acceleration,result[body.Id].Force.X*body.InverseMass);
        Near(0,result[body.Id].Force.Y);Near(0,result[body.Id].Torque.Length);
        Assert.Equal(before,new[]{body.Snapshot(),floor.Snapshot()});
    }

    [Fact]
    public void InvalidDeclarationsRejectWithoutChangingPhysicalState()
    {
        var body=Body(0);var gradient=new ConstraintGradient([new(body,X,default)]);
        var before=body.Snapshot();var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,default}};
        var drive=new AccelerationDrive(new(0),gradient,0,1,-10,10);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsDriveId(-1));
        Assert.Throws<ArgumentNullException>(()=>new AccelerationDrive(new(0),null!,0,0,0,0));
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,invalid,0,0,0));
            Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,0,invalid,0,0));
            Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,0,0,invalid,0));
            Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,0,0,0,invalid));
        }
        Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,0,0,1,2));
        Assert.Throws<ArgumentException>(()=>new AccelerationDrive(new(0),gradient,0,0,-2,-1));
        Assert.Throws<ArgumentNullException>(()=>Solve([body],[],null!,loads,out _));
        Assert.Throws<ArgumentNullException>(()=>Solve([body],[],[null!],loads,out _));
        Assert.Throws<ArgumentException>(()=>Solve([body],[],[drive,drive],loads,out _));
        Assert.Throws<ArgumentException>(()=>Solve([Body(0)],[],[drive],loads,out _));
        var overflow=new AccelerationDrive(new(0),gradient,-double.MaxValue,double.MaxValue,-1,1);
        Assert.Throws<ArgumentException>(()=>Solve([body],[],[overflow],loads,out _));
        var zero=new AccelerationDrive(new(0),new([new(body,default,default)]),0,1,-1,1);
        Assert.Throws<ArgumentException>(()=>Solve([body],[],[zero],loads,out _));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var fixedDrive=new AccelerationDrive(new(1),new([new(anchor,X,default)]),0,1,-1,1);
        Assert.Throws<ArgumentException>(()=>Solve([anchor],[],[fixedDrive],new(){{anchor.Id,default}},out _));
        Assert.Equal(before,body.Snapshot());
    }
}
