using Godot;
using CuriousContraptions.Physics;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorldSweepShapeTests
{
    private static readonly Vector3 Offset=new(0,10,0);
    private static WorldSweepResult Cast(Vector3 origin,float radius,Vector3 displacement,
        Action<MachinePart> declare,Transform3D? pose=null)
    {
        using var scene=new GeometryQueryScene();
        var world=scene.World; var part=scene.Part;
        var transform=pose??Transform3D.Identity;
        transform.Origin+=Offset; part.Transform=transform;
        declare(part);
        return WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(radius),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin+Offset)),SceneGeometryAdapter.CaptureVector(displacement));
    }
    [Theory]
    [InlineData(.001f)]
    [InlineData(.1f)]
    [InlineData(1f)]
    public void CannotSkipThinBoxes(float halfWidth)
    {
        var result = Cast(new(-5,0,0), .25f, new(10,0,0),
            p => p.Boxes.Add(new(Vector3.Zero,new(halfWidth,1,1), MachinePart.RootBody)));
        Assert.Equal(WorldSweepStatus.Contact, result.Status);
        Assert.InRange(result.Distance, 4.75f-halfWidth-.0002f, 4.75f-halfWidth+.0002f);
        Assert.Equal(SceneGeometryAdapter.CaptureVector(Vector3.Left), result.Normal);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    [InlineData(90f)]
    [InlineData(180f)]
    public void RigidTransformsPreserveTravelAndNormal(float degrees)
    {
        var pose = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(degrees)),new(2,3,4));
        var result = Cast(pose * new Vector3(-5,0,0), .25f, pose.Basis * new Vector3(10,0,0),
            p=>p.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody)),pose);
        Assert.Equal(WorldSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,3.749f,3.751f);
        Assert.InRange((result.Normal-SceneGeometryAdapter.CaptureVector(pose.Basis*Vector3.Left)).Length,0,.001f);
    }

    [Fact]
    public void MissAndSeparatingOrTangentTravelRemainClear()
    {
        foreach(var (origin,displacement) in new (Vector3,Vector3)[] {
            (new(-3,1.3f,0),new(6,0,0)),
            (new(-1.25f,0,0),new(-2,0,0)),
            (new(-1.25f,0,0),new(0,2,0)),
            (new(-1.25f,0,0),Vector3.Zero) })
            Assert.Equal(WorldSweepStatus.Clear,Cast(origin,.25f,displacement,
                p=>p.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody))).Status);
    }

    [Fact]
    public void OverlapIsExplicitEvenWithoutMovement()
    {
        foreach(var displacement in new[] {Vector3.Zero,Vector3.Left*5})
        {
            var result=Cast(Vector3.Zero,.25f,displacement,
                p=>p.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody)));
            Assert.Equal(WorldSweepStatus.Overlapping,result.Status);
            Assert.Equal(0,result.Distance);
            Assert.Equal(1.25f,result.Penetration);
        }
    }

    [Fact]
    public void EndpointContactIsNotSkipped()
    {
        var result=Cast(new(-3,0,0),.25f,new(1.75f,0,0),
            p=>p.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody)));
        Assert.Equal(WorldSweepStatus.Contact,result.Status);
        // The double query retains the solver's contact bracket; do not round it to a scene float.
        Assert.InRange(Math.Abs(result.Distance-1.75),0,1e-7);
    }

    [Fact]
    public void SpheresUseCombinedRadiiInBothDirections()
    {
        foreach(var sign in new[]{-1,1})
        {
            var result=Cast(Vector3.Right*sign*3,.25f,Vector3.Left*sign*6,
                p=>p.Spheres.Add(new(Vector3.Zero,.5f,MachinePart.RootBody)));
            Assert.Equal(WorldSweepStatus.Contact,result.Status);
            Assert.InRange(result.Distance,2.2498f,2.2502f);
            Assert.Equal(SceneGeometryAdapter.CaptureVector(Vector3.Right*sign),result.Normal);
        }
    }

    [Fact]
    public void TubeBoreIsOpenButShellStopsHead()
    {
        var tube=new TubeProxy(Transform3D.Identity,2,1,1.1f,false);
        Assert.Equal(WorldSweepStatus.Clear,
            Cast(new(-3,0,0),.25f,new(6,0,0),p=>p.Tubes.Add(tube)).Status);
        var wall=Cast(Vector3.Zero,.25f,Vector3.Up*2,p=>p.Tubes.Add(tube));
        Assert.Equal(WorldSweepStatus.Contact,wall.Status);
        Assert.InRange(wall.Distance,.7449f,.7501f); // Declared 0.005-unit shell bound.
    }

    [Fact]
    public void TransverseTravelInsideBoreDoesNotIgnoreLaterCollision()
    {
        var tube=new TubeProxy(Transform3D.Identity,2,1,1.1f,false);
        var result=Cast(new(0,.744f,0),.25f,Vector3.Back,p=>p.Tubes.Add(tube));
        Assert.Equal(WorldSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,.01f,.12f); // Starts inside the guaranteed bore, then enters another convex cell.
    }

    [Fact]
    public void FrustumNarrowingStopsOversizeHead()
    {
        var funnel=new FrustumProxy(Transform3D.Identity,2,1,.2f,.1f);
        var result=Cast(new(-3,0,0),.3f,new(6,0,0),p=>p.Frustums.Add(funnel));
        Assert.Equal(WorldSweepStatus.Contact,result.Status);
        Assert.InRange(result.Distance,4.4f,4.6f);
        Assert.Equal(WorldSweepStatus.Clear,
            Cast(new(-3,0,0),.1f,new(6,0,0),p=>p.Frustums.Add(funnel)).Status);
    }

    [Fact]
    public void BendBoreIsNotTreatedAsSolidBoundingBox()
    {
        var bend=new BendProxy(Transform3D.Identity,2,Mathf.Pi/2,1,1.1f);
        var origin=bend.Centre(Mathf.Pi/4);
        Assert.Equal(WorldSweepStatus.Clear,
            Cast(origin,.25f,BendProxy.Tangent(Mathf.Pi/4)*.1f,p=>p.Bends.Add(bend)).Status);
        Assert.Equal(WorldSweepStatus.Contact,
            Cast(origin,.25f,Vector3.Back*2,p=>p.Bends.Add(bend)).Status);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void RejectsInvalidRadius(float radius) =>
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(Vector3.Zero,radius,Vector3.Right,
            p=>p.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody))));

    [Fact]
    public void RejectsInvalidGeometryInsteadOfInventingClearance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(Vector3.Zero,1,Vector3.Right,
            p=>p.Boxes.Add(new(Vector3.Zero,new(0,1,1), MachinePart.RootBody))));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(Vector3.Zero,1,Vector3.Right,
            p=>p.Spheres.Add(new(Vector3.Zero,float.NaN,MachinePart.RootBody))));
        Assert.Throws<ArgumentException>(()=>Cast(new(float.NaN,0,0),1,Vector3.Right,
            p=>p.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody))));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(Vector3.Zero,1,new(float.PositiveInfinity,0,0),
            p=>p.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody))));
    }
    public enum InvalidPose { Scale, Shear, Reflection, Nonfinite }
    [Theory]
    [InlineData(InvalidPose.Scale)]
    [InlineData(InvalidPose.Shear)]
    [InlineData(InvalidPose.Reflection)]
    [InlineData(InvalidPose.Nonfinite)]
    public void ScenePoseBoundaryRejectsNonRigidTransforms(InvalidPose kind)
    {
        var pose=Transform3D.Identity;
        pose.Basis=kind switch
        {
            InvalidPose.Scale=>new Basis(new Vector3(2,0,0),Vector3.Up,Vector3.Back),
            InvalidPose.Shear=>new Basis(Vector3.Right,new Vector3(.2f,1,0),Vector3.Back),
            InvalidPose.Reflection=>new Basis(Vector3.Left,Vector3.Up,Vector3.Back),
            InvalidPose.Nonfinite=>new Basis(new Vector3(float.NaN,0,0),Vector3.Up,Vector3.Back),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        Assert.Throws<ArgumentException>(()=>SceneGeometryAdapter.CaptureRigidPose(pose));
    }

    [Fact]
    public void ScenePoseBoundaryPreservesAuthoredRigidMotion()
    {
        var pose=new Transform3D(Basis.FromEuler(new(.3f,.7f,-.2f)),new(3,5,-2));
        var rigid=SceneGeometryAdapter.CaptureRigidPose(pose);
        var point=new Vector3(.2f,-.4f,1);
        var actual=rigid.TransformPoint(SceneGeometryAdapter.CaptureVector(point));
        Assert.InRange((actual-SceneGeometryAdapter.CaptureVector(pose*point)).Length,0,.000001);
        Assert.InRange((rigid.InverseTransformPoint(actual)-SceneGeometryAdapter.CaptureVector(point)).Length,0,1e-12);
    }

    [Fact]
    public void AnalyticBoreTangencyInsideCompiledShellIsExplicitOverlap()
    {
        var hit=Cast(new(0,.75f,0),.25f,Vector3.Back,
            p=>p.Tubes.Add(new(Transform3D.Identity,2,1,1.1f,false)));
        Assert.Equal(WorldSweepStatus.Overlapping,hit.Status);
        Assert.InRange(hit.Penetration,0,.0051f);
    }

}
