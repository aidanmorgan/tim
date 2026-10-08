using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class TangentQuadraticTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0);
    private static PhysicsBody Body()=>new(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
    private static ConstraintGradient Gradient(PhysicsBody body,CollisionVector axis)=>new([new(body,axis,default)]);
    private static void Near(double expected,double actual,double tolerance=1e-12)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    public void RankOneRetainsMovableAndNullDirections(double angle)
    {
        var body=Body();var a=Math.Cos(angle);var b=Math.Sin(angle);
        var mass=new TangentQuadratic(Gradient(body,X*a),Gradient(body,X*b));
        var free=mass.Minimum(a*2,b*2,4);
        // A rank-one quadratic fixes the movable response; null impulses need
        // not be unique when the applied load lies in its range.
        Near(2,a*free.X+b*free.Y);
        Assert.InRange(Math.Sqrt(free.X*free.X+free.Y*free.Y),0,4+1e-14);
        var boundary=mass.Minimum(-b*3,a*3,2);
        Near(-b*2,boundary.X);Near(a*2,boundary.Y);
        Assert.Equal(default,body.LinearVelocity);
    }

    [Fact]
    public void ZeroMassMinimizesLinearWorkOnTheDisk()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var mass=new TangentQuadratic(Gradient(body,X),Gradient(body,Y));
        var result=mass.Minimum(3,4,2);
        Near(1.2,result.X);Near(1.6,result.Y);
        Assert.Equal((0d,0d),mass.Minimum(0,0,2));
        Assert.Equal((0d,0d),mass.Minimum(3,4,0));
    }

    [Theory]
    [InlineData(.7)]
    [InlineData(10)]
    public void AnisotropicMinimumSatisfiesStationarityAndCone(double radius)
    {
        var body=Body();var mass=new TangentQuadratic(Gradient(body,X*2),Gradient(body,X+Y*3));
        var result=mass.Minimum(5,4,radius);
        var length=Math.Sqrt(result.X*result.X+result.Y*result.Y);
        Assert.InRange(length,0,radius+1e-15);
        var gx=4*result.X+2*result.Y-5;var gy=2*result.X+10*result.Y-4;
        var lambda=length<radius-1e-12?0:-(gx*result.X+gy*result.Y)/(radius*radius);
        Assert.True(lambda>=0);
        Near(0,gx+lambda*result.X);Near(0,gy+lambda*result.Y);
        Assert.Equal(default,body.LinearVelocity);
    }

    [Theory]
    [InlineData(1e-100)]
    [InlineData(1e-180)]
    [InlineData(1e-280)]
    public void TinyDiskDoesNotEraseOpposingImpulse(double radius)
    {
        var body=Body();var mass=new TangentQuadratic(Gradient(body,X),Gradient(body,Y));
        var result=mass.Minimum(3,4,radius);
        Near(.6,result.X/radius);Near(.8,result.Y/radius);
    }

    [Fact]
    public void InvalidLoadsAndRadiiReject()
    {
        var body=Body();var mass=new TangentQuadratic(Gradient(body,X),Gradient(body,Y));
        Assert.Throws<ArgumentException>(()=>mass.Minimum(double.NaN,0,1));
        Assert.Throws<ArgumentException>(()=>mass.Minimum(0,double.PositiveInfinity,1));
        Assert.Throws<ArgumentException>(()=>mass.Minimum(0,0,-1));
        Assert.Throws<ArgumentException>(()=>mass.Minimum(0,0,double.PositiveInfinity));
    }
}

