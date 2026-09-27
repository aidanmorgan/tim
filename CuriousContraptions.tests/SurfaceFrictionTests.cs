using Godot;

namespace CuriousContraptions.Tests;

public class SurfaceFrictionTests
{
    [Theory]
    [InlineData(0f,.012f,.3f,0f)]
    [InlineData(.001f,.012f,.3f,.0003f)]
    [InlineData(1f,.012f,.3f,.012f)]
    [InlineData(100f,20f,.3f,6f)]
    [InlineData(1f,0f,.3f,0f)]
    [InlineData(1f,.012f,0f,0f)]
    public void LossIsBoundedByNormalImpulseBudgetAndAvailableSpeed(
        float impulse,float budget,float coefficient,float expectedLoss)
    {
        var before=new Vector3(6,2,0);
        var result=SurfaceFriction.Apply(before,Vector3.Up,impulse,budget,coefficient);
        Assert.InRange(result.SpeedLoss,expectedLoss-.000001f,expectedLoss+.000001f);
        Assert.Equal(before.Y,result.Velocity.Y);
        Assert.InRange(result.Velocity.X,0,before.X);
        Assert.InRange(result.Velocity.LengthSquared(),0,before.LengthSquared());
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(47f)]
    [InlineData(90f)]
    public void RotatedContactRetainsNormalVelocity(float degrees)
    {
        var basis=new Basis(Vector3.Back,Mathf.DegToRad(degrees));
        var result=SurfaceFriction.Apply(basis*new Vector3(6,2,0),basis.Y,.01f,1,.3f);
        var local=basis.Inverse()*result.Velocity;
        Assert.InRange(local.X,5.99699f,5.99701f);
        Assert.InRange(local.Y,1.99999f,2.00001f);
    }

    [Fact]
    public void SubdividingNormalImpulseDoesNotMultiplyFrictionBudget()
    {
        const float budget=.01f,impulse=.05f,coefficient=.3f;
        var velocity=new Vector3(6,2,0);
        var single=SurfaceFriction.Apply(velocity,Vector3.Up,impulse,budget,coefficient);
        var remaining=budget;
        for(var i=0;i<100;i++)
        {
            var result=SurfaceFriction.Apply(velocity,Vector3.Up,impulse/100,remaining,coefficient);
            velocity=result.Velocity;
            remaining=Math.Max(0,remaining-result.SpeedLoss);
        }
        Assert.InRange(velocity.DistanceTo(single.Velocity),0,.00003f);
        Assert.InRange(remaining,0,.000001f);
    }

    [Fact]
    public void InvalidInputsAreRejected()
    {
        var velocity=Vector3.Right;
        Assert.Throws<ArgumentOutOfRangeException>(()=>SurfaceFriction.Apply(
            new(float.NaN,0,0),Vector3.Up,1,1,1));
        Assert.Throws<ArgumentException>(()=>SurfaceFriction.Apply(velocity,Vector3.Zero,1,1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SurfaceFriction.Apply(velocity,Vector3.Up,-1,1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SurfaceFriction.Apply(velocity,Vector3.Up,1,float.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SurfaceFriction.Apply(velocity,Vector3.Up,1,1,-1));
    }
}
