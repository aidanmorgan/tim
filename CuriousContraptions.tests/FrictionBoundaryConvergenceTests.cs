using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class FrictionBoundaryConvergenceTests
{
    [Theory]
    [InlineData(1e-7,false)]
    [InlineData(1e-7,true)]
    [InlineData(-1e-7,false)]
    [InlineData(-1e-7,true)]
    [InlineData(.1,false)]
    [InlineData(.1,true)]
    [InlineData(-.1,false)]
    [InlineData(-.1,true)]
    public void BlockedTangentialAccelerationReachesCoulombBoundary(double bias,bool reverse)
    {
        var x=new CollisionVector(1,0,0);var y=new CollisionVector(0,1,0);var z=new CollisionVector(0,0,1);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,-y,default,1,new(1,1,1));
        var map=new ContactKinematics(y,new([new(body,y,default)]),
            new([new(body,-z,default)]),new([new(body,-x,default)]));
        var contact=ContactConstraint.ForAcceleration(map,0,x*bias,default,.2,FrictionRegime.Sticking);
        var guide=new ImpulseConstraint(new([new(body,x,default)]),0,double.NegativeInfinity,double.PositiveInfinity);
        var block=new BilateralConstraintBlock([guide]);
        IImpulseConstraint[] constraints=reverse?[contact,block]:[block,contact];
        var result=ImpulseSolver.Solve(constraints,tolerance:1e-10);
        // The guide fixes ax=0, so nonzero material bias cannot be cancelled by
        // an interior friction force. Complementarity requires the disk boundary.
        Assert.InRange(result.MaximumResidual,0,1e-10);
        Assert.InRange((body.LinearVelocity-default(CollisionVector)).Length,0,1e-10);
        Assert.InRange(Math.Abs(contact.Impulse.Normal-1),0,1e-10);
        Assert.InRange((contact.TangentImpulse+x*(Math.Sign(bias)*.2)).Length,0,1e-10);
        Assert.InRange(Math.Abs(guide.AccumulatedImpulse-Math.Sign(bias)*.2),0,1e-10);
        Assert.InRange((-y+y*contact.Impulse.Normal+contact.TangentImpulse+x*guide.AccumulatedImpulse-body.LinearVelocity).Length,0,1e-12);
    }
}
