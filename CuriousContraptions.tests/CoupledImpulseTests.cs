using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CoupledImpulseTests
{
    private static PhysicsBody Body(int id,CollisionVector velocity=default,PhysicsMotionType type=PhysicsMotionType.Dynamic)=>
        type==PhysicsMotionType.Dynamic?
            new(new(id),type,RigidPose.Identity,velocity,default,1,new(1,1,1)):
            new(new(id),type,RigidPose.Identity,default,default);

    [Theory]
    [InlineData(.01)]
    [InlineData(.0001)]
    [InlineData(.000001)]
    public void AlmostParallelContactsRetractObsoleteWarmImpulseInOneCoupledIteration(double tilt)
    {
        var body=Body(0,new(2,-.1,0)); var wall=Body(1,type:PhysicsMotionType.Static);
        var direction=new CollisionVector(tilt,1,0); direction/=direction.Length;
        var oblique=new ContactConstraint(ContactKinematics.AtPoint(body,wall,default,direction),0,0,0);
        var support=new ContactConstraint(ContactKinematics.AtPoint(body,wall,default,new(0,1,0)),0,0,0);
        oblique.WarmStart(new(.1,default));
        var result=ImpulseSolver.Solve([oblique,support],1);
        Assert.Equal(1,result.Iterations);
        Assert.InRange(oblique.Normal.AccumulatedImpulse,0,1e-12);
        Assert.InRange((body.LinearVelocity-new CollisionVector(2,0,0)).Length,0,1e-12);
    }

    [Fact]
    public void BoundedPairsMatchIndependentQuadraticGridAndComplementarity()
    {
        var random=new Random(811);
        for(var sample=0;sample<400;sample++)
        {
            var body=Body(0,new(random.NextDouble()*4-2,random.NextDouble()*4-2,0));
            var first=new ConstraintGradient([new(body,new(1,0,0),default)]);
            var angle=.1+random.NextDouble()*2.9;
            var second=new ConstraintGradient([new(body,new(Math.Cos(angle),Math.Sin(angle),0),default)]);
            ImpulseConstraint Row(ConstraintGradient g)=>new(g,random.NextDouble()*4-2,-random.NextDouble()*2,random.NextDouble()*2,random.NextDouble()*.2);
            var a=Row(first); var b=Row(second);
            var ea=a.TargetSpeed-first.Speed; var eb=b.TargetSpeed-second.Speed;
            var cross=first.Coupling(second);
            double Energy(double x,double y)=>.5*(a.InverseEffectiveMass*x*x+2*cross*x*y+b.InverseEffectiveMass*y*y)-ea*x-eb*y;
            var result=ImpulseSolver.Solve([a,b],1);
            Assert.InRange(result.MaximumResidual,0,1e-8);
            var optimum=Energy(a.AccumulatedImpulse,b.AccumulatedImpulse);
            for(var i=0;i<=20;i++)
            for(var j=0;j<=20;j++)
            {
                var x=a.MinimumImpulse+(a.MaximumImpulse-a.MinimumImpulse)*i/20;
                var y=b.MinimumImpulse+(b.MaximumImpulse-b.MinimumImpulse)*j/20;
                Assert.True(optimum<=Energy(x,y)+1e-10);
            }
        }
    }

    [Fact]
    public void DuplicateRowsRetainFiniteImpulsesAndDoNotDoubleTheResponse()
    {
        var body=Body(0,new(0,-3,0));
        var gradient=new ConstraintGradient([new(body,new(0,1,0),default)]);
        var a=new ImpulseConstraint(gradient,0,0,double.PositiveInfinity);
        var b=new ImpulseConstraint(gradient,0,0,double.PositiveInfinity);
        ImpulseSolver.Solve([a,b],1);
        Assert.Equal(default,body.LinearVelocity);
        Assert.Equal(3,a.AccumulatedImpulse+b.AccumulatedImpulse);
    }

    [Fact]
    public void CoupledMultiBodyRowsConserveMomentum()
    {
        var a=Body(0,new(2,-3,0)); var b=Body(1,new(-1,1,0));
        var c=Body(2,new(4,2,0));
        var x=new CollisionVector(1,0,0); var y=new CollisionVector(.5,1,0);
        var first=new ImpulseConstraint(new([new(a,x,default),new(b,-x,default)]),0,double.NegativeInfinity,0);
        var second=new ImpulseConstraint(new([new(a,y,default),new(c,-y,default)]),0,0,double.PositiveInfinity);
        var momentum=a.LinearVelocity+b.LinearVelocity+c.LinearVelocity;
        var energy=a.KineticEnergy+b.KineticEnergy+c.KineticEnergy;
        ImpulseSolver.Solve([first,second],1);
        Assert.InRange((momentum-a.LinearVelocity-b.LinearVelocity-c.LinearVelocity).Length,0,1e-12);
        Assert.True(a.KineticEnergy+b.KineticEnergy+c.KineticEnergy<=energy+1e-12);
    }
}
