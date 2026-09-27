using Godot;

namespace CuriousContraptions.Tests;

public class SphereImpactTests
{
    [Theory]
    [InlineData(1f,1f,1f)]
    [InlineData(1f,4f,1f)]
    [InlineData(4f,1f,0f)]
    [InlineData(.5f,8f,.4f)]
    public void ImpulseConservesMomentumAndCannotAddEnergy(float aMass,float bMass,float bounce)
    {
        var a=new Vector3(10,2,3);var b=new Vector3(-4,1,-2);
        var result=SphereImpact.Resolve(a,aMass,b,bMass,Vector3.Left,bounce);
        var beforeMomentum=a*aMass+b*bMass;
        var afterMomentum=result.FirstVelocity*aMass+result.SecondVelocity*bMass;
        Assert.InRange((afterMomentum-beforeMomentum).Length(),0,.0001f);
        var before=.5f*aMass*a.LengthSquared()+.5f*bMass*b.LengthSquared();
        var after=.5f*aMass*result.FirstVelocity.LengthSquared()+.5f*bMass*result.SecondVelocity.LengthSquared();
        Assert.InRange(after,0,before+.001f);
        Assert.Equal(a.Y,result.FirstVelocity.Y);Assert.Equal(a.Z,result.FirstVelocity.Z);
        Assert.Equal(b.Y,result.SecondVelocity.Y);Assert.Equal(b.Z,result.SecondVelocity.Z);
    }

    [Fact]
    public void SweptImpactAdvancesToContactThenUsesRemainingTime()
    {
        var a=new Vector3(-5,0,0);var b=new Vector3(5,0,0);
        var va=new Vector3(20,0,0);var vb=new Vector3(-20,0,0);
        const float duration=.5f;
        var hit=MovingSphereSweep.Cast(a,.5f,va,b,.5f,vb,duration);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        a+=va*hit.Time;b+=vb*hit.Time;
        var impact=SphereImpact.Resolve(va,1,vb,1,hit.Normal,1);
        a+=impact.FirstVelocity*(duration-hit.Time);
        b+=impact.SecondVelocity*(duration-hit.Time);
        Assert.InRange(a.X,-6.0001f,-5.9999f);
        Assert.InRange(b.X,5.9999f,6.0001f);
        Assert.InRange(a.DistanceTo(b),11.999f,12.001f);
    }

    [Fact]
    public void SwappingBodiesPreservesResult()
    {
        var a=new Vector3(10,2,0);var b=new Vector3(-4,0,0);
        var first=SphereImpact.Resolve(a,2,b,4,Vector3.Left,.5f);
        var second=SphereImpact.Resolve(b,4,a,2,Vector3.Right,.5f);
        Assert.Equal(first.FirstVelocity,second.SecondVelocity);
        Assert.Equal(first.SecondVelocity,second.FirstVelocity);
    }

    [Fact]
    public void SeparatingContactHasNoImpulse()
    {
        var result=SphereImpact.Resolve(Vector3.Left,1,Vector3.Right,1,Vector3.Left,1);
        Assert.Equal(0,result.Impulse);Assert.Equal(Vector3.Left,result.FirstVelocity);Assert.Equal(Vector3.Right,result.SecondVelocity);
    }

    [Fact]
    public void InvalidPhysicalArgumentsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereImpact.Resolve(Vector3.Zero,0,Vector3.Zero,1,Vector3.Up,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereImpact.Resolve(Vector3.Zero,1,Vector3.Zero,float.NaN,Vector3.Up,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereImpact.Resolve(Vector3.Zero,1,Vector3.Zero,1,Vector3.Up,1.1f));
        Assert.Throws<ArgumentException>(()=>SphereImpact.Resolve(Vector3.Zero,1,Vector3.Zero,1,Vector3.Up*2,1));
    }
}
