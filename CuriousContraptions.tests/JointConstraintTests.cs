using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class JointConstraintTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static ImpulseBody Dynamic(int id,CollisionVector center,CollisionVector velocity,CollisionVector angular)=>
        new(new(id),PhysicsMotionType.Dynamic,center,velocity,angular,1,new InertiaTensor(1,1,1));
    private static ImpulseBody Fixed()=>new(new(1),PhysicsMotionType.Static,default,default,default);
    private static JointFrame Frame(CollisionVector at=default,RigidRotation? rotation=null)=>new(at,rotation??RigidRotation.Identity);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>Assert.InRange((a-b).Length,0,tolerance);

    [Fact]
    public void BallSocketRemovesAnchorVelocityButLeavesSpin()
    {
        var a=Dynamic(0,default,new(1,2,3),new(4,5,6));
        ImpulseSolver.Solve(JointConstraints.BallSocket(a,Fixed(),Frame(),Frame()));
        Near(default,a.LinearVelocity); Near(new(4,5,6),a.AngularVelocity);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(.8)]
    [InlineData(2.4)]
    public void HingeHasExactlyOneFreeAngularDegreeOfFreedom(double tilt)
    {
        var rotation=RigidRotation.FromRotationVector(X*tilt);
        var axis=rotation.Apply(Z);
        var a=Dynamic(0,default,new(1,2,3),axis*5+rotation.Apply(X)*2+rotation.Apply(Y)*3);
        ImpulseSolver.Solve(JointConstraints.Hinge(a,Fixed(),Frame(rotation:rotation),Frame(rotation:rotation)));
        Near(default,a.LinearVelocity); Near(axis*5,a.AngularVelocity);
    }
    [Fact]
    public void OffsetHingeUsesParallelAxisInertiaThroughItsAnchorRows()
    {
        var a=Dynamic(0,X,Y,default);
        ImpulseSolver.Solve(JointConstraints.Hinge(a,Fixed(),Frame(),Frame()));
        Near(default,a.PointVelocity(default));
        Near(Y*.5,a.LinearVelocity); Near(Z*.5,a.AngularVelocity);
    }
    [Fact]
    public void SliderLeavesOnlyItsDeclaredTranslationAxisFree()
    {
        var rotation=RigidRotation.FromRotationVector(Y*.7);
        var axis=rotation.Apply(Z);
        var a=Dynamic(0,default,axis*5+rotation.Apply(X)*3,new(1,2,3));
        ImpulseSolver.Solve(JointConstraints.Slider(a,Fixed(),Frame(rotation:rotation),Frame(rotation:rotation)));
        Near(axis*5,a.LinearVelocity); Near(default,a.AngularVelocity);
    }
    [Fact]
    public void SliderIncludesRotationOfTheMovingRailAtAnOffset()
    {
        var a=Dynamic(0,Z*3,default,default);
        var rail=new ImpulseBody(new(1),PhysicsMotionType.Kinematic,default,default,Y);
        ImpulseSolver.Solve(JointConstraints.Slider(a,rail,Frame(Z*3),Frame()));
        Near(X*3,a.LinearVelocity); Near(Y,a.AngularVelocity);
    }
    [Fact]
    public void FrameCorrectionPointsTowardAlignmentIncludingHalfTurns()
    {
        foreach(var angle in new[]{.6,Math.PI})
        {
            var a=Dynamic(0,default,default,default);
            var frame=Frame(rotation:RigidRotation.FromRotationVector(X*angle));
            ImpulseSolver.Solve(JointConstraints.Hinge(a,Fixed(),frame,Frame(),2));
            Near(-X*(angle*2),a.AngularVelocity,1e-7);
        }
        var slider=Dynamic(0,default,default,default);
        ImpulseSolver.Solve(JointConstraints.Slider(slider,Fixed(),Frame(new(1,2,3),
            RigidRotation.FromRotationVector(Z*.5)),Frame(),2));
        Near(new(-2,-4,0),slider.PointVelocity(new(1,2,3))); Near(-Z,slider.AngularVelocity);
    }
    [Theory]
    [InlineData(-1,-3,0)]
    [InlineData(1,3,0)]
    [InlineData(0,3,1)]
    [InlineData(0,-3,-1)]
    [InlineData(-1,1,1)]
    public void LimitsAreUnilateralAndPredictTheNextInterval(double position,double speed,double expected)
    {
        var a=Dynamic(0,X*position,X*speed,default);
        var rows=JointConstraints.Limits(a,Fixed(),new(X,default,-X,default),position,-1,1,1);
        ImpulseSolver.Solve(rows);
        Near(X*expected,a.LinearVelocity);
    }
    [Fact]
    public void RopeOnlyTensionsAndPreservesMomentumBetweenDynamicEndpoints()
    {
        var a=Dynamic(0,X,X*3,default); var b=Dynamic(1,-X,-X,default);
        var rope=JointConstraints.Rope(a,b,a.Center,b.Center,2,.1);
        ImpulseSolver.Solve([rope]);
        Near(X,a.LinearVelocity); Near(X,b.LinearVelocity);
        Assert.True(rope.AccumulatedImpulse<0);
        var slack=Dynamic(0,X,-X,default);
        var free=JointConstraints.Rope(slack,Fixed(),X,default,2,.1);
        ImpulseSolver.Solve([free]); Near(-X,slack.LinearVelocity); Assert.Equal(0,free.AccumulatedImpulse);
    }
    [Fact]
    public void RotationCompositionAndLogAreConsistent()
    {
        var random=new Random(81);
        for(var i=0;i<200;i++)
        {
            var v=new CollisionVector(random.NextDouble(),random.NextDouble(),random.NextDouble());
            var r=RigidRotation.FromRotationVector(v*2);
            Near(v,r.Inverse().Apply(r.Apply(v)));
            var reconstructed=RigidRotation.FromRotationVector(r.RotationVector());
            Near(r.Apply(X),reconstructed.Apply(X));
            var b=RigidRotation.FromRotationVector(Y*.3);
            Near(r.Apply(b.Apply(Z)),(r*b).Apply(Z));
        }
    }
    [Fact]
    public void InvalidFramesLimitsAndRopesFailExplicitly()
    {
        var a=Dynamic(0,default,default,default); var b=Fixed();
        Assert.Throws<InvalidOperationException>(()=>default(RigidRotation).Apply(X));
        Assert.Throws<ArgumentException>(()=>new RigidRotation(0,0,0,0));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Hinge(a,b,default,Frame()));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Limits(a,b,new(X,default,-X,default),0,2,1,.1));
        Assert.Throws<ArgumentException>(()=>JointConstraints.Rope(a,b,default,default,1,.1));
    }
}
