using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactGapTests
{
    public enum GeometryCase { RoundedPointFace, RoundedPoints, RoundedPointEdge, CrossedRoundedEdges, ParallelRoundedEdges }
    private static ConvexInstance Shape(ConvexGeometry geometry)=>new(geometry,AffineTransform.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(1,1,1));
    private static ConvexInstance Segment(CollisionVector axis)=>
        Shape(new ConvexRounded(new ConvexHull([-axis,axis]),.25));
    private static void Near(double expected,double actual,double tolerance=1e-8)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-7)]
    public void RollingRoundedPointHasNoFalseCentripetalFloorGap(double spin)
    {
        var a=Body(0,new(0,.5,0),new(2,0,0),new(0,0,spin));
        var b=Body(1,new(0,-.5,0));
        var gap=Assert.Single(ContactGap.Query(a,Shape(new ConvexSphere(.5)),b,Shape(new ConvexBox(new(4,.5,4))),.001,1e-8));
        Near(0,gap.Separation); Near(0,gap.Rate); Near(0,gap.ConvectiveAcceleration);
        Assert.All(gap.Gradient.Terms.ToArray(),term=>Near(0,term.Angular.Length));
    }

    [Theory]
    [InlineData(GeometryCase.RoundedPointFace)]
    [InlineData(GeometryCase.RoundedPoints)]
    [InlineData(GeometryCase.RoundedPointEdge)]
    [InlineData(GeometryCase.CrossedRoundedEdges)]
    [InlineData(GeometryCase.ParallelRoundedEdges)]
    public void AnalyticGapDerivativesMatchMovingGeometryAndSwappedBodies(GeometryCase geometry)
    {
        var (sa,sb,height)=geometry switch
        {
            GeometryCase.RoundedPointFace=>(Shape(new ConvexSphere(.5)),Shape(new ConvexBox(new(4,.5,4))),1.0),
            GeometryCase.RoundedPoints=>(Shape(new ConvexSphere(.5)),Shape(new ConvexSphere(.5)),1.0),
            GeometryCase.RoundedPointEdge=>(Shape(new ConvexSphere(.5)),Segment(new(2,0,0)),.75),
            GeometryCase.CrossedRoundedEdges=>(Segment(new(2,0,0)),Segment(new(0,0,2)),.5),
            GeometryCase.ParallelRoundedEdges=>(Segment(new(2,0,0)),Segment(new(2,0,0)),.5),
            _=>throw new ArgumentOutOfRangeException(nameof(geometry))
        };
        var a=Body(0,new(0,height,0),new(.3,.02,-.1),new(.2,0,.1));
        var b=Body(1,default,new(-.1,-.03,.2),new(-.1,.1,.05));
        // Parallel features change active dimension under relative rotation.
        // Keep that case parallel; verify its active endpoint derivatives here.
        if(geometry==GeometryCase.ParallelRoundedEdges)
        { a=Body(0,new(0,height,0),new(0,.02,.3)); b=Body(1,default,new(0,-.03,.1)); }
        const double h=1e-4;
        double Separation(double time)
        {
            PhysicsBody Moved(PhysicsBody body)=>new(body.Id,PhysicsMotionType.Kinematic,
                new(body.Center+body.LinearVelocity*time,RigidRotation.FromRotationVector(body.AngularVelocity*time)*body.Pose.Rotation),
                body.LinearVelocity,body.AngularVelocity);
            var aa=Moved(a); var bb=Moved(b);
            var ma=new ConvexPose(sa,aa.Pose);
            var mb=new ConvexPose(sb,bb.Pose);
            return ConvexSeparation.Query(ma,mb,1e-12).UpperBound;
        }
        var gaps=ContactGap.Query(a,sa,b,sb,.001,1e-9);
        var reverse=ContactGap.Query(b,sb,a,sa,.001,1e-9);
        Assert.NotEmpty(gaps); Assert.Equal(gaps.Length,reverse.Length);
        var first=(Separation(h)-Separation(-h))/(2*h);
        var second=(Separation(h)-2*Separation(0)+Separation(-h))/(h*h);
        foreach(var gap in gaps)
        {
            Near(first,gap.Rate,2e-6);
            Near(second,gap.ConvectiveAcceleration,2e-4);
            Assert.Contains(reverse,r=>Math.Abs(r.Rate-gap.Rate)<1e-8&&Math.Abs(r.ConvectiveAcceleration-gap.ConvectiveAcceleration)<1e-8);
        }
    }

    [Fact]
    public void RoundedPointCurvatureDoesNotDependOnMaterialSpin()
    {
        var a=Body(0,new(0,1,0),new(3,0,0),new(4,5,6));
        var b=Body(1,default);
        var sphere=Shape(new ConvexSphere(.5));
        var gap=Assert.Single(ContactGap.Query(a,sphere,b,sphere,.001,1e-8));
        Near(9,gap.ConvectiveAcceleration); Near(0,gap.Rate);
        Assert.All(gap.Gradient.Terms.ToArray(),term=>Near(0,term.Angular.Length));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(120)]
    [InlineData(480)]
    public void NormalSupportBalancesLoadsWithAnExplicitAccelerationErrorBudget(int frequency)
    {
        var a=Body(0,new(0,.5,0));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
        var box=Shape(new ConvexBox(new(.5,.5,.5))); var floor=Shape(new ConvexBox(new(10,.5,10)));
        var pair=new PersistentContactPair(a,box,b,floor,new(0,0,0),.01,.1,[]);
        var before=a.Snapshot();
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(new(0,-10,0),new(0,0,2))},{b.Id,default}};
        var support=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[pair],loads,1.0/frequency,1.0/frequency,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Near(0,support.Wrenches[a.Id].Force.Length,2e-9); Near(0,support.Wrenches[a.Id].Torque.Length,2e-9);
        Assert.Equal(before,a.Snapshot()); Assert.Equal(default,support.Wrenches[b.Id]);
        loads[a.Id]=new(new(0,10,0),default);
        Assert.Equal(loads[a.Id],AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[pair],loads,1.0/frequency,1.0/frequency,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()).Wrenches[a.Id]);
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[Body(0,default),b],[],[pair],loads,.01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
        Assert.Throws<ArgumentOutOfRangeException>(()=>AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[pair],loads,.01,.01,1e-7,1e-8,0,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
    }

    [Fact]
    public void DynamicContactSupportSharesReactionWithoutAttraction()
    {
        var a=Body(0,new(0,1,0)); var b=Body(1,default); var sphere=Shape(new ConvexSphere(.5));
        var rows=ContactGap.Query(a,sphere,b,sphere,.001,1e-8).Select(g=>g.NormalConstraint).ToArray();
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(new(0,-10,0),default)},{b.Id,default}};
        var support=AccelerationSolver.Solve([a,b],rows,[],loads,1e-9,out _,[],out _,out _,out _);
        Near(-5,support[a.Id].Force.Y); Near(-5,support[b.Id].Force.Y);
        loads[a.Id]=new(new(0,10,0),default);
        support=AccelerationSolver.Solve([a,b],rows,[],loads,1e-9,out _,[],out _,out _,out _);
        Assert.Equal(loads[a.Id],support[a.Id]); Assert.Equal(default,support[b.Id]);
    }

    [Fact]
    public void SharedWorldRollingSupportAndReleaseReplayExactly()
    {
        var a=Body(0,new(0,.5,0),new(1,0,0),new(0,0,-2));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
        var sphere=Shape(new ConvexSphere(.5)); var floor=Shape(new ConvexBox(new(10,.5,10)));
        var world=new PhysicsWorld([],[new(a,new([sphere]),new(0,0,0)),new(b,new([floor]),new(0,0,0))],[],new(new(0,-9.8,0)));
        var start=world.Capture();
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        var after=a.Snapshot();
        Near(.5,a.Center.Y); Near(1,a.Center.X); Near(1,a.LinearVelocity.X); Near(0,a.LinearVelocity.Y);
        Near(-2,a.AngularVelocity.Z);
        world.Restore(start);
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        Assert.Equal(after,a.Snapshot());
        world.Step([new(a.Id,new(0,20,0),default)],[],.1);
        Assert.True(a.Center.Y>.5); Assert.True(a.LinearVelocity.Y>0);
    }

    [Fact]
    public void StackedBodiesShareContinuousSupportAndRestoreExactly()
    {
        var lower=Body(0,new(0,.5,0)); var upper=Body(1,new(0,1.5,0));
        var floor=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
        var sphere=new CompoundGeometry([Shape(new ConvexSphere(.5))]);
        var world=new PhysicsWorld([],[new(lower,sphere,new(0,0,0)),new(upper,sphere,new(0,0,0)),
            new(floor,new([Shape(new ConvexBox(new(5,.5,5)))]),new(0,0,0))],[],new(new(0,-9.8,0)));
        var start=world.Capture();
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        var endLower=lower.Snapshot(); var endUpper=upper.Snapshot();
        Near(.5,lower.Center.Y); Near(1.5,upper.Center.Y);
        Near(0,lower.LinearVelocity.Length); Near(0,upper.LinearVelocity.Length);
        world.Restore(start);
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        Assert.Equal(endLower,lower.Snapshot()); Assert.Equal(endUpper,upper.Snapshot());
        world.Step([new(upper.Id,new(0,20,0),default)],[],.1);
        Assert.True(upper.Center.Y>1.5); Near(.5,lower.Center.Y); Near(0,lower.LinearVelocity.Length);
    }

    [Fact]
    public void ClearAndInvalidQueriesAreNotSilentlyGivenSupport()
    {
        var sphere=Shape(new ConvexSphere(.5)); var a=Body(0,new(0,3,0)); var b=Body(1,default);
        Assert.Empty(ContactGap.Query(a,sphere,b,sphere,.001,1e-8));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorldSettings(default,accelerationTolerance:0));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorldSettings(default,accelerationTolerance:double.NaN));
        Assert.Throws<ArgumentException>(()=>ContactGap.Query(a,sphere,a,sphere,.001,1e-8));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactGap.Query(a,sphere,b,sphere,.001,0));
    }
}
