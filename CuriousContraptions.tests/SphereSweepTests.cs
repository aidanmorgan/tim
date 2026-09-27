using Godot;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class SphereSweepTests
{
    [Theory]
    [InlineData(.001f)]
    [InlineData(.1f)]
    [InlineData(1f)]
    public void CannotSkipThinBoxes(float halfWidth)
    {
        var result = SphereSweep.Cast(new(-5,0,0), .25f, new(10,0,0),
            p => SphereSweep.BoxSurface(p, new(halfWidth,1,1)));
        Assert.Equal(SphereSweepStatus.Contact, result.Status);
        Assert.InRange(result.Distance, 4.75f-halfWidth-.0002f, 4.75f-halfWidth+.0002f);
        Assert.Equal(Vector3.Left, result.Normal);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    [InlineData(90f)]
    [InlineData(180f)]
    public void RigidTransformsPreserveTravelAndNormal(float degrees)
    {
        var pose = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(degrees)),new(2,3,4));
        var inverse = pose.AffineInverse();
        var result = SphereSweep.Cast(pose * new Vector3(-5,0,0), .25f, pose.Basis * new Vector3(10,0,0), p =>
        {
            var sample = SphereSweep.BoxSurface(inverse*p,Vector3.One);
            return (pose.Basis*sample.Normal,sample.Distance);
        });
        Assert.Equal(SphereSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,3.749f,3.751f);
        Assert.InRange(result.Normal.DistanceTo(pose.Basis*Vector3.Left),0,.001f);
    }

    [Fact]
    public void MissAndSeparatingOrTangentTravelRemainClear()
    {
        foreach(var (origin,displacement) in new (Vector3,Vector3)[] {
            (new(-3,1.3f,0),new(6,0,0)),
            (new(-1.25f,0,0),new(-2,0,0)),
            (new(-1.25f,0,0),new(0,2,0)),
            (new(-1.25f,0,0),Vector3.Zero) })
            Assert.Equal(SphereSweepStatus.Clear,SphereSweep.Cast(origin,.25f,displacement,
                p=>SphereSweep.BoxSurface(p,Vector3.One)).Status);
    }

    [Fact]
    public void OverlapIsExplicitEvenWithoutMovement()
    {
        foreach(var displacement in new[] {Vector3.Zero,Vector3.Left*5})
        {
            var result=SphereSweep.Cast(Vector3.Zero,.25f,displacement,
                p=>SphereSweep.BoxSurface(p,Vector3.One));
            Assert.Equal(SphereSweepStatus.Overlapping,result.Status);
            Assert.Equal(0,result.Distance);
        }
    }

    [Fact]
    public void EndpointContactIsNotSkipped()
    {
        var result=SphereSweep.Cast(new(-3,0,0),.25f,new(1.75f,0,0),
            p=>SphereSweep.BoxSurface(p,Vector3.One));
        Assert.Equal(SphereSweepStatus.Contact,result.Status);
        Assert.Equal(1.75f,result.Distance);
    }

    [Fact]
    public void SpheresUseCombinedRadiiInBothDirections()
    {
        foreach(var sign in new[]{-1,1})
        {
            var result=SphereSweep.Cast(Vector3.Right*sign*3,.25f,Vector3.Left*sign*6,
                p=>SphereSweep.SphereSurface(p,.5f));
            Assert.Equal(SphereSweepStatus.Contact,result.Status);
            Assert.InRange(result.Distance,2.2498f,2.2502f);
            Assert.Equal(Vector3.Right*sign,result.Normal);
        }
    }

    [Fact]
    public void TubeBoreIsOpenButShellStopsHead()
    {
        var tube=new TubeProxy(Transform3D.Identity,2,1,1.1f,false);
        Assert.Equal(SphereSweepStatus.Clear,
            SphereSweep.Cast(new(-3,0,0),.25f,new(6,0,0),tube.Surface).Status);
        var wall=SphereSweep.Cast(Vector3.Zero,.25f,Vector3.Up*2,tube.Surface);
        Assert.Equal(SphereSweepStatus.Contact,wall.Status);
        Assert.InRange(wall.Distance,.7498f,.7502f);
    }

    [Fact]
    public void TangencyInsideCurvedBoreDoesNotIgnoreLaterCollision()
    {
        var tube=new TubeProxy(Transform3D.Identity,2,1,1.1f,false);
        var result=SphereSweep.Cast(new(0,.75f,0),.25f,Vector3.Back,tube.Surface);
        Assert.Equal(SphereSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,0,.001f);
    }

    [Fact]
    public void FrustumNarrowingStopsOversizeHead()
    {
        var funnel=new FrustumProxy(Transform3D.Identity,2,1,.2f,.1f);
        var result=SphereSweep.Cast(new(-3,0,0),.3f,new(6,0,0),funnel.Surface);
        Assert.Equal(SphereSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,4.4f,4.6f);
        Assert.Equal(SphereSweepStatus.Clear,
            SphereSweep.Cast(new(-3,0,0),.1f,new(6,0,0),funnel.Surface).Status);
    }

    [Fact]
    public void BendBoreIsNotTreatedAsSolidBoundingBox()
    {
        var bend=new BendProxy(Transform3D.Identity,2,Mathf.Pi/2,1,1.1f);
        var origin=bend.Centre(Mathf.Pi/4);
        Assert.Equal(SphereSweepStatus.Clear,
            SphereSweep.Cast(origin,.25f,BendProxy.Tangent(Mathf.Pi/4)*.1f,bend.Surface).Status);
        Assert.Equal(SphereSweepStatus.Contact,
            SphereSweep.Cast(origin,.25f,Vector3.Back*2,bend.Surface).Status);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RejectsInvalidRadius(float radius) =>
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereSweep.Cast(Vector3.Zero,radius,Vector3.Right,
            p=>SphereSweep.SphereSurface(p,1)));

    [Fact]
    public void RejectsInvalidGeometryInsteadOfInventingClearance()
    {
        Assert.Throws<ArgumentException>(()=>SphereSweep.Cast(Vector3.Zero,1,Vector3.Right,
            _=>(Vector3.Zero,2)));
        Assert.Throws<ArgumentException>(()=>SphereSweep.Cast(Vector3.Zero,1,Vector3.Right,
            _=>(Vector3.Up,float.NaN)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereSweep.Cast(new(float.NaN,0,0),1,Vector3.Right,
            p=>SphereSweep.SphereSurface(p,1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SphereSweep.Cast(Vector3.Zero,1,new(float.PositiveInfinity,0,0),
            p=>SphereSweep.SphereSurface(p,1)));
    }
}
