using Godot;

namespace CuriousContraptions.Tests;

public class MovingSphereSweepTests
{
    [Theory]
    [InlineData(10f)]
    [InlineData(100f)]
    [InlineData(10000f)]
    public void OpposingFastBodiesCannotCrossUndetected(float speed)
    {
        var hit=MovingSphereSweep.Cast(Vector3.Left*5,.5f,Vector3.Right*speed,
            Vector3.Right*5,.5f,Vector3.Left*speed,10/speed);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,4.5f/speed-1e-6f,4.5f/speed+1e-6f);
        Assert.Equal(Vector3.Left,hit.Normal);
        var separation=10-2*(double)speed*hit.Time;
        Assert.True(separation>=1-1e-7);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    [InlineData(90f)]
    public void QueryIsSymmetricAndInvariantUnderSharedVelocity(float angle)
    {
        var basis=new Basis(Vector3.Up,Mathf.DegToRad(angle));
        var a=basis*new Vector3(-3,0,0);var b=basis*new Vector3(3,0,0);
        var va=basis*new Vector3(4,0,0);var vb=basis*new Vector3(-2,0,0);
        var shared=new Vector3(3,-5,7);
        var hit=MovingSphereSweep.Cast(a,.3f,va,b,.7f,vb,2);
        var reverse=MovingSphereSweep.Cast(b,.7f,vb,a,.3f,va,2);
        var shifted=MovingSphereSweep.Cast(a,.3f,va+shared,b,.7f,vb+shared,2);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.Equal(hit.Time,reverse.Time);
        Assert.InRange((hit.Normal+reverse.Normal).Length(),0,.00001f);
        Assert.InRange(Math.Abs(hit.Time-shifted.Time),0,.00001f);
    }

