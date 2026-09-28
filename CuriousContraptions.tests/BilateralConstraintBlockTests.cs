using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BilateralConstraintBlockTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity,CollisionVector angular)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,angular,2,new InertiaTensor(2,3,4,.2,.1,.3));
    private static PhysicsBody Ground()=>new(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void OffsetJointBlockConvergesWithoutInflatingIterationBudget(double length)
    {
        var a=Body(0,Z*length,new(3,2,1),new(2,3,4)); var ground=Ground();
        var fa=new JointFrame(default,RigidRotation.Identity);
        var rows=JointConstraints.Slider(a,ground,fa,fa);
        var result=ImpulseSolver.Solve(rows,8,1e-7);
        Assert.InRange(result.MaximumResidual,0,1e-7);
        Assert.InRange(a.AngularVelocity.Length,0,1e-7);
        Assert.InRange(Math.Abs(a.LinearVelocity.X)+Math.Abs(a.LinearVelocity.Y),0,1e-7);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-1),0,1e-7);
    }
    [Fact]
    public void HingeAndFrictionContactShareOneCoupledSolve()
    {
        var beam=Body(0,X,default,default); var ground=Ground();
        var ball=Body(1,X*2+Y,new(1,-3,.5),default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var constraints=new List<IImpulseConstraint>(JointConstraints.Hinge(beam,ground,frame,frame));
        var contact=new ContactConstraint(ball,beam,X*2,Y,0,0,.4);
        constraints.Add(contact);
        var solved=ImpulseSolver.Solve(constraints);
        Assert.InRange(solved.MaximumResidual,0,1e-8);
        Assert.InRange(beam.PointVelocity(default).Length,0,1e-8);
        Assert.True(contact.Normal.AccumulatedImpulse>0);
        Assert.True(contact.TangentImpulse.Length>0);
        Assert.InRange(Math.Abs(beam.AngularVelocity.X)+Math.Abs(beam.AngularVelocity.Y),0,1e-8);
    }
    [Fact]
    public void DependentOrBoundedRowsAreRejectedRatherThanRegularized()
    {
        var a=Body(0,default,default,default); var b=Ground();
        ImpulseConstraint Row()=>new(new ConstraintJacobian(X,default,-X,default).Bind(a,b),0,double.NegativeInfinity,double.PositiveInfinity);
        Assert.Throws<ArgumentException>(()=>new BilateralConstraintBlock([Row(),Row()]));
        Assert.Throws<ArgumentException>(()=>new BilateralConstraintBlock([ImpulseConstraint.Contact(a,b,default,X,0,0)]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BilateralConstraintBlock([]));
    }
}