    [Fact]
    public void GlancingIntersectionAndNearMissAreDistinguished()
    {
        var hit=MovingSphereSweep.Cast(new(-2,.99f,0),.5f,Vector3.Right*4,
            Vector3.Zero,.5f,Vector3.Zero,1);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,.46f,.47f);
        Assert.True(hit.Normal.X<0);Assert.True(hit.Normal.Y>.98f);
        var miss=MovingSphereSweep.Cast(new(-2,1.01f,0),.5f,Vector3.Right*4,
            Vector3.Zero,.5f,Vector3.Zero,1);
        Assert.Equal(SphereSweepStatus.Clear,miss.Status);
    }

    [Theory]
    [InlineData(-1f,SphereSweepStatus.Contact)]
    [InlineData(0f,SphereSweepStatus.Clear)]
    [InlineData(1f,SphereSweepStatus.Clear)]
    public void InitialTouchOnlyBlocksApproach(float velocity,SphereSweepStatus status)
    {
        var hit=MovingSphereSweep.Cast(Vector3.Right,.5f,Vector3.Right*velocity,
            Vector3.Zero,.5f,Vector3.Zero,1);
        Assert.Equal(status,hit.Status);
    }

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(1e-20f)]
    [InlineData(1f)]
    public void TinyAxialApproachIsNotLostToTangencyRoundoff(float speed)
    {
        foreach (var reverse in new[] { false, true })
        {
            var hit = reverse
                ? MovingSphereSweep.Cast(Vector3.Zero, .5f, Vector3.Zero,
                    Vector3.Right, .5f, Vector3.Left * speed, 1)
                : MovingSphereSweep.Cast(Vector3.Right, .5f, Vector3.Left * speed,
                    Vector3.Zero, .5f, Vector3.Zero, 1);
            Assert.Equal(SphereSweepStatus.Contact, hit.Status);
            Assert.Equal(0, hit.Time);
        }
    }

    [Theory]
    [InlineData(1e-10f)]
    [InlineData(1f)]
    [InlineData(1e10f)]
    public void ResolvedObliqueContactDoesNotRepeatAtZeroTime(float scale)
    {
        var first = new Vector3(0, 3.8460832f, .2267073f);
        var second = new Vector3(0, 4.4737387f, .43078774f);
        var tangent = new Vector3(0, -.13901666f, .42754984f) * scale;
        foreach (var reverse in new[] { false, true })
        {
            var clear = reverse
                ? MovingSphereSweep.Cast(second, .34f, tangent, first, .32f, Vector3.Zero, 1)
                : MovingSphereSweep.Cast(first, .32f, Vector3.Zero, second, .34f, tangent, 1);
            Assert.Equal(SphereSweepStatus.Clear, clear.Status);
            var inward = tangent + (first - second).Normalized() * (.001f * scale);
            var contact = reverse
                ? MovingSphereSweep.Cast(second, .34f, inward, first, .32f, Vector3.Zero, 1)
                : MovingSphereSweep.Cast(first, .32f, Vector3.Zero, second, .34f, inward, 1);
            Assert.Equal(SphereSweepStatus.Contact, contact.Status);
        }
    }

    [Fact]
    public void StationaryOverlapReportsDepthAndDoesNotPretendToRepairIt()
    {
        var hit=MovingSphereSweep.Cast(Vector3.Right*.5f,.5f,Vector3.Zero,
            Vector3.Zero,.5f,Vector3.Zero,0);
        Assert.Equal(SphereSweepStatus.Overlapping,hit.Status);
        Assert.Equal(.5f,hit.Penetration);Assert.Equal(Vector3.Right,hit.Normal);
        var coincident=MovingSphereSweep.Cast(Vector3.Zero,.5f,Vector3.Right,
            Vector3.Zero,.5f,Vector3.Left,1);
        Assert.Equal(Vector3.Left,coincident.Normal);
        Assert.Equal(1,coincident.Penetration);
    }

    [Fact]
    public void EndpointContactAndLaterContactHaveDifferentResults()
    {
        var atEnd=MovingSphereSweep.Cast(Vector3.Left*3,.5f,Vector3.Right*2,
            Vector3.Zero,.5f,Vector3.Zero,1);
        Assert.Equal(SphereSweepStatus.Contact,atEnd.Status);Assert.Equal(1,atEnd.Time);
        var later=MovingSphereSweep.Cast(Vector3.Left*3,.5f,Vector3.Right,
            Vector3.Zero,.5f,Vector3.Zero,1);
        Assert.Equal(SphereSweepStatus.Clear,later.Status);
    }

    [Fact]
    public void IdenticalVelocityAndRecedingBodiesStayClear()
    {
        Assert.Equal(SphereSweepStatus.Clear,MovingSphereSweep.Cast(Vector3.Left*3,.5f,Vector3.One,
            Vector3.Zero,.5f,Vector3.One,10).Status);
        Assert.Equal(SphereSweepStatus.Clear,MovingSphereSweep.Cast(Vector3.Left*3,.5f,Vector3.Left,
            Vector3.Zero,.5f,Vector3.Right,10).Status);
    }

    [Fact]
    public void LargeFiniteCoordinatesDoNotOverflowFloatIntermediates()
    {
        var hit=MovingSphereSweep.Cast(new(-1e20f,0,0),1e19f,new(1e20f,0,0),
            new(1e20f,0,0),1e19f,new(-1e20f,0,0),2);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,.89999f,.90001f);Assert.Equal(Vector3.Left,hit.Normal);
    }

    [Theory]
    [InlineData(0f,1f)]
    [InlineData(-1f,1f)]
    [InlineData(float.NaN,1f)]
    [InlineData(1f,-1f)]
    [InlineData(1f,float.PositiveInfinity)]
    public void InvalidRadiusOrDurationIsRejected(float radius,float duration)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>MovingSphereSweep.Cast(Vector3.Zero,radius,Vector3.Zero,
            Vector3.One,1,Vector3.Zero,duration));

    [Fact]
    public void InvalidVectorsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>MovingSphereSweep.Cast(new(float.NaN,0,0),1,Vector3.Zero,
            Vector3.One,1,Vector3.Zero,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MovingSphereSweep.Cast(Vector3.Zero,1,Vector3.Zero,
            Vector3.One,1,new(0,float.PositiveInfinity,0),1));
    }
}
